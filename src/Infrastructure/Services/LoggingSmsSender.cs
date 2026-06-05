using Microsoft.Extensions.Logging;
using MySociety.Application.Common.Interfaces;

namespace MySociety.Infrastructure.Services;

/// <summary>
/// Logs SMS content for development and environments without an SMS provider configured.
/// Replace with a real provider (Twilio, Azure Communication Services, etc.) for production SMS.
/// </summary>
public class LoggingSmsSender : ISmsSender
{
    private readonly ILogger<LoggingSmsSender> _logger;

    public LoggingSmsSender(ILogger<LoggingSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string phone, string message, CancellationToken cancellationToken)
    {
        _logger.LogWarning("SMS to {Phone}: {Message}", phone, message);
        return Task.CompletedTask;
    }
}
