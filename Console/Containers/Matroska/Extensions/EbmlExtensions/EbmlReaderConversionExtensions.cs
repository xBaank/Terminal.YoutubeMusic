namespace Console.Containers.Matroska.Extensions.EbmlExtensions;

using System.Buffers;
using Console.Containers.Matroska.EBML;
using Console.Containers.Matroska.Elements;
using DiscordBot.MusicPlayer.Containers.Matroska.Extensions.EbmlExtensions;

internal static class EbmlReaderConversionExtensions
{
    public static async ValueTask<VInt> ReadVInt(
        this EbmlReader ebmlReader,
        MatroskaElement matroskaElement,
        CancellationToken token
    )
    {
        VInt ParseFunc(Memory<byte> i) => i;
        return await ebmlReader.GetFromRented(matroskaElement.Size, ParseFunc, token);
    }

    public static async ValueTask<ulong?> TryReadUlong(
        this EbmlReader ebmlReader,
        MatroskaElement matroskaElement,
        CancellationToken token
    )
    {
        ulong? ParseFunc(Memory<byte> i) => i.AsReadOnlyMemory().TryReadUlong();
        return await ebmlReader.GetFromRented(matroskaElement.Size, ParseFunc, token);
    }

    public static async ValueTask<uint?> TryReadUInt(
        this EbmlReader ebmlReader,
        MatroskaElement matroskaElement,
        CancellationToken token
    )
    {
        uint? ParseFunc(Memory<byte> i) => i.AsReadOnlyMemory().TryReadUInt();
        return await ebmlReader.GetFromRented(matroskaElement.Size, ParseFunc, token);
    }

    public static async ValueTask<long?> TryReadLong(
        this EbmlReader ebmlReader,
        MatroskaElement matroskaElement,
        CancellationToken token
    )
    {
        long? ParseFunc(Memory<byte> i) => i.AsReadOnlyMemory().TryReadLong();
        return await ebmlReader.GetFromRented(matroskaElement.Size, ParseFunc, token);
    }

    public static async ValueTask<double?> TryReadDouble(
        this EbmlReader ebmlReader,
        MatroskaElement matroskaElement,
        CancellationToken token
    )
    {
        double? ParseFunc(Memory<byte> i) => i.AsReadOnlyMemory().TryReadDouble();
        return await ebmlReader.GetFromRented(matroskaElement.Size, ParseFunc, token);
    }

    public static async ValueTask<int?> TryReadInt(
        this EbmlReader ebmlReader,
        MatroskaElement matroskaElement,
        CancellationToken token
    )
    {
        int? ParseFunc(Memory<byte> i) => i.AsReadOnlyMemory().TryReadInt();
        return await ebmlReader.GetFromRented(matroskaElement.Size, ParseFunc, token);
    }

    public static async ValueTask<float?> TryReadFloat(
        this EbmlReader ebmlReader,
        MatroskaElement matroskaElement,
        CancellationToken token
    )
    {
        float? ParseFunc(Memory<byte> i) => i.AsReadOnlyMemory().TryReadFloat();
        return await ebmlReader.GetFromRented(matroskaElement.Size, ParseFunc, token);
    }

    public static async ValueTask<DateTime?> TryReadDate(
        this EbmlReader ebmlReader,
        MatroskaElement matroskaElement,
        CancellationToken token
    )
    {
        DateTime? ParseFunc(Memory<byte> i) => i.AsReadOnlyMemory().TryReadDate();
        return await ebmlReader.GetFromRented(matroskaElement.Size, ParseFunc, token);
    }

    public static async ValueTask<string?> TryReadString(
        this EbmlReader ebmlReader,
        MatroskaElement matroskaElement,
        CancellationToken token
    )
    {
        string? ParseFunc(Memory<byte> i) => i.AsReadOnlyMemory().TryReadString();
        return await ebmlReader.GetFromRented(matroskaElement.Size, ParseFunc, token);
    }

    public static async ValueTask<string?> TryReadStringUtf8(
        this EbmlReader ebmlReader,
        MatroskaElement matroskaElement,
        CancellationToken token
    )
    {
        string? ParseFunc(Memory<byte> i) => i.AsReadOnlyMemory().TryReadStringUtf8();
        return await ebmlReader.GetFromRented(matroskaElement.Size, ParseFunc, token);
    }

    private static async Task<T> GetFromRented<T>(
        this EbmlReader ebmlReader,
        long size,
        Func<Memory<byte>, T> parseFunc,
        CancellationToken token
    )
    {
        if (size > int.MaxValue)
            throw new Exception("Size is too big");

        using var memoryOwner = MemoryPool<byte>.Shared.Rent((int)size);
        var buff = memoryOwner.Memory[..(int)size];

        var read = await ebmlReader.ReadAsync(buff, token);

        if (read < buff.Length)
            throw new EndOfStreamException("Unexpected end of stream");

        return parseFunc(buff);
    }

    public static async ValueTask<SimpleBlock> GetSimpleBlock(
        this EbmlReader ebmlReader,
        Memory<byte> buff,
        CancellationToken token
    )
    {
        var read = await ebmlReader.ReadAsync(buff, token);

        if (read < buff.Length)
            throw new EndOfStreamException("Unexpected end of stream");

        SimpleBlock block = buff;

        return block;
    }
}
