using System;
using System.IO;
using System.Text;
using Concentus;
using Concentus.Structs;
using Console.Buffers;
using Console.Containers.Matroska;
using Console.DownloadHandlers;
using Console.Extensions;
using NAudio.Wave;
using Nito.AsyncEx;
using OpenTK.Audio.OpenAL;
using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos;

namespace Console.Audio;

enum State
{
    Playing,
    Paused,
    Stopped
}

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

    private const int sampleRate = 48000; // Adjust this to match your actual sample rate
    private const int channels = 2;
    private readonly IOpusDecoder _decoder = OpusCodecFactory.CreateDecoder(sampleRate, channels);

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

    public TimeSpan? Time => default;
    public TimeSpan? TotalTime => _currentSong?.Duration;
    public ALSourceState? State => SourceState();
    public Video? Song => _currentSong;

    public async ValueTask DisposeAsync()
    {
        await Stop();
        ALC.DestroyContext(_context);
        ALC.CloseDevice(_device);
        AL.DeleteSource(_sourceId);
    }

    public void Seek(TimeSpan time) { }

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

        var urlHandler = new YtDownloadUrlHandler(youtubeClient, nextSong.Id);
        var matroskaBuffer = await MatroskaPlayerBuffer.Create(urlHandler);

        //Go back with matroskaBuffer writing to a stream, the stream should have a channel for 50 packets or so.
        //if its filled then we await in writing, at the same time when writing it should be decoded from opus.

        var task = Task.Run(async () =>
        {
            await matroskaBuffer
                .GetFrames(CancellationToken.None)
                .Select(i => i.ToArray())
                .BatchAsync(50)
                .ForEachAsync(dataList =>
                {
                    foreach (var data in dataList)
                    {
                        var frames = OpusPacketInfo.GetNumFrames(data);
                        var samplePerFrame = OpusPacketInfo.GetNumSamplesPerFrame(data, sampleRate);
                        var frameSize = frames * samplePerFrame;
                        short[] pcm = new short[frameSize * channels];
                        int decodedSamples = _decoder.Decode(data, pcm, frameSize);
                        var result = ShortsToBytes(pcm, 0, pcm.Length);

                        var bufferId = AL.GenBuffer();
                        AL.BufferData(bufferId, _targetFormat, result, sampleRate);
                        AL.SourceQueueBuffer(_sourceId, bufferId);
                    }
                });
        });

        var task2 = Task.Run(async () =>
        {
            while (true)
            {
                AL.GetSource(_sourceId, ALGetSourcei.BuffersProcessed, out int releasedCount);

                if (releasedCount > 0)
                {
                    int[] bufferIds = new int[releasedCount];
                    AL.SourceUnqueueBuffers(_sourceId, releasedCount, bufferIds);
                    AL.DeleteBuffers(bufferIds);
                }

                await Task.Delay(100);
            }
        });

        await Task.Delay(2000);

        AL.SourcePlay(_sourceId);
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
