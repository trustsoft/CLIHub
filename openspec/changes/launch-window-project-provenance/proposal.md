# Launch window project provenance mark

## Why

The launch window is the primary project surface, displaying project rows with name, path, and favorite status. Each project has a provenance (manual vs discovered), currently visible only on the Settings Projects page. Verified at HEAD `ccf4285`:

- `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:151-155` shows provenance as a compact "M"/"D" mark with tooltip and accessible name via `ProjectModel.SourceMark` and `SourceLabel` (`:106-128`).
- `src/CLIHub/Views/LaunchWindow.xaml:156-225` shows project rows (logo, name, path, favorite star) with no provenance indicator — the data is present in `ProjectModel` but not bound.
- The launch window is where users select projects and see their list most frequently; provenance is context that helps distinguish manually registered projects from auto-discovered ones.

Making provenance visible in both surfaces unifies the display of this stable metadata and reinforces which projects were added explicitly vs found by scanning.

## What Changes

- The launch window project row `DataTemplate` adds a provenance mark binding: the compact "M"/"D" text with tooltip and accessible name from `ProjectModel.SourceMark` and `SourceLabel`.
- Visual presentation: an equivalent `ProjectProvenanceMark` style in `LaunchWindowStyles.xaml` with the same token-based values as the Settings page (decision recorded in `design.md`; the Settings page is not edited, so the two findings stay independently committable).
- Layout: the mark is placed to preserve the compact row (verified with the width-aware path-trimming logic), most likely after the name TextBlock in the launch row's text StackPanel.
- Spec delta: MODIFIED the existing `project-management` "Project provenance" requirement, preserving its five original scenarios and adding one scenario confirming the launch window shows the mark.

## Impact

- Affected specs: `project-management` (MODIFIED "Project provenance" requirement, one added scenario).
- Affected code:
  - `src/CLIHub/Views/LaunchWindow.xaml` — add provenance mark binding in the project-row DataTemplate.
  - `src/CLIHub/Themes/LaunchWindowStyles.xaml` — add the launch-window `ProjectProvenanceMark` style.
  - No ViewModel, model, service, or configuration changes — `ProjectModel` already exposes `SourceMark` and `SourceLabel`.
  - No new tests — the data path is unit-tested (`tests/CLIHub.Tests/ViewModels/Projects/ProjectModelCatalogTests.cs:93-111`); the visual binding is verified by manual smoke.
- Non-goals:
  - No change to Settings page appearance or behavior.
  - No new persisted fields or configuration schema changes.
  - No enrichment or ProjectModel signature changes.
  - No automated XAML-parsing test (fragile and would not verify runtime rendering).

## Dependencies

None — `ProjectModel.SourceMark` and `SourceLabel` exist as of the `introduce-project-model` change (archived).
