using Microsoft.Data.Sqlite;

namespace MySociety.Api.Hosting;

internal static class HostingConfiguration
{
    /// <summary>
    /// Resolves SQLite path for Azure App Service (persistent /home/data or D:\home\data).
    /// Accepts bare paths like "/home/data/mysociety.db" (common Azure Portal mistake).
    /// </summary>
    public static string ResolveSqliteConnectionString(string? configured, bool usePersistentStorage)
    {
        var connectionString = NormalizeConnectionStringInput(configured);

        try
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(builder.DataSource) || builder.DataSource == ":memory:")
            {
                return connectionString;
            }

            builder.DataSource = NormalizeDataSourcePath(builder.DataSource);

            // Wait up to 30s for SQLite locks instead of failing immediately under concurrent reads.
            if (builder.DefaultTimeout <= 0)
            {
                builder.DefaultTimeout = 30;
            }

            if (usePersistentStorage && !IsUnderAppServiceHome(builder.DataSource))
            {
                var fileName = Path.GetFileName(builder.DataSource);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = "mysociety.db";
                }

                builder.DataSource = Path.Combine(ResolveAppServiceHome(), "data", fileName);
            }

            return builder.ConnectionString;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Invalid SQLite connection string. Use 'Data Source=/home/data/mysociety.db' (Linux) " +
                $"or set ConnectionStrings__DefaultConnection in Azure App Settings. ({ex.Message})",
                ex);
        }
    }

    /// <summary>
    /// Safe summary for logs (no secrets).
    /// </summary>
    public static string SummarizeForLog(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return "(empty — using default)";
        }

        var trimmed = configured.Trim();
        if (trimmed.Length > 80)
        {
            trimmed = trimmed[..80] + "...";
        }

        return trimmed;
    }

    public static string ResolveLogDirectory(string contentRoot)
    {
        var candidates = new List<string>();

        var home = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrWhiteSpace(home))
        {
            candidates.Add(Path.Combine(NormalizeHomePath(home), "LogFiles", "Application"));
        }

        var homeExpanded = Environment.GetEnvironmentVariable("HOME_EXPANDED");
        if (!string.IsNullOrWhiteSpace(homeExpanded))
        {
            candidates.Add(Path.Combine(NormalizeHomePath(homeExpanded), "LogFiles", "Application"));
        }

        if (OperatingSystem.IsWindows())
        {
            candidates.Add(Path.Combine(@"D:\home", "LogFiles", "Application"));
        }
        else
        {
            candidates.Add(Path.Combine("/home", "LogFiles", "Application"));
        }

        candidates.Add(Path.Combine(contentRoot, "logs"));

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (TryEnsureDirectory(candidate))
            {
                return candidate;
            }
        }

        return contentRoot;
    }

    public static void EnsureSqliteDirectoryExists(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource) || builder.DataSource == ":memory:")
        {
            return;
        }

        var fullPath = Path.GetFullPath(builder.DataSource);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException($"Cannot determine directory for SQLite file: {builder.DataSource}");
        }

        Directory.CreateDirectory(directory);

        if (!Directory.Exists(directory))
        {
            throw new InvalidOperationException($"SQLite data directory was not created: {directory}");
        }
    }

    public static string DescribeSqlitePath(string connectionString)
    {
        try
        {
            var builder = new SqliteConnectionStringBuilder(connectionString);
            var fullPath = Path.GetFullPath(builder.DataSource);
            var dir = Path.GetDirectoryName(fullPath) ?? "";
            var dirExists = Directory.Exists(dir);
            var fileExists = File.Exists(fullPath);
            return $"path={fullPath}; dirExists={dirExists}; fileExists={fileExists}";
        }
        catch (Exception ex)
        {
            return $"unreadable ({ex.Message})";
        }
    }

    private static string NormalizeConnectionStringInput(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return "Data Source=mysociety.db";
        }

        var trimmed = configured.Trim().Trim('"', '\'');

        if (LooksLikeSqliteConnectionString(trimmed))
        {
            return trimmed;
        }

        // Bare file path (Azure Portal often sets only the path).
        return $"Data Source={trimmed}";
    }

    private static bool LooksLikeSqliteConnectionString(string value) =>
        value.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
        || value.Contains("Filename=", StringComparison.OrdinalIgnoreCase)
        || value.Contains("DataSource=", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeDataSourcePath(string dataSource)
    {
        // Windows path configured on Linux App Service.
        if (!OperatingSystem.IsWindows()
            && (dataSource.StartsWith(@"D:\", StringComparison.OrdinalIgnoreCase)
                || dataSource.StartsWith("D:/", StringComparison.OrdinalIgnoreCase)))
        {
            var relative = dataSource[2..].Replace('\\', '/').TrimStart('/');
            return Path.Combine("/home", relative).Replace('\\', '/');
        }

        // Linux path configured on Windows App Service.
        if (OperatingSystem.IsWindows() && dataSource.StartsWith("/home/", StringComparison.Ordinal))
        {
            var relative = dataSource["/home/".Length..].Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(ResolveAppServiceHome(), relative);
        }

        return dataSource;
    }

    private static bool IsUnderAppServiceHome(string dataSource)
    {
        if (dataSource.StartsWith("/home/", StringComparison.Ordinal))
        {
            return true;
        }

        if (OperatingSystem.IsWindows())
        {
            var normalized = dataSource.Replace('/', '\\');
            return normalized.StartsWith(@"D:\home\", StringComparison.OrdinalIgnoreCase)
                || normalized.StartsWith(@"D:\home", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static string ResolveAppServiceHome()
    {
        var home = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrWhiteSpace(home))
        {
            return NormalizeHomePath(home);
        }

        var homeExpanded = Environment.GetEnvironmentVariable("HOME_EXPANDED");
        if (!string.IsNullOrWhiteSpace(homeExpanded))
        {
            return NormalizeHomePath(homeExpanded);
        }

        return OperatingSystem.IsWindows() ? @"D:\home" : "/home";
    }

    private static string NormalizeHomePath(string home)
    {
        if (home.StartsWith("/c/", StringComparison.OrdinalIgnoreCase) ||
            home.StartsWith("/C/", StringComparison.Ordinal))
        {
            return @"C:\";
        }

        if (home.StartsWith("/d/", StringComparison.OrdinalIgnoreCase) ||
            home.StartsWith("/D/", StringComparison.Ordinal))
        {
            return @"D:\";
        }

        return home.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool TryEnsureDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return Directory.Exists(path);
        }
        catch
        {
            return false;
        }
    }
}
