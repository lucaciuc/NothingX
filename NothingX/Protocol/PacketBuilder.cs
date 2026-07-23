using NothingX.Models;

namespace NothingX.Protocol;

/// <summary>
/// Builds outgoing protocol packets for the Nothing earbuds.
/// Handles FSN auto-increment and packet construction.
/// </summary>
public class PacketBuilder
{
    private int _fsn;
    private readonly int _deviceType;

    public PacketBuilder(int deviceType = NothingPacket.DEVICE_TYPE_TWS)
    {
        _deviceType = deviceType;
        _fsn = 0;
    }

    /// <summary>Create the next FSN (0-254, wrapping)</summary>
    private byte NextFsn()
    {
        _fsn++;
        if (_fsn >= 254) _fsn = 0;
        return (byte)_fsn;
    }

    /// <summary>Reset the frame sequence counter</summary>
    public void ResetFsn() => _fsn = 0;

    /// <summary>
    /// Build a command packet with optional payload.
    /// </summary>
    public NothingPacket Build(int command, byte[]? payload = null, bool needCrc = true)
    {
        var packet = new NothingPacket
        {
            Command = (ushort)command,
            Fsn = NextFsn(),
            Payload = payload ?? []
        };
        packet.SetControl(deviceType: _deviceType, crc: needCrc);
        packet.Length = (ushort)packet.Payload.Length;
        return packet;
    }

    /// <summary>Build a query packet (no payload needed)</summary>
    public NothingPacket BuildQuery(int queryCommand)
        => Build(queryCommand);


    /// <summary>Build the protocol activation packet (required after connect)</summary>
    public NothingPacket BuildActivate()
        => Build(Commands.Set.SET_PROTOCOL_ACTIVATED, [0x01]);

    /// <summary>Build a "find my earbuds" packet</summary>
    public NothingPacket BuildFindEarbuds(bool start)
        => Build(Commands.Set.SET_WHERE_AM_I, [0x06, start ? (byte)0x01 : (byte)0x00]);

    /// <summary>Build a set ANC mode packet</summary>
    public NothingPacket BuildSetAnc(byte mode)
        => Build(Commands.Set.SET_CURRENT_NOISE_REDUCTION, [1, mode, 0]);

    /// <summary>Build a set EQ mode packet</summary>
    public NothingPacket BuildSetEqMode(byte mode)
        => Build(Commands.Set.SET_EQ_MODE, [mode]);

    /// <summary>Build a low latency / game mode packet</summary>
    public NothingPacket BuildSetLowLatency(bool enabled)
        => Build(Commands.Set.SET_LAG_MODE, [enabled ? (byte)0x01 : (byte)0x02]);

    /// <summary>Build a register notification packet</summary>
    public NothingPacket BuildRegisterNotification(int notificationCommand)
        => Build(Commands.Set.REGISTER_NOTIFICATION, [
            (byte)(notificationCommand & 0xFF),
            (byte)((notificationCommand >> 8) & 0xFF)
        ]);

    /// <summary>Build an advanced custom EQ packet</summary>
    public NothingPacket BuildSetCustomEq(byte[] payload)
        => Build(Commands.Set.SET_ADVANCE_CUSTOM_EQ_VALUE, payload);

    /// <summary>Build a simple custom EQ packet</summary>
    public NothingPacket BuildSetSimpleCustomEq(byte[] payload)
        => Build(Commands.Set.SET_SIMPLE_CUSTOM_EQ, payload);

    /// <summary>Build a set advanced EQ mode packet</summary>
    public NothingPacket BuildSetAdvancedEqMode(bool enabled)
        => Build(Commands.Set.SET_ADVANCE_CUSTOM_EQ_MODE, [enabled ? (byte)1 : (byte)0]);

    /// <summary>Build a set gesture packet</summary>
    public NothingPacket BuildSetGesture(byte[] payload)
        => Build(Commands.Set.SET_KEY_CONFIGURATION, payload);

    /// <summary>Build a set spatial audio packet</summary>
    public NothingPacket BuildSetSpatialAudio(SpatialAudioMode mode)
        => Build(Commands.Set.SET_SPATIAL_AUDIO, [(byte)mode, 0x00]);


    /// <summary>Build a set ANC level packet</summary>
    public NothingPacket BuildSetAncLevel(byte level)
        => Build(Commands.Set.SET_NOISE_REDUCTION_CONFIGURATION, [level]);


    /// <summary>Build a set auto power off time packet</summary>
    public NothingPacket BuildSetAutoPowerOff(byte minutes)
        => Build(Commands.Set.SET_AUTO_POWER_OFF_TIME, [0x00, minutes, 0x00]);

    /// <summary>Build a set dual connection enable packet</summary>
    public NothingPacket BuildSetDualConnection(bool enabled)
        => Build(Commands.Set.SET_DUAL_ENABLE, [enabled ? (byte)0x01 : (byte)0x00]);

    /// <summary>Build a set dual connection device connect/disconnect packet</summary>
    public NothingPacket BuildSetDualDevice(bool connect, byte[] macAddress)
    {
        var payload = new byte[7];
        payload[0] = connect ? (byte)0x01 : (byte)0x00;
        if (macAddress != null && macAddress.Length >= 6)
        {
            System.Array.Copy(macAddress, 0, payload, 1, 6);
        }
        return Build(Commands.Set.SET_DUAL_DEVICE, payload);
    }
}
