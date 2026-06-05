namespace MySociety.Api.Hosting;

/// <summary>
/// Writes startup markers before Serilog/IIS are ready — check Kudu LogFiles if stdout is empty.
/// </summary>
internal static class EmergencyStartupLog
{
    private static readonly string[] MarkerPaths =
    [
        "/home/LogFiles/startup-marker.txt",
        @"D:\home\LogFiles\startup-marker.txt",
        Path.Combine(Path.GetTempPath(), "mysociety-startup-marker.txt"),
    ];

    public static void Mark(string message)
    {
        var line = $"{DateTime.UtcNow:O} {message}{Environment.NewLine}";
        foreach (var path in MarkerPaths)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(path, line);
            }
            catch
            {
                // Best effort only.
            }
        }
    }
}
