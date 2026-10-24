# Low-severity sweep 2: ProjectState boundary copies, running-agent cache decision, Git Bash PATH wording

## Why

`docs/backlog.md` carries three low-severity items the user approved as one batch on 2026-10-24.
Each was re-verified against the current tree before inclusion (the backlog cites state from
2026-10-20; one claim needed corrected line numbers, one item is recommended dropped — see
design for the per-item verdicts):

1. **ProjectState shares live items at the persistence boundary** (`docs/backlog.md:220-225`) —
   `ProjectState.From`/`ApplyTo`/`CopyTo` share `Project` references (and `ApplyTo` shares the
   live `List<Project>`) with the cached configuration snapshot, so service-owned in-place
   mutations (`ProjectService.ToggleFavorite`, `SetCurrentProject`) leak into stored state
   outside any update transaction. Confirmed exactly; all five cited line numbers are still
   byte-accurate.
2. **Monitor-level running-agent cache** (`docs/backlog.md:52-63`) — the backlog proposes a
   monitor-level cache "only with an explicit invalidation story". Re-evaluated against the
   post-`commandline-agent-blocking` tier design: **recommended dropped as resolved-by-design**.
   The requery hot path now uses the cheap executable-name tier (no WMI), both execution-time
   gates run the full tier off the UI thread with a 2-second WMI budget, and the Settings-load
   pass is async and cancellable. The residual per-requery cost is one name-filtered
   `Process.GetProcesses` snapshot; the item's own trigger (profiling evidence) never fired, and
   no cache key is provably fresh. This change records that decision; it implements nothing for
   this item.
3. **Runtime executable PATH-scan identity** (`docs/backlog.md:134-146`) — the PATH steps of
   `WindowsRuntimeLocator.LocateGitBash`/`LocatePowerShell7` accept the first matching
   executable, while the `agent-commands` requirement says Git Bash shall "never an arbitrary
   `bash.exe` resolved from the operating system path" — wording stronger than the approved
   lookup order guarantees. Confirmed with corrected line numbers (the backlog's `:233-303` /
   `:304-369` spans drifted after `low-sweep` restructured the registry step). The remedy is
   the spec-wording option: a MODIFIED `agent-commands` requirement that states the actual
   guarantee. Distribution validation of PATH hits is evaluated and rejected (design C4).

The two implemented items are independent (persistence-boundary hardening vs. a spec-wording
delta); bundling them with the item-2 decision follows the `low-sweep`/`hygiene-sweep` batch
precedent and avoids separate verify-and-archive cycles for individually small work.

## What Changes

1. **Copy projects across the ProjectState boundary** — the five crossing points in
   `src/CLIHub.Core/Models/Projects/ProjectState.cs` (`From(AppConfig)` :31,
   `From(ConfigurationSnapshot)` :48, `ApplyTo(AppConfig)` :58, `ApplyTo(ConfigurationSnapshot)`
   :70, `CopyTo` :83) copy items with the canonical `Project.Copy()` instead of sharing
   references; `ApplyTo` assigns a fresh list of copies instead of the live list. After a
   `ProjectService.Persist()`, the repository's cached snapshot no longer aliases the service's
   live `Project` instances. New tests prove mutation isolation in both directions, including
   one that fails today against the real `ConfigurationRepository`. Details in design A1–A5.
2. **No cache work** — item 2 is recorded as resolved-by-design. The `agent-process-matching`
   tier contract (advisory requery tier, authoritative uncached gates) is the resolution; the
   backlog entry is rewritten at implementation time. Details in design B1–B3.
3. **Align the `agent-commands` Git Bash wording with the approved lookup order** — a MODIFIED
   "Terminal vs captured execution" requirement (scenario names preserved) replaces the
   over-claiming "never an arbitrary `bash.exe` resolved from the operating system path" clause
   with the actual guarantee: fixed preference order (registry `InstallPath` → well-known → PATH
   outside the Windows directory), absolute path, WSL-launcher exclusion, PATH hits accepted
   without distribution verification. The "Git Bash resolves to Git for Windows" scenario THEN
   and two aligned lines (the WSL scenario's AND, the unresolvable-Git-Bash WHEN) get the same
   honesty, and one ADDED scenario pins the deliberate pwsh PATH acceptance (Store execution
   alias). No production change. Details in design C1–C5.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `agent-commands` — the only behavior-bearing artifact of item 3 is the spec text itself, so
  the change carries a `specs/agent-commands/spec.md` delta (MODIFIED "Terminal vs captured
  execution" with all scenario names preserved plus one ADDED scenario). Items 1 and 2 take no
  deltas: item 1 is implementation hardening that brings the code under the existing
  `configuration-snapshot` contract ("Stores use the snapshot boundary" /
  "Read values are detached from the store", `openspec/specs/configuration-snapshot/spec.md:71-83`)
  — no scenario's observable contract changes; item 2 changes nothing at all.

Because item 3 requires a delta, the change does not set `skip_specs` (following
`openspec/changes/archive/2026-10-24-commandline-agent-blocking/`, which also has no
`.openspec.yaml`).

## Impact

- Code:
  - `src/CLIHub.Core/Models/Projects/ProjectState.cs` — the five crossing points copy items via
    `Project.Copy()`; no signature, namespace, or DI change.
- Tests:
  - `tests/CLIHub.Core.Tests/Projects/ProjectStateTests.cs` (new) — five boundary isolation
    facts (`From(AppConfig)`, `From(snapshot)`, `ApplyTo(AppConfig)`, `ApplyTo(snapshot)`,
    `CopyTo`), each failing before the fix.
  - `tests/CLIHub.Core.Tests/Projects/ProjectStateStoreTests.cs` — two added facts:
    `Load`-returned state mutation does not change stored state (pins the already-correct
    direction), and a real-`ConfigurationRepository` fact proving a `Project` held out of an
    `Update` callback cannot corrupt stored state after the transaction completes (fails
    before the fix; the `FakeConfigurationRepository` re-copies and would mask it — design A3).
  - `tests/CLIHub.Core.Tests/Configuration/AppConfigSerializationTests.cs:279` — optional rename
    only (`ProjectState_TransfersProjectOwnershipWithoutChangingValues` names the aliasing
    semantics being removed); assertions unchanged.
- Specs: `specs/agent-commands/spec.md` delta in this change (item 3).
- Docs: `docs/backlog.md` — the three swept entries rewritten (two resolved, one
  resolved-by-design), one hunk per entry.
- Risk: low. Item 1 is confined to `ProjectState` (callers verified: `ProjectStateStore`
  `Load`/`Update`, `ProjectService.Persist`; no caller relies on shared references — no
  `ReferenceEquals` on `Project` anywhere in `src/`, comparisons are by `Id`/`Path` value).
  Item 3 is wording-only; every locator test and the unchanged WSL/unresolvable scenarios keep
  passing because behavior does not change.

## Non-goals

- No persistence-format, `AppConfig` shape, or `Project.Copy()` changes (the copy is already
  the canonical detached copy, `src/CLIHub.Core/Models/Projects/Project.cs:54-63`).
- No removal of the production-unused `From(AppConfig)`/`ApplyTo(AppConfig)` pair (test-covered
  public API; dead-API sweeps are separate work).
- No running-agent cache, no invalidation infrastructure, no WMI event watchers, no
  `AgentProcessMonitor`/`WindowActionCoordinator`/`WindowsAgentProcessInspector` changes
  (item 2 is a recorded decision, not work).
- No distribution validation for Git Bash PATH hits (no `git.exe` sibling or version-file
  checks) — evaluated and rejected (design C4); the existing trigger ("a user report of a wrong
  bash flavor") remains the reopen condition recorded in the rewritten backlog entry.
- No `LocatePowerShell7` changes of any kind.
- No changes to `docs/backlog.md` beyond the three entry rewrites.
- No CHANGELOG entry now; entries land under the next release heading.
- No commit, push, or archive in this change's own tasks; those follow the repo approval policy.

## Dependencies

- None on other active changes (`openspec/changes/` holds no other active change; working tree
  clean).
- Item 2's decision depends on the shipped `commandline-agent-blocking` design (2026-10-24,
  archived): the advisory/gate tier split (its design D3) and the 2-second WMI budget are what
  make the drop honest.
- Item 3's delta must keep the requirement name "Terminal vs captured execution" and every
  existing scenario name; the wording follows the lookup order pinned by the archived
  `2026-10-20-git-bash-resolution` change and restated by `low-sweep` design E6.
