using System.Diagnostics;

public struct FrameOffset
{
    public uint RawValue;

    public bool IsKeyframe
    {
        readonly get => (RawValue & 1) != 0;
        set => RawValue = value ? (RawValue | 1) : (RawValue & ~1u);
    }

    public uint Value
    {
        readonly get => RawValue & ~1u;
        set
        {
            RawValue = (RawValue & 1) | (value & ~1u);
            Debug.Assert(value == (value & ~1));
        }
    }
}
