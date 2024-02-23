using System.Runtime.InteropServices;

public unsafe sealed class BinkFile : IDisposable
{
    private BinkHeader _header;

    private FrameOffset[] _frameOffsets;

    private AudioTrackInfo[] _trackInfo;

    private uint[] _trackIDs;

    public Stream BinkStream { get; }

    public ref readonly BinkHeader Header => ref _header;

    public ReadOnlySpan<uint> TrackIDs => _trackIDs;

    public ReadOnlySpan<AudioTrackInfo> TrackInfo => _trackInfo;

    public ReadOnlySpan<FrameOffset> FrameOffsets => _frameOffsets;

    public long FrameOffsetPosition { get; }

    public BinkFile(Stream stream)
    {
        BinkStream = stream;
        stream.ReadExactly(MemoryMarshal.AsBytes(new Span<BinkHeader>(ref _header)));
        _trackInfo = new AudioTrackInfo[_header.AudioTrackCount];

        for (int i = 0; i < _header.AudioTrackCount; i++)
            stream.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref _trackInfo[i].Unknown, 2)));

        for (int i = 0; i < _header.AudioTrackCount; i++)
            stream.ReadExactly(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref _trackInfo[i].SampleRate, 2)));

        _trackIDs = new uint[_header.AudioTrackCount];
        stream.ReadExactly(MemoryMarshal.AsBytes(_trackIDs.AsSpan()));

        FrameOffsetPosition = stream.Position;
        _frameOffsets = new FrameOffset[_header.FrameCount + 1];
        stream.ReadExactly(MemoryMarshal.AsBytes(_frameOffsets.AsSpan()));
    }

    public BinkFrame ReadFrame(int index, bool audioOnly)
    {
        uint length;
        BinkStream.Position = FrameOffsets[index].Value;
        var audio = new byte[_header.AudioTrackCount][];

        for (int i = 0; i < _header.AudioTrackCount; i++)
        {
            BinkStream.ReadExactly(new Span<byte>(&length, 4));
            var frame = new byte[length];
            BinkStream.ReadExactly(frame);
            audio[i] = frame;
        }

        if (audioOnly)
            return new BinkFrame { AudioFrames = audio };

        length = FrameOffsets[index + 1].Value - (uint)BinkStream.Position;
        var video = new byte[length];
        BinkStream.ReadExactly(video);
        return new BinkFrame
        {
            AudioFrames = audio,
            VideoFrame = video,
        };
    }

    public void Dispose()
    {
        BinkStream.Dispose();
    }
}
