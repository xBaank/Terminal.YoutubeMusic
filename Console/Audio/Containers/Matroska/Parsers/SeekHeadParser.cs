namespace Console.Audio.Containers.Matroska.Parsers;

using Console.Audio.Containers.Matroska.EBML;
using Console.Audio.Containers.Matroska.Elements;
using Console.Audio.Containers.Matroska.Extensions.EbmlExtensions;
using Console.Extensions;
using Extensions.MatroskaExtensions;
using static Types.ElementTypes;

internal class SeekHeadParser
{
    private readonly EbmlReader _reader;

    public SeekHeadParser(EbmlReader ebmlReader) => _reader = ebmlReader;

    public async Task<TopElements> ReadSeekHead(long pos, CancellationToken token)
    {
        var seekHead =
            await _reader.Read(pos, token).PipeAsync(i => i.As(SeekHead))
            ?? throw new Exception("SeekHead not found");
        TopElements topElements = new(seekHead.Position);

        await foreach (var matroskaElement in _reader.ReadAll(seekHead.Size, token))
            await ReadSeek(matroskaElement, topElements, token);

        return topElements;
    }

    private async Task ReadSeek(
        MatroskaElement seek,
        TopElements topElements,
        CancellationToken token
    )
    {
        if (seek.Id != Seek.Id)
            throw new Exception("Found Invalid element in seekHead"); //Seekhead only contains seeks and seeks only contains seekId and seekPosition

        //The seekPos are relative to the seekHead Pos
        topElements.Add(await GetSeekId(token), await GetSeekPos(token));
    }

    private async Task<long> GetSeekPos(CancellationToken token)
    {
        var element = await _reader
            .Read(token)
            .PipeAsync(i => i.As(SeekPos) ?? throw new Exception("SeekPosition not found"));

        return await _reader.TryReadLong(element, token)
            ?? throw new Exception("Couldn't parse seek position");
    }

    private async Task<VInt> GetSeekId(CancellationToken token)
    {
        var element = await _reader
            .Read(token)
            .PipeAsync(i => i.As(SeekId) ?? throw new Exception("SeekId not found"));

        return await _reader.ReadVInt(element, token);
    }
}
