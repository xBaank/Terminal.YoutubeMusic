namespace Console.Extensions;

using System.Buffers;

public static class StreamExtensions
{
    public static async Task WriteSharedAsync(
        this Stream stream,
        byte[] buffer,
        int count,
        CancellationToken token = default
    )
    {
        try
        {
            await stream.WriteAsync(buffer.AsMemory(0, count), token);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
