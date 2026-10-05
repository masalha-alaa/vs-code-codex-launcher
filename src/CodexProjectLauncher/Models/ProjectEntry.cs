namespace CodexProjectLauncher.Models;

public sealed class ProjectEntry
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public DateTime? LastOpenedUtc { get; set; }
}
