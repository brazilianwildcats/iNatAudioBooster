namespace INatTrailCamAudioBooster;

internal static class AppPaths
{
    public static string BaseDirectory => AppContext.BaseDirectory;
    public static string AssetsDirectory => Path.Combine(BaseDirectory, "assets");
    public static string ToolsDirectory => Path.Combine(BaseDirectory, "tools", "ffmpeg", "bin");
    public static string FfmpegPath => Path.Combine(ToolsDirectory, "ffmpeg.exe");
    public static string FfprobePath => Path.Combine(ToolsDirectory, "ffprobe.exe");
    public static string LicensesDirectory => Path.Combine(BaseDirectory, "LICENSES");
    public static string DataDirectory => Path.Combine(BaseDirectory, "data");
    public static string LogsDirectory => Path.Combine(BaseDirectory, "logs");
    public static string SettingsFile => Path.Combine(DataDirectory, "settings.json");
    public static string LogFile { get; private set; } =
        Path.Combine(LogsDirectory, $"audio-booster-{DateTime.Now:yyyyMMdd-HHmmss}.log");

    public static void Initialize()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(LogsDirectory);
        LogFile = Path.Combine(LogsDirectory, $"audio-booster-{DateTime.Now:yyyyMMdd-HHmmss}.log");
    }
}

internal static class AppLog
{
    private static readonly object Sync = new();

    public static void Initialize()
    {
        AppPaths.Initialize();
        Write("============================================================");
        Write("iNat TrailCam Audio Booster V02");
        Write($"Diretório: {AppPaths.BaseDirectory}");
        Write($"Windows: {Environment.OSVersion}");
        Write($".NET: {Environment.Version}");
        Write("============================================================");
    }

    public static void Write(string message)
    {
        try
        {
            lock (Sync)
            {
                File.AppendAllText(
                    AppPaths.LogFile,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
            // O log nunca deve derrubar o aplicativo.
        }
    }

    public static void WriteException(string context, Exception ex)
    {
        Write($"{context}: {ex}");
    }
}
