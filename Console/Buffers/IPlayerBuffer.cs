namespace Console.Buffers;

public interface IPlayerBuffer : IDisposable, IAsyncDisposable
{
    public TimeSpan CurrentTime { get; }

    public TimeSpan TotalTime { get; }

    /// <summary>
    ///     Writes the url media to the stream.
    /// </summary>
    /// <returns></returns>
    public Task WriteFile(Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Seeks to a timeStamp in milliseconds.
    /// </summary>
    /// <param name="timeStamp"></param>
    public ValueTask<bool> Seek(long timeStamp);
}
