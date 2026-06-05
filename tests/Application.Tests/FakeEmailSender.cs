using MySociety.Application.Common.Interfaces;

namespace MySociety.Application.Tests;

internal sealed class FakeEmailSender : IEmailSender
{
    public string? LastTo { get; private set; }
    public string? LastSubject { get; private set; }
    public string? LastBody { get; private set; }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        LastTo = to;
        LastSubject = subject;
        LastBody = body;
        return Task.CompletedTask;
    }

    public string? ExtractResetCode()
    {
        if (LastBody is null)
        {
            return null;
        }

        const string marker = "reset code is ";
        var index = LastBody.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        var start = index + marker.Length;
        var end = start;
        while (end < LastBody.Length && char.IsDigit(LastBody[end]))
        {
            end++;
        }

        return end > start ? LastBody[start..end] : null;
    }
}
