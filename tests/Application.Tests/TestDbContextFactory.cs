using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MySociety.Infrastructure.Persistence;

namespace MySociety.Application.Tests;

internal static class TestDbContextFactory
{
    public static async Task<AppDbContext> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }
}
