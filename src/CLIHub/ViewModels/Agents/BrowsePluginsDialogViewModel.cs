namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;
using System.IO;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Plugins;
using CLIHub.Core.Updates;
using CLIHub.Launching;
using CLIHub.Lifetime;

/// <summary>
///   Browse Plugins dialog: fetches the remote feed, displays available plugins with status,
///   and handles install/update workflows with checksum verification.
/// </summary>
public sealed class BrowsePluginsDialogViewModel : ObservableObject
{
    private readonly IPluginFeedClient feedClient;
    private readonly IPluginInstaller installer;
    private readonly IPluginStateRepository stateRepository;
    private readonly IPluginDialogService dialogService;
    private readonly IUpdateVersionProvider versionProvider;
    private readonly IApplicationOperationLifetime operationLifetime;
    private readonly ILogger<BrowsePluginsDialogViewModel> logger;

    private bool isLoading;
    private string? errorMessage;

    /// <summary>
    ///   Creates the dialog view model.
    /// </summary>
    /// <param name="feedClient"> Feed client for fetching the index and downloading packages. </param>
    /// <param name="installer"> Plugin installer for inspecting and installing packages. </param>
    /// <param name="stateRepository"> Plugin state repository for reading installed versions. </param>
    /// <param name="dialogService"> Dialog service for confirmation and error dialogs. </param>
    /// <param name="versionProvider"> Current application version provider for minAppVersion gating. </param>
    /// <param name="operationLifetime"> Application operation lifetime for async workflows. </param>
    /// <param name="logger"> Logger. </param>
    public BrowsePluginsDialogViewModel(
        IPluginFeedClient feedClient,
        IPluginInstaller installer,
        IPluginStateRepository stateRepository,
        IPluginDialogService dialogService,
        IUpdateVersionProvider versionProvider,
        IApplicationOperationLifetime operationLifetime,
        ILogger<BrowsePluginsDialogViewModel> logger)
    {
        this.feedClient = feedClient ?? throw new ArgumentNullException(nameof(feedClient));
        this.installer = installer ?? throw new ArgumentNullException(nameof(installer));
        this.stateRepository = stateRepository ?? throw new ArgumentNullException(nameof(stateRepository));
        this.dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        this.versionProvider = versionProvider ?? throw new ArgumentNullException(nameof(versionProvider));
        this.operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        this.Plugins.CollectionChanged += (_, _) =>
        {
            this.OnPropertyChanged(nameof(this.HasPlugins));
            this.OnPropertyChanged(nameof(this.IsEmpty));
        };

        this.RefreshCommand = new RelayCommand(() => _ = this.RunAsync("Refresh plugin feed", this.LoadFeedAsync), () => !this.IsLoading);
        this.InstallCommand = new RelayCommand<FeedPluginItem>(item => _ = this.RunAsync($"Install {item.Name}", ct => this.InstallPluginAsync(item, ct)), item => item is not null && item.CanInstall && !this.IsLoading);
    }

    /// <summary>
    ///   Available plugins from the feed.
    /// </summary>
    public ObservableCollection<FeedPluginItem> Plugins { get; } = new();

    /// <summary>
    ///   Whether the feed is currently loading.
    /// </summary>
    public bool IsLoading
    {
        get => this.isLoading;
        private set
        {
            if (this.SetProperty(ref this.isLoading, value))
            {
                this.OnPropertyChanged(nameof(this.IsIdle));
                this.RefreshCommand.RaiseCanExecuteChanged();
                this.InstallCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    ///   Whether the dialog is idle (not loading).
    /// </summary>
    public bool IsIdle => !this.IsLoading;

    /// <summary>
    ///   Error message from the last feed load, or null.
    /// </summary>
    public string? ErrorMessage
    {
        get => this.errorMessage;
        private set
        {
            if (this.SetProperty(ref this.errorMessage, value))
            {
                this.OnPropertyChanged(nameof(this.HasError));
                this.OnPropertyChanged(nameof(this.IsEmpty));
            }
        }
    }

    /// <summary>
    ///   Whether an error message is present.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(this.ErrorMessage);

    /// <summary>
    ///   Whether plugins are available to display.
    /// </summary>
    public bool HasPlugins => this.Plugins.Count > 0;

    /// <summary>
    ///   Whether the empty state should be shown (no error, no plugins, and not loading).
    /// </summary>
    public bool IsEmpty => !this.HasError && !this.HasPlugins && !this.IsLoading;

    /// <summary>
    ///   Refresh command: reloads the feed.
    /// </summary>
    public RelayCommand RefreshCommand { get; }

    /// <summary>
    ///   Install command: downloads, verifies, confirms, and installs a plugin.
    /// </summary>
    public RelayCommand<FeedPluginItem> InstallCommand { get; }

    /// <summary>
    ///   Loads the feed on dialog open.
    /// </summary>
    public async Task LoadAsync()
    {
        await this.LoadFeedAsync(CancellationToken.None);
    }

    private async Task LoadFeedAsync(CancellationToken cancellationToken)
    {
        this.IsLoading = true;
        this.ErrorMessage = null;
        this.Plugins.Clear();

        try
        {
            var feedResult = await this.feedClient.GetFeedAsync(cancellationToken);
            if (!feedResult.Success)
            {
                this.ErrorMessage = feedResult.Errors.Count > 0
                    ? string.Join(" ", feedResult.Errors)
                    : "Could not load plugin feed.";
                this.logger.LogWarning("Feed load failed: {Errors}", string.Join("; ", feedResult.Errors));
                return;
            }

            if (feedResult.Index is null || feedResult.Index.Plugins.Count == 0)
            {
                this.logger.LogInformation("Feed loaded with no plugins");
                return;
            }

            var pluginState = this.stateRepository.Load();
            var currentVersion = this.versionProvider.GetCurrentVersion();
            var builtinIds = EmbeddedPluginResources.BuiltinPluginIds(typeof(EmbeddedPluginResources).Assembly);

            foreach (var entry in feedResult.Index.Plugins)
            {
                // Filter out reserved built-in IDs
                if (builtinIds.Contains(entry.Id))
                {
                    this.logger.LogInformation("Skipping built-in plugin {PluginId} from feed", entry.Id);
                    continue;
                }

                var stateEntry = pluginState.Plugins.GetValueOrDefault(entry.Id);
                var installedVersion = stateEntry?.InstalledVersion;

                // Distinguish: no state entry → Available; state entry exists but InstalledVersion is null → UpdateAvailable
                PluginUpdateStatus status;
                if (stateEntry is null)
                {
                    status = PluginUpdateStatus.Available;
                }
                else if (installedVersion is null)
                {
                    status = PluginUpdateStatus.UpdateAvailable;
                }
                else
                {
                    status = PluginFeedVersionComparator.Compare(installedVersion, entry.Version);
                }

                var canInstall = AppVersionComparator.IsSatisfied(currentVersion, entry.MinAppVersion);
                var isDowngrade = status == PluginUpdateStatus.DifferentVersionAvailable &&
                                  installedVersion is not null &&
                                  PluginFeedVersionComparator.TryParseVersion(installedVersion).HasValue &&
                                  PluginFeedVersionComparator.TryParseVersion(entry.Version).HasValue &&
                                  PluginFeedVersionComparator.CompareVersions(
                                      PluginFeedVersionComparator.TryParseVersion(installedVersion)!.Value,
                                      PluginFeedVersionComparator.TryParseVersion(entry.Version)!.Value) > 0;

                var item = new FeedPluginItem(
                    entry.Id,
                    entry.Name,
                    entry.Description,
                    entry.Version,
                    entry.Publisher,
                    entry.Homepage,
                    entry.PackageUrl,
                    entry.Sha256,
                    entry.MinAppVersion,
                    entry.ReleaseNotesUrl,
                    status,
                    installedVersion,
                    canInstall,
                    isDowngrade);

                this.Plugins.Add(item);
            }

            this.logger.LogInformation("Loaded {Count} plugins from feed", this.Plugins.Count);
        }
        catch (Exception ex)
        {
            this.ErrorMessage = "Could not load plugin feed. Check your network connection.";
            this.logger.LogError(ex, "Feed load failed with exception");
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    private async Task InstallPluginAsync(FeedPluginItem item, CancellationToken cancellationToken)
    {
        string? tempFilePath = null;
        try
        {
            // Download package with checksum verification
            var downloadResult = await this.feedClient.DownloadPackageAsync(item.PackageUrl, item.Sha256, cancellationToken);
            if (!downloadResult.Success)
            {
                var errorMsg = downloadResult.Errors.Count > 0
                    ? string.Join(" ", downloadResult.Errors)
                    : "Package download failed.";
                this.dialogService.ShowValidationError([errorMsg]);
                this.logger.LogWarning("Package download failed for {PluginId}: {Errors}", item.Id, string.Join("; ", downloadResult.Errors));
                return;
            }

            tempFilePath = downloadResult.TempFilePath;

            // Inspect the package
            var preview = this.installer.Inspect(tempFilePath!);
            if (!preview.IsValid)
            {
                this.dialogService.ShowValidationError(preview.Errors);
                this.logger.LogWarning("Package inspection failed for {PluginId}: {Errors}", item.Id, string.Join("; ", preview.Errors));
                return;
            }

            // Show confirmation dialog with all declared commands
            if (!this.dialogService.ShowInstallConfirmation(preview))
            {
                this.logger.LogInformation("User cancelled installation of {PluginId}", item.Id);
                return;
            }

            // Install the plugin
            var replaceExisting = item.Status == PluginUpdateStatus.UpdateAvailable ||
                                  item.Status == PluginUpdateStatus.DifferentVersionAvailable ||
                                  item.Status == PluginUpdateStatus.Current;

            var installResult = this.installer.Install(tempFilePath!, replaceExisting);
            if (installResult.Success)
            {
                this.dialogService.ShowInstallOutcome(installResult, item.Name);
                this.logger.LogInformation("Successfully installed {PluginId} from feed", item.Id);

                // Reload feed to update status
                await this.LoadFeedAsync(cancellationToken);
            }
            else
            {
                this.dialogService.ShowValidationError(installResult.Errors);
                this.logger.LogWarning("Installation failed for {PluginId}: {Errors}", item.Id, string.Join("; ", installResult.Errors));
            }
        }
        catch (OperationCanceledException)
        {
            this.logger.LogInformation("Plugin installation cancelled for {PluginId}", item.Id);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Plugin installation failed for {PluginId}", item.Id);
            this.dialogService.ShowValidationError(["Installation failed. See logs for details."]);
        }
        finally
        {
            // Clean up temp file
            if (tempFilePath is not null && File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                    this.logger.LogDebug("Deleted temporary package file {Path}", tempFilePath);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "Failed to delete temporary package file {Path}", tempFilePath);
                }
            }
        }
    }

    private Task RunAsync(string operationName, Func<CancellationToken, Task> operation) =>
        this.operationLifetime.RunAsync(operationName, operation);
}
