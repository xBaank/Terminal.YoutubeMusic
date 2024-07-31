namespace Console;

internal class SharedCancellationTokenSource
{
    private CancellationTokenSource _cancellationTokenSource = new();
    private readonly object _lock = new();

    public CancellationToken Token => _cancellationTokenSource.Token;

    public void Cancel() => _cancellationTokenSource.Cancel();

    public void Reset()
    {
        lock (_lock)
        {
            _cancellationTokenSource = new();
        }
    }
}
