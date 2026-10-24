# Block updates for runtime-hosted agents via command-line process matching

## Why

Agent update blocking matches executable file names only
(`src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs:57-68`), and the Windows
inspector supplies only the process id, executable path, and start time
(`src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs:89-93`). Five of the six seed agents
declare an npm distribution source (`src/CLIHub.Core/SeedPlugins/*/plugin.json`, `updateCheck.source
= "npm"`), and npm installs on Windows launch through a `.cmd` shim that starts a shared
`node.exe` (`node.exe C:\...\node_modules\<package>\bin\<agent>.js ...`). Those agents are therefore
never matched as running, and `update blocking does not fire for them` — an update can run against
a live agent, the exact situation the monitor exists to prevent.

This is the parked backlog item "Runtime-hosted agent blocking (Option B)"
(`docs/backlog.md:61-71`, severity Medium — product gap). **Provenance note:** the user explicitly
pulled this item out of the backlog on 2026-10-24 as a proactive product-gap decision. Its parked
trigger — "a user reports an update not being blocked for an agent launched through node or
python" — has **not** fired; no user report exists. This change closes the gap ahead of a report
rather than in response to one.

Option B was previously analyzed and deferred in
`openspec/changes/archive/2026-10-20-harden-probe-caching-and-process-matching/design.md:240-260,332-334`,
which chose "Option A — amputate" and stated Option B "is a separate proposal if product
requirements demand blocking for runtime-hosted agents … (cached async snapshot preferred over
per-call WMI)". This is that separate proposal. It carries the archived analysis's constraints
forward instead of contradicting them: no per-process WMI (one bulk query per enumeration), a hard
timeout around a query that has no native cancellation, Winmgmt may be disabled by policy (so the
failure mode must be safe), and the cost must not land on the synchronous UI path. One archived
objection is factually stale: "a new package" — `System.Management` 8.0.0 is already referenced and
unused in `src/CLIHub.Core.Windows/CLIHub.Core.Windows.csproj:11`, so no new dependency is added.
Where this design departs from the archived preference for a "cached async snapshot", it does so
explicitly and with the reason (design D7).

## What Changes

**Behavior:**

- A running process matches a plugin when either (a) the process executable's file name equals the
  plugin's launch executable file name, ignoring case (unchanged), or (b) the process runs inside a
  known shared runtime host (`node`, `python`, `pythonw`) and its command line contains the plugin's
  declared npm package under `node_modules\` (new). Unrelated runtime-host scripts do not match.
- The Settings CLI Agents page's blocked state now also blocks cards whose agent is running under a
  runtime host; the execution-time gates (`AgentManagementService.UpdateAsync` and the launch
  window's update click) block those updates authoritatively.
- WMI command-line retrieval is best effort with a hard 2-second budget: on failure or timeout the
  pass logs a warning and degrades to executable-name matching (fail-open), matching the monitor's
  existing failure contract.

**Code (planned, not implemented here):**

- `CLIHub.Core`: `AgentProcessInstance` regains an optional nullable `CommandLine`; the matcher
  gains the runtime-host command-line rule and a match-detail parameter
  (`AgentProcessMatchDetail.ExecutableName` / `.CommandLine`) on candidate derivation;
  `AgentProcessMonitor` exposes the detail tier; `AgentManagementService.UpdateAsync` moves its
  gate enumeration into `Task.Run`.
- `CLIHub.Core.Windows`: `WindowsAgentProcessInspector` joins one bulk WMI
  `SELECT ProcessId, CommandLine FROM Win32_Process` query (name-filtered to candidates) onto the
  existing snapshot, behind an internal `IProcessCommandLineSource` seam with a hard timeout.
- `CLIHub` (WPF): `WindowActionCoordinator.CanExecuteUpdate` keeps cheap executable-name matching
  as the advisory early-out; `ExecuteUpdate` performs the authoritative command-line-detail check
  off the UI thread before starting the update.
- Specs: new `agent-process-matching` capability; a new blocked-state scenario each in `agent-ui`
  and `preferences-ui` (original scenario names preserved).
- Docs: `docs/architecture/processes.md` runtime-host limitation paragraph is replaced with the new
  contract; the backlog entry is resolved.

## Impact

**User-visible:**

- npm-shim agents (opencode, qwen-code, cline, pi, openclaude) now block their own update while
  running, on both the Settings card and the launch window.
- On machines where WMI is slow, unavailable, or disabled by policy, behavior is unchanged from
  today (name-only matching) plus one logged warning; updates remain available.
- The launch window's Update button may stay enabled while a runtime-hosted agent runs (the
  per-requery check stays cheap); clicking it reports the standard blocked message instead of
  starting the update.

**Internal:**

- Inspector port signature gains an explicit detail parameter; all inspector mocks in tests are
  updated mechanically. `WindowActionCoordinator.ExecuteUpdate` becomes a fire-and-forget async
  flow in the class's existing `_ = this.RunAgentCommandAsync(...)` shape. No XAML changes, no
  configuration-schema changes, no plugin-descriptor schema changes.

**Tests:**

- New matcher unit tests for command-line semantics (positive, boundary, negative), monitor tier
  tests, `AgentManagementService` gate tests, coordinator advisory/gate tests, and Windows
  inspector join/timeout/degradation tests through a fake command-line source. Real-WMI behavior
  and the live UI stay a manual smoke (see design D8).

## Non-goals

- PEB / `NtQueryInformationProcess` command-line retrieval (rejected in the archived analysis;
  design D1).
- Tracking the processes CLIHub itself launches (archived Option C, rejected).
- A monitor-level TTL cache of running agents (the backlog's monitor-cache entry warns it would
  stale the execution-time gate; the gates stay uncached and exact).
- Plugin descriptor schema changes — the npm `updateCheck.package` marker is reused as declared;
  no new fields.
- Command-line matching for non-npm distributions (`githubReleases` agents match by executable
  name only).
- Runtime hosts beyond `node`, `python`, `pythonw` — `pwsh` and shells are deliberately excluded
  (design D4); adding hosts later is a one-line set change.
- Blocking actions other than update (launch, version, CLIHub's own application updates) while an
  agent runs.
- Matching elevated/other-session processes whose command line WMI cannot read; those processes
  are skipped exactly as inaccessible `MainModule` reads are skipped today.
- A CHANGELOG entry now; it is deferred to the next release heading per the convention of the two
  most recent changes (`consolidate-version-population`, `async-blocked-state`).

## Dependencies

None — no other active OpenSpec change exists. The change builds on archived decisions without
reopening them: `2026-10-20-harden-probe-caching-and-process-matching` (Option B constraints, the
removed `CommandLine` field's history), `2026-10-20-fix-settings-open-delay` (candidate-name
pre-filter and assumption A1), and `2026-10-24-async-blocked-state` (the off-UI-thread blocked-state
pass this change feeds with fuller matching; that change explicitly listed this item as a non-goal).
