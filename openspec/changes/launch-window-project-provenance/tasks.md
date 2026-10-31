# Tasks: launch-window-project-provenance

Implementation happens in a fresh session; `design.md` carries the full rationale and file references. Tasks are checked off as they land. The change is UI-only; no service, model, or configuration changes are needed.

## 1. Launch-window style

- [x] 1.1 Read `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:10-24` to verify the reference `ProjectProvenanceMark` style values (FontSize, FontWeight, VerticalAlignment, Foreground, DataTrigger on `CanRemove`)
- [x] 1.2 Read `src/CLIHub/Themes/LaunchWindowStyles.xaml` around its keyed styles and add an equivalent `<Style x:Key="ProjectProvenanceMark" TargetType="TextBlock">` with the exact same token-based values; add a comment pointing to `SettingsProjectRegistryPage.xaml` as the sibling definition (do NOT edit the Settings page)
- [x] 1.3 Build: `dotnet build CLIHub.sln` — green with no XAML parsing errors

## 2. Launch-window provenance binding

- [x] 2.1 Read `src/CLIHub/Views/LaunchWindow.xaml:190-220` (the text StackPanel in the project-row DataTemplate) to locate the name TextBlock (`:194-197`)
- [x] 2.2 Replace the standalone name TextBlock with a two-column `Grid`:
    - `ColumnDefinition Width="*"` for the name TextBlock (same bindings: `FontSize=Sizing.FontSize.Name`, `Foreground=TextPrimaryBrush`, `Text=Name`, `TextTrimming=CharacterEllipsis`), no margin
    - `ColumnDefinition Width="Auto"` for a provenance-mark TextBlock with `Margin="8,0,0,0"`, `Style="{StaticResource ProjectProvenanceMark}"`, `AutomationProperties.Name="{Binding SourceLabel}"`, `Text="{Binding SourceMark}"`, `ToolTip="{Binding SourceLabel}"`
    - The `*` column is required so the name keeps adaptive `CharacterEllipsis` trimming; a horizontal StackPanel would measure the name with unbounded width and break trimming
- [x] 2.3 Build: `dotnet build CLIHub.sln` — green with no XAML parsing errors
- [x] 2.4 If build errors reference `ProjectProvenanceMark` not found, verify that `LaunchWindow.xaml`'s resources merge `LaunchWindowStyles.xaml` and that the style is defined directly in that file (it is not coming from `Controls.xaml`)

## 3. Manual smoke test

- [x] 3.1 Run: `dotnet run --project src/CLIHub/CLIHub.csproj`
- [x] 3.2 Verify the application has at least one manual project and one discovered project (use the Settings Projects page to add/remove as needed if the config is empty or uniform)
- [x] 3.3 Open the launch window (system tray → launch or hotkey)
- [x] 3.4 Confirm each project row shows the correct provenance mark after the name: "M" for manual projects, "D" for discovered projects
- [ ] 3.5 Hover over a mark and verify the tooltip displays the full label ("Manual" or "Discovered")
- [ ] 3.6 Check visual spacing: the mark should sit 8 DIP to the right of the name with no overlap or awkward gaps; the path below should still be trimmed correctly with the ellipsis appearing when the pane is narrow
- [ ] 3.7 Resize the launch window or drag the pane splitter: confirm the path re-trims dynamically and does not overlap the favorite star or provenance mark
- [x] 3.8 If an accessibility inspector or screen reader is available, verify the provenance mark's accessible name is the full label ("Manual"/"Discovered")
- [ ] 3.9 Switch themes (Settings → Appearance → Dark/Light/Blue) and verify the provenance mark color follows the palette (muted for discovered, accent-selection for manual) in each theme

## 4. Spec delta (already drafted — verify, do not rewrite)

- [x] 4.1 Read `openspec/specs/project-management/spec.md:147-169` (the "Project provenance" requirement and its existing scenarios)
- [x] 4.2 Read the drafted delta at `openspec/changes/launch-window-project-provenance/specs/project-management/spec.md` and confirm it keeps all five original scenario names and adds exactly `#### Scenario: Launch window shows provenance mark`
- [x] 4.3 If the implementation deviates from the drafted behavior, update the delta to match what was actually built (do not add scenarios for unbuilt behavior)

## 5. Final validation

- [x] 5.1 Build: `dotnet build CLIHub.sln` — green with no warnings
- [x] 5.2 Tests: `dotnet test CLIHub.sln` — green (no new test is expected; existing `ProjectModelCatalogTests` already cover the data path)
- [x] 5.3 Validate: `npx --yes openspec validate launch-window-project-provenance` (validate only this change, as instructed — do not run `--all`)
- [x] 5.4 Quote the exact validation output in the final report
