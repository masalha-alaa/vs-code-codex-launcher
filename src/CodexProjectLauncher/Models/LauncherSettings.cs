namespace CodexProjectLauncher.Models;

public sealed class LauncherSettings
{
    public string VsCodeExecutablePath { get; set; } = string.Empty;
    public string CodexHomesRoot { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CodexHomes");
    public string VsCodeDataRoot { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VSCodeInstances");
    public bool BootstrapCodexState { get; set; } = true;
}
