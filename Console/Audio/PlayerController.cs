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

namespace Console.Audio;

public class PlayerController : IAsyncDisposable
{
    private readonly AsyncLock _lock = new();

    private float _volume = 0.5f;

    private Queue<Video> _queue = new();
    private Video? _currentSong = null;

    private readonly ALDevice _device;
    private readonly ALContext _context;
    private readonly int _sourceId;
    private readonly ALFormat _targetFormat;
    private Matroska? _matroskaPlayerBuffer = null;
    private AudioSender? _audioSender = null;

    private readonly YoutubeClient youtubeClient = new();

    public event Action? StateChanged;
    public event Action<IEnumerable<Video>>? QueueChanged;

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
    }

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
    public Video? Song => _currentSong;

    public async ValueTask DisposeAsync()
    {
        await Stop();
        ALC.DestroyContext(_context);
        ALC.CloseDevice(_device);
        AL.DeleteSource(_sourceId);
        if (_matroskaPlayerBuffer is not null)
            await _matroskaPlayerBuffer.DisposeAsync();
    }

    public async ValueTask Seek(TimeSpan time)
    {
        _audioSender?.ClearBuffer();
        if (_matroskaPlayerBuffer is not null)
            await _matroskaPlayerBuffer.Seek((long)time.TotalMilliseconds);
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

    private ALSourceState SourceState()
    {
        AL.GetSource(_sourceId, ALGetSourcei.SourceState, out int stateInt);
        return (ALSourceState)stateInt;
    }

    public async Task PlayAsync()
    {
        using var _ = await _lock.LockAsync();

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

        var nextSong = _currentSong ?? GetNextSong();

        if (nextSong is null)
            return;

        _currentSong = nextSong;

        _audioSender = new AudioSender(_sourceId, _targetFormat);
        _matroskaPlayerBuffer = await Matroska.Create(
            new YtDownloadUrlHandler(youtubeClient, nextSong.Id),
            _audioSender
        );

        var __ = Task.Run(() => _matroskaPlayerBuffer.AddFrames(CancellationToken.None));
        var ___ = Task.Run(() => _audioSender.StartSending());

        StateChanged?.Invoke();
    }

    public async Task SkipAsync()
    {
        using (await _lock.LockAsync())
        {
            AL.SourceStop(_sourceId);
            _currentSong = null;
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
