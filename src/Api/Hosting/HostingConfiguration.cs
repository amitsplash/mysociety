namespace MySociety.Api.Hosting;

internal static class HostingConfiguration
{
    public static string SummarizeForLog(string? configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return "(empty)";
        }

        return SummarizeConnectionString(configured);
    }

    public static string SummarizeConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return "missing";
        }

        var redacted = connectionString;
        foreach (var key in new[] { "Password=", "Pwd=" })
        {
            var index = redacted.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            var valueStart = index + key.Length;
            var valueEnd = redacted.IndexOf(';', valueStart);
            if (valueEnd < 0)
            {
                valueEnd = redacted.Length;
            }

            redacted = string.Concat(redacted.AsSpan(0, valueStart), "***", redacted.AsSpan(valueEnd));
        }

        if (redacted.Length > 120)
        {
            redacted = redacted[..120] + "...";
        }

        return redacted;
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
