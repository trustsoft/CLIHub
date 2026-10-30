# Tasks: Scanning and Runtime Probe Unification

## Stage 1: Agent Probe Policy + Update Check Source

### 1.1 AgentProbePolicy Extraction
- [x] 1.1.1 Create `src/CLIHub.Core/Agents/AgentProbePolicy.cs` with `DefaultTtl`, `DefaultProbeTimeout`, `ResolveTtl(int?)`, `ResolveTimeout(int?)` as internal static members
- [x] 1.1.2 Update `AgentVersionService.cs`: change `DefaultTtl` and `DefaultProbeTimeout` to public aliases of `AgentProbePolicy` (e.g., `public static TimeSpan DefaultTtl => AgentProbePolicy.DefaultTtl;`)
- [x] 1.1.3 Update `AgentVersionService.Ttl` property to call `AgentProbePolicy.ResolveTtl(this.preferencesStore.Load().AgentProbeTtlMinutes)`
- [x] 1.1.4 Update `AgentDetectionService.cs`: change `DefaultTtl` constant to internal alias `AgentProbePolicy.DefaultTtl` (preserve for AgentDetectionServiceTests.cs:118), update `Ttl` property to call `AgentProbePolicy.ResolveTtl(...)`
- [x] 1.1.5 Update `AgentUpdateAvailabilityService.Ttl` property to call `AgentProbePolicy.ResolveTtl(...)` instead of referencing `AgentVersionService.DefaultTtl`
- [x] 1.1.6 Update `AgentReleaseClient.ProbeTimeout` property to call `AgentProbePolicy.ResolveTimeout(this.preferencesStore.Load().AgentProbeTimeoutSeconds)`
- [x] 1.1.7 Create `tests/CLIHub.Core.Tests/Agents/AgentProbePolicyTests.cs` with tests for `ResolveTtl` (positive/zero/negative/null), `ResolveTimeout` (positive/zero/negative/null), default constants
- [x] 1.1.8 Run `dotnet build CLIHub.sln` (must succeed)
- [x] 1.1.9 Run `dotnet test CLIHub.sln` (all tests must pass, including new AgentProbePolicyTests)

### 1.2 UpdateCheckSource Extraction
- [x] 1.2.1 Create `src/CLIHub.Core/Models/Plugins/UpdateCheckSource.cs` with `Npm`/`GitHubReleases` constants, `SourceKind` enum, `Problem` enum, `TryResolve(PluginUpdateCheck?, out SourceKind, out Problem)` (returns true only when `kind != None`) as internal static members
- [x] 1.2.2 Update `AgentUpdateAvailabilityService.IsSupportedBlock(Plugin plugin, PluginUpdateCheck updateCheck)`: replace inline source checks with `UpdateCheckSource.TryResolve(updateCheck, out _, out var problem)`, map `Problem` to existing log messages
- [x] 1.2.3 Update `AgentReleaseClient.TryBuildRequest(...)`: replace inline source checks with `UpdateCheckSource.TryResolve(updateCheck, out var kind, out var problem)`, map `Problem` to existing log messages
- [x] 1.2.4 Update `AgentProcessMatcher.DeclaresNpmUpdateCheck(...)`: replace inline check with `UpdateCheckSource.TryResolve(plugin.UpdateCheck, out var kind, out _) && kind == SourceKind.Npm` (null-safe)
- [x] 1.2.5 Create `tests/CLIHub.Core.Tests/Models/Plugins/UpdateCheckSourceTests.cs` with tests for `TryResolve` (null, npm/githubReleases with/without package/repo, blank source, unknown source)
- [x] 1.2.6 Run `dotnet build CLIHub.sln` (must succeed)
- [x] 1.2.7 Run `dotnet test CLIHub.sln` (all tests must pass, including new UpdateCheckSourceTests; existing agent tests must pass unchanged)

### Stage 1 Gate
- [x] 1.G.1 Verify `dotnet build CLIHub.sln` succeeds with zero errors and zero new warnings
- [x] 1.G.2 Verify `dotnet test CLIHub.sln` reports all tests passed (baseline: 1,282 + new AgentProbePolicy + new UpdateCheckSource tests)
- [x] 1.G.3 Verify `SettingsViewModel.cs` lines 342–350, 361, 366 still compile and reference `AgentVersionService.DefaultTtl` / `DefaultProbeTimeout` (unchanged public API)

---

## Stage 2: ProjectScanningService Consolidation

### 2.1 ScanPass Record and Unified Runner
- [x] 2.1.1 Create private `ScanPass` record in `ProjectScanningService.cs` with `ProjectScanningOptions Options`, `IReadOnlyList<string> AllWatchedRoots`, `bool IsSingleRoot`, `string? TargetRoot`
- [x] 2.1.2 Extract `RunPassAsync(ScanPass pass)` method: unified pipeline with status before (based on `configured = pass.AllWatchedRoots.Count > 0`; single-root: `State = Scanning`, `IsScanning = true`, do NOT set `IsWatching`; full: `State = configured ? Scanning : NotConfigured`, `IsWatching = configured`, `IsScanning = configured`) / enumerate with `pass.Options` / reconcile / status after (single-root: `this.status with { State = this.status.IsWatching ? Idle : NotConfigured, IsScanning = false, ... }` do NOT set `IsWatching`; full: `this.status with { State = configured ? Idle : NotConfigured, IsWatching = configured, IsScanning = false, ... }`); coverage merge uses `if (!pass.IsSingleRoot) scannedDirectoryCounts.Clear();`
- [x] 2.1.3 Update `RunScanAsync()`: construct `ScanPass` with `Options = options`, `AllWatchedRoots = options.WatchedDirectories`, `IsSingleRoot = false`, `TargetRoot = null`, call `RunPassAsync(pass)`
- [x] 2.1.4 Update `RunRootScanAsync(string root)`: construct `rootOptions = options.ForRoot(root) with { WatchedDirectories = new[] { root } }`, then construct `ScanPass` with `Options = rootOptions`, `AllWatchedRoots = options.WatchedDirectories` (the original full list), `IsSingleRoot = true`, `TargetRoot = root`, call `RunPassAsync(pass)`

### 2.2 Unified Removal Algorithm
- [x] 2.2.1 Extract `ComputePassRemovals(ScanPass pass, IReadOnlyList<string> discoveredPaths, IReadOnlyList<string> scannedRoots)` method: unified removal with guards for `pass.IsSingleRoot && scannedRoots.Count == 0` (return empty); only when `pass.IsSingleRoot`, derive `otherRoots` from `pass.AllWatchedRoots` excluding `pass.TargetRoot` via `ProjectPathPolicy.AreEqual` and normalize each with `ProjectPathPolicy.Normalize`; `pass.IsSingleRoot && !IsUnder(path, pass.TargetRoot)` (skip); `pass.IsSingleRoot && otherRoots.Any(...)` (skip "other roots still own it"); otherwise remove when the project is under no watched root or is under a scanned root and was not discovered this pass
- [x] 2.2.2 Update `RunPassAsync(...)` reconcile block: call `ComputePassRemovals(pass, result.DiscoveredPaths, result.ScannedRoots)` (note: only three parameters, not four)
- [x] 2.2.3 Delete `ComputeRemovals(...)` and `ComputeRootRemovals(...)` methods (now replaced by `ComputePassRemovals`)

### 2.3 Reset-to-NotConfigured Helper
- [x] 2.3.1 Extract `PublishNotConfiguredAndClearCoverage()` method: posts `scannedDirectoryCounts.Clear()` and publishes `this.status with { State = NotConfigured, IsWatching = false, IsScanning = false, ScannedDirectoryCount = 0 }` (preserves `LastScanUtc`, `DiscoveredCount`, `Error`)
- [x] 2.3.2 Update `Stop()`: detach watcher/timer, call `PublishNotConfiguredAndClearCoverage()`
- [x] 2.3.3 Update `Configure()` no-roots branch: detach old watcher/timer, call `PublishNotConfiguredAndClearCoverage()`, then `RequestScan()`

### 2.4 Tests
- [x] 2.4.1 Add `ProjectScanningServiceTests.Reload_FullScan_ClearsCoverageBeforeFilling` test: drive `Reload` with fake environment, assert full scan clears coverage by checking `service.Status.ScannedDirectoryCount`
- [x] 2.4.2 Add `ProjectScanningServiceTests.Rescan_SingleRoot_OverwritesOnlyScannedRoot` test: drive `Rescan` with fake environment, assert single-root rescan preserves other roots' coverage by checking `service.Status.ScannedDirectoryCount`
- [x] 2.4.3 Add `ProjectScanningServiceTests.Rescan_ProjectAlsoUnderAnotherWatchedRoot_KeepsProject` test: assert "other roots still own it" guard via `ReconcileDiscoveredProjects` calls (already covered by the existing `Rescan_KeepsProjectUnderAnotherWatchedRoot`; no duplicate added)
- [x] 2.4.4 Add `ProjectScanningServiceTests.Rescan_MissingRoot_RemovesNothing` test: assert `scannedRoots.Count == 0` guard via `ReconcileDiscoveredProjects` calls (already exists verbatim; no duplicate added)
- [x] 2.4.5 Run `dotnet build CLIHub.sln` (must succeed)
- [x] 2.4.6 Run `dotnet test CLIHub.sln` (all tests must pass, including new ProjectScanningService tests; existing 487-line ProjectScanningServiceTests must pass unchanged except for new tests)

### Stage 2 Gate
- [x] 2.G.1 Verify `dotnet build CLIHub.sln` succeeds with zero errors and zero new warnings
- [x] 2.G.2 Verify `dotnet test CLIHub.sln` reports all tests passed (baseline + Stage 1 tests + new ProjectScanningService tests)
- [x] 2.G.3 Verify `ProjectScanningService.cs` line count reduced by ~80 lines (from 610 to ~530) — actual 579 (−31); the duplication is gone but the unified pass/removal guard shape and docs cost lines. Accepted deviation (verifier PASS).
- [x] 2.G.4 Verify error log messages "Project scan failed" and "Project scan failed for watched directory {Root}" remain unchanged (grep source for exact text)

---

## Stage 3: WindowsRuntimeLocator Consolidation

### 3.1 IsUnderDirectory Guard Wrapper
- [x] 3.1.1 Add `IsUnderDirectoryGuarded(string path, string directory)` private method in `WindowsRuntimeLocator.cs`: returns `false` for blank `directory`, calls `ProjectPathPolicy.IsUnder(path, directory)`, catches `ArgumentException`/`NotSupportedException`/`PathTooLongException`, returns `false` on exception
- [x] 3.1.2 Update `LocateGitBash()` PATH loop (line 297): replace `IsUnderDirectory(directory, windowsDirectory)` with `IsUnderDirectoryGuarded(directory, windowsDirectory)`
- [x] 3.1.3 Delete `IsUnderDirectory(string path, string directory)` method (lines 515–543, now replaced by `IsUnderDirectoryGuarded`)

### 3.2 Unified Executable Probe
- [x] 3.2.1 Add `TryExecutable(string container, IReadOnlyList<string> segments, string label)` private method: blank check (returns null for blank `container`), combines `container` + `segments` with `Path.Combine` (catches `ArgumentException`, logs "Rejected {label} {Container} because it is not a valid path", returns null), checks `environment.FileExists(candidate)` (logs "Rejected {label} {Container} because {Candidate} does not exist" on miss), calls `ToAbsolutePath(candidate)`
- [x] 3.2.2 Update `TryGitCandidate(string installRoot)`: replace body with `return TryExecutable(installRoot, new[] { "bin", BashExecutableName }, "install root");`
- [x] 3.2.3 Update `TryPowerShellCandidate(string installDirectory)`: replace body with `return TryExecutable(installDirectory, new[] { PwshExecutableName }, "install directory");`

### 3.3 Unified PATH Scan
- [x] 3.3.1 Add `LocateOnPath(string displayName, string executableName, string? excludedRoot, string? excludedRootLabel)` private method: loops `environment.GetPathDirectories()`, filters with `IsUnderDirectoryGuarded(directory, excludedRoot)` if `excludedRoot` is non-null (logs "Rejected PATH entry {Directory} because it is under the {ExcludedRootLabel} directory {ExcludedRoot}" using the provided `excludedRootLabel`), combines with `executableName` via `Path.Combine` (catches `ArgumentException`, logs "Rejected PATH entry {Directory} because it is not a valid path"), checks `environment.FileExists(candidate)` (logs "Rejected PATH candidate {Candidate} because the file does not exist" on miss), calls `ToAbsolutePath(candidate)`, logs "Resolved {DisplayName} to {ExecutablePath} from the PATH entry {Directory}" on success, returns resolved path or null
- [x] 3.3.2 Update `LocateGitBash()` PATH scan (lines 294–329): replace the PATH loop with `var pathCandidate = this.LocateOnPath("Git Bash", BashExecutableName, windowsDirectory, "Windows"); if (pathCandidate is not null) { return pathCandidate; }` (insert after well-known roots loop, before the existing "Checked common Git for Windows..." warning)
- [x] 3.3.3 Update `LocatePowerShell7()` PATH scan (lines 369–395): replace the PATH loop with `var pathCandidate = this.LocateOnPath("PowerShell 7", PwshExecutableName, null, null); if (pathCandidate is not null) { return pathCandidate; }` (insert after well-known path check, before the existing "Checked common PowerShell Core..." warning)

### 3.4 Tests
- [x] 3.4.1 Add `WindowsRuntimeLocatorTests.Locate_GitBash_BlankInstallRoot_ReturnsNull` test: drive `Locate(RuntimeKind.GitBash)` with blank install root, assert null (tests use `NullLogger`)
- [x] 3.4.2 Add `WindowsRuntimeLocatorTests.Locate_GitBash_InvalidPathEntry_ReturnsNullOrResolvesFromOtherSource` test: drive `Locate(RuntimeKind.GitBash)` with invalid PATH entry, assert resolved path or null
- [x] 3.4.3 Add `WindowsRuntimeLocatorTests.Locate_PowerShell7_BlankInstallDirectory_ReturnsNull` test: drive `Locate(RuntimeKind.PowerShell7)` with blank install directory, assert null
- [x] 3.4.4 Run `dotnet build CLIHub.sln` (must succeed)
- [x] 3.4.5 Run `dotnet test CLIHub.sln` (all tests must pass, including new WindowsRuntimeLocator tests; existing 253-line WindowsRuntimeLocatorTests must pass unchanged except for new tests)

### Stage 3 Gate
- [x] 3.G.1 Verify `dotnet build CLIHub.sln` succeeds with zero errors and zero new warnings
- [x] 3.G.2 Verify `dotnet test CLIHub.sln` reports all tests passed (baseline + Stage 1 tests + Stage 2 tests + new WindowsRuntimeLocator tests)
- [x] 3.G.3 Verify `WindowsRuntimeLocator.cs` line count reduced by ~60 lines (from 544 to ~484) — actual 504 (−40); probe/PATH unification is complete but the four-parameter PATH helper and docs cost lines. Accepted deviation (verifier PASS).
- [x] 3.G.4 Verify log messages remain byte-identical (grep source for Git Bash/PowerShell 7 log strings and compare with design.md)

---

## Stage 4: Final Gates and User Smoke

### 4.1 Build and Test Gates
- [x] 4.1.1 Run `dotnet build CLIHub.sln` (must succeed with zero errors and zero new warnings)
- [x] 4.1.2 Run `dotnet test CLIHub.sln` (all tests must pass; baseline: 1,282 + new tests from Stages 1-3)
- [x] 4.1.3 Run `openspec validate --all` (must report 30 passed (29 specs + this change), 0 failed)

### 4.2 User-Side Manual Smoke
**Note:** This smoke test MUST be completed by the user before commit/archive approval. The implementer reports that it is ready but CANNOT confirm the runtime paths resolve correctly.

- [x] 4.2.1 Launch CLIHub application (`dotnet run --project src/CLIHub/CLIHub.csproj`)
- [x] 4.2.2 Select a project and launch an agent with **Git Bash** runtime (if Git for Windows is installed): verify the agent launches successfully and the terminal opens
- [x] 4.2.3 Select a project and launch an agent with **PowerShell 7** runtime (if PowerShell 7 is installed): verify the agent launches successfully and the terminal opens
- [x] 4.2.4 Open Settings → Agents page, click **Detect** for an installed agent: verify detection succeeds and the version is displayed
- [x] 4.2.5 Open Settings → Agents page, click **Check for updates** for an installed agent: verify the update check completes and the status is displayed (UpToDate/UpdateAvailable/Unknown)
- [x] 4.2.6 Confirm that the runtime paths (Git Bash `bash.exe`, PowerShell 7 `pwsh.exe`) resolve as before (check logs under `%APPDATA%\CLIHub\logs\` for "Resolved Git Bash to..." and "Resolved PowerShell 7 to..." messages; compare with pre-change logs if available)
- [x] 4.2.7 Report smoke result to orchestrator/user: PASS (all checks succeeded) or FAIL (which check failed, logs excerpt, error message) — PASS, user-confirmed 2026-10-30

### Stage 4 Gate
- [x] 4.G.1 Verify all build/test/validate gates pass
- [x] 4.G.2 Verify user-side manual smoke result is PASS (if FAIL, block commit/archive and report)
- [x] 4.G.3 Verify `git status --porcelain` shows only changes under `openspec/changes/scanning-and-runtime-probe-unification/` and `src/CLIHub.Core/`, `src/CLIHub.Core.Windows/`, `tests/CLIHub.Core.Tests/`, `tests/CLIHub.Core.Windows.Tests/` (no unrelated files, no `.opencode/` or `docs/backlog.md` changes)

---

## Completion Checklist

- [x] All 4 stages completed with all tasks checked
- [x] All stage gates passed
- [x] User-side manual smoke test PASS (task 4.2.7)
- [x] `dotnet build CLIHub.sln` succeeds
- [x] `dotnet test CLIHub.sln` reports all tests passed (baseline + new tests)
- [x] `openspec validate --all` reports 30 passed (29 specs + this change), 0 failed
- [x] `git status --porcelain` shows only expected changes (no unrelated files)
- [x] Ready for user commit/archive approval (implementer reports completion and awaits user confirmation)
