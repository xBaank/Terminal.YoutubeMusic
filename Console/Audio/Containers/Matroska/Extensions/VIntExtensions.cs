namespace Console.Audio.Containers.Matroska.Extensions;

using Console.Audio.Containers.Matroska.EBML;

internal static class VIntExtensions
{
    public static long ToLong(this VInt vInt)
    {
        if (vInt.Value > long.MaxValue)
            throw new InvalidCastException("Could not read VInt as long");

        return (long)vInt.Value;
    }
}
