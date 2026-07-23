using CommunityToolkit.Mvvm.ComponentModel;

namespace NothingX.Models;

public partial class DualDevice : ObservableObject
{
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _macAddress = string.Empty;
    [ObservableProperty] private byte[] _macBytes = [];
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private bool _isCurrentDevice;
}
