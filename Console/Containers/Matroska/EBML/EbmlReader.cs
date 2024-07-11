namespace Console.Containers.Matroska.EBML;

using System.Buffers;
using System.Runtime.CompilerServices;
using Elements;
using Extensions;
using static EbmlUtils;

internal class EbmlReader : IAsyncDisposable, IDisposable
{
    private readonly IMemoryOwner<byte> _memoryOwner;
    private readonly Stream _stream;

    public EbmlReader(Stream stream)
    {
        _stream = stream;
        _memoryOwner = MemoryPool<byte>.Shared.Rent(4096);
    }

    public async ValueTask DisposeAsync()
    {
        await _stream.DisposeAsync();
        _memoryOwner.Dispose();
    }

    public void Dispose()
    {
        _stream.Dispose();
        _memoryOwner.Dispose();
    }

    /// <summary>
    ///     Reads all element in a given size
    /// </summary>
    /// <remarks>
    ///     Inside the iteration you must read the element <see cref="ReadAsync" /> or skip it <see cref="Skip" />,
    ///     otherwise the iteration will end prematurely
    /// </remarks>
    /// <param name="elementSize"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>An asynchronous enumerable</returns>
    public async IAsyncEnumerable<MatroskaElement> ReadAll(
        long elementSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        bool hasMore;
        do
        {
            var pos = _stream.Position;
            var (id, bytesUsedInId) = await ReadBytesAsync(cancellationToken);
            var (size, bytesUsedInSize) = await ReadBytesAsync(cancellationToken, true);

            var isEof =
                id == default
                || size == default
                || bytesUsedInId == default
                || bytesUsedInSize == default;

            //Skip the bytes used in the id and size
            elementSize -= bytesUsedInId + bytesUsedInSize + size.ToLong();

            hasMore = elementSize > 0 && !isEof;

            if (!isEof)
                yield return new MatroskaElement(id, size.ToLong(), pos);
        } while (hasMore);
    }

    /// <summary>
    ///     Seek to the given position from the beginning of the stream
    /// </summary>
    /// <param name="offset"></param>
    /// <returns>The new position</returns>
    public long Seek(long offset) => _stream.Seek(offset, SeekOrigin.Begin);

    /// <summary>
    ///     Skips the given amount of bytes
    /// </summary>
    /// <param name="size"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>The skipped amount of bytes</returns>
    public ValueTask<long> Skip(long size, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<long>(cancellationToken);

        if (!_stream.CanSeek)
            throw new NotSupportedException("Stream does not support seeking");

        return ValueTask.FromResult(_stream.Seek(size, SeekOrigin.Current));
    }

    /// <summary>
    ///     Reads the data into the buffer.
    /// </summary>
    /// <param name="memory"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>the number of bytes read.</returns>
    public ValueTask<int> ReadAsync(
        Memory<byte> memory,
        CancellationToken cancellationToken = default
    ) => _stream.ReadAsync(memory, cancellationToken);

    public async ValueTask<MatroskaElement> Read(CancellationToken cancellationToken = default)
    {
        var pos = _stream.Position;
        (var id, int _) = await ReadBytesAsync(cancellationToken);
        (var size, int _) = await ReadBytesAsync(cancellationToken, true);
        return new MatroskaElement(id, size.ToLong(), pos);
    }

    private async ValueTask<(VInt id, byte bytesUsed)> ReadBytesAsync(
        CancellationToken cancellationToken,
        bool readAsLength = false
    )
    {
        //Read just the size part of the vint
        var readAsync = await _stream.ReadAsync(_memoryOwner.Memory[..1], cancellationToken);

        if (readAsync == 0)
            throw new EndOfStreamException("Unexpected end of stream");

        var octets = GetVintSize(readAsLength, _memoryOwner.Memory.Span, out var mask);

        //Read the rest of the vint
        readAsync = await _stream.ReadAsync(
            _memoryOwner.Memory.Slice(1, octets - 1),
            cancellationToken
        );

        if (readAsync != octets - 1)
            throw new EndOfStreamException("Unexpected end of stream");

        var vint = GetVint(readAsLength, octets, mask, _memoryOwner.Memory.Span);

        return (vint, (byte)octets);
    }
}
