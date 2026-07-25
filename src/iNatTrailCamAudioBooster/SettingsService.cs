using System.Text.Json;

namespace INatTrailCamAudioBooster;

internal static class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(AppPaths.SettingsFile))
                return new AppSettings();

            var json = File.ReadAllText(AppPaths.SettingsFile);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            AppLog.WriteException("Não foi possível ler as configurações", ex);
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataDirectory);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(AppPaths.SettingsFile, json);
        }
        catch (Exception ex)
        {
            AppLog.WriteException("Não foi possível salvar as configurações", ex);
        }
    }
}
