using System.Text;
using Concentus;
using Concentus.Structs;
using Console.Buffers;
using Spectre.Console;

namespace Console.Containers.Matroska;

internal class OpusToPCMStream(MatroskaPlayerBuffer source) : Stream
{
    private const int sampleRate = 48000; // Adjust this to match your actual sample rate
    private const int channels = 2;
    private readonly IOpusDecoder _decoder = OpusCodecFactory.CreateDecoder(sampleRate, channels);
    private IAsyncEnumerator<ReadOnlyMemory<byte>> _frames = source
        .GetFrames(CancellationToken.None)
        .GetAsyncEnumerator();

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

    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer, offset, count).Result;

    public override async Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var data = _frames.Current.ToArray();

            // Decode Opus data
            var frames = OpusPacketInfo.GetNumFrames(data);
            var samplePerFrame = OpusPacketInfo.GetNumSamplesPerFrame(data, sampleRate);
            var frameSize = frames * samplePerFrame;
            short[] pcm = new short[frameSize * channels];

            int decodedSamples = _decoder.Decode(data, pcm, frameSize);

            // Convert decoded PCM samples to bytes and copy to 'buffer'
            Buffer.BlockCopy(pcm, 0, buffer, offset, decodedSamples * sizeof(short));

            return decodedSamples * sizeof(short);
        }
        finally
        {
            await _frames.MoveNextAsync();
        }
    }

    public override long Seek(long offset, SeekOrigin origin) =>
        source.InputStream.Seek(offset, origin);

    public override void SetLength(long value) { }

    public override void Write(byte[] buffer, int offset, int count) { }
}
