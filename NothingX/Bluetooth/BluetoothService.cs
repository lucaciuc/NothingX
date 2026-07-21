using System.Diagnostics;
using NothingX.Protocol;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace NothingX.Bluetooth;

/// <summary>
/// Manages RFCOMM Bluetooth connections to Nothing earbuds.
/// Uses Windows.Devices.Bluetooth.Rfcomm WinRT APIs.
/// </summary>
public class BluetoothService : IDisposable
{
    // Nothing earbuds custom SPP UUID
    private static readonly Guid NothingSppUuid = Guid.Parse("AEAC4A03-DFF5-498F-843A-34487CF133EB");
    private static readonly Guid FallbackSppUuid = Guid.Parse("00001105-0000-1000-8000-00805F9B34FB");

    private StreamSocket? _socket;
    private DataWriter? _writer;
    private DataReader? _reader;
    private CancellationTokenSource? _readCts;
    private BluetoothDevice? _btDevice;

    public bool IsConnected => _socket != null;

    public event Action<byte[]>? DataReceived;
    public event Action? Disconnected;
    public event Action<string>? LogMessage;

    /// <summary>
    /// Discover paired Bluetooth devices that may be Nothing earbuds.
    /// </summary>
    public async Task<List<(string Name, string Address, ulong BluetoothAddress)>> DiscoverDevicesAsync()
    {
        var results = new List<(string Name, string Address, ulong BluetoothAddress)>();

        // Query for paired Bluetooth devices
        string selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        var devices = await DeviceInformation.FindAllAsync(selector);

        foreach (var deviceInfo in devices)
        {
            try
            {
                using var btDev = await BluetoothDevice.FromIdAsync(deviceInfo.Id);
                if (btDev == null) continue;

                string name = btDev.Name ?? "";
                // Filter for Nothing devices by name and ensure it's not overly broad
                if (name.StartsWith("Nothing", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("CMF", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("Ear", StringComparison.OrdinalIgnoreCase) ||
                    (name.StartsWith("Ear (") && name.EndsWith(")")))
                {
                    string address = btDev.BluetoothAddress.ToString("X12");
                    string formattedAddr = string.Join(":",
                        Enumerable.Range(0, 6).Select(i => address.Substring(i * 2, 2)));
                    results.Add((name, formattedAddr, btDev.BluetoothAddress));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error querying device {deviceInfo.Id}: {ex.Message}");
            }
        }

        return results;
    }

    /// <summary>
    /// Connect to a Nothing earbuds device via RFCOMM/SPP.
    /// </summary>
    public async Task ConnectAsync(ulong bluetoothAddress)
    {
        Disconnect();

        LogMessage?.Invoke($"Connecting to {bluetoothAddress:X12}...");

        _btDevice = await BluetoothDevice.FromBluetoothAddressAsync(bluetoothAddress);
        if (_btDevice == null)
            throw new InvalidOperationException("Could not find Bluetooth device.");

        // Get RFCOMM services
        var rfcommResult = await _btDevice.GetRfcommServicesForIdAsync(
            RfcommServiceId.FromUuid(NothingSppUuid),
            BluetoothCacheMode.Uncached);

        RfcommDeviceService? service = null;

        if (rfcommResult.Services.Count > 0)
        {
            service = rfcommResult.Services[0];
            LogMessage?.Invoke("Found Nothing SPP service.");
        }
        else
        {
            // Try fallback UUID
            rfcommResult = await _btDevice.GetRfcommServicesForIdAsync(
                RfcommServiceId.FromUuid(FallbackSppUuid),
                BluetoothCacheMode.Uncached);

            if (rfcommResult.Services.Count > 0)
            {
                service = rfcommResult.Services[0];
                LogMessage?.Invoke("Using fallback SPP service.");
            }
        }

        if (service == null)
        {
            // Try serial port profile
            rfcommResult = await _btDevice.GetRfcommServicesAsync(BluetoothCacheMode.Uncached);
            if (rfcommResult.Services.Count > 0)
            {
                service = rfcommResult.Services[0];
                LogMessage?.Invoke($"Using first available RFCOMM service: {service.ServiceId.Uuid}");
            }
        }

        if (service == null)
            throw new InvalidOperationException("No RFCOMM service found on device. Make sure earbuds are paired and nearby.");

        _socket = new StreamSocket();
        await _socket.ConnectAsync(
            service.ConnectionHostName,
            service.ConnectionServiceName,
            SocketProtectionLevel.BluetoothEncryptionWithAuthentication);

        _writer = new DataWriter(_socket.OutputStream);
        _reader = new DataReader(_socket.InputStream)
        {
            InputStreamOptions = InputStreamOptions.Partial
        };

        LogMessage?.Invoke("Connected!");

        // Start reading loop
        _readCts = new CancellationTokenSource();
        _ = Task.Run(() => ReadLoopAsync(_readCts.Token));
    }

    /// <summary>
    /// Send raw bytes to the earbuds.
    /// </summary>
    public async Task SendAsync(byte[] data)
    {
        DataWriter? writerLocal;
        lock (_disconnectLock)
        {
            writerLocal = _writer;
        }

        if (writerLocal == null)
            throw new InvalidOperationException("Not connected.");

        writerLocal.WriteBytes(data);
        await writerLocal.StoreAsync();
        await writerLocal.FlushAsync();
    }

    private readonly object _disconnectLock = new();

    /// <summary>
    /// Disconnect from the earbuds.
    /// </summary>
    public void Disconnect()
    {
        lock (_disconnectLock)
        {
            if (_readCts != null)
            {
                _readCts.Cancel();
                _readCts.Dispose();
                _readCts = null;
            }

            _writer?.Dispose();
            _writer = null;
            _reader?.Dispose();
            _reader = null;
            _socket?.Dispose();
            _socket = null;
            _btDevice?.Dispose();
            _btDevice = null;
        }

        Disconnected?.Invoke();
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _reader != null)
            {
                uint bytesRead = await _reader.LoadAsync(1024).AsTask(ct);
                if (bytesRead == 0) break;

                var buffer = new byte[bytesRead];
                _reader.ReadBytes(buffer);
                DataReceived?.Invoke(buffer);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Debug.WriteLine($"Read error: {ex.Message}");
            LogMessage?.Invoke($"Connection error: {ex.Message}");
        }
        finally
        {
            if (!ct.IsCancellationRequested)
            {
                LogMessage?.Invoke("Connection lost.");
                Disconnect();
            }
        }
    }

    public void Dispose()
    {
        Disconnect();
        GC.SuppressFinalize(this);
    }
}
