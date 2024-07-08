using NAudio.Wave;
using YoutubeExplode;
using YoutubeExplode.Videos;

namespace Console;

enum State
{
    Playing,
    Paused,
    Stopped
}

public class Player(YoutubeClient youtubeClient) : IDisposable
{
    private Queue<Video> _queue = new();
    private Video? _currentSong = null;
    private State _state = State.Stopped;
    private MediaFoundationReader? audioStream = null;
    private WaveOutEvent? outputDevice = null;

    public Video? Song => _currentSong;

    public void Dispose()
    {
        audioStream?.Dispose();
        outputDevice?.Dispose();
    }

    public async Task AddAsync(VideoId id) =>
        _queue.Enqueue(await youtubeClient.Videos.GetAsync(id));

    public async Task PlayAsync()
    {
        var nextSong = _currentSong ?? _queue.TryGet();
        if (nextSong is null)
            return;

        _currentSong = nextSong;
        _state = State.Playing;

        var stream = await youtubeClient.Videos.Streams.GetManifestAsync(nextSong.Id);
        var bestAudio = stream.GetAudioOnlyStreams().MaxBy(i => i.Bitrate);
        if (bestAudio is null)
            return;

        Dispose();

        audioStream = new MediaFoundationReader(bestAudio.Url);
        outputDevice = new WaveOutEvent();
        outputDevice.Init(audioStream);
        outputDevice.Play();
    }

    public void Pause()
    {
        outputDevice?.Pause();
        _state = State.Paused;
    }

    public void Stop()
    {
        outputDevice?.Stop();
        _state = State.Stopped;
    }
}
