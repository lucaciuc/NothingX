using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NothingX.Models;

/// <summary>
/// Comprehensive device info for a connected Nothing earbuds device.
/// </summary>
public partial class DeviceInfo : ObservableObject
{
    [ObservableProperty] private string _name = "Nothing Ear";
    [ObservableProperty] private string _address = "";
    [ObservableProperty] private string _firmwareVersion = "";
    [ObservableProperty] private string _serialNumber = "";
    [ObservableProperty] private string _modelId = "";
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private AncMode _ancMode = AncMode.Off;
    [ObservableProperty] private EqPreset _eqPreset = EqPreset.Balanced;
    [ObservableProperty] private bool _lowLatencyMode;
    [ObservableProperty] private bool _inEarLeft;
    [ObservableProperty] private bool _inEarRight;
    [ObservableProperty] private BatteryInfo _battery = new();
    
    // Custom EQ states
    [ObservableProperty] private bool _isAdvancedEqMode;
    
    // New Audio Features
    [ObservableProperty] private SpatialAudioMode _spatialAudioMode;
    [ObservableProperty] private bool _ultraBassEnabled;
    [ObservableProperty] private int _ultraBassLevel;
    [ObservableProperty] private int _ancLevel;
    [ObservableProperty] private bool _highQualityAudioEnabled;
    [ObservableProperty] private int _autoPowerOffTime;
    [ObservableProperty] private bool _dualConnectionEnabled;
    [ObservableProperty] private ObservableCollection<DualDevice> _dualDevices = new();
    
    [ObservableProperty] private SimpleEq? _simpleEq;
    [ObservableProperty] private CustomEq? _advancedEq;
    
    public bool HasLegacyBassCommand { get; set; }
}
