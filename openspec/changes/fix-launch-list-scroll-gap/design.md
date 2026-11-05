# Design: Fix Launch List Scroll Gap

## Verified Current State

All line numbers verified at HEAD `5ff482e` (version 1.1.1).

### Layout tokens — `src/CLIHub/Themes/Common/Sizing.xaml`

- **Line 68:** `<sys:Double x:Key="Sizing.PaneList.MaxHeight">434</sys:Double>`
- **Line 77:** `<sys:Double x:Key="Sizing.AgentRow.Height">60</sys:Double>`
- **Line 81:** `<Thickness x:Key="Sizing.AgentRow.Padding">12,0,12,0</Thickness>`
- **Line 90:** `<sys:Double x:Key="Sizing.ProjectLogo.Height">42</sys:Double>`
- **Line 97:** `<Thickness x:Key="Sizing.ProjectRow.Padding">12,9,12,9</Thickness>`

### Styles — `src/CLIHub/Themes/Common/LaunchWindowStyles.xaml`

- **Lines 110-136 (`ProjectRowContainer`):** `ListBoxItem` `Margin="0,1"` (line 115) wrapping a
  `ControlTemplate` whose `Border` carries `Padding="{StaticResource Sizing.ProjectRow.Padding}"` (line
  122, 9 DIP top/bottom). Row pitch: logo height 42 + padding 9+9 + margin 1+1 = 62 DIP.
- **Lines 139-167 (`AgentRowContainer`):** `ListBoxItem` `Margin="0,1"` (line 143) wrapping a
  `ControlTemplate` whose root is a `StackPanel` (line 147). Its **first child** is a 1 DIP `Rectangle`
  divider (lines 148-151) whose `Visibility` is bound through `IndexToDividerVisibility`
  (`IndexToDividerVisibilityConverter`, `src/CLIHub/Converters/IndexToDividerVisibilityConverter.cs:21-22`:
  visible when `AlternationIndex > 0`, collapsed for the first row). Its **second child** is the row
  `Border` (lines 152-157, `Padding="{StaticResource Sizing.AgentRow.Padding}"`, 0 DIP top/bottom).
  Because the divider is a `StackPanel` sibling, it occupies real layout height whenever it is visible.
  Row pitch for rows after the first: row height 60 + divider 1 + margin 1+1 = 63 DIP. (The very first
  row, with the divider `Collapsed`, is only 62 DIP, but `StackPanel` lays out every item the same way the
  moment virtualization realizes it — the *pitch* that matters for a long scrolled list is the 63 DIP
  steady-state value, confirmed by the measurement below.)
- **Lines 170-242 (`PaneList`):** `ListBox` style. `MaxHeight="{StaticResource Sizing.PaneList.MaxHeight}"`
  (line 174, 434 DIP). `ScrollViewer.VerticalScrollBarVisibility="Auto"` (line 176).
  `VirtualizingPanel.VirtualizationMode="Standard"` (line 177) — no `VirtualizingPanel.ScrollUnit` setter
  exists, so it defaults to `Item`, and `ScrollViewer.CanContentScroll` is left at its WPF default of
  `True` for a virtualizing items panel. The custom `ControlTemplate` (lines 184-195) replaces the default
  `ListBox` template with a bare `ScrollViewer` + `ItemsPresenter`, explicitly to avoid the default
  template's 1 px border eating into the viewport (comment, lines 178-183). The custom `ScrollViewer`
  template (lines 196-241) puts the vertical `ScrollBar` in its own `Auto`-width grid column so it takes no
  space while hidden.
- **Lines 178-183 (the comment to correct):**
  ```
  Own template: the default ListBox template hardcodes a 1px Border padding, which
  shaves the scroll viewport to 432px, while MaxHeight is tuned to exactly seven 62px
  rows (434px); that 2px deficit makes the item-based ScrollViewer report a scrollable
  overflow and show the scrollbar even though every row fits.
  ```
  This is true for the Projects list (62 DIP rows) but the "tuned to exactly seven 62px rows" framing
  reads as if row-height arithmetic is what keeps the scrolled end flush. It is not: item-based
  (`ScrollUnit="Item"`) scrolling is what breaks the Agents list, and arithmetic happening to work out for
  one row height is not a property anyone should rely on.

### Views — `src/CLIHub/Views/LaunchWindow.xaml`

- **Lines 113-122:** `PanesGrid` has three columns (Projects pane, splitter, Agents pane), all
  `Width="Auto"`, in one `Grid` row — so both panes render at the same height.
- **Lines 159-163:** Projects `ListBox`, `Style="{StaticResource PaneList}"`,
  `ItemContainerStyle="{StaticResource ProjectRowContainer}"`.
- **Lines 315-319:** Agents `ListBox`, `Style="{StaticResource PaneList}"`,
  `ItemContainerStyle="{StaticResource AgentRowContainer}"`, `AlternationCount="100000"` (so
  `AlternationIndex` is always available to the divider's `IndexToDividerVisibility` binding).

### Empirical measurement (reproducing the defect)

Measured on the built app via UI Automation, device pixels at 150% DPI, window 912×806 px (608×537 DIP).
Both `ListBox`es measured 651 px = 434 DIP tall, same top (`PanesGrid` places both panes in one row, as
above). Each list was scrolled with `ScrollPattern.SetScrollPercent(ScrollPattern.NoScroll, 100)` and the
realized `ListItem` bounding rectangles were read back:

| | Projects (18 items, 62 DIP pitch) | Agents (8 items, 63 DIP pitch) |
|---|---|---|
| Last row bottom at 100% scroll | 1094 px | 1012 px |
| Viewport bottom | 1095 px | 1095 px |
| Gap | ~0 (flush) | 83 px = 55 DIP |
| `VerticalViewSize` | 38.89% (7 of 18 items) | 75% (6 of 8 items) |
| Realized scroll offset | — | 2 whole items (188 px); flush would be 1.11 items (105 px) |

**Root cause:** `ScrollViewer.CanContentScroll="True"` with the default `VirtualizingPanel.ScrollUnit` of
`Item` means the vertical offset is expressed in whole-item units, so the end of the scroll lands on an
item boundary instead of on the exact flush position. For the Agents list: 8 items × 63 DIP = 504 DIP of
content in a 434 DIP viewport, so the flush offset is 70 DIP (1.11 items); the item-boundary offset the
list can actually reach near the end is 2 items = 126 DIP, which is 56 DIP past the flush position. The
content therefore ends at 504 − 126 = 378 DIP from the viewport top and the remaining 434 − 378 = 56 DIP
of the viewport is blank — the 55 DIP measured above (within rounding). The Projects list has no such
remainder because 434 / 62 = 7 exactly, so item quantization and the flush position coincide there; that
coincidence is the only reason one pane looks correct and the other does not. The `VerticalViewSize`
figures corroborate the model: each list reports an integer number of whole rows (7 of 18 projects, 6 of
8 agents), not the true fractional viewport.

**Second, latent defect from the same code:** seven agent rows at the 63 DIP pitch total 441 DIP, which
is greater than the 434 DIP `MaxHeight`, so the Agents list acquires a scrollbar (and a few DIP of
scrollable overflow) at exactly seven agents, while the Projects list at seven projects (7*62 = 434 DIP)
needs none. The two lists diverge in scrollbar behavior at the same row count for no content reason — a
accidental consequence of the divider's layout height, not of any difference in available space.

### Test coverage

No test in `tests/CLIHub.Tests/`, `tests/CLIHub.Core.Tests/`, or `tests/CLIHub.Core.Windows.Tests/`
instantiates `LaunchWindow`, loads its XAML, or reads any layout/scroll dimension. A repo-wide search for
`UIAutomation`, `FlaUI`, and `ScrollPattern` across `*.cs` returns no matches: there is no existing UI
Automation test harness, host executable, or test project configured to drive the built app. The
verification task for this change therefore has to both set up a minimal UI Automation smoke check against
the built `CLIHub.exe` and record the result, since no reusable harness exists to extend.

## Decisions

### 1. Switch `PaneList` to pixel-unit scrolling

`VirtualizingPanel.ScrollUnit="Pixel"` is added to the `PaneList` style
(`LaunchWindowStyles.xaml:170-183`), alongside the existing `VirtualizingPanel.VirtualizationMode="Standard"`.
`ScrollViewer.CanContentScroll` stays `True` (its current effective value — no setter overrides it today),
which keeps `UI virtualization` (`VirtualizingPanel.IsVirtualizing`, inherited true for a virtualizing
panel) turned on; `ScrollUnit="Pixel"` only changes the unit the offset is expressed and quantized in, not
whether virtualization happens.

**Rationale:** with `ScrollUnit="Pixel"`, the maximum vertical offset is
`contentHeight - viewportHeight` in DIP, with no item-boundary rounding. "The end of the scroll fills the
viewport" becomes a structural property of any list under this style, independent of whether its row pitch
happens to divide `Sizing.PaneList.MaxHeight`. This removes the latent bug class entirely rather than
re-tuning row heights to fit this one `MaxHeight` value, which is the approach that already failed once
(the Projects list "working" is why the Agents list's divergence went unnoticed).

### 2. Draw the agent row divider as a zero-height overlay

`AgentRowContainer`'s `ControlTemplate` (`LaunchWindowStyles.xaml:146-158`) stops using a `StackPanel` with
the divider as a layout sibling. Instead the divider `Rectangle` is drawn as an overlay on top of the row
content — for example, both the existing `Border` (named `Bd`) and the divider `Rectangle` become children
of a `Grid` (replacing the `StackPanel` as the template's root), with the `Rectangle` given
`VerticalAlignment="Top"` so it paints along the row's top edge without occupying its own row in a layout
container. The divider keeps its existing properties: `Height="1"`, `Fill="{DynamicResource
RowDividerBrush}"`, `Margin="12,0,12,0"`, and the same `IndexToDividerVisibility` binding so it is still
collapsed for the first row of the list.

**Rationale:** a `Grid` with overlapping children assigns the divider zero layout height (it paints inside
the row's bounds without adding to `DesiredSize`), so the agent row's pitch becomes 60 (row) + 1+1
(`ListBoxItem` margin, unchanged) = 62 DIP — identical to the project row pitch. Seven agent rows then
total exactly 434 DIP, matching seven project rows, so the two lists share one row rhythm for both the
flush-scroll-end property and the "a full pane of rows needs no scrollbar" property. The divider's visual
result (a 1 DIP hairline between rows, 12 DIP inset, never above the first row) is unchanged; only its
layout participation changes.

### 3. Correct the `PaneList` comment

The comment at `LaunchWindowStyles.xaml:178-183` is rewritten to describe only what remains true after
decisions 1-2: the custom template avoids the default `ListBox` template's 1 px border eating into the
viewport, and `MaxHeight` is still chosen as seven rows at the shared 62 DIP pitch (434 DIP) — but the
"exactly tuned so offsets land flush" framing is removed, since that is now `ScrollUnit="Pixel"`'s job,
not row-height arithmetic's.

**Rationale:** leaving the old comment in place after this change would misattribute the fix and mislead
the next reader into thinking row-height tuning (not `ScrollUnit`) keeps scrolling flush, which is exactly
the misunderstanding that let the Agents list regress unnoticed.

## Alternatives Rejected

### Keep item-based scrolling and retune the agent row height to divide 434 evenly

For example, dropping `Sizing.AgentRow.Height` so the 63 DIP pitch becomes a divisor of 434 (nearest
options: 62 DIP needs removing the divider's height anyway, or some other value that still leaves a
remainder at a different agent count). Rejected: this only moves the row count at which the defect
reappears (any list length other than an exact multiple of the pane height still overshoots at the
scrolled end under item-based scrolling) and does nothing for a user with, say, 11 agents instead of a
multiple of 7. `ScrollUnit="Pixel"` fixes the defect for every item count, not just the one currently
visible in testing.

### Keep the divider as a layout element and change `Sizing.PaneList.MaxHeight` to a multiple of 63

Rejected per the non-goals: the window's maximum list height (434 DIP, tuned to the project row pitch) is
explicitly out of scope, and changing it to fit the agent pitch would make the two lists' maximum visible
row counts diverge (434/62 = 7 project rows vs. 434/63 ≈ 6.9 agent rows at a retuned height), which is the
opposite of the one-row-rhythm goal.

### Set `ScrollViewer.CanContentScroll="False"` instead of `ScrollUnit="Pixel"`

Rejected: `CanContentScroll="False"` disables UI virtualization for the panel entirely
(`VirtualizingPanel.IsVirtualizing` requires `CanContentScroll="True"` to have an effect), which would
make every row in a long list realize eagerly. `ScrollUnit="Pixel"` with `CanContentScroll="True"` keeps
virtualization on (WPF supports pixel-based scrolling with virtualization since .NET Framework 4.5) and is
the narrower, non-regressive change.

## Deviations

None from the decisions handed down in the brief. The only elaboration beyond the brief is the specific
overlay mechanism for decision 2 (a `Grid` replacing the `StackPanel` as the template root, with the
divider `VerticalAlignment="Top"`), since the brief specified the *outcome* ("drawn as an overlay... adds
no height") but not the exact WPF construct; `Grid` with overlapping children is the standard,
dependency-free way to achieve a zero-height overlay in this template and needs no new converter, adorner,
or attached behavior.

## Open Questions

1. The exact XAML construct for the divider overlay (which element becomes the `Grid`'s root child order,
   whether the divider needs `Panel.ZIndex` to paint above or below the `Border`'s hover/selection
   background) is left to the implementation session to finalize while reading the live `AgentRowContainer`
   template, since the divider must not be visually covered by the row `Border`'s `HoverOverlayBrush` on
   `IsMouseOver` (`LaunchWindowStyles.xaml:160-162`) — today the divider is a `StackPanel` sibling, entirely
   outside the `Border`, so the current visual order already keeps the divider separate from the hover
   fill; the overlay form needs to preserve that (verify during implementation that the hover highlight
   does not visually paint over the divider, or accept that it does and keep the divider beneath it if that
   matches current pixels — check against a screenshot before and after).
2. Whether `VirtualizingPanel.ScrollUnit="Pixel"` changes the mouse-wheel scroll increment's felt speed
   (WPF's default per-wheel-click delta for pixel scrolling is a fixed DIP amount rather than "one item"):
   this is a user-visible feel change not called out in the brief's non-goals list explicitly, but it
   follows unavoidably from decision 1. Flag it in the manual smoke task so the user can confirm the wheel
   feel is still acceptable; no code decision is needed unless the user asks for a specific increment.
