using System.Text.RegularExpressions;
using MySociety.Application.Common.Interfaces;

namespace MySociety.Application.Tests;

internal sealed class FakeSmsSender : ISmsSender
{
    public string? LastPhone { get; private set; }
    public string? LastMessage { get; private set; }

    public Task SendAsync(string phone, string message, CancellationToken cancellationToken)
    {
        LastPhone = phone;
        LastMessage = message;
        return Task.CompletedTask;
    }

    public string? ExtractLastOtp()
    {
        if (LastMessage is null)
        {
            return null;
        }

        return Regex.Match(LastMessage, @"\b(\d{6})\b").Groups[1].Value;
    }
}
