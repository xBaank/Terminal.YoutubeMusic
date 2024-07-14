using System.Threading.Channels;
using Concentus;
using Concentus.Structs;
using OpenTK.Audio.OpenAL;

namespace Console.Audio;

internal class AudioSender(int sourceId, ALFormat targetFormat, CancellationToken token = default)
{
    private const int sampleRate = 48000;
    private const int channels = 2;
    private readonly IOpusDecoder _decoder = OpusCodecFactory.CreateDecoder(sampleRate, channels);
    private readonly Channel<byte[]> _queue = Channel.CreateBounded<byte[]>(50);
    public int SampleRate => sampleRate;

    public int Channels => channels;

    private static byte[] ShortsToBytes(short[] input, int offset, int length)
    {
        byte[] processedValues = new byte[length * 2];
        for (int i = 0; i < length; i++)
        {
            processedValues[i * 2] = (byte)input[i + offset];
            processedValues[i * 2 + 1] = (byte)(input[i + offset] >> 8);
        }

        return processedValues;
    }

    public async ValueTask Add(byte[] data)
    {
        var frames = OpusPacketInfo.GetNumFrames(data);
        var samplePerFrame = OpusPacketInfo.GetNumSamplesPerFrame(data, sampleRate);
        var frameSize = frames * samplePerFrame;
        short[] pcm = new short[frameSize * channels];
        _decoder.Decode(data, pcm, frameSize);
        var result = ShortsToBytes(pcm, 0, pcm.Length);
        await _queue.Writer.WriteAsync(result);
    }

    public async Task StartSending()
    {
        var fillBuffers = await _queue.Reader.ReadAllAsync(token).Take(50).ToListAsync();

        foreach (var item in fillBuffers)
        {
            var buffer = AL.GenBuffer();
            AL.BufferData(buffer, targetFormat, item, sampleRate);
            AL.SourceQueueBuffer(sourceId, buffer);
        }

        var _ = Task.Run(
            async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    AL.GetSource(sourceId, ALGetSourcei.BuffersProcessed, out int releasedCount);

                    if (releasedCount > 0)
                    {
                        int[] bufferIds = new int[releasedCount];
                        AL.SourceUnqueueBuffers(sourceId, releasedCount, bufferIds);
                        foreach (var buffer in bufferIds)
                        {
                            var next = await _queue.Reader.ReadAsync(token);
                            AL.BufferData(buffer, targetFormat, next, sampleRate);
                            AL.SourceQueueBuffer(sourceId, buffer);
                        }
                    }

                    AL.GetSource(sourceId, ALGetSourcei.SourceState, out int stateInt);
                    if ((ALSourceState)stateInt == ALSourceState.Stopped)
                    {
                        AL.SourcePlay(sourceId);
                    }

                    await Task.Delay(100);
                }
            },
            token
        );

        AL.SourcePlay(sourceId);
    }
}
