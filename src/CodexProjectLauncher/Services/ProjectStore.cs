using System.Text.Json;
using CodexProjectLauncher.Models;

namespace CodexProjectLauncher.Services;

public sealed class ProjectStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string AppDataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CodexProjectLauncher");

    private static string ProjectsFilePath => Path.Combine(AppDataDirectory, "projects.json");

    public IReadOnlyList<ProjectEntry> Load()
    {
        Directory.CreateDirectory(AppDataDirectory);

        if (!File.Exists(ProjectsFilePath))
        {
            return Array.Empty<ProjectEntry>();
        }

        try
        {
            var json = File.ReadAllText(ProjectsFilePath);
            return JsonSerializer.Deserialize<List<ProjectEntry>>(json, JsonOptions)
                   ?? new List<ProjectEntry>();
        }
        catch
        {
            return Array.Empty<ProjectEntry>();
        }
    }

    public void Save(IEnumerable<ProjectEntry> projects)
    {
        Directory.CreateDirectory(AppDataDirectory);

        var tempPath = ProjectsFilePath + ".tmp";
        var json = JsonSerializer.Serialize(projects, JsonOptions);
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, ProjectsFilePath, true);
    }
}
