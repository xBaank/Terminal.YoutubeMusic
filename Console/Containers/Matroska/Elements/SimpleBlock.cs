namespace Console.Containers.Matroska.Elements;

using Console.Containers.Matroska.EBML;
using Extensions.EbmlExtensions;
using Types;
using static Types.LacingType;

internal readonly struct SimpleBlock
{
    public SimpleBlock(ReadOnlyMemory<byte> data)
    {
        TrackNumber = data[..1]; //Track
        Timestamp = data[1..3].TryReadInt() ?? throw new Exception("Could not get timeStamp"); //Its a short type but we can read it as int
        var flags = data[3..4].Span[0]; //Flags
        LacingType = TryGetLacingType(flags) ?? throw new Exception("Could not get lacing type");

        FramesData = data[4..];
    }

    public static implicit operator SimpleBlock(ReadOnlyMemory<byte> data) => new(data);

    public static implicit operator SimpleBlock(Memory<byte> data) => new(data);

    public static implicit operator SimpleBlock(byte[] data) => new(data);

    public VInt TrackNumber { get; }

    public int Timestamp { get; }

    public LacingType LacingType { get; }

    public ReadOnlyMemory<byte> FramesData { get; }

    private static LacingType? TryGetLacingType(byte flags)
    {
        if ((flags & 0b00000110) == 0b000)
            return NoLacing;

        if ((flags & 0b00000110) == 0b010)
            return Xiph;

        if ((flags & 0b00000110) == 0b110)
            return Ebml;

        if ((flags & 0b00000110) == 0b100)
            return FixedSize;

        return null;
    }
}
