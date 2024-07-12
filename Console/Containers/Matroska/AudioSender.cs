using System.Threading.Channels;
using Concentus;
using Concentus.Structs;
using Console.Buffers;
using OpenTK.Audio.OpenAL;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Console.Containers.Matroska;

internal class AudioSender(
    MatroskaPlayerBuffer source,
    int sourceId,
    ALFormat targetFormat,
    CancellationToken token = default
)
{
    private const int sampleRate = 48000;
    private const int channels = 2;
    private readonly IOpusDecoder _decoder = OpusCodecFactory.CreateDecoder(sampleRate, channels);
    private readonly Channel<byte[]> _queue = Channel.CreateBounded<byte[]>(50);

    private IAsyncEnumerable<ReadOnlyMemory<byte>> _frames = source.GetFrames(
        CancellationToken.None
    );

    public int SampleRate => sampleRate;

    public int Channels => channels;

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

    public void StartSending()
    {
        Task.Run(
            async () =>
            {
                await foreach (var item in source.WriteFile(token))
                {
                    var data = item.ToArray();
                    var frames = OpusPacketInfo.GetNumFrames(data);
                    var samplePerFrame = OpusPacketInfo.GetNumSamplesPerFrame(data, SampleRate);
                    var frameSize = frames * samplePerFrame;
                    short[] pcm = new short[frameSize * Channels];
                    _decoder.Decode(data, pcm, frameSize);
                    var result = ShortsToBytes(pcm, 0, pcm.Length);
                    await _queue.Writer.WriteAsync(result, token);
                }
            },
            token
        );

        Task.Run(
            async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    var next = await _queue.Reader.ReadAsync(token);

                    var bufferId = AL.GenBuffer();
                    AL.BufferData(bufferId, targetFormat, next, sampleRate);
                    AL.SourceQueueBuffer(sourceId, bufferId);
                }
            },
            token
        );

        AL.SourcePlay(sourceId);
    }
}
