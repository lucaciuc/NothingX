namespace NothingX.Models;

public enum GestureType : byte
{
    SingleTap = 1,
    DoubleTap = 2,
    TripleTap = 3,
    LongPress = 4,
    DoubleTapAndHold = 5
}

public enum GestureAction : byte
{
    NoAction = 0,
    PlayPause = 1,
    NextTrack = 2,
    PreviousTrack = 3,
    VoiceAssistant = 4,
    NoiseCancellation = 5,
    VolumeUp = 6,
    VolumeDown = 7
}

public class GestureConfig
{
    public byte Device { get; set; } // 0 = Left, 1 = Right
    public byte Button { get; set; } = 1;
    public GestureType Gesture { get; set; }
    public GestureAction Action { get; set; }

    public byte[] ToPayload()
    {
        // 1 byte (number of operations) + 4 bytes for the single operation
        return [
            1, 
            Device, 
            Button, 
            (byte)Gesture, 
            (byte)Action
        ];
    }
}
