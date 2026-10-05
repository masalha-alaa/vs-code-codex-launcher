using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using CodexProjectLauncher.Models;

namespace CodexProjectLauncher.Services;

public sealed class VsCodeLauncher
{
    private static readonly string[] CodexStateFiles =
    [
        "auth.json",
        "config.toml",
        "environments.toml"
    ];

    public string GetCodexHome(ProjectEntry project, LauncherSettings settings) =>
        Path.Combine(settings.CodexHomesRoot, project.Id);

    public string GetVsCodeDataDirectory(ProjectEntry project, LauncherSettings settings) =>
        Path.Combine(settings.VsCodeDataRoot, project.Id);

    public string ResolveVsCodeExecutable(LauncherSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.VsCodeExecutablePath) &&
            File.Exists(settings.VsCodeExecutablePath))
        {
            return settings.VsCodeExecutablePath;
        }

        var candidates = new[]
        {
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Microsoft VS Code", "Code.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Microsoft VS Code", "Code.exe"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft VS Code", "Code.exe")
        };

        var match = candidates.FirstOrDefault(File.Exists);
        if (match is not null)
        {
            return match;
        }

        throw new FileNotFoundException(
            "VS Code was not found. Open Settings and select Code.exe manually.");
    }

    public void Launch(ProjectEntry project, LauncherSettings settings)
    {
        if (!Directory.Exists(project.FolderPath))
        {
            throw new DirectoryNotFoundException($"Project folder not found: {project.FolderPath}");
        }

        var codeExe = ResolveVsCodeExecutable(settings);
        var codexHome = GetCodexHome(project, settings);
        var userDataDirectory = GetVsCodeDataDirectory(project, settings);

        Directory.CreateDirectory(codexHome);
        Directory.CreateDirectory(userDataDirectory);

        if (settings.BootstrapCodexState)
        {
            BootstrapCodexState(codexHome);
        }

        SeedVsCodeUserData(userDataDirectory);
        EnsureCodexAgentHostDisabled(userDataDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = codeExe,
            UseShellExecute = false,
            WorkingDirectory = project.FolderPath
        };

        startInfo.Environment["CODEX_HOME"] = codexHome;
        startInfo.ArgumentList.Add("--user-data-dir");
        startInfo.ArgumentList.Add(userDataDirectory);
        startInfo.ArgumentList.Add("--new-window");
        startInfo.ArgumentList.Add(project.FolderPath);

        Process.Start(startInfo);
    }

    private static void BootstrapCodexState(string targetCodexHome)
    {
        var sourceCodexHome = Environment.GetEnvironmentVariable("CODEX_HOME");
        if (string.IsNullOrWhiteSpace(sourceCodexHome) ||
            PathsEqual(sourceCodexHome, targetCodexHome) ||
            !Directory.Exists(sourceCodexHome))
        {
            sourceCodexHome = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".codex");
        }

        if (!Directory.Exists(sourceCodexHome) || PathsEqual(sourceCodexHome, targetCodexHome))
        {
            return;
        }

        foreach (var fileName in CodexStateFiles)
        {
            var source = Path.Combine(sourceCodexHome, fileName);
            var target = Path.Combine(targetCodexHome, fileName);

            if (File.Exists(source) && !File.Exists(target))
            {
                File.Copy(source, target);
            }
        }
    }

    private static void SeedVsCodeUserData(string userDataDirectory)
    {
        var targetUserDirectory = Path.Combine(userDataDirectory, "User");
        Directory.CreateDirectory(targetUserDirectory);

        var sourceUserDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Code",
            "User");

        if (!Directory.Exists(sourceUserDirectory))
        {
            return;
        }

        CopyFileIfMissing(
            Path.Combine(sourceUserDirectory, "settings.json"),
            Path.Combine(targetUserDirectory, "settings.json"));
        CopyFileIfMissing(
            Path.Combine(sourceUserDirectory, "keybindings.json"),
            Path.Combine(targetUserDirectory, "keybindings.json"));

        var sourceSnippets = Path.Combine(sourceUserDirectory, "snippets");
        var targetSnippets = Path.Combine(targetUserDirectory, "snippets");
        if (Directory.Exists(sourceSnippets) && !Directory.Exists(targetSnippets))
        {
            CopyDirectory(sourceSnippets, targetSnippets);
        }
    }

    private static void EnsureCodexAgentHostDisabled(string userDataDirectory)
    {
        var userDirectory = Path.Combine(userDataDirectory, "User");
        Directory.CreateDirectory(userDirectory);
        var settingsPath = Path.Combine(userDirectory, "settings.json");

        JsonObject root;

        if (File.Exists(settingsPath))
        {
            try
            {
                var json = File.ReadAllText(settingsPath);
                root = JsonNode.Parse(
                           json,
                           new JsonNodeOptions(),
                           new JsonDocumentOptions
                           {
                               AllowTrailingCommas = true,
                               CommentHandling = JsonCommentHandling.Skip
                           }) as JsonObject
                       ?? new JsonObject();
            }
            catch
            {
                root = new JsonObject();
            }
        }
        else
        {
            root = new JsonObject();
        }

        root["chat.editor.codex.preferAgentHost"] = false;
        File.WriteAllText(
            settingsPath,
            root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void CopyFileIfMissing(string source, string target)
    {
        if (File.Exists(source) && !File.Exists(target))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(source, target);
        }
    }

    private static void CopyDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (var file in Directory.EnumerateFiles(sourceDirectory))
        {
            File.Copy(file, Path.Combine(targetDirectory, Path.GetFileName(file)), false);
        }

        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory))
        {
            CopyDirectory(directory, Path.Combine(targetDirectory, Path.GetFileName(directory)));
        }
    }

    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return string.Equals(
                Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
