namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;

using CLIHub.Core.Models;
using CLIHub.Core.Scanning;
using CLIHub.Launching;
using CLIHub.Settings;

/// <summary>
///   Watched directory and scanning settings section: watched roots, ignored patterns, scan depth,
///   discovery mode, immediate-apply logic, watcher status, and disposal.
/// </summary>
public sealed class WatchedDirectoryEditor : ObservableObject, IDisposable
{
    private static readonly DiscoveryModeOption[] DiscoveryModeOptions =
    {
        new(ProjectDiscoveryMode.Git, "Git only"),
        new(ProjectDiscoveryMode.Agents, "Agents only"),
        new(ProjectDiscoveryMode.Both, "Git and agents")
    };

    private static readonly ScanDepthOption[] ScanDepthChoices = Enumerable
        .Range(
            ProjectScanningDefaults.MinScanDepth,
            ProjectScanningDefaults.MaxScanDepth - ProjectScanningDefaults.MinScanDepth + 1)
        .Select(depth => new ScanDepthOption(
            depth,
            depth == ProjectScanningDefaults.DefaultScanDepth
                ? $"{depth} levels (default)"
                : depth == 1 ? "1 level" : $"{depth} levels"))
        .ToArray();

    private readonly SettingsApplicationService settingsApplication;
    private readonly IProjectScanner projectScanner;
    private readonly IProjectDialogService dialogs;
    private readonly Action<string?> setValidationError;
    private readonly List<WatchedDirectoryRow> observedWatchedDirectoryRows = new();

    private string scanDepthText = ProjectScanningDefaults.DefaultScanDepth.ToString();
    private DiscoveryModeOption selectedDiscoveryMode = DiscoveryModeOptions[2];
    private string newIgnoredPatternText = string.Empty;
    private string newWatchedDirectoryText = string.Empty;
    private string? watchedDirectoryError;
    private int discoveredProjectCount;
    private int manualProjectCount;
    private int scannedDirectoryCount;
    private string watcherStateLabel = "Off";
    private string watcherDetailText = "No watched roots configured";
    private bool disposed;

    /// <summary>
    ///   Creates the scanning editor.
    /// </summary>
    /// <param name="settingsApplication"> Typed Settings draft and application service. </param>
    /// <param name="projectScanner"> Scanner whose status is shown on the Project Scanning page. </param>
    /// <param name="dialogs"> Folder picker used to add watched directories. </param>
    /// <param name="setValidationError"> Reports a shared validation error to the owning view model. </param>
    public WatchedDirectoryEditor(SettingsApplicationService settingsApplication,
                                  IProjectScanner projectScanner,
                                  IProjectDialogService dialogs,
                                  Action<string?> setValidationError)
    {
        this.settingsApplication = settingsApplication;
        this.projectScanner = projectScanner ?? throw new ArgumentNullException(nameof(projectScanner));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.setValidationError = setValidationError ?? throw new ArgumentNullException(nameof(setValidationError));

        this.BrowseWatchedDirectoryCommand = new RelayCommand(this.BrowseWatchedDirectory);
        this.AddWatchedDirectoryFromPickerCommand = new RelayCommand(this.AddWatchedDirectoryFromPicker);
        this.AddWatchedDirectoryCommand = new RelayCommand(this.AddWatchedDirectory);
        this.RemoveWatchedDirectoryCommand = new RelayCommand<string>(this.RemoveWatchedDirectory);
        this.ScanWatchedDirectoryCommand = new RelayCommand<string>(this.ScanWatchedDirectory);
        this.AddIgnoredPatternCommand = new RelayCommand(this.AddIgnoredPattern);
        this.RemoveIgnoredPatternCommand = new RelayCommand<string>(this.RemoveIgnoredPattern);
        this.ScanNowCommand = new RelayCommand(() => this.projectScanner.Reload());

        this.WatchedDirectories.CollectionChanged += this.OnWatchedDirectoriesChanged;
        this.projectScanner.StatusChanged += this.OnScannerStatusChanged;
        this.ApplyScannerStatus(this.projectScanner.Status);
    }

    /// <summary>
    ///   Watched root directories edited on the Project Scanning page.
    /// </summary>
    public ObservableCollection<WatchedDirectoryRow> WatchedDirectories { get; } = new();

    /// <summary>
    ///   The absolute directory path pending registration.
    /// </summary>
    public string NewWatchedDirectoryText
    {
        get => this.newWatchedDirectoryText;
        set
        {
            if (this.SetProperty(ref this.newWatchedDirectoryText, value))
            {
                this.WatchedDirectoryError = null;
            }
        }
    }

    /// <summary>
    ///   The validation error shown under the watched-directory entry, or null.
    /// </summary>
    public string? WatchedDirectoryError
    {
        get => this.watchedDirectoryError;
        private set
        {
            if (this.SetProperty(ref this.watchedDirectoryError, value))
            {
                this.OnPropertyChanged(nameof(this.HasWatchedDirectoryError));
            }
        }
    }

    /// <summary>
    ///   True when <see cref="WatchedDirectoryError" /> has a message.
    /// </summary>
    public bool HasWatchedDirectoryError => !string.IsNullOrEmpty(this.watchedDirectoryError);

    /// <summary>
    ///   The watched-root count shown next to the section header.
    /// </summary>
    public string WatchedRootsSummary => this.WatchedDirectories.Count switch
    {
        0 => "No roots monitored",
        1 => "1 root monitored",
        var count => $"{count} roots monitored"
    };

    /// <summary>
    ///   The ignored directory-name patterns edited on the Project Scanning page.
    /// </summary>
    public ObservableCollection<string> IgnoredPatterns { get; } = new();

    /// <summary>
    ///   The ignored pattern pending to be added.
    /// </summary>
    public string NewIgnoredPatternText
    {
        get => this.newIgnoredPatternText;
        set => this.SetProperty(ref this.newIgnoredPatternText, value);
    }

    /// <summary>
    ///   The repository scan depth as text.
    /// </summary>
    public string ScanDepthText
    {
        get => this.scanDepthText;
        set
        {
            if (this.SetProperty(ref this.scanDepthText, value))
            {
                this.OnPropertyChanged(nameof(this.SelectedScanDepth));
            }
        }
    }

    /// <summary>
    ///   The scan depth options offered by the selector.
    /// </summary>
    public IReadOnlyList<ScanDepthOption> ScanDepthOptions => ScanDepthChoices;

    /// <summary>
    ///   The selected scan depth option.
    /// </summary>
    public ScanDepthOption SelectedScanDepth
    {
        get => ScanDepthChoices.FirstOrDefault(option =>
                   string.Equals(option.Value.ToString(), this.scanDepthText, StringComparison.Ordinal))
               ?? ScanDepthChoices.First(option => option.Value == ProjectScanningDefaults.DefaultScanDepth);
        set
        {
            if (value is not null && !string.Equals(this.scanDepthText, value.Value.ToString(), StringComparison.Ordinal))
            {
                this.ScanDepthText = value.Value.ToString();
                this.ApplyScanningSettings();
            }
        }
    }

    /// <summary>
    ///   The repository discovery mode options offered by the selector.
    /// </summary>
    public IReadOnlyList<DiscoveryModeOption> DiscoveryModes => DiscoveryModeOptions;

    /// <summary>
    ///   The selected repository discovery mode.
    /// </summary>
    public DiscoveryModeOption SelectedDiscoveryMode
    {
        get => this.selectedDiscoveryMode;
        set
        {
            if (value is not null && this.SetProperty(ref this.selectedDiscoveryMode, value))
            {
                this.ApplyScanningSettings();
            }
        }
    }

    /// <summary>
    ///   The number of discovered projects registered from the last scan.
    /// </summary>
    public int DiscoveredProjectCount
    {
        get => this.discoveredProjectCount;
        private set
        {
            if (this.SetProperty(ref this.discoveredProjectCount, value))
            {
                this.OnProjectTotalsChanged();
            }
        }
    }

    /// <summary>
    ///   The number of projects that were added manually rather than discovered.
    /// </summary>
    public int ManualProjectCount
    {
        get => this.manualProjectCount;
        private set
        {
            if (this.SetProperty(ref this.manualProjectCount, value))
            {
                this.OnProjectTotalsChanged();
            }
        }
    }

    /// <summary>
    ///   The total number of registered projects: discovered plus manually added.
    /// </summary>
    public int TotalProjectCount => this.DiscoveredProjectCount + this.ManualProjectCount;

    /// <summary>
    ///   The provenance split shown under the total project count, e.g. "31 discovered · 4 manual".
    /// </summary>
    public string ProjectMixSummary =>
        $"{this.DiscoveredProjectCount} discovered · {this.ManualProjectCount} manual";

    /// <summary>
    ///   The number of folders the last scan examined across the watched roots.
    /// </summary>
    public int ScannedDirectoryCount
    {
        get => this.scannedDirectoryCount;
        private set => this.SetProperty(ref this.scannedDirectoryCount, value);
    }

    /// <summary>
    ///   The short watcher state shown in the summary card: Off, Idle, Scanning, Watching, or Error.
    /// </summary>
    public string WatcherStateLabel
    {
        get => this.watcherStateLabel;
        private set => this.SetProperty(ref this.watcherStateLabel, value);
    }

    /// <summary>
    ///   The watcher detail shown under the state, e.g. the last scan time.
    /// </summary>
    public string WatcherDetailText
    {
        get => this.watcherDetailText;
        private set => this.SetProperty(ref this.watcherDetailText, value);
    }

    /// <summary>
    ///   Opens the folder picker and fills the watched-directory entry.
    /// </summary>
    public RelayCommand BrowseWatchedDirectoryCommand { get; }

    /// <summary>
    ///   Opens the folder picker and registers the selected folder as a watched root.
    /// </summary>
    public RelayCommand AddWatchedDirectoryFromPickerCommand { get; }

    /// <summary>
    ///   Registers the entered absolute folder path as a watched root.
    /// </summary>
    public RelayCommand AddWatchedDirectoryCommand { get; }

    /// <summary>
    ///   Removes the watched root passed as the command parameter.
    /// </summary>
    public RelayCommand<string> RemoveWatchedDirectoryCommand { get; }

    /// <summary>
    ///   Rescans only the watched root passed as the command parameter.
    /// </summary>
    public RelayCommand<string> ScanWatchedDirectoryCommand { get; }

    /// <summary>
    ///   Adds the entered ignored pattern.
    /// </summary>
    public RelayCommand AddIgnoredPatternCommand { get; }

    /// <summary>
    ///   Removes the ignored pattern passed as the command parameter.
    /// </summary>
    public RelayCommand<string> RemoveIgnoredPatternCommand { get; }

    /// <summary>
    ///   Re-scans the saved watched roots.
    /// </summary>
    public RelayCommand ScanNowCommand { get; }

    private string ScannedCoverageSuffix => this.ScannedDirectoryCount switch
    {
        0 => string.Empty,
        1 => " · 1 folder",
        var count => $" · {count} folders"
    };

    /// <summary>
    ///   Loads the scanning settings from a stored draft.
    /// </summary>
    /// <param name="draft"> The stored Settings draft. </param>
    public void Load(SettingsDraft draft)
    {
        this.WatchedDirectories.Clear();
        var defaultScanDepth = ProjectScanningDefaults.NormalizeScanDepth(draft.ScanDepth);
        foreach (var directory in draft.WatchedDirectories ?? Array.Empty<string>())
        {
            var settings = draft.WatchedDirectorySettings is not null
                && draft.WatchedDirectorySettings.TryGetValue(directory, out var stored)
                    ? stored
                    : null;

            this.WatchedDirectories.Add(this.CreateWatchedDirectoryRow(
                directory,
                settings?.ScanDepth ?? defaultScanDepth,
                settings?.DiscoveryMode ?? draft.DiscoveryMode));
        }

        this.IgnoredPatterns.Clear();
        foreach (var pattern in draft.IgnoredPatterns ?? Array.Empty<string>())
        {
            this.IgnoredPatterns.Add(pattern);
        }

        this.scanDepthText = draft.ScanDepth.ToString();
        this.selectedDiscoveryMode = DiscoveryModeOptions.FirstOrDefault(o => o.Mode == draft.DiscoveryMode)
            ?? DiscoveryModeOptions[2];
        this.newIgnoredPatternText = string.Empty;
        this.newWatchedDirectoryText = string.Empty;
        this.watchedDirectoryError = null;
        this.ApplyScannerStatus(this.projectScanner.Status);

        this.OnPropertyChanged(nameof(this.ScanDepthText));
        this.OnPropertyChanged(nameof(this.SelectedScanDepth));
        this.OnPropertyChanged(nameof(this.SelectedDiscoveryMode));
        this.OnPropertyChanged(nameof(this.NewIgnoredPatternText));
        this.OnPropertyChanged(nameof(this.NewWatchedDirectoryText));
        this.OnPropertyChanged(nameof(this.WatchedDirectoryError));
        this.OnPropertyChanged(nameof(this.HasWatchedDirectoryError));
        this.OnPropertyChanged(nameof(this.WatchedRootsSummary));
        this.OnPropertyChanged(nameof(this.DiscoveredProjectCount));
        this.OnPropertyChanged(nameof(this.ManualProjectCount));
        this.OnPropertyChanged(nameof(this.TotalProjectCount));
        this.OnPropertyChanged(nameof(this.ProjectMixSummary));
        this.OnPropertyChanged(nameof(this.WatcherStateLabel));
        this.OnPropertyChanged(nameof(this.WatcherDetailText));
    }

    /// <summary>
    ///   Creates the input for persistence and scanner reconfiguration.
    /// </summary>
    /// <returns> The scanning values currently shown on the page. </returns>
    public ScanningSettingsInput CreateInput() => new(
        this.WatchedDirectories.Select(row => row.Path).ToList(),
        this.SelectedScanDepth.Value,
        this.IgnoredPatterns.ToList(),
        this.SelectedDiscoveryMode.Mode,
        this.WatchedDirectories.ToDictionary(
            row => row.Path,
            row => new WatchedDirectoryScanSettings(row.SelectedDepth.Value, row.SelectedDiscovery.Mode),
            StringComparer.OrdinalIgnoreCase));

    /// <summary>
    ///   Applies the scanning settings currently shown on the page and rescans in the background.
    ///   The remaining settings still go through Save/Cancel.
    /// </summary>
    /// <returns> True when the settings were persisted and the scanner was reconfigured. </returns>
    public bool ApplyScanningSettings()
    {
        if (this.disposed)
        {
            return true;
        }

        var result = this.settingsApplication.ApplyScanning(this.CreateInput());
        if (result.Success)
        {
            return true;
        }

        this.setValidationError(result.Error);
        return false;
    }

    /// <summary>
    ///   Detaches the scanner status and watched-row subscriptions.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.projectScanner.StatusChanged -= this.OnScannerStatusChanged;

        foreach (var row in this.observedWatchedDirectoryRows)
        {
            row.PropertyChanged -= this.OnWatchedDirectoryRowChanged;
        }

        this.observedWatchedDirectoryRows.Clear();
    }

    private void BrowseWatchedDirectory()
    {
        var folder = this.dialogs.SelectFolder("Add Watched Directory");
        if (!string.IsNullOrWhiteSpace(folder))
        {
            this.NewWatchedDirectoryText = folder;
        }
    }

    private void AddWatchedDirectoryFromPicker()
    {
        var folder = this.dialogs.SelectFolder("Add Watched Directory");
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        if (this.TryAddWatchedDirectory(folder))
        {
            this.WatchedDirectoryError = null;
            this.setValidationError(null);
        }
    }

    private void AddWatchedDirectory()
    {
        if (this.TryAddWatchedDirectory(this.NewWatchedDirectoryText))
        {
            this.NewWatchedDirectoryText = string.Empty;
            this.setValidationError(null);
        }
    }

    private bool TryAddWatchedDirectory(string value)
    {
        var text = value.Trim();
        if (text.Length == 0)
        {
            this.WatchedDirectoryError = "Enter a folder path or browse for one.";
            return false;
        }

        if (!Path.IsPathFullyQualified(text) || !Directory.Exists(text))
        {
            this.WatchedDirectoryError = "Enter an existing absolute folder path.";
            return false;
        }

        string path;
        try
        {
            path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(text));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            this.WatchedDirectoryError = "Enter a valid absolute folder path.";
            return false;
        }

        if (!this.WatchedDirectories.Any(existing => string.Equals(existing.Path, path, StringComparison.OrdinalIgnoreCase)))
        {
            var row = this.CreateWatchedDirectoryRow(
                path,
                this.SelectedScanDepth.Value,
                this.SelectedDiscoveryMode.Mode);

            this.WatchedDirectories.Add(row);

            if (this.ApplyScanningSettings())
            {
                return true;
            }

            // A failed apply must not leave an unpersisted root in the live list.
            this.WatchedDirectories.Remove(row);
        }

        return true;
    }

    private WatchedDirectoryRow CreateWatchedDirectoryRow(string path, int scanDepth, ProjectDiscoveryMode discoveryMode)
    {
        var normalizedDepth = ProjectScanningDefaults.NormalizeScanDepth(scanDepth);
        var depth = ScanDepthChoices.First(option => option.Value == normalizedDepth);
        var discovery = DiscoveryModeOptions.FirstOrDefault(option => option.Mode == discoveryMode) ?? DiscoveryModeOptions[2];

        return new WatchedDirectoryRow(path, ScanDepthChoices, DiscoveryModeOptions, depth, discovery);
    }

    private void RemoveWatchedDirectory(string directory)
    {
        var row = this.WatchedDirectories.FirstOrDefault(
            candidate => string.Equals(candidate.Path, directory, StringComparison.OrdinalIgnoreCase));

        if (row is null)
        {
            return;
        }

        var index = this.WatchedDirectories.IndexOf(row);
        this.WatchedDirectories.RemoveAt(index);

        if (!this.ApplyScanningSettings())
        {
            this.WatchedDirectories.Insert(index, row);
        }
    }

    private void ScanWatchedDirectory(string directory) => this.projectScanner.Rescan(directory);

    private void OnWatchedDirectoriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        this.SyncObservedWatchedDirectoryRows(e);
        this.OnPropertyChanged(nameof(this.WatchedRootsSummary));
        this.RefreshWatchedDirectoryCounts();
    }

    private void SyncObservedWatchedDirectoryRows(NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var row in this.observedWatchedDirectoryRows)
            {
                row.PropertyChanged -= this.OnWatchedDirectoryRowChanged;
            }

            this.observedWatchedDirectoryRows.Clear();
            return;
        }

        if (e.OldItems is not null)
        {
            foreach (WatchedDirectoryRow row in e.OldItems)
            {
                row.PropertyChanged -= this.OnWatchedDirectoryRowChanged;
                this.observedWatchedDirectoryRows.Remove(row);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (WatchedDirectoryRow row in e.NewItems)
            {
                row.PropertyChanged += this.OnWatchedDirectoryRowChanged;
                this.observedWatchedDirectoryRows.Add(row);
            }
        }
    }

    private void OnWatchedDirectoryRowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WatchedDirectoryRow.SelectedDepth) or nameof(WatchedDirectoryRow.SelectedDiscovery))
        {
            this.ApplyScanningSettings();
        }
    }

    private void RefreshWatchedDirectoryCounts()
    {
        foreach (var row in this.WatchedDirectories)
        {
            row.DiscoveredCount = this.projectScanner.CountDiscoveredProjects(row.Path);
        }
    }

    private void AddIgnoredPattern()
    {
        var pattern = this.NewIgnoredPatternText.Trim();
        if (pattern.Length == 0)
        {
            return;
        }

        if (!this.IgnoredPatterns.Any(existing => string.Equals(existing, pattern, StringComparison.OrdinalIgnoreCase)))
        {
            this.IgnoredPatterns.Add(pattern);

            if (!this.ApplyScanningSettings())
            {
                this.IgnoredPatterns.Remove(pattern);
            }
        }

        this.NewIgnoredPatternText = string.Empty;
    }

    private void RemoveIgnoredPattern(string pattern)
    {
        var index = this.IgnoredPatterns.IndexOf(pattern);
        if (index < 0)
        {
            return;
        }

        this.IgnoredPatterns.RemoveAt(index);

        if (!this.ApplyScanningSettings())
        {
            this.IgnoredPatterns.Insert(index, pattern);
        }
    }

    private void OnScannerStatusChanged(object? sender, ProjectScanStatus status)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(() => this.ApplyScannerStatus(status));
            return;
        }

        this.ApplyScannerStatus(status);
    }

    private void ApplyScannerStatus(ProjectScanStatus? status)
    {
        var current = status ?? ProjectScanStatus.NotConfigured;
        this.DiscoveredProjectCount = current.DiscoveredCount;
        this.ManualProjectCount = this.projectScanner.CountManualProjects();
        this.ScannedDirectoryCount = current.ScannedDirectoryCount;
        this.WatcherStateLabel = current.State switch
        {
            ProjectScanState.NotConfigured => "Off",
            ProjectScanState.Scanning => "Scanning",
            ProjectScanState.Error => "Error",
            _ => current.IsWatching ? "Watching" : "Idle"
        };
        this.WatcherDetailText = current.State switch
        {
            ProjectScanState.NotConfigured => "No watched roots configured",
            ProjectScanState.Scanning => "Scan in progress",
            ProjectScanState.Error => current.Error ?? "Scan failed",
            _ => current.LastScanUtc is { } last
                ? $"Last scan {last.ToLocalTime():HH:mm:ss}{this.ScannedCoverageSuffix}"
                : "Not scanned yet"
        };

        this.RefreshWatchedDirectoryCounts();
    }

    private void OnProjectTotalsChanged()
    {
        this.OnPropertyChanged(nameof(this.TotalProjectCount));
        this.OnPropertyChanged(nameof(this.ProjectMixSummary));
    }
}
