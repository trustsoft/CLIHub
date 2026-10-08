namespace CLIHub;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Models;
using CLIHub.Core.Updates;

/// <summary>
///   Coordinates shared update operations while keeping entry-point presentation policy outside Core.
/// </summary>
public sealed class UpdateWorkflow : IUpdateWorkflow, IDisposable
{
    private static readonly TimeSpan NotificationDelay = TimeSpan.FromSeconds(2);

    private readonly IUpdateVersionProvider _versionProvider;
    private readonly IUpdateChecker _checker;
    private readonly IUpdateStateSource _stateSource;
    private readonly IUpdateDownloader _downloader;
    private readonly IUpdateInstaller _installer;
    private readonly ILogger<UpdateWorkflow> _logger;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;
    private readonly object _checkLock = new();
    private Task<UpdateCheckResult>? _activeCheck;
    private int _downloadActive;
    private bool _disposed;

    /// <summary>
    ///   Creates the workflow over the shared Core update service.
    /// </summary>
    /// <param name="versionProvider"> Current application version provider. </param>
    /// <param name="checker"> Core update checker. </param>
    /// <param name="stateSource"> Shared Core update state. </param>
    /// <param name="downloader"> Core update downloader. </param>
    /// <param name="installer"> Core update installer. </param>
    /// <param name="logger"> Logger for failures and automatic-restart outcomes. </param>
    /// <param name="delay"> Optional cancellation-aware notification delay. </param>
    public UpdateWorkflow(
        IUpdateVersionProvider versionProvider,
        IUpdateChecker checker,
        IUpdateStateSource stateSource,
        IUpdateDownloader downloader,
        IUpdateInstaller installer,
        ILogger<UpdateWorkflow> logger,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _versionProvider = versionProvider ?? throw new ArgumentNullException(nameof(versionProvider));
        _checker = checker ?? throw new ArgumentNullException(nameof(checker));
        _stateSource = stateSource ?? throw new ArgumentNullException(nameof(stateSource));
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _installer = installer ?? throw new ArgumentNullException(nameof(installer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _delay = delay ?? ((duration, cancellationToken) => Task.Delay(duration, cancellationToken));
        _stateSource.UpdateStateChanged += OnSourceStateChanged;
    }

    /// <inheritdoc />
    public bool IsDownloading => Volatile.Read(ref _downloadActive) != 0 || _stateSource.IsDownloading;

    /// <inheritdoc />
    public string? LastKnownAvailableVersion => _stateSource.LastKnownAvailableVersion;

    /// <inheritdoc />
    public bool IsCheckingForUpdates
    {
        get
        {
            lock (_checkLock)
            {
                return _activeCheck is { IsCompleted: false };
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
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stateSource.UpdateStateChanged -= OnSourceStateChanged;
    }

    /// <inheritdoc />
    public string GetCurrentVersion() => _versionProvider.GetCurrentVersion();

    /// <inheritdoc />
    public Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TaskCompletionSource<UpdateCheckResult>? completion = null;
        Task<UpdateCheckResult> check;
        lock (_checkLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_activeCheck is null || _activeCheck.IsCompleted)
            {
                completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _activeCheck = completion.Task;
            }

            check = _activeCheck;
        }

        if (completion is not null)
        {
            _ = CheckCoreAsync(completion, cancellationToken);
        }

        return completion is null && cancellationToken.CanBeCanceled
            ? check.WaitAsync(cancellationToken)
            : check;
    }

    /// <inheritdoc />
    public async Task<UpdateDownloadResult> DownloadUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _downloadActive, 1, 0) != 0)
        {
            return new(UpdateDownloadStatus.AlreadyDownloading, LastKnownAvailableVersion);
        }

        try
        {
            OnSourceStateChanged(this, EventArgs.Empty);
            return await _downloader.DownloadUpdateAsync(cancellationToken);
        }
        finally
        {
            Interlocked.Exchange(ref _downloadActive, 0);
            OnSourceStateChanged(this, EventArgs.Empty);
        }
    }

    /// <inheritdoc />
    public void ApplyDownloadedUpdateAndRestart() => _installer.ApplyDownloadedUpdateAndRestart();

    /// <inheritdoc />
    public async Task DownloadAndApplyAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _downloadActive, 1, 0) != 0)
        {
            return;
        }

        try
        {
            OnSourceStateChanged(this, EventArgs.Empty);
            var result = await _downloader.DownloadUpdateAsync(cancellationToken);
            if (result.Status == UpdateDownloadStatus.Downloaded && result.AvailableVersion is { } version)
            {
                UpdateDownloaded?.Invoke(this, version);
                OnSourceStateChanged(this, EventArgs.Empty);
                await _delay(NotificationDelay, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                ApplyDownloadedUpdateAndRestart();
                return;
            }

            if (result.Status == UpdateDownloadStatus.Failed && result.AvailableVersion is { } failedVersion)
            {
                UpdateDownloadFailed?.Invoke(this, failedVersion);
            }

            _logger.LogInformation("Update download not applied: {Status}", result.Status);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Update download cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Applying the downloaded update failed");
        }
        finally
        {
            Interlocked.Exchange(ref _downloadActive, 0);
            OnSourceStateChanged(this, EventArgs.Empty);
        }
    }

    private async Task CheckCoreAsync(
        TaskCompletionSource<UpdateCheckResult> completion,
        CancellationToken cancellationToken)
    {
        UpdateCheckResult? result = null;
        Exception? failure = null;
        try
        {
            OnSourceStateChanged(this, EventArgs.Empty);
            result = await _checker.CheckForUpdatesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            failure = ex;
        }
        finally
        {
            lock (_checkLock)
            {
                _activeCheck = null;
            }

            try
            {
                OnSourceStateChanged(this, EventArgs.Empty);
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
        if (!_disposed)
        {
            UpdateStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
