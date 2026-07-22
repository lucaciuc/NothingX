namespace NothingX.Models;

/// <summary>ANC (Active Noise Cancellation) modes</summary>
public enum AncMode : byte
{
    NoiseCancellation = 1,
    Transparency = 7,
    Adaptive = 4,
    Off = 5
}

/// <summary>EQ preset modes</summary>
public enum EqPreset : byte
{
    Balanced = 0,
    Voice = 1,
    MoreTreble = 2,
    MoreBass = 3,
    DiracOpteo = 4,
    Custom = 5
}

/// <summary>Spatial audio modes</summary>
public enum SpatialAudioMode : byte
{
    Off = 0,
    Concert = 2,
    Cinema = 3
}
