namespace MySociety.Api.Hosting;

internal sealed class DatabaseMigrationState
{
    private volatile bool _isComplete;
    private Exception? _failure;

    public bool IsComplete => _isComplete;

    public Exception? Failure => _failure;

    public void MarkComplete()
    {
        _isComplete = true;
    }

    public void MarkFailed(Exception exception)
    {
        _failure = exception;
    }
}
