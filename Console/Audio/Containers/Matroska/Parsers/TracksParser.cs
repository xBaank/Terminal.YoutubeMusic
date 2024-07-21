namespace Console.Audio.Containers.Matroska.Parsers;

using Console.Audio.Containers.Matroska.EBML;
using Console.Audio.Containers.Matroska.Elements;
using Console.Audio.Containers.Matroska.Extensions.EbmlExtensions;
using Console.Containers.Matroska.Extensions.EbmlExtensions;
using Console.Extensions;
using Types;
using static Types.AudioCodecTypes;
using static Types.ElementTypes;

internal class TracksParser
{
    private readonly EbmlReader _reader;

    private List<AudioTrack>? _audioTracks;

    public TracksParser(EbmlReader reader) => _reader = reader;

    public async ValueTask<List<AudioTrack>> GetAudioTracks(long pos, CancellationToken token) =>
        _audioTracks ??= await LoadTracks(pos, token);

    private async Task<List<AudioTrack>> LoadTracks(long pos, CancellationToken token)
    {
        var element = await _reader.Read(pos, token);
        var audioTracks = new List<AudioTrack>();

        await foreach (var trackEntry in _reader.ReadAll(element.Size, token))
        {
            if (trackEntry.Id != TrackEntry.Id)
                throw new Exception("Found unexpected element");

            var audioTrack = await TryGetAudioTrack(trackEntry, token);

            //We only want audio tracks of opus codec
            if (audioTrack is not { Codec: Opus })
                continue;

            audioTracks.Add(audioTrack.Value);
        }

        if (!audioTracks.Any())
            throw new Exception("No audio tracks found");

        return audioTracks;
    }

    private async Task<AudioTrack?> TryGetAudioTrack(
        MatroskaElement matroskaElement,
        CancellationToken token
    )
    {
        var trackNumber = 0;
        AudioCodecTypes? codecType = 0;
        AudioSettings? audioSettings = null;
        TrackTypes? trackType = null;

        await foreach (var element in _reader.ReadAll(matroskaElement.Size, token))
        {
            if (element.Id == TrackNumber.Id)
            {
                trackNumber =
                    await _reader.TryReadInt(element, token)
                    ?? throw new Exception("Couldn't parse track number");
                continue;
            }

            if (element.Id == TrackType.Id)
            {
                trackType = await _reader
                    .TryReadInt(element, token)
                    .PipeAsync(i => i ?? throw new Exception("Couldn't parse trackType"))
                    .PipeAsync(TrackTypesUtils.TryGetType);
                continue;
            }

            if (element.Id == CodecId.Id)
            {
                codecType = await _reader
                    .TryReadString(element, token)
                    .PipeAsync(i => i ?? throw new Exception("Couldn't parse codecType"))
                    .PipeAsync(AudioCodecTypesUtils.TryGetType);
                continue;
            }

            if (element.Id == Audio.Id)
            {
                audioSettings = await TryReadAudioSettings(element, token);
                continue;
            }

            await _reader.Skip(element.Size, token);
        }

        if (trackType is not TrackTypes.Audio)
            return null;
        if (!codecType.HasValue)
            return null;
        if (!audioSettings.HasValue)
            return null;

        return new AudioTrack(codecType.Value, audioSettings.Value, trackNumber);
    }

    private async Task<AudioSettings?> TryReadAudioSettings(
        MatroskaElement matroskaElement,
        CancellationToken token
    )
    {
        //Default values set in the specification
        var audioChannels = 1;
        float audioSamplingFrequency = 8000;
        var audioBitDepth = 0;

        await foreach (var element in _reader.ReadAll(matroskaElement.Size, token))
        {
            if (element.Id == Channels.Id)
            {
                audioChannels =
                    await _reader.TryReadInt(element, token)
                    ?? throw new Exception("Couldn't parse audio channels");
                continue;
            }
            if (element.Id == Sampling.Id)
            {
                audioSamplingFrequency =
                    await _reader.TryReadFloat(element, token)
                    ?? throw new Exception("Couldn't parse audio sampling frequency");
                continue;
            }
            if (element.Id == BitDepth.Id)
            {
                audioBitDepth =
                    await _reader.TryReadInt(element, token)
                    ?? throw new Exception("Couldn't parse audio bit depth");
                continue;
            }
            await _reader.Skip(element.Size, token);
        }

        return new AudioSettings(audioSamplingFrequency, audioChannels, audioBitDepth);
    }
}
