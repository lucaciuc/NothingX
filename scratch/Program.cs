using System;

namespace scratch
{
    class Program
    {
        static void Main()
        {
            byte[] packet = new byte[] { 
                0x55, 
                0x20, 0x01, // Control (CRC=1, Dev=1)
                0x1E, 0x40, // Command 0x401E
                0x06, 0x00, // Length 6
                0x09,       // FSN 9
                0x01, 0x05, 0x00, 0x02, 0x01, 0x00 // Payload
            };

            ushort crc = Calculate(packet);
            Console.WriteLine($"CRC with SOF: {crc:X4}");

            ushort crcNoSof = Calculate(packet.AsSpan(1));
            Console.WriteLine($"CRC without SOF: {crcNoSof:X4}");
            
            // Check packet with cmd 0x7001 (len=0, fsn=1)
            byte[] p2 = new byte[] {
                0x55,
                0x20, 0x01,
                0x01, 0x70,
                0x00, 0x00,
                0x01
            };
            Console.WriteLine($"P2 CRC with SOF: {Calculate(p2):X4}");
            Console.WriteLine($"P2 CRC without SOF: {Calculate(p2.AsSpan(1)):X4}");
        }

        static ushort Calculate(ReadOnlySpan<byte> data)
        {
            ushort crc = 0xFFFF;
            ushort[] table = GenerateTable();
            foreach (byte b in data)
            {
                crc = (ushort)((crc << 8) ^ table[((crc >> 8) ^ b) & 0xFF]);
            }
            return crc;
        }

        static ushort[] GenerateTable()
        {
            var table = new ushort[256];
            for (int i = 0; i < 256; i++)
            {
                ushort crc = (ushort)(i << 8);
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x8000) != 0) crc = (ushort)((crc << 1) ^ 0x1021);
                    else crc <<= 1;
                }
                table[i] = crc;
            }
            return table;
        }
    }
}
