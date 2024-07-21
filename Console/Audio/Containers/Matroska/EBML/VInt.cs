namespace Console.Audio.Containers.Matroska.EBML;

using Console.Audio.Containers.Matroska.Extensions.EbmlExtensions;

internal readonly struct VInt
{
    //The equal method uses ulong (could use also long)
    public bool Equals(VInt other) => Value == other.Value;

    public override bool Equals(object? obj) => obj is VInt other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public ulong Value { get; }

    public byte Length { get; }

    private VInt(ReadOnlySpan<byte> data)
    {
        if (data.Length > 8)
            throw new ArgumentException("VInt can only be 8 bytes long");

        if (data.Length < 1)
            throw new ArgumentException("VInt cannot be less than 1 bytes long");

        Length = (byte)data.Length;

        Value = ToUlong(data);
    }

    public static implicit operator VInt(ReadOnlyMemory<byte> data) => new(data.Span);

    public static implicit operator VInt(Memory<byte> data) => new(data.Span);

    public static implicit operator VInt(ReadOnlySpan<byte> data) => new(data);

    public static implicit operator VInt(Span<byte> data) => new(data);

    public static implicit operator VInt(byte[] data) => new(data);

    public static bool operator ==(VInt a, VInt b) => a.Equals(b);

    public static bool operator !=(VInt a, VInt b) => !a.Equals(b);

    private static ulong ToUlong(ReadOnlySpan<byte> data)
    {
        //Basically what we are doing is creating a new span over the data that is not empty and then converting it to a ulong by copying it to the back of the new span
        Span<byte> buff = stackalloc byte[8];
        var value = data.TrimStartEmptyBytes();

        if (value.Length > 8)
            throw new InvalidCastException("Could not read VInt as ulong");

        value.CopyFromEnd(buff);
        return buff.AsReadOnlySpan().TryReadUlong()
            ?? throw new InvalidCastException("Could not read VInt as ulong");
    }
}
