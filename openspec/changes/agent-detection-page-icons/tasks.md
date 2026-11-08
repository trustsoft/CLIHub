# Tasks: Agent Detection page icon set

## 1. Add new glyph constants

- [x] 1.1 In `src/CLIHub/Theming/IconGlyphs.cs`, add four constants after the existing `Process` constant (line 25): `Radar = "\uEB44"`, `Recent = "\uE823"`, `Stopwatch = "\uE916"`, `UpdateRestore = "\uE777"`. Verify the file compiles with `dotnet build CLIHub.sln` and no new warnings are introduced.

Gates:

```powershell
dotnet build CLIHub.sln
```

## 2. Swap the page/sidebar icon

- [x] 2.1 In `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs` line 82, change `IconGlyphs.Process` to `IconGlyphs.Radar` for the `agent-detection` catalog entry. Verify the build still passes and that the change affects only that one line.

Gates:

```powershell
dotnet build CLIHub.sln
```

## 3. Update setting-row card icons

- [x] 3.1 In `src/CLIHub/Views/SettingsAgentDetectionPage.xaml` line 38, change the Cache TTL `SettingCard` icon attribute from `{x:Static themes:IconGlyphs.Diagnostic}` to `{x:Static themes:IconGlyphs.Recent}`.
- [x] 3.2 In `src/CLIHub/Views/SettingsAgentDetectionPage.xaml` line 49, change the Version check timeout `SettingCard` icon attribute from `{x:Static themes:IconGlyphs.Diagnostic}` to `{x:Static themes:IconGlyphs.Stopwatch}`.
- [x] 3.3 Build the project and verify no residual `Diagnostic` references remain in `SettingsAgentDetectionPage.xaml`: `dotnet build CLIHub.sln` must pass.

Gates:

```powershell
dotnet build CLIHub.sln
```

## 4. Update summary card icons

- [x] 4.1 In `src/CLIHub/Views/SettingsWindow.xaml` line 238 (CACHE TTL card), change `{x:Static themes:IconGlyphs.Diagnostic}` to `{x:Static themes:IconGlyphs.Recent}`.
- [x] 4.2 In `src/CLIHub/Views/SettingsWindow.xaml` line 243 (TIMEOUT card), change `{x:Static themes:IconGlyphs.Diagnostic}` to `{x:Static themes:IconGlyphs.Stopwatch}`.
- [x] 4.3 In `src/CLIHub/Views/SettingsWindow.xaml` line 249 (DEFAULTS card), change `{x:Static themes:IconGlyphs.Diagnostic}` to `{x:Static themes:IconGlyphs.UpdateRestore}`.
- [x] 4.4 Verify no residual `Diagnostic` references remain in the `AgentDetectionSummaryCards` DataTemplate in `SettingsWindow.xaml`, and that the build passes.

Gates:

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

## 5. Validate spec deltas

- [x] 5.1 Run `openspec validate --all` and confirm it reports 0 failed items. The change already has its spec delta files at `openspec/changes/agent-detection-page-icons/specs/settings-theme/spec.md` and `openspec/changes/agent-detection-page-icons/specs/preferences-ui/spec.md`; no source edit is required for this step.
- [x] 5.2 Add the user-visible entry to `CHANGELOG.md` under `## Unreleased` → `### Changed`, naming the change id and the two updated capabilities (added during implementation; the plan omitted it, but every shipped change carries a changelog bullet).

Gates:

```powershell
openspec validate --all
```

## 6. Smoke test

- [x] 6.1 Run the application (`dotnet run --project src/CLIHub/CLIHub.csproj`), open Settings, and navigate to "Agent Detection". Verify: (a) the sidebar item shows the Radar glyph (a circle with concentric arcs, not a gear); (b) the page-header emblem shows the same Radar glyph; (c) the Cache TTL row card shows the Recent glyph (circular arrow); (d) the Version check timeout row card shows the Stopwatch glyph (round with a crown/stem); (e) the CACHE TTL summary card shows Recent; (f) the TIMEOUT summary card shows Stopwatch; (g) the DEFAULTS summary card shows UpdateRestore (circular undo arrow). No glyph repeats across the seven positions.

## Workflow follow-up

- Run `graphify update .` after implementation is complete to refresh the knowledge graph.
- Archive the change only after all gates pass and the user approves: `openspec archive agent-detection-page-icons`.
- After archiving, run the post-archive integrity checks: `git status --porcelain` (clean), `openspec list` (change absent), `openspec validate --all` (0 errors).
