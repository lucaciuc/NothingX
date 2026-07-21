namespace NothingX.Protocol;

/// <summary>
/// Parses incoming byte streams into NothingPacket objects.
/// Handles buffering and partial reads from the RFCOMM stream.
/// </summary>
public class PacketParser
{
    private readonly List<byte> _buffer = [];
    private const int MAX_BUFFER_SIZE = 8192;

    /// <summary>
    /// Feed raw bytes from the Bluetooth stream. Returns all complete packets found.
    /// </summary>
    public IReadOnlyList<NothingPacket> Feed(ReadOnlySpan<byte> data)
    {
        var packets = new List<NothingPacket>();

        // Append new data to buffer
        foreach (byte b in data)
        {
            _buffer.Add(b);
        }

        // Prevent unbounded buffer growth
        if (_buffer.Count > MAX_BUFFER_SIZE)
        {
            _buffer.RemoveRange(0, _buffer.Count - MAX_BUFFER_SIZE);
        }

        // Try to extract packets
        while (TryExtractPacket(out var packet))
        {
            if (packet != null)
            {
                packets.Add(packet);
            }
        }

        return packets;
    }

    private bool TryExtractPacket(out NothingPacket? packet)
    {
        packet = null;

        // Find SOF byte
        int sofIndex = _buffer.IndexOf(NothingPacket.DEFAULT_SOF);
        if (sofIndex < 0)
        {
            _buffer.Clear();
            return false;
        }

        // Discard bytes before SOF
        if (sofIndex > 0)
        {
            _buffer.RemoveRange(0, sofIndex);
        }

        // Need at least header
        if (_buffer.Count < NothingPacket.HEADER_SIZE)
            return false;

        // Read control to check for CRC
        ushort control = (ushort)(_buffer[1] | (_buffer[2] << 8));
        bool hasCrc = (control & 0x20) != 0;

        // Read payload length
        ushort length = (ushort)(_buffer[5] | (_buffer[6] << 8));

        // Sanity check length
        if (length > 512)
        {
            // Bad packet, skip this SOF
            _buffer.RemoveAt(0);
            return true; // Try again
        }

        int totalSize = NothingPacket.HEADER_SIZE + length + (hasCrc ? NothingPacket.CRC_SIZE : 0);

        // Wait for more data
        if (_buffer.Count < totalSize)
            return false;

        // Parse the packet
        var rawData = _buffer.GetRange(0, totalSize).ToArray();
        packet = NothingPacket.Parse(rawData);

        // Remove parsed bytes from buffer
        _buffer.RemoveRange(0, totalSize);

        if (packet != null && hasCrc && !packet.VerifyCrc())
        {
            // CRC mismatch, discard packet
            Console.WriteLine($"[Parser] CRC mismatch for packet: {packet} (Length: {length})");
            packet = null;
        }

        return true;
    }

    /// <summary>Clear the internal buffer</summary>
    public void Reset() => _buffer.Clear();
}
