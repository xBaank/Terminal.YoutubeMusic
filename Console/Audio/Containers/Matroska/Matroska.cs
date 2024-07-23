using System.Buffers;
using Concentus;
using Concentus.Structs;
using Console.Audio.Containers.Matroska.EBML;
using Console.Audio.Containers.Matroska.Elements;
using Console.Audio.Containers.Matroska.Extensions.EbmlExtensions;
using Console.Audio.Containers.Matroska.Extensions.MatroskaExtensions;
using Console.Audio.Containers.Matroska.Parsers;
using Console.Audio.Containers.Matroska.Types;
using Console.Audio.DownloadHandlers;
using Console.Extensions;
using Nito.Disposables;

namespace Console.Audio.Containers.Matroska;

using static ElementTypes;

internal class Matroska : IDisposable, IAsyncDisposable
{
    private readonly EbmlReader _ebmlReader;
    private readonly AudioSender _sender;
    private readonly Stream _inputStream;
    private readonly IOpusDecoder _decoder;
    private List<AudioTrack>? _audioTracks;
    private List<CuePoint>? _cuePoints;
    private IMemoryOwner<byte> _memoryOwner = MemoryPool<byte>.Shared.Rent(1024);
    private bool _hasFinishEventFired = false;
    private long _seekTime;
    private CancellationTokenSource _seekToken;

    public event Action? OnFinish;

    private Matroska(Stream stream, AudioSender sender)
    {
        _inputStream = stream;
        _ebmlReader = new EbmlReader(stream);
        _sender = sender;
        _decoder = OpusCodecFactory.CreateDecoder(_sender.SampleRate, _sender.Channels);
        _seekToken = new CancellationTokenSource();
    }

    public void Dispose()
    {
        _ebmlReader.Dispose();
        _inputStream.Dispose();
        _memoryOwner.Dispose();
        CurrentTime = default;
        TotalTime = default;
    }

    public async ValueTask DisposeAsync()
    {
        await _ebmlReader.DisposeAsync();
        await _inputStream.DisposeAsync();
        _memoryOwner.Dispose();
        CurrentTime = default;
        TotalTime = default;
    }

    public TimeSpan CurrentTime { get; private set; } = TimeSpan.Zero;
    public TimeSpan TotalTime { get; private set; }

    public async Task AddFrames(CancellationToken cancellationToken)
    {
        if (_cuePoints is null)
            throw new Exception("No cues found");

        if (_audioTracks is null)
            throw new Exception("No audio tracks found");

        var cuePointToSeekFrom = _cuePoints
            .OrderByDescending(i => i.Time)
            .First(i => i.Time <= _seekTime);
        var cues = _cuePoints.Where(i => i.Time >= cuePointToSeekFrom.Time);
        var clusters = cues.Select(cuePoint => new
        {
            Cuetrack = cuePoint.TrackPositions.First(i => i.Track == _audioTracks[0].Number),
            CueTime = cuePoint.Time
        });

        _seekToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            foreach (var cue in clusters)
                await WriteCluster(cue.Cuetrack.ClusterPosition, _seekToken.Token);
        }
        catch (OperationCanceledException)
        {
            //If only seek was canceled, we play again from the requested time
            //Maybe recursion is not the best way to do this ?
            if (!cancellationToken.IsCancellationRequested)
                await AddFrames(cancellationToken);
            else
                throw;
        }

        if (!_hasFinishEventFired)
        {
            OnFinish?.Invoke();
            _hasFinishEventFired = true;
        }
    }

    public ValueTask<bool> Seek(long timeStamp)
    {
        if (timeStamp > TotalTime.TotalMilliseconds || timeStamp < 0)
            return ValueTask.FromResult(false);

        _seekTime = timeStamp;
        _seekToken.Cancel();
        return ValueTask.FromResult(true);
    }

    private async ValueTask WriteCluster(long pos, CancellationToken cancellationToken)
    {
        var cluster =
            await _ebmlReader.Read(pos, cancellationToken).PipeAsync(i => i.As(Cluster))
            ?? throw new Exception("Cluster not found");

        TimeSpan? time = null;

        await foreach (var matroskaElement in _ebmlReader.ReadAll(cluster.Size, cancellationToken))
        {
            //TODO use timescale here
            if (matroskaElement.Id == Timestamp.Id)
            {
                time = await _ebmlReader
                    .TryReadUlong(matroskaElement, cancellationToken)
                    .PipeAsync(i => i ?? throw new Exception("Cluster time not found"))
                    .PipeAsync(i => TimeSpan.FromMilliseconds(i));

                continue;
            }

            await WriteBlock(
                matroskaElement,
                time ?? throw new Exception("TimeStamp was not the first element on the cluster"),
                cancellationToken
            );
        }
    }

    private static void ShortsToBytes(ReadOnlySpan<short> input, Span<byte> output)
    {
        for (int i = 0; i < input.Length; i++)
        {
            output[i * 2] = (byte)input[i];
            output[i * 2 + 1] = (byte)(input[i] >> 8);
        }
    }

    private async ValueTask AddOpusPacket(ReadOnlyMemory<byte> data)
    {
        var frames = OpusPacketInfo.GetNumFrames(data.Span);
        var samplePerFrame = OpusPacketInfo.GetNumSamplesPerFrame(data.Span, _sender.SampleRate);
        var frameSize = frames * samplePerFrame;
        var pcmSize = frameSize * _sender.Channels;

        var pcm = ArrayPool<short>.Shared.Rent(pcmSize);
        var pcmBytes = ArrayPool<byte>.Shared.Rent(pcmSize * 2);

        try
        {
            _decoder.Decode(data.Span, pcm.AsSpan()[..pcmSize], frameSize);
            ShortsToBytes(pcm.AsSpan()[..pcmSize], pcmBytes.AsSpan()[..(pcmSize * 2)]);
            await _sender.Add(new PcmPacket(pcmBytes, pcmSize * 2));
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(pcmBytes);
            throw;
        }
        finally
        {
            ArrayPool<short>.Shared.Return(pcm);
        }
    }

    private async ValueTask WriteBlock(
        MatroskaElement matroskaElement,
        TimeSpan time,
        CancellationToken cancellationToken
    )
    {
        if (matroskaElement.Id == SimpleBlock.Id)
        {
            var size = (int)matroskaElement.Size;

            if (_memoryOwner.Memory.Length < size)
            {
                _memoryOwner.Dispose();
                _memoryOwner = MemoryPool<byte>.Shared.Rent(size);
            }

            var memory = _memoryOwner.Memory[..size];

            var block = await _ebmlReader.GetSimpleBlock(memory, cancellationToken);
            CurrentTime = time + TimeSpan.FromMilliseconds(block.Timestamp);

            if (CurrentTime.TotalMilliseconds < _seekTime)
                return;

            foreach (var frame in block.GetFrames())
                await AddOpusPacket(frame);

            return;
        }

        //TODO handle blocks
        await _ebmlReader.Skip(matroskaElement.Size, cancellationToken);
    }

    private async Task LoadFileInfo(CancellationToken token)
    {
        var headerParser = new EbmlHeaderParser(_ebmlReader);

        var isValid = await headerParser
            .TryGetDocType(token)
            .PipeAsync(i => i ?? throw new Exception("Couldn't parse docType"))
            .PipeAsync(i => i is "webm" or "matroska");

        if (!isValid)
            throw new Exception("Invalid DocType file");

        var segmentparser = await SegmentParser.CreateAsync(_ebmlReader, token);

        _cuePoints = await segmentparser.GetCuePoints(token);
        _audioTracks = await segmentparser.GetAudioTracks(token);
        TotalTime = await segmentparser.GetDuration(token);
    }

    public static async ValueTask<Matroska> Create(
        IDownloadUrlHandler downloadUrlHandler,
        AudioSender sender,
        CancellationToken token = default
    )
    {
        var stream = await HttpSegmentedStream.Create(downloadUrlHandler);
        var playerBuffer = new Matroska(stream, sender);
        await playerBuffer.LoadFileInfo(token);

        return playerBuffer;
    }
}
