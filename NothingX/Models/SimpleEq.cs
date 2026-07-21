namespace NothingX.Models;

/// <summary>
/// Represents the Simple 3-band equalizer configuration.
/// The payload consists of 1 byte for number of bands, 4 bytes for total gain, 
/// and 13 bytes per band (filterType, gain, freq, quality). Total: 5 byte header + 39 bytes = 44 bytes.
/// </summary>
public class SimpleEq
{
    public float TotalGain { get; set; } = 0.0f;
    public EqBand[] Bands { get; } = new EqBand[3];

    public SimpleEq()
    {
        // Bass
        Bands[0] = new EqBand { FilterType = 0, Gain = 0, Frequency = 140.0f, Quality = 0.8f };
        // Mid
        Bands[1] = new EqBand { FilterType = 1, Gain = 0, Frequency = 980.0f, Quality = 0.7f };
        // Treble
        Bands[2] = new EqBand { FilterType = 2, Gain = 0, Frequency = 3400.0f, Quality = 0.7f };
    }

    public byte[] ToPayload()
    {
        int size = Bands.Length;
        // 1 byte (size) + 4 bytes (TotalGain) + size * 13 bytes
        byte[] payload = new byte[5 + size * 13];
        
        payload[0] = (byte)size;
        
        // Write TotalGain (float -> 4 bytes)
        var gainBytes = BitConverter.GetBytes(TotalGain);
        Array.Copy(gainBytes, 0, payload, 1, 4);

        int offset = 5;
        for (int i = 0; i < size; i++)
        {
            var band = Bands[i];
            payload[offset] = band.FilterType;
            offset += 1;

            var bGain = BitConverter.GetBytes(Math.Clamp(band.Gain, -6f, 6f));
            Array.Copy(bGain, 0, payload, offset, 4);
            offset += 4;

            var bFreq = BitConverter.GetBytes(band.Frequency);
            Array.Copy(bFreq, 0, payload, offset, 4);
            offset += 4;

            var bQual = BitConverter.GetBytes(band.Quality);
            Array.Copy(bQual, 0, payload, offset, 4);
            offset += 4;
        }

        return payload;
    }

    public static SimpleEq FromPayload(byte[] payload)
    {
        var eq = new SimpleEq();
        if (payload.Length >= 5)
        {
            int size = payload[0];
            eq.TotalGain = BitConverter.ToSingle(payload, 1);

            int offset = 5;
            for (int i = 0; i < Math.Min(size, 3); i++)
            {
                if (offset + 13 > payload.Length) break;

                eq.Bands[i].FilterType = payload[offset];
                offset += 1;
                eq.Bands[i].Gain = BitConverter.ToSingle(payload, offset);
                offset += 4;
                eq.Bands[i].Frequency = BitConverter.ToSingle(payload, offset);
                offset += 4;
                eq.Bands[i].Quality = BitConverter.ToSingle(payload, offset);
                offset += 4;
            }
        }
        return eq;
    }
}
