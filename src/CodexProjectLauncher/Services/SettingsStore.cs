using System.Text.Json;
using CodexProjectLauncher.Models;

namespace CodexProjectLauncher.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string SettingsFilePath => Path.Combine(ProjectStore.AppDataDirectory, "settings.json");

    public LauncherSettings Load()
    {
        Directory.CreateDirectory(ProjectStore.AppDataDirectory);

        if (!File.Exists(SettingsFilePath))
        {
            return new LauncherSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<LauncherSettings>(json, JsonOptions)
                           ?? new LauncherSettings();

            settings.CodexHomesRoot = string.IsNullOrWhiteSpace(settings.CodexHomesRoot)
                ? new LauncherSettings().CodexHomesRoot
                : settings.CodexHomesRoot;
            settings.VsCodeDataRoot = string.IsNullOrWhiteSpace(settings.VsCodeDataRoot)
                ? new LauncherSettings().VsCodeDataRoot
                : settings.VsCodeDataRoot;

            return settings;
        }
        catch
        {
            return new LauncherSettings();
        }
    }

    public void Save(LauncherSettings settings)
    {
        Directory.CreateDirectory(ProjectStore.AppDataDirectory);
        File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
