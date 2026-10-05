using System.Windows;
using CodexProjectLauncher.Models;
using CodexProjectLauncher.Services;
using Microsoft.Win32;

namespace CodexProjectLauncher;

public partial class SettingsWindow : Window
{
    private readonly VsCodeLauncher _vsCodeLauncher = new();

    public SettingsWindow(LauncherSettings settings)
    {
        InitializeComponent();

        Settings = new LauncherSettings
        {
            VsCodeExecutablePath = settings.VsCodeExecutablePath,
            CodexHomesRoot = settings.CodexHomesRoot,
            VsCodeDataRoot = settings.VsCodeDataRoot,
            BootstrapCodexState = settings.BootstrapCodexState
        };

        if (string.IsNullOrWhiteSpace(Settings.VsCodeExecutablePath))
        {
            try
            {
                Settings.VsCodeExecutablePath = _vsCodeLauncher.ResolveVsCodeExecutable(Settings);
            }
            catch
            {
            }
        }

        VsCodePathBox.Text = Settings.VsCodeExecutablePath;
        CodexRootBox.Text = Settings.CodexHomesRoot;
        VsCodeDataRootBox.Text = Settings.VsCodeDataRoot;
        BootstrapCodexStateCheckBox.IsChecked = Settings.BootstrapCodexState;
    }

    public LauncherSettings Settings { get; private set; }

    private void BrowseVsCodeButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select Code.exe",
            Filter = "Visual Studio Code (Code.exe)|Code.exe|Executable files (*.exe)|*.exe",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            VsCodePathBox.Text = dialog.FileName;
        }
    }

    private void BrowseCodexRootButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = BrowseForFolder("Select Codex homes root", CodexRootBox.Text);
        if (selected is not null)
        {
            CodexRootBox.Text = selected;
        }
    }

    private void BrowseVsCodeDataRootButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = BrowseForFolder("Select VS Code data root", VsCodeDataRootBox.Text);
        if (selected is not null)
        {
            VsCodeDataRootBox.Text = selected;
        }
    }

    private static string? BrowseForFolder(string title, string currentPath)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false
        };

        if (Directory.Exists(currentPath))
        {
            dialog.InitialDirectory = currentPath;
        }

        return dialog.ShowDialog() == true
            ? dialog.FolderName
            : null;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var codePath = VsCodePathBox.Text.Trim();
        var codexRoot = CodexRootBox.Text.Trim();
        var dataRoot = VsCodeDataRootBox.Text.Trim();

        if (codePath.Length > 0 && !File.Exists(codePath))
        {
            MessageBox.Show(this, "The selected VS Code executable does not exist.", "Invalid path", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (codexRoot.Length == 0 || dataRoot.Length == 0)
        {
            MessageBox.Show(this, "Codex and VS Code data root paths cannot be empty.", "Invalid path", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Settings = new LauncherSettings
        {
            VsCodeExecutablePath = codePath,
            CodexHomesRoot = Path.GetFullPath(Environment.ExpandEnvironmentVariables(codexRoot)),
            VsCodeDataRoot = Path.GetFullPath(Environment.ExpandEnvironmentVariables(dataRoot)),
            BootstrapCodexState = BootstrapCodexStateCheckBox.IsChecked == true
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
