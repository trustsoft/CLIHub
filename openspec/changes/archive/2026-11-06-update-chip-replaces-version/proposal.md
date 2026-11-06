# Proposal: Update Chip Replaces the Version Chip

## Why

In Settings -> CLI Agents, an installed agent's card row currently renders, in order: name, the
version/state chip (`AgentStateChip`, e.g. `v0.26.4`), the origin badge (`AgentOriginChip`, e.g.
`Built-in`), and, when an update is available, the advisory update chip (`AgentUpdateChip`, e.g.
`0.26.4 → 0.26.8`). When an update is available, the row shows both the version chip and the update
chip side by side: `Owen Code [v0.26.4] [Built-in] [0.26.4 → 0.26.8]`. The update chip already
repeats the installed version as its left-hand half, so the version chip becomes redundant exactly
when the update chip is showing, and the row grows by a whole chip's width at the point where the
user most wants to glance at it.

The user has approved replacing the version chip with the update chip in the same slot whenever an
update is available, so the row reads `Owen Code [0.26.4 → 0.26.8] [Built-in]` instead — one chip
position, not two, carries version information at a time.

## What Changes

- `src/CLIHub/Views/SettingsAgentsPage.xaml` (installed-card row only):
  - Reorder the row so the `AgentUpdateChip` `Border` renders before the `AgentOriginChip` `Border`
    (moving it out of its current third position into the version chip's former second position,
    immediately after the name).
  - Add a `DataTrigger` to the `AgentStateChip` style (`Binding HasUpdateAvailable, Value=True` ->
    `Visibility=Collapsed`) so the version/state chip is hidden whenever the update chip is shown.
- No ViewModel, converter, binding, tooltip, automation-name, color, or spacing change. No change to
  `InvalidAgentStateChip` or `AgentUpdateChip` styling. No new control class.
- `openspec/specs/agent-ui/spec.md`: update the "Agent card displayed content" requirement so the
  update indicator is stated as replacing the state chip in its slot, not as an addition next to it;
  add one new scenario covering the chip's return when the update clears.

## Non-Goals

- No change to the disabled-card or invalid-card templates — neither renders an update chip today,
  and this change does not add one.
- No change to the update chip's accent styling, tooltip ("Newer version available"), or automation
  name ("Update available").
- No change to `AgentStateChip`'s existing `InstallState=NotInstalled` danger trigger or to
  `InvalidAgentStateChip`.
- No new boolean-to-visibility converter; the existing `BoolToVisibility` converter on the update
  chip and a `DataTrigger` on the state chip are sufficient.
- No change to `AgentCardItem` (`HasUpdateAvailable`, `UpdateAvailabilityText`, `VersionChipText`) or
  any other ViewModel.
- No test changes: no existing test asserts XAML visibility or chip ordering; the VM-level
  assertions this change relies on (`HasUpdateAvailable`, `UpdateAvailabilityText`) are unchanged.

## Impact

- `src/CLIHub/Views/SettingsAgentsPage.xaml`: 1 reordering edit (move the `AgentUpdateChip` `Border`
  before the `AgentOriginChip` `Border` in the installed-card row, ~lines 251-270) and 1 style edit
  (add a `DataTrigger` to `AgentStateChip`, ~line 13).
- `openspec/specs/agent-ui/spec.md`: 1 requirement text update and 1 scenario THEN-clause update,
  both under "Agent card displayed content"; 1 new scenario added.
- Pure presentation change: no ViewModel, binding, converter, or behavior changes. No automated test
  needs updating; see `design.md` "Current State" for the check performed.

## Dependencies

None. This change is self-contained and can be implemented independently of any other in-flight
change.
