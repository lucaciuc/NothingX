using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using NothingX.Bluetooth;
using NothingX.Models;

namespace NothingX.Protocol;

/// <summary>
/// High-level protocol service for communicating with Nothing earbuds.
/// Provides async methods for all device operations.
/// </summary>
public class NothingProtocol : IDisposable
{
    private readonly BluetoothService _bluetooth;
    private readonly PacketBuilder _builder = new();
    private readonly PacketParser _parser = new();
    private readonly ConcurrentDictionary<int, TaskCompletionSource<NothingPacket>> _pendingRequests = new();
    private readonly TimeSpan _responseTimeout = TimeSpan.FromSeconds(5);

    public DeviceInfo Device { get; } = new();

    public event Action<NothingPacket>? NotificationReceived;
    public event Action? SettingsChanged;
    public event Action<string>? Log;

    private DateTime _lastBatteryQuery = DateTime.MinValue;
    private DateTime _lastSettingsChange = DateTime.MinValue;

    public NothingProtocol()
    {
#if DEBUG
        Log += msg => Console.WriteLine($"[Protocol] {msg}");
#endif
        _bluetooth = new BluetoothService();
        _bluetooth.DataReceived += OnDataReceived;
        _bluetooth.Disconnected += OnDisconnected;
        _bluetooth.LogMessage += msg => Log?.Invoke(msg);
    }

    /// <summary>Discover paired Nothing devices</summary>
    public Task<List<(string Name, string Address, ulong BluetoothAddress)>> DiscoverDevicesAsync()
        => _bluetooth.DiscoverDevicesAsync();

    /// <summary>
    /// Connect to the earbuds and perform the initialization handshake.
    /// </summary>
    public async Task ConnectAsync(ulong bluetoothAddress)
    {
        await _bluetooth.ConnectAsync(bluetoothAddress);
        _parser.Reset();
        _builder.ResetFsn();

        Device.IsConnected = true;
        Log?.Invoke("Activating protocol...");

        // Step 1: Activate protocol
        var activatePacket = _builder.BuildActivate();
        await SendPacketAsync(activatePacket);
        await Task.Delay(200);

        // Step 2: Get protocol version
        Log?.Invoke("Querying device...");
        var versionResponse = await SendAndWaitAsync(
            _builder.BuildQuery(Commands.Query.GET_PROTOCOL_VERSION));
        if (versionResponse != null)
        {
            Device.FirmwareVersion = versionResponse.PayloadAsString();
            Log?.Invoke($"Firmware: {Device.FirmwareVersion}");
        }

        // Step 3: Register for notifications
        await RegisterNotificationsAsync();

        // Step 4: Query initial state
        await RefreshStateAsync();

        Log?.Invoke("Ready!");
    }

    /// <summary>Disconnect from the earbuds</summary>
    public void Disconnect()
    {
        Device.IsConnected = false;
        _bluetooth.Disconnect();
    }

    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    /// <summary>Refresh all device state</summary>
    public async Task RefreshStateAsync()
    {
        if (!await _refreshLock.WaitAsync(0)) return;
        try
        {
            await GetBatteryAsync();
            await GetAncModeAsync();
            await GetEqModeAsync();
            await GetAdvancedEqModeAsync();
            await GetSimpleEqAsync();
            await GetCustomEqAsync();
            await GetLowLatencyAsync();
            await GetSpatialAudioAsync();
            await GetBassEnhancerAsync();
            await GetAncLevelAsync();
            await GetSystemAudioAsync();
            await GetAutoPowerOffAsync();
            await GetDualConnectionAsync();
            await GetDualDeviceListAsync();
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /// <summary>Get battery levels</summary>
    public async Task<BatteryInfo?> GetBatteryAsync()
    {
        var response = await SendAndWaitAsync(
            _builder.BuildQuery(Commands.Query.GET_REMOTE_BATTERY_LEVEL));
        if (response?.IsOk == true && response.Payload.Length > 0)
        {
            var battery = BatteryInfo.FromPayload(response.Payload);
            Device.Battery = battery;
            return battery;
        }
        return null;
    }

    /// <summary>Get current ANC mode</summary>
    public async Task<AncMode?> GetAncModeAsync()
    {
        var response = await SendAndWaitAsync(
            _builder.BuildQuery(Commands.Query.GET_CURRENT_NOISE_REDUCTION));
        if (response?.IsOk == true && response.Payload.Length >= 1)
        {
            var rawMode = response.Payload.Length >= 2 ? response.Payload[1] : response.Payload[0];
            Log?.Invoke($"Raw ANC Mode received: {rawMode}");
            var mode = (AncMode)rawMode;
            Device.AncMode = mode;
            return mode;
        }
        return null;
    }

    /// <summary>Set ANC mode</summary>
    public async Task<bool> SetAncModeAsync(AncMode mode)
    {
        var response = await SendAndWaitAsync(
            _builder.BuildSetAnc((byte)mode));
        if (response?.IsOk == true)
        {
            Device.AncMode = mode;
            return true;
        }
        return false;
    }

    /// <summary>Get current ANC level</summary>
    public async Task<int?> GetAncLevelAsync()
    {
        var response = await SendAndWaitAsync(
            _builder.BuildQuery(Commands.Query.GET_NOISE_REDUCTION_CONFIGURATION));
        if (response?.IsOk == true && response.Payload.Length >= 1)
        {
            var level = response.Payload[0];
            Device.AncLevel = level;
            return level;
        }
        return null;
    }

    /// <summary>Set ANC level</summary>
    public async Task<bool> SetAncLevelAsync(int level)
    {
        if (level < 0 || level > 255) throw new ArgumentOutOfRangeException(nameof(level));
        var response = await SendAndWaitAsync(
            _builder.BuildSetAncLevel((byte)level));
        if (response?.IsOk == true)
        {
            Device.AncLevel = level;
            return true;
        }
        return false;
    }

    /// <summary>Get current EQ mode</summary>
    public async Task<EqPreset?> GetEqModeAsync()
    {
        var response = await SendAndWaitAsync(
            _builder.BuildQuery(Commands.Query.GET_EQ_MODE));
        if (response?.IsOk == true && response.Payload.Length >= 1)
        {
            var preset = (EqPreset)response.Payload[0];
            Device.EqPreset = preset;
            return preset;
        }
        return null;
    }

    /// <summary>Set EQ mode</summary>
    public async Task<bool> SetEqModeAsync(EqPreset preset)
    {
        // Disable advanced mode before applying a preset
        await SetAdvancedEqModeAsync(false);
        var response = await SendAndWaitAsync(
            _builder.BuildSetEqMode((byte)preset));
        if (response?.IsOk == true)
        {
            Device.EqPreset = preset;
            return true;
        }
        return false;
    }

    /// <summary>Get low latency mode status</summary>
    public async Task<bool?> GetLowLatencyAsync()
    {
        var response = await SendAndWaitAsync(
            _builder.BuildQuery(Commands.Query.GET_HOST_LAG_MODE));
        if (response?.IsOk == true && response.Payload.Length >= 1)
        {
            bool enabled = response.Payload[0] == 1;
            Device.LowLatencyMode = enabled;
            return enabled;
        }
        return null;
    }

    /// <summary>Set low latency mode</summary>
    public async Task<bool> SetLowLatencyAsync(bool enabled)
    {
        var response = await SendAndWaitAsync(
            _builder.BuildSetLowLatency(enabled));
        if (response?.IsOk == true)
        {
            Device.LowLatencyMode = enabled;
            return true;
        }
        return false;
    }

    /// <summary>Get spatial audio status</summary>
    public async Task<SpatialAudioMode?> GetSpatialAudioAsync()
    {
        var response = await SendAndWaitAsync(
            _builder.BuildQuery(Commands.Query.GET_SPATIAL_AUDIO));
        if (response?.IsOk == true && response.Payload.Length >= 1)
        {
            var mode = (SpatialAudioMode)response.Payload[0];
            Device.SpatialAudioMode = mode;
            return mode;
        }
        return null;
    }

    /// <summary>Set spatial audio</summary>
    public async Task<bool> SetSpatialAudioAsync(SpatialAudioMode mode)
    {
        var response = await SendAndWaitAsync(
            _builder.BuildSetSpatialAudio(mode));
        if (response?.IsOk == true)
        {
            Device.SpatialAudioMode = mode;
            return true;
        }
        return false;
    }

    /// <summary>Get bass enhancer mode and level</summary>
    public async Task<bool?> GetBassEnhancerAsync()
    {
        var response = await SendAndWaitAsync(
            _builder.BuildQuery(Commands.Query.GET_BASS_ENHANCER_MODE));

        // Fallback for Headphones/CMF devices that might use BASS_BOOST command (0xC04E)
        if (response == null || !response.IsOk)
        {
            response = await SendAndWaitAsync(
                _builder.BuildQuery(Commands.Query.GET_BASS_BOOST)); // 0xC04E
            if (response?.IsOk == true) 
            {
                Device.HasLegacyBassCommand = true;
            }
        }

        if (response?.IsOk == true && response.Payload.Length >= 2)
        {
            bool enabled = response.Payload[0] == 1;
            int level = response.Payload[1];
            
            // Confirmed via btsnoop: Level 1 = 0x05 (5), Level 2 = 0x0A (10)
            if (Device.HasLegacyBassCommand)
            {
                level = level / 5;
            }
            
            Device.UltraBassEnabled = enabled;
            Device.UltraBassLevel = level;
            return enabled;
        }
        return null;
    }

    /// <summary>Set bass enhancer mode and level</summary>
    public async Task<bool> SetBassEnhancerAsync(bool enabled, int level)
    {
        if (level < 0 || level > 50) throw new ArgumentOutOfRangeException(nameof(level));
        var command = Device.HasLegacyBassCommand 
            ? Commands.Set.SET_BASS_ENHANCER  // 0xF051 confirmed via btsnoop
            : Commands.Set.SET_BASS_ENHANCER_MODE; // 0xF057 for other models
            
        byte[] payload;
        if (Device.HasLegacyBassCommand)
        {
            // Confirmed via btsnoop: Level 1 = 0x05, Level 2 = 0x0A (level * 5)
            byte scaledLevel = (byte)(level * 5);
            payload = [enabled ? (byte)1 : (byte)0, scaledLevel];
        }
        else
        {
            payload = [enabled ? (byte)1 : (byte)0, (byte)level];
        }
            
        var response = await SendAndWaitAsync(_builder.Build(command, payload));
            
        if (response?.IsOk == true)
        {
            Device.UltraBassEnabled = enabled;
            Device.UltraBassLevel = level;
            return true;
        }
        return false;
    }

    /// <summary>Find my earbuds (play sound)</summary>
    public async Task<bool> FindEarbudsAsync(bool start = true)
    {
        var response = await SendAndWaitAsync(
            _builder.BuildFindEarbuds(start));
        return response?.IsOk == true;
    }

    /// <summary>Get High-Quality Audio (LDAC) status</summary>
    public async Task<bool?> GetSystemAudioAsync()
    {
        // We found out through reverse engineering that while the SET command is SET_SYSTEM_AUDIO (0xF01C), 
        // the official app uses GET_LHDC_COMMANDS (0xC029) to query the LDAC status!
        var response = await SendAndWaitAsync(
            _builder.BuildQuery(Commands.Query.GET_LHDC_COMMANDS));
            
        if (response?.IsOk == true && response.Payload.Length >= 1)
        {
            bool enabled = response.Payload[0] != 0;
            Device.HighQualityAudioEnabled = enabled;
            return enabled;
        }
        return null;
    }

    /// <summary>Set High-Quality Audio (LDAC)</summary>
    public async Task<bool> SetSystemAudioAsync(bool enabled)
    {
        // Confirmed via btsnoop: 0xF01C with payload 0x02 for LDAC, 0x00 for AAC
        var response = await SendAndWaitAsync(
            _builder.Build(Commands.Set.SET_HIGH_QUALITY_AUDIO, 
                [enabled ? (byte)0x02 : (byte)0x00]));
        if (response?.IsOk == true)
        {
            Device.HighQualityAudioEnabled = enabled;
            return true;
        }
        return false;
    }

    /// <summary>Get Auto Power-Off Time</summary>
    public async Task<int?> GetAutoPowerOffAsync()
    {
        var response = await SendAndWaitAsync(
            _builder.BuildQuery(Commands.Query.GET_AUTO_POWER_OFF_TIME));
        if (response?.IsOk == true && response.Payload.Length >= 2)
        {
            // Payload format is usually [0x00, minutes, 0x00]
            int time = response.Payload[1];
            Device.AutoPowerOffTime = time;
            return time;
        }
        return null;
    }

    /// <summary>Set Auto Power-Off Time</summary>
    public async Task<bool> SetAutoPowerOffAsync(int minutes)
    {
        if (minutes < 0 || minutes > 255) throw new ArgumentOutOfRangeException(nameof(minutes));
        var response = await SendAndWaitAsync(
            _builder.BuildSetAutoPowerOff((byte)minutes));
        if (response?.IsOk == true)
        {
            Device.AutoPowerOffTime = minutes;
            return true;
        }
        return false;
    }

    /// <summary>Get Dual Connection Status</summary>
    public async Task<bool?> GetDualConnectionAsync()
    {
        var response = await SendAndWaitAsync(
            _builder.BuildQuery(Commands.Query.GET_DUAL_ENABLE));
        if (response?.IsOk == true && response.Payload.Length >= 1)
        {
            bool enabled = response.Payload[0] == 1;
            Device.DualConnectionEnabled = enabled;
            return enabled;
        }
        return null;
    }

    /// <summary>Set Dual Connection Status</summary>
    public async Task<bool> SetDualConnectionAsync(bool enabled)
    {
        var response = await SendAndWaitAsync(
            _builder.BuildSetDualConnection(enabled));
        if (response?.IsOk == true)
        {
            Device.DualConnectionEnabled = enabled;
            if (enabled)
            {
                // Fetch the device list immediately after enabling
                await GetDualDeviceListAsync();
            }
            return true;
        }
        return false;
    }

    /// <summary>Get list of paired dual connection devices</summary>
    public async Task<List<DualDevice>?> GetDualDeviceListAsync()
    {
        var response = await SendAndWaitAsync(
            _builder.BuildQuery(Commands.Query.GET_DUAL_DEVICE_LIST));
            
        if (response?.IsOk == true && response.Payload.Length >= 3)
        {
            var list = new List<DualDevice>();
            int count = response.Payload[2];
            int offset = 3;
            
            for (int i = 0; i < count; i++)
            {
                if (offset + 8 > response.Payload.Length) break;
                
                byte status = response.Payload[offset];
                byte[] macBytes = new byte[6];
                System.Array.Copy(response.Payload, offset + 1, macBytes, 0, 6);
                string macAddress = BitConverter.ToString(macBytes).Replace("-", ":");
                
                int nameLen = response.Payload[offset + 7];
                offset += 8;
                
                if (offset + nameLen > response.Payload.Length) break;
                
                string name = Encoding.UTF8.GetString(response.Payload, offset, nameLen);
                offset += nameLen;
                
                list.Add(new DualDevice
                {
                    Name = name,
                    MacAddress = macAddress,
                    MacBytes = macBytes,
                    IsConnected = (status & 0x01) != 0,
                    IsCurrentDevice = (status & 0x10) != 0
                });
            }
            
            // Update device info
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                Device.DualDevices.Clear();
                foreach (var d in list) Device.DualDevices.Add(d);
            });
            return list;
        }
        return null;
    }
    
    /// <summary>Set dual connection device state</summary>
    public async Task<bool> SetDualDeviceAsync(byte[] macAddress, bool connect)
    {
        var response = await SendAndWaitAsync(
            _builder.BuildSetDualDevice(connect, macAddress));
        return response?.IsOk == true;
    }

    /// <summary>Get custom EQ values</summary>
    public async Task<CustomEq?> GetCustomEqAsync()
    {
        var response = await SendAndWaitAsync(
            _builder.BuildQuery(Commands.Query.GET_ADVANCE_CUSTOM_EQ_VALUE));
        if (response?.IsOk == true && response.Payload.Length > 0)
        {
            var eq = CustomEq.FromPayload(response.Payload);
            if (eq != null)
            {
                Device.AdvancedEq = eq;
                return eq;
            }
        }
        return null;
    }

    /// <summary>Get simple custom EQ values</summary>
    public async Task<SimpleEq?> GetSimpleEqAsync()
    {
        var response = await SendAndWaitAsync(
            _builder.BuildQuery(Commands.Query.GET_SIMPLE_CUSTOM_EQ));
        if (response?.IsOk == true && response.Payload.Length > 0)
        {
            var eq = SimpleEq.FromPayload(response.Payload);
            Device.SimpleEq = eq;
            return eq;
        }
        return null;
    }

    /// <summary>Get whether advanced custom EQ mode is enabled</summary>
    public async Task<bool?> GetAdvancedEqModeAsync()
    {
        var response = await SendAndWaitAsync(
            _builder.BuildQuery(Commands.Query.GET_ADVANCE_CUSTOM_EQ_MODE));
        if (response?.IsOk == true && response.Payload.Length >= 1)
        {
            bool enabled = response.Payload[0] == 1;
            Device.IsAdvancedEqMode = enabled;
            return enabled;
        }
        return null;
    }

    /// <summary>Set custom EQ values (Advanced 8-band)</summary>
    public async Task<bool> SetCustomEqAsync(CustomEq eq)
    {
        // Must enable advanced mode and switch to Custom EQ preset first
        await SetAdvancedEqModeAsync(true);
        await SendAndWaitAsync(_builder.BuildSetEqMode((byte)EqPreset.Custom));
        Device.EqPreset = EqPreset.Custom;
        
        var response = await SendAndWaitAsync(
            _builder.BuildSetCustomEq(eq.ToPayload()));
        return response?.IsOk == true;
    }

    /// <summary>Set simple EQ values (3-band radar)</summary>
    public async Task<bool> SetSimpleEqAsync(SimpleEq eq)
    {
        // Must disable advanced mode and switch to Custom EQ preset first,
        // otherwise the earbuds stay in the built-in preset (Balanced, etc.)
        // and ignore the custom band data.
        await SetAdvancedEqModeAsync(false);
        await SendAndWaitAsync(_builder.BuildSetEqMode((byte)EqPreset.Custom));
        Device.EqPreset = EqPreset.Custom;
        
        var response = await SendAndWaitAsync(
            _builder.BuildSetSimpleCustomEq(eq.ToPayload()));
        return response?.IsOk == true;
    }

    /// <summary>Enable or disable advanced EQ mode</summary>
    public async Task SetAdvancedEqModeAsync(bool enabled)
    {
        // Device doesn't respond to 0xF042 — fire and forget with a small delay
        await SendPacketAsync(_builder.BuildSetAdvancedEqMode(enabled));
        await Task.Delay(100);
    }

    /// <summary>Get gesture configurations</summary>
    public async Task<bool> GetGesturesAsync()
    {
        var response = await SendAndWaitAsync(
            _builder.BuildQuery(Commands.Query.GET_KEY_CONFIGURATION));
        return response?.IsOk == true;
    }

    /// <summary>Set gesture configuration</summary>
    public async Task<bool> SetGestureAsync(GestureConfig config)
    {
        var response = await SendAndWaitAsync(
            _builder.BuildSetGesture(config.ToPayload()));
        return response?.IsOk == true;
    }

    /// <summary>Register for push notifications from the earbuds</summary>
    private async Task RegisterNotificationsAsync()
    {
        int[] notifications = [
            Commands.Notification.EVENT_BATTERY_CHANGED,
            Commands.Notification.EVENT_DEVICE_STATUS_CHANGED,
            Commands.Notification.EVENT_NOISE_REDUCTION_LEVEL_CHANGED,
            Commands.Notification.EVENT_GAME_MODE_CHANGED,
            Commands.Notification.EVENT_WORKING_STATUS_CHANGE,
        ];

        foreach (var notif in notifications)
        {
            var packet = _builder.BuildRegisterNotification(notif);
            await SendPacketAsync(packet);
            await Task.Delay(50);
        }
    }

    /// <summary>Send a packet and wait for the response</summary>
    private async Task<NothingPacket?> SendAndWaitAsync(NothingPacket packet)
    {
        int responseCmd = packet.ResponseCommand;
        var tcs = new TaskCompletionSource<NothingPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[responseCmd] = tcs;

        try
        {
            await SendPacketAsync(packet);

            using var cts = new CancellationTokenSource(_responseTimeout);
            cts.Token.Register(() => tcs.TrySetCanceled());

            var response = await tcs.Task;
            return response;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SendAndWait error: {ex.Message}");
            return null;
        }
        finally
        {
            _pendingRequests.TryRemove(responseCmd, out _);
        }
    }

    /// <summary>Send a raw packet</summary>
    private async Task SendPacketAsync(NothingPacket packet)
    {
        byte[] data = packet.ToBytes();
        Log?.Invoke($"→ {packet}");
        await _bluetooth.SendAsync(data);
    }

    private void OnDataReceived(byte[] data)
    {
        var packets = _parser.Feed(data);
        foreach (var packet in packets)
        {
            Log?.Invoke($"← {packet}");
            try { System.IO.File.AppendAllText(@"C:\Users\Andrew\Desktop\packet_log.txt", $"{DateTime.Now:HH:mm:ss.fff} | CMD: {packet.Command:X4} | {packet}\n"); } catch {}
            HandleReceivedPacket(packet);
        }
    }

    private void HandleReceivedPacket(NothingPacket packet)
    {
        int responseCmd = packet.ResponseCommand;

        // Check if this is a response to a pending request
        if (_pendingRequests.TryRemove(responseCmd, out var tcs))
        {
            tcs.TrySetResult(packet);
            return;
        }

        // Handle push notifications. Notifications are sent by the earbuds with the request bit set.
        switch (packet.RequestCommand)
        {
            case Commands.Notification.EVENT_BATTERY_CHANGED:
                // Debounce battery queries (max 1 per second)
                if ((DateTime.UtcNow - _lastBatteryQuery).TotalSeconds > 1)
                {
                    _lastBatteryQuery = DateTime.UtcNow;
                    _ = GetBatteryAsync();
                }
                break;

            case Commands.Notification.EVENT_NOISE_REDUCTION_LEVEL_CHANGED:
                if (packet.Payload.Length >= 1)
                {
                    var rawMode = packet.Payload.Length >= 2 ? packet.Payload[1] : packet.Payload[0];
                    Log?.Invoke($"Raw ANC Mode received (Notification): {rawMode}");
                    Device.AncMode = (AncMode)rawMode;
                }
                break;

            case Commands.Notification.EVENT_GAME_MODE_CHANGED:
            case Commands.Query.GET_HOST_LAG_MODE:
                if (packet.Payload.Length >= 1)
                    Device.LowLatencyMode = packet.Payload[0] == 1;
                break;

            case Commands.Query.GET_SPATIAL_AUDIO:
                if (packet.Payload.Length >= 1)
                    Device.SpatialAudioMode = (SpatialAudioMode)packet.Payload[0];
                break;
                
            case Commands.Notification.EVENT_DUAL_DEVICE_CONNECT_STATE:
            case Commands.Notification.EVENT_DUAL_DEVICE_SWITCH_STATE:
                _ = GetDualConnectionAsync();
                _ = GetDualDeviceListAsync();
                break;

            case Commands.Notification.EVENT_DEVICE_STATUS_CHANGED:
            case Commands.Notification.EVENT_WORKING_STATUS_CHANGE:
                // Debounce UI refreshes (max 1 per second)
                if ((DateTime.UtcNow - _lastSettingsChange).TotalSeconds > 1)
                {
                    _lastSettingsChange = DateTime.UtcNow;
                    SettingsChanged?.Invoke();
                }
                break;
        }

        NotificationReceived?.Invoke(packet);
    }

    private void OnDisconnected()
    {
        Device.IsConnected = false;
        // Cancel all pending requests
        foreach (var kvp in _pendingRequests)
        {
            kvp.Value.TrySetCanceled();
        }
        _pendingRequests.Clear();
    }

    public void Dispose()
    {
        Disconnect();
        _bluetooth.Dispose();
        GC.SuppressFinalize(this);
    }
}
