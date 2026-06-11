namespace MySociety.Application.Common.Exceptions;

public class ConflictException : Exception
{
    public ConflictException(string message, string? code = null) : base(message)
    {
        Code = code;
    }

    public string? Code { get; }
}
