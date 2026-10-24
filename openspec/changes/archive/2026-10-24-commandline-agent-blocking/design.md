# Design: command-line agent blocking

## 1. Scope and goal

Close the "Runtime-hosted agent blocking (Option B)" product gap
(`docs/backlog.md:61-71`): update blocking must also fire for agents that run inside a shared
runtime host (npm shims → `node.exe`, python-hosted CLIs → `python.exe`), without putting a
multi-second WMI call on the synchronous UI path and without contradicting the accepted
`2026-10-20-harden-probe-caching-and-process-matching` decision that deferred this work.

All line numbers below are from the clean tree at commit `25d3ab7`.

## 2. Verified current state

| Area | Verified evidence |
|---|---|
| Matcher contract | `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs:57-68` — `IsMatch` compares `Path.GetFileNameWithoutExtension` of process path vs launch executable, `OrdinalIgnoreCase`; nothing else. `GetCandidateProcessNames` (`:27-44`) derives candidates from `plugin.Commands.Launch.Executable` file names only |
| Inspector port | `src/CLIHub.Core/Infrastructure/Processes/IAgentProcessInspector.cs:32` — `GetRunningProcesses(IReadOnlySet<string> candidateProcessNames)`; the remarks (`:14-26`) document the candidate pre-filter and assumption A1 (process name equals the image file name without extension) |
| Windows inspector | `src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs:28-111` — `Process.GetProcesses` (`:36`), PID ≥ 100 skip (`:43`), candidate-name pre-filter (`:54`), `MainModule?.FileName` with access-denied skip (`:60-70`), `StartTime` with fallback (`:78-87`); `:89` states "Matching is executable-name based; command lines are not retrieved" |
| Process model | `src/CLIHub.Core/Models/Agents/AgentProcessInstance.cs:9-11` — three positional fields; no `CommandLine` (removed by the 2026-10-20 change as the carrier of a nonexistent capability) |
| Monitor | `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:65-93` — one enumeration: candidates (`:72`) → inspector (`:73`) → first-match-per-process attribution (`:75-85`); exceptions log and return empty (`:87-90`); per-plugin overload filters by plugin id (`:100-107`) |
| Update gate (Settings) | `src/CLIHub.Core/Agents/AgentManagementService.cs:64-102` — `UpdateAsync` blocks when `GetRunningAgents(plugin)` is non-empty (`:76-83`), then executes the update command |
| Launch window Update | `src/CLIHub/ViewModels/Shared/WindowActionCoordinator.cs:145-161` — `CanExecuteUpdate` calls `HasRunningAgents(agent.Plugin)` synchronously (runs on every WPF `CommandManager` requery); `:163-182` — `ExecuteUpdate` re-checks `GetRunningAgents(agent.Plugin)` (`:173`) then runs the command through `LaunchCommandCoordinator` (`:181` → `src/CLIHub/ViewModels/Launch/LaunchCommandCoordinator.cs:49-120`), which has **no later running-agent gate** |
| Blocked-state pass | `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:345-391` — `PopulateBlockedStateAsync` runs `Task.Run(() => this.processMonitor.GetRunningAgents(), cancellationToken)` (`:362`) off the UI thread, generation- and token-guarded card writes (`:371-381`), started only when a plugin declares `update` (`:199`, `:223-226`) |
| Distribution evidence | All five npm seed agents declare `updateCheck.source = "npm"` with a package: `opencode` → `@opencode/cli`, `qwen-code` → `@qwen-code/qwen-code`, `cline-cli` → `cline`, `pi` → `@earendil-works/pi-coding-agent`, `openclaude` → `@twigpine/openclaude` (`src/CLIHub.Core/SeedPlugins/*/plugin.json`); `github-copilot-cli` declares `githubReleases`. Source-kind comparison precedent: `StringComparison.OrdinalIgnoreCase` (`src/CLIHub.Core/Agents/AgentReleaseClient.cs:95`, `AgentUpdateAvailabilityService.cs:162`) |
| Marker model | `src/CLIHub.Core/Models/Plugins/PluginUpdateCheck.cs:13` — `Package` is the npm package name; it survives catalog detached reads (`Plugin.Copy`, `src/CLIHub.Core/Models/Plugins/Plugin.cs:70`) |
| Package already present | `src/CLIHub.Core.Windows/CLIHub.Core.Windows.csproj:11` — `System.Management` 8.0.0 is referenced and unused (grep finds no `System.Management` usage in any `.cs` under `src/`) — residue of the cut-back 2026-10-10 implementation |
| Test seams | Inspector is mocked with `Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>()))` in `tests/CLIHub.Core.Tests/Agents/AgentProcessMonitorTests.cs:34,51,68,86,104`, `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs:31`, `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs:55,79,89,497,508,517,528,594,641`, `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs:443`; the Windows inspector has direct tests (`tests/CLIHub.Core.Windows.Tests/WindowsAgentProcessInspectorTests.cs:26-105`); `InternalsVisibleTo("CLIHub.Core.Windows.Tests")` exists (`src/CLIHub.Core.Windows/CLIHub.Core.Windows.csproj:19`); locator fake-seam precedent: `tests/CLIHub.Core.Windows.Tests/FakeWindowsRuntimeEnvironment.cs` |
| Regression pin to replace | `tests/CLIHub.Core.Tests/Infrastructure/Processes/AgentProcessMatcherTests.cs:78-91` — `IsMatch_RuntimeHostedProcess_ReturnsFalse` pins the documented limitation this change removes |
| Governing specs | `openspec/specs/agent-ui/spec.md:219-277` (card availability + async blocked state; "a process matching the agent being updated is running" at `:235`), `openspec/specs/preferences-ui/spec.md:535-567` (card actions; "a process matching that agent is running" at `:546,551`), `openspec/specs/agent-update-detection/spec.md:110-114` ("no matching agent process is running" as an existing rule reference). **No capability defines matching semantics**; `agent-detection` is marker-based install/project detection only (`openspec/specs/agent-detection/spec.md:3-5`) |
| Prior decision | `openspec/changes/archive/2026-10-20-harden-probe-caching-and-process-matching/design.md` — Option B analysis at `:240-260` (bulk WMI vs PEB risk table, cost objections), rejection of Option C at `:262-268`, deferral at `:272-275,332-334`; history of the removed per-process WMI at `:39-42` |
| Docs statement | `docs/architecture/processes.md:20-22` documents the limitation ("Agents launched through a command shim into a shared runtime … are not matched") and `:29-33` the inspector's no-command-lines contract and the follow-up direction |

### Caller census (production, verified by codegraph + grep over `src/`)

`IAgentProcessInspector.GetRunningProcesses` — exactly one production caller:

- `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:73`

`AgentProcessMatcher.GetCandidateProcessNames` / `IsMatch` — one production caller each:

- `AgentProcessMonitor.cs:72` and `:79`

`AgentProcessMonitor.GetRunningAgents` / `HasRunningAgents` — four production call sites:

1. `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:362` — blocked-state pass, already
   inside `Task.Run` (cancellable, off UI thread). **Safe place for the expensive tier.**
2. `src/CLIHub/ViewModels/Shared/WindowActionCoordinator.cs:155` — `CanExecuteUpdate`, synchronous
   on every `CommandManager` requery. **Hot path; must stay cheap.**
3. `src/CLIHub/ViewModels/Shared/WindowActionCoordinator.cs:173` — `ExecuteUpdate`, synchronous at
   click time; the only running-agent gate on the launch-window update path. **Authoritative.**
4. `src/CLIHub.Core/Agents/AgentManagementService.cs:76` — `UpdateAsync` execution-time gate, the
   only gate on the Settings update path. **Authoritative.**

`AgentProcessMonitor.HasRunningAgents()` (parameterless, `:41-44`) has **no production caller**
(tests only); it follows the default tier like everything else.

## 3. Decisions

### D1 — Command-line retrieval route: one bulk WMI query per enumeration, behind the inspector port

`WindowsAgentProcessInspector` keeps its `Process.GetProcesses` snapshot and, when command-line
detail is requested and at least one candidate-named running process exists, issues **one** WMI
query — `SELECT ProcessId, CommandLine FROM Win32_Process` with a `WHERE Name = '…' OR …` filter
over the candidate names — via the already-referenced `System.Management` package, maps PID →
command line, and joins it onto the instances it returns. This is the archived analysis's "bulk
WMI" route, not per-process WMI (the 2026-10-10 design's mistake, `archive/…/design.md:39-42`).

PEB reading (`NtQueryInformationProcess` + `ReadProcessMemory`) stays rejected for the archived
reasons (`archive/…/design.md:245`): architecture- and session-specific offsets, unsafe code,
breakage on protected/elevated processes, fragility across Windows versions, and poor testability.
WMI's own weaknesses — often-unreadable `CommandLine` for other users'/elevated processes, Winmgmt
disabled by policy, EDR interception (`archive/…/design.md:244`) — are absorbed by D6's degradation
contract rather than solved.

The dependency objection in the archived table ("a new package and a new failure mode") is half
stale: `System.Management` 8.0.0 is already referenced by `CLIHub.Core.Windows.csproj:11`. The new
failure mode is real and is exactly what D6 specifies.

### D2 — Port shape: an explicit match-detail parameter; `CommandLine` returns to the model

One new enum in Core and one new parameter on the existing port method — no second method, no
async port:

```csharp
namespace CLIHub.Core.Infrastructure.Processes;

/// <summary>
///   The fidelity of running-agent process matching.
/// </summary>
public enum AgentProcessMatchDetail
{
    /// <summary>
    ///   Matches the process executable's file name against the launch executable only. Cheap:
    ///   no command-line retrieval.
    /// </summary>
    ExecutableName,

    /// <summary>
    ///   Additionally matches processes hosted inside a shared runtime (for example
    ///   <c>node.exe</c>) by their command line. Authoritative for update gating.
    /// </summary>
    CommandLine
}
```

```csharp
IReadOnlyList<AgentProcessInstance> GetRunningProcesses(
    IReadOnlySet<string> candidateProcessNames,
    AgentProcessMatchDetail detail);
```

The parameter is **explicit — no default** — on the interface, so every implementation, caller,
and Moq setup must choose a tier deliberately (a default on the mocked member would let existing
one-argument setups silently stop matching calls that pass the other tier; Moq loose mocks would
then return null and the monitor's catch would silently swallow it). The monitor keeps an
optional parameter **with** default `CommandLine`, so the authoritative tier is the default at the
service level and only the hot path opts down:

```csharp
public IReadOnlyList<RunningAgent> GetRunningAgents(
    AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine)

public bool HasRunningAgents(Plugin plugin,
                             AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine)
```

`AgentProcessInstance` regains the field the 2026-10-20 change removed, as an **optional nullable**
positional parameter so every existing three-argument construction site keeps compiling:

```csharp
public sealed record AgentProcessInstance(int ProcessId,
                                          string ExecutablePath,
                                          DateTime StartTime,
                                          string? CommandLine = null);
```

That removal was justified because the field was "the carrier of the removed capability"
(`archive/…/design.md:228-229`); this change delivers the capability, so the field returns, and
the port's XML documentation is updated to state which fields must be supplied always (id, path,
start time) versus in `CommandLine` detail only (command line, null when unavailable or degraded).

### D3 — Cost placement per call site: advisory cheap tier on requery, authoritative full tier at the gates

Verified against the census in §2:

| Call site | Tier | Change |
|---|---|---|
| `AgentManagementViewModel.PopulateBlockedStateAsync` (`:362`) | `CommandLine` (default) | None — the pass already runs the enumeration inside `Task.Run` under a cancellable token; it simply becomes fuller |
| `WindowActionCoordinator.CanExecuteUpdate` (`:155`) | `ExecutableName` (explicit) | Pass the cheap tier; per-requery cost stays exactly today's (snapshot + name filter + a few module reads; no WMI) |
| `WindowActionCoordinator.ExecuteUpdate` (`:173`) | `CommandLine` (default) | The gate becomes the fire-and-forget async flow of D7 so the WMI wait stays off the UI thread |
| `AgentManagementService.UpdateAsync` (`:76`) | `CommandLine` (default) | The gate enumeration moves into `Task.Run` (D7) |

This is defense in depth, matching the brief's suggested split: the enabled state of the launch
window's Update button is an **advisory early-out** (a runtime-hosted agent may leave the button
enabled), while both execution paths gate on full command-line matching before anything starts.
Accepted UX trade: clicking Update while a node-shim agent runs reports the existing
"Update blocked while the agent is running: …" status message (`StatusMessageCoordinator.ReportUpdateBlockedByRunningAgents`, `WindowActionCoordinator.cs:176-178`) instead of the button being pre-grayed. The Settings cards do not have this trade — their settled state comes from the full-tier pass.

The launch window has no second gate after `ExecuteUpdate` (verified: `LaunchCommandCoordinator.ExecuteAsync` (`LaunchCommandCoordinator.cs:69-120`) goes straight to `IAgentCommandWorkflow.ExecuteAsync`), so site 3 must use the full tier — this is not optional.

### D4 — Candidate superset and narrowing: host names derived in the matcher, gated on npm-declared plugins

Today's candidates are the launch executables' file names (`AgentProcessMatcher.cs:27-44`) —
plugin-declared (`PluginCommand.Executable`, e.g. `"opencode"`, `"pi"`), extension-stripped,
case-insensitive. The superset adds the shared runtime hosts, but only when they can pay off:

- The known-host set is a matcher-owned constant: `node`, `python`, `pythonw`
  (`AgentProcessMatcher.RuntimeHostProcessNames`, case-insensitive). These are the hosts the
  original runtime branch targeted (Node.js/Python, `archive/…/design.md:29`) and the hosts npm
  shims and python entry points actually spawn.
- Host names join the candidate set **only** when `detail == CommandLine` **and** at least one
  plugin declares a launch command plus an npm `updateCheck` package (the command-line marker of
  D5). With no npm-declared agent in the catalog, enumeration is byte-for-byte today's behavior —
  a machine full of `node.exe` processes never triggers a WMI read.
- The WMI query's `WHERE` filter is the candidate names ∩ running candidate processes, and it is
  skipped entirely when no candidate-named process is running. One query per enumeration,
  regardless of how many processes match.

`pwsh`/`powershell` are deliberately excluded: CLIHub itself spawns those terminals constantly, so
they would force a WMI read on nearly every enumeration, and PowerShell command lines carry user
scripts with no package marker — pure cost, no possible match. `bun`/`deno` stay out until a
descriptor declares an agent that needs them. Narrowing invariant preserved and extended: a
command-line match requires a host-named process, host names are candidates exactly when
command-line matching is active, so the pre-filter cannot drop a match the matcher would make
(extends the A1 argument recorded in `IAgentProcessInspector.cs:22-26`).

### D5 — Command-line matching semantics: declared npm package path segment

A process command-line-matches a plugin when **all** hold:

1. The plugin declares a launch command (the existing guard, `AgentProcessMatcher.cs:59-62`).
2. The plugin's `updateCheck` is an npm source with a non-empty `Package` — source kind compared
   `OrdinalIgnoreCase`, the established precedent (`AgentReleaseClient.cs:95`).
3. The process executable's file name (without extension) is in
   `RuntimeHostProcessNames` (`node`, `python`, `pythonw`).
4. The process's `CommandLine` (non-null, non-empty; null under D6 degradation) contains the
   marker `node_modules\` + package with `/` → `\` + a trailing `\`, after the command line is
   normalized by `/` → `\`, compared `OrdinalIgnoreCase`.

The trailing separator is the boundary rule: `node_modules\@opencode\cli\` does **not** match the
command line of `node_modules\@opencode\cli-extra\bin\x.js`, and `node_modules\cline\` does not
match `node_modules\cline-something\`. Concrete shapes:

- npm global shim (the backlog's example):
  `node.exe C:\Users\Maxim\AppData\Roaming\npm\node_modules\@opencode\cli\bin\opencode.js run`
  → matches plugin `opencode` (`@opencode/cli`).
- Unscoped package: `node.exe "C:\...\node_modules\cline\dist\index.js"` → matches `cline-cli`.
- Forward slashes (node normalizes paths):
  `node.exe C:/Users/x/npm/node_modules/@qwen-code/qwen-code/bin/qwen.js` → matches `qwen-code`.
- Project-local install: `node.exe D:\proj\node_modules\@earendil-works\pi-coding-agent\bin\pi.js`
  → matches `pi` (an agent running from a project tree is still a running agent).

Negative cases (each gets a test):

- `node.exe server.js`, `node.exe D:\build\script.js`, `python.exe app.py` — no
  `node_modules\<package>\` segment → no match, the update is not blocked.
- A node process of a *different* package → no match (boundary + exact segment).
- A host-named process with `CommandLine == null` (WMI could not read it, or D6 degraded) → no
  command-line match; the name rule still applies (which cannot match a host-named process unless
  the plugin's launch executable *is* the host).
- A plugin without an npm `updateCheck` (e.g. `github-copilot`) never command-line-matches; it
  keeps executable-name matching only.

Matching stays "executable-name OR command-line" inside `IsMatch`; the monitor's first-match
attribution per process (`AgentProcessMonitor.cs:77-84`) is unchanged (a command line naming two
packages is pathological; first plugin wins, same as today's tie handling).

### D6 — Failure and timeout: hard 2-second budget, degrade to name-only, fail open

The WMI join runs behind an internal seam so the Windows adapter's logic is testable without WMI:

```csharp
namespace CLIHub.Core.Windows;

/// <summary>
///   Supplies command lines for running processes by process id. Implementations perform one
///   bulk query for the given process names.
/// </summary>
internal interface IProcessCommandLineSource
{
    /// <summary>Returns a PID → command-line map; entries may be missing when a command line is unreadable.</summary>
    IReadOnlyDictionary<int, string> GetCommandLines(IReadOnlySet<string> processNames);
}
```

The real `WmiProcessCommandLineSource` (`System.Management`, one `ManagementObjectSearcher` query
with `EnumerationOptions.Timeout` set as a best-effort hint) is plain synchronous; the **hard
timeout lives in the inspector**: the source call runs on the thread pool and is awaited with a
2-second wall-clock budget (`Task.Run(...).Wait(CommandLineTimeout)`), because synchronous WMI has
no reliable cancellation (archived analysis, `archive/…/design.md:244`). On timeout the abandoned
query task is left to complete in the background with its exception observed-and-swallowed by a
continuation; on any failure the inspector logs one warning through its existing
`ILogger<WindowsAgentProcessInspector>` and returns the snapshot with `CommandLine = null`.

Per call site, degraded matching means:

- **Blocked-state pass** — cards settle per executable-name matching. Consistent with the
  existing fail-open contract: an enumeration *failure* already logs and settles at declarative
  availability (`agent-ui` "Blocked-state enumeration failure is logged",
  `openspec/specs/agent-ui/spec.md:273-277`; `AgentProcessMonitor.cs:87-90`); a *degraded*
  enumeration is strictly more information than a failed one.
- **Execution-time gates** (`UpdateAsync`, `ExecuteUpdate`) — name-only matching applies, so on a
  degraded pass a runtime-hosted agent would not be blocked. **Fail-open is deliberate**: the
  gates' existing behavior on enumeration failure is already fail-open (the monitor logs and
  returns empty, the update proceeds), and fail-closed would block *every* agent update on any
  machine where Winmgmt is disabled by policy or intercepted by EDR — a far worse regression than
  the gap this change closes. The monitor-cache backlog entry's staleness warning
  (`docs/backlog.md:48-59`) concerns caching; this design keeps both gates uncached and exact
  whenever WMI answers within budget.

### D7 — Threading at the two gates; why no cached snapshot

- `AgentManagementService.UpdateAsync` (`AgentManagementService.cs:76`): the gate becomes
  `var runningAgents = await Task.Run(() => this.processMonitor.GetRunningAgents(plugin), cancellationToken).ConfigureAwait(false);`
  — the same enumeration, off the caller's context, honoring the caller's token. The Settings
  card's blocked message shape is unchanged.
- `WindowActionCoordinator.ExecuteUpdate` (`WindowActionCoordinator.cs:163-182`): keep the
  synchronous early-outs (no agent, `!CanUpdate`, plus the cheap name-only fast block), then hand
  off to a private `async Task` continuation in the class's established fire-and-forget shape
  (`_ = this.RunAgentCommandAsync(...)` at `:63,181`): run the full-tier check in `Task.Run`,
  report `ReportUpdateBlockedByRunningAgents` on a block, otherwise continue into
  `RunAgentCommandAsync`. The WPF `RelayCommand` execute delegate stays synchronous; only the
  gate-to-launch sequence becomes asynchronous, exactly as the launch path already is.

**Deviation from the archived preference, recorded:** the archived deferral said Option B prefers
a "cached, asynchronous process snapshot" (`archive/…/design.md:253-254,334`). That preference was
written when `GetRunningAgents` ran synchronously on the UI thread at Settings load and on every
requery. Since then, `async-blocked-state` (2026-10-24) moved the Settings enumeration off the UI
thread, and this change moves both click-time gates' expensive part into `Task.Run`. With no
synchronous-UI caller left on the expensive tier, a cache has no responsiveness job to do — only a
staleness risk at the gates, which the backlog entry explicitly forbids. No cache is introduced;
revisit only if profiling (the monitor-cache trigger) shows the filtered full-tier enumeration as
a hot spot.

### D8 — Test strategy

| Layer | Test vehicle | Coverage |
|---|---|---|
| Matcher semantics | `AgentProcessMatcherTests` (pure records) | npm shim match, scoped/unscoped package, forward slashes, case-insensitivity, trailing-separator boundary, unrelated script, different package, null/empty command line, non-npm plugin, no-launch plugin; candidate derivation adds host names only under `CommandLine` detail with an npm plugin, adds nothing without one; existing name-match tests unchanged |
| Monitor tiers | `AgentProcessMonitorTests` (mock inspector) | detail flows to the inspector; `CommandLine` default matches a node-shim instance; `ExecutableName` tier does not; per-plugin attribution across tiers; every existing mock setup updated to the two-argument signature |
| Service gate | `AgentManagementServiceTests` | blocked outcome for a runtime-hosted instance (node.exe + package command line); not blocked for an unrelated node script; blocked message names the plugin |
| Launch window | new `WindowActionCoordinatorTests` (none exist today — `tests/CLIHub.Tests/ViewModels/Shared/` has only `StatusMessageCoordinatorTests`) | `CanExecuteUpdate` consults only the `ExecutableName` tier (mock verifies the detail argument) and stays false on a name match; `ExecuteUpdate` blocks and reports on a full-tier match without invoking the launch coordinator; unblocked path launches |
| Settings cards | existing `AgentManagementViewModelTests` | one new test: a node-shim `AgentProcessInstance` (with command line) blocks the card's Update and writes the explanation — through the existing blocked-state pass seams (`Processes(...)` helper at `:79` gains a command-line argument) |
| Windows adapter | new `WindowsAgentProcessInspectorTests` cases with a fake `IProcessCommandLineSource` (internal seam, `InternalsVisibleTo` at `CLIHub.Core.Windows.csproj:19`) | join correctness (command line attached to the right PID); `ExecutableName` detail never queries the source; no candidate processes → no query; source throws → warning + null command lines; source blocks past the budget → timeout degradation. Constructor injection keeps the existing direct-call tests working with the production source excluded |

Stays **manual** (CI-unreliable per the archived analysis, `archive/…/design.md:256-258`): real
WMI behavior on a live machine (start `node …\node_modules\@opencode\cli\bin\opencode.js`, verify
the card blocks), the 2-second budget against a genuinely hung Winmgmt, EDR-intercepted queries,
and the live launch-window click path. These land as the change's manual smoke task.

### D9 — Spec placement: a new `agent-process-matching` capability

The brief guessed the owning requirement lives in `agent-detection`; verified, **no capability
defines process matching** — `agent-detection` covers marker-based install/project detection only
(`openspec/specs/agent-detection/spec.md:3-47`), and the blocked-state specs reference "a process
matching the agent" without defining matching (`agent-ui:235`, `preferences-ui:546,551`). Matching
semantics need a durable home, so this change adds the `agent-process-matching` capability with
the executable-name rule, the runtime-host rule, the detail tiers, candidate narrowing, and the
degradation contract; `agent-ui`'s "Agent card action availability" and `preferences-ui`'s "Agent
card actions" requirements each gain scenarios for the runtime-hosted block (all original scenario
names preserved verbatim). `agent-update-detection:110-114` stays untouched — its "no matching
agent process is running" wording remains true and now resolves through the new capability.

### D10 — Docs and backlog

`docs/architecture/processes.md:20-33` is rewritten in the implementation: the matcher bullet
gains the command-line rule, the inspector bullet gains the bulk-WMI join, budget, and degradation,
and the closing "runtime-hosted agents are not detected" paragraph (`:29-33`) is replaced with the
new contract and the known residual (elevated/other-session command lines unreadable; degraded
passes match by name only). The backlog entry (`docs/backlog.md:61-71`) is resolved in the
established format, and the `agent-ui` card-availability prose keeps working because its
"process matching" phrase now resolves through the new capability.

## 4. Alternatives considered

| Alternative | Rejected because |
|---|---|
| PEB read (`NtQueryInformationProcess`) | Archived risk table: unsafe code, per-architecture offsets, breaks on protected processes, fragile across Windows versions, hard to test (`archive/…/design.md:245`) |
| Per-process WMI | The 2026-10-10 design did this and was cut back for hangs; the archived history says one query per process was the mistake (`archive/…/design.md:39-42`). Bulk single query instead |
| Making `IAgentProcessInspector` async | Ripples `async` through `AgentProcessMonitor`, `HasRunningAgents`, and the WPF `RelayCommand` canExecute (which cannot be async); the two-tier split plus `Task.Run` at the gates achieves the same responsiveness with a sync port |
| A cached, invalidated snapshot service | No synchronous-UI caller remains on the expensive tier after D7; a cache would only add the gate-staleness risk the monitor-cache backlog entry forbids (`docs/backlog.md:48-59`) |
| Tracking processes CLIHub launches (archived Option C) | Agents start outside CLIHub too; PID reuse/job objects needed; rejected in the archived design (`archive/…/design.md:262-268`) |
| First-token-after-host matching (`node.exe <script> …`) | The first argument *is* the script path in the shim shape, so the package-segment marker subsumes it with strictly fewer false positives; a bare-name first token (e.g. `node server.js`) would over-block on name collisions |
| Descriptor-declared host list or markers | No schema churn needed — the npm `updateCheck.package` already encodes the marker for exactly the agents that need it; revisit when a non-npm runtime-hosted agent appears (open question 3) |
| Including `pwsh` in the host set | CLIHub's own terminals are pwsh/powershell — every launch would force a WMI read per enumeration, and pwsh command lines carry user scripts with no package marker: cost without possible matches (D4) |
| Fail-closed gates on WMI failure | Would block every agent update on Winmgmt-disabled/EDR machines; inconsistent with the monitor's existing fail-open failure contract (`AgentProcessMonitor.cs:87-90`) |

## 5. Deviations from the brief

1. **"likely agent-detection's matching requirement"** — no such requirement exists;
   `agent-detection` is marker-based install/project detection. The matching semantics get a new
   `agent-process-matching` capability instead (D9).
2. **"cached async snapshot preferred"** (carried from the archived decision) — deliberately not
   followed; the situation that motivated it (synchronous-UI callers) no longer exists after
   `async-blocked-state` plus D7. Reasoning in D7.
3. **The brief's design question 2 named two cost sites (Settings load, `CanExecuteUpdate`) plus
   `UpdateAsync`** — the verified census shows a fourth production site
   (`WindowActionCoordinator.ExecuteUpdate:173`), which is the *only* gate on the launch-window
   path and therefore needs the full tier plus the async restructure (D3/D7).
4. **"possibly a new blocked-state scenario in agent-ui"** — confirmed, and `preferences-ui` needs
   one too (its "Update blocked while running" scenario governs the Settings card click path,
   `preferences-ui:549-552`); both are included.

## 6. Open questions — resolved by the user (2026-10-24)

All five open questions are resolved by accepting the recorded defaults; implementation proceeds
from them, and items 1 and 5 stay smoke-observable:

1. **2-second budget value** — accepted (2 s); confirm the degradation latency feels right during
   the manual smoke.
2. **System.Management 8.0.0 on net10.0** — accepted (keep 8.0.0; bump only on a surfaced runtime
   incompatibility).
3. **Host set extensibility** — accepted (defer until a real non-npm/python descriptor demands a
   descriptor-declared extension).
4. **Launch-window pre-gray for runtime-hosted agents** — accepted (not done; enabled button +
   click-time block; a background-refreshing advisory state would be a separate UI change).
5. **Abandoned WMI query pile-up** — accepted (no cooldown now; revisit only if a smoke shows
   pile-up).
