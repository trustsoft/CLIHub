# Design: Scanning and Runtime Probe Unification

## Verified Current State

### 1. ProjectScanningService (src/CLIHub.Core/Scanning/ProjectScanningService.cs)

**Duplication:**
- `RunScanAsync()` at line 381 and `RunRootScanAsync(string root)` at line 458 duplicate the same load/publish/enumerate/reconcile/count/publish pipeline structure.
- `ComputeRemovals(...)` at line 554 and `ComputeRootRemovals(...)` at line 503 are two removal algorithms with different guards (full-pass removal vs single-root removal with "other roots still own it" check).
- `Stop()` at line 158 and the `Configure()` no-roots branch at lines 231–247 duplicate the "clear coverage + publish NotConfigured" block.

**Current behavior (verified from source):**
- `RunScanAsync()` (lines 381–430):
  - Sets `State = configured ? Scanning : NotConfigured`, `IsWatching = configured`, `IsScanning = configured`
  - Enumerates with `RepositoryDiscovery.Enumerate(options, plugins, errorLogger)`
  - Posts reconcile block that:
    - Computes removals with `ComputeRemovals(discoveredPaths, watchedDirectories, scannedRoots)`
    - Calls `projects.ReconcileDiscoveredProjects(discoveredPaths, removed)`
    - **Clears** `scannedDirectoryCounts`, then fills it from `result.ScannedDirectoryCounts`
    - Counts registered discovered projects (not this pass's paths)
    - Publishes final status: `State = configured ? Idle : NotConfigured`, `IsWatching = configured`, `IsScanning = false`, `LastScanUtc`, `DiscoveredCount`, `ScannedDirectoryCount`, `Error = null`

- `RunRootScanAsync(string root)` (lines 458–501):
  - Sets `State = Scanning`, `IsScanning = true`
  - Enumerates with `RepositoryDiscovery.Enumerate(rootOptions, plugins, errorLogger)` where `rootOptions = options.ForRoot(root) with { WatchedDirectories = new[] { root } }`
  - Posts reconcile block that:
    - Computes removals with `ComputeRootRemovals(root, discoveredPaths, scannedRoots, watchedDirectories)`
    - Calls `projects.ReconcileDiscoveredProjects(discoveredPaths, removed)`
    - **Overwrites** entries in `scannedDirectoryCounts` from `result.ScannedDirectoryCounts` (does NOT clear first)
    - Counts registered discovered projects
    - Publishes final status: `State = this.status.IsWatching ? Idle : NotConfigured`, `IsScanning = false`, `LastScanUtc`, `DiscoveredCount`, `ScannedDirectoryCount`, `Error = null`

- `ComputeRemovals(discoveredPaths, watchedRoots, scannedRoots)` (lines 554–590):
  - Full-pass removal: for each discovered project, remove it when:
    - It is under NO watched root (lines 577–581), OR
    - It is under a scanned root AND was not discovered this pass (lines 583–586)

- `ComputeRootRemovals(root, discoveredPaths, scannedRoots, watchedRoots)` (lines 503–552):
  - Single-root removal guard: if `scannedRoots.Count == 0` (missing/unreadable root), return empty (lines 509–512)
  - For each discovered project under `root`:
    - If it is also under another watched root (lines 540–543), keep it ("other roots still own it")
    - Otherwise, if not discovered this pass, remove it (lines 545–548)

- `Stop()` (lines 158–183):
  - Detaches watcher/timer
  - Posts: clears `scannedDirectoryCounts`, publishes `State = NotConfigured`, `IsWatching = false`, `IsScanning = false`, `ScannedDirectoryCount = 0`

- `Configure()` no-roots branch (lines 231–247):
  - Detaches old watcher/timer
  - Posts: clears `scannedDirectoryCounts`, publishes `State = NotConfigured`, `IsWatching = false`, `IsScanning = false`, `ScannedDirectoryCount = 0`
  - Still calls `RequestScan()` to reconcile discovered projects under removed roots

**Error logging:**
- `RunScanAsync` (line 371): "Project scan failed"
- `RunRootScanAsync` (line 449): "Project scan failed for watched directory {Root}"

### 2. WindowsRuntimeLocator (src/CLIHub.Core.Windows/WindowsRuntimeLocator.cs)

**Duplication:**
- `LocateGitBash()` (lines 268–336) and `LocatePowerShell7()` (lines 338–402) repeat the PATH scan structure (lines 295–329 vs 369–395).
- `TryGitCandidate(string installRoot)` (lines 446–471) and `TryPowerShellCandidate(string installDirectory)` (lines 473–501) repeat the candidate probe (combine container + relative path, check existence, resolve to absolute).
- `IsUnderDirectory(path, directory)` (lines 515–543) reimplements `ProjectPathPolicy.IsUnder`.

**Current behavior:**
- Git Bash lookup order (lines 268–336):
  1. Registry InstallPath views (lines 270–280): calls `ReadGitRegistryInstallPaths()`, then `TryGitCandidate(installPath)` for each; logs "Resolved Git Bash to {ExecutablePath} from the Git for Windows registry InstallPath"
  2. Well-known roots (lines 282–292): calls `environment.GetGitWellKnownInstallRoots()`, then `TryGitCandidate(root)` for each; logs "Resolved Git Bash to {ExecutablePath} from the well-known install location {InstallRoot}"
  3. PATH (lines 294–329): calls `environment.GetPathDirectories()`, filters Windows-directory entries with `IsUnderDirectory(directory, windowsDirectory)` (logs "Rejected PATH entry {Directory} because it is under the Windows directory {WindowsDirectory}"), combines with `BashExecutableName`, checks existence, resolves; logs "Resolved Git Bash to {ExecutablePath} from the PATH entry {Directory}"
  4. Warning (lines 331–335): logs lookup order when nothing found

- PowerShell 7 lookup order (lines 338–402):
  1. Registry InstallPath (lines 340–351): calls `ReadPowerShellRegistryInstallPath()`, then `TryPowerShellCandidate(registryInstallPath)`; logs "Resolved PowerShell 7 to {ExecutablePath} from the PowerShell Core registry InstallPath"
  2. Well-known path (lines 353–364): calls `environment.GetPowerShellWellKnownExecutablePath()`, checks existence, resolves; logs "Resolved PowerShell 7 to {ExecutablePath} from the well-known install location"
  3. PATH (lines 369–395): calls `environment.GetPathDirectories()`, NO Windows-directory exclusion (comment at lines 366–368), combines with `PwshExecutableName`, checks existence, resolves; logs "Resolved PowerShell 7 to {ExecutablePath} from the PATH entry {Directory}"
  4. Warning (lines 397–401): logs lookup order when nothing found

- `TryGitCandidate(string installRoot)` (lines 446–471):
  - Blank check (lines 448–451)
  - Combines `installRoot`, `"bin"`, `BashExecutableName` with `Path.Combine` (catches `ArgumentException`, logs "Rejected install root {InstallRoot} because it is not a valid path", returns null)
  - Checks `environment.FileExists(candidate)` (logs "Rejected install root {InstallRoot} because {Candidate} does not exist" on miss)
  - Calls `ToAbsolutePath(candidate)` (catches `ArgumentException`/`NotSupportedException`/`PathTooLongException`, returns null)

- `TryPowerShellCandidate(string installDirectory)` (lines 473–501):
  - Blank check (lines 475–478)
  - Combines `installDirectory`, `PwshExecutableName` with `Path.Combine` (catches `ArgumentException`, logs "Rejected install directory {InstallDirectory} because it is not a valid path", returns null)
  - Checks `environment.FileExists(candidate)` (logs "Rejected install directory {InstallDirectory} because {Candidate} does not exist" on miss)
  - Calls `ToAbsolutePath(candidate)` (catches `ArgumentException`/`NotSupportedException`/`PathTooLongException`, returns null)

- `IsUnderDirectory(path, directory)` (lines 515–543):
  - Returns `false` for blank `directory` (lines 517–520)
  - Calls `Path.GetFullPath(path)` and `Path.GetFullPath(directory)`, trims trailing separators, catches `ArgumentException`/`NotSupportedException`/`PathTooLongException`, returns `false` on exception (lines 522–534)
  - Compares normalized paths: equality (lines 536–539) or prefix + separator (lines 541–542)

**ProjectPathPolicy.IsUnder** (src/CLIHub.Core/Projects/ProjectPathPolicy.cs, lines 33–44):
- Calls `Normalize(path)` and `Normalize(root)` where `Normalize` is `Path.GetFullPath(path).TrimEnd(...)`
- Compares normalized paths: equality or prefix + separator
- **Throws** `ArgumentException`/`NotSupportedException`/`PathTooLongException` for blank or invalid paths (no guard)

### 3. Agent Probe Policy

**TTL formula duplication:**
- `AgentDetectionService.cs` (lines 77–84):
  ```csharp
  private TimeSpan Ttl
  {
      get
      {
          var minutes = this.preferencesStore.Load().AgentProbeTtlMinutes;
          return minutes is > 0 ? TimeSpan.FromMinutes(minutes.Value) : DefaultTtl;
      }
  }
  ```
  - `DefaultTtl` at line 14: `TimeSpan.FromMinutes(15)`

- `AgentVersionService.cs` (lines 124–131):
  ```csharp
  private TimeSpan Ttl
  {
      get
      {
          var minutes = this.preferencesStore.Load().AgentProbeTtlMinutes;
          return minutes is > 0 ? TimeSpan.FromMinutes(minutes.Value) : DefaultTtl;
      }
  }
  ```
  - `DefaultTtl` at line 22: `public static readonly TimeSpan.FromMinutes(15)`
  - `DefaultProbeTimeout` at line 27: `public static readonly TimeSpan.FromSeconds(10)`

- `AgentUpdateAvailabilityService.cs` (lines 195–202):
  ```csharp
  private TimeSpan Ttl
  {
      get
      {
          var minutes = this.preferencesStore.Load().AgentProbeTtlMinutes;
          return minutes is > 0 ? TimeSpan.FromMinutes(minutes.Value) : AgentVersionService.DefaultTtl;
      }
  }
  ```
  - References `AgentVersionService.DefaultTtl`

**Timeout usage:**
- `AgentReleaseClient.cs` (lines 163–170):
  ```csharp
  private TimeSpan ProbeTimeout
  {
      get
      {
          var seconds = this.preferencesStore.Load().AgentProbeTimeoutSeconds;
          return seconds is > 0 ? TimeSpan.FromSeconds(seconds.Value) : AgentVersionService.DefaultProbeTimeout;
      }
  }
  ```
  - References `AgentVersionService.DefaultProbeTimeout`

**Public API usage (SettingsViewModel.cs):**
- Line 345: `(int)AgentVersionService.DefaultTtl.TotalMinutes` (load fallback)
- Line 350: `(int)AgentVersionService.DefaultProbeTimeout.TotalSeconds` (load fallback)
- Line 361: `((int)AgentVersionService.DefaultTtl.TotalMinutes).ToString()` (watermark)
- Line 366: `((int)AgentVersionService.DefaultProbeTimeout.TotalSeconds).ToString()` (watermark)

**InFlight duplication:**
- `AgentVersionService.cs` (lines 36, 62–78):
  ```csharp
  private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> inFlight = new();
  
  var lazy = this.inFlight.GetOrAdd(
      plugin.Id,
      _ => new Lazy<Task<string?>>(
          () => this.ProbeAndCacheAsync(plugin),
          LazyThreadSafetyMode.ExecutionAndPublication));
  
  try
  {
      return await lazy.Value.WaitAsync(cancellationToken);
  }
  finally
  {
      if (lazy.IsValueCreated && lazy.Value.IsCompleted)
      {
          this.inFlight.TryRemove(plugin.Id, out _);
      }
  }
  ```

- `AgentUpdateAvailabilityService.cs` (lines 24, 58–74): identical structure, different value type (`Task<AgentUpdateAvailability>`)

### 4. Update-Source Policy

**Source resolution duplication:**
- `AgentUpdateAvailabilityService.IsSupportedBlock(plugin, updateCheck)` (lines 160–193):
  - npm (lines 162–173): checks `Source == "npm"` (case-insensitive), logs "Agent {PluginId} declares an npm update check without a package; ignoring it" when `Package` is blank, returns false
  - githubReleases (lines 175–186): checks `Source == "githubReleases"` (case-insensitive), logs "Agent {PluginId} declares a githubReleases update check without a repo; ignoring it" when `Repo` is blank, returns false
  - Unknown (lines 188–192): logs "Agent {PluginId} declares an unknown update check source {Source}; ignoring it", returns false

- `AgentReleaseClient.TryBuildRequest(updateCheck, out request)` (lines 92–134):
  - npm (lines 95–105): checks `Source == "npm"` (case-insensitive), logs "Update check source npm has no package for {Source} block" when `Package` is blank, returns false
  - githubReleases (lines 106–116): checks `Source == "githubReleases"` (case-insensitive), logs "Update check source githubReleases has no repo" when `Repo` is blank, returns false
  - Unknown (lines 117–123): logs "Ignoring update check with unknown source kind {Source}", returns false

- `AgentProcessMatcher.DeclaresNpmUpdateCheck(plugin)` (lines 143–150):
  - Pure predicate: returns `updateCheck is not null && Source == "npm" && !string.IsNullOrWhiteSpace(Package)`
  - No logging

**ParseLatestVersion field selection** (`AgentReleaseClient.cs`, line 141):
- `var field = string.Equals(source, "npm", StringComparison.OrdinalIgnoreCase) ? "version" : "tag_name";`

## Per-Area Contracts

### 1. ProjectScanningService

**Extracted components:**
- `ScanPass` record (private): `{ ProjectScanningOptions Options, IReadOnlyList<string> AllWatchedRoots, bool IsSingleRoot, string? TargetRoot }`
  - Full pass: `Options = options`, `AllWatchedRoots = options.WatchedDirectories`
  - Single-root pass: `Options = rootOptions` (where `rootOptions.WatchedDirectories = new[] { root }`), `AllWatchedRoots = options.WatchedDirectories` (the original full list), `TargetRoot = root`
- `RunPassAsync(ScanPass pass)`: unified pipeline
  - Before: publishes status based on `configured = pass.AllWatchedRoots.Count > 0`:
    - Single-root: `State = Scanning`, `IsScanning = true`, `Error = null` (does NOT set `IsWatching`)
    - Full: `State = configured ? Scanning : NotConfigured`, `IsWatching = configured`, `IsScanning = configured`, `Error = null`
  - Enumerates with `pass.Options`
  - After: posts reconcile block with coverage merge (clear for full: `if (!pass.IsSingleRoot) scannedDirectoryCounts.Clear();`, overwrite for single-root), publishes final state:
    - Single-root: `this.status with { State = this.status.IsWatching ? Idle : NotConfigured, IsScanning = false, LastScanUtc, DiscoveredCount, ScannedDirectoryCount, Error = null }` (does NOT set `IsWatching`)
    - Full: `this.status with { State = configured ? Idle : NotConfigured, IsWatching = configured, IsScanning = false, LastScanUtc, DiscoveredCount, ScannedDirectoryCount, Error = null }`
- `ComputePassRemovals(pass, discoveredPaths, scannedRoots)`: unified removal
  - If `pass.IsSingleRoot && scannedRoots.Count == 0`, return empty
  - Derives `otherRoots` from `pass.AllWatchedRoots` excluding `pass.TargetRoot` (for single-root pass)
  - For each discovered project:
    - If `pass.IsSingleRoot && !IsUnder(path, pass.TargetRoot)`, skip
    - If `pass.IsSingleRoot && otherRoots.Any(other => IsUnder(path, other))`, skip ("other roots still own it")
    - If !under any watched root OR (under a scanned root AND not discovered), remove
- `PublishNotConfiguredAndClearCoverage()`: shared reset helper
  - Posts: `scannedDirectoryCounts.Clear()`, then publishes `this.status with { State = NotConfigured, IsWatching = false, IsScanning = false, ScannedDirectoryCount = 0 }` (preserves `LastScanUtc`, `DiscoveredCount`, `Error`)

**Invariant preservation table:**

| Aspect | Full Pass | Single-Root Pass | Preserved How |
|--------|-----------|------------------|---------------|
| Status before | `State = configured ? Scanning : NotConfigured`, `IsWatching = configured` | `State = Scanning`, `IsWatching` preserved | `pass.IsSingleRoot` guard in `RunPassAsync` |
| Status after | `State = configured ? Idle : NotConfigured`, `IsWatching = configured` | `State = this.status.IsWatching ? Idle : NotConfigured` | `pass.IsSingleRoot` guard in `RunPassAsync` |
| Coverage merge | Clear then fill | Overwrite scanned entries | `if (!pass.IsSingleRoot) scannedDirectoryCounts.Clear();` |
| Removal candidates | All discovered projects | Only projects under `targetRoot` | `if (pass.IsSingleRoot && !IsUnder(path, pass.TargetRoot)) continue;` |
| "Other roots own it" guard | Not applicable | Active | `if (pass.IsSingleRoot && otherRoots.Any(...)) continue;` |
| Missing root behavior | Scans nothing, removes projects under no root | Scans nothing, removes nothing | `if (pass.IsSingleRoot && scannedRoots.Count == 0) return empty;` |
| Error log | "Project scan failed" | "Project scan failed for watched directory {Root}" | Error handlers in `ScanLoopAsync` vs `RescanRootAsync` unchanged |

### 2. WindowsRuntimeLocator

**Extracted components:**
- `TryExecutable(string container, IReadOnlyList<string> segments, string label)`: unified probe
  - Blank check for `container`: returns null silently (no log)
  - Combines `container` + `segments` with `Path.Combine` (catches `ArgumentException`, logs "Rejected {label} {Container} because it is not a valid path", returns null)
  - Checks `environment.FileExists(candidate)` (logs "Rejected {label} {Container} because {Candidate} does not exist" on miss)
  - Calls `ToAbsolutePath(candidate)`
  - The logged `{Container}` structured property renders the same text as the current "install root {InstallRoot}" and "install directory {InstallDirectory}" messages
  - Example: `TryExecutable(installRoot, ["bin", "bash.exe"], "install root")` for Git; `TryExecutable(installDirectory, ["pwsh.exe"], "install directory")` for PowerShell
- `LocateOnPath(displayName, executableName, excludedRoot, excludedRootLabel)`: unified PATH scan
  - Loops `environment.GetPathDirectories()`
  - If `excludedRoot` is non-null, filters with `IsUnderDirectoryGuarded(directory, excludedRoot)` (logs "Rejected PATH entry {Directory} because it is under the {ExcludedRootLabel} directory {ExcludedRoot}" using the provided `excludedRootLabel`)
  - Combines with `executableName` via `Path.Combine`, checks existence, resolves (catches `ArgumentException`, logs "Rejected PATH entry {Directory} because it is not a valid path"; logs "Rejected PATH candidate {Candidate} because the file does not exist" on miss)
  - Logs "Resolved {DisplayName} to {ExecutablePath} from the PATH entry {Directory}" on success
  - Returns the resolved path on success, null on failure
- `IsUnderDirectoryGuarded(path, directory)`: wrapper over `ProjectPathPolicy.IsUnder`
  - Returns `false` for blank `directory`
  - Tries `ProjectPathPolicy.IsUnder(path, directory)`, catches `ArgumentException`/`NotSupportedException`/`PathTooLongException`, returns `false` on exception

**Invariant preservation:**
- Git Bash: registry → well-known → PATH (Windows-directory exclusion) → warning
- PowerShell 7: registry → well-known → PATH (no exclusion) → warning
- Exact log messages preserved:
  - Git Bash PATH exclusion: "Rejected PATH entry {Directory} because it is under the Windows directory {WindowsDirectory}" (via `excludedRootLabel: "Windows"`)
  - PowerShell 7 PATH: no exclusion (via `excludedRoot: null`)
  - Git Bash PATH success: "Resolved Git Bash to {ExecutablePath} from the PATH entry {Directory}"
  - PowerShell 7 PATH success: "Resolved PowerShell 7 to {ExecutablePath} from the PATH entry {Directory}"
  - Git Bash registry/well-known: "Resolved Git Bash to {ExecutablePath} from the Git for Windows registry InstallPath" / "from the well-known install location {InstallRoot}"
  - PowerShell 7 registry/well-known: "Resolved PowerShell 7 to {ExecutablePath} from the PowerShell Core registry InstallPath" / "from the well-known install location"
  - Rejection messages: rendered text identical to current (structured property names may differ)
- `LocateGitBash()` calls `LocateOnPath("Git Bash", BashExecutableName, windowsDirectory, "Windows")`, then logs its existing runtime-specific warning if null
- `LocatePowerShell7()` calls `LocateOnPath("PowerShell 7", PwshExecutableName, null, null)`, then logs its existing runtime-specific warning if null
- `Locate(RuntimeKind)` returns null for cmd/powershell (unchanged)

### 3. Agent Probe Policy

**Extracted components:**
- `AgentProbePolicy` (internal, `src/CLIHub.Core/Agents/AgentProbePolicy.cs`):
  - `const int DefaultTtlMinutes = 15;`
  - `const int DefaultProbeTimeoutSeconds = 10;`
  - `static TimeSpan DefaultTtl { get; } = TimeSpan.FromMinutes(DefaultTtlMinutes);`
  - `static TimeSpan DefaultProbeTimeout { get; } = TimeSpan.FromSeconds(DefaultProbeTimeoutSeconds);`
  - `static TimeSpan ResolveTtl(int? agentProbeTtlMinutes)` → `minutes is > 0 ? TimeSpan.FromMinutes(minutes.Value) : DefaultTtl`
  - `static TimeSpan ResolveTimeout(int? agentProbeTimeoutSeconds)` → `seconds is > 0 ? TimeSpan.FromSeconds(seconds.Value) : DefaultProbeTimeout`
- `AgentVersionService.DefaultTtl` → public alias `AgentProbePolicy.DefaultTtl` (preserve for SettingsViewModel.cs:342, 361)
- `AgentVersionService.DefaultProbeTimeout` → public alias `AgentProbePolicy.DefaultProbeTimeout` (preserve for SettingsViewModel.cs:350, 366)
- `AgentDetectionService.DefaultTtl` → internal alias `AgentProbePolicy.DefaultTtl` (preserve for AgentDetectionServiceTests.cs:118)
- Three services' `Ttl` properties → call `AgentProbePolicy.ResolveTtl(this.preferencesStore.Load().AgentProbeTtlMinutes)`
- `AgentReleaseClient.ProbeTimeout` property → calls `AgentProbePolicy.ResolveTimeout(this.preferencesStore.Load().AgentProbeTimeoutSeconds)`

**InFlight helper decision:**
SKIP. Rationale: A generic `InFlight<TKey,TValue>` would need to abstract:
- Key type (string for both)
- Value type (Task<string?> vs Task<AgentUpdateAvailability>)
- Factory delegate (Func<TKey, Task<TValue>>)
- Lazy construction and removal logic

The result is ~40 lines to replace 2×15 lines of explicit code, and it introduces a new abstraction for a pattern used exactly twice. The explicit code is legible and testable as-is. Cost/benefit does not justify extraction. If a third identical cache appears, revisit.

### 4. Update-Source Policy

**Extracted components:**
- `UpdateCheckSource` (internal, `src/CLIHub.Core/Models/Plugins/UpdateCheckSource.cs`, namespace `CLIHub.Core.Models.Plugins`):
  - `const string Npm = "npm";`
  - `const string GitHubReleases = "githubReleases";`
  - `enum SourceKind { None, Npm, GitHubReleases }`
  - `enum Problem { None, MissingPackage, MissingRepo, UnknownSource }`
  - `static bool TryResolve(PluginUpdateCheck? updateCheck, out SourceKind kind, out Problem problem)`:
    - null → `(None, None)`, returns false
    - null/blank source → `(None, UnknownSource)`, returns false
    - npm + blank package → `(None, MissingPackage)`, returns false
    - npm + package → `(Npm, None)`, returns true
    - githubReleases + blank repo → `(None, MissingRepo)`, returns false
    - githubReleases + repo → `(GitHubReleases, None)`, returns true
    - Uses `OrdinalIgnoreCase` for source comparison and `IsNullOrWhiteSpace` for blank checks
    - Returns true only when `kind != None`

**Location rationale:**
Place in `src/CLIHub.Core/Models/Plugins/` (namespace `CLIHub.Core.Models.Plugins`). Wire-type adjacency: `PluginUpdateCheck` is the wire type, and this is its validation/resolution policy. Dependency direction: `CLIHub.Core.Agents` already references `CLIHub.Core.Models`, so `Agents` can use `Models.Plugins.UpdateCheckSource` without creating a cycle. `CLIHub.Core.Infrastructure.Processes.AgentProcessMatcher` also references `Models`, so it can use the constant/helper as well.

**Invariant preservation:**
- `IsSupportedBlock` and `TryBuildRequest` map the same `Problem` to their own existing messages (each caller logs its own text)
- `DeclaresNpmUpdateCheck` → `UpdateCheckSource.TryResolve(plugin.UpdateCheck, out var kind, out _) && kind == SourceKind.Npm` (null-safe, pure predicate, no logging)
- `ParseLatestVersion` → unchanged (not modified as part of this change; it already re-derives the field selection after a successful `TryBuildRequest`)

## Alternatives Considered and Rejected

### Alternative 1: Generic ScanPass Strategy Pattern
**Considered:** Abstract `IScanStrategy` with `GetOptions()`, `MergeCoverage()`, `ComputeRemovals()`, `GetFinalState()`.
**Rejected:** Over-abstraction for two passes. The pass record + inline guards is simpler, testable, and reviewable. A third pass type (e.g., incremental watcher-driven pass) would reopen this.

### Alternative 2: Shared Removal Predicate
**Considered:** Extract `ShouldRemove(project, pass)` with all guards inline.
**Rejected:** The two removal algorithms have different iteration scopes (all projects vs projects under target root) and different guard structure (no "other roots" guard for full pass). Unifying them into one predicate obscures the pass-specific removal logic. The unified `ComputePassRemovals` with explicit `if (pass.IsSingleRoot)` guards is clearer.

### Alternative 3: PathPolicy Extension in WindowsRuntimeLocator
**Considered:** Add `ProjectPathPolicy.TryIsUnder(path, root, out result)` that returns false on exception.
**Rejected:** `ProjectPathPolicy` is the canonical path-comparison contract for projects and scanning, and its `IsUnder` signature is `IsUnder(string path, string root) → bool` with `Normalize` throwing. Changing it to a Try pattern or adding a non-throwing overload would diverge from its current usage (where callers wrap the exception or pre-validate paths). The Windows runtime locator has a different concern (PATH entries can be invalid, and that's not an error) and should guard locally. A wrapper in `WindowsRuntimeLocator` is the right boundary.

### Alternative 4: UpdateCheckSource in CLIHub.Core.Agents
**Considered:** Place `UpdateCheckSource` in `CLIHub.Core.Agents` since it is used primarily by agent services.
**Rejected:** `AgentProcessMatcher` lives in `CLIHub.Core.Infrastructure.Processes`, and `Agents` depends on `Infrastructure.Processes` (via `IProcessOrchestrator`). If `UpdateCheckSource` is in `Agents`, then `AgentProcessMatcher` would need to reference `Agents`, creating a confusing namespace-level cycle (`Infrastructure.Processes` ← `Agents` → `Infrastructure.Processes`). Placing it in `Models.Plugins` keeps it adjacent to the wire type and avoids the cycle.

### Alternative 5: InFlight<TKey, TValue> Helper
**Considered:** Extract `InFlight<TKey, TValue>` for the duplicated Lazy<Task<T>> cache blocks.
**Rejected:** (See "InFlight helper decision" above.) Cost/benefit does not justify extraction for two uses. Revisit when a third identical cache appears.

## Open Questions

None. All decisions are grounded in verified source code and existing tests.

## Test Strategy

**Existing tests (unchanged, must pass):**
- `tests/CLIHub.Core.Tests/Scanning/ProjectScanningServiceTests.cs` (487 lines): pins full-pass and single-root reconciliation, coverage merge, removal rules, status transitions, watcher/debounce behavior.
- `tests/CLIHub.Core.Windows.Tests/WindowsRuntimeLocatorTests.cs` (253 lines): pins registry/well-known/PATH lookup order, Windows-directory exclusion for Git, no exclusion for PowerShell, ToAbsolutePath error handling.
- `tests/CLIHub.Core.Tests/Agents/`: pins TTL/timeout resolution, cache invalidation, in-flight deduplication, probe degradation.

**New tests (focused, assert existing behavior through public surface):**
1. `ProjectScanningServiceTests` (scenario-level tests through `Start`/`Reload`/`Rescan` with fake environment):
   - `Reload_FullScan_ClearsCoverageBeforeFilling` (assert full scan clears `scannedDirectoryCounts` by checking `service.Status.ScannedDirectoryCount`)
   - `Rescan_SingleRoot_OverwritesOnlyScannedRoot` (assert single-root rescan preserves other roots' coverage by checking `service.Status.ScannedDirectoryCount`)
   - `Rescan_ProjectAlsoUnderAnotherWatchedRoot_KeepsProject` (assert "other roots still own it" guard via `ReconcileDiscoveredProjects` calls)
   - `Rescan_MissingRoot_RemovesNothing` (assert `scannedRoots.Count == 0` guard via `ReconcileDiscoveredProjects` calls)
2. `WindowsRuntimeLocatorTests` (scenario-level tests through `Locate(RuntimeKind)` with fake environment and `NullLogger`):
   - `Locate_GitBash_BlankInstallRoot_ReturnsNull` (assert blank guard without checking logs, since tests use `NullLogger`)
   - `Locate_GitBash_InvalidPathEntry_ReturnsNullOrResolvesFromOtherSource` (assert invalid PATH entry handling via resolved path or null)
   - `Locate_PowerShell7_BlankInstallDirectory_ReturnsNull` (assert blank guard for PowerShell)
3. `AgentProbePolicyTests` (new file, tests the internal static members directly):
   - `ResolveTtl_WithPositivePreference_ReturnsPreference`
   - `ResolveTtl_WithZeroOrNegative_ReturnsDefault`
   - `ResolveTtl_WithNull_ReturnsDefault`
   - `ResolveTimeout_WithPositivePreference_ReturnsPreference`
   - `ResolveTimeout_WithZeroOrNegative_ReturnsDefault`
   - `ResolveTimeout_WithNull_ReturnsDefault`
   - `DefaultTtl_Is15Minutes`
   - `DefaultProbeTimeout_Is10Seconds`
4. `UpdateCheckSourceTests` (new file, tests the internal static members directly):
   - `TryResolve_Null_ReturnsFalseWithNone`
   - `TryResolve_NpmWithPackage_ReturnsTrueWithNpm`
   - `TryResolve_NpmWithBlankPackage_ReturnsFalseWithMissingPackage`
   - `TryResolve_GitHubReleasesWithRepo_ReturnsTrueWithGitHubReleases`
   - `TryResolve_GitHubReleasesWithBlankRepo_ReturnsFalseWithMissingRepo`
   - `TryResolve_UnknownSource_ReturnsFalseWithUnknownSource`
   - `TryResolve_BlankSource_ReturnsFalseWithUnknownSource`

**Baseline:** 1,282 tests passed (667 Core + 60 Core.Windows + 555 app) and 29/29 specs before the change. With this change present, `openspec validate --all` reports 30 items (29 specs + this change). No regressions allowed; new tests only assert existing behavior.

## Split Contingency

If a stage fails to leave the tree green after multiple attempts, the implementer may:
1. Report the failure with logs and error output.
2. Revert the stage's changes (git reset/revert to the last green commit).
3. Document the blocker in the change directory (e.g., `BLOCKER.md` with reproduction steps).
4. Proceed with the remaining stages if they are independent.
5. Escalate the blocker to the orchestrator/user for resolution.

The change is designed to stage independently (agent policy → update source → scanning → runtime locator), so a blocker in one area does not block the others.
