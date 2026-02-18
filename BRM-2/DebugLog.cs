namespace BRM_2;

/// <summary>
/// Simple file-based debug logger for MAUI apps where console output isn't visible.
/// Logs to: ~/Documents/BRM-2-Debug.log
/// </summary>
public static class DebugLog
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Personal),
        "BRM-2-Debug.log"
    );

    static DebugLog()
    {
        // Clear log on app start
        try
        {
            File.Delete(LogPath);
        }
        catch { }
    }

    public static void WriteLine(string message)
    {
        try
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logLine = $"[{timestamp}] {message}";
            File.AppendAllText(LogPath, logLine + Environment.NewLine);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DebugLog error: {ex}");
        }
    }

    public static string GetLogPath() => LogPath;
}
