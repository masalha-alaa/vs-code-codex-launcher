using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CodexProjectLauncher.Models;
using CodexProjectLauncher.Services;

namespace CodexProjectLauncher;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<ProjectEntry> _projects = [];
    private readonly ProjectStore _projectStore = new();
    private readonly SettingsStore _settingsStore = new();
    private readonly VsCodeLauncher _vsCodeLauncher = new();
    private readonly ICollectionView _projectsView;
    private LauncherSettings _settings;

    public MainWindow()
    {
        InitializeComponent();

        _settings = _settingsStore.Load();
        foreach (var project in _projectStore.Load())
        {
            _projects.Add(project);
        }

        _projectsView = CollectionViewSource.GetDefaultView(_projects);
        _projectsView.Filter = FilterProject;
        ProjectsList.ItemsSource = _projectsView;

        UpdateProjectCount();
        SelectFirstProject();
        StateChanged += (_, _) => UpdateMaximizeGlyph();
    }

    private ProjectEntry? SelectedProject => ProjectsList.SelectedItem as ProjectEntry;

    private bool FilterProject(object item)
    {
        if (item is not ProjectEntry project)
        {
            return false;
        }

        var query = SearchBox.Text.Trim();
        return query.Length == 0 ||
               project.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               project.FolderPath.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text)
            ? Visibility.Visible
            : Visibility.Collapsed;

        _projectsView?.Refresh();
        UpdateProjectCount();

        if (ProjectsList.SelectedItem is null && ProjectsList.Items.Count > 0)
        {
            ProjectsList.SelectedIndex = 0;
        }
    }

    private void ProjectsList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        RenderSelectedProject();

    private void ProjectsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SelectedProject is not null)
        {
            OpenSelectedProject();
        }
    }

    private void AddProjectButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select a project folder",
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true ||
            string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            return;
        }

        var folderPath = Path.GetFullPath(dialog.FolderName)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var existing = _projects.FirstOrDefault(p => PathsEqual(p.FolderPath, folderPath));
        if (existing is not null)
        {
            ProjectsList.SelectedItem = existing;
            ProjectsList.ScrollIntoView(existing);
            SetStatus("Project already added.", true);
            return;
        }

        var displayName = new DirectoryInfo(folderPath).Name;
        var project = new ProjectEntry
        {
            DisplayName = displayName,
            FolderPath = folderPath,
            Id = BuildUniqueProjectId(displayName, folderPath)
        };

        _projects.Add(project);
        SaveProjects();
        _projectsView.Refresh();
        ProjectsList.SelectedItem = project;
        ProjectsList.ScrollIntoView(project);
        UpdateProjectCount();
        SetStatus("Project added.", true);
    }

    private void OpenSelectedButton_Click(object sender, RoutedEventArgs e) => OpenSelectedProject();

    private void OpenSelectedProject()
    {
        var project = SelectedProject;
        if (project is null)
        {
            return;
        }

        try
        {
            SetStatus("Opening VS Code…", true);
            _vsCodeLauncher.Launch(project, _settings);
            project.LastOpenedUtc = DateTime.UtcNow;
            SaveProjects();
            RenderSelectedProject();
            SetStatus("VS Code launched.", true);
        }
        catch (Exception ex)
        {
            SetStatus("Launch failed.", false);
            MessageBox.Show(
                this,
                ex.Message,
                "Could not open VS Code",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var project = SelectedProject;
        if (project is null)
        {
            return;
        }

        if (!Directory.Exists(project.FolderPath))
        {
            MessageBox.Show(
                this,
                $"Project folder not found:\n{project.FolderPath}",
                "Folder not found",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            ArgumentList = { project.FolderPath },
            UseShellExecute = true
        });
    }

    private void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        var project = SelectedProject;
        if (project is null)
        {
            return;
        }

        var dialog = new TextPromptWindow(
            "Rename Project",
            "Project name",
            project.DisplayName)
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var newName = dialog.Value.Trim();
        if (newName.Length == 0)
        {
            return;
        }

        project.DisplayName = newName;
        SaveProjects();
        _projectsView.Refresh();
        RenderSelectedProject();
        SetStatus("Project renamed.", true);
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        var project = SelectedProject;
        if (project is null)
        {
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Remove '{project.DisplayName}' from the launcher?\n\nIts project files, Codex history, and VS Code data will be kept.",
            "Remove Project",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        var index = _projects.IndexOf(project);
        _projects.Remove(project);
        SaveProjects();
        _projectsView.Refresh();
        UpdateProjectCount();

        if (ProjectsList.Items.Count > 0)
        {
            ProjectsList.SelectedIndex = Math.Min(index, ProjectsList.Items.Count - 1);
        }
        else
        {
            RenderSelectedProject();
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedId = SelectedProject?.Id;
        _projects.Clear();

        foreach (var project in _projectStore.Load())
        {
            _projects.Add(project);
        }

        _settings = _settingsStore.Load();
        _projectsView.Refresh();
        UpdateProjectCount();

        if (selectedId is not null)
        {
            ProjectsList.SelectedItem = _projects.FirstOrDefault(p => p.Id == selectedId);
        }

        if (ProjectsList.SelectedItem is null)
        {
            SelectFirstProject();
        }

        SetStatus("Refreshed.", true);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_settings)
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        _settings = dialog.Settings;
        _settingsStore.Save(_settings);
        RenderSelectedProject();
        SetStatus("Settings saved.", true);
    }

    private void RenderSelectedProject()
    {
        var project = SelectedProject;
        var hasProject = project is not null;

        DetailsPanel.Visibility = hasProject ? Visibility.Visible : Visibility.Collapsed;
        EmptyState.Visibility = hasProject ? Visibility.Collapsed : Visibility.Visible;

        if (project is null)
        {
            return;
        }

        ProjectNameText.Text = project.DisplayName;
        ProjectPathText.Text = project.FolderPath;
        CodexHomeText.Text = _vsCodeLauncher.GetCodexHome(project, _settings);
        VsCodeDataText.Text = _vsCodeLauncher.GetVsCodeDataDirectory(project, _settings);
        LastOpenedText.Text = project.LastOpenedUtc is null
            ? "Never opened"
            : $"Last opened: {project.LastOpenedUtc.Value.ToLocalTime():ddd, dd MMM yyyy HH:mm}";

        var folderExists = Directory.Exists(project.FolderPath);
        SetStatus(folderExists ? "Ready" : "Project folder is missing.", folderExists);
    }

    private void SelectFirstProject()
    {
        if (ProjectsList.Items.Count > 0)
        {
            ProjectsList.SelectedIndex = 0;
        }
        else
        {
            RenderSelectedProject();
        }
    }

    private void SaveProjects() => _projectStore.Save(_projects);

    private string BuildUniqueProjectId(string displayName, string folderPath)
    {
        var slug = Slugify(displayName);
        if (slug.Length == 0)
        {
            slug = "project";
        }

        if (_projects.All(p => !string.Equals(p.Id, slug, StringComparison.OrdinalIgnoreCase)))
        {
            return slug;
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(folderPath)))
            .ToLowerInvariant()[..8];
        return $"{slug}-{hash}";
    }

    private static string Slugify(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousDash = false;

        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch) || ch is '-' or '_')
            {
                builder.Append(ch);
                previousDash = false;
            }
            else if (!previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        return builder.ToString().Trim('-');
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

    private void UpdateProjectCount()
    {
        var count = ProjectsList.Items.Count;
        ProjectCountText.Text = count == 1 ? "1 project" : $"{count} projects";
    }

    private void SetStatus(string text, bool healthy)
    {
        StatusText.Text = text;
        StatusDot.Fill = healthy
            ? new SolidColorBrush(Color.FromRgb(87, 197, 142))
            : new SolidColorBrush(Color.FromRgb(217, 107, 107));
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void UpdateMaximizeGlyph()
    {
        MaximizeButton.Content = WindowState == WindowState.Maximized ? "❐" : "□";
        MaximizeButton.ToolTip = WindowState == WindowState.Maximized ? "Restore" : "Maximize";
    }
}
