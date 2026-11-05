# Proposal: Outline the Agent Version Chip

## Why

In Settings -> CLI Agents, each agent row shows up to three chips next to the agent name: a
state/version chip, an origin badge ("Built-in" / plugin name), and an optional update chip. Three
of these are already outlined (transparent background, 1px `TextMutedBrush` or semantic-colored
border): the "Not installed" state, the origin badge, and the update chip. Only the normal
installed-version chip (e.g. "v1.2.3") is still filled — it inherits `VersionChipBorder`'s
`ChipBgBrush` background and `ChipStrokeBrush` border unchanged.

That leaves one chip with a visibly different treatment in a row of otherwise-consistent chips.
The user has approved moving the origin badge's outlined look onto the version chip so all chips in
the row read as the same visual family, with color (muted vs. danger vs. accent) carrying the only
meaningful difference.

## What Changes

- Add one new shared style, `OutlineChipBorder`, to `src/CLIHub/Themes/Common/Controls.xaml`,
  based on `VersionChipBorder` but with `Background="Transparent"` and
  `BorderBrush="{DynamicResource TextMutedBrush}"`.
- `src/CLIHub/Views/SettingsAgentsPage.xaml`:
  - `AgentStateChip` switches its `BasedOn` from `VersionChipBorder` to `OutlineChipBorder`. Its
    existing `InstallState=NotInstalled` danger trigger is unchanged.
  - `AgentOriginChip` switches its `BasedOn` from `VersionChipBorder` to `OutlineChipBorder` and
    drops its now-redundant `Background="Transparent"` / `BorderBrush="{DynamicResource
    TextMutedBrush}"` setters (the base style supplies them). Its `Origin=installed` accent trigger
    is unchanged.
  - `InvalidAgentStateChip` is untouched — it already fully overrides background and border for the
    danger look and does not go through `VersionChipBorder`'s filled defaults in practice.
  - The `*ChipText` styles (`AgentStateChipText`, `AgentOriginChipText`) are untouched: they already
    inherit `TextMutedBrush` foreground from `VersionChipText`, and their danger/accent triggers are
    unaffected by a border-only change.
- No change to `VersionChipBorder`, `VersionChipText`, or any other consumer of
  `VersionChipBorder` (footer version chip, project badges, `SettingCard` badge).

## Non-Goals

- No change to chip content, text bindings, tooltips, or automation names.
- No change to the "Not installed" danger chip, the update-available chip, or the invalid-card
  chip — their look is already correct per the approved goal.
- No change to the footer "1.1.1" version chip (`SettingsWindow.xaml:556`), the `SettingCard` badge
  (`SettingsStyles.xaml:245`), or any other `VersionChipBorder` consumer outside the agents list.
- No change to the launch window's plain-text agent version display (`LaunchWindow.xaml:347-351`) —
  it is not a chip and is out of scope.
- No palette or brush value changes; no new control class; no behavior change.

## Impact

- `src/CLIHub/Themes/Common/Controls.xaml`: +1 new keyed style (`OutlineChipBorder`), placed next to
  `VersionChipBorder`/`VersionChipText`.
- `src/CLIHub/Views/SettingsAgentsPage.xaml`: 2 style edits (`AgentStateChip`, `AgentOriginChip`
  `BasedOn` + setter cleanup). No other file changes.
- Pure presentation change: no ViewModel, binding, converter, or behavior changes. No test
  behavior to update (no automated test currently pins chip background color; confirmed by not
  finding any such assertion in `tests/CLIHub.Tests/`).
- `openspec/specs/`: no delta. See `.openspec.yaml` (`skip_specs: true`) for rationale.

## Dependencies

None. This change is self-contained and can be implemented independently of any other in-flight
change.
