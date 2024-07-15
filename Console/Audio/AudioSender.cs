using System.Threading.Channels;
using OpenTK.Audio.OpenAL;

namespace Console.Audio;

internal class AudioSender(int sourceId, ALFormat targetFormat, CancellationToken token = default)
{
    private readonly Channel<byte[]> _queue = Channel.CreateBounded<byte[]>(50);
    public readonly int SampleRate = 48000;
    public readonly int Channels = 2;

    public void ClearBuffer()
    {
        while (_queue.Reader.TryRead(out var _)) { }
    }

    public async ValueTask Add(byte[] data) => await _queue.Writer.WriteAsync(data);

    public async Task StartSending()
    {
        var fillBuffers = await _queue.Reader.ReadAllAsync(token).Take(10).ToListAsync();

        foreach (var item in fillBuffers)
        {
            var buffer = AL.GenBuffer();
            AL.BufferData(buffer, targetFormat, item, SampleRate);
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
                            AL.BufferData(buffer, targetFormat, next, SampleRate);
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
