# Summary card detail and alert lines use the full card width

## Why

On the Settings → Projects page, the DISCOVERED card's detail line "Managed by Project Scanning"
wraps to two lines ("Managed by Project / Scanning") at the card width of the reported screenshot
(~188 DIP), even though the text fits on one line 24 px further right. The cause is template
geometry, not the string: in the shared `SummaryCard` template
(src/CLIHub/Themes/SettingsStyles.xaml:363-410) the detail and alert `TextBlock`s live in the
column-0 `StackPanel` of a `*`/`Auto` grid, while the 24x24 icon block occupies column 1
(SettingsStyles.xaml:395-409). The detail line's layout slot therefore ends at the icon block's left
edge, so the 24 px under the icon stay permanently empty. The same 24 px loss applies to all nine
summary cards on the Project Scanning, Projects, and CLI Agents pages.

The user reviewed three variants in the approved mockup (`summary-card-layout.html`, sections 2 and
5) and picked variant A: detail and alert lines extend to the card's full content width, text stays
left-aligned.

## What Changes

- Restructure the `SummaryCard` `ControlTemplate` grid in `src/CLIHub/Themes/SettingsStyles.xaml`
  into two `Auto` rows: row 0 keeps the label/value `StackPanel` in column 0 and the icon block in
  column 1; `DetailText` and `AlertText` move into a new `StackPanel` in row 1 with
  `Grid.ColumnSpan="2"`, so their right edge becomes the card's right content edge — the icon
  block's right edge.
- Label and value stay in column 0 and keep reserving the icon column; no text is right-aligned.
- Card height does not change: the detail line already starts below the 24 px icon block because
  row 0 (label + value) is taller than the icon.
- Every existing trigger (`Detail`/`Alert` null and empty collapse, `IsAlertVisible`, the four
  `ValueTone` tones) and every template binding (`ValueToolTip`/`DetailToolTip`/`AlertToolTip`) is
  preserved unchanged; the named elements keep their `x:Name`s, so the triggers keep resolving.
- No new dependency properties and no public API change in `src/CLIHub/Controls/SummaryCard.cs`.
- The nine call sites in `src/CLIHub/Views/SettingsWindow.xaml` (lines 183-249) need no change.
- Spec delta: the `settings-theme` requirement "Persistent page header" gains one scenario stating
  that the detail/alert lines span the card content width while the label and value reserve the
  icon column.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `settings-theme` — MODIFIED requirement "Persistent page header": all eight original scenario
  names preserved verbatim; one new scenario ("Summary detail lines span the card width") added.

## Impact

Source: `src/CLIHub/Themes/SettingsStyles.xaml` only — the template restructure plus one sentence in
the comment above the style (lines 346-351). No other source, test, or documentation file changes.

Tests: none added or changed. Design.md "Testing decision" records why an automated layout assertion
is not warranted: the repo has no control-template test harness (no test applies a template or walks
the visual tree), and a pixel-level assertion would depend on font metrics and DPI. The behavior is
pinned by the spec scenario and a manual smoke task instead.

User-visible impact: long detail and alert lines on all nine summary cards (Project Scanning:
PROJECTS/AGENTS/DAEMON; Projects: PROJECTS/MANUAL/DISCOVERED; CLI Agents: AGENTS/DEFAULT/DETECTION)
gain 24 px of usable width and wrap later or not at all; nothing else moves and no card changes
height.

## Non-goals

- No right alignment of the detail/alert text (mockup variant B — rejected by the user).
- No new dependency properties, styles, or public API surface on `SummaryCard`.
- No changes to the nine `SettingsWindow.xaml` call sites, the label/value placement, the icon block
  size, the card padding, or any other style in the file.
- No automated WPF layout test harness.
- No reflow work on other settings cards; this change is limited to the `SummaryCard` template.

## Dependencies

- None on other active changes: the only other active change, `agent-install-state`, touches
  `agent-*` capabilities and different source files; its uncommitted edits do not include
  `SettingsStyles.xaml` or `SettingsWindow.xaml`, so there is no overlap.
- The approved mockup `summary-card-layout.html` currently sits untracked in the repository root;
  committing it is outside this change (decided at commit time by the user).
