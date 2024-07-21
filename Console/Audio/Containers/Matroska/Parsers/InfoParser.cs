namespace Console.Audio.Containers.Matroska.Parsers;

using Console.Audio.Containers.Matroska.EBML;
using Console.Audio.Containers.Matroska.Extensions.EbmlExtensions;
using static Types.ElementTypes;

internal class InfoParser
{
    private readonly EbmlReader _reader;
    private TimeSpan? _duration;

    public InfoParser(EbmlReader ebmlReader) => _reader = ebmlReader;

    public async ValueTask<TimeSpan> GetDuration(long pos, CancellationToken token) =>
        _duration ??= await LoadDuration(pos, token);

    private async Task<TimeSpan> LoadDuration(long pos, CancellationToken token)
    {
        var element = await _reader.Read(pos, token);

        if (element.Id != Info.Id)
            throw new Exception("Invalid element");

        var infoValues = await GetInfo(element.Size, token);

        if (infoValues.timeScaleData == default)
            throw new Exception("No timeScaleData found");

        if (infoValues.durationData == default)
            throw new Exception("No durationData found");

        const int nanoSecondsPerMilisecond = 1000000;

        return TimeSpan.FromMilliseconds(
            infoValues.durationData * infoValues.timeScaleData / nanoSecondsPerMilisecond
        );
    }

    private async ValueTask<(ulong timeScaleData, double durationData)> GetInfo(
        long size,
        CancellationToken token
    )
    {
        ulong timeScaleData = 0;
        double durationData = 0;

        await foreach (var matroskaElement in _reader.ReadAll(size, token))
        {
            if (matroskaElement.Id == TimestampScale.Id)
            {
                timeScaleData =
                    await _reader.TryReadUlong(matroskaElement, token)
                    ?? throw new Exception("Couldn't parse TimeScaleData");
                continue;
            }

            if (matroskaElement.Id == Duration.Id)
            {
                durationData =
                    await _reader.TryReadDouble(matroskaElement, token)
                    ?? throw new Exception("Couldn't parse duration");
                continue;
            }

            await _reader.Skip(matroskaElement.Size, token);
        }
        return (timeScaleData, durationData);
    }
}
