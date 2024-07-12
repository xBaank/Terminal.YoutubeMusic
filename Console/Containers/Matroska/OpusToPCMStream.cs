using Concentus;
using Concentus.Structs;
using Console.Buffers;

namespace Console.Containers.Matroska;

internal class OpusToPCMStream(MatroskaPlayerBuffer source) : Stream
{
    private const int sampleRate = 48000; // Adjust this to match your actual sample rate
    private const int channels = 2;
    private readonly IOpusDecoder _decoder = OpusCodecFactory.CreateDecoder(sampleRate, channels);

    private readonly Queue<byte[]> _queue = new();

    private IAsyncEnumerable<ReadOnlyMemory<byte>> _frames = source.GetFrames(
        CancellationToken.None
    );

    public int SampleRate => sampleRate;

    public int Channels => channels;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => source.InputStream.Length;

    public override long Position
    {
        get => source.InputStream.Position;
        set => source.InputStream.Position = value;
    }

    public override void Flush() { }

    private static byte[] ShortsToBytes(short[] input, int offset, int length)
    {
        byte[] processedValues = new byte[length * 2];
        for (int i = 0; i < length; i++)
        {
            processedValues[i * 2] = (byte)(input[i + offset]);
            processedValues[i * 2 + 1] = (byte)((input[i + offset] >> 8));
        }

        return processedValues;
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer, offset, count, CancellationToken.None).Result;

    public override async Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    )
    {
        var toFill = 500 - _queue.Count;
        var fillData = await _frames.Take(toFill).Select(i => i.ToArray()).ToListAsync();
        fillData.ForEach(i => _queue.Enqueue(i));

        var data = _queue.Dequeue();

        // Decode Opus data
        var frames = OpusPacketInfo.GetNumFrames(data);
        var samplePerFrame = OpusPacketInfo.GetNumSamplesPerFrame(data, SampleRate);
        var frameSize = frames * samplePerFrame;
        short[] pcm = new short[frameSize * Channels];

        int decodedSamples = _decoder.Decode(data, pcm, frameSize);

        var result = ShortsToBytes(pcm, 0, pcm.Length);
        result.CopyTo(buffer.AsSpan());

        return result.Length;
    }

    public override long Seek(long offset, SeekOrigin origin) =>
        source.InputStream.Seek(offset, origin);

    public override void SetLength(long value) { }

    public override void Write(byte[] buffer, int offset, int count) { }
}
