namespace Console.Containers.Matroska.Types;

using static AudioCodecTypes;

internal enum AudioCodecTypes
{
    Vorbis,
    Opus,
}

internal static class AudioCodecTypesUtils
{
    public static AudioCodecTypes? TryGetType(string type) =>
        type switch
        {
            "A_OPUS" => Opus,
            "A_VORBIS" => Vorbis,
            _ => null,
        };
}
