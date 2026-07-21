namespace NothingX.Protocol;

/// <summary>
/// CRC-16 implementation matching the Nothing earbuds firmware.
/// Uses CRC-16/CCITT-FALSE (polynomial 0x1021, init 0xFFFF).
/// Reverse-engineered from com.nothing.base.util.Utils.obtainCrc16()
/// </summary>
public static class Crc16
{
    private const ushort Polynomial = 0x1021;
    private const ushort InitialValue = 0xFFFF;

    private static readonly ushort[] Table = GenerateTable();

    private static ushort[] GenerateTable()
    {
        var table = new ushort[256];
        for (int i = 0; i < 256; i++)
        {
            ushort crc = (ushort)(i << 8);
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ Polynomial);
                else
                    crc <<= 1;
            }
            table[i] = crc;
        }
        return table;
    }

    /// <summary>
    /// Calculate CRC-16/CCITT-FALSE checksum over the given data.
    /// </summary>
    public static ushort Calculate(ReadOnlySpan<byte> data)
    {
        ushort crc = InitialValue;
        foreach (byte b in data)
        {
            crc = (ushort)((crc << 8) ^ Table[((crc >> 8) ^ b) & 0xFF]);
        }
        return crc;
    }

    /// <summary>
    /// Calculate CRC-16 and return as a 2-byte array (little-endian).
    /// </summary>
    public static byte[] CalculateBytes(ReadOnlySpan<byte> data)
    {
        ushort crc = Calculate(data);
        return [(byte)(crc & 0xFF), (byte)((crc >> 8) & 0xFF)];
    }
}
