# Agent row inset alignment

## Why

`project-row-trailing-inset` (archived 2026-11-01) made the project row's own leading and trailing
content insets symmetric at 12 DIP each by setting `Sizing.ProjectRow.Padding` to `"12,9,12,9"`
(`src/CLIHub/Themes/Sizing.xaml:92`). That fixed the project row's internal symmetry but did not
touch the agent row, whose padding is still `Sizing.AgentRow.Padding` = `"10,0,10,0"`
(`src/CLIHub/Themes/Sizing.xaml:76`). Both rows sit inside a pane padded uniformly by
`Sizing.Pane.Margin` = `10` (`src/CLIHub/Themes/Sizing.xaml:63`,
`src/CLIHub/Views/LaunchWindow.xaml:123,279`), so the two panes' *rows* are no longer the same
distance from the shared reference points:

- **Divider side**: the project row's content now ends 12 (row padding) + 10 (pane margin) = 22 DIP
  from the pane divider; the agent row's content still starts 10 + 10 = 20 DIP from the divider. A
  2 DIP mismatch.
- **Window-edge side**: the project logo sits 12 + 10 = 22 DIP from the window's left border; the
  agent row's action buttons sit 10 + 10 = 20 DIP from the window's right border. The same 2 DIP
  mismatch, on the outer edges.

Before `project-row-trailing-inset`, both rows used a 10 DIP horizontal padding and both insets were
20 DIP; that change deliberately moved only the project side to 12, leaving this cross-pane gap. The
parked finding is recorded in `docs/backlog.md`, section "UI and theming", entry "Project and agent
rows no longer end symmetrically about the pane divider" (lines 586-598).

## What Changes

- **`Sizing.AgentRow.Padding`** (`src/CLIHub/Themes/Sizing.xaml:76`) changes from `"10,0,10,0"` to
  `"12,0,12,0"` — both horizontal insets become 12 DIP, matching the project row's padding.
- No other token, markup, or behavior changes. `Sizing.AgentRow.TextMargin` (the gap before the
  action buttons) is untouched.

Net effect: both the agents pane's content and the projects pane's content sit 22 DIP from the pane
divider and 22 DIP from the window's outer border, on both sides.

## Impact

- Affected file: `src/CLIHub/Themes/Sizing.xaml` (one `Thickness` value).
- `docs/architecture/ui.md` is reviewed; no sentence there currently claims a specific agent-row
  inset value, so no doc edit is required by this change (confirmed in `design.md`).
- `docs/backlog.md`'s "Project and agent rows..." entry is marked done by the implementer, with the
  corrected target value (`12,0,12,0`, not the backlog's draft suggestion of `10,0,12,0` — see
  Design for why the draft value was wrong).
- No C#, ViewModel, or test changes are expected; confirmed no test asserts `Sizing.AgentRow.Padding`
  or any agent-row pixel metric (see Design, concern 3).
- Visual outcome: the agents pane's logo/text content and its action buttons each move 2 DIP inward
  (toward the row's center) relative to today; the agents pane's own desired width grows by 4 DIP
  (2 per side) to keep every row unclipped, which the content-sized `Auto` pane column and the
  content-sized window already accommodate for any row-content change.

## Non-goals

- No change to `Sizing.ProjectRow.Padding`, `Sizing.Pane.Margin`, footer insets, or any other
  spacing token.
- No change to `Sizing.AgentRow.TextMargin` (the 24 DIP gap before the action buttons) — the spec
  scenario "Gap before the actions" already passes and is unaffected by a change to the row's
  own left/right padding.
- No change to the project row, which is already correct per the prior change.
- No theme brush changes, no C# or ViewModel changes — this is a pure XAML/resource spacing fix.
- No new spec scenarios: `openspec/specs/launch-window-layout/spec.md` does not pin a numeric inset
  value for either row type, so the change stays within the existing contract.

## Dependencies

Builds directly on `project-row-trailing-inset` (archived 2026-11-01,
`openspec/changes/archive/2026-11-01-project-row-trailing-inset/`), which established the 12 DIP
project-row padding this change now matches. No other change depends on this one.
