# Update Service Refactoring - Design

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                      UpdateService                          │
│  (Thin Facade - implements all public interfaces)          │
│                                                             │
│  • IUpdateVersionProvider                                   │
│  • IUpdateChecker                                          │
│  • IUpdateStateSource                                      │
│  • IUpdateDownloader                                       │
│  • IUpdateInstaller                                        │
└────────┬────────────┬─────────────┬─────────────┬──────────┘
         │            │             │             │
         ▼            ▼             ▼             ▼
┌────────────┐ ┌────────────┐ ┌─────────────┐ ┌──────────────┐
│ Velopack   │ │   Update   │ │   Update    │ │   Update     │
│  Manager   │ │  Checker   │ │ Downloader  │ │  Installer   │
│  Provider  │ │            │ │             │ │              │
└────────────┘ └────────────┘ └─────────────┘ └──────────────┘
```

## Component Responsibilities

### 1. VelopackManagerProvider

**Purpose:** Encapsulates Velopack UpdateManager lifecycle and initialization logic.

**Responsibilities:**
- Lazy initialization of UpdateManager
- GitHub vs. local source detection
- Handling NotInitializedException when Velopack is not available
- Exposing Manager and IsAvailable properties

**Interface:**
```csharp
internal interface IVelopackManagerProvider
{
    UpdateManager? Manager { get; }
    bool IsAvailable { get; }
}
```

**Why internal class, not interface:**
- Only used by UpdateService, no need for polymorphism
- Simplifies testing (UpdateService tests already use CreateForTesting)
- Avoids DI registration complexity

### 2. UpdateChecker

**Purpose:** Handles the check-for-updates operation with timeout and error handling.

**Responsibilities:**
- Calling manager.CheckForUpdatesAsync()
- Timeout handling with Task.WhenAny
- Late check observation (fire-and-forget)
- Exception handling (NotInstalledException, OperationCanceledException)
- Returning structured UpdateCheckResult

**Interface:**
```csharp
internal interface IUpdateChecker
{
    Task<(UpdateCheckResult Result, UpdateInfo? Update)> CheckAsync(
        UpdateManager manager,
        string currentVersion,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}
```

**Note:** Takes UpdateManager as parameter rather than dependency to support test injection pattern.

### 3. UpdateDownloader

**Purpose:** Manages download state and orchestrates the download operation.

**Responsibilities:**
- Download state tracking (IsDownloading flag)
- Thread-safe state synchronization (_downloadGate)
- Calling manager.DownloadUpdatesAsync()
- Storing downloaded asset for later installation
- Error handling and result mapping

**Interface:**
```csharp
internal interface IUpdateDownloader
{
    bool IsDownloading { get; }
    VelopackAsset? DownloadedAsset { get; }
    
    Task<UpdateDownloadResult> DownloadAsync(
        UpdateManager manager,
        UpdateInfo update,
        CancellationToken cancellationToken);
}
```

**State management:**
- Maintains `_isDownloading` flag with lock-based synchronization
- Stores `_downloadedAsset` for ApplyDownloadedUpdateAndRestart
- Provides thread-safe property access

### 4. UpdateInstaller

**Purpose:** Applies downloaded updates and triggers application restart.

**Responsibilities:**
- Validating that an update was downloaded
- Calling manager.ApplyUpdatesAndRestart()
- Exception handling and logging

**Interface:**
```csharp
internal interface IUpdateInstaller
{
    void Apply(UpdateManager manager, VelopackAsset asset);
}
```

**Note:** Simple stateless operation, receives manager and asset as parameters.

## UpdateService Refactored Structure

```csharp
public class UpdateService : IUpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private readonly VelopackManagerProvider _managerProvider;
    private readonly UpdateChecker _checker;
    private readonly UpdateDownloader _downloader;
    private readonly UpdateInstaller _installer;
    private readonly TimeSpan _checkTimeout;
    private string? _lastKnownAvailableVersion;
    
    // Public constructor - production use
    public UpdateService(ILogger<UpdateService> logger) { ... }
    
    // Private constructor - test seam
    private UpdateService(ILogger, UpdateManager, TimeSpan?) { ... }
    
    // Test factory - preserves existing test compatibility
    internal static UpdateService CreateForTesting(...) { ... }
    
    // IUpdateVersionProvider
    public string GetCurrentVersion() 
    {
        return _managerProvider.Manager?.CurrentVersion 
            ?? GetVersionFromAssembly();
    }
    
    // IUpdateChecker
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(...)
    {
        var (result, _) = await CheckForUpdateCoreAsync(...);
        return result;
    }
    
    // IUpdateStateSource
    public bool IsDownloading => _downloader.IsDownloading;
    public string? LastKnownAvailableVersion => _lastKnownAvailableVersion;
    public event EventHandler? UpdateStateChanged;
    
    // IUpdateDownloader
    public async Task<UpdateDownloadResult> DownloadUpdateAsync(...)
    {
        var (result, update) = await CheckForUpdateCoreAsync(...);
        if (update != null)
        {
            var downloadResult = await _downloader.DownloadAsync(...);
            RaiseStateChanged();
            return downloadResult;
        }
        ...
    }
    
    // IUpdateInstaller
    public void ApplyDownloadedUpdateAndRestart()
    {
        if (_managerProvider.Manager is { } manager && 
            _downloader.DownloadedAsset is { } asset)
        {
            _installer.Apply(manager, asset);
        }
    }
    
    private async Task<(UpdateCheckResult, UpdateInfo?)> CheckForUpdateCoreAsync(...)
    {
        if (!_managerProvider.IsAvailable)
        {
            SetAvailableVersion(null);
            return (NotInstalled, null);
        }
        
        var (result, update) = await _checker.CheckAsync(...);
        SetAvailableVersion(result.AvailableVersion);
        return (result, update);
    }
}
```

## Testing Strategy

### Existing Tests (UpdateServiceTests.cs)
- **Keep unchanged** - All 14 tests continue using CreateForTesting factory
- Tests inject UpdateManager through the test seam
- VelopackManagerProvider wraps injected manager in test mode

### New Tests
Each internal component gets isolated unit tests:

**VelopackManagerProviderTests** (3 tests):
- Manager initialization with GitHub URL
- Manager initialization with local URL
- NotInitializedException handling

**UpdateCheckerTests** (4 tests):
- Successful update available
- No update available
- Timeout handling
- Exception handling (NotInstalledException, general exceptions)

**UpdateDownloaderTests** (3 tests):
- Successful download
- Download cancellation
- Already downloading state

**UpdateInstallerTests** (2 tests):
- Successful apply
- Exception handling

**Total:** 12 new tests + 14 existing = 26 tests for update subsystem

## Migration Path

### Phase 1: Create Internal Components
1. Create VelopackManagerProvider class
2. Create UpdateChecker class with tests
3. Create UpdateDownloader class with tests
4. Create UpdateInstaller class with tests

### Phase 2: Refactor UpdateService
1. Add component fields to UpdateService
2. Initialize components in constructors
3. Delegate interface methods to components
4. Keep CreateForTesting working with test-injected manager
5. Run all 14 existing tests - verify they pass

### Phase 3: Verification
1. Run full test suite (555 tests)
2. Manual testing of update check, download, install
3. Verify no behavioral changes

## Design Decisions

### Why internal classes instead of public interfaces?

**Decision:** Use internal classes for the extracted components.

**Rationale:**
- Components are implementation details of UpdateService
- No external consumers need these abstractions
- Simplifies DI registration (no new services)
- UpdateService tests already use CreateForTesting factory
- Easier to evolve internal structure later

### Why pass UpdateManager as parameter?

**Decision:** Components receive UpdateManager as method parameter, not constructor dependency.

**Rationale:**
- Preserves existing test injection pattern (CreateForTesting)
- Manager is lazily initialized; passing it ensures it exists
- Simplifies component construction (no need for IVelopackManagerProvider dependency)
- Makes it clear that components are stateless operations on manager

### Why keep UpdateService as facade?

**Decision:** Keep UpdateService implementing all public interfaces, delegating to components.

**Rationale:**
- Zero breaking changes for consumers
- Existing DI registration stays unchanged
- UpdateWorkflow continues working with split interfaces
- Test compatibility preserved

## File Structure

```
src/CLIHub.Core/Updates/
  ├── IUpdateService.cs              (unchanged)
  ├── IUpdateChecker.cs              (unchanged)
  ├── IUpdateDownloader.cs           (unchanged)
  ├── IUpdateInstaller.cs            (unchanged)
  ├── IUpdateStateSource.cs          (unchanged)
  ├── IUpdateVersionProvider.cs      (unchanged)
  ├── UpdateService.cs               (refactored - ~150 lines)
  ├── VelopackManagerProvider.cs     (new - internal)
  ├── UpdateChecker.cs               (new - internal)
  ├── UpdateDownloader.cs            (new - internal)
  ├── UpdateInstaller.cs             (new - internal)
  ├── ReleaseNotesService.cs         (unchanged)
  └── UpdateControlLogic.cs          (unchanged)

tests/CLIHub.Core.Tests/
  ├── UpdateServiceTests.cs          (unchanged - 14 tests)
  ├── VelopackManagerProviderTests.cs (new - 3 tests)
  ├── UpdateCheckerTests.cs          (new - 4 tests)
  ├── UpdateDownloaderTests.cs       (new - 3 tests)
  ├── UpdateInstallerTests.cs        (new - 2 tests)
  └── UpdateControlLogicTests.cs     (unchanged)
```

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Breaking existing UpdateService tests | Keep CreateForTesting factory working exactly as before |
| Thread-safety issues with state | Preserve existing locking patterns in UpdateDownloader |
| Regression in update behavior | Run full manual update cycle after refactoring |
| Over-engineering internal structure | Keep components internal, no DI registration |
