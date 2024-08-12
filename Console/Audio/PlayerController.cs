using Console.Audio.Containers.Matroska;
using Console.Audio.DownloadHandlers;
using Nito.AsyncEx;
using Nito.Disposables.Internals;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;

namespace Console.Audio;

internal class PlayerController : IAsyncDisposable
{
    private readonly AsyncLock _lock = new();

    private float _volume = 0.5f;

    private List<IVideo> _queue = [];
    private int _currentSongIndex = 0;

    private readonly YoutubeClient _youtubeClient;
    private Matroska? _matroskaPlayerBuffer = null;
    private AudioSender? _audioSender = null;
    private CancellationTokenSource _currentSongTokenSource = new();
    private bool _disposed = false;

    public event Action? StateChanged;
    public event Action<IEnumerable<IVideo>>? QueueChanged; //Maybe emit state to show a loading spinner
    public event Action? OnFinish;

    public int Volume
    {
        get { return (int)(_volume * 100); }
        set
        {
            if (value is < 0 or > 100)
                return;

            _volume = value / 100f;
        }
    }

    public TimeSpan? Time => _matroskaPlayerBuffer?.CurrentTime;
    public TimeSpan? TotalTime => _matroskaPlayerBuffer?.TotalTime ?? Song?.Duration;
    public PlayState State
    {
        get { return _audioSender?.State ?? PlayState.Stopped; }
        set
        {
            if (_audioSender is not null)
                _audioSender.State = (PlayState)value;
        }
    }

    public IVideo? Song => _queue.ElementAtOrDefault(_currentSongIndex);
    public IReadOnlyCollection<IVideo> Songs => _queue;
    public LoopState LoopState { get; set; }

    public PlayerController(YoutubeClient youtubeClient)
    {
        _youtubeClient = youtubeClient;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await StopAsync().ConfigureAwait(false);

        if (_matroskaPlayerBuffer is not null)
        {
            await _matroskaPlayerBuffer.DisposeAsync().ConfigureAwait(false);
        }

        if (_audioSender is not null)
        {
            await _audioSender.DisposeAsync().ConfigureAwait(false);
        }

        // Suppress finalization
        GC.SuppressFinalize(this);
    }

    public async ValueTask SeekAsync(TimeSpan time)
    {
        using var _ = await _lock.LockAsync();
        _audioSender?.ClearBuffer();
        if (_matroskaPlayerBuffer is not null)
            await _matroskaPlayerBuffer.Seek((long)time.TotalMilliseconds);
    }

    public async Task<List<ISearchResult>> SearchAsync(
        string query,
        CancellationToken token = default
    ) =>
        await _youtubeClient
            .Search.GetResultsAsync(query, token)
            .Take(50)
            .ToListAsync(cancellationToken: token);

    public async Task<List<Recommendation>> GetRecommendationsAsync() =>
        await _youtubeClient.Search.GetRecommendationsAsync().ToListAsync();

    private void ResetState()
    {
        State = PlayState.Stopped;
        _audioSender?.ClearBuffer();
        _currentSongTokenSource.Cancel();
    }

    public async Task SkipToAsync(IVideo video)
    {
        using var _ = await _lock.LockAsync();

        ResetState();
        _currentSongIndex = _queue.IndexOf(video);
    }

    public async Task SetAsync(
        IReadOnlyCollection<IVideo> videos,
        CancellationToken cancellationToken = default
    )
    {
        using var _ = await _lock.LockAsync(cancellationToken);

        ResetState();
        _currentSongIndex = 0;

        _queue = [.. videos];
        QueueChanged?.Invoke(_queue);
    }

    public async Task SetAsync(
        Recommendation recommendation,
        CancellationToken cancellationToken = default
    )
    {
        using var _ = await _lock.LockAsync(cancellationToken);

        ResetState();
        _currentSongIndex = 0;

        var firstVideo = recommendation.VideoId is not null
            ? await _youtubeClient.Videos.GetAsync(recommendation.VideoId.Value, cancellationToken)
            : null;

        var playlist = await _youtubeClient
            .Playlists.GetVideosAsync(recommendation.PlaylistId, cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);

        _queue = [firstVideo, .. playlist];
        _queue = _queue.WhereNotNull().DistinctBy(i => i.Id).ToList(); //Remove duplicate videos
        QueueChanged?.Invoke(_queue);
    }

    public async Task SetAsync(ISearchResult item, CancellationToken cancellationToken = default)
    {
        using var _ = await _lock.LockAsync(cancellationToken);

        ResetState();
        _currentSongIndex = 0;

        if (item is VideoSearchResult videoSearchResult)
        {
            _queue = [videoSearchResult];
        }

        if (item is PlaylistSearchResult playlistSearchResult)
        {
            var videos = await _youtubeClient
                .Playlists.GetVideosAsync(playlistSearchResult.Id, cancellationToken)
                .ToListAsync<IVideo>(cancellationToken: cancellationToken);

            _queue = videos;
        }

        if (item is ChannelSearchResult channelSearchResult)
        {
            var videos = await _youtubeClient
                .Channels.GetUploadsAsync(channelSearchResult.Id, cancellationToken)
                .ToListAsync<IVideo>(cancellationToken: cancellationToken);

            _queue = videos;
        }

        QueueChanged?.Invoke(_queue);
    }

    public async Task PlayAsync()
    {
        using var _l = await _lock.LockAsync();

        if (Song is null)
            return;

        if (State == PlayState.Playing)
        {
            return;
        }

        if (State == PlayState.Paused)
        {
            StateChanged?.Invoke();
            State = PlayState.Playing;
            return;
        }

        if (_audioSender is not null)
            await _audioSender.DisposeAsync();

        if (_matroskaPlayerBuffer is not null)
            await _matroskaPlayerBuffer.DisposeAsync();

        _currentSongTokenSource = new CancellationTokenSource();
        _audioSender = new AudioSender();
        State = PlayState.Playing;

        try
        {
            _matroskaPlayerBuffer = await Matroska.Create(
                new YtDownloadUrlHandler(_youtubeClient, Song.Id),
                _audioSender,
                _currentSongTokenSource.Token
            );

            _matroskaPlayerBuffer.OnFinish += async () =>
            {
                _currentSongTokenSource.Cancel();
                await _audioSender.DisposeAsync();
                OnFinish?.Invoke();
            };

            _ = Task.Run(() => _matroskaPlayerBuffer.AddFrames(_currentSongTokenSource.Token));
            _ = Task.Run(() => _audioSender.StartSending(_currentSongTokenSource.Token));

            StateChanged?.Invoke();
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            //If there is any error when loading just skip
            //This could happen if the video is too old and there is no opus support
            _currentSongTokenSource.Cancel();
            await _audioSender.DisposeAsync();
            OnFinish?.Invoke();
        }
    }

    public async Task SkipAsync(bool bypassLoop = false)
    {
        _currentSongTokenSource.Cancel();

        using (await _lock.LockAsync())
        {
            if (
                (bypassLoop || LoopState is LoopState.OFF or LoopState.ALL)
                && _currentSongIndex <= _queue.Count
            )
                _currentSongIndex++;

            if (LoopState == LoopState.ALL && _currentSongIndex >= _queue.Count)
                _currentSongIndex = 0;

            _audioSender?.ClearBuffer();
            State = PlayState.Stopped;
        }
    }

    public async Task GoBackAsync()
    {
        _currentSongTokenSource.Cancel();

        using (await _lock.LockAsync())
        {
            if (_currentSongIndex > 0)
                _currentSongIndex--;
            State = PlayState.Stopped;

            _audioSender?.ClearBuffer();
        }
    }

    public async Task PauseAsync()
    {
        using var _ = await _lock.LockAsync();
        State = PlayState.Paused;
    }

    public async Task StopAsync()
    {
        using var _ = await _lock.LockAsync();
        State = PlayState.Stopped;
    }
}
