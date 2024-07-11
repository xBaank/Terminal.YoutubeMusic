namespace Console.Containers.Matroska.Types;

using static TrackTypes;

internal enum TrackTypes
{
    Video = 1,
    Audio = 2,
}

internal static class TrackTypesUtils
{
    public static TrackTypes? TryGetType(int type) =>
        type switch
        {
            1 => Video,
            2 => Audio,
            _ => null,
        };
}
