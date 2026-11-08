# Proposal: provenance-mark-uniform-tone

## Why

The `ProjectProvenanceMark` style applies `TextPrimaryBrush` (bright primary foreground) to the "M"
mark when `CanRemove` is `True`, and `TextMutedBrush` (muted foreground) to "D" when `CanRemove` is
`False`. This asymmetry was introduced by the archived change
`openspec/changes/archive/2026-10-31-provenance-mark-contrast/` as a fix for an accessibility
contrast failure: the earlier `AccentSelectionBrush` used for manual projects was nearly invisible on
light surfaces. The fix overcorrected: `TextPrimaryBrush` gives the "M" mark the same visual weight
as the project name (`TextPrimaryBrush` at `FontWeight=Medium`), drawing more attention than the mark
deserves. The "D" mark, rendered in `TextMutedBrush`, is visually consistent with the mark's role as
a secondary indicator. The user's intent is that both marks share the same muted tone so neither
competes with primary content; the letter ("M" vs "D") remains the only distinction.

The preceding change confirmed that `TextMutedBrush` is readable in all three themes:
- Light: `#FF5F5F66` — readable on light surfaces
- Dark: `#FF9A9A9A` — readable on dark gray
- Blue: `#FF9BA4B5` — readable on blue

There is therefore no contrast regression in dropping the `TextPrimaryBrush` emphasis.

## What changes

### Visual change — uniform tone (implemented)

Remove the `Style.Triggers` / `DataTrigger` block from the `ProjectProvenanceMark` style in
`src/CLIHub/Themes/Common/Controls.xaml` (lines 71–75). After the change the style has a single
setter: `Foreground = {DynamicResource TextMutedBrush}` for all marks regardless of `CanRemove`.

Update the comment on lines 62–65 of the same file to reflect the new single-tone rule.

### Alignment fix — coaxial markers

Smoke testing revealed that the "D"/"M" provenance mark (path line, Rail Column 1, Row 1) is
slightly offset to the right of the favorite star (name line, Rail Column 1, Row 0) despite both
having `HorizontalAlignment="Right"`. The root cause is glyph-width asymmetry: right-alignment
places the right edges of the advance boxes at the same position, but the ★ glyph (13 pt) and the
M/D glyphs (11 pt SemiBold) have different advance widths, so their horizontal centers differ.

Fix: change `HorizontalAlignment="Right"` to `HorizontalAlignment="Center"` on both `TextBlock`
elements in the rail (star at `LaunchWindow.xaml:245`; mark at `LaunchWindow.xaml:254`). Centering
each glyph within the same fixed 16 DIP rail column aligns their horizontal midpoints regardless of
advance width.

Update the rail comment at `LaunchWindow.xaml:199–202` to describe the new centering rule.

This intentionally supersedes the flush-right alignment chosen by the archived change
`2026-11-01-project-row-trailing-inset`, which selected `HorizontalAlignment="Right"` for exact
12 DIP trailing-inset symmetry. The user has explicitly requested coaxial markers over trailing-inset
symmetry. The fixed-width rail and the no-jitter guarantee are unchanged; only the intra-rail
horizontal alignment changes. See the trade-off in the Design document.

### Documentation change

Update `docs/architecture/ui.md` lines 114–121 to replace the description of the right-alignment
rule and 12 DIP symmetry claim with the new centering rule, and re-wrap the paragraph to ≤100
columns. (Lines 119–121 were already updated by the uniform-tone task; lines 114–118 still describe
right-alignment.)

### CHANGELOG entry

The CHANGELOG bullet added under task 3.1 already covers the provenance mark's uniform tone. The
alignment fix is a follow-up correction to the same smoke-test session; it does not warrant a
separate entry. The existing bullet is left as is.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. The provenance mark's binding shape, tooltip/accessible name, character, and shared style
key are unchanged. The color change has no behavioral capability impact; the alignment fix changes
only intra-rail horizontal placement. No durable behavioral contract in `openspec/specs/` is
affected: the relevant scenarios do not specify horizontal alignment within the rail or trailing
inset symmetry. Therefore `skip_specs: true` continues to hold.

This intentionally supersedes the `TextPrimaryBrush` emphasis introduced by
`2026-10-31-provenance-mark-contrast`. That change upgraded from an invisible `AccentSelectionBrush`
to the readable `TextPrimaryBrush`; this change steps back to the originally intended uniform muted
tone now that readability in `TextMutedBrush` is confirmed across all three palettes.

## Impact

**Modified files:**
- `src/CLIHub/Themes/Common/Controls.xaml` — remove `Style.Triggers` block and update comment *(done)*
- `src/CLIHub/Views/LaunchWindow.xaml` — change `HorizontalAlignment` from `Right` to `Center` on
  both rail marker `TextBlock`s (lines 245 and 254); update the rail comment (lines 199–202)
- `docs/architecture/ui.md` — update the rail alignment description and re-wrap paragraph
- `CHANGELOG.md` — one line under `### Changed` *(done)*

**No breaking changes.** The style key `ProjectProvenanceMark` is unchanged. Both consumers
(`LaunchWindow.xaml:252–259`, `SettingsProjectRegistryPage.xaml:135–139`) are unchanged. The
`CanRemove` binding on the `TextBlock` element is no longer relevant to rendering (the trigger that
consumed it is gone), but the binding itself is declared in the style and does not affect other
properties, so removing the trigger silently stops using it without an error.

## Non-goals

- Changing the mark characters, data binding shape, or font metrics
- Adding or removing surfaces that display the provenance mark
- Refactoring the style key or merging it with any other style
- Unit-testing XAML style resolution (no build-time gate exists; verification is manual smoke)
- Adjusting the `Sizing.ProjectRow.MarkerRailWidth` token (16 DIP stays; centering works within it)

## Dependencies

None. This change is self-contained and does not depend on any other open change. The in-progress
`agent-detection-page-icons` change touches unrelated files; there is no file overlap.
