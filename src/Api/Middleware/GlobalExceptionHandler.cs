using System.Net;
using System.Text.Json;
using MySociety.Application.Common.Exceptions;

namespace MySociety.Api.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (IsClientCancellation(context, ex))
        {
            _logger.LogDebug("Request cancelled while processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static bool IsClientCancellation(HttpContext context, Exception exception) =>
        exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested;

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            NotFoundException notFound => (HttpStatusCode.NotFound, notFound.Message),
            ValidationException validation => (HttpStatusCode.BadRequest, validation.Message),
            ConflictException conflict => (HttpStatusCode.Conflict, conflict.Message),
            UnauthorizedException unauthorized => (HttpStatusCode.Unauthorized, unauthorized.Message),
            ForbiddenException forbidden => (HttpStatusCode.Forbidden, forbidden.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Handled exception returned as {StatusCode}: {Message}",
                (int)statusCode,
                message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(new { error = message });
        await context.Response.WriteAsync(payload);
    }
}
