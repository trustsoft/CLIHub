# Proposal: Scanning and Runtime Probe Unification

## Why

Portfolio item 5 from the 2026-10-30 refactoring analysis (backlog.md) identified duplicated pipeline and policy across four areas:

1. **ProjectScanningService** (610 lines) implements the full-pass and single-root scan pipelines twice (lines 381–430 vs 458–501), with two removal algorithms (503–590) and two "clear coverage + publish NotConfigured" blocks (158–183 vs 231–247).
2. **WindowsRuntimeLocator** (544 lines) repeats the PATH scan (lines 295–329 and 369–395) and the candidate probe (446–471 and 473–501) per runtime, plus reimplements `ProjectPathPolicy.IsUnder` at lines 515–543.
3. **Agent probe services** repeat the TTL formula across three classes (AgentDetectionService:81–82, AgentVersionService:128–129, AgentUpdateAvailabilityService:199–200) and expose public defaults that SettingsViewModel depends on (lines 342–350, 361, 366).
4. **Update-source policy** is re-derived in three places: `AgentUpdateAvailabilityService.IsSupportedBlock` (160), `AgentReleaseClient.TryBuildRequest` (92), and `AgentProcessMatcher.DeclaresNpmUpdateCheck` (143).

Cost: duplication complicates changes to scan coverage, runtime lookup order, probe parameters, or update-check rules. Adding a third runtime or a third update source requires repeating the same pipeline, and changing the TTL semantics or removal rules means editing multiple copies.

This change consolidates the duplication into shared helpers and policies while preserving every existing behavior. It is behavior-preserving and does not touch OpenSpec specs (thus `skip_specs: true`).

## What

Four independent unifications (each isolated, reviewable, and testable):

### 1. ProjectScanningService: one scan-pass shape

Extract ONE pass runner (e.g., a private `ScanPass` record with `{ ProjectScanningOptions Options, IReadOnlyList<string> AllWatchedRoots, bool IsSingleRoot, string? TargetRoot }` + `RunPassAsync(ScanPass pass)`) from the duplicated full-pass (RunScanAsync:381–430) and single-root (RunRootScanAsync:458–501) pipelines, with thin entry points that compose the pass. Collapse the two removal algorithms (ComputeRemovals:554–590 and ComputeRootRemovals:503–552) into ONE removal method that keeps the single-root "other roots still own it" guard (lines 540–543). Extract ONE reset-to-NotConfigured helper from the duplicated Stop (158–183) and Configure no-roots branch (231–247).

Preserved invariants:
- UI-thread marshalling: `Post(Action)` and `Publish(ProjectScanStatus)` semantics remain unchanged; `Status`/`StatusChanged` stay unchanged; `Publish` posts and assigns as today.
- Coverage replace-vs-merge: a full pass clears `scannedDirectoryCounts` then fills it; a single-root pass only overwrites the entries it scanned (other roots keep their last number); `Reload` prunes removed roots; `Stop` clears.
- Removal rules:
  - Full pass: a discovered project is removed when it is under no watched root, OR when it is under a scanned root and was not discovered this pass. Manual projects are never removed.
  - Single-root rescan: only projects under the target root are candidates; a project that is also under another watched root is kept ("other roots still own it"); when `scannedRoots.Count == 0` (missing/unreadable root) nothing is removed.
  - Normalization/comparison uses `ProjectPathPolicy.Normalize` / `IsUnder` / `AreEqual` as today.
- Status fields and text: `State`, `IsWatching`, `IsScanning`, `LastScanUtc`, `DiscoveredCount`, `ScannedDirectoryCount`, `Error`; the discovered count is always the number of registered discovered-provenance projects (never this pass's path count). The full pass sets `IsWatching = configured`; the root pass preserves `this.status.IsWatching`. The full pass sets `State = configured ? Scanning : NotConfigured` before enumerating and `State = configured ? Idle : NotConfigured` after; the root pass sets `Scanning` then `this.status.IsWatching ? Idle : NotConfigured`.
- `scanGate`, `scanPending`, debounce/watcher behavior, `ObjectDisposedException` handling, and the two distinct error log messages ("Project scan failed" vs "Project scan failed for watched directory {Root}") stay unchanged.

### 2. WindowsRuntimeLocator: one executable probe

Extract ONE `TryExecutable(string container, IReadOnlyList<string> segments, string label)` that covers `bin\bash.exe` and `pwsh.exe` probes (blank check for `container` returns null silently; `Path.Combine` ArgumentException is caught and logged; existence check logs on miss), and ONE `LocateOnPath(displayName, executableName, excludedRoot, excludedRootLabel)` helper that uses the provided `excludedRootLabel` for log messages. Replace `IsUnderDirectory` (lines 515–543) with a guarded wrapper over `ProjectPathPolicy.IsUnder` that preserves the blank-directory → false and invalid-path → false contract (so a blank/invalid PATH entry never throws).

CRITICAL guard: `ProjectPathPolicy.IsUnder` calls `Normalize` → `Path.GetFullPath`, which THROWS on an empty/invalid path. Current `IsUnderDirectory` returns `false` for a blank directory and catches `ArgumentException`/`NotSupportedException`/`PathTooLongException`. The wrapper must preserve that contract and must not let a PATH entry throw.

Preserved invariants:
- Lookup order: Git registry InstallPath views → Git well-known roots → PATH (with Windows-directory exclusion). PowerShell: `PowerShellCore` registry InstallPath → well-known `ProgramFiles\PowerShell\7\pwsh.exe` → PATH (NO Windows-directory exclusion).
- The throwing-registry degradation: `ReadGitRegistryInstallPaths`/`ReadPowerShellRegistryInstallPath` catch `IOException`/`SecurityException`/`UnauthorizedAccessException` and log debug, never throw.
- The exact logged messages (Information success messages per source, Debug rejection messages, and the two final Warning lookup-order strings). If helpers abstract logging, they must take enough context to emit byte-identical messages.
- `ToAbsolutePath` behavior and `Path.Combine` `ArgumentException` handling.
- `Locate(RuntimeKind)` returns null for cmd/powershell.

### 3. Agent probe policy

Add one internal `AgentProbePolicy` class with the shared defaults and TTL/timeout resolution logic. Route all five call sites through it:
- TTL formula: `AgentDetectionService.cs:81–82`, `AgentVersionService.cs:128–129`, `AgentUpdateAvailabilityService.cs:199–200`
- Defaults: 15 minutes (`AgentVersionService.DefaultTtl`, `AgentDetectionService.DefaultTtl`) and 10 seconds (`AgentVersionService.DefaultProbeTimeout`)
- External reference: `AgentReleaseClient.cs:168` uses `AgentVersionService.DefaultProbeTimeout`
- UI reference: `SettingsViewModel.cs:342–350, 361, 366` reads both defaults

Keep `AgentVersionService.DefaultTtl` and `DefaultProbeTimeout` as public aliases (e.g., assign from `AgentProbePolicy` or expose as pass-through properties) so SettingsViewModel remains unchanged. Keep `AgentDetectionService.DefaultTtl` as internal alias for AgentDetectionServiceTests.cs:118.

Optionally: add ONE concrete internal `InFlight<TKey,TValue>` helper for the identical `ConcurrentDictionary<string, Lazy<Task<T>>>` blocks in `AgentVersionService.cs:36,62–78` and `AgentUpdateAvailabilityService.cs:24,58–74`. It must preserve: `LazyThreadSafetyMode.ExecutionAndPublication`, `WaitAsync(cancellationToken)` (per-caller cancellation), and the `finally` removal only when `lazy.IsValueCreated && lazy.Value.IsCompleted` (a cancelled caller must NOT remove a still-running shared probe). It must NOT become a generic cache, must NOT introduce `ICacheService`, and must NOT reopen ADR-0013. If it would need more than ~30 lines or grows options, skip it.

### 4. Update-source policy

Add ONE internal `UpdateCheckSource` class with the two source constants (`"npm"` and `"githubReleases"`) and `TryResolve(PluginUpdateCheck? updateCheck, out kind, out problem)` (kind = npm/githubReleases/none; problem = missing package / missing repo / unknown source; returns true only when `kind != None`, handles null updateCheck as `(None, None)` with false result).

Location: co-locate with the wire type under `src/CLIHub.Core/Models/Plugins/` (namespace `CLIHub.Core.Models.Plugins`) OR in `CLIHub.Core.Agents`. Rationale: avoid creating a confusing namespace-level cycle (`AgentProcessMatcher` lives in `CLIHub.Core.Infrastructure.Processes`, and `Agents` already depends on `Infrastructure.Processes` via `IProcessOrchestrator`). There is no assembly-level constraint (single Core assembly; architecture tests only check project references), but keep the dependency direction defensible.

Preserved invariants:
- `PluginUpdateCheck.Source` stays the raw wire string.
- The two callers log DIFFERENT messages for the same problem, so `TryResolve` must return a neutral problem and each caller must map it to its own existing message:
  - `IsSupportedBlock` npm missing package: "Agent {PluginId} declares an npm update check without a package; ignoring it"
  - `TryBuildRequest` npm missing package: "Update check source npm has no package for {Source} block"
  - `IsSupportedBlock` github missing repo: "Agent {PluginId} declares a githubReleases update check without a repo; ignoring it"
  - `TryBuildRequest` github missing repo: "Update check source githubReleases has no repo"
  - `IsSupportedBlock` unknown: "Agent {PluginId} declares an unknown update check source {Source}; ignoring it"
  - `TryBuildRequest` unknown: "Ignoring update check with unknown source kind {Source}"
- `DeclaresNpmUpdateCheck` (AgentProcessMatcher:143) stays a pure null-safe predicate (npm + non-empty package, no logging).
- `ParseLatestVersion`'s `source == "npm" ? "version" : "tag_name"` branch (AgentReleaseClient:141) is unchanged (not modified as part of this change; it already re-derives the field selection after a successful `TryBuildRequest`).

## Non-Goals (Explicitly Out of Scope)

- NO durable behavior change; `skip_specs: true`.
- NO reopening ADR-0013 (`IPersistenceService<T>`, `ICacheService`).
- NO merging the two version regexes.
- NO flattening `ProcessOrchestrator`'s domain/infrastructure split.
- NO changing update warnings, runtime lookup order/exclusions, scan coverage semantics, removal rules, status text, or public APIs the Settings UI/tests depend on.
- NO fixing unrelated backlog items (Settings, launch-window, dead-path cleanup, test helpers, flaky tests).
- NO touching `.opencode/`, `docs/backlog.md`, or any source file outside the change scope.

## Impact

**Low risk, high value:**
- 610-line ProjectScanningService loses ~80 lines of duplication.
- 544-line WindowsRuntimeLocator loses ~60 lines of duplication.
- Agent probe policy centralizes TTL/timeout formula in one place; future changes (e.g., adaptive TTL, per-agent timeout) touch one class.
- Update-source policy centralizes source validation; adding a third source (e.g., PyPI) touches one class.
- No user-visible behavior change.
- Existing test suites pin the contract: 1,282 tests (667 Core + 60 Core.Windows + 555 app) and 30/30 (29 specs + this change) as the baseline.

**Dependencies:**
- None. This change is self-contained and does not depend on other changes or infrastructure.

**Risk mitigation:**
- Staged implementation (agent policy + update source → scanning → runtime locator) ensures each stage is independently green and reviewable.
- Existing tests remain unchanged and must pass after each stage.
- User-side smoke test: launch an agent with Git Bash and PowerShell 7, run Detect and Check updates, confirm runtime/agent paths resolve as before.
