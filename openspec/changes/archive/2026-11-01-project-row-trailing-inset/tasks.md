# Tasks: project-row-trailing-inset

Implementation happens in a fresh session; `design.md` carries the full rationale and file references. Tasks are checked off as they land. The change is XAML-only; no C# code, ViewModel, or configuration changes are needed.

## 1. Update sizing tokens

- [x] 1.1 Read `src/CLIHub/Themes/Sizing.xaml:91` to verify the current `Sizing.ProjectRow.Padding` value (`"12,9,10,9"`)
- [x] 1.2 Edit `Sizing.ProjectRow.Padding` from `"12,9,10,9"` to `"12,9,12,9"` — the trailing (right) padding becomes 12, matching the leading (left) padding
- [x] 1.3 Read `src/CLIHub/Themes/Sizing.xaml:98` to verify the current `Sizing.ProjectRow.MarkerRailWidth` value (30)
- [x] 1.4 Edit `Sizing.ProjectRow.MarkerRailWidth` from 30 to 16 — the rail shrinks to accommodate the star glyph (FontSize 13 at ~12-13 DIP rendered width) with 3-4 DIP slack for right alignment
- [x] 1.5 Build: `dotnet build CLIHub.sln` — green with no XAML parsing errors

## 2. Update marker alignment in launch window

- [x] 2.1 Read `src/CLIHub/Views/LaunchWindow.xaml:239-246` to verify the favorite star's current `HorizontalAlignment="Center"` (`:241`)
- [x] 2.2 Edit the favorite star's `HorizontalAlignment` from `"Center"` to `"Right"` — the star sits flush at the rail's right edge instead of centered
- [x] 2.3 Read `src/CLIHub/Views/LaunchWindow.xaml:248-255` to verify the provenance mark's current `HorizontalAlignment="Center"` (`:250`)
- [x] 2.4 Edit the provenance mark's `HorizontalAlignment` from `"Center"` to `"Right"` — the mark sits flush at the rail's right edge, aligned with the star above it
- [x] 2.5 Build: `dotnet build CLIHub.sln` — green with no XAML parsing errors
- [x] 2.6 Update the marker-rail paragraph in `docs/architecture/ui.md` — both markers right-align to the rail's trailing edge, the project row's leading and trailing content insets are symmetric (12 DIP), and the fixed width keeps the path text from shifting when the star is hidden
- [x] 2.7 Add a `Fixed` bullet under the existing `1.0.4` heading in `CHANGELOG.md` recording the symmetric trailing inset (`project-row-trailing-inset`, `launch-window-layout`)
- [x] 2.8 Build: `dotnet build CLIHub.sln` — green after the documentation updates

## 3. Manual smoke test

The user must perform this smoke test to verify the visual outcome before the change is archived.

- [x] 3.1 Run: `dotnet run --project src/CLIHub/CLIHub.csproj`
- [x] 3.2 Open the launch window (system tray → launch or hotkey)
- [x] 3.3 Select a project row (click on it to trigger the accent selection highlight)
- [x] 3.4 Measure or visually confirm: the favorite star's right edge sits 12 DIP from the row highlight's right edge, matching the 12 DIP leading inset from the logo to the row highlight's left edge
- [x] 3.5 Verify the provenance mark ("M" or "D") on the path line is also flush right, aligned under the star
- [x] 3.6 Check that no row jitters when toggling a project's favorite state (the rail remains fixed-width even when the star is hidden)
- [x] 3.7 Resize the pane or drag the splitter: confirm the path re-trims dynamically and the markers stay flush at the row's trailing edge with no gap or overlap
- [x] 3.8 Switch themes (Settings → Appearance → Dark/Light/Blue) and verify the symmetric insets hold in each theme

Confirmed by the user ("smoke - ok", 2026-11-01). An orchestrator capture of the running build measured the selected row at
1.5x scaling: highlight `x 17..454`, content `x 35..436`, so the leading and trailing insets are both 18 px (12 DIP).

## 4. Final validation

- [x] 4.1 Build: `dotnet build CLIHub.sln` — green with no warnings
- [x] 4.2 Tests: `dotnet test CLIHub.sln` — green (no test changes are expected; the change is XAML-only)
- [x] 4.3 Validate: `openspec validate --all` — green (no spec deltas are produced; `skip_specs: true` is set in `.openspec.yaml`)
- [x] 4.4 Update graph: `graphify update .` — keep the knowledge graph current after XAML edits
- [x] 4.5 Quote the exact `openspec validate --all` output in the final report
