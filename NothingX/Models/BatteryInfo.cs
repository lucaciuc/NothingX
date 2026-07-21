using CommunityToolkit.Mvvm.ComponentModel;

namespace NothingX.Models;

/// <summary>
/// Battery levels for earbuds and charging case.
/// </summary>
public partial class BatteryInfo : ObservableObject
{
    [ObservableProperty] private int _left;
    [ObservableProperty] private int _right;
    [ObservableProperty] private int _case;
    [ObservableProperty] private bool _leftCharging;
    [ObservableProperty] private bool _rightCharging;
    [ObservableProperty] private bool _caseCharging;

    public static BatteryInfo FromPayload(byte[] payload)
    {
#if DEBUG
        Console.WriteLine($"[DEBUG] Battery Payload: {BitConverter.ToString(payload)}");
#endif
        var info = new BatteryInfo();
        if (payload.Length >= 1) info.Left = payload[0] & 0x7F;
        if (payload.Length >= 2) info.Right = payload[1] & 0x7F;
        if (payload.Length >= 3) info.Case = payload[2] & 0x7F;
        if (payload.Length >= 1) info.LeftCharging = (payload[0] & 0x80) != 0;
        if (payload.Length >= 2) info.RightCharging = (payload[1] & 0x80) != 0;
        if (payload.Length >= 3) info.CaseCharging = (payload[2] & 0x80) != 0;
        return info;
    }
}
