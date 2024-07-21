namespace Console.Audio.Containers.Matroska.Extensions.EbmlExtensions;

using Console.Audio.Containers.Matroska.EBML;
using Console.Audio.Containers.Matroska.Elements;

internal static class EbmlReaderUtilsExtensions
{
    /// <summary>
    ///     Seeks to a position and reads the next element
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="pos"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static ValueTask<MatroskaElement> Read(
        this EbmlReader reader,
        long pos,
        CancellationToken token = default
    )
    {
        if (reader.Seek(pos) != pos)
            throw new Exception("Couldn't seek to position");

        return reader.Read(token);
    }
}
