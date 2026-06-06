using Microsoft.EntityFrameworkCore;
using MySociety.Infrastructure.Persistence;
using Serilog;

namespace MySociety.Api.Hosting;

internal static class DatabaseBootstrap
{
    public static async Task ApplyMigrationsAsync(
        IServiceProvider services,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var applied = await db.Database.GetAppliedMigrationsAsync(cancellationToken);
        var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);

        Log.Information(
            "Migration state — applied: [{Applied}]; pending: [{Pending}]",
            applied.Any() ? string.Join(", ", applied) : "(none)",
            pending.Any() ? string.Join(", ", pending) : "(none)");

        if (!pending.Any())
        {
            Log.Information("Database schema is up to date");
        }
        else
        {
            await db.Database.MigrateAsync(cancellationToken);
            Log.Information("Database migrations applied successfully");
        }

        if (environment.IsDevelopment())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync(cancellationToken);
            Log.Information("Development seed data applied");
        }
    }
}
