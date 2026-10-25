# Split the CLI Agents Detect action: installation check and update check

## Why

On Settings → CLI Agents the AGENTS section has exactly one chip button, `Detect`
(src/CLIHub/Views/SettingsAgentsPage.xaml:121-142), whose command
(`AgentManagementViewModel.DetectAsync`, src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:431-478)
does three unrelated things per agent: re-detects host installation, re-probes the version, and
re-checks update availability (`ApplyProbeResultAsync` calls `availability.CheckAsync` for every
installed agent, src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:490-504). The user cannot
answer "is it installed here, and what version?" without also hitting every agent's declared
distribution source, and cannot answer "is there a newer version?" without re-running every
installation probe. The per-card first action is named "Test" while doing exactly the same combined
work (src/CLIHub/Views/SettingsAgentsPage.xaml:210-220), which names neither half of what it does.

## What Changes

Decisions were taken with the user; this change implements exactly these:

1. **Two header chip buttons** in the AGENTS section, side by side on the right (the single-button
   `Grid` child becomes a right-aligned horizontal `controls:SpacedPanel`):
   - `Detect` (keeps `IconGlyphs.Refresh`): re-detects installation **and version** locally — no
     network, no update-availability call.
   - `Check updates` (new, `IconGlyphs.Sync`): forces a fresh update-availability check for every
     installed agent, bypassing the cached TTL via `IAgentUpdateAvailabilityService.InvalidateFor` +
     `CheckAsync` at the call site.
   Both are icon+label chip buttons in the existing `AgentsHeaderChipButton` style, each with an
   `AutomationProperties.Name` and a tooltip that states exactly what it does and that it does not
   do the other thing.
2. **Per-card action renamed Test → Detect.** Tooltip and `AutomationProperties.Name` change to
   Detect; semantics become strictly local (installation + version, no availability call).
3. **Installation check semantics** (header `Detect` and per-card `Detect`): card → `Detecting`,
   settle from `AgentManagementService.ProbeAsync`. Installed → `Installed` + freshly probed
   version, existing `UpdateAvailability` untouched. Not installed → `NotInstalled`,
   `Version = null`, `UpdateAvailability = null`. A thrown probe restores the previous card state
   (existing restore-on-throw behavior preserved).
4. **Update check semantics** (`Check updates`): for every card whose `InstallState` is `Installed`,
   `InvalidateFor` then `CheckAsync`, then assign `UpdateAvailability`; `NotInstalled` cards are
   skipped and their `UpdateAvailability` cleared. No version command and no detection re-run at
   the call site in this action.
5. **Load-time population stays as is** (`PopulateVersionsAsync` probes version and availability
   for installed cards). The new button is a forced re-check, not the only trigger.
6. **Busy state:** the two header actions never run concurrently; each reports its busy state
   (`IsDetecting`, new `IsCheckingUpdates`); each command is disabled while the other runs; the
   per-card Detect is disabled while either header action runs (rule stated in design.md).
7. **Manual-check result:** a muted summary line next to the header buttons —
   `Checked 5 agents · 1 update available` / `Checked 5 agents · no updates` (correct
   singular/plural), `No installed agents to check` when zero agents are installed, cleared on page
   reload and when a new check starts.
8. `AgentManagementService.ProbeAsync` stays the detection+version probe (it already short-circuits
   the version command for a not-installed agent, src/CLIHub.Core/Agents/AgentManagementService.cs:60-64).
   The force path is `InvalidateFor` + `CheckAsync` at the call site; `AgentUpdateAvailabilityService`
   is not changed.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `agent-ui` — ADDED requirement "Agent management header actions" (two actions, split semantics,
  mutual exclusion, tooltips, summary line); the requirement "Agent card action availability" is
  replaced (REMOVE + ADD, or MODIFIED with the scenario name preserved — decided by validator
  behavior, recorded in design.md) so the scenario "Test always available" becomes a Detect
  scenario with local-only semantics and the per-card busy gate.
- `agent-update-detection` — ADDED requirement "On-demand update availability check" (forced check
  bypasses the per-agent cache via invalidation; gated on host installation; no detection or
  version command at the call site; the availability service itself is unchanged); MODIFIED
  requirement "Update checks are gated on host installation" gains a forced-check scenario (all
  existing scenario names preserved).

`agent-version` and `agent-availability-display` need no delta: the version probe semantics and
caching are untouched, and the launch-window display behavior is untouched.

## Impact

Source (all in `src/`):

- `CLIHub/ViewModels/Agents/IAgentManagementSection.cs` — new members: `CheckUpdatesCommand`,
  `IsCheckingUpdates`, `CanCheckUpdates`, `UpdateCheckSummary`; the per-card command is renamed to
  match the action (design Decision 7); doc updates for the narrowed Detect semantics.
- `CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` — split busy flags and combined can-run
  predicates; `ApplyProbeResultAsync` loses its availability branch; new `CheckUpdatesAsync` with
  invalidate+check per installed card and the summary text; `Load()` clears the summary.
- `CLIHub/Views/SettingsAgentsPage.xaml` — two header buttons in a horizontal `SpacedPanel` plus
  the muted summary line; per-card action renamed to Detect and gated on the header busy state.
- Core services (`AgentManagementService`, `AgentUpdateAvailabilityService`,
  `AgentVersionService`) — **no changes**.

Docs: `docs/backlog.md` — the two deferred entries that name the "Test" action / `TestAsync`
reworded to the new action vocabulary (behavior unchanged); `docs/images/settings-agents.png`
re-captured during the smoke stage (the AGENTS header changes visually).

Tests: `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs` — the
`DetectCommand_*` and `TestCommand_*` tests that assert the old combined behavior (notably
`DetectCommand_WhileProbing_ShowsDetectingThenSettlesEachCard` asserting `CheckAsync(a)` `Times.Exactly(2)`
at lines 852-853) are updated to the new contract and new coverage is added for the forced check,
busy exclusivity, and the summary line — updated, never weakened (design Decision 9).

Behavioral impact beyond the split: pressing header `Detect` no longer refreshes update chips (they
keep their last value until a `Check updates` or page reload); `Check updates` performs one source
request per installed agent on demand; the per-card first button is labeled Detect.

## Non-goals

- No change to the automatic load-time population (`PopulateVersionsAsync` keeps probing version
  and availability for installed cards).
- No change to `agent-version`'s caching/TTL behavior.
- No plugin descriptor changes and no new detection rules.
- No new persisted preference.
- No change to the per-card Update action or the running-agent block
  (`PopulateBlockedState`/`AgentProcessMonitor` stay as they are).
- No change to `AgentUpdateAvailabilityService` or `AgentVersionService` internals.
- No batch/progress UI for the forced check beyond the summary line and the busy state.

## Dependencies

- None on other active changes: `openspec/changes/` holds no other active change (working tree
  clean at `9d808b1`).
- Builds on the state model introduced by the archived `2026-10-26-agent-install-state` change
  (`AgentInstallState`, nullable `Version`, gated availability), which is now the durable spec
  baseline in `openspec/specs/agent-ui`, `agent-version`, `agent-update-detection`, and
  `agent-availability-display`.
