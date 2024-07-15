using System.Threading.Channels;
using OpenTK.Audio.OpenAL;

namespace Console.Audio;

internal class AudioSender(int sourceId, ALFormat targetFormat)
{
    private readonly Channel<byte[]> _queue = Channel.CreateBounded<byte[]>(50);
    public readonly int SampleRate = 48000;
    public readonly int Channels = 2;
    private bool _clearBuffer = false;

    public void ClearBuffer()
    {
        _clearBuffer = true;
        while (_queue.Reader.TryRead(out var _)) { }
    }

    public async ValueTask Add(byte[] data) => await _queue.Writer.WriteAsync(data);

    private async ValueTask ClearBufferAL(CancellationToken token)
    {
        AL.SourceStop(sourceId);

        AL.GetSource(sourceId, ALGetSourcei.BuffersQueued, out int queuedCount);

        if (queuedCount > 0)
        {
            int[] bufferIds = new int[queuedCount];
            AL.SourceUnqueueBuffers(sourceId, queuedCount, bufferIds);
            foreach (var buffer in bufferIds)
            {
                var next = await _queue.Reader.ReadAsync(token);
                AL.BufferData(buffer, targetFormat, next, SampleRate);
                AL.SourceQueueBuffer(sourceId, buffer);
            }
        }

        _clearBuffer = false;
        AL.SourcePlay(sourceId);
    }

    public async Task StartSending(CancellationToken token = default)
    {
        var fillBuffers = await _queue
            .Reader.ReadAllAsync(token)
            .Take(10)
            .ToListAsync(cancellationToken: token);

        foreach (var item in fillBuffers)
        {
            var buffer = AL.GenBuffer();
            AL.BufferData(buffer, targetFormat, item, SampleRate);
            AL.SourceQueueBuffer(sourceId, buffer);
        }

        var _ = Task.Run(
            async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        if (_clearBuffer)
                        {
                            await ClearBufferAL(token);
                            continue;
                        }

                        AL.GetSource(
                            sourceId,
                            ALGetSourcei.BuffersProcessed,
                            out int releasedCount
                        );

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
                }
                finally
                {
                    await ClearBufferAL(token);
                }
            },
            token
        );

        AL.SourcePlay(sourceId);
    }
}
