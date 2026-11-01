# Design: project-row-trailing-inset

## Current state (verified in code at HEAD `94f1849`)

| Concern | Today | File:Line |
|---|---|---|
| Project row padding | `Sizing.ProjectRow.Padding` is `"12,9,10,9"` (left 12, top 9, right 10, bottom 9) | `src/CLIHub/Themes/Sizing.xaml:91` |
| Marker rail width | `Sizing.ProjectRow.MarkerRailWidth` is 30 DIP | `src/CLIHub/Themes/Sizing.xaml:98` |
| Marker rail column | Fixed-width column inside the project row text host, contains favorite star (row 0) and provenance mark (row 1) | `src/CLIHub/Views/LaunchWindow.xaml:199` |
| Favorite star | FontSize 13, Text "★", `HorizontalAlignment="Center"` in the rail | `src/CLIHub/Views/LaunchWindow.xaml:239-246` |
| Provenance mark | Style `ProjectProvenanceMark` (FontSize `Sizing.FontSize.Section` = 11), Text from `SourceMark` ("M"/"D"), `HorizontalAlignment="Center"` in the rail | `src/CLIHub/Views/LaunchWindow.xaml:248-255` |
| Text font | `Sizing.FontFamily.Text` = "Segoe UI Variable Text, Segoe UI" | `src/CLIHub/Themes/Sizing.xaml:21` |
| Row container padding | Applied through `ProjectRowContainer` style at LaunchWindowStyles.xaml:252-278, Border Padding binds to `Sizing.ProjectRow.Padding` at `:264` | `src/CLIHub/Themes/LaunchWindowStyles.xaml:264` |
| ScrollViewer standoff | When vertical scrollbar is visible, PART_ScrollContentPresenter gains 4px right margin (`:375`); this is independent of row padding | `src/CLIHub/Themes/LaunchWindowStyles.xaml:374-376` |
| Path trimming binding | PathDisplay MultiBinding measures `NameText.ActualWidth` (`:230`) — the text column width, excluding the rail | `src/CLIHub/Views/LaunchWindow.xaml:224-235` |

Working tree is clean at HEAD `94f1849`.

## Measured asymmetry

User screenshot at 1.5x DPI scale revealed asymmetric insets (measured from row highlight edge to visible content edge):

- **Leading inset**: 12 DIP — row highlight left edge → project logo left edge
  - Source: `Sizing.ProjectRow.Padding` left value = 12
  - Verification: the logo sits in Grid.Column="0" with no additional margin, so the padding is the only inset

- **Trailing inset**: ~18-19 DIP — star/provenance mark right edge → row highlight right edge
  - Arithmetic: rowPaddingRight(10) + (railWidth(30) / 2) - (starGlyphWidth / 2)
  - The star glyph "★" at FontSize 13 with `Sizing.FontFamily.Text` renders at approximately 12-13 DIP width
  - Centered in a 30 DIP rail: the glyph's right edge sits at rail_left + 15 + (glyph_width / 2) ≈ 21-22 DIP from the rail's left, leaving 8-9 DIP of rail after it
  - Adding the 10 DIP row padding: 10 + 8-9 ≈ 18-19 DIP trailing inset
  - This exceeds the 12 DIP leading inset by ~50%, creating visible asymmetry

## Decision: equalize to 12 DIP with right-aligned markers in a narrower rail

The brief specifies the target: trailing inset becomes 12 DIP, matching the leading inset, while keeping the fixed-width rail to prevent row jitter.

Three paths considered:

1. **Keep 30 DIP centered rail, change only row padding** — set `Sizing.ProjectRow.Padding` right to match the desired inset minus half-rail offset. This cannot reach 12 DIP: to achieve a 12 DIP trailing inset with a 30 DIP centered rail, the row padding would need to be 12 - (30/2 - glyph_half) = 12 - 9 = 3 DIP, but that would create a 3 DIP gap between the rail and the scrollbar standoff trigger, breaking the visual rhythm. Rejected: cannot achieve the target without visual artifacts.

2. **Remove the rail entirely, right-align markers in the text column** — make the text column hold both text and markers with markers at `HorizontalAlignment="Right"`. Simpler XAML, but loses the fixed-width jitter guarantee that the `launch-window-project-provenance` change established: when the favorite star is hidden, the provenance mark would shift left. Rejected: contradicts the decision rationale from the archived change.

3. **Shrink the rail and right-align markers within it** (chosen) — reduce `Sizing.ProjectRow.MarkerRailWidth` to the actual width the markers need (star glyph at FontSize 13 ≈ 12-13 DIP rendered width, plus minimal centering tolerance → 16 DIP rail width), change both markers from `HorizontalAlignment="Center"` to `HorizontalAlignment="Right"`, and set `Sizing.ProjectRow.Padding` right to 12. The markers sit flush at the rail's right edge, and the trailing inset becomes rowPaddingRight(12) + 0 = 12 DIP.

**Chosen:** Option 3. Rationale: achieves the 12 DIP target, preserves the fixed-width rail for jitter prevention, and keeps the markers visually flush at the row's trailing edge with no floating or asymmetry.

## Target state arithmetic

After the change:

- **`Sizing.ProjectRow.Padding`**: `"12,9,12,9"` (right 12, matching left 12)
- **`Sizing.ProjectRow.MarkerRailWidth`**: 16 DIP (accommodates the star glyph at FontSize 13, measured at ~12-13 DIP, with 3-4 DIP slack)
- **Marker alignment**: both star and provenance mark use `HorizontalAlignment="Right"` — they sit flush at the rail's right edge
- **Trailing inset**: rowPaddingRight(12) + marker at rail edge(0) = **12 DIP**, matching the leading inset

The rail shrinks by 14 DIP (from 30 to 16), but remains a fixed-width column, so row jitter is still prevented when the favorite star is hidden.

## Open questions

1. **Exact rail width** — 16 DIP is estimated for the star glyph at FontSize 13 with `Sizing.FontFamily.Text`. The implementation session should measure or test the rendered width and adjust if the glyph clips or has excessive slack. A rail width of 14-18 DIP is acceptable; the key is that the markers right-align and the trailing inset reaches 12 DIP.

2. **Provenance mark width** — the provenance mark uses FontSize 11 (smaller than the star's 13) and Text "M" or "D". If "M" or "D" at FontSize 11 renders wider than the star at FontSize 13 (unlikely but possible with font metrics), the rail width should accommodate the widest marker. Visual smoke during implementation will confirm.

3. **ScrollViewer standoff interaction** — the standoff (LaunchWindowStyles.xaml:374-376) triggers on scrollbar visibility and adds 4px right margin to the scroll content presenter, independent of row padding. The change does not alter the standoff logic, but smoke should confirm that with the scrollbar visible, the rows still end 12 DIP from the pane's edge (the standoff is outside the row's coordinate space, so row padding 12 + standoff 4 ≈ 16 DIP from the pane edge when scrolling). This is existing behavior; the change only equalizes the row's own insets.

## Alternatives considered

- **Asymmetric padding with optical compensation** — adjust the row padding asymmetrically (e.g., left 12, right 8) to make the visible inset appear equal after accounting for the centered markers. Rejected: optical hacks are fragile across DPI scales and font rendering; explicit symmetry (12 left, 12 right, markers flush right) is predictable and maintainable.

- **Dynamic rail width based on marker visibility** — make the rail collapse when the favorite star is hidden. Rejected: the fixed-width rail is the explicit design decision from the `launch-window-project-provenance` change; removing it contradicts that rationale.

- **Increase leading inset to match trailing** — set left padding to 18-19 DIP to match the current trailing inset. Rejected: the brief specifies 12 DIP as the target, and increasing the leading inset would waste horizontal space in the already width-constrained launch window.

## Impact on path trimming

The path's `PathDisplay` MultiBinding (LaunchWindow.xaml:224-235) measures `NameText.ActualWidth` (`:230`), which is the text column's width and excludes the rail. Since the rail shrinks from 30 to 16 DIP, the text column gains 14 DIP of width, and the path has 14 DIP more room before trimming. This is a positive side effect — paths will trim less aggressively — and requires no binding change.

## Spec deltas

None. The `launch-window-layout` spec (openspec/specs/launch-window-layout/spec.md) specifies the row's visual structure (logo, name, path, favorite marker at lines 80-106) but does not pin inset values. The change is a spacing refinement within the existing contract, so `skip_specs: true` is appropriate.
