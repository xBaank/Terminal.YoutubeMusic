namespace Console.Containers.Matroska.Extensions.EbmlExtensions;

public static class SpanExtensions
{
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this Span<T> source) => source;

    public static ReadOnlySpan<byte> TrimStartEmptyBytes(this ReadOnlySpan<byte> source) =>
        source[source.GetStartEmptyBytes()..];

    public static int GetStartEmptyBytes(this ReadOnlySpan<byte> source)
    {
        var emptyBytes = 0;
        foreach (var byt in source)
        {
            if (byt != 0)
                break;

            emptyBytes++;
        }
        return emptyBytes;
    }

    public static void CopyFromEnd<T>(this ReadOnlySpan<T> source, Span<T> destination)
    {
        if (destination.Length < source.Length)
            throw new ArgumentException("Destination span is too small");

        var diff = destination.Length - source.Length;

        for (var i = 0; i < source.Length; i++)
        {
            destination[i + diff] = source[i];
        }
    }
}
