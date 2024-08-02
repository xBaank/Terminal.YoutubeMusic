using System.Threading.Channels;
using OpenTK.Audio.OpenAL;

namespace Console.Audio;

internal class AudioSender(int sourceId, ALFormat targetFormat) : IAsyncDisposable
{
    private readonly Channel<PcmPacket> _queue = Channel.CreateBounded<PcmPacket>(150);
    public readonly int SampleRate = 48000;
    public readonly int Channels = 2;
    private readonly int[] _buffers = AL.GenBuffers(50);
    private bool _clearBuffer = false;

    public void ClearBuffer()
    {
        _clearBuffer = true;
        while (_queue.Reader.TryRead(out var next))
        {
            next.Dispose();
        }
    }

    public async ValueTask Add(PcmPacket data) => await _queue.Writer.WriteAsync(data);

    private async ValueTask ClearBufferAL(CancellationToken token)
    {
        AL.GetSource(sourceId, ALGetSourcei.SourceState, out int initialState);

        AL.SourceStop(sourceId);
        AL.GetSource(sourceId, ALGetSourcei.BuffersQueued, out int queuedCount);

        if (queuedCount > 0)
        {
            int[] bufferIds = new int[queuedCount];
            AL.SourceUnqueueBuffers(sourceId, queuedCount, bufferIds);
            foreach (var buffer in bufferIds)
            {
                using var next = await _queue.Reader.ReadAsync(token);
                AL.BufferData(buffer, targetFormat, next.Data, SampleRate);
                AL.SourceQueueBuffer(sourceId, buffer);
            }
        }

        _clearBuffer = false;

        if ((ALSourceState)initialState == ALSourceState.Playing)
        {
            AL.SourcePlay(sourceId);
        }

        if ((ALSourceState)initialState == ALSourceState.Paused)
        {
            AL.SourcePlay(sourceId);
            AL.SourcePause(sourceId);
        }
    }

    public async Task StartSending(CancellationToken token = default)
    {
        var fillBuffers = await _queue
            .Reader.ReadAllAsync(token)
            .Take(10)
            .ToListAsync(cancellationToken: token);

        for (int i = 0; i < fillBuffers.Count; i++)
        {
            using var item = fillBuffers[i];
            AL.BufferData(_buffers[i], targetFormat, item.Data, SampleRate);
            AL.SourceQueueBuffer(sourceId, _buffers[i]);
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
                                using var next = await _queue.Reader.ReadAsync(token);
                                AL.BufferData(buffer, targetFormat, next.Data, SampleRate);
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

    public ValueTask DisposeAsync()
    {
        ClearBuffer();
        AL.SourceStop(sourceId);
        AL.GetSource(sourceId, ALGetSourcei.BuffersProcessed, out int releasedCount);
        int[] bufferIds = new int[releasedCount];
        AL.SourceUnqueueBuffers(sourceId, releasedCount, bufferIds);
        AL.DeleteBuffers(bufferIds);
        return ValueTask.CompletedTask;
    }
}
