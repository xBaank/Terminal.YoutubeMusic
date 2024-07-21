namespace Console.Audio.Containers.Matroska.Extensions.EbmlExtensions;

using static System.Buffers.Binary.BinaryPrimitives;
using static System.Text.Encoding;

public static class SpanConversionsExtensions
{
    //EBML Elements that are defined as a Signed Integer Element, Unsigned Integer Element, Float Element, or Date Element use big-endian storage.
    //https://github.com/ietf-wg-cellar/ebml-specification/blob/master/specification.markdown#ebml-element-types
    public static ulong? TryReadUlong(this ReadOnlySpan<byte> source)
    {
        Span<byte> buff = stackalloc byte[sizeof(ulong)];
        source.CopyFromEnd(buff);

        var hasResult = TryReadUInt64BigEndian(buff, out var result);

        if (!hasResult)
            return null;

        return result;
    }

    public static string? TryReadString(this ReadOnlySpan<byte> source) =>
        Default.GetString(source);

    public static string? TryReadStringUtf8(this ReadOnlySpan<byte> source) =>
        UTF8.GetString(source);

    public static long? TryReadlong(this ReadOnlySpan<byte> source)
    {
        Span<byte> buff = stackalloc byte[sizeof(long)];
        source.CopyFromEnd(buff);

        var hasResult = TryReadInt64BigEndian(buff, out var result);

        if (!hasResult)
            return null;

        return result;
    }

    public static int? TryReadInt(this ReadOnlySpan<byte> source)
    {
        Span<byte> buff = stackalloc byte[sizeof(int)];
        source.CopyFromEnd(buff);

        var hasResult = TryReadInt32BigEndian(buff, out var result);

        if (!hasResult)
            return null;

        return result;
    }

    public static DateTime? TryReadDate(this ReadOnlySpan<byte> source)
    {
        Span<byte> buff = stackalloc byte[sizeof(int)];
        source.CopyFromEnd(buff);

        const int nanoSecondsPerMilisecond = 1000000;

        var hasResult = TryReadInt32BigEndian(buff, out var result);

        if (!hasResult)
            return null;

        //https://github.com/ietf-wg-cellar/ebml-specification/blob/master/specification.markdown#date-element
        return new DateTime(2001, 01, 01).AddMilliseconds(result * nanoSecondsPerMilisecond);
    }

    public static uint? TryReadUInt(this ReadOnlySpan<byte> source)
    {
        Span<byte> buff = stackalloc byte[sizeof(uint)];
        source.CopyFromEnd(buff);

        var hasResult = TryReadUInt32BigEndian(buff, out var result);

        if (!hasResult)
            return null;

        return result;
    }

    public static double? TryReadDouble(this ReadOnlySpan<byte> source)
    {
        //When reaading a double, if the size is the same or less than the float, we should read it as a float.
        if (source.Length <= sizeof(float))
            return source.TryReadFloat();

        Span<byte> buff = stackalloc byte[sizeof(double)];
        source.CopyFromEnd(buff);

        var hasResult = TryReadDoubleBigEndian(buff, out var result);

        if (!hasResult)
            return null;

        return result;
    }

    public static float? TryReadFloat(this ReadOnlySpan<byte> source)
    {
        Span<byte> buff = stackalloc byte[sizeof(float)];
        source.CopyFromEnd(buff);

        var hasResult = TryReadSingleBigEndian(buff, out var result);

        if (!hasResult)
            return null;

        return result;
    }
}
