using System.Runtime.InteropServices;
using System.Threading.Channels;
using PortAudioSharp;

namespace Console.Audio;

internal class AudioSender : IAsyncDisposable
{
    private readonly Channel<PcmPacket<short>> _queue = Channel.CreateBounded<PcmPacket<short>>(
        150
    );
    public readonly int SampleRate = 48000;
    public readonly int Channels = 2;
    public PlayState State { get; set; } = PlayState.Stopped;
    private PortAudioSharp.Stream? _stream;
    private float _volume = 1.0f; // Default volume

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
            if (token.IsCancellationRequested)
                return StreamCallbackResult.Abort;

            if (State == PlayState.Paused)
            {
                var spanUnmanagedBuffer = new Span<short>(
                    output.ToPointer(),
                    (int)(frameCount * 2)
                );
                spanUnmanagedBuffer.Clear();
                return StreamCallbackResult.Continue;
            }

            if (State == PlayState.Stopped)
                return StreamCallbackResult.Abort;
            else if (_queue.Reader.TryRead(out var nextBuffer))
            {
                using var buffer = nextBuffer;
                var sizeInBytes = (int)frameCount * 2;
                var spanUnmanagedBuffer = new Span<short>(output.ToPointer(), sizeInBytes);
                buffer.Data[..sizeInBytes].CopyTo(spanUnmanagedBuffer);
            }
            else
            {
                var spanUnmanagedBuffer = new Span<short>(
                    output.ToPointer(),
                    (int)(frameCount * 2)
                );
                spanUnmanagedBuffer.Clear();
            }

            return StreamCallbackResult.Continue;
        }

        StreamParameters param = new();
        var deviceIndex = PortAudio.DefaultOutputDevice;
        var info = PortAudio.GetDeviceInfo(deviceIndex);
        param.device = PortAudio.DefaultOutputDevice;
        param.channelCount = Channels;
        param.sampleFormat = SampleFormat.Int16;
        param.suggestedLatency = info.defaultLowOutputLatency;
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
        _stream?.Dispose();
        return ValueTask.CompletedTask;
    }
}
