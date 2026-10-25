# Agent install state: explicit Detecting/Installed/NotInstalled, no version sentinels

## Why

On Settings → CLI Agents, an uninstalled agent shows "Not installed" on its card chip but
"OpenClaude (unknown)" in the "Default agent" ComboBox. Two defects compound:

1. **A display string carries parseable state.** `AgentOption.DisplayName`
   (src/CLIHub/ViewModels/Agents/AgentCardItem.cs:19-20) appends `Version` whenever it is not the
   pending placeholder `"…"` and ignores `IsInstalled`. For a missing agent the version probe
   returns null and the view model writes the literal `"unknown"`
   (src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:305,398,421), so the selector renders
   `OpenClaude (unknown)` while the card correctly says the agent is missing. The `"…"`/`"unknown"`
   sentinels (src/CLIHub/Helpers/AgentVersionText.cs:12,17) conflate three meanings in one string:
   still probing, version unavailable, resolved version.

2. **Version work runs for agents that cannot have a version.**
   `AgentManagementViewModel.PopulateVersionsAsync` (src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:300-315)
   probes the version and then the update-availability service for every card, including
   not-installed ones, launching a doomed process per card on every page load. The availability
   service re-probes the installed version itself (src/CLIHub.Core/Agents/AgentUpdateAvailabilityService.cs:114-120),
   and `AgentManagementService.ProbeAsync` (src/CLIHub.Core/Agents/AgentManagementService.cs:56-57)
   also runs the version command unconditionally of the installed result. A network source request
   follows only in the edge case where detection markers are missing but the version command still
   emits a parseable version; the doomed process launch is unconditional.

The same `"unknown"` sentinel is also written into the launch-window agent row
(src/CLIHub/ViewModels/Agents/AgentPaneController.cs:183) and the legacy window
(src/CLIHub/Views/MainWindow.xaml.cs:206), and the pending `"…"` is rendered by the row subtitle
(src/CLIHub/ViewModels/Agents/AgentItem.cs:95-108, src/CLIHub/Views/LaunchWindow.xaml:317).

## What Changes

Decisions were taken with the user; this change implements exactly these:

1. **Explicit install state.** A view-model enum `AgentInstallState { Detecting, Installed, NotInstalled }`
   (defined next to `AgentCardItem`). `AgentCardItem` and `AgentOption` carry it; `IsInstalled`
   remains as a derived convenience (`InstallState == Installed`) so existing XAML and tests keep a
   bool, but no display text is derived from version-string sentinels anymore.
2. **Version is "no value", not sentinel text.** `null`/absent version means unresolved. Remove
   `AgentVersionText.Unknown` and every write of the literal `"unknown"`
   (AgentPaneController.cs:183, MainWindow.xaml.cs:206, AgentManagementViewModel.cs:305,398,421).
   The `"…"` pending sentinel is no longer rendered anywhere. `AgentVersionText` survives as the
   owner of display text only.
3. **Selector text.** Installed + resolved version → `{Name} ({version})`; installed without one →
   `{Name}`; not installed → `{Name} (not installed)` (lowercase, mirroring the runtime selector at
   src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:41 and its tests); detecting → `{Name}`. Only
   `NotInstalled` options are disabled — a Detecting option stays selectable
   (today's DataTrigger binds `IsInstalled`, src/CLIHub/Views/SettingsAgentsPage.xaml:81-84).
4. **Card chip.** Detecting → "Detecting…"; NotInstalled → "Not installed" (danger styling kept);
   Installed + version → `v{version}`; Installed without → "Installed". "Detecting…" uses the chip's
   default neutral look.
5. **Honest Detecting.** `Load()` settles state synchronously from the cached detection check (no
   fake Detecting at load). `DetectAsync` sets every card to Detecting for the pass; `TestAsync`
   sets its card. A thrown probe restores the previous state instead of leaving a card stuck.
6. **Gate version work by state.** A NotInstalled card gets no version probe and no
   update-availability check in `PopulateVersionsAsync`, and its `UpdateAvailability` is cleared so
   a stale "update available" chip cannot survive an uninstall; the same gate applies in
   `DetectAsync`/`TestAsync`. `AgentManagementService.ProbeAsync` returns
   `new AgentProbeResult(false, null)` without running the version command when detection says not
   installed. `AgentUpdateAvailabilityService` itself is NOT changed — the call-site gate plus its
   existing degrade-to-Unknown path is the design.
7. **Launch window.** The agent-row version subtitle shows a version only when one was resolved —
   never "unknown", never "…". Both population paths stop writing "unknown"; the subtitle collapses
   when empty (`EmptyToCollapsedVisibility`, App.xaml:45), and the legacy MainWindow binding stops
   rendering an empty "Version: " strip.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `agent-ui` — ADDED requirement "Default agent selector reflects installation state"; MODIFIED
  requirement "Agent card displayed content" (Detecting chip state; update indicator cleared when a
  probe reports not installed; all original scenario names preserved).
- `agent-version` — MODIFIED requirement "Version lookup" (no version command for a not-installed
  agent); MODIFIED requirement "Unknown version display" (no placeholder text for an unresolved
  version; the scenario "Unknown shown" keeps its name with an inverted body — the tooling forbids
  renaming a scenario inside a MODIFIED block; the inversion is flagged in the delta — see design
  Decision 9).
- `agent-update-detection` — ADDED requirement "Update checks are gated on host installation".
- `agent-availability-display` — ADDED requirement "Version subtitle only for resolved versions".

## Impact

Source (all in `src/`):

- `CLIHub/ViewModels/Agents/AgentCardItem.cs` — `AgentInstallState` enum, `InstallState` on
  `AgentCardItem` and `AgentOption`, nullable `Version`, state-derived chip/selector text.
- `CLIHub/Helpers/AgentVersionText.cs` — shrunk to a display-text owner; `IsPending`/`IsKnown` and
  the `"…"`/`"unknown"` sentinels removed.
- `CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` — null version writes, gating, Detecting
  transitions and restore, stale-availability clearing, `TestAsync` message without a version
  suffix.
- `CLIHub/ViewModels/Agents/AgentItem.cs` — `Version` becomes `string?` defaulting to null.
- `CLIHub/ViewModels/Agents/AgentPaneController.cs` — stop writing `"unknown"`.
- `CLIHub/Views/MainWindow.xaml.cs` — stop writing `"unknown"` (legacy window).
- `CLIHub/Views/SettingsAgentsPage.xaml` — chip and ComboBox DataTriggers key on
  `InstallState == NotInstalled` instead of `IsInstalled == False`.
- `CLIHub/Views/LaunchWindow.xaml` — version subtitle collapses when no version is resolved.
- `CLIHub/Views/MainWindow.xaml` — legacy row collapses its "Version: " subtitle when empty.
- `CLIHub.Core/Agents/AgentManagementService.cs` — `ProbeAsync` short-circuits for not-installed
  agents. `AgentUpdateAvailabilityService` and `AgentVersionService` are untouched.

Docs: `docs/backlog.md` — the resolved "Magic version sentinels" entry is rewritten for the new
state model (`docs/architecture/ui.md` needs no edit: its placeholder mentions are the settings
numeric field).

Tests (updated to the new contract, never weakened — see design Decision 8):
`tests/CLIHub.Tests/Helpers/AgentVersionTextTests.cs`,
`tests/CLIHub.Tests/ViewModels/Agents/AgentCardItemTests.cs`,
`tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`,
`tests/CLIHub.Tests/ViewModels/Agents/AgentItemTests.cs` (additions; it asserts no Version behavior
today),
`tests/CLIHub.Tests/ViewModels/Agents/AgentPaneControllerTests.cs` (`"…"` assertion at line 179
becomes a null assertion),
`tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs` (`ProbeAsync_UnknownVersion_ReturnsNullVersion`
at line 237 additionally verifies the version service is never called).

Behavioral impact beyond the fixed symptom: fewer process launches on the CLI Agents page and on
Detect/Test for not-installed agents; transient "Detecting…" chips during a Detect pass; the
"Default agent" ComboBox marks and disables not-installed options.

## Non-goals

- No plugin descriptor or schema changes, and no new detection rules.
- No new persisted preference.
- No per-card progress indicator beyond the "Detecting…" chip.
- No change to how uninstalled agents are hidden from the launch list
  (`AgentListComposer` filter stays as is).
- No change to `AgentUpdateAvailabilityService` or `AgentVersionService` internals.
- No registration or removal of the legacy `MainWindow`.

## Dependencies

- None on other active changes: `openspec/changes/` holds no other active change (working tree
  clean at `bdd9770`).
- Wording follows the runtime-selector precedent established by
  `openspec/specs/agent-version` and `RuntimeOption.DisplayName`
  (src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:41), and the hiding behavior of the archived
  `2026-10-03-hide-uninstalled-agents` change is untouched.
