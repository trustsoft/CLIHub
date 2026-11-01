# Project row trailing inset equalization

## Why

The launch window project row displays a logo, name, path, favorite star, and provenance mark. A user screenshot at 1.5x DPI scale revealed asymmetric content insets (measured from the row highlight edge to the visible content edge):

- **Leading inset**: 12 DIP (row highlight left edge → logo left edge), matching `Sizing.ProjectRow.Padding` left value of 12
- **Trailing inset**: ~18-19 DIP (star/provenance mark right edge → row highlight right edge)

Verified at HEAD `94f1849`:

- `src/CLIHub/Themes/Sizing.xaml:91` — `Sizing.ProjectRow.Padding` is `"12,9,10,9"` (right padding 10)
- `src/CLIHub/Themes/Sizing.xaml:98` — `Sizing.ProjectRow.MarkerRailWidth` is 30 DIP
- `src/CLIHub/Views/LaunchWindow.xaml:239-255` — favorite star (FontSize 13, `:243`) and provenance mark both use `HorizontalAlignment="Center"` inside the 30 DIP rail

The trailing inset arithmetic: rowPaddingRight(10) + railWidth/2(15) - halfGlyphWidth(~6 for the 13pt star) ≈ 19 DIP.

This asymmetry is visually jarring — the eye sees the star and provenance mark floating inward from where they belong, creating an unbalanced composition. The leading and trailing insets should match.

## What Changes

- **`Sizing.ProjectRow.Padding`** right value becomes 12 (from 10) → `"12,9,12,9"` — the row padding is now symmetric
- **`Sizing.ProjectRow.MarkerRailWidth`** shrinks from 30 to the actual width the widest marker (the favorite star glyph "★" at FontSize 13 with `Sizing.FontFamily.Text`) needs, plus minimal slack for centering tolerance — measured at ~12-13 DIP rendered width, so a rail width of 16 DIP provides adequate space without leaving excess
- **Marker alignment** changes from `HorizontalAlignment="Center"` to `HorizontalAlignment="Right"` for both the favorite star (`:241`) and the provenance mark (`:250`) — they sit flush at the rail's right edge instead of centered
- **Path trimming MultiBinding** remains unchanged — it already measures `NameText.ActualWidth` (`:230`), which excludes the rail, so the path calculation stays correct

The target trailing inset: rowPaddingRight(12) + markers at rail's right edge(0) = 12 DIP, matching the leading inset.

## Impact

- Affected files:
  - `src/CLIHub/Themes/Sizing.xaml` — update `Sizing.ProjectRow.Padding` and `Sizing.ProjectRow.MarkerRailWidth`
  - `src/CLIHub/Views/LaunchWindow.xaml` — change marker `HorizontalAlignment` from `Center` to `Right`
- The scrollbar standoff behavior in the `PaneList` ScrollViewer template (LaunchWindowStyles.xaml:344-381) is unaffected — the standoff triggers on scrollbar visibility, not row padding
- The path's `PathDisplay` MultiBinding already binds to `NameText.ActualWidth` — no binding change needed
- Visual outcome: the star and provenance mark end exactly 12 DIP from the row highlight's right edge, matching the 12 DIP leading inset from the logo
- The fixed-width rail (decision from the `launch-window-project-provenance` change) remains — it still prevents row jitter when the favorite star is hidden; only its width and marker alignment change

## Non-goals

- No change to agent row padding, pane margins, footer insets, or any other spacing token
- No theme brush changes
- No C# code, ViewModel, or behavior changes — this is a pure XAML spacing refinement
- No new spec scenarios — `openspec/specs/launch-window-layout/spec.md` specifies the row's visual structure (logo, name, path, favorite marker) but does not pin inset values, so the change does not alter any durable contract

## Dependencies

None — the files and tokens already exist as of the `launch-window-project-provenance` change (archived 2026-10-31).
