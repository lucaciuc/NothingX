using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NothingX.Models;
using NothingX.Protocol;

using NothingX.ViewModels;

namespace NothingX.ViewModels;

public enum DeviceView { Connect, Device, Controls, Settings }

public partial class EqBandViewModel : ObservableObject
{
    private readonly Action? _onChanged;

    [ObservableProperty] private string _label = "";
    
    private float _gain;
    public float Gain
    {
        get => _gain;
        set
        {
            if (SetProperty(ref _gain, value))
                _onChanged?.Invoke();
        }
    }

    public EqBandViewModel(string label, Action? onChanged = null)
    {
        Label = label;
        _onChanged = onChanged;
    }
}

public class AutoPowerOffOption
{
    public string Display { get; set; } = "";
    public int Value { get; set; }
    
    public override string ToString() => Display;
}

public partial class MainViewModel : ObservableObject, IDisposable
{
    public List<AutoPowerOffOption> AutoPowerOffOptions { get; } = [
        new AutoPowerOffOption { Display = "30 mins", Value = 30 },
        new AutoPowerOffOption { Display = "1 hour", Value = 60 },
        new AutoPowerOffOption { Display = "2 hours", Value = 120 },
        new AutoPowerOffOption { Display = "3 hours", Value = 180 },
        new AutoPowerOffOption { Display = "4 hours", Value = 240 }
    ];

    private readonly NothingProtocol _protocol = new();

    public DeviceInfo Device => _protocol.Device;

    [ObservableProperty] private bool _isConnecting;
    [ObservableProperty] private string _statusText = "Disconnected";
    [ObservableProperty] private bool _isFinding;
    [ObservableProperty] private bool _isHeadphone;
    [ObservableProperty] private DeviceView _selectedDeviceView = DeviceView.Connect;
    [ObservableProperty] private DeviceListItem? _selectedDevice;

    public ObservableCollection<DeviceListItem> DiscoveredDevices { get; } = [];
    public ObservableCollection<string> LogEntries { get; } = [];

    // Phase 2: Advanced Features
    public ObservableCollection<EqBandViewModel> CustomEqBands { get; } = [];
    public SimpleEq _simpleEqModel = new();
    public CustomEq _customEqModel = new();
    
    [ObservableProperty] private float _simpleEqBass;
    [ObservableProperty] private float _simpleEqMid;
    [ObservableProperty] private float _simpleEqTreble;
    
    // Audio Enhancements
    public Array AvailableSpatialAudioModes => Enum.GetValues(typeof(SpatialAudioMode));

    public int BassEnhancerSliderValue
    {
        get => Device.UltraBassEnabled ? (Device.UltraBassLevel == 1 ? 5 : 10) : 0;
        set
        {
            if (value == 0)
            {
                Device.UltraBassEnabled = false;
            }
            else
            {
                Device.UltraBassEnabled = true;
                Device.UltraBassLevel = (value <= 5) ? 1 : 2;
            }
            _ = ApplyUltraBassAsync();
            OnPropertyChanged(nameof(BassEnhancerSliderValue));
            OnPropertyChanged(nameof(BassEnhancerStateText));
        }
    }

    public string BassEnhancerStateText
    {
        get => Device.UltraBassEnabled ? $"On • Level {Device.UltraBassLevel}" : "Off";
    }

    public System.Collections.ObjectModel.ObservableCollection<DualDevice> DualDevices => _protocol.Device.DualDevices;
    
    [ObservableProperty] private GestureAction _leftDoubleTap;
    [ObservableProperty] private GestureAction _leftTripleTap;
    [ObservableProperty] private GestureAction _leftLongPress;
    [ObservableProperty] private GestureAction _rightDoubleTap;
    [ObservableProperty] private GestureAction _rightTripleTap;
    [ObservableProperty] private GestureAction _rightLongPress;

    public Array AvailableGestureActions => Enum.GetValues(typeof(GestureAction));

    public MainViewModel()
    {
        _protocol.Log += msg =>
        {
            Application.Current?.Dispatcher?.Invoke(() =>
            {
                LogEntries.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {msg}");
                if (LogEntries.Count > 200) LogEntries.RemoveAt(LogEntries.Count - 1);
            });
        };

        // Initialize EQ Bands
        string[] freqs = ["55", "110", "220", "440", "1.3k", "3.3k", "6.6k", "13.2k"];
        for (int i = 0; i < 8; i++)
        {
            CustomEqBands.Add(new EqBandViewModel(freqs[i], null));
        }

        _protocol.SettingsChanged += OnSettingsChanged;
        _protocol.Device.PropertyChanged += OnDevicePropertyChanged;
        _protocol.Device.Battery.PropertyChanged += OnBatteryPropertyChanged;
    }

    private void OnDevicePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        Application.Current?.Dispatcher?.Invoke(() =>
        {
            SyncFromDevice();
        });
    }

    private void OnBatteryPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        Application.Current?.Dispatcher?.Invoke(SyncFromDevice);
    }

    private void SyncFromDevice()
    {
        var dev = _protocol.Device;
        IsHeadphone = dev.Name.Contains("Headphone", StringComparison.OrdinalIgnoreCase) || 
                      dev.Name.Contains("Neckband", StringComparison.OrdinalIgnoreCase);
        
        OnPropertyChanged(nameof(BassEnhancerSliderValue));
        OnPropertyChanged(nameof(BassEnhancerStateText));

        if (dev.SimpleEq != null)
        {
            // Band mapping: Band 0 = Mid, Band 1 = Treble, Band 2 = Bass
            // (verified against Nothing X Android app)
            SimpleEqBass = (int)Math.Round(dev.SimpleEq.Bands[2].Gain);
            SimpleEqMid = (int)Math.Round(dev.SimpleEq.Bands[0].Gain);
            SimpleEqTreble = (int)Math.Round(dev.SimpleEq.Bands[1].Gain);
        }

        if (dev.AdvancedEq != null)
        {
            for (int i = 0; i < 8; i++)
            {
                if (i < CustomEqBands.Count)
                {
                    CustomEqBands[i].Gain = dev.AdvancedEq.Bands[i].Gain;
                }
            }
        }

        if (!dev.IsConnected)
        {
            StatusText = "Disconnected";
            SelectedDeviceView = DeviceView.Connect;
        }
    }

    [RelayCommand]
    private async Task ScanDevicesAsync()
    {
        StatusText = "Scanning...";
        DiscoveredDevices.Clear();

        try
        {
            var devices = await _protocol.DiscoverDevicesAsync();
            foreach (var (name, address, btAddr) in devices)
            {
                DiscoveredDevices.Add(new DeviceListItem(name, address, btAddr));
            }
            StatusText = devices.Count > 0
                ? $"Found {devices.Count} device(s)"
                : "No Nothing devices found. Make sure earbuds are paired.";
        }
        catch (Exception ex)
        {
            StatusText = $"Scan error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ConnectToDeviceAsync(DeviceListItem? device)
    {
        if (device == null) return;

        IsConnecting = true;
        StatusText = $"Connecting to {device.Name}...";

        try
        {
            _protocol.Device.Name = device.Name;
            await _protocol.ConnectAsync(device.BluetoothAddress);
            StatusText = "Connected";
            SelectedDeviceView = DeviceView.Device;
        }
        catch (Exception ex)
        {
            StatusText = $"Connection failed: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private void DisconnectDevice()
    {
        _protocol.Disconnect();
        StatusText = "Disconnected";
        SelectedDeviceView = DeviceView.Connect;
    }

    [RelayCommand]
    private async Task SetAncModeAsync(string modeStr)
    {
        if (Enum.TryParse<AncMode>(modeStr, out var mode))
        {
            await _protocol.SetAncModeAsync(mode);
        }
    }

    [RelayCommand]
    private async Task SetSpatialAudioModeAsync(string modeStr)
    {
        if (Enum.TryParse<SpatialAudioMode>(modeStr, out var mode))
        {
            await _protocol.SetSpatialAudioAsync(mode);
            Device.SpatialAudioMode = mode;
        }
    }

    [RelayCommand]
    private async Task SetEqPresetAsync(string presetStr)
    {
        if (Enum.TryParse<EqPreset>(presetStr, out var preset))
        {
            await _protocol.SetEqModeAsync(preset);
        }
    }

    [RelayCommand]
    private async Task ToggleLowLatencyAsync()
    {
        await _protocol.SetLowLatencyAsync(Device.LowLatencyMode);
    }

    [RelayCommand]
    private void ToggleAdvancedEq(string isAdvancedStr)
    {
        if (bool.TryParse(isAdvancedStr, out bool isAdvanced))
        {
            Device.IsAdvancedEqMode = isAdvanced;
        }
    }

    [RelayCommand]
    private async Task ToggleHighQualityAudioAsync()
    {
        await _protocol.SetSystemAudioAsync(Device.HighQualityAudioEnabled);
    }

    [RelayCommand]
    private async Task ApplyAutoPowerOffAsync()
    {
        await _protocol.SetAutoPowerOffAsync(Device.AutoPowerOffTime);
    }

    [RelayCommand]
    private async Task ToggleDualConnectionAsync()
    {
        await _protocol.SetDualConnectionAsync(Device.DualConnectionEnabled);
    }

    [RelayCommand]
    private async Task ToggleDualDeviceAsync(DualDevice device)
    {
        if (device == null || device.IsCurrentDevice) return;
        
        bool success = await _protocol.SetDualDeviceAsync(device.MacBytes, device.IsConnected);
        if (success)
        {
            // Re-fetch the list to ensure accurate state
            await _protocol.GetDualDeviceListAsync();
        }
    }

    [RelayCommand]
    private async Task ApplyUltraBassAsync()
    {
        if (Device.IsConnected)
        {
            await _protocol.SetBassEnhancerAsync(Device.UltraBassEnabled, Device.UltraBassLevel);
        }
    }

    [RelayCommand]
    private async Task ApplyAncLevelAsync(string levelStr)
    {
        if (int.TryParse(levelStr, out int level))
        {
            await _protocol.SetAncLevelAsync(level);
        }
    }

    [RelayCommand]
    private async Task FindEarbudsAsync()
    {
        IsFinding = !IsFinding;
        await _protocol.FindEarbudsAsync(IsFinding);
    }

    [RelayCommand]
    private async Task ApplyCustomEqAsync()
    {
        if (!Device.IsConnected) return;
        
        for (int i = 0; i < 8; i++)
        {
            _customEqModel.Bands[i].Gain = CustomEqBands[i].Gain;
        }
        await _protocol.SetCustomEqAsync(_customEqModel);
    }

    [RelayCommand]
    private async Task ApplySimpleEqAsync()
    {
        if (!Device.IsConnected) return;
        
        // Band mapping: Band 0 = Mid, Band 1 = Treble, Band 2 = Bass
        _simpleEqModel.Bands[2].Gain = SimpleEqBass;
        _simpleEqModel.Bands[0].Gain = SimpleEqMid;
        _simpleEqModel.Bands[1].Gain = SimpleEqTreble;
        
        await _protocol.SetSimpleEqAsync(_simpleEqModel);
    }

    [RelayCommand]
    private async Task SyncGesturesAsync()
    {
        if (!Device.IsConnected) return;
        
        // Sync Left Earbud (Device = 0)
        await _protocol.SetGestureAsync(new GestureConfig { Device = 0, Gesture = GestureType.DoubleTap, Action = LeftDoubleTap });
        await Task.Delay(50);
        await _protocol.SetGestureAsync(new GestureConfig { Device = 0, Gesture = GestureType.TripleTap, Action = LeftTripleTap });
        await Task.Delay(50);
        await _protocol.SetGestureAsync(new GestureConfig { Device = 0, Gesture = GestureType.LongPress, Action = LeftLongPress });
        await Task.Delay(50);

        // Sync Right Earbud (Device = 1)
        await _protocol.SetGestureAsync(new GestureConfig { Device = 1, Gesture = GestureType.DoubleTap, Action = RightDoubleTap });
        await Task.Delay(50);
        await _protocol.SetGestureAsync(new GestureConfig { Device = 1, Gesture = GestureType.TripleTap, Action = RightTripleTap });
        await Task.Delay(50);
        await _protocol.SetGestureAsync(new GestureConfig { Device = 1, Gesture = GestureType.LongPress, Action = RightLongPress });
    }

    private void OnSettingsChanged()
    {
        Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await RefreshAsync();
        });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (Device.IsConnected)
        {
            await _protocol.RefreshStateAsync();
        }
    }

    [RelayCommand]
    private void ShowControls()
    {
        SelectedDeviceView = DeviceView.Controls;
    }

    [RelayCommand]
    private void ShowSettings()
    {
        SelectedDeviceView = DeviceView.Settings;
    }

    [RelayCommand]
    private void GoBackToDevice()
    {
        SelectedDeviceView = DeviceView.Device;
    }

    public void Dispose()
    {
        _protocol.SettingsChanged -= OnSettingsChanged;
        _protocol.Device.PropertyChanged -= OnDevicePropertyChanged;
        _protocol.Device.Battery.PropertyChanged -= OnBatteryPropertyChanged;
        
        _protocol.Dispose();
        GC.SuppressFinalize(this);
    }
}

public record DeviceListItem(string Name, string Address, ulong BluetoothAddress);
