using Microsoft.EntityFrameworkCore;
using MySociety.Infrastructure.Persistence;
using Serilog;

namespace MySociety.Api.Hosting;

internal static class DatabaseBootstrap
{
    private const int MaxConnectionAttempts = 5;

    public static async Task ApplyMigrationsAsync(
        IServiceProvider services,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        Log.Information("Starting database migration check");

        for (var attempt = 1; attempt <= MaxConnectionAttempts; attempt++)
        {
            try
            {
                await ApplyMigrationsCoreAsync(services, environment, cancellationToken);
                return;
            }
            catch (Exception ex) when (attempt < MaxConnectionAttempts && IsTransientConnectionFailure(ex))
            {
                var delay = TimeSpan.FromSeconds(attempt * 5);
                Log.Warning(
                    ex,
                    "Database connection attempt {Attempt}/{MaxAttempts} failed; retrying in {DelaySeconds}s",
                    attempt,
                    MaxConnectionAttempts,
                    delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private static async Task ApplyMigrationsCoreAsync(
        IServiceProvider services,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var connectionString = db.Database.GetConnectionString();
        if (!string.IsNullOrWhiteSpace(connectionString)
            && PostgresConnectionConfiguration.UsesPoolerEndpoint(connectionString))
        {
            Log.Warning(
                "Connection string uses a pooler endpoint. EF migrations need a direct PostgreSQL host " +
                "(Neon: hostname without '-pooler'; Supabase: db.{ref}.supabase.co port 5432, not pooler port 6543).");
        }

        var applied = await db.Database.GetAppliedMigrationsAsync(cancellationToken);
        var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);

        Log.Information(
            "Migration state — applied: [{Applied}]; pending: [{Pending}]",
            applied.Any() ? string.Join(", ", applied) : "(none)",
            pending.Any() ? string.Join(", ", pending) : "(none)");

        if (pending.Any())
        {
            Log.Information("Applying {Count} pending migration(s)...", pending.Count());
            await db.Database.MigrateAsync(cancellationToken);
            Log.Information("Database migrations applied successfully");
        }
        else
        {
            await VerifyCoreSchemaAsync(db, cancellationToken);
            Log.Information("Database schema is up to date");
        }

        if (environment.IsDevelopment())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync(cancellationToken);
            Log.Information("Development seed data applied");
        }
    }

    private static async Task VerifyCoreSchemaAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            _ = await db.Users.AsNoTracking().AnyAsync(cancellationToken);
        }
        catch (Exception ex) when (IsMissingRelation(ex))
        {
            throw new InvalidOperationException(
                "EF migration history indicates the database is up to date, but core tables are missing. " +
                "Drop public.__EFMigrationsHistory (or use a fresh database) and redeploy.",
                ex);
        }
    }

    private static bool IsTransientConnectionFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("Failed to connect", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("No such host", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsMissingRelation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains("42P01", StringComparison.Ordinal)
                || current.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
