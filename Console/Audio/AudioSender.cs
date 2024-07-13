﻿using System.Threading.Channels;
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
        var _ = Task.Run(
            async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    AL.GetSource(0, ALGetSourcei.BuffersProcessed, out int releasedCount);

                    if (releasedCount > 0)
                    {
                        int[] bufferIds = new int[releasedCount];
                        AL.SourceUnqueueBuffers(sourceId, releasedCount, bufferIds);
                        AL.DeleteBuffers(bufferIds);
                    }

                    var next = await _queue.Reader.ReadAsync(token);
                    var buffer = AL.GenBuffer();
                    AL.BufferData(buffer, targetFormat, next, sampleRate);
                    AL.SourceQueueBuffer(sourceId, buffer);
                }
            },
            token
        );

        await Task.Delay(2000);

        AL.SourcePlay(sourceId);
    }
}
