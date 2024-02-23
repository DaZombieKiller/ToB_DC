using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct BinkHeader
{
    public fixed byte Signature[3];
    public byte Revision;
    public uint FileLength;
    public uint FrameCount;
    public uint MaxFrameLength;
    public uint FrameCount2;
    public uint Width;
    public uint Height;
    public uint FpsDividend;
    public uint FpsDivider;
    public uint Flags;
    public uint AudioTrackCount;
    public uint Unknown;
}
