namespace CLIHub.Updates;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Core.Updates;

/// <summary>
///   Coordinates shared update operations while keeping entry-point presentation policy outside Core.
/// </summary>
public sealed class UpdateWorkflow : IUpdateWorkflow, IDisposable
{
    private static readonly TimeSpan NotificationDelay = TimeSpan.FromSeconds(2);

    private readonly IUpdateVersionProvider versionProvider;
    private readonly IUpdateChecker checker;
    private readonly IUpdateStateSource stateSource;
    private readonly IUpdateDownloader downloader;
    private readonly IUpdateInstaller installer;
    private readonly IPreferencesStore preferences;
    private readonly ILogger<UpdateWorkflow> logger;
    private readonly Func<TimeSpan, CancellationToken, Task> delay;
    private readonly object checkLock = new();
    private Task<UpdateCheckResult>? activeCheck;
    private int downloadActive;
    private bool disposed;

    /// <summary>
    ///   Creates the workflow over the shared Core update service.
    /// </summary>
    /// <param name="versionProvider"> Current application version provider. </param>
    /// <param name="checker"> Core update checker. </param>
    /// <param name="stateSource"> Shared Core update state. </param>
    /// <param name="downloader"> Core update downloader. </param>
    /// <param name="installer"> Core update installer. </param>
    /// <param name="preferencesStore"> Preferences source for background auto-download. </param>
    /// <param name="logger"> Logger for failures and automatic-restart outcomes. </param>
    /// <param name="delay"> Optional cancellation-aware notification delay. </param>
    public UpdateWorkflow(IUpdateVersionProvider versionProvider,
                          IUpdateChecker checker,
                          IUpdateStateSource stateSource,
                          IUpdateDownloader downloader,
                          IUpdateInstaller installer,
                          IPreferencesStore preferencesStore,
                          ILogger<UpdateWorkflow> logger,
                          Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        this.versionProvider = versionProvider ?? throw new ArgumentNullException(nameof(versionProvider));
        this.checker = checker ?? throw new ArgumentNullException(nameof(checker));
        this.stateSource = stateSource ?? throw new ArgumentNullException(nameof(stateSource));
        this.downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        this.installer = installer ?? throw new ArgumentNullException(nameof(installer));
        this.preferences = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.delay = delay ?? ((duration, cancellationToken) => Task.Delay(duration, cancellationToken));
        this.stateSource.UpdateStateChanged += this.OnSourceStateChanged;
    }

    /// <inheritdoc />
    public bool IsDownloading => Volatile.Read(ref this.downloadActive) != 0 || this.stateSource.IsDownloading;

    /// <inheritdoc />
    public string? LastKnownAvailableVersion => this.stateSource.LastKnownAvailableVersion;

    /// <inheritdoc />
    public bool IsCheckingForUpdates
    {
        get
        {
            lock (this.checkLock)
            {
                return this.activeCheck is { IsCompleted: false };
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler? UpdateStateChanged;

    /// <inheritdoc />
    public event EventHandler<string>? UpdateDownloaded;

    /// <inheritdoc />
    public event EventHandler<string>? UpdateDownloadFailed;

    /// <summary>
    ///   Removes the underlying update-state subscription.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.stateSource.UpdateStateChanged -= this.OnSourceStateChanged;
    }

    /// <inheritdoc />
    public string GetCurrentVersion() => this.versionProvider.GetCurrentVersion();

    /// <inheritdoc />
    public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TaskCompletionSource<UpdateCheckResult>? completion = null;
        Task<UpdateCheckResult> check;
        lock (this.checkLock)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);
            if (this.activeCheck is null || this.activeCheck.IsCompleted)
            {
                completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
                this.activeCheck = completion.Task;
            }

            check = this.activeCheck;
        }

        if (completion is not null)
        {
            _ = this.CheckCoreAsync(completion, cancellationToken);
        }

        return completion is null && cancellationToken.CanBeCanceled
            ? check.WaitAsync(cancellationToken)
            : check;
    }

    /// <inheritdoc />
    public async Task<UpdateDownloadResult> DownloadUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref this.downloadActive, 1, 0) != 0)
        {
            return new(UpdateDownloadStatus.AlreadyDownloading, this.LastKnownAvailableVersion);
        }

        try
        {
            this.OnSourceStateChanged(this, EventArgs.Empty);
            return await this.downloader.DownloadUpdateAsync(cancellationToken);
        }
        finally
        {
            Interlocked.Exchange(ref this.downloadActive, 0);
            this.OnSourceStateChanged(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public void ApplyDownloadedUpdateAndRestart() => this.installer.ApplyDownloadedUpdateAndRestart();

    /// <inheritdoc />
    public async Task DownloadAndApplyAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref this.downloadActive, 1, 0) != 0)
        {
            return;
        }

        try
        {
            this.OnSourceStateChanged(this, EventArgs.Empty);
            var result = await this.downloader.DownloadUpdateAsync(cancellationToken);
            if (result.Status == UpdateDownloadStatus.Downloaded && result.AvailableVersion is { } version)
            {
                this.UpdateDownloaded?.Invoke(this, version);
                this.OnSourceStateChanged(this, EventArgs.Empty);
                await this.delay(NotificationDelay, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                this.ApplyDownloadedUpdateAndRestart();
                return;
            }

            if (result.Status == UpdateDownloadStatus.Failed && result.AvailableVersion is { } failedVersion)
            {
                this.UpdateDownloadFailed?.Invoke(this, failedVersion);
            }

            this.logger.LogInformation("Update download not applied: {Status}", result.Status);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            this.logger.LogInformation("Update download cancelled");
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Applying the downloaded update failed");
        }
        finally
        {
            Interlocked.Exchange(ref this.downloadActive, 0);
            this.OnSourceStateChanged(this, EventArgs.Empty);
        }
    }

    private async Task CheckCoreAsync(TaskCompletionSource<UpdateCheckResult> completion,
                                      CancellationToken cancellationToken)
    {
        UpdateCheckResult? result = null;
        Exception? failure = null;
        try
        {
            this.OnSourceStateChanged(this, EventArgs.Empty);
            result = await this.checker.CheckForUpdatesAsync(cancellationToken);
            this.MaybeStartAutoDownload(result);
        }
        catch (Exception ex)
        {
            failure = ex;
        }
        finally
        {
            lock (this.checkLock)
            {
                this.activeCheck = null;
            }

            try
            {
                this.OnSourceStateChanged(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                failure ??= ex;
            }

            if (failure is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                completion.TrySetCanceled(cancellationToken);
            }
            else if (failure is not null)
            {
                completion.TrySetException(failure);
            }
            else
            {
                completion.TrySetResult(result!);
            }
        }
    }

    private void OnSourceStateChanged(object? sender, EventArgs e)
    {
        if (!this.disposed)
        {
            this.UpdateStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void MaybeStartAutoDownload(UpdateCheckResult result)
    {
        if (result.Status != UpdateStatus.UpdateAvailable || result.AvailableVersion is null)
        {
            return;
        }

        if (!this.preferences.Load().AutoDownloadUpdates)
        {
            return;
        }

        _ = this.AutoDownloadAsync();
    }

    private async Task AutoDownloadAsync()
    {
        try
        {
            await this.DownloadUpdateAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Automatic update download failed");
        }
    }
}
