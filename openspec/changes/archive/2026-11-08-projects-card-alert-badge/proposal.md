# Projects card: danger alert replaced with compact chip and icon count badge (E1)

## Why

The PROJECTS summary card on the Settings → Projects page currently grows to a fourth line when a
project folder is missing: a full-width danger-toned line `1 folder missing` / `{n} folders missing`
(rendered via `src/CLIHub/Controls/SummaryCard.cs:89-102`,
`src/CLIHub/Themes/Common/SettingsStyles.xaml:409-415`, bound at
`src/CLIHub/Views/SettingsWindow.xaml:190-194`). Because the three cards sit side by side, that
fourth line pushes every card taller even when only one card carries it, creating a height
inconsistency that is visible every time a folder is missing.

The card width is ~190 DIP (content area ≈ 582 DIP split across three cards with 8 DIP gaps, so
`(582 − 16) / 3 ≈ 189 DIP` plus the 9 DIP padding on each side). At that width the value row is
21 DIP tall and a compact chip `1 missing` fits comfortably alongside the numeric value without
growing the row.

The user reviewed the mockup `mockup-projects-summary-card.html` (untracked at the repo root) and
approved variant E1 — the combined chip-and-badge layout — which moves the danger indicator out of
the fourth alert line and into two always-visible slots: a compact danger chip after the numeric
value, and a count badge overlaid on the icon block. Both collapse when there are no missing folders,
and the full description moves into a shared tooltip text. Card height stays 75 DIP in both states.

## What Changes

### `src/CLIHub/Controls/SummaryCard.cs`

Replace the three alert dependency properties (`Alert`, `AlertToolTip`, `IsAlertVisible`) with four
new badge-oriented properties:

| New property | Type | Purpose |
|---|---|---|
| `BadgeText` | `string?` | Compact chip label, e.g. `"1 missing"`. Chip hidden when null/empty. |
| `BadgeTooltip` | `string?` | Tooltip for both the chip and the icon count badge. |
| `IsBadgeVisible` | `bool` | Master visibility flag; when false both indicators are hidden. |
| `BadgeCount` | `string?` | Count label for the icon block overlay, e.g. `"1"`. |

The `SummaryCardTone` enum and all other DPs (`Icon`, `Label`, `Value`, `ValueTone`, `ValueToolTip`,
`Detail`, `DetailToolTip`) are unchanged. The class-level XML doc is updated to reflect the new
surface.

### `src/CLIHub/Themes/Common/SettingsStyles.xaml`

Modify the `SummaryCard` `ControlTemplate`:

1. Remove `AlertText` (the danger `TextBlock` in row 1 of the grid).
2. Add a `WrapPanel` alongside the existing `ValueText` in row 0, column 0, with a fixed
   `Margin="8,0,0,0"` left gap on the chip:
   - A danger chip `Border` + `TextBlock` bound to `BadgeText`, styled with `DangerBrush` border
     and foreground (outline style, transparent fill), `CornerRadius` from `Sizing.CornerRadius.Chip`,
     font size 11, `Padding="5,1"`, `VerticalAlignment="Center"`, `Visibility="Collapsed"` by default.
   - The chip's `ToolTip` is bound to `BadgeTooltip`.
3. Make the icon block a `Grid` (relative-positioned in CSS terms; in XAML: wrap the icon `Border`
   and a count `Border` in a `Grid` using negative `Margin` on the overlay, or use a containing
   `Canvas`-style arrangement). The count overlay is a 15×15 DIP (min-width 15) `Border` with
   `CornerRadius` 6 (`Sizing.CornerRadius.Chip`), `Background="{DynamicResource DangerBrush}"`, `BorderBrush=` card background,
   `BorderThickness="2"`, positioned at the top-right of the 24×24 icon block at approx. `−5/−6`
   DIP offset, containing a `TextBlock` bound to `BadgeCount`, `Foreground` dark.
   - The count border's `ToolTip` is bound to `BadgeTooltip`.
   - Both are `Visibility="Collapsed"` by default.
4. `ControlTemplate.Triggers`: add a `MultiTrigger` (or a single `Trigger` on `IsBadgeVisible=True`
   combined with null/empty checks on `BadgeText` / `BadgeCount`) that makes the chip and count
   overlay visible when `IsBadgeVisible=True` and the corresponding text is non-null/non-empty.
   Remove the three existing alert triggers.

### `src/CLIHub/Views/SettingsWindow.xaml`

The single consumer of the alert properties (`RegistrySummaryCards`, lines 188-208) is updated:

- Remove `Alert`, `AlertToolTip`, `IsAlertVisible` bindings on the PROJECTS card.
- Add `BadgeText="{Binding ProjectRegistry.MissingFolderChip}"`,
  `BadgeTooltip="{Binding ProjectRegistry.BadgeTooltipText}"`,
  `BadgeCount="{Binding ProjectRegistry.MissingFolderCount, StringFormat=\{0\}}"`,
  `IsBadgeVisible="{Binding ProjectRegistry.HasMissingFolders}"`.

### `src/CLIHub/ViewModels/Projects/IProjectRegistrySection.cs` and `ProjectRegistryViewModel.cs`

Add two new properties:

- `MissingFolderChip` — compact chip text: `"1 missing"` / `"{n} missing"` (empty string when 0).
- `BadgeTooltipText` — combined tooltip: `"{FolderStatusSummary} · folders that no longer exist"` when
  `HasMissingFolders`; empty string otherwise.

The existing `FolderStatusSummary`, `HasMissingFolders`, and `MissingFolderCount` properties are
preserved unchanged. `RaiseSummaryChanges` gains notifications for the two new properties.

### `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs`

Add tests asserting:

- `MissingFolderChip` returns `"1 missing"` for one missing folder.
- `MissingFolderChip` returns `"2 missing"` for two missing folders.
- `MissingFolderChip` returns `string.Empty` for zero missing folders.
- `BadgeTooltipText` returns `"1 folder missing · folders that no longer exist"` for one.
- `BadgeTooltipText` returns `string.Empty` for zero missing folders.

### Accessibility

The PROJECTS card is `Focusable=False` (template line 354), so a hover-only tooltip is not
sufficient. The chip's inner `TextBlock` (which has an automation peer) carries
`AutomationProperties.HelpText="{TemplateBinding BadgeTooltip}"`. When the chip is visible, screen
readers can read both the chip label (`"1 missing"`) and the full description from the help-text
property without the card needing a focus stop. No `AutomationLabel` VM property is added; the
accessible status comes entirely from the chip element in the template.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `settings-theme` — MODIFIED requirement "Projects page summary cards": scenario "Missing folders
  reported" revised to describe chip + icon badge rather than a separate alert line; all other
  scenario names preserved verbatim; one new scenario added: "Indicators collapse when folders are
  healthy".

## Impact

- `src/CLIHub/Controls/SummaryCard.cs` — three DPs removed, four added; XML doc updated.
- `src/CLIHub/Themes/Common/SettingsStyles.xaml` — template updated: `AlertText` removed, `WrapPanel`
  chip and count overlay added, chip `TextBlock` carries `AutomationProperties.HelpText`, triggers revised.
- `src/CLIHub/Views/SettingsWindow.xaml` — one call site updated (PROJECTS card in `RegistrySummaryCards`).
- `src/CLIHub/ViewModels/Projects/IProjectRegistrySection.cs` — two new property declarations
  (`MissingFolderChip`, `BadgeTooltipText`); `MissingFolderCount` promoted to the interface;
  `FolderStatusSummary` XML doc updated.
- `src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs` — two new properties + `RaiseSummaryChanges`.
- `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs` - five new test methods.
- `tests/CLIHub.Tests/Controls/SummaryCardTests.cs` - the default-visibility test renamed and retargeted
  from the removed `IsAlertVisible` to `IsBadgeVisible`; required for the test project to compile.
- `openspec/specs/settings-theme/spec.md` — MODIFIED "Projects page summary cards" requirement
  (scenario name "Missing folders reported" kept; body updated; one scenario added).

Other consumers of `SummaryCard` (MANUAL, DISCOVERED, AGENTS/DEFAULT/DETECTION/DAEMON/CACHE TTL/
TIMEOUT/DEFAULTS cards) are unaffected; they never set `Alert`/`AlertToolTip`/`IsAlertVisible` and
will not set the new badge properties either.

## Non-goals

- No change to `FolderStatusSummary`, `HasMissingFolders`, `MissingFolderCount`, or any test that
  asserts those properties.
- No change to the row-level `Folder missing` chip in `SettingsProjectRegistryPage.xaml`.
- No change to the card layout geometry (padding, icon block size, grid structure) beyond what the
  chip and badge overlay require.
- No `SummaryCard` usage on any page other than the PROJECTS card on the Projects page uses badge
  indicators; all other cards stay unchanged at their call sites.
- No automated WPF visual-tree test harness.
- No change to the other two cards on the Projects page (MANUAL, DISCOVERED) or to the other pages'
  summary cards.

## Dependencies

None. No other open change touches `SummaryCard.cs`, `SettingsStyles.xaml`, `SettingsWindow.xaml`
(badge area), `IProjectRegistrySection.cs`, `ProjectRegistryViewModel.cs`, or the `settings-theme`
spec.
