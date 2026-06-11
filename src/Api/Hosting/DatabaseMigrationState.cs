namespace MySociety.Api.Hosting;

internal sealed class DatabaseMigrationState
{
    private volatile bool _isComplete;
    private volatile bool _isInProgress;
    private Exception? _failure;

    public bool IsComplete => _isComplete;

    public bool IsInProgress => _isInProgress;

    public Exception? Failure => _failure;

    public void MarkInProgress()
    {
        _isInProgress = true;
    }

    public void MarkComplete()
    {
        _isComplete = true;
        _isInProgress = false;
    }

    public void MarkFailed(Exception exception)
    {
        _failure = exception;
        _isInProgress = false;
    }
}
