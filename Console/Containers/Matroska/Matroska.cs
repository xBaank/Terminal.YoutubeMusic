using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using Concentus;
using Concentus.Structs;
using Console.Audio;
using Console.Containers.Matroska.EBML;
using Console.Containers.Matroska.Elements;
using Console.Containers.Matroska.Extensions.EbmlExtensions;
using Console.Containers.Matroska.Extensions.MatroskaExtensions;
using Console.Containers.Matroska.Parsers;
using Console.Containers.Matroska.Types;
using Console.Extensions;
using DiscordBot.MusicPlayer.DownloadHandlers;
using Terminal.Gui;

namespace Console.Containers.Matroska;

using static ElementTypes;

internal class Matroska
{
    private readonly EbmlReader _ebmlReader;
    private readonly AudioSender _sender;
    public readonly Stream InputStream;
    private List<AudioTrack>? _audioTracks;
    private List<CuePoint>? _cuePoints;
    private IMemoryOwner<byte> _memoryOwner = MemoryPool<byte>.Shared.Rent(1024);

    private readonly IOpusDecoder _decoder;

    private long _seekTime;
    private CancellationTokenSource _seekToken;

    private Matroska(Stream stream, AudioSender sender)
    {
        InputStream = stream;
        _ebmlReader = new EbmlReader(stream);
        _sender = sender;
        _decoder = OpusCodecFactory.CreateDecoder(_sender.SampleRate, _sender.Channels);
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
    public bool HasFinished { get; private set; }

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

        HasFinished = true;
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

    private static byte[] ShortsToBytes(short[] input, int offset, int length)
    {
        byte[] processedValues = new byte[length * 2];
        for (int i = 0; i < length; i++)
        {
            processedValues[i * 2] = (byte)input[i + offset];
            processedValues[i * 2 + 1] = (byte)(input[i + offset] >> 8);
        }

        return processedValues;
    }

    private async ValueTask AddOpusPacket(byte[] data)
    {
        var frames = OpusPacketInfo.GetNumFrames(data);
        var samplePerFrame = OpusPacketInfo.GetNumSamplesPerFrame(data, _sender.SampleRate);
        var frameSize = frames * samplePerFrame;
        short[] pcm = new short[frameSize * _sender.Channels];
        _decoder.Decode(data, pcm, frameSize);
        var result = ShortsToBytes(pcm, 0, pcm.Length);
        await _sender.Add(result);
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
                await AddOpusPacket(frame.ToArray());

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

        stream.BufferSize = 9_898_989;
        stream.CompletionOption = HttpCompletionOption.ResponseHeadersRead;
        return playerBuffer;
    }
}
