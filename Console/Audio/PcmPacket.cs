using System.Buffers;

namespace Console.Audio;

internal readonly struct PcmPacket<T>(T[] Data, int Lenght) : IDisposable
    where T : struct
{
    private readonly T[] _data = Data;
    public readonly Span<T> Data => _data.AsSpan()[..Lenght];
    public int Lenght { get; } = Lenght;

    public readonly void Dispose() => ArrayPool<T>.Shared.Return(_data);
}
