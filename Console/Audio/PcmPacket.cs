using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Console.Audio;

internal struct PcmPacket(byte[] Data, int Lenght) : IDisposable
{
    private readonly byte[] _data = Data;
    public readonly ReadOnlySpan<byte> Data => _data.AsSpan()[..Lenght];
    public int Lenght { get; } = Lenght;

    public readonly void Dispose() => ArrayPool<byte>.Shared.Return(_data);
}
