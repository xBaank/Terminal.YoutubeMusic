using System.Threading.Channels;
using PortAudioSharp;

namespace Console.Audio;

internal class AudioSender(float volume, PlayState initialState) : IAsyncDisposable
{
    private readonly Channel<PcmPacket<short>> _queue = Channel.CreateBounded<PcmPacket<short>>(
        500
    );
    public readonly int SampleRate = 48000;
    public readonly int Channels = 2;
    public PlayState State { get; set; } = initialState;
    public float Volume { get; set; } = volume;
    public TimeSpan CurrentTime { get; private set; } = default;
    public TaskCompletionSource WaitForEmptyBuffer { get; set; } = new();
    private PortAudioSharp.Stream? _stream;

    public void ClearBuffer()
    {
        while (_queue.Reader.TryRead(out var next))
        {
            next.Dispose();
        }
    }

    public async ValueTask Add(PcmPacket<short> data) => await _queue.Writer.WriteAsync(data);

    public unsafe void StartSending(CancellationToken token = default)
    {
        PortAudio.Initialize();

        // Define a callback delegate for audio processing
        StreamCallbackResult callback(
            IntPtr input,
            IntPtr output,
            UInt32 frameCount,
            ref StreamCallbackTimeInfo timeInfo,
            StreamCallbackFlags statusFlags,
            IntPtr userData
        )
        {
            var sizeInBytes = (int)frameCount * 2;

            if (token.IsCancellationRequested)
                return StreamCallbackResult.Abort;

            if (State == PlayState.Paused)
            {
                var spanUnmanagedBuffer = new Span<short>(output.ToPointer(), sizeInBytes);
                spanUnmanagedBuffer.Clear();
                return StreamCallbackResult.Continue;
            }

            if (State == PlayState.Stopped)
                return StreamCallbackResult.Abort;

            if (_queue.Reader.TryRead(out var nextBuffer))
            {
                using var buffer = nextBuffer;
                var spanUnmanagedBuffer = new Span<short>(output.ToPointer(), sizeInBytes);
                var source = buffer.Data[..sizeInBytes];

                for (var i = 0; i < source.Length; i++)
                {
                    source[i] = (short)(source[i] * Volume);
                }

                source.CopyTo(spanUnmanagedBuffer);
                CurrentTime = buffer.Time;
                return StreamCallbackResult.Continue;
            }
            else
            {
                WaitForEmptyBuffer.TrySetResult();
                var spanUnmanagedBuffer = new Span<short>(output.ToPointer(), sizeInBytes);
                spanUnmanagedBuffer.Clear();
                return StreamCallbackResult.Continue;
            }
        }

        StreamParameters param = new();
        var deviceIndex = PortAudio.DefaultOutputDevice;
        var info = PortAudio.GetDeviceInfo(deviceIndex);
        param.device = PortAudio.DefaultOutputDevice;
        param.channelCount = Channels;
        param.sampleFormat = SampleFormat.Int16;
        param.suggestedLatency = info.defaultHighOutputLatency;
        param.hostApiSpecificStreamInfo = IntPtr.Zero;

        _stream = new PortAudioSharp.Stream(
            inParams: null,
            outParams: param,
            streamFlags: StreamFlags.ClipOff,
            sampleRate: SampleRate,
            framesPerBuffer: 960, //TODO This should not be hardcoded maybe?
            callback: callback,
            userData: IntPtr.Zero
        );

        _stream.Start();
    }

    public ValueTask DisposeAsync()
    {
        ClearBuffer();
        State = PlayState.Stopped;
        try
        {
            _stream?.Stop();
            _stream?.Dispose();
        }
        catch { }
        return ValueTask.CompletedTask;
    }
}
