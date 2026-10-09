# Update Service Refactoring - Specification

## Component Contracts

### 1. VelopackManagerProvider

Internal class that encapsulates Velopack UpdateManager lifecycle.

```csharp
namespace CLIHub.Core.Updates;

internal sealed class VelopackManagerProvider
{
    public VelopackManagerProvider(string repositoryUrl, ILogger logger);
    public VelopackManagerProvider(UpdateManager manager); // Test constructor
    
    public UpdateManager? Manager { get; }
    public bool IsAvailable { get; }
}
```

**Behavior:**
- `Manager`: Returns lazily-initialized UpdateManager, or null if Velopack is not initialized
- `IsAvailable`: Returns true if Manager is not null
- Constructor creates UpdateManager with GithubSource if URL contains "github.com", otherwise uses default source
- Catches and logs NotInitializedException during initialization

**Thread Safety:** Lazy<T> ensures thread-safe initialization.

---

### 2. UpdateChecker

Internal class that handles update checking with timeout and error handling.

```csharp
namespace CLIHub.Core.Updates;

internal sealed class UpdateChecker
{
    public UpdateChecker(ILogger logger);
    
    public Task<(UpdateCheckResult Result, UpdateInfo? Update)> CheckAsync(
        UpdateManager manager,
        string currentVersion,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}
```

**Behavior:**
- Calls `manager.CheckForUpdatesAsync()` with timeout using `Task.WhenAny`
- On timeout: logs warning, observes late completion, returns `(Failed, null)`
- On success with update: returns `(UpdateAvailable, UpdateInfo)`
- On success without update: returns `(UpToDate, null)`
- On NotInstalledException: logs info, returns `(NotInstalled, null)`
- On OperationCanceledException: logs warning, returns `(Failed, null)`
- On other exceptions: logs warning, returns `(Failed, null)`

**UpdateCheckResult:**
```csharp
public record UpdateCheckResult(
    UpdateStatus Status,
    string CurrentVersion,
    string? AvailableVersion);

public enum UpdateStatus
{
    NotInstalled,
    UpToDate,
    UpdateAvailable,
    Failed
}
```

**Thread Safety:** Stateless, safe to call concurrently.

---

### 3. UpdateDownloader

Internal class that manages download state and orchestrates downloads.

```csharp
namespace CLIHub.Core.Updates;

internal sealed class UpdateDownloader
{
    public UpdateDownloader(ILogger logger);
    
    public bool IsDownloading { get; }
    public VelopackAsset? DownloadedAsset { get; }
    
    public Task<UpdateDownloadResult> DownloadAsync(
        UpdateManager manager,
        UpdateInfo update,
        CancellationToken cancellationToken);
}
```

**Behavior:**
- `IsDownloading`: Returns true while download is in progress
- `DownloadedAsset`: Returns the most recently downloaded asset, or null
- `DownloadAsync`:
  - Returns `AlreadyDownloading` if a download is in progress
  - Calls `manager.DownloadUpdatesAsync(update, null, cancellationToken)`
  - On success: stores asset, returns `(Downloaded, version)`
  - On OperationCanceledException: logs warning, returns `(Failed, version)`
  - On other exceptions: logs warning, returns `(Failed, version)`

**UpdateDownloadResult:**
```csharp
public record UpdateDownloadResult(
    UpdateDownloadStatus Status,
    string? AvailableVersion);

public enum UpdateDownloadStatus
{
    Downloaded,
    AlreadyDownloading,
    NoUpdate,
    NotInstalled,
    Failed
}
```

**Thread Safety:** 
- Uses `lock (_downloadGate)` for all state access
- `IsDownloading` getter is synchronized
- `DownloadedAsset` getter is synchronized
- `DownloadAsync` sets flag at start and clears at end in finally block

---

### 4. UpdateInstaller

Internal class that applies downloaded updates.

```csharp
namespace CLIHub.Core.Updates;

internal sealed class UpdateInstaller
{
    public UpdateInstaller(ILogger logger);
    
    public void Apply(UpdateManager manager, VelopackAsset asset);
}
```

**Behavior:**
- Calls `manager.ApplyUpdatesAndRestart(asset)`
- On exception: logs warning and re-throws

**Thread Safety:** Stateless, safe to call concurrently.

---

## UpdateService Refactored API

All public interfaces remain unchanged. Internal implementation delegates to components.

### Constructor Changes

**Production constructor:**
```csharp
public UpdateService(ILogger<UpdateService> logger)
{
    _logger = logger;
    _checkTimeout = TimeSpan.FromSeconds(15);
    _managerProvider = new VelopackManagerProvider(RepositoryUrl, logger);
    _checker = new UpdateChecker(logger);
    _downloader = new UpdateDownloader(logger);
    _installer = new UpdateInstaller(logger);
}
```

**Test constructor (private):**
```csharp
private UpdateService(
    ILogger<UpdateService> logger, 
    UpdateManager manager, 
    TimeSpan? checkTimeout)
{
    _logger = logger;
    _checkTimeout = checkTimeout ?? TimeSpan.FromSeconds(15);
    _managerProvider = new VelopackManagerProvider(manager); // Test wrapper
    _checker = new UpdateChecker(logger);
    _downloader = new UpdateDownloader(logger);
    _installer = new UpdateInstaller(logger);
}
```

**Test factory (unchanged signature):**
```csharp
internal static UpdateService CreateForTesting(
    ILogger<UpdateService> logger,
    UpdateManager manager,
    TimeSpan? checkTimeout = null)
    => new(logger, manager, checkTimeout);
```

### Method Implementations

**GetCurrentVersion():**
```csharp
public string GetCurrentVersion()
{
    if (_managerProvider.Manager?.CurrentVersion is { } version)
    {
        return Normalize(version.ToString());
    }

    var informational = Assembly.GetEntryAssembly()
        ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion;

    return string.IsNullOrWhiteSpace(informational) 
        ? "unknown" 
        : Normalize(informational);
}
```

**CheckForUpdatesAsync():**
```csharp
public async Task<UpdateCheckResult> CheckForUpdatesAsync(
    CancellationToken cancellationToken = default)
{
    var (result, _) = await CheckForUpdateCoreAsync(cancellationToken);
    return result;
}

private async Task<(UpdateCheckResult Result, UpdateInfo? Update)> 
    CheckForUpdateCoreAsync(CancellationToken cancellationToken)
{
    var current = GetCurrentVersion();

    if (!_managerProvider.IsAvailable)
    {
        _logger.LogInformation("Update check skipped: not a Velopack install");
        SetAvailableVersion(null);
        return (new UpdateCheckResult(UpdateStatus.NotInstalled, current, null), null);
    }

    var (result, update) = await _checker.CheckAsync(
        _managerProvider.Manager!,
        current,
        _checkTimeout,
        cancellationToken);

    SetAvailableVersion(result.AvailableVersion);
    return (result, update);
}
```

**DownloadUpdateAsync():**
```csharp
public async Task<UpdateDownloadResult> DownloadUpdateAsync(
    CancellationToken cancellationToken = default)
{
    if (_downloader.IsDownloading)
    {
        _logger.LogInformation("Download already in progress");
        return new UpdateDownloadResult(
            UpdateDownloadStatus.AlreadyDownloading, 
            null);
    }

    try
    {
        var (result, update) = await CheckForUpdateCoreAsync(cancellationToken);

        if (result.Status != UpdateStatus.UpdateAvailable || update is null)
        {
            var status = result.Status switch
            {
                UpdateStatus.NotInstalled => UpdateDownloadStatus.NotInstalled,
                UpdateStatus.UpToDate => UpdateDownloadStatus.NoUpdate,
                _ => UpdateDownloadStatus.Failed,
            };

            _logger.LogInformation("Download not started: {Status}", status);
            return new UpdateDownloadResult(status, null);
        }

        var downloadResult = await _downloader.DownloadAsync(
            _managerProvider.Manager!,
            update,
            cancellationToken);

        _logger.LogInformation(
            "Download completed: {Status}, {Version}",
            downloadResult.Status,
            downloadResult.AvailableVersion);

        return downloadResult;
    }
    finally
    {
        RaiseStateChanged();
    }
}
```

**ApplyDownloadedUpdateAndRestart():**
```csharp
public void ApplyDownloadedUpdateAndRestart()
{
    if (!_managerProvider.IsAvailable)
    {
        _logger.LogInformation("No manager; restart skipped");
        return;
    }

    if (_downloader.DownloadedAsset is not { } asset)
    {
        _logger.LogInformation("No downloaded update; restart skipped");
        return;
    }

    _installer.Apply(_managerProvider.Manager!, asset);
}
```

**State properties:**
```csharp
public bool IsDownloading => _downloader.IsDownloading;

public string? LastKnownAvailableVersion => _lastKnownAvailableVersion;

public event EventHandler? UpdateStateChanged;

private void SetAvailableVersion(string? version)
{
    var changed = !string.Equals(
        Interlocked.Exchange(ref _lastKnownAvailableVersion, version),
        version,
        StringComparison.Ordinal);

    if (changed)
    {
        RaiseStateChanged();
    }
}

private void RaiseStateChanged() 
    => UpdateStateChanged?.Invoke(this, EventArgs.Empty);
```

---

## Testing Requirements

### Existing Tests (No Changes)

All 14 tests in `UpdateServiceTests.cs` must pass without modification:
- Version detection tests
- Update check success/timeout/failure tests
- Download tests with state management
- Apply tests

### New Tests

**VelopackManagerProviderTests.cs** (3 tests):
1. `Constructor_WithGitHubUrl_CreatesGitHubSource`
2. `Constructor_WithLocalUrl_CreatesDefaultSource`
3. `Manager_WhenNotInitialized_ReturnsNull`

**UpdateCheckerTests.cs** (4 tests):
1. `CheckAsync_WithAvailableUpdate_ReturnsUpdateAvailable`
2. `CheckAsync_WithNoUpdate_ReturnsUpToDate`
3. `CheckAsync_WhenTimeout_ReturnsFailed`
4. `CheckAsync_WhenNotInstalled_ReturnsNotInstalled`

**UpdateDownloaderTests.cs** (3 tests):
1. `DownloadAsync_Success_StoresAssetAndReturnsDownloaded`
2. `DownloadAsync_WhenCancelled_ReturnsFailed`
3. `IsDownloading_DuringDownload_ReturnsTrue`

**UpdateInstallerTests.cs** (2 tests):
1. `Apply_CallsManagerApplyUpdatesAndRestart`
2. `Apply_WhenException_LogsAndRethrows`

---

## Size Estimates

| Component | Lines of Code |
|-----------|---------------|
| VelopackManagerProvider.cs | ~60 |
| UpdateChecker.cs | ~80 |
| UpdateDownloader.cs | ~70 |
| UpdateInstaller.cs | ~30 |
| UpdateService.cs (refactored) | ~150 |
| **Total (down from 317)** | **~390** |

**Note:** Total lines increase due to separation, but individual components are simpler and more testable.

---

## Migration Verification Checklist

### Phase 1: Component Creation
- [ ] Create VelopackManagerProvider with tests
- [ ] Create UpdateChecker with tests
- [ ] Create UpdateDownloader with tests
- [ ] Create UpdateInstaller with tests
- [ ] All new tests pass (12 tests)

### Phase 2: UpdateService Refactoring
- [ ] Add component fields to UpdateService
- [ ] Initialize components in constructors
- [ ] Refactor GetCurrentVersion() to use provider
- [ ] Refactor CheckForUpdatesAsync() to use checker
- [ ] Refactor DownloadUpdateAsync() to use downloader
- [ ] Refactor ApplyDownloadedUpdateAndRestart() to use installer
- [ ] Verify CreateForTesting still works

### Phase 3: Verification
- [ ] All 14 existing UpdateService tests pass
- [ ] All 12 new component tests pass
- [ ] Full suite: 555 tests pass
- [ ] Manual test: check for updates in running app
- [ ] Manual test: download update
- [ ] Code review: UpdateService is ~150 lines

---

## Breaking Change Analysis

**None.** This is a pure internal refactoring.

### Public API (Unchanged)
- All interface definitions remain identical
- DI registration unchanged (services.AddSingleton<IUpdateService, UpdateService>())
- UpdateService public constructor signature unchanged
- All consumer code (UpdateWorkflow, ViewModels) unchanged

### Internal API (Changed)
- UpdateService.CreateForTesting signature unchanged, implementation wraps injected manager
- New internal classes not exposed outside Updates namespace

### Test Compatibility
- Existing UpdateServiceTests use CreateForTesting - preserved
- No changes needed to any existing test code
