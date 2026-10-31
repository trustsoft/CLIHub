# Design: provenance-mark-contrast

## Context

The project provenance mark appears in two surfaces:
- Settings Projects page: `SettingsProjectRegistryPage.xaml:152-155`
- Launch window project list: `LaunchWindow.xaml:252-255`

Both reference `Style="{StaticResource ProjectProvenanceMark}"`. The style is currently defined locally in each surface's resource scope:
- Settings: `SettingsProjectRegistryPage.xaml:10-24` (local `UserControl.Resources`)
- Launch window: `LaunchWindowStyles.xaml:254-266` (duplicated from Settings)

Both definitions are identical: `FontSize=Sizing.FontSize.Section`, `FontWeight=SemiBold`, `VerticalAlignment=Center`, default `Foreground=TextMutedBrush`, with a `DataTrigger` on `CanRemove=True` setting `Foreground=AccentSelectionBrush`.

Manual smoke testing in Light theme (2026-10-31) showed the manual mark (`M`, triggered by `CanRemove=True`) is nearly invisible: `AccentSelectionBrush` is a translucent accent with low contrast on light surfaces. Discovered projects (`D`, `CanRemove=False`) use `TextMutedBrush` and remain readable.

The backlog item documenting the duplication is at `docs/backlog.md:243-252` with trigger "a third surface needs the mark, or the mark's visual values change." This change is triggered by the contrast issue.

## Current state with file:line evidence

### Settings Projects page

Style definition:
- `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:10-24`:
  ```xaml
  <Style x:Key="ProjectProvenanceMark" TargetType="TextBlock">
      <Setter Property="FontSize" Value="{StaticResource Sizing.FontSize.Section}"/>
      <Setter Property="FontWeight" Value="SemiBold"/>
      <Setter Property="VerticalAlignment" Value="Center"/>
      <Setter Property="Foreground" Value="{DynamicResource TextMutedBrush}"/>
      <Style.Triggers>
          <DataTrigger Binding="{Binding CanRemove}" Value="True">
              <Setter Property="Foreground" Value="{DynamicResource AccentSelectionBrush}"/>
          </DataTrigger>
      </Style.Triggers>
  </Style>
  ```

Style reference:
- `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:152-155`:
  ```xaml
  <TextBlock Margin="8,0,0,0"
             Style="{StaticResource ProjectProvenanceMark}"
             AutomationProperties.Name="{Binding SourceLabel}"
             Text="{Binding SourceMark}"
             ToolTip="{Binding SourceLabel}"/>
  ```

### Launch window

Style definition (duplicate):
- `src/CLIHub/Themes/LaunchWindowStyles.xaml:254-266`:
  ```xaml
  <Style x:Key="ProjectProvenanceMark" TargetType="TextBlock">
      <Setter Property="FontSize" Value="{StaticResource Sizing.FontSize.Section}"/>
      <Setter Property="FontWeight" Value="SemiBold"/>
      <Setter Property="VerticalAlignment" Value="Center"/>
      <Setter Property="Foreground" Value="{DynamicResource TextMutedBrush}"/>
      <Style.Triggers>
          <DataTrigger Binding="{Binding CanRemove}" Value="True">
              <Setter Property="Foreground" Value="{DynamicResource AccentSelectionBrush}"/>
          </DataTrigger>
      </Style.Triggers>
  </Style>
  ```

Comment at lines 251-253 documents the duplication:
```xaml
<!-- Project row provenance mark: the compact "M"/"D" source indicator, matching the style with
     the same key defined in SettingsProjectRegistryPage.xaml (the sibling definition). Kept
     local to each surface so both stay independently owned; the visual values must stay equal. -->
```

Style reference:
- `src/CLIHub/Views/LaunchWindow.xaml:248-255`:
  ```xaml
  <TextBlock Grid.Row="1"
             Grid.Column="1"
             HorizontalAlignment="Center"
             VerticalAlignment="Center"
             Style="{StaticResource ProjectProvenanceMark}"
             AutomationProperties.Name="{Binding SourceLabel}"
             Text="{Binding SourceMark}"
             ToolTip="{Binding SourceLabel}"/>
  ```

### Resource dictionary merge chains

Settings:
- `App.xaml` application-level merge includes `SettingsStyles.xaml`
- `src/CLIHub/Themes/SettingsStyles.xaml:6-8` merges `Controls.xaml`

Launch window:
- `src/CLIHub/Views/LaunchWindow.xaml:15-17` merges `LaunchWindowStyles.xaml`
- `src/CLIHub/Themes/LaunchWindowStyles.xaml:14-16` merges `Controls.xaml`

`Controls.xaml` already holds shared styles used by both surfaces (typography, buttons, tooltips). It merges `Sizing.xaml` (line 11-13), so `Sizing.FontSize.Section` is in scope.

### Backlog

- `docs/backlog.md:243-252`:
  ```markdown
  ### Provenance-mark style is duplicated across two project surfaces
  
  - Severity: Low (maintainability; both definitions currently use identical tokens)
  - Evidence: `SettingsProjectRegistryPage.xaml:10-24` holds the local `ProjectProvenanceMark` style;
    `launch-window-project-provenance` adds an equivalent style in `LaunchWindowStyles.xaml` rather
    than editing the Settings page, so the two findings can be committed independently. Both use
    `Sizing.FontSize.Section`, `TextMutedBrush`, `AccentSelectionBrush`, and a `CanRemove` trigger.
  - Proposed remedy: promote the style into `Controls.xaml` (or a `ProjectStyles.xaml`) and delete both
    local copies in one focused change.
  - Trigger: a third surface needs the mark, or the mark's visual values change.
  ```

## Goals / Non-Goals

**Goals:**
- Make the provenance mark readable in Light, Dark, and Blue themes
- Remove the style duplication by promoting to `Controls.xaml`
- Keep the character distinction (`M` vs `D`) as the primary signal; foreground is secondary emphasis

**Non-Goals:**
- Changing the mark characters, data binding, or positioning
- Unit-testing XAML style resolution (no build-time gate exists; verification is manual smoke)
- Inventing a brittle XAML-parsing test that offers no runtime safety

## Decisions

### Decision 1: New brush rule for manual projects

**Chosen:** Use `TextPrimaryBrush` for manual projects (`CanRemove=True`).

**Rationale:** `TextPrimaryBrush` is a semantic token with strong contrast in all themes:
- Light theme: near-black (high contrast on white)
- Dark theme: near-white (high contrast on dark gray)
- Blue theme: white (high contrast on blue)

The current `AccentSelectionBrush` is a translucent accent intended for selected items with a colored surface behind them (see `AccentSelectionBrush` usage in `LaunchWindowStyles.xaml:108,289` and `SettingsStyles.xaml`). On the Settings page's light surface in Light theme, the translucent accent loses opacity and blends into the background.

`TextPrimaryBrush` is already used for primary content like project names (`LaunchWindow.xaml:237`, `SettingsProjectRegistryPage.xaml:146`) and agent names, so it is semantically appropriate for the manual mark's "primary provenance" role.

**Alternatives considered:**
- Keep `AccentSelectionBrush`, increase opacity: Rejected — `AccentSelectionBrush` is a palette token we don't control; changing it affects other surfaces
- Use `AccentBrush` (opaque accent): Rejected — less semantic (not a brand/accent UI element); still theme-dependent contrast
- Use a hardcoded color: Rejected — breaks theme consistency and maintainability

### Decision 2: Keep `TextMutedBrush` for discovered projects

**Chosen:** Discovered projects continue using `TextMutedBrush` (default foreground, no trigger).

**Rationale:** `TextMutedBrush` is readable in all themes and semantically correct for discovered projects (secondary/automatic provenance). Smoke testing confirmed it is visible in Light theme.

### Decision 3: Promote to `Controls.xaml`

**Chosen:** Add the shared style to `src/CLIHub/Themes/Controls.xaml` and delete both local definitions.

**Rationale:**
- `Controls.xaml` already holds shared styles (typography, buttons) used by both surfaces
- Both merge chains include `Controls.xaml`, so no extra merge declarations needed
- `Controls.xaml` merges `Sizing.xaml`, so `Sizing.FontSize.Section` resolves
- The backlog item proposed `Controls.xaml` or a `ProjectStyles.xaml`; `Controls.xaml` is simpler (no new file, no extra merge)

**Alternatives considered:**
- Create `ProjectStyles.xaml`: Rejected — unnecessary indirection for one style; `Controls.xaml` is the established home for shared keyed styles
- Keep duplicated and update both: Rejected — defeats the deduplication goal and backlog trigger

### Decision 4: Manual smoke verification only

**Chosen:** Verification is manual smoke in Light, Dark, and Blue themes, observing both surfaces.

**Rationale:**
- WPF `ResourceDictionary` resolution is runtime-only; a missing key compiles but throws at window load
- No build gate catches a missing key; the only verification is launching the app and observing
- Unit-testing XAML style resolution requires hosting a WPF window in a test, which is brittle and offers no safety beyond what manual smoke confirms

A screenshot or verbal confirmation in the tasks verifies:
- Manual mark is readable (strong contrast) in Light/Dark/Blue
- Discovered mark is readable (muted but visible) in Light/Dark/Blue
- Both surfaces (Settings page, launch window) resolve the shared style
- The mark positioning and alignment are unchanged

## Risks / Trade-offs

**[Risk] Missing key only fails at runtime** → Mitigation: manual smoke includes actual app launch and navigation to both surfaces.

**[Risk] `TextPrimaryBrush` too strong for manual mark** → Trade-off: the character `M` vs `D` remains the primary distinction; a stronger foreground for manual is intentional secondary emphasis. If too strong, we can revert or choose a middle tone.

**[Trade-off] No automated style-resolution test** → Benefit: avoids brittle XAML-hosting test infrastructure that offers no compile-time safety. Cost: regression requires manual re-verification. Accepted because the style is narrow and stable.

## Open Questions

None. The visual rule and promotion path are settled; implementation is mechanical.
