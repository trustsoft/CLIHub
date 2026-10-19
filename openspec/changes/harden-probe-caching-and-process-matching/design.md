# Design: harden probe caching and process matching

## 1. Scope

| Finding | Severity | Fix stage | Outcome |
|---|---|---|---|
| Probing one agent invalidates every agent's detection and version caches | High | Stage 1 | Scoped invalidation; unrelated agents stay cache-served |
| Runtime process matching is unreachable while comments and tests claim support | High | Stage 2 | Dead capability removed; contract documented as executable-name matching |

The two stages touch disjoint production code and can be reviewed independently; the only shared file is
`tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs`, where Stage 2 makes a mechanical
`AgentProcessInstance` constructor update. They stay in one change because both correct the same class
of defect — agent state that is either recomputed too broadly or claimed more broadly than it is
implemented — and they share the same test projects and verification gates.

## 2. Verified current state

All line numbers are from the clean tree at commit `0b8f799`.

| Area | Verified evidence |
|---|---|
| Global probe invalidation | `src/CLIHub.Core/Agents/AgentManagementService.cs:46-47` calls `detection.Invalidate()` and `version.Invalidate()` before probing one plugin |
| Detection cache shape | `src/CLIHub.Core/Agents/AgentDetectionService.cs:18` (`ConcurrentDictionary<string, CacheEntry>`), `:30-35` (keys `sys:<id>` and `proj:<id>:<path>`), `:38` (`cache.Clear()`), `:53-60` (TTL from preferences) |
| Version cache shape | `src/CLIHub.Core/Agents/AgentVersionService.cs:34-35` (`cache` keyed by plugin id, plus `inFlight` of `Lazy<Task<string?>>`), `:55-64` (TTL check, single-flight), `:107` (`cache.Clear()` only) |
| Version probe cancellation | `AgentVersionService.cs:90` passes `CancellationToken.None` to `CaptureOutputAsync`; the caller's token only bounds `WaitAsync` at `:68` |
| Detect-all loop | `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:269-293`, `ProbeAsync` call at `:276` |
| Launch-window cache use | `src/CLIHub/ViewModels/Agents/AgentPaneController.cs:159-188` populates versions from the cache; `:123-128` intentionally performs the global clear for manual refresh/plugin reload |
| Legacy refresh caller | `src/CLIHub/Views/MainWindow.xaml.cs:219-225` performs the same global clear from the unregistered legacy window |
| Runtime matching branch | `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs:45-49` calls `IsRuntimeProcess`; `:54-102` contains the Node.js/Python branch; `:68-71` returns false for an empty command line |
| Inspector command line | `src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs:76-77` assigns `string.Empty`; `:103-108` defines an uncalled `GetCommandLine` that returns null |
| Process model | `src/CLIHub.Core/Models/Agents/AgentProcessInstance.cs:8-14` carries `CommandLine`; no production reader remains after the matcher branch is removed |
| Tests that hide the gap | `tests/CLIHub.Core.Tests/Infrastructure/Processes/AgentProcessMatcherTests.cs:45-91` and `tests/CLIHub.Core.Tests/Agents/AgentProcessMonitorTests.cs:222-240` construct command lines by hand, so they never exercise the production inspector |
| Monitor call sites | `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:60-87` enumerates processes; `:94-101` filters per plugin |
| Update blocking | `src/CLIHub/ViewModels/Shared/WindowActionCoordinator.cs:145-161` (availability) and `:163-182` (execution-time double check); `src/CLIHub.Core/Agents/AgentManagementService.cs:69-76` (Settings path) |
| False documentation | `docs/architecture/processes.md:18-20` claims Node.js/Python runtime matching; `:27-30` notes that the inspector does not retrieve command lines |
| Governing scenarios | `openspec/specs/agent-ui/spec.md:233-241`; `openspec/specs/preferences-ui/spec.md:534-542`; cache wording at `openspec/specs/agent-detection/spec.md:49-95` and `openspec/specs/agent-version/spec.md:37-52,109-127` |
| Scoped-invalidation precedent | `openspec/specs/logo-cache/spec.md:89-113`; `src/CLIHub.Core/Infrastructure/Persistence/ILogoCacheService.cs:26-35` (`InvalidateAll(string? keyPrefix)`) |

History matters for Stage 2: the archived `2026-10-10-agent-process-monitoring` design retrieved the
command line with one WMI query **per process** (`SPEC.md:136-155`). The implementation replaced that
with `string.Empty` because WMI "can hang or be slow" — the runtime branch is the residue of that
removed capability, and its tests kept passing because they inject command lines directly.

## 3. Finding 1 — scoped probe-cache invalidation

### 3.1 Cache shapes

| Service | Key | Value | TTL | Eviction |
|---|---|---|---|---|
| `AgentDetectionService` | `sys:<pluginId>` for host detection; `proj:<pluginId>:<projectPath>` for project availability | boolean + timestamp | `AgentProbeTtlMinutes` or `DefaultTtl` (15 min) | Lazy overwrite after TTL; `Clear()` |
| `AgentVersionService` | `<pluginId>` in `cache`; `<pluginId>` in `inFlight` | version string or null + timestamp; `Lazy<Task<string?>>` | `AgentProbeTtlMinutes` or `DefaultTtl` (15 min) | Lazy overwrite after TTL; `Clear()` for the cache only; in-flight entries remove themselves on completion |

Both services are singletons in composition (`src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:108-112`),
so the global clear affects every card and every launch-window refresh.

### 3.2 Why the current behavior is costly

- `ProbeAsync` for agent A clears the detection entries for agents B..N and the version entries for
  B..N. The next `AgentPaneController.Refresh` (window shown/activated per
  `agent-availability-display:138-155`) recomputes `File.Exists`/`Directory.Exists` for every system
  path of every agent (and every project indicator when a project is selected), and
  `PopulateVersionsAsync` starts one version process per agent. That is the opposite of the spec's
  "version population stays cache-served" scenario.
- `DetectAsync` is worse: for each card it clears everything, then repopulates only that card's
  system detection and version entries. Card 2's clear discards card 1's fresh results. After N cards
  only the last agent's system detection and version are cached, so the immediate refresh repeats
  N−1 version probes and re-checks the earlier agents' detection.
- The manual "Test" action is the common trigger; the user sees a slow first launch-window open after
  testing one agent, with process/CPU churn unrelated to that agent.

### 3.3 Scoped API

Add one operation to each interface, keeping the parameterless full clear:

```csharp
public interface IAgentDetectionService
{
    // existing members ...

    /// <summary>
    ///   Clears the cached system and project detection results of one agent so its next
    ///   detection re-checks the file system. Other agents' cached results are preserved.
    /// </summary>
    /// <param name="plugin"> The agent whose results are cleared. </param>
    void InvalidateFor(Plugin plugin);
}

public interface IAgentVersionService
{
    // existing members ...

    /// <summary>
    ///   Clears one agent's cached version so its next lookup runs the command again.
    ///   Other agents' cached versions and already-running probes are unaffected.
    /// </summary>
    /// <param name="plugin"> The agent whose cached version is cleared. </param>
    void InvalidateFor(Plugin plugin);
}
```

Detection implementation (refactor the three key literals into helpers so lookup and invalidation
cannot drift):

```csharp
public void InvalidateFor(Plugin plugin)
{
    ArgumentNullException.ThrowIfNull(plugin);

    this.cache.TryRemove(BuildSystemKey(plugin.Id), out _);

    var projectPrefix = BuildProjectKeyPrefix(plugin.Id);
    foreach (var key in this.cache.Keys)
    {
        if (key.StartsWith(projectPrefix, StringComparison.Ordinal))
        {
            this.cache.TryRemove(key, out _);
        }
    }
}

private static string BuildSystemKey(string pluginId) => $"sys:{pluginId}";
private static string BuildProjectKeyPrefix(string pluginId) => $"proj:{pluginId}:";
private static string BuildProjectKey(string pluginId, string projectPath) =>
    $"{BuildProjectKeyPrefix(pluginId)}{projectPath}";
```

Version implementation:

```csharp
public void InvalidateFor(Plugin plugin)
{
    ArgumentNullException.ThrowIfNull(plugin);
    this.cache.TryRemove(plugin.Id, out _);
}
```

Management service:

```csharp
this.detection.InvalidateFor(plugin);
this.version.InvalidateFor(plugin);
```

`DetectAsync` stays as it is. Each `ProbeAsync` call now invalidates exactly its own agent, so the loop
invalidates each affected agent once and preserves the results produced by earlier iterations.

### 3.4 Concurrency and edge cases

- **Unknown plugin:** both operations are no-ops when no entry exists.
- **Plugin id casing:** exact `Ordinal` matching is correct because cache keys are built from the same
  `plugin.Id` value, and the catalog's duplicate-id check is case-sensitive
  (`src/CLIHub.Core/Plugins/PluginCatalog.cs:141`).
- **Project entries:** the `proj:<id>:` prefix removes every project-path variant for that agent. Path
  separators and casing are irrelevant because only the prefix is compared, and the trailing colon keeps
  agent `a` from matching entries of agent `ab`.
- **In-flight version probe:** `InvalidateFor` removes only a completed cache entry, not `inFlight`.
  This preserves the single-flight coalescing required by `agent-version:85-95`. A probe that started
  before the invalidation may still publish its result; that result is at most one probe old and is the
  same value a concurrent caller already shares. Removing `inFlight` would start a duplicate process
  and create a last-writer race, so it is deliberately not done.
- **Caller cancellation:** `ProbeAndCacheAsync` passes `CancellationToken.None` to the process runner.
  This is pre-existing and outside this change; a cancelled UI action can still complete and cache its
  probe. Recorded as an open question for a later pass.
- **Full clear retained:** `AgentPaneController.InvalidateCaches` and the legacy refresh still need a
  global clear; removing `Invalidate()` would be a behavior regression.

### 3.5 Alternatives considered

| Alternative | Rejected because |
|---|---|
| Batch `InvalidateFor(IEnumerable<Plugin>)` plus `ProbeManyAsync` | The built-in catalog has six agents; per-agent scoped removal is O(cache size) with no I/O, and a batch API adds new surface and a second probe workflow for no measurable gain. It can be added later without breaking this API. |
| `Invalidate(Func<string, bool> predicate)` | Leaks the key scheme to callers and invites accidental broad clears; two concrete services do not justify a general predicate surface. |
| A shared cache abstraction for both services | Violates the repo preference for existing patterns over new abstractions; the caches differ (in-flight coalescing, boolean vs string values, TTL source). |
| `InvalidateFor(string pluginId)` | The sibling operations take `Plugin`, and the only caller already has a `Plugin`. Passing the object keeps the null guard and the call sites readable. |
| Increase the TTL or only clear on manual refresh | Does not fix correctness: a manual probe must be visible immediately, and the clearing agent must not disturb others. |

### 3.6 Tests for Stage 1

| Test | Assertion |
|---|---|
| `InvalidateFor_Plugin_RemovesSystemAndProjectEntriesForThatAgentOnly` | Agent A's system and project markers are re-checked; agent B's results stay cached (assert by changing the file system after priming) |
| `InvalidateFor_Plugin_RemovesEntriesForEveryProject` | Both `proj:A:p1` and `proj:A:p2` re-check |
| `InvalidateFor_UnknownPlugin_PreservesExistingEntries` | No other entry is disturbed |
| `InvalidateFor_Plugin_RunsOnlyThatAgentsVersionCommandAgain` | Prime A and B, change the fake output, invalidate A: A returns the new value, B keeps the old cached value, run count increases by one |
| `ProbeAsync_InvalidatesOnlyTheProbedAgent` | Moq verifies `InvalidateFor(plugin)` once per service and parameterless `Invalidate()` never |
| `ProbeAsync_LeavesOtherAgentsCacheServed` | Real detection + version services; prime A and B; remove A's marker and change output; probe A; B's detection and version lookups add no file checks or processes |
| `DetectAll_InvalidatesEachAgentOnce` | Loop `ProbeAsync` over A and B with mocked services: each agent's `InvalidateFor` runs once and the parameterless `Invalidate()` never runs |
| `DetectAll_LeavesEveryProbedAgentCached` | Loop `ProbeAsync` over A and B, then look up both agents' system detection and version: no additional version runs, and both show their detect-pass values (regression for "only the last card is cached") |

Existing tests keep passing: `Invalidate_ForcesReCheck`, `Invalidate_ClearsCachedVersions`, and the
`AgentPaneControllerTests` strict mocks still call only `Invalidate()`.

## 4. Finding 2 — process-matcher honesty

### 4.1 Evidence

1. `WindowsAgentProcessInspector` constructs every `AgentProcessInstance` with `string.Empty`
   (`:76-83`) and never calls its own `GetCommandLine` (`:103-108`). The helper returns null and its
   TODO says WMI was disabled because it can hang.
2. `AgentProcessMatcher.IsRuntimeProcess` can never return true for a process the production inspector
   produces: non-runtime executable names fail its runtime check (`:59-65`), and `node`/`python`
   processes fail the empty-command-line guard (`:68-71`). The markers below the guard (`:73-99`) are
   unreachable.
3. The matcher and monitor tests fabricate command lines (`AgentProcessMatcherTests.cs:45-91`,
   `AgentProcessMonitorTests.cs:222-240`), so they assert a capability the production path cannot
   deliver.
4. `docs/architecture/processes.md:18-20` states that runtime matching is supported; `:27-30` states
   that the command-line field is empty in production. The document contradicts itself.
5. The archived original design (`openspec/changes/archive/2026-10-10-agent-process-monitoring/SPEC.md:136-155`)
   used per-process WMI and was deliberately replaced by the empty value.
6. The npm-distributed seed agents (the `agent-update-availability` proposal maps opencode, cline,
   pi, and openclaude to npm packages) are commonly launched through a Windows `.cmd` shim that starts
   a shared `node.exe`. That is exactly the shape the runtime branch was meant to cover; agents with a
   native launcher still match directly.

### 4.2 Option A — amputate (recommended)

Remove the capability that does not exist:

- Delete `AgentProcessMatcher.IsRuntimeProcess` and its call; document `IsMatch` and the class as
  matching on the launch executable's file name only (case-insensitive), guarded by the
  launch-command check.
- Delete `WindowsAgentProcessInspector.GetCommandLine` and the `commandLine` local; the inspector
  returns only the process id, executable path, and start time.
- Remove `CommandLine` from `AgentProcessInstance` and update the port XML documentation to state
  which fields an inspector must supply. Keeping a permanently empty member would preserve a smaller
  version of the same false contract and leave a trap for future readers. This is not the finding-11
  dead-member sweep: the field is the carrier of the removed capability.
- Update `docs/architecture/processes.md` to describe executable-name matching and to state the
  known limitation for runtime-hosted agents.
- Replace the runtime tests with one regression test that a `node.exe` process is **not** matched, so
  the limitation is visible and intentional.

Consequences: direct `.exe` installs (including agents that ship a native launcher) keep blocking
updates correctly. Agents reachable only through a `.cmd`/`.ps1` shim and a shared `node.exe` do not
block updates. The code stops pretending otherwise.

### 4.3 Option B — implement command-line retrieval

Two implementation routes were evaluated:

| Route | Description | Risks |
|---|---|---|
| Bulk WMI | One `SELECT ProcessId, CommandLine FROM Win32_Process` per snapshot via `System.Management`, mapped by PID | Synchronous WMI has no native timeout or cancellation; the first query can take seconds; `Win32_Process.CommandLine` is often unreadable for other users' or elevated processes; Winmgmt can be disabled by policy; EDR may flag or block the query; a new package and a new failure mode on every refresh |
| P/Invoke PEB read | `NtQueryInformationProcess` + `ReadProcessMemory` to read `RTL_USER_PROCESS_PARAMETERS.CommandLine` | Architecture- and session-specific structure offsets; unsafe code; breaks for protected/elevated processes; fragile across Windows versions; harder to test |

Neither is cheap:

- `GetRunningProcesses()` is synchronous and is called from the UI thread once per card in
  `AgentManagementViewModel.Load` (`:183-185`) and again from `WindowActionCoordinator` for the
  availability check (`:155`) and the execution-time check (`:173`). A WMI call per invocation would
  multiply a multi-second query per page load, not per refresh.
- Making it safe requires an asynchronous process-snapshot service with caching, invalidation, and a
  background timeout — an architecture change larger than this finding.
- Tests cannot reliably cover protected processes or WMI failures on CI; the Windows test project
  would need new gating and would still be environment-dependent.

This option is a separate proposal if product requirements demand blocking for runtime-hosted agents.
It would keep the current matcher branch (or replace it with snapshot-driven matching) and add
command-line retrieval behind `IAgentProcessInspector`.

### 4.4 Option C — track launched process identity (rejected)

Record the process ids CLIHub launches and treat them as running agents. Rejected because launches go
through a terminal runtime (`ProcessLauncher` → `IInteractiveProcessRunner`), so the agent is a child of
PowerShell/cmd/bash rather than the process CLIHub starts; agents can also be started outside CLIHub;
PID reuse and process-tree lifetime need job objects to be correct. It does not cover the scenarios the
spec describes and adds more platform machinery than Option B.

### 4.5 Decision and spec impact

**Decision: Option A.** The repo values an honest contract over aspirational code, and Option B is not
cheap or low-risk: it adds a platform dependency and a synchronous, heuristically permissioned query to
a UI-thread path. A documented limitation plus a clean model is preferable to an unreachable branch and
tests that assert fiction.

Spec impact analysis:

- `openspec/specs/agent-ui/spec.md:233-236` says: "WHEN a process matching the agent being updated is
  running / THEN 'Update' is unavailable...". It does not define matching and does not promise runtime
  or command-line matching.
- `openspec/specs/preferences-ui/spec.md:534-542` says the same in the Settings-card wording.
- The promise of runtime matching exists only in `docs/architecture/processes.md`, which is an
  architecture explanation, not a durable behavior contract.

Therefore Stage 2 needs **no** `agent-ui` or `preferences-ui` delta: it removes dead code and corrects
documentation, and the scenarios continue to hold for executable-name matches. We deliberately do not
add a scenario that codifies "runtime-hosted processes are never matched"; that would freeze an
implementation limitation into the behavior contract. The residual gap is documented in the architecture
note and listed in proposal.md so the follow-up decision is explicit.

### 4.6 Tests and documentation for Stage 2

| Change | Test |
|---|---|
| Matcher matches only by executable file name | Keep direct-match, case-insensitive, different-executable, and no-launch-command tests |
| Runtime branch removed | Replace the runtime-dependent tests with `IsMatch_RuntimeHostedProcess_ReturnsFalse` |
| `CommandLine` removed from the model | Update both test helpers and all constructor sites; the suite must compile without a command-line argument |
| Monitor behavior | Keep per-agent and other-agent tests; update them to the three-argument constructor |
| Inspector | Existing tests already assert only non-empty executable paths and valid start times; add nothing that requires a command line |

Documentation update in `docs/architecture/processes.md`: replace the runtime-matching paragraph with
the executable-name contract, and replace the WMI note with an explicit statement that the Windows
inspector does not retrieve command lines and that runtime-hosted agents are not detected; name the
follow-up direction (process identity or a cached async snapshot) without committing to it.

## 5. Ordering and staging

1. **Stage 1 — scoped invalidation.** Interface additions, two service implementations, management
   call-site change, tests. This is the behavioral fix with the wider blast radius (public interfaces,
   strict mocks); it ends with `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` green.
2. **Stage 2 — matcher honesty.** Matcher, inspector, model, tests, docs. No overlap with Stage 1 files;
   it ends with build, tests, and `openspec validate --all` green.

If the user approves commits later, each stage is a self-contained commit; Stage 1's spec deltas belong
to the same commit as its code so the behavior contract and implementation move together.

## 6. Verification

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
openspec validate --all
graphify update .
```

Stage 1 and Stage 2 each run the first two commands. The full set runs after both stages, with
`graphify update .` only after source edits are complete.

## 7. Risks and open questions

- **Residual runtime-hosted gap (accepted):** after Stage 2, npm-shim agents running as `node.exe` do not
  block updates. This is now documented rather than implied. If product requirements demand coverage,
  open a separate change for Option B (cached async snapshot preferred over per-call WMI).
- **In-flight probe after invalidation:** a probe started before `InvalidateFor` may still publish its
  result. This is accepted to preserve single-flight coalescing. A generation token would be needed for
  strict ordering; out of scope.
- **Cancellation gap:** `AgentVersionService` still runs the version command to completion after the
  caller's token is cancelled. Pre-existing; recorded for a future change because fixing it requires
  process-kill semantics in `CaptureOutputAsync`.
- **Interface growth:** adding `InvalidateFor` to the two interfaces forces no changes in strict mocks
  that only use `Invalidate()`; Moq accepts the extra member. Confirm during implementation that no
  hand-written fake implements either interface (none were found in the tree).
- **Model removal:** `AgentProcessInstance.CommandLine` is public; removal is intentional and verified
  to have no production readers beyond the matcher. If an external consumer exists outside this
  repository, it would need a compatibility note — none is known.
- **Unused matcher logger:** `AgentProcessMatcher` injects a logger it never uses. It is part of the
  finding-11 dead-member sweep and is deliberately not removed here.
