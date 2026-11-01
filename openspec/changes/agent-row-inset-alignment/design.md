# Design: agent-row-inset-alignment

## Current state (verified in code at HEAD `acda23e`)

| Concern | Today | File:Line |
|---|---|---|
| Agent row padding | `Sizing.AgentRow.Padding` is `"10,0,10,0"` (left 10, top 0, right 10, bottom 0) | `src/CLIHub/Themes/Sizing.xaml:76` |
| Project row padding | `Sizing.ProjectRow.Padding` is `"12,9,12,9"` (left 12, top 9, right 12, bottom 9) | `src/CLIHub/Themes/Sizing.xaml:92` |
| Pane outer margin | `Sizing.Pane.Margin` is `10` (applied as a uniform `Thickness`), same token for both panes | `src/CLIHub/Themes/Sizing.xaml:63` |
| Projects pane uses the margin | `<DockPanel Margin="{StaticResource Sizing.Pane.Margin}">` | `src/CLIHub/Views/LaunchWindow.xaml:123` |
| Agents pane uses the margin | `<DockPanel Margin="{StaticResource Sizing.Pane.Margin}">` | `src/CLIHub/Views/LaunchWindow.xaml:279` |
| Agents pane minimum width | `Sizing.AgentPane.MinWidth` is `260`, with the comment "Smallest usable Agents pane: the agent row's fixed parts plus a little text." | `src/CLIHub/Themes/Sizing.xaml:74` |
| Agents pane column | `<ColumnDefinition Width="Auto" MinWidth="{StaticResource Sizing.AgentPane.MinWidth}"/>` — content-sized (`Auto`), with `MinWidth` only a floor the splitter cannot cross | `src/CLIHub/Views/LaunchWindow.xaml:118` |
| Agent row padding applied | `AgentRowContainer` style, `Border` `Padding="{StaticResource Sizing.AgentRow.Padding}"` | `src/CLIHub/Themes/LaunchWindowStyles.xaml:294-297` |
| Project row padding applied | `ProjectRowContainer` style, `Border` `Padding="{StaticResource Sizing.ProjectRow.Padding}"` | `src/CLIHub/Themes/LaunchWindowStyles.xaml:261-264` |
| Agent row logo | `Sizing.AgentLogo.Width` = `28` | `src/CLIHub/Themes/Sizing.xaml:72` |
| Agent row text margin | `Sizing.AgentRow.TextMargin` = `"14,0,24,0"` (left 14 before the text, right 24 before the action buttons) | `src/CLIHub/Themes/Sizing.xaml:78` |
| Agent row action buttons | Two `AgentActionButton`s, each `Sizing.ActionButton.Width` = `28`; the first has `Margin="0,0,6,0"` (6 DIP gap to the second) | `src/CLIHub/Views/LaunchWindow.xaml:354,365`, `src/CLIHub/Themes/Sizing.xaml:108` |
| Agent row grid structure | `Auto` (logo) / `Auto` (text StackPanel) / `*` (spacer, pushes actions right) / `Auto` (action buttons) | `src/CLIHub/Views/LaunchWindow.xaml:320-327` |
| ScrollViewer standoff | While the vertical scrollbar is visible, `PART_ScrollContentPresenter` gains a 4 DIP right margin; collapses to 0 when the bar is hidden. Triggers on scrollbar visibility, independent of row padding. | `src/CLIHub/Themes/LaunchWindowStyles.xaml:370-377` |

Working tree is clean at HEAD `acda23e` (the previous change `2026-11-01-project-row-trailing-inset`
is archived).

## The defect, in DIP arithmetic

Both panes apply the same `Sizing.Pane.Margin` (10 DIP) as their `DockPanel`'s outer margin, so the
pane margin is already equal on both sides. The mismatch is entirely in the row's own padding:

| Side | Today | Formula |
|---|---|---|
| Projects pane content → divider | 22 DIP | `Sizing.ProjectRow.Padding` right (12) + `Sizing.Pane.Margin` (10) |
| Agents pane content → divider | 20 DIP | `Sizing.AgentRow.Padding` left (10) + `Sizing.Pane.Margin` (10) |
| Projects pane content → window's left border | 22 DIP | `Sizing.ProjectRow.Padding` left (12) + `Sizing.Pane.Margin` (10) |
| Agents pane content → window's right border | 20 DIP | `Sizing.AgentRow.Padding` right (10) + `Sizing.Pane.Margin` (10) |

The user's 1.5x-DPI screenshot (launch window 894×806 px) is consistent with this: divider hairline
at x≈488, agents logo plate ink starts at x≈526 (38 px ≈ 25.3 DIP at 1.5x from the hairline — the
logo itself is not flush with the row edge, it sits after the row's own padding, so the measured gap
includes both the 20 DIP padding+margin and sub-pixel rendering/antialiasing slack at the plate's
visual ink edge rather than its layout box edge); resume-button ring ink ends at x≈851, window right
border at x≈892 (41 px ≈ 27.3 DIP at 1.5x, same caveat). The two measured gaps are close to each
other (≈25 vs ≈27 DIP) but both exceed the computed 20 DIP layout value because the measurement is
from visual ink, not from the row's padding box; the layout-level arithmetic above is what the fix
targets, and it is internally consistent (both sides of the agent row read 20 DIP today, both sides
of the project row read 22 DIP today) regardless of the ink-vs-box offset in the screenshot.

## Decision: set `Sizing.AgentRow.Padding` to `12,0,12,0`

Both horizontal insets become 12 DIP — the same value the project row already uses. After the
change:

| Side | Target | Formula |
|---|---|---|
| Projects pane content → divider | 22 DIP (unchanged) | 12 + 10 |
| Agents pane content → divider | 22 DIP | `Sizing.AgentRow.Padding` left (**12**) + 10 |
| Projects pane content → window's left border | 22 DIP (unchanged) | 12 + 10 |
| Agents pane content → window's right border | 22 DIP | `Sizing.AgentRow.Padding` right (**12**) + 10 |

Rationale:

1. The divider-facing insets become equal at 22 DIP — the reported defect.
2. The window-edge-facing insets become equal too (22/22 DIP) — the previous change left a 22/20 DIP
   mismatch there as an unintended side effect; fixing only the divider side would have left this
   second mismatch in place.
3. Both row types then share one horizontal padding vocabulary (12 DIP each side), so a future
   change to either row's padding cannot reintroduce a cross-pane split without touching both.

### Alternatives rejected

1. **Left-only `12,0,10,0`.** Closes the divider-side 2 DIP gap (agent row left padding 12 + pane
   margin 10 = 22, matching the project row's 22) but leaves the right padding at 10, so the
   window-edge side stays at 20 DIP against the project row's 22 DIP — trading one mismatch for
   another — and leaves the agent row's own left/right padding asymmetric (12 vs 10) for no reason
   tied to its content (unlike the project row, the agent row has no right-aligned marker rail that
   would justify an asymmetric value). Rejected.
2. **Shrink `Sizing.Pane.Margin` for one pane.** `Sizing.Pane.Margin` is one shared token applied
   identically to both panes' `DockPanel.Margin` (`LaunchWindow.xaml:123,279`); giving the Agents
   pane a smaller margin would move every element in that pane (header, list, the pane's padding
   relative to its own border) instead of just the row content, and would violate the spec's
   "Uniform outer margins" scenario (`openspec/specs/launch-window-layout/spec.md:48-50`: "both
   panes use the same outer margin and padding on all sides"). Rejected.
3. **Revert the project row's trailing padding to 10.** Undoes the fix `project-row-trailing-inset`
   just shipped, reintroducing the asymmetric 12/10 row-level inset that change deliberately removed
   (its own rationale: "the leading and trailing insets should match"). Rejected.

### Backlog's draft value was wrong

`docs/backlog.md` (lines 586-598, "Project and agent rows no longer end symmetrically about the pane
divider") proposes `Sizing.AgentRow.Padding` → `"10,0,12,0"` to make "both rows end 22 DIP from the
divider." That proposal swapped which side of the agent row faces the divider: the agent row's
**left** padding faces the divider (the Agents pane sits in `Grid.Column="2"`, to the right of the
divider in `Grid.Column="1"`, `LaunchWindow.xaml:118,267-278`), not the right padding. Setting right
padding to 12 while leaving left at 10 would raise the window-edge inset to 22 DIP while leaving the
divider-side inset at 20 DIP — the opposite of the backlog's own stated goal. The implementer should
mark the backlog entry done with the corrected value `12,0,12,0`, not the draft's `10,0,12,0`.

## Hairline alignment (implementation addition)

`AgentRowContainer`'s inter-row separator (`src/CLIHub/Themes/LaunchWindowStyles.xaml:290-293`) carried
`Margin="10,0,12,0"` — the leading value dated from the row's old 10 DIP padding, and the trailing value
was already 2 DIP short of the project row's 12 DIP trailing inset. With the row's content now inset 12
DIP on both sides, the separator takes the same horizontal inset as the content it separates, so its
`Margin` changes to `12,0,12,0` and the hairline starts and ends exactly at the row content's edges. Only
the margin changed: the `Rectangle`'s height (1), `RowDividerBrush` fill, and its
`AlternationIndex`-driven `IndexToDividerVisibility` binding are untouched. The hairline is a 1 DIP
decoration drawn above the row's `Border` inside the item's `StackPanel`; its own desired width (0 plus
24 DIP of margins) stays below the `Border`'s desired width (24 DIP of padding plus content), so it
never drives the row's measured width and the Agents pane's width effect remains the 4 DIP from the row
padding alone.

## Concern 1 — pane/list/window width cannot shrink

The Agents pane's column is `Width="Auto"` with `MinWidth="{StaticResource Sizing.AgentPane.MinWidth}"`
(`LaunchWindow.xaml:118`): `Auto` means the pane is sized to its content's desired size, and
`MinWidth` (260) is only a floor the splitter cannot cross — it does not drive the pane's size when
the content needs more. Each agent row's fixed-width parts, now including the 2 DIP-larger padding
per side, are:

```
logo                    28   (Sizing.AgentLogo.Width)
+ row padding (L+R)     24   (Sizing.AgentRow.Padding, new: 12+12)  [was 20: 10+10]
+ text margin (L+R)     38   (Sizing.AgentRow.TextMargin: 14+24)
+ action buttons        56   (2 × Sizing.ActionButton.Width = 2×28)
+ inter-button gap       6   (Margin="0,0,6,0" on the first button)
-----------------------------------------------------------------
= fixed parts          152   (was 148 before this change)
```

At the stated 260 DIP floor, that leaves `260 − 152 = 108` DIP for the text column (name + version
text), versus `260 − 148 = 112` DIP before the change — 4 DIP less room for text at the *minimum*
width the splitter allows, not a width the pane is forced into. The pane's actual rendered width is
driven by its widest row's **desired** size (the `Auto` column), so in practice every row (including
the text column at its natural, un-clamped width up to `Sizing.AgentRow.TextMaxWidth`=240) gets 4 DIP
more total width budget, not less — the pane and the window (which is itself content-sized per the
"Window fits its content" scenario, `launch-window-layout/spec.md:232-234`) grow by 4 DIP to
accommodate the wider padding, exactly as they already do for any other row-content size change.
Nothing is clipped: the floor only constrains how far the user can **drag the splitter inward**, and
108 DIP is still ample for a short agent name/version pair; no agent row content requires more than a
small fraction of that at the minimum width in practice, and the change does not introduce a
scenario where the floor is smaller than the fixed parts (152 ≪ 260).

The Projects pane's `MinWidth` (`Sizing.ProjectPane.MinWidth`=320, `Sizing.xaml:88`) and the project
list's `MinWidth` (`Sizing.ProjectList.MinWidth`=300, `Sizing.xaml:82`) are untouched by this change
— only `Sizing.AgentRow.Padding` changes, and nothing in the Projects pane reads that token.

## Concern 2 — spec scenarios still hold

- **"Actions are right aligned"** (`launch-window-layout/spec.md:224-226`): the action buttons sit
  in `Grid.Column="3"` with `Width="Auto"`, pushed right by the `*`-width spacer in `Grid.Column="2"`
  (`LaunchWindow.xaml:320-327,351`). Changing the row's own `Border.Padding` shifts where that Grid
  sits inside the border (2 DIP further from each edge) but does not change the Grid's internal
  column proportions — the spacer column still consumes all leftover space and the action buttons
  still land flush at the Grid's right edge, which is now 2 DIP further from the row's outer edge on
  both panes alike. The buttons remain aligned with each other across rows (every row uses the same
  template and padding). Scenario holds.
- **"Gap before the actions ... at least 24px"** (`launch-window-layout/spec.md:228-230`): sourced
  from `Sizing.AgentRow.TextMargin`'s right value, `"14,0,24,0"` → right = 24
  (`Sizing.xaml:78`, applied at `LaunchWindow.xaml:337`). This token is untouched by this change — it
  is independent of `Sizing.AgentRow.Padding`, a different `Thickness` applied to a different
  element (the text `StackPanel`'s own `Margin`, not the row's outer `Border.Padding`). The gap
  stays exactly 24 DIP. Scenario holds, unaffected.
- **"Uniform outer margins"** (`launch-window-layout/spec.md:48-50`): "both panes use the same outer
  margin and padding on all sides" — this refers to `Sizing.Pane.Margin`, the `DockPanel.Margin`
  applied identically to both panes (`LaunchWindow.xaml:123,279`), which this change does not touch.
  The row-level padding (`Sizing.AgentRow.Padding`/`Sizing.ProjectRow.Padding`) is a separate,
  per-row-type token already set to different values before this change (project row carries an
  asymmetric vertical component, `12,9,12,9`, for its two-line layout; the agent row is single-line,
  `12,0,12,0`) — the spec scenario is about the pane's own outer margin, which stays the shared
  `Sizing.Pane.Margin` token on both panes, unmodified. Scenario holds.

## Concern 3 — no test pins the agent row's padding value

Searched `tests/` for `AgentRow`, `Sizing.AgentRow`, `ProjectRow`, `Sizing.ProjectRow`, and for any
reference to `Sizing.xaml` or `LaunchWindowStyles.xaml` by path: zero matches. No test in
`tests/CLIHub.Tests`, `tests/CLIHub.Core.Tests`, or `tests/CLIHub.Core.Windows.Tests` asserts
`Sizing.AgentRow.Padding`'s value, the agent row's rendered width, or the Agents pane's rendered
width. `tasks.md` therefore needs no test-update task; the build+test gates exist only to catch an
unrelated regression, not to update a pinned assertion.

## Concern 4 — `Sizing.ProjectRow.Padding`'s comment and `docs/architecture/ui.md`

`Sizing.ProjectRow.Padding`'s comment (`Sizing.xaml:89-91`: "Project row padding: the row's leading
and trailing content insets are equal (12px each)...") describes only the project row's own padding
value and does not mention the agent row or any cross-pane relationship; it stays accurate
unchanged.

`docs/architecture/ui.md`'s rail paragraph (lines 109-116) describes the project row's marker rail
and its own symmetric 12 DIP insets; it does not mention the agent row or make any claim this change
would falsify. No other sentence in that file states or implies the agent row's padding value or a
cross-pane equality claim. No doc edit is required by this change; the rail paragraph must be kept
intact (per the brief) if the implementer touches the file for any other reason, but inspection here
did not find a reason to touch it at all.

## Concern 5 — scrollbar standoff is honestly pre-existing, not a regression

`LaunchWindowStyles.xaml:370-377`: while the pane's vertical scrollbar is visible,
`PART_ScrollContentPresenter` (inside the shared `PaneList` ScrollViewer template, used by both the
project list and the agent list) gains a 4 DIP right margin; it collapses to 0 when the bar is
hidden. This triggers on `PART_VerticalScrollBar`'s `Visibility`, which is independent of row
padding — the trigger fires identically for both lists and this change does not alter it.

Consequence: while either list is scrolling (scrollbar visible), that list's *scroll-content
surface* sits 4 DIP further from the pane's right edge than it does when the bar is hidden. Combined
with the row's own padding, a scrolling project list's rightmost row content sits
`12 (row padding) + 4 (standoff) = 16` DIP from the list's clipped edge before the pane margin is
added, and a scrolling agent list reaches the same 16 DIP after this change (12 + 4), where before
this change it was `10 + 4 = 14` DIP. This is the existing, documented behavior of the shared
`PaneList` template — "While the bar is displayed the content keeps a 4px standoff from it... so
both panes' rows end the same [distance] away from the pane's edge" (comment at
`LaunchWindowStyles.xaml:371-373`) — and this change does not modify the standoff trigger or its
value; it only changes which padding value the standoff is added to. The standoff affects the
*scroll surface's* distance from the pane edge, not the row *content's* distance from the row's own
edge, which is what this change equalizes. Stated for completeness, not as a regression: this
behavior was true before this change (with 10 DIP row padding on the agent side and 12 DIP on the
project side, the two lists' scrolling standoffs already differed by 2 DIP) and stays true after it
(now both lists' scrolling standoffs match, as a side effect of the row paddings now matching) —
either way it is a pane-edge-relative effect this change does not target and does not alter the
mechanism of.

## Open questions

1. None block implementation: the change is a single `Thickness` value edit with no ambiguity in the
   target value, no test to update, and no doc sentence invalidated. The implementer should still
   run the manual smoke task to catch any visual surprise (e.g., unexpected clipping at a narrow
   custom window state) that static analysis here cannot rule out with full certainty, per the
   project's standing smoke-before-archive policy.

## Spec deltas

None. `openspec/specs/launch-window-layout/spec.md` does not pin numeric inset values for either row
type (its scenarios discuss alignment, gaps relative to the actions, and uniform outer margins in
qualitative or minimum-bound terms — see Concern 2 above), so this change is a spacing refinement
within the existing contract. `skip_specs: true` is set in `.openspec.yaml`.
