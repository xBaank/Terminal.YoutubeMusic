using Console.Audio.Containers.Matroska;
using Console.Audio.DownloadHandlers;
using Nito.AsyncEx;
using Nito.Disposables.Internals;
using OpenTK.Audio.OpenAL;
using Terminal.Gui;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;

namespace Console.Audio;

public class PlayerController : IAsyncDisposable
{
    private readonly AsyncLock _lock = new();

    private float _volume = 0.5f;

    private List<IVideo> _queue = [];
    private int _currentSongIndex = 0;

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
            AL.Source(_sourceId, ALSourcef.Gain, _volume);
        }
    }

    public TimeSpan? Time => _matroskaPlayerBuffer?.CurrentTime;
    public TimeSpan? TotalTime => _matroskaPlayerBuffer?.TotalTime ?? Song?.Duration;
    public ALSourceState? State => SourceState();
    public IVideo? Song => _queue.ElementAtOrDefault(_currentSongIndex);
    public IReadOnlyCollection<IVideo> Songs => _queue;
    public LoopState LoopState { get; set; }

    public PlayerController()
    {
        _device = ALC.OpenDevice(Environment.GetEnvironmentVariable("DeviceName"));
        _context = ALC.CreateContext(_device, new ALContextAttributes());
        ALC.MakeContextCurrent(_context);

        var error = ALC.GetError(_device);
        // Check for any errors
        if (error != AlcError.NoError)
        {
            throw new Exception($"Error code: {error}");
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

        await StopAsync().ConfigureAwait(false);
        ALC.DestroyContext(_context);
        ALC.CloseDevice(_device);
        AL.DeleteSource(_sourceId);

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
        await youtubeClient
            .Search.GetResultsAsync(query, token)
            .Take(50)
            .ToListAsync(cancellationToken: token);

    public async Task<List<Recommendation>> GetRecommendationsAsync() =>
        await youtubeClient.Search.GetRecommendationsAsync().ToListAsync();

    public async Task SkipToAsync(IVideo video)
    {
        using var _ = await _lock.LockAsync();
        AL.SourceStop(_sourceId);
        _audioSender?.ClearBuffer();
        _currentSongTokenSource.Cancel();
        _currentSongIndex = _queue.IndexOf(video);
    }

    public async Task SetAsync(Recommendation recommendation)
    {
        using var _ = await _lock.LockAsync();

        AL.SourceStop(_sourceId);
        _audioSender?.ClearBuffer();
        _currentSongTokenSource.Cancel();
        _currentSongIndex = 0;

        var firstVideo = recommendation.VideoId is not null
            ? await youtubeClient.Videos.GetAsync(recommendation.VideoId.Value)
            : null;

        var playlist = await youtubeClient
            .Playlists.GetVideosAsync(recommendation.PlaylistId)
            .ToListAsync();

        _queue = [firstVideo, .. playlist];
        _queue = _queue.WhereNotNull().DistinctBy(i => i.Id).ToList(); //Remove duplicate videos
        QueueChanged?.Invoke(_queue);
    }

    public async Task SetAsync(ISearchResult item)
    {
        using var _ = await _lock.LockAsync();

        AL.SourceStop(_sourceId);
        _audioSender?.ClearBuffer();
        _currentSongTokenSource.Cancel();
        _currentSongIndex = 0;

        if (item is VideoSearchResult videoSearchResult)
        {
            _queue = [videoSearchResult];
        }

        if (item is PlaylistSearchResult playlistSearchResult)
        {
            var videos = await youtubeClient
                .Playlists.GetVideosAsync(playlistSearchResult.Id)
                .ToListAsync<IVideo>();

            _queue = videos;
        }

        if (item is ChannelSearchResult channelSearchResult)
        {
            var videos = await youtubeClient
                .Channels.GetUploadsAsync(channelSearchResult.Id)
                .ToListAsync<IVideo>();

            _queue = videos;
        }

        QueueChanged?.Invoke(_queue);
    }

    private ALSourceState SourceState()
    {
        AL.GetSource(_sourceId, ALGetSourcei.SourceState, out int stateInt);
        return (ALSourceState)stateInt;
    }

    public async Task PlayAsync()
    {
        using var _l = await _lock.LockAsync();

        if (Song is null)
            return;

        if (SourceState() == ALSourceState.Playing)
        {
            return;
        }

        if (SourceState() == ALSourceState.Paused)
        {
            StateChanged?.Invoke();
            AL.SourcePlay(_sourceId);
            return;
        }

        if (_audioSender is not null)
            await _audioSender.DisposeAsync();

        if (_matroskaPlayerBuffer is not null)
            await _matroskaPlayerBuffer.DisposeAsync();

        _currentSongTokenSource = new CancellationTokenSource();

        _audioSender = new AudioSender(_sourceId, _targetFormat);

        try
        {
            _matroskaPlayerBuffer = await Matroska.Create(
                new YtDownloadUrlHandler(youtubeClient, Song.Id),
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
            AL.SourceStop(_sourceId);
        }
    }

    public async Task GoBackAsync()
    {
        _currentSongTokenSource.Cancel();

        using (await _lock.LockAsync())
        {
            if (_currentSongIndex > 0)
                _currentSongIndex--;
            AL.SourceStop(_sourceId);
            _audioSender?.ClearBuffer();
        }
    }

    public async Task PauseAsync()
    {
        using var _ = await _lock.LockAsync();
        AL.SourcePause(_sourceId);
    }

    public async Task StopAsync()
    {
        using var _ = await _lock.LockAsync();
        AL.SourceStop(_sourceId);
    }
}
