using System.Buffers;
using System.Runtime.CompilerServices;
using Console.Containers.Matroska;
using Console.Containers.Matroska.EBML;
using Console.Containers.Matroska.Elements;
using Console.Containers.Matroska.Extensions.EbmlExtensions;
using Console.Containers.Matroska.Extensions.MatroskaExtensions;
using Console.Containers.Matroska.Parsers;
using Console.Containers.Matroska.Types;
using Console.Extensions;
using DiscordBot.MusicPlayer.DownloadHandlers;

namespace Console.Buffers;

using static ElementTypes;

internal class MatroskaPlayerBuffer
{
    private readonly EbmlReader _ebmlReader;

    public readonly Stream InputStream;
    private List<AudioTrack>? _audioTracks;
    private List<CuePoint>? _cuePoints;
    private IMemoryOwner<byte> _memoryOwner = MemoryPool<byte>.Shared.Rent(1024);

    private long _seekTime;
    private CancellationTokenSource _seekToken;

    private MatroskaPlayerBuffer(Stream stream)
    {
        InputStream = stream;
        _ebmlReader = new EbmlReader(stream);
        _seekToken = new CancellationTokenSource();
    }

    public void Dispose()
    {
        _ebmlReader.Dispose();
        InputStream.Dispose();
        _memoryOwner.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _ebmlReader.DisposeAsync();
        await InputStream.DisposeAsync();
        _memoryOwner.Dispose();
    }

    public TimeSpan CurrentTime { get; private set; } = TimeSpan.Zero;

    public TimeSpan TotalTime { get; private set; }

    public async IAsyncEnumerable<ReadOnlyMemory<byte>> GetFrames(
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
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

        foreach (var cue in clusters)
        {
            await foreach (var i in GetCluster(cue.Cuetrack.ClusterPosition, _seekToken.Token))
                yield return i;
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

    private async IAsyncEnumerable<ReadOnlyMemory<byte>> GetCluster(
        long pos,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
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

            await foreach (var i in GetBlocks(matroskaElement, time!.Value, cancellationToken))
                yield return i;
        }
    }

    private async IAsyncEnumerable<ReadOnlyMemory<byte>> GetBlocks(
        MatroskaElement matroskaElement,
        TimeSpan time,
        [EnumeratorCancellation] CancellationToken cancellationToken
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
                yield break;

            foreach (var frame in block.GetFrames())
                yield return frame;

            yield break;
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

    public static async ValueTask<MatroskaPlayerBuffer> Create(
        IDownloadUrlHandler downloadUrlHandler,
        CancellationToken token = default
    )
    {
        var stream = await HttpSegmentedStream.Create(downloadUrlHandler, bufferSize: 1024 * 10);
        stream.CompletionOption = HttpCompletionOption.ResponseContentRead;

        var playerBuffer = new MatroskaPlayerBuffer(stream);
        await playerBuffer.LoadFileInfo(token);

        stream.BufferSize = 9_898_989;
        stream.CompletionOption = HttpCompletionOption.ResponseHeadersRead;
        return playerBuffer;
    }
}
