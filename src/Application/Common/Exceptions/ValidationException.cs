namespace MySociety.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(IEnumerable<string> errors)
        : base(string.Join("; ", errors))
    {
        Errors = errors;
    }

    public IEnumerable<string> Errors { get; } = [];
}
