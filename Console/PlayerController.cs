using System;
using System.Reflection.PortableExecutable;
using Concentus;
using Concentus.Structs;
using Console.Buffers;
using Console.Containers.Matroska;
using Console.DownloadHandlers;
using Console.Extensions;
using NAudio.Wave;
using Nito.AsyncEx;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace Console;

enum State
{
    Playing,
    Paused,
    Stopped
}

public class PlayerController : IDisposable
{
    private readonly AsyncLock _lock = new();

    private float _volume = 0.5f;

    private Queue<Video> _queue = new();
    private Video? _currentSong = null;
    private WaveStream? audioStream = null;
    private WaveOutEvent? outputDevice = null;
    private readonly YoutubeClient youtubeClient = new();

    public event Action? StateChanged;
    public event Action<IEnumerable<Video>>? QueueChanged;

    public int Volume
    {
        get { return (int)(_volume * 100); }
        set
        {
            if (value is < 0 or > 100)
                return;

            _volume = value / 100f;
            if (outputDevice is not null)
            {
                outputDevice.Volume = _volume;
            }
        }
    }

    public TimeSpan? Time => audioStream?.CurrentTime;
    public TimeSpan? TotalTime => audioStream?.TotalTime;
    public PlaybackState? State => outputDevice?.PlaybackState;
    public Video? Song => _currentSong;

    /// <summary>
    /// Converts linear short samples into interleaved byte samples, for writing to a file, waveout device, etc.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
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

    public void Dispose()
    {
        audioStream?.Dispose();
        outputDevice?.Dispose();
    }

    public void Seek(TimeSpan time)
    {
        if (audioStream is null)
            return;
        if (outputDevice is null)
            return;

        audioStream.Seek(
            (long)(outputDevice.OutputWaveFormat.AverageBytesPerSecond * time.TotalSeconds),
            SeekOrigin.Begin
        );
    }

    public async Task<List<VideoSearchResult>> Search(string query) =>
        await youtubeClient.Search.GetVideosAsync(query).Take(50).ToListAsync();

    public async Task AddAsync(VideoId id)
    {
        using var _ = await _lock.LockAsync();
        _queue.Enqueue(await youtubeClient.Videos.GetAsync(id));
        QueueChanged?.Invoke(_queue);
    }

    private Video? GetNextSong()
    {
        var next = _queue.TryGet();

        if (next is not null)
        {
            QueueChanged?.Invoke(_queue);
        }

        return next;
    }

    public async Task PlayAsync()
    {
        using var _ = await _lock.LockAsync();

        if (outputDevice?.PlaybackState == PlaybackState.Playing)
        {
            return;
        }

        if (outputDevice?.PlaybackState == PlaybackState.Paused && Song is not null)
        {
            StateChanged?.Invoke();
            outputDevice.Play();
            return;
        }

        var nextSong = _currentSong ?? GetNextSong();

        if (nextSong is null)
            return;

        _currentSong = nextSong;

        Dispose();

        var urlHandler = new YtDownloadUrlHandler(youtubeClient, nextSong.Id);
        var matroskaBuffer = await MatroskaPlayerBuffer.Create(urlHandler);
        var stream = new OpusToPCMStream(matroskaBuffer);

        var frames = matroskaBuffer.GetFrames(CancellationToken.None);

        int sampleRate = 48000;
        int channels = 2;

        IOpusDecoder _decoder = OpusCodecFactory.CreateDecoder(sampleRate, channels);

        // Decode Opus data

        var decodedSamples = (
            await frames
                .Select(frame =>
                {
                    var data = frame.ToArray();
                    var frames = OpusPacketInfo.GetNumFrames(data);
                    var samplePerFrame = OpusPacketInfo.GetNumSamplesPerFrame(data, sampleRate);
                    var frameSize = frames * samplePerFrame;
                    short[] pcm = new short[frameSize * channels];
                    _decoder.Decode(data, pcm, frameSize, false);
                    byte[] buffer = ShortsToBytes(pcm, 0, pcm.Length);
                    Buffer.BlockCopy(pcm, 0, buffer, 0, buffer.Length);
                    return buffer;
                })
                .ToListAsync()
        ).SelectMany(i => i).ToArray();

        var stream = new MemoryStream(decodedSamples) { Position = 0 };
        outputDevice = new WaveOutEvent() { Volume = _volume };
        audioStream = new RawSourceWaveStream(stream, new WaveFormat(sampleRate, channels));

        outputDevice.Init(audioStream);
        outputDevice.Play();

        outputDevice.PlaybackStopped += async (_, _) =>
        {
            _currentSong = null;
            StateChanged?.Invoke();
            await PlayAsync();
        };

        StateChanged?.Invoke();
    }

    public async Task SkipAsync()
    {
        using (await _lock.LockAsync())
        {
            outputDevice?.Stop();
            _currentSong = null;
        }
    }

    public async Task Pause()
    {
        using var _ = await _lock.LockAsync();
        outputDevice?.Pause();
    }

    public async Task Stop()
    {
        using var _ = await _lock.LockAsync();
        outputDevice?.Stop();
    }
}
