namespace NothingX.Models;

/// <summary>
/// Represents the custom 8-band equalizer configuration.
/// The payload consists of 1 byte for number of bands, 4 bytes for total gain, 
/// and 13 bytes per band (filterType, gain, freq, quality).
/// </summary>
public class CustomEq
{
    public byte ProfileIndex { get; set; } = 0;
    public float TotalGain { get; set; } = 0.0f;
    public EqBand[] Bands { get; } = new EqBand[8];

    public CustomEq()
    {
        // Default frequencies for the Nothing 8-band Advanced EQ
        Bands[0] = new EqBand { FilterType = 1, Gain = 0, Frequency = 55, Quality = 1.0f };
        Bands[1] = new EqBand { FilterType = 1, Gain = 0, Frequency = 110, Quality = 1.0f };
        Bands[2] = new EqBand { FilterType = 1, Gain = 0, Frequency = 220, Quality = 1.0f };
        Bands[3] = new EqBand { FilterType = 1, Gain = 0, Frequency = 440, Quality = 1.0f };
        Bands[4] = new EqBand { FilterType = 1, Gain = 0, Frequency = 1320, Quality = 1.0f };
        Bands[5] = new EqBand { FilterType = 1, Gain = 0, Frequency = 3300, Quality = 1.0f };
        Bands[6] = new EqBand { FilterType = 1, Gain = 0, Frequency = 6600, Quality = 1.0f };
        Bands[7] = new EqBand { FilterType = 1, Gain = 0, Frequency = 13200, Quality = 1.0f };
    }

    /// <summary>
    /// Serializes the custom EQ into the binary payload format for the earbuds.
    /// Payload is EXACTLY 110 bytes for 8 bands.
    /// </summary>
    public byte[] ToPayload()
    {
        int size = Bands.Length;
        // 1 byte (ProfileIndex) + 1 byte (size) + 4 bytes (TotalGain) + size * 13 bytes
        byte[] payload = new byte[6 + size * 13];
        
        payload[0] = ProfileIndex;
        payload[1] = (byte)size;
        
        // Write TotalGain (float -> 4 bytes)
        var gainBytes = BitConverter.GetBytes(TotalGain);
        Array.Copy(gainBytes, 0, payload, 2, 4);

        int offset = 6;
        for (int i = 0; i < size; i++)
        {
            Bands[i].WriteToPayload(payload, ref offset);
        }

        return payload;
    }

    /// <summary>
    /// Deserializes a binary payload into a CustomEq object.
    /// </summary>
    public static CustomEq? FromPayload(byte[] payload)
    {
        if (payload.Length < 6) return null;
        
        var eq = new CustomEq();
        eq.ProfileIndex = payload[0];
        int size = payload[1];
        
        if (size > 8) return null;
        if (payload.Length < 6 + size * 13) return null;

        eq.TotalGain = BitConverter.ToSingle(payload, 2);

        int offset = 6;
        for (int i = 0; i < size && i < 8; i++)
        {
            eq.Bands[i].ReadFromPayload(payload, ref offset);
        }

        return eq;
    }
}

public class EqBand
{
    public byte FilterType { get; set; }
    public float Gain { get; set; }
    public float Frequency { get; set; }
    public float Quality { get; set; }

    public void WriteToPayload(byte[] payload, ref int offset)
    {
        payload[offset++] = FilterType;
        
        var bGain = BitConverter.GetBytes(Math.Clamp(Gain, -6f, 6f));
        Array.Copy(bGain, 0, payload, offset, 4);
        offset += 4;

        var bFreq = BitConverter.GetBytes(Frequency);
        Array.Copy(bFreq, 0, payload, offset, 4);
        offset += 4;

        var bQual = BitConverter.GetBytes(Quality);
        Array.Copy(bQual, 0, payload, offset, 4);
        offset += 4;
    }

    public void ReadFromPayload(byte[] payload, ref int offset)
    {
        FilterType = payload[offset++];
        Gain = BitConverter.ToSingle(payload, offset);
        offset += 4;
        Frequency = BitConverter.ToSingle(payload, offset);
        offset += 4;
        Quality = BitConverter.ToSingle(payload, offset);
        offset += 4;
    }
}
