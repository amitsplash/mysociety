using MySociety.Api.Hosting;

namespace MySociety.Api.Middleware;

internal sealed class DatabaseMigrationMiddleware
{
    private static readonly PathString HealthPath = new("/health");

    private readonly RequestDelegate _next;
    private readonly DatabaseMigrationState _migrationState;

    public DatabaseMigrationMiddleware(RequestDelegate next, DatabaseMigrationState migrationState)
    {
        _next = next;
        _migrationState = migrationState;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments(HealthPath) || path.Value is "/" or "")
        {
            await _next(context);
            return;
        }

        if (_migrationState.Failure is not null)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                status = "unavailable",
                message = "Database setup failed. Check service logs."
            });
            return;
        }

        if (!_migrationState.IsComplete)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                status = "starting",
                message = "Database migration in progress."
            });
            return;
        }

        await _next(context);
    }
}
