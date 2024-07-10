using NAudio.Wave;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;

namespace Console;

enum State
{
    Playing,
    Paused,
    Stopped
}

public class PlayerController : IDisposable
{
    private float _volume = 0.5f;
    private Queue<Video> _queue = new();
    private Video? _currentSong = null;
    private MediaFoundationReader? audioStream = null;
    private WaveOutEvent? outputDevice = null;
    private readonly YoutubeClient youtubeClient = new();

    public event Action<Video>? Playing;
    public int Volume => (int)(_volume * 100);
    public TimeSpan? Time => audioStream?.CurrentTime;
    public TimeSpan? TotalTime => audioStream?.TotalTime;

    public PlaybackState? State => outputDevice?.PlaybackState;

    public Video? Song => _currentSong;

    public void Dispose()
    {
        audioStream?.Dispose();
        outputDevice?.Dispose();
    }

    public async Task<List<VideoSearchResult>> Search(string query) =>
        await youtubeClient.Search.GetVideosAsync(query).Take(50).ToListAsync();

    public async Task AddAsync(VideoId id) =>
        _queue.Enqueue(await youtubeClient.Videos.GetAsync(id));

    public async Task PlayAsync()
    {
        if (outputDevice?.PlaybackState == PlaybackState.Playing)
        {
            return;
        }

        if (outputDevice?.PlaybackState == PlaybackState.Paused && Song is not null)
        {
            Playing?.Invoke(Song);
            outputDevice.Play();
            return;
        }

        var nextSong = _currentSong ?? _queue.TryGet();

        if (nextSong is null)
            return;

        _currentSong = nextSong;

        var stream = await youtubeClient.Videos.Streams.GetManifestAsync(nextSong.Id);
        var bestAudio = stream.GetAudioOnlyStreams().MaxBy(i => i.Bitrate);
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
            await PlayAsync();
        };

        Playing?.Invoke(nextSong);
    }

    public void Pause()
    {
        outputDevice?.Pause();
    }

    public void Stop()
    {
        outputDevice?.Stop();
    }
}
