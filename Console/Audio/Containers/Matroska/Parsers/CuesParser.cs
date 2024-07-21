namespace Console.Audio.Containers.Matroska.Parsers;

using Console.Audio.Containers.Matroska.EBML;
using Console.Audio.Containers.Matroska.Elements;
using Console.Audio.Containers.Matroska.Extensions.EbmlExtensions;
using Console.Containers.Matroska.Extensions.EbmlExtensions;
using Types;
using static Types.ElementTypes;

internal class CuesParser
{
    private readonly EbmlReader _reader;

    private List<CuePoint>? _cuePoints;

    public CuesParser(EbmlReader reader) => _reader = reader;

    public async ValueTask<List<CuePoint>> GetCuePoints(
        long pos,
        long seekHeadPos,
        CancellationToken token
    ) => _cuePoints ??= await LoadCuePoints(pos, seekHeadPos, token);

    private async Task<List<CuePoint>> LoadCuePoints(
        long pos,
        long seekHeadPos,
        CancellationToken token
    )
    {
        var element = await _reader.Read(pos, token);

        if (element.Id != Cues.Id)
            throw new Exception("Invalid element id");

        var cuePoints = new List<CuePoint>();

        await foreach (var cuePoint in _reader.ReadAll(element.Size, token))
        {
            if (cuePoint.Id != ElementTypes.CuePoint.Id)
                throw new Exception("Invalid cue point");

            cuePoints.Add(await GetCuePoint(cuePoint, seekHeadPos, token));
        }

        if (!cuePoints.Any())
            throw new Exception("No cue points found");

        return cuePoints;
    }

    private async ValueTask<CuePoint> GetCuePoint(
        MatroskaElement element,
        long seekHeadPos,
        CancellationToken token
    )
    {
        ulong time = 0;
        List<CueTrackPosition> trackPositions = new();

        await foreach (var matroskaElement in _reader.ReadAll(element.Size, token))
        {
            if (matroskaElement.Id == CueTime.Id)
            {
                time =
                    await _reader.TryReadUlong(matroskaElement, token)
                    ?? throw new Exception("Couldn't parse CueTime");
                continue;
            }

            if (matroskaElement.Id == CueTrackPositions.Id)
            {
                trackPositions.Add(
                    await TryGetCueTrack(matroskaElement.Size, seekHeadPos, token)
                        ?? throw new Exception("Couldn't parse CueTrackPosition")
                );
                continue;
            }

            await _reader.Skip(matroskaElement.Size, token);
        }

        return new CuePoint(time, trackPositions);
    }

    private async ValueTask<CueTrackPosition?> TryGetCueTrack(
        long size,
        long seekHeadPos,
        CancellationToken token
    )
    {
        var track = 0;
        long clusterPosition = 0;

        await foreach (var matroskaElement in _reader.ReadAll(size, token))
        {
            if (matroskaElement.Id == CueTrack.Id)
            {
                track =
                    await _reader.TryReadInt(matroskaElement, token)
                    ?? throw new Exception("Couldn't parse CueTrack");
                continue;
            }

            if (matroskaElement.Id == CueClusterPosition.Id)
            {
                clusterPosition =
                    await _reader.TryReadLong(matroskaElement, token)
                    ?? throw new Exception("Couldn't parse CueClusterPosition");
                continue;
            }

            await _reader.Skip(matroskaElement.Size, token);
        }

        if (track == default || clusterPosition == default)
            return null;

        return new CueTrackPosition(clusterPosition + seekHeadPos, track);
    }
}
