using System.Buffers.Binary;

namespace NothingX.Protocol;

/// <summary>
/// Represents a Nothing earbuds protocol packet.
/// 
/// Packet structure:
/// ┌──────┬─────────┬─────────┬────────┬─────┬──────────────┬──────────┐
/// │ SOF  │ Control │ Command │ Length │ FSN │   Payload    │ CRC16    │
/// │ 1B   │  2B     │   2B    │  2B    │ 1B  │  Length B    │ 2B (opt) │
/// └──────┴─────────┴─────────┴────────┴─────┴──────────────┴──────────┘
/// </summary>
public class NothingPacket
{
    public const byte DEFAULT_SOF = 0x55;
    public const int HEADER_SIZE = 8; // SOF(1) + Control(2) + Command(2) + Length(2) + FSN(1)
    public const int CRC_SIZE = 2;

    // Control field masks
    private const int MASK_RSP_CODE = 0x1F;
    private const int MASK_CRC = 0x20;
    private const int MASK_MULTI_FRAME = 0x40;
    private const int MASK_DEVICE_TYPE = 0x0F00;

    // Command masks
    private const int MASK_REQUEST = 0x8000;
    private const int MASK_RESPONSE = 0x7FFF;

    // Device types
    public const int DEVICE_TYPE_TWS = 1;
    public const int DEVICE_TYPE_WATCH = 2;

    /// <summary>Start of frame byte (always 0x55)</summary>
    public byte Sof { get; set; } = DEFAULT_SOF;

    /// <summary>Control field (2 bytes) — contains device type, CRC flag, multi-frame flag, response code</summary>
    public ushort Control { get; set; }

    /// <summary>Command identifier (2 bytes)</summary>
    public ushort Command { get; set; }

    /// <summary>Payload length in bytes</summary>
    public ushort Length { get; set; }

    /// <summary>Frame sequence number (0-254)</summary>
    public byte Fsn { get; set; }

    /// <summary>Command payload data</summary>
    public byte[] Payload { get; set; } = [];

    /// <summary>CRC-16 checksum (if CRC flag is set)</summary>
    public ushort Checksum { get; set; }

    /// <summary>Raw bytes this packet was parsed from (null if constructed)</summary>
    public byte[]? RawBytes { get; set; }

    // --- Control field accessors ---

    public int ResponseCode => Control & MASK_RSP_CODE;
    public bool HasCrc => (Control & MASK_CRC) != 0;
    public bool IsMultiFrame => (Control & MASK_MULTI_FRAME) != 0;
    public int DeviceType => (Control & MASK_DEVICE_TYPE) >> 8;
    public bool IsOk => ResponseCode == 0;

    // --- Command accessors ---

    public bool IsRequest => (Command & MASK_REQUEST) != 0;
    public int RequestCommand => Command | MASK_REQUEST;
    public int ResponseCommand => Command & MASK_RESPONSE;

    /// <summary>Total packet size in bytes</summary>
    public int PacketSize => HEADER_SIZE + Length + (HasCrc ? CRC_SIZE : 0);

    /// <summary>
    /// Set the control field from component values.
    /// </summary>
    public void SetControl(int rspCode = 0, bool crc = true, bool multiFrame = false, int deviceType = DEVICE_TYPE_TWS)
    {
        ushort ctrl = (ushort)(rspCode & MASK_RSP_CODE);
        if (crc) ctrl |= MASK_CRC;
        if (multiFrame) ctrl |= MASK_MULTI_FRAME;
        ctrl |= (ushort)((deviceType << 8) & MASK_DEVICE_TYPE);
        Control = ctrl;
    }

    /// <summary>
    /// Serialize this packet to a byte array ready for transmission.
    /// </summary>
    public byte[] ToBytes()
    {
        int totalSize = HEADER_SIZE + Payload.Length + (HasCrc ? CRC_SIZE : 0);
        var buffer = new byte[totalSize];

        buffer[0] = Sof;
        // Control: 2 bytes, little-endian byte order in the wire format
        buffer[1] = (byte)(Control & 0xFF);
        buffer[2] = (byte)((Control >> 8) & 0xFF);
        // Command: 2 bytes
        buffer[3] = (byte)(Command & 0xFF);
        buffer[4] = (byte)((Command >> 8) & 0xFF);
        // Length: 2 bytes
        buffer[5] = (byte)(Payload.Length & 0xFF);
        buffer[6] = (byte)((Payload.Length >> 8) & 0xFF);
        // FSN
        buffer[7] = Fsn;
        // Payload
        if (Payload.Length > 0)
        {
            Array.Copy(Payload, 0, buffer, HEADER_SIZE, Payload.Length);
        }
        // CRC16
        if (HasCrc)
        {
            var dataForCrc = buffer.AsSpan(0, HEADER_SIZE + Payload.Length);
            ushort crc = Crc16.Calculate(dataForCrc);
            buffer[^2] = (byte)(crc & 0xFF);
            buffer[^1] = (byte)((crc >> 8) & 0xFF);
            Checksum = crc;
        }

        return buffer;
    }

    /// <summary>
    /// Parse a packet from raw bytes.
    /// </summary>
    public static NothingPacket? Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < HEADER_SIZE)
            return null;

        var packet = new NothingPacket
        {
            Sof = data[0],
            Control = (ushort)(data[1] | (data[2] << 8)),
            Command = (ushort)(data[3] | (data[4] << 8)),
            Length = (ushort)(data[5] | (data[6] << 8)),
            Fsn = data[7]
        };

        int payloadEnd = HEADER_SIZE + packet.Length;
        if (payloadEnd > data.Length)
            return null;

        if (packet.Length > 0)
        {
            packet.Payload = data.Slice(HEADER_SIZE, packet.Length).ToArray();
        }

        if (packet.HasCrc)
        {
            int crcOffset = payloadEnd;
            if (crcOffset + CRC_SIZE > data.Length)
                return null;
            packet.Checksum = (ushort)(data[crcOffset] | (data[crcOffset + 1] << 8));
        }

        packet.RawBytes = data[..packet.PacketSize].ToArray();
        return packet;
    }

    /// <summary>
    /// Verify the CRC checksum of this packet.
    /// </summary>
    public bool VerifyCrc()
    {
        if (!HasCrc || RawBytes == null) return true;
        var dataForCrc = RawBytes.AsSpan(0, HEADER_SIZE + Length);
        return Crc16.Calculate(dataForCrc) == Checksum;
    }

    /// <summary>
    /// Extract the payload as an integer value.
    /// </summary>
    public int PayloadAsInt()
    {
        if (Payload.Length == 0) return 0;
        if (Payload.Length == 1) return Payload[0];
        if (Payload.Length == 2) return Payload[0] | (Payload[1] << 8);
        if (Payload.Length >= 4) return Payload[0] | (Payload[1] << 8) | (Payload[2] << 16) | (Payload[3] << 24);
        return Payload[0];
    }

    /// <summary>
    /// Extract the payload as a boolean (first byte == 1).
    /// </summary>
    public bool PayloadAsBool() => Payload.Length > 0 && Payload[0] == 1;

    /// <summary>
    /// Extract the payload as a UTF-8 string.
    /// </summary>
    public string PayloadAsString()
    {
        string raw = System.Text.Encoding.UTF8.GetString(Payload);
        return new string(raw.Where(c => !char.IsControl(c)).ToArray());
    }

    public override string ToString()
    {
        string cmdName = Commands.GetName(ResponseCommand);
        return $"Packet(cmd={cmdName} [0x{Command:X4}], len={Length}, fsn={Fsn}, rsp={ResponseCode}, payload={Convert.ToHexString(Payload)})";
    }
}
