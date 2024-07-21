namespace Console.Audio.Containers.Matroska.Parsers;

using Console.Audio.Containers.Matroska.EBML;
using Console.Audio.Containers.Matroska.Elements;
using Console.Audio.Containers.Matroska.Extensions.EbmlExtensions;
using Console.Containers.Matroska.Extensions.EbmlExtensions;
using Console.Extensions;
using Extensions.MatroskaExtensions;
using static Types.ElementTypes;

internal class EbmlHeaderParser
{
    private readonly EbmlReader _reader;
    private string? _type;

    public EbmlHeaderParser(EbmlReader reader) => _reader = reader;

    public async ValueTask<string?> TryGetDocType(CancellationToken token) =>
        _type ??= await LoadGetDocType(token);

    private async Task<string?> LoadGetDocType(CancellationToken token)
    {
        var ebmlHeader =
            await _reader.Read(0, token).PipeAsync(i => i.As(EbmlHeader))
            ?? throw new Exception("EBML header not found");

        await foreach (var matroskaElement in _reader.ReadAll(ebmlHeader.Size, token))
            await ParseElement(matroskaElement, token);

        return _type;
    }

    private async Task ParseElement(MatroskaElement matroskaElement, CancellationToken token)
    {
        if (matroskaElement.Id == DocType.Id)
        {
            _type = await _reader
                .TryReadString(matroskaElement, token)
                .PipeAsync(i => i ?? throw new Exception("Couldn't parse DocType"));
            return;
        }
        await _reader.Skip(matroskaElement.Size, token);
    }
}
