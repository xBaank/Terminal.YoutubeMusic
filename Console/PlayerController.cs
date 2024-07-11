using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Threading;
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
    private MediaFoundationReader? audioStream = null;
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
    public TimeSpan? TotalTime => audioStream?.TotalTime ?? _currentSong?.Duration;
    public PlaybackState? State => outputDevice?.PlaybackState;
    public Video? Song => _currentSong;

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

        audioStream.Position = (long)(
            outputDevice.OutputWaveFormat.AverageBytesPerSecond * time.TotalSeconds
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

        var stream = await youtubeClient.Videos.Streams.GetManifestAsync(nextSong.Id);
        var bestAudio = stream.GetAudioOnlyStreams().GetWithHighestBitrate();
        if (bestAudio is null)
            return;

        Dispose();

        audioStream = new MediaFoundationReader(bestAudio.Url);
        outputDevice = new WaveOutEvent() { Volume = _volume };
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
