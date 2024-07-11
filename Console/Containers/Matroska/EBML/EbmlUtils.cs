namespace Console.Containers.Matroska.EBML;

using System.Buffers;

internal static class EbmlUtils
{
    public static VInt GetVint(bool readAsLength, int octets, int mask, ReadOnlySpan<byte> data)
    {
        using var memoryOwner = MemoryPool<byte>.Shared.Rent(octets);
        var span = memoryOwner.Memory.Span;

        for (var i = 0; i < octets; i++)
        {
            span[i] = data[i];

            if (readAsLength && i == 0)
                span[i] ^= (byte)mask;
        }

        return span[..octets];
    }

    public static VInt GetVint(bool readAsLength, ReadOnlySpan<byte> data)
    {
        var octets = GetVintSize(readAsLength, data, out var mask);
        return GetVint(readAsLength, octets, mask, data);
    }

    public static int GetVintSize(bool readAsLength, ReadOnlySpan<byte> data, out int mask)
    {
        var octets = 0;
        var isBitMarked = false;
        mask = 0b1000_0000;
        var byteValue = data[0];

        while (!isBitMarked)
        {
            octets++;

            if (mask == 0 || readAsLength && octets > 8)
                throw new IndexOutOfRangeException("Buffer is too small");

            if ((mask & byteValue) == 0)
            {
                mask >>= 1;
                continue;
            }

            isBitMarked = true;
        }
        return octets;
    }
}
