using System.Data;
using Concentus;
using Concentus.Structs;
using Console.Containers.Matroska;
using Console.DownloadHandlers;
using Console.Extensions;
using Nito.AsyncEx;
using OpenTK.Audio.OpenAL;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;
using static Terminal.Gui.SpinnerStyle;

namespace Console.Audio;

public class PlayerController : IAsyncDisposable
{
    private readonly AsyncLock _lock = new();

    private float _volume = 0.5f;

    private Queue<IVideo> _queue = new();
    private IVideo? _currentSong = null;

    private readonly ALDevice _device;
    private readonly ALContext _context;
    private readonly int _sourceId;
    private readonly ALFormat _targetFormat;
    private Matroska? _matroskaPlayerBuffer = null;
    private AudioSender? _audioSender = null;
    private CancellationTokenSource _currentSongTokenSource = new();
    private bool _disposed = false;

    private readonly YoutubeClient youtubeClient = new();

    public event Action? StateChanged;
    public event Action<IEnumerable<IVideo>>? QueueChanged;
    public event Action? OnFinish;

    public int Volume
    {
        get { return (int)(_volume * 100); }
        set
        {
            if (value is < 0 or > 100)
                return;

            _volume = value / 100f;
            AL.Source(_sourceId, ALSourcef.Gain, _volume);
        }
    }

    public TimeSpan? Time => _matroskaPlayerBuffer?.CurrentTime;
    public TimeSpan? TotalTime => _matroskaPlayerBuffer?.TotalTime ?? _currentSong?.Duration;
    public ALSourceState? State => SourceState();
    public IVideo? Song => _currentSong;

    public PlayerController()
    {
        _device = ALC.OpenDevice("");
        _context = ALC.CreateContext(_device, new ALContextAttributes());
        ALC.MakeContextCurrent(_context);

        // Check for any errors
        if (ALC.GetError(_device) != AlcError.NoError)
        {
            System.Console.WriteLine("Error initializing OpenAL context");
            return;
        }

        _sourceId = AL.GenSource();
        _targetFormat = ALFormat.Stereo16;
        AL.Source(_sourceId, ALSourcef.Gain, _volume);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await Stop().ConfigureAwait(false);
        ALC.DestroyContext(_context);
        ALC.CloseDevice(_device);
        AL.DeleteSource(_sourceId);

        if (_matroskaPlayerBuffer is not null)
        {
            await _matroskaPlayerBuffer.DisposeAsync().ConfigureAwait(false);
        }

        // Suppress finalization
        GC.SuppressFinalize(this);
    }

    public async ValueTask Seek(TimeSpan time)
    {
        _audioSender?.ClearBuffer();
        if (_matroskaPlayerBuffer is not null)
            await _matroskaPlayerBuffer.Seek((long)time.TotalMilliseconds);
    }

    public async Task<List<ISearchResult>> Search(string query) =>
        await youtubeClient.Search.GetResultsAsync(query).Take(50).ToListAsync();

    public async Task AddAsync(ISearchResult item)
    {
        using var _ = await _lock.LockAsync();

        if (item is VideoSearchResult videoSearchResult)
        {
            _queue.Enqueue(videoSearchResult);
        }

        if (item is PlaylistSearchResult playlistSearchResult)
        {
            var videos = await youtubeClient
                .Playlists.GetVideosAsync(playlistSearchResult.Id)
                .ToListAsync();

            videos.ForEach(video => _queue.Enqueue(video));
        }

        if (item is ChannelSearchResult channelSearchResult)
        {
            var videos = await youtubeClient
                .Channels.GetUploadsAsync(channelSearchResult.Id)
                .ToListAsync();

            videos.ForEach(video => _queue.Enqueue(video));
        }

        QueueChanged?.Invoke(_queue);
    }

    private ValueTask<IVideo?> GetNextSong()
    {
        var next = _queue.TryGet();

        if (next is not null)
            QueueChanged?.Invoke(_queue);

        return ValueTask.FromResult(next);
    }

    private ALSourceState SourceState()
    {
        AL.GetSource(_sourceId, ALGetSourcei.SourceState, out int stateInt);
        return (ALSourceState)stateInt;
    }

    public async Task PlayAsync()
    {
        using var _l = await _lock.LockAsync();

        if (SourceState() == ALSourceState.Playing)
        {
            return;
        }

        if (SourceState() == ALSourceState.Paused && Song is not null)
        {
            StateChanged?.Invoke();
            AL.SourcePlay(_sourceId);
            return;
        }

        var nextSong = _currentSong ?? await GetNextSong();

        if (nextSong is null)
            return;

        _currentSong = nextSong;

        if (_matroskaPlayerBuffer is not null)
            await _matroskaPlayerBuffer.DisposeAsync();

        _currentSongTokenSource = new CancellationTokenSource();

        _audioSender = new AudioSender(_sourceId, _targetFormat);
        _matroskaPlayerBuffer = await Matroska.Create(
            new YtDownloadUrlHandler(youtubeClient, _currentSong.Id),
            _audioSender,
            _currentSongTokenSource.Token
        );

        _matroskaPlayerBuffer.OnFinish += OnFinish;

        _ = Task.Run(() => _matroskaPlayerBuffer.AddFrames(_currentSongTokenSource.Token));
        _ = Task.Run(() => _audioSender.StartSending(_currentSongTokenSource.Token));

        StateChanged?.Invoke();
    }

    public async Task SkipAsync()
    {
        using (await _lock.LockAsync())
        {
            AL.SourceStop(_sourceId);
            _currentSong = null;
            _audioSender?.ClearBuffer();
            _currentSongTokenSource.Cancel();
        }
    }

    public async Task Pause()
    {
        using var _ = await _lock.LockAsync();
        AL.SourcePause(_sourceId);
    }

    public async Task Stop()
    {
        using var _ = await _lock.LockAsync();
        AL.SourceStop(_sourceId);
    }
}
