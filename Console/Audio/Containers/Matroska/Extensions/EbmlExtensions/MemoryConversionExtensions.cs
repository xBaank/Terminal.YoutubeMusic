namespace Console.Audio.Containers.Matroska.Extensions.EbmlExtensions;

public static class MemoryConversionExtensions
{
    //Same as SpanConversionExtesions but for Memory
    public static ulong? TryReadUlong(this ReadOnlyMemory<byte> source) =>
        source.Span.TryReadUlong();

    public static uint? TryReadUInt(this ReadOnlyMemory<byte> source) => source.Span.TryReadUInt();

    public static long? TryReadLong(this ReadOnlyMemory<byte> source) => source.Span.TryReadlong();

    public static int? TryReadInt(this ReadOnlyMemory<byte> source) => source.Span.TryReadInt();

    public static double? TryReadDouble(this ReadOnlyMemory<byte> source) =>
        source.Span.TryReadDouble();

    public static float? TryReadFloat(this ReadOnlyMemory<byte> source) =>
        source.Span.TryReadFloat();

    public static DateTime? TryReadDate(this ReadOnlyMemory<byte> source) =>
        source.Span.TryReadDate();

    public static string? TryReadString(this ReadOnlyMemory<byte> source) =>
        source.Span.TryReadString();

    public static string? TryReadStringUtf8(this ReadOnlyMemory<byte> source) =>
        source.Span.TryReadStringUtf8();
}
