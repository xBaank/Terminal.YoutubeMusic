namespace Console.Audio.Containers.Matroska.Parsers;

using Console.Audio.Containers.Matroska.EBML;
using Console.Audio.Containers.Matroska.Elements;
using Console.Extensions;
using Extensions.MatroskaExtensions;
using static Types.ElementTypes;

internal class SegmentParser
{
    private readonly CuesParser _cuesParser;
    private readonly InfoParser _infoParser;
    private readonly EbmlReader _reader;
    private readonly SeekHeadParser _seekHeadParser;
    private readonly TracksParser _tracksParser;
    private long _seekHeadPosition;
    private TopElements? _topElements;

    private SegmentParser(EbmlReader reader)
    {
        _reader = reader;
        _cuesParser = new CuesParser(reader);
        _seekHeadParser = new SeekHeadParser(reader);
        _tracksParser = new TracksParser(reader);
        _infoParser = new InfoParser(reader);
    }

    private async Task ParseSegment(CancellationToken token)
    {
        _ =
            await _reader.Read(token).PipeAsync(i => i.As(Segment))
            ?? throw new Exception("Segment not found");

        var seekHead =
            await _reader.Read(token).PipeAsync(i => i.As(SeekHead))
            ?? throw new Exception("SeekHead not found");

        _seekHeadPosition = seekHead.Position;
    }

    public static async Task<SegmentParser> CreateAsync(
        EbmlReader ebmlReader,
        CancellationToken token
    )
    {
        var segmentParser = new SegmentParser(ebmlReader);
        await segmentParser.ParseSegment(token);
        return segmentParser;
    }

    public async Task<List<CuePoint>> GetCuePoints(CancellationToken token)
    {
        _topElements ??= await _seekHeadParser.ReadSeekHead(_seekHeadPosition, token);

        if (!_topElements.Contains(Cues))
            throw new Exception("Cues not found");

        return await _cuesParser.GetCuePoints(_topElements[Cues.Id], _seekHeadPosition, token);
    }

    public async Task<List<AudioTrack>> GetAudioTracks(CancellationToken token)
    {
        _topElements ??= await _seekHeadParser.ReadSeekHead(_seekHeadPosition, token);

        if (!_topElements.Contains(Tracks))
            throw new Exception("Tracks not found");

        return await _tracksParser.GetAudioTracks(_topElements[Tracks.Id], token);
    }

    public async Task<TimeSpan> GetDuration(CancellationToken token)
    {
        _topElements ??= await _seekHeadParser.ReadSeekHead(_seekHeadPosition, token);

        if (!_topElements.Contains(Info))
            throw new Exception("Duration not found");

        return await _infoParser.GetDuration(_topElements[Info.Id], token);
    }
}
