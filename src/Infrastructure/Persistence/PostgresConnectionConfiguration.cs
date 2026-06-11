using Npgsql;

namespace MySociety.Infrastructure.Persistence;

public static class PostgresConnectionConfiguration
{
    private const int MinimumConnectionTimeoutSeconds = 60;
    private const int MinimumCommandTimeoutSeconds = 180;

    public static string Normalize(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        builder.Timeout = Math.Max(builder.Timeout, MinimumConnectionTimeoutSeconds);
        builder.CommandTimeout = Math.Max(builder.CommandTimeout, MinimumCommandTimeoutSeconds);
        if (builder.KeepAlive == 0)
        {
            builder.KeepAlive = 30;
        }

        if (builder.SslMode == SslMode.Prefer)
        {
            builder.SslMode = SslMode.Require;
        }

        return builder.ConnectionString;
    }

    public static bool UsesPoolerEndpoint(string connectionString) =>
        connectionString.Contains("pooler", StringComparison.OrdinalIgnoreCase);
}
