# Design: provenance-mark-uniform-tone

## Context

The `ProjectProvenanceMark` style was introduced by the archived change
`openspec/changes/archive/2026-10-31-provenance-mark-contrast/` to fix a contrast failure in the
Light theme where `AccentSelectionBrush` made the "M" mark nearly invisible. The fix promoted the
style to `Controls.xaml` and applied `TextPrimaryBrush` to manual projects (`CanRemove=True`) via a
`DataTrigger`. This change removes that trigger so both marks render at the same `TextMutedBrush`
muted tone.

## Current state with file:line evidence

### Style definition

`src/CLIHub/Themes/Common/Controls.xaml:62–76`:
```xaml
<!-- Project row provenance mark: the compact "M"/"D" source indicator shared by the Settings
     Projects page and the launch window project list. Manual projects (CanRemove is true exactly
     for them) use the strong primary foreground so the mark stays readable on light surfaces;
     discovered projects keep the muted tone. The character remains the primary distinction. -->
<Style x:Key="ProjectProvenanceMark" TargetType="TextBlock">
    <Setter Property="FontSize" Value="{StaticResource Sizing.FontSize.Section}"/>
    <Setter Property="FontWeight" Value="SemiBold"/>
    <Setter Property="VerticalAlignment" Value="Center"/>
    <Setter Property="Foreground" Value="{DynamicResource TextMutedBrush}"/>
    <Style.Triggers>
        <DataTrigger Binding="{Binding CanRemove}" Value="True">
            <Setter Property="Foreground" Value="{DynamicResource TextPrimaryBrush}"/>
        </DataTrigger>
    </Style.Triggers>
</Style>
```

The `DataTrigger` on `CanRemove=True` (lines 72–74) sets `Foreground` to `TextPrimaryBrush` for
manual projects. This is the sole target of the change.

### Style consumers

Both consumers reference the style key unchanged; no style edits are needed.

`src/CLIHub/Views/LaunchWindow.xaml:252–259` (current, pre-alignment-fix):
```xaml
<TextBlock Grid.Row="1"
           Grid.Column="1"
           HorizontalAlignment="Right"
           VerticalAlignment="Center"
           Style="{StaticResource ProjectProvenanceMark}"
           AutomationProperties.Name="{Binding SourceLabel}"
           Text="{Binding SourceMark}"
           ToolTip="{Binding SourceLabel}"/>
```

`src/CLIHub/Views/SettingsProjectRegistryPage.xaml:135–139`:
```xaml
<TextBlock Margin="8,0,0,0"
           Style="{StaticResource ProjectProvenanceMark}"
           AutomationProperties.Name="{Binding SourceLabel}"
           Text="{Binding SourceMark}"
           ToolTip="{Binding SourceLabel}"/>
```

The Settings registry page uses a `StackPanel`-based layout with `Margin="8,0,0,0"` for the mark;
it does not use the fixed-width rail, so the alignment fix does not apply there.

### Rail structure in the launch window

`src/CLIHub/Views/LaunchWindow.xaml:193–259` (Grid with two columns, two rows):

```
Column 0 (*): name text (Row 0), path text (Row 1)
Column 1 (16 DIP fixed): star ★ (Row 0), provenance mark M/D (Row 1)
```

Rail comment at lines 199–202:
```xaml
<!-- Fixed right rail: the star sits on the name line and the
     provenance mark on the path line, both right-aligned to the
     rail's trailing edge, so the two markers never compete on
     one line and hiding the star shifts nothing. -->
```

Star `TextBlock` (`LaunchWindow.xaml:243–250`):
```xaml
<TextBlock Grid.Row="0"
           Grid.Column="1"
           HorizontalAlignment="Right"
           VerticalAlignment="Center"
           FontSize="13"
           Foreground="{DynamicResource AccentBrush}"
           Text="★"
           Visibility="{Binding IsFavorite, Converter={StaticResource BoolToVisibility}}"/>
```

Provenance mark `TextBlock` (`LaunchWindow.xaml:252–259`):
```xaml
<TextBlock Grid.Row="1"
           Grid.Column="1"
           HorizontalAlignment="Right"
           VerticalAlignment="Center"
           Style="{StaticResource ProjectProvenanceMark}"
           AutomationProperties.Name="{Binding SourceLabel}"
           Text="{Binding SourceMark}"
           ToolTip="{Binding SourceLabel}"/>
```

Rail sizing token at `src/CLIHub/Themes/Common/Sizing.xaml:106`:
```xaml
<GridLength x:Key="Sizing.ProjectRow.MarkerRailWidth">16</GridLength>
```

### Brush values across all three palettes

| Palette file | `TextMutedBrush` | `TextPrimaryBrush` |
|---|---|---|
| `src/CLIHub/Themes/Light.xaml:39` | `#FF5F5F66` | `#FF1B1B1F` |
| `src/CLIHub/Themes/Dark.xaml:39` | `#FF9A9A9A` | `#FFE9E9E9` |
| `src/CLIHub/Themes/Blue.xaml:39` | `#FF9BA4B5` | `#FFE8EBF1` |

The archived change `2026-10-31-provenance-mark-contrast` confirmed `TextMutedBrush` is readable in
all three themes via manual smoke. No contrast regression is introduced.

### Architecture doc — current text

`docs/architecture/ui.md:114–121` (lines 114–118 still describe right-alignment; lines 119–121
were updated by task 2.1):
```
The launch-window project row uses a fixed-width right marker rail inside the text column: the favorite
star occupies the name row and the provenance mark the path row. Both rail markers right-align to the
rail's trailing edge, so the project row's leading and trailing content insets are symmetric (12 DIP
each), and the rail stays fixed-width so hiding the star does not shift the path text. The name and path
share the `*` text column, and the path display converter measures the name text's width so the rail
never eats into the trimmed path. The `ProjectProvenanceMark` style lives in `Controls.xaml` (both marks use `TextMutedBrush`;
the character is the only distinction), so the Settings registry and the launch window render
the same mark; the mark's tooltip and accessible name carry the full `Manual`/`Discovered` label.
```

Lines 114–118 must be updated to describe centering instead of right-alignment, and the 12 DIP
trailing-inset symmetry claim must be removed or qualified.

### Spec — no color or alignment mandate

`openspec/specs/project-management/spec.md:176–182` ("Launch window shows provenance mark"):
The scenario requires the `ProjectProvenanceMark` style, the correct mark character, and the full
label as tooltip and accessible name. It does NOT specify which brush the style uses or how the mark
is horizontally positioned within the rail, so the uniform-tone change requires no spec delta.

`openspec/specs/launch-window-layout/spec.md` — read in full. No scenario specifies horizontal
alignment within the marker rail, the trailing inset value (12 DIP), or the `HorizontalAlignment`
attribute on individual rail elements. The closest scenario is "Favorite marker" (line 104), which
requires the marker to be shown or hidden but says nothing about its horizontal position. Therefore
`skip_specs: true` continues to hold for both the uniform-tone change and the alignment fix.

### CHANGELOG structure

`CHANGELOG.md:11–29` has `## Unreleased` with subsections `### Added`, `### Changed`, `### Fixed`,
`### Removed`. The new entry belongs under `### Changed`.

## Decisions

### Decision 1: Remove the DataTrigger entirely (not replace it)

**Chosen:** Delete the entire `Style.Triggers` block. The style becomes a flat four-setter style
with no trigger.

**Rationale:** The `CanRemove` property is the VM's "can be manually removed" flag. It has no
visual role after this change. Keeping a no-op trigger or replacing it with a same-value setter would
be misleading. Flat is simpler and leaves no dead binding.

**Alternatives considered:**
- Replace trigger setter with `TextMutedBrush`: Rejected — the trigger block itself would still
  exist and suggest that `CanRemove` has visual significance, which it does not after this change.
- Add a mid-tone brush (`TextSecondaryBrush` if it existed): Rejected — no such semantic token
  exists in the palette; `TextMutedBrush` is the correct secondary-content token and is already
  applied for discovered projects.

### Decision 2: skip_specs: true

**Chosen:** `skip_specs: true` in `.openspec.yaml`; no delta file under `specs/`.

**Rationale:** The durable spec scenario mandates the style key, character, and accessible name, not
the brush. This is a style-only visual change with no behavioral capability change — exactly the
same rationale used in the archived `2026-10-31-provenance-mark-contrast` change, which also used
`skip_specs: true` for the same style.

**Alternatives considered:**
- Add a spec note about uniform color: Rejected — the spec is intentionally silent on brush choices
  to remain palette-agnostic. Encoding a specific brush token in a behavior spec would couple spec
  and implementation unnecessarily.

### Decision 3: Update comment in Controls.xaml alongside the trigger removal

**Chosen:** The comment at lines 62–65 explicitly describes the old rule ("Manual projects use the
strong primary foreground..."). It must be updated to avoid misleading the next reader. The comment
update is part of the same edit as the trigger removal; they are one atomic step.

**Rationale:** The comment is documentation for the style. A stale comment describing a removed
mechanism is worse than no comment. The update is two lines of text, zero risk of regression.

### Decision 4: Manual smoke in all three themes for both surfaces

**Chosen:** Verification requires launching the app and observing both the Settings Projects page
and the launch window project list in Light, Dark, and Blue themes. The smoke checks that the "M"
mark renders in the same muted tone as "D" and that neither mark is illegible.

**Rationale:** WPF resource resolution fails at runtime only; `dotnet build` passes with a missing
style key. The only way to confirm that the trigger is gone and `TextMutedBrush` applies uniformly
is to launch the app. Six observations (3 themes × 2 surfaces) cover the full matrix.

## Alignment fix: root cause and design

### Root cause

Both rail markers use `HorizontalAlignment="Right"` inside the same 16 DIP fixed-width `Grid`
column. Right-alignment places the right edge of each element's advance box at the column's right
boundary. The advance box width equals the glyph's advance width (including side bearings), which
differs between the ★ glyph (13 pt, ~13.6 DIP advance) and the M/D glyphs (11 pt SemiBold,
~8–9 DIP advance). The offset from the column's left edge to the glyph's horizontal center
therefore differs between the two rows, producing the visible horizontal misalignment.

Arithmetic (approximate, at 96 DPI, default font metrics):
- Rail width: 16 DIP
- Star (★, 13 pt): advance ≈ 13–14 DIP → center offset from column left ≈ 7 DIP
- M/D (11 pt SemiBold): advance ≈ 8–9 DIP → center offset from column left ≈ 11–11.5 DIP
- Misalignment ≈ 4–4.5 DIP, visible at normal viewing distance

Right-alignment cannot produce coaxial centers when glyph advance widths differ. Centering each
element in the column places each element's horizontal midpoint at column-left + 8 DIP (half of 16),
independent of glyph width.

### Decision 5: Center both markers in the rail column

**Chosen:** Change `HorizontalAlignment="Right"` to `HorizontalAlignment="Center"` on both the
star `TextBlock` (`LaunchWindow.xaml:245`) and the provenance mark `TextBlock`
(`LaunchWindow.xaml:254`). No other attribute changes.

**Rationale:** Both markers live in the same fixed-width `Grid` column. `HorizontalAlignment="Center"`
centers each element's layout box within the column width; because both share the same column width
(16 DIP), their horizontal midpoints coincide. The fix is a single attribute value on two elements;
it introduces no new tokens, no new styles, and no layout dependencies.

**Trade-off — trailing inset:** With right-alignment, the right edge of each element's advance box
sits at the column's right edge, which sits 12 DIP from the row's trailing edge
(`Sizing.ProjectRow.Padding` right = 12 DIP). The trailing inset between the glyph's right edge
and the row boundary is therefore exactly 12 DIP for right-aligned content.

With centering, the glyph's right edge sits at column-right − (column width − glyph width) / 2:
- Star (advance ≈ 13–14 DIP in 16 DIP column): right edge ≈ 1–1.5 DIP inset from column right
  → trailing inset from row edge ≈ 13–13.5 DIP (≈1–1.5 DIP more than the 12 DIP baseline)
- M/D (advance ≈ 8–9 DIP in 16 DIP column): right edge ≈ 3.5–4 DIP inset from column right
  → trailing inset from row edge ≈ 15.5–16 DIP (≈3.5–4 DIP more than the 12 DIP baseline)

The star's trailing inset grows by ~1–1.5 DIP; the M/D inset grows by ~3.5–4 DIP. These amounts
are within normal perceptual tolerance. The user has explicitly requested coaxial markers;
this supersedes the 12 DIP trailing-inset symmetry that the archived
`2026-11-01-project-row-trailing-inset` change optimized for. The fixed-width rail and the
no-jitter guarantee (the rail is a separate `Grid` column) are unchanged.

**Alternatives considered:**

- Keep `HorizontalAlignment="Right"` and add per-element left margins to compensate: Rejected.
  Glyph advance widths vary by font metrics, DPI, font rendering mode, and Windows version. Hard-coded
  margin offsets would need to be re-verified whenever any of those factors change. The fix would be
  fragile, invisible in source review, and wrong on any machine with different font metrics.

- Change to a single-column fixed-width `Border` per marker with `HorizontalAlignment="Center"`:
  Rejected. The current structure is a two-row `Grid` column that already constrains both markers to
  the same slot; adding wrappers is unnecessary indirection.

- Widen the rail from 16 to 18 DIP to absorb the star at 13 pt: Rejected. Star advance width is
  already ≈ 13–14 DIP, so the 16 DIP rail is not the constraint; the constraint is that right-alignment
  cannot coax centers when widths differ, regardless of rail width.

### Decision 6: Update every rail-alignment comment

**Chosen:** Update all comments that describe the rail's alignment strategy: the rail comment at
`LaunchWindow.xaml:199–202`, the outer text/rail comment at `LaunchWindow.xaml:173–175`, and both
`Sizing.xaml` comments (`:94–96` on the row padding, `:101–105` on the rail width). Each now describes
centering instead of right-alignment.

**Rationale:** Every one of these comments states that the markers right-align to the rail's trailing
edge. After this change that statement is false, and comments that contradict the adjacent markup are
worse than no comment. Because they live in files this change already edits (or in the same styling
token file), correcting them is a three-line, zero-risk close-out of the same contradiction rather
than a separate housekeeping pass; the verifier flagged the `LaunchWindow.xaml:173–175` and
`Sizing.xaml` comments as stale after the first alignment pass. No token value changes.

## Risks and trade-offs

**[Risk] `TextMutedBrush` readability** — Mitigated: the archived `2026-10-31-provenance-mark-contrast`
design explicitly confirmed `TextMutedBrush` is readable in all three themes. Values from the current
palette files match those values exactly (see table above). No regression risk.

**[Risk] Stale `CanRemove` binding** — After the trigger is removed, the `DataTrigger` no longer
binds `CanRemove`. The binding is declared in `Style.Triggers`; when the entire block is deleted,
no binding of `CanRemove` remains in the style. The property is still exposed on the ViewModel for
the remove-button logic; this change does not affect that. No error is produced.

**[Trade-off] "M" mark loses emphasis** — This is intentional. The brief explicitly states the
primary distinction is the character itself; uniform muting is the desired state. An implementation
session should not second-guess this.

**[Trade-off] Centering increases trailing inset** — Centering the markers within the 16 DIP rail
adds ~1–1.5 DIP to the star's trailing inset and ~3.5–4 DIP to the mark's trailing inset beyond the
baseline 12 DIP. This is a deliberate trade-off: the user's coaxial requirement takes precedence
over trailing-inset symmetry. The fixed-width rail and jitter-free behavior are unchanged.

**[Trade-off] `Sizing.xaml` comments required correction** — Two comments in
`Sizing.ProjectRow` (`:94–96` and `:101–105`) said the markers right-align to the rail's trailing
edge. They are corrected in this change (see Decision 6) rather than deferred, so no stale alignment
claim survives in the live source. Token values are unchanged.

## Open questions

None. The change is fully defined. The implementation session needs no further analysis.
