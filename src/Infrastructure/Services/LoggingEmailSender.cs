using Microsoft.Extensions.Logging;
using MySociety.Application.Common.Interfaces;

namespace MySociety.Infrastructure.Services;

/// <summary>
/// Logs email content when SMTP is not configured (development / staging).
/// </summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Email to {To} | Subject: {Subject} | Body: {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
