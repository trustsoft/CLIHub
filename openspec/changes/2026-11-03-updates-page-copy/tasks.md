# Tasks: Updates Page Copy Revision

## Stage 1: Copy Edits

- [x] 1.1 Change the page description from `"Check for new CLIHub builds, configure update preferences, and view release notes."` to `"Update checks, downloads, and notifications."` on line 105 of `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs`
- [x] 1.2 Change `Content="View release notes  →"` to `Content="Release notes →"` on line 110 of `src/CLIHub/Views/SettingsWindow.xaml`; verify `ToolTip="Open What's New"` (line 112) is unchanged
- [x] 1.3 Remove the entire `<controls:SectionHeader Text="UPDATE PREFERENCES"/>` element (line 10) from `src/CLIHub/Views/SettingsUpdatesPage.xaml` and add `Margin="0,18,0,8"` to the `<controls:SettingsGroup>` opening tag (line 11) to preserve the vertical rhythm (18 px top gap, 8 px bottom gap)
- [x] 1.4 Change `Hint="Look for a new version when CLIHub starts."` to `Hint="Runs once, in the background."` on line 13 of `src/CLIHub/Views/SettingsUpdatesPage.xaml`
- [x] 1.5 Change `Hint="Download an available update in the background."` to `Hint="Downloads in the background."` on line 21 of `src/CLIHub/Views/SettingsUpdatesPage.xaml`
- [x] 1.6 Change `Hint="Show a notification when a stable update is available."` to `Hint="Uses Windows notifications."` on line 29 of `src/CLIHub/Views/SettingsUpdatesPage.xaml`
- [x] 1.7 Change `Title="Notify about pre-release versions"` to `Title="Notify about pre-releases"` on line 38 of `src/CLIHub/Views/SettingsUpdatesPage.xaml`
- [x] 1.8 Change `Hint="Not available yet: CLIHub publishes stable releases only."` to `Hint="Not available yet. Stable releases only."` on line 39 of `src/CLIHub/Views/SettingsUpdatesPage.xaml`

## Stage 2: Spec Delta

- [x] 2.1 Create `openspec/changes/2026-11-03-updates-page-copy/specs/preferences-ui/spec.md` with the MODIFIED requirement "Updates page options" reflecting the new UI labels: `"Release notes"`, no section header, and `"Notify about pre-releases"`
- [x] 2.2 Remove `skip_specs: true` and `x-skip_specs_rationale` from `.openspec.yaml`
- [x] 2.3 Update `proposal.md` to document the spec delta in the Impact section
- [x] 2.4 Update `design.md` to add a "Spec Delta Required" section explaining the discovered quotes and the decision to create a delta
- [x] 2.5 Add a second MODIFIED requirement to `specs/preferences-ui/spec.md`: "Release notes from Settings" with the `Open release notes` scenario updated to `"Release notes"`
- [x] 2.6 Create `specs/settings-theme/spec.md` with the MODIFIED requirement "Persistent page header" and the `Title chip still wraps with the title` scenario updated to `"Release notes"`
- [x] 2.7 Run completeness sweep with `rg -n 'View release notes|UPDATE PREFERENCES|pre-release versions' openspec/specs` to confirm all durable-spec references are covered
- [x] 2.8 Update `proposal.md` Spec Delta section to list all three MODIFIED requirements across two capabilities
- [x] 2.9 Update `design.md` Spec Delta Required section to document all three planning gaps and the completeness sweep
- [x] 2.10 Park the pre-existing "from the status card" wording in `docs/backlog.md` with evidence and a revisit trigger

## Stage 3: Verification Gates

- [x] 3.1 Run `dotnet build CLIHub.sln` from the repository root and verify zero errors
- [x] 3.2 Run `dotnet test CLIHub.sln` from the repository root and verify all tests pass
- [x] 3.3 Run `openspec validate --all` from the repository root and verify zero errors
- [x] 3.4 Run `graphify update .` from the repository root to refresh the knowledge graph after source edits

## Stage 4: Manual Smoke Test

- [x] 4.1 Open CLIHub and navigate to Settings → Updates in dark theme; confirm the approved copy is shown with no clipped or overlapping text, and the page rhythm (first group about 18 px below the divider, 8 px bottom gap) matches the approved mockup
- [x] 4.2 Switch to light theme and re-check Settings → Updates; confirm the copy and spacing are correct in light theme as well
- [x] 4.3 Verify the disabled pre-release card (row 4) still reads correctly with the shortened title and hint, and that its disabled appearance is preserved
