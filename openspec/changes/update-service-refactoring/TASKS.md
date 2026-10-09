# Update Service Refactoring - Tasks

## Phase 1: Create Internal Components (4-5 hours)

### Task 1.1: Create VelopackManagerProvider (1 hour)
**Goal:** Extract UpdateManager initialization logic into a dedicated provider.

**Steps:**
1. Create `src/CLIHub.Core/Updates/VelopackManagerProvider.cs`
2. Implement production constructor with lazy initialization
3. Implement test constructor that wraps injected manager
4. Add IsAvailable property
5. Create `tests/CLIHub.Core.Tests/VelopackManagerProviderTests.cs`
6. Write 3 tests:
   - Constructor_WithGitHubUrl_CreatesGitHubSource
   - Constructor_WithLocalUrl_CreatesDefaultSource
   - Manager_WhenNotInitialized_ReturnsNull
7. Run tests: `dotnet test --filter VelopackManagerProvider`

**Acceptance:**
- VelopackManagerProvider.cs created (~60 lines)
- 3 tests pass
- Handles NotInitializedException correctly

---

### Task 1.2: Create UpdateChecker (1.5 hours)
**Goal:** Extract update checking logic with timeout handling.

**Steps:**
1. Create `src/CLIHub.Core/Updates/UpdateChecker.cs`
2. Implement CheckAsync method with:
   - Timeout handling using Task.WhenAny
   - Late check observation (fire-and-forget)
   - Exception handling (NotInstalledException, OperationCanceledException, general)
   - Result mapping to UpdateCheckResult
3. Create `tests/CLIHub.Core.Tests/UpdateCheckerTests.cs`
4. Write 4 tests:
   - CheckAsync_WithAvailableUpdate_ReturnsUpdateAvailable
   - CheckAsync_WithNoUpdate_ReturnsUpToDate
   - CheckAsync_WhenTimeout_ReturnsFailed
   - CheckAsync_WhenNotInstalled_ReturnsNotInstalled
5. Run tests: `dotnet test --filter UpdateChecker`

**Acceptance:**
- UpdateChecker.cs created (~80 lines)
- 4 tests pass
- Timeout behavior works correctly
- Exception handling matches original logic

---

### Task 1.3: Create UpdateDownloader (1.5 hours)
**Goal:** Extract download orchestration with state management.

**Steps:**
1. Create `src/CLIHub.Core/Updates/UpdateDownloader.cs`
2. Implement state fields:
   - `_isDownloading` flag with lock-based synchronization
   - `_downloadedAsset` storage
   - `_downloadGate` object for locking
3. Implement DownloadAsync method with:
   - AlreadyDownloading check
   - manager.DownloadUpdatesAsync call
   - Asset storage on success
   - Exception handling
4. Create `tests/CLIHub.Core.Tests/UpdateDownloaderTests.cs`
5. Write 3 tests:
   - DownloadAsync_Success_StoresAssetAndReturnsDownloaded
   - DownloadAsync_WhenCancelled_ReturnsFailed
   - IsDownloading_DuringDownload_ReturnsTrue
6. Run tests: `dotnet test --filter UpdateDownloader`

**Acceptance:**
- UpdateDownloader.cs created (~70 lines)
- 3 tests pass
- Thread-safe state management works correctly
- Downloaded asset is stored for later installation

---

### Task 1.4: Create UpdateInstaller (30 minutes)
**Goal:** Extract update application logic.

**Steps:**
1. Create `src/CLIHub.Core/Updates/UpdateInstaller.cs`
2. Implement Apply method:
   - Call manager.ApplyUpdatesAndRestart
   - Exception handling with logging and re-throw
3. Create `tests/CLIHub.Core.Tests/UpdateInstallerTests.cs`
4. Write 2 tests:
   - Apply_CallsManagerApplyUpdatesAndRestart
   - Apply_WhenException_LogsAndRethrows
5. Run tests: `dotnet test --filter UpdateInstaller`

**Acceptance:**
- UpdateInstaller.cs created (~30 lines)
- 2 tests pass
- Exceptions are logged and re-thrown

---

### Phase 1 Checkpoint
Run all new tests together:
```bash
dotnet test --filter "VelopackManagerProvider|UpdateChecker|UpdateDownloader|UpdateInstaller"
```

**Expected:** 12 new tests pass

---

## Phase 2: Refactor UpdateService (2-3 hours)

### Task 2.1: Add Component Fields (15 minutes)
**Goal:** Add fields for internal components to UpdateService.

**Steps:**
1. Open `src/CLIHub.Core/Updates/UpdateService.cs`
2. Add private readonly fields:
   - `_managerProvider` (VelopackManagerProvider)
   - `_checker` (UpdateChecker)
   - `_downloader` (UpdateDownloader)
   - `_installer` (UpdateInstaller)
3. Remove old fields that will be replaced:
   - `_defaultManager` (moved to provider)
   - `_injectedManager` (moved to provider)
   - `_isDownloading` (moved to downloader)
   - `_downloadedAsset` (moved to downloader)
   - `_downloadGate` (moved to downloader)

**Acceptance:**
- Fields added
- Build succeeds (warnings expected for unused fields)

---

### Task 2.2: Update Constructors (30 minutes)
**Goal:** Initialize components in constructors.

**Steps:**
1. Update public constructor:
   ```csharp
   public UpdateService(ILogger<UpdateService> logger)
   {
       _logger = logger;
       _checkTimeout = CheckTimeout;
       _managerProvider = new VelopackManagerProvider(RepositoryUrl, logger);
       _checker = new UpdateChecker(logger);
       _downloader = new UpdateDownloader(logger);
       _installer = new UpdateInstaller(logger);
   }
   ```
2. Update private test constructor:
   ```csharp
   private UpdateService(
       ILogger<UpdateService> logger, 
       UpdateManager manager, 
       TimeSpan? checkTimeout)
   {
       _logger = logger;
       _checkTimeout = checkTimeout ?? CheckTimeout;
       _managerProvider = new VelopackManagerProvider(manager);
       _checker = new UpdateChecker(logger);
       _downloader = new UpdateDownloader(logger);
       _installer = new UpdateInstaller(logger);
   }
   ```
3. Keep CreateForTesting factory unchanged

**Acceptance:**
- Constructors updated
- Build succeeds

---

### Task 2.3: Refactor GetCurrentVersion (15 minutes)
**Goal:** Delegate to VelopackManagerProvider.

**Steps:**
1. Replace `Manager` property usage with `_managerProvider.Manager`
2. Update GetCurrentVersion():
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
3. Remove old Manager property and CreateDefaultManager method

**Acceptance:**
- GetCurrentVersion refactored
- Build succeeds
- Version-related tests pass

---

### Task 2.4: Refactor CheckForUpdatesAsync (30 minutes)
**Goal:** Delegate to UpdateChecker.

**Steps:**
1. Update CheckForUpdateCoreAsync:
   ```csharp
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
2. Remove ObserveLateUpdateCheckAsync (now in UpdateChecker)

**Acceptance:**
- CheckForUpdatesAsync refactored
- Build succeeds
- Check-related tests pass

---

### Task 2.5: Refactor DownloadUpdateAsync (45 minutes)
**Goal:** Delegate to UpdateDownloader.

**Steps:**
1. Replace _isDownloading usage with _downloader.IsDownloading
2. Update DownloadUpdateAsync:
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
3. Update IsDownloading property:
   ```csharp
   public bool IsDownloading => _downloader.IsDownloading;
   ```

**Acceptance:**
- DownloadUpdateAsync refactored
- Build succeeds
- Download-related tests pass

---

### Task 2.6: Refactor ApplyDownloadedUpdateAndRestart (15 minutes)
**Goal:** Delegate to UpdateInstaller.

**Steps:**
1. Update ApplyDownloadedUpdateAndRestart:
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

**Acceptance:**
- ApplyDownloadedUpdateAndRestart refactored
- Build succeeds
- Apply-related tests pass

---

### Phase 2 Checkpoint
Run all UpdateService tests:
```bash
dotnet test --filter UpdateService
```

**Expected:** All 14 existing tests pass

---

## Phase 3: Verification and Cleanup (1 hour)

### Task 3.1: Run Full Test Suite (15 minutes)
**Goal:** Verify no regressions across entire codebase.

**Steps:**
1. Build solution: `dotnet build CLIHub.sln -c Release`
2. Run all tests: `dotnet test CLIHub.sln -c Release`
3. Verify: 555 + 12 new = 567 tests pass

**Acceptance:**
- Build succeeds with no warnings in Updates/ files
- All 567 tests pass

---

### Task 3.2: Manual Testing (30 minutes)
**Goal:** Verify update functionality in running application.

**Steps:**
1. Run application: `dotnet run --project src/CLIHub/CLIHub.csproj`
2. Open settings window
3. Click "Check for Updates"
4. Verify:
   - Check completes without errors
   - Status displays correctly (up-to-date or update available)
   - If update available, download button works
   - Downloaded update can be applied (if testing with actual update)

**Acceptance:**
- Manual update check works
- No exceptions in logs
- UI responds correctly

---

### Task 3.3: Code Metrics Verification (15 minutes)
**Goal:** Confirm refactoring achieved size reduction goals.

**Steps:**
1. Count lines in UpdateService.cs: should be ~150 lines (down from 317)
2. Count total lines in new components: ~240 lines
3. Verify component responsibilities are clear and focused
4. Check that no TODOs or temporary code remains

**Acceptance:**
- UpdateService.cs is ~150 lines
- Each component has single clear responsibility
- No technical debt introduced

---

## Summary

**Total Estimated Time:** 7-9 hours

| Phase | Tasks | Time | Tests Added |
|-------|-------|------|-------------|
| Phase 1: Components | 4 tasks | 4-5h | 12 |
| Phase 2: Refactoring | 6 tasks | 2-3h | 0 |
| Phase 3: Verification | 3 tasks | 1h | 0 |
| **Total** | **13 tasks** | **7-9h** | **12** |

**Final Test Count:** 567 (555 existing + 12 new)

**Success Metrics:**
- ✅ UpdateService reduced from 317 to ~150 lines
- ✅ Four focused components created (~240 total lines)
- ✅ All existing tests pass without modification
- ✅ 12 new component tests added
- ✅ No breaking changes to public APIs
- ✅ Manual testing confirms functionality preserved
