using System.Text;
using System.Runtime.InteropServices;

unsafe
{
    using var video = new BinkFile(File.Open(args[0], FileMode.Open, FileAccess.ReadWrite));
    using var patch = new BinaryReader(File.OpenRead(args[1]));

    int startFrame = patch.ReadInt32();
    int frameCount = patch.ReadInt32();

    if (frameCount == 0)
        return;

    var header  = video.Header;
    var offsets = video.FrameOffsets.ToArray();

    // Store the rest of the video in memory temporarily so we can write it again after.
    using var ms = new MemoryStream();
    video.BinkStream.Seek(offsets[startFrame + frameCount].Value, SeekOrigin.Begin);
    video.BinkStream.CopyTo(ms);

    // Read audio frames.
    var frames = new BinkFrame[frameCount];

    for (int i = 0; i < frames.Length; i++)
        frames[i] = video.ReadFrame(startFrame + i, audioOnly: true);

    using (var writer = new BinaryWriter(video.BinkStream, Encoding.UTF8, leaveOpen: true))
    {
        int i;

        // Seek to the first frame we're replacing.
        video.BinkStream.Position = offsets[startFrame].Value;

        for (i = startFrame; i < startFrame + frameCount; i++)
        {
            offsets[i].IsKeyframe = patch.ReadBoolean();
            offsets[i].Value = (uint)video.BinkStream.Position;

            var frameSize = patch.ReadInt32();
            var frameData = patch.ReadBytes(frameSize);

            foreach (var track in frames[i - startFrame].AudioFrames)
            {
                frameSize += 4;
                frameSize += track.Length;
                writer.Write(track.Length);
                writer.Write(track);
            }

            header.MaxFrameLength = uint.Max(header.MaxFrameLength, (uint)frameSize);
            writer.Write(frameData);
        }

        var offset = (uint)video.BinkStream.Position - offsets[i].Value;

        for (; i < header.FrameCount; i++)
        {
            offsets[i].Value += offset;
        }
    }

    ms.Position = 0; 
    ms.CopyTo(video.BinkStream);
    header.FileLength = (uint)video.BinkStream.Position - 8;
    offsets[^1].Value = (uint)video.BinkStream.Position;
    video.BinkStream.SetLength(video.BinkStream.Position);

    video.BinkStream.Position = 0;
    video.BinkStream.Write(new ReadOnlySpan<byte>(&header, sizeof(BinkHeader)));
    video.BinkStream.Position = video.FrameOffsetPosition;
    video.BinkStream.Write(MemoryMarshal.AsBytes(offsets.AsSpan()));
}
