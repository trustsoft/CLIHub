# Tasks: agent-detection-settings-page

## 1. Create new Agent Detection page view

- [x] 1.1 Create `src/CLIHub/Views/SettingsAgentDetectionPage.xaml` as a `UserControl` with a `StackPanel` root containing one `SectionHeader` ("DETECTION") and one `SettingsGroup` with the two `SettingCard` + `NumericField` controls. Copy the DETECTION section structure from `SettingsAgentsPage.xaml:105-128`, adjusting bindings to use `DataContext` properties directly (no `AgentsState.` prefix). Verify the solution compiles with 0 warnings/errors.

- [x] 1.2 Create `src/CLIHub/Views/SettingsAgentDetectionPage.xaml.cs` with minimal code-behind (constructor calling `InitializeComponent()`). Verify the solution compiles.

## 2. Add page to Settings navigation

- [x] 2.1 Add the new `SettingsPage` entry to `SettingsPageCatalog.AllPages` in `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs` after the `agents` entry (between lines 76-77). Use key `"agent-detection"`, title `"Agent Detection"`, description `"Caching and timeout behavior for agent detection."`, group `"Engines & Repos"`, icon `IconGlyphs.Diagnostic`, and `SummaryRowKey: "AgentDetectionSummaryCards"`. Verify the solution compiles.

- [x] 2.2 Add the page host declaration to `src/CLIHub/Views/SettingsWindow.xaml` in the scrolling grid (after line 464). Declare `<views:SettingsAgentDetectionPage Visibility="{Binding SelectedPage.Key, Converter={StaticResource PageKeyVisibility}, ConverterParameter=agent-detection}"/>`. Verify the solution compiles.

## 3. Add ViewModel summary properties for header cards

- [x] 3.1 Add four new properties to `SettingsViewModel` (after line 349, in the detection properties section): `ProbePolicyCacheTtlSummary` (readonly string, formats effective TTL with "min" unit), `ProbePolicyCacheTtlDetail` (readonly string, distinguishes custom vs. "Built-in default"), `ProbePolicyTimeoutSummary` (readonly string, formats effective timeout with "sec" unit), `ProbePolicyTimeoutDetail` (readonly string, distinguishes custom vs. "Built-in default"). Implement as getters that parse `ProbeTtlText`/`ProbeTimeoutText` and fall back to watermark values. Include XML documentation. Verify the solution compiles.

## 4. Create summary template in SettingsWindow.xaml

- [x] 4.1 Add the `AgentDetectionSummaryCards` DataTemplate to `src/CLIHub/Views/SettingsWindow.xaml` resources (after line 249, before the `NavPages` CollectionViewSource). The template contains a `SpacedPanel` with two `SummaryCard` controls: CACHE TTL (bound to `ProbePolicyCacheTtlSummary` and `ProbePolicyCacheTtlDetail`, icon `IconGlyphs.Diagnostic`) and TIMEOUT (bound to `ProbePolicyTimeoutSummary` and `ProbePolicyTimeoutDetail`, icon `IconGlyphs.Diagnostic`). Verify the solution compiles.

## 5. Remove DETECTION section from CLI Agents page

- [x] 5.1 Delete the DETECTION section from `src/CLIHub/Views/SettingsAgentsPage.xaml` (lines 105-128: the `SectionHeader` and the `SettingsGroup` containing the two detection fields). Leave the MANAGEMENT section and the AGENTS section intact. Verify the solution compiles with 0 warnings/errors.

- [x] 5.2 Remove the DETECTION summary card from the `AgentsSummaryCards` template in `src/CLIHub/Views/SettingsWindow.xaml` (lines 243-247: the third `SummaryCard` bound to `ProbePolicyDetail` and `ProbePolicySummary`). Keep the AGENTS and DEFAULT cards. Verify the solution compiles.

## 6. Update tests

- [x] 6.1 Update `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs`: change line 26 from `Assert.Equal(9, viewModel.Pages.Count);` to `Assert.Equal(10, viewModel.Pages.Count);`, add `"agent-detection"` to the expected keys array at line 29 (after `"agents"`), and add the slot metadata tuple `("agent-detection", null, null, "AgentDetectionSummaryCards")` to the assertion at lines 72-84. Verify tests compile.

- [x] 6.2 Update `tests/CLIHub.Tests/Views/SettingsPageMetadataTests.cs`: change line 57 from `Assert.Equal(9, pages.Length);` to `Assert.Equal(10, pages.Length);`. The test automatically validates the new page's host declaration and summary template. Run `dotnet test CLIHub.sln` and verify all tests pass.

## 7. Update documentation

- [x] 7.1 Update `docs/architecture/ui.md`: on line 56, change "on CLI Agents, the agents, default-agent, and detection summary cards use it" to "on CLI Agents, the agents and default-agent summary cards use it; on Agent Detection, the cache TTL and timeout summary cards use it." In the "Adding a Settings Page" section (lines 70-84), update any reference to "nine pages" to "ten pages."

- [x] 7.2 Update `docs/glossary.md`: in the DETECTION entry (lines 108-114), change "The Settings UI labels this area 'DETECTION' (the section header and the page-header summary card)" to "The Settings UI labels this area 'DETECTION' (the Agent Detection page section header and summary cards)" and add a sentence: "The detection settings appear on the Agent Detection Settings page, positioned immediately after CLI Agents in the sidebar."

- [x] 7.3 Review `README.md`: if the Settings section enumerates pages, add "Agent Detection" after "CLI Agents." If README images are listed, note that capturing the new page is deferred to a manual task (see open question 1 in design.md).

## 8. Spec deltas

- [x] 8.1 Create `openspec/changes/agent-detection-settings-page/specs/preferences-ui/spec.md` with MODIFIED requirements: update "Settings sections and pages" to list ten pages (add "Agent Detection" after "CLI Agents" with its description and icon), update "CLI Agents page management" to remove the detection fields and DETECTION summary card, and add the new "Agent Detection page content" requirement with scenarios for the two fields and the two summary cards. Preserve original scenario names for all MODIFIED requirements. Verify the delta spec is valid markdown.

- [x] 8.2 Create `openspec/changes/agent-detection-settings-page/specs/settings-theme/spec.md` with MODIFIED requirements: update "Persistent page header" to change the CLI Agents summary-row enumeration from "agents, default-agent, and detection cards" to "agents and default-agent cards" and add Agent Detection to the enumeration with "cache TTL and timeout cards"; update "CLI Agents page summary cards" to describe two cards (AGENTS, DEFAULT) instead of three and retire the detection-card behavior from the requirement text and the "Cards follow live changes" scenario. The validator refuses to drop the original scenarios, so keep "Probe card shows the effective policy" and "Custom probe values" under their original names and reword them to state that the relocated behavior is provided by the Agent Detection page. Add ADDED requirement "Agent Detection page summary cards" describing the two new cards (CACHE TTL, TIMEOUT) with effective-value display, detail-line logic distinguishing "Built-in default" from "Custom (default: N min|sec)", and live refresh when detection fields are edited. Preserve original scenario names for all MODIFIED requirements. Verify the delta spec is valid markdown.

## 8A. Increment 2: Update aggregation properties on IAgentManagementSection

- [x] 8A.1 Add four new properties to `src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs` (after line 125, before the `Load` method): `UpdatesAvailableCount` (int, readonly), `UpdatesValue` (string, readonly), `UpdatesDetail` (string, readonly), `UpdatesValueTone` (SummaryCardTone, readonly). Include XML documentation describing the UPDATES card aggregation logic. Verify the solution compiles.

- [x] 8A.2 Implement the four properties in `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` (after line 293, before `Load`). `UpdatesAvailableCount` counts cards where `HasUpdateAvailable` is true. `UpdatesValue` returns the count as a string when > 0, "None" when every installed card is UpToDate (or no installed cards), "Unknown" otherwise. `UpdatesDetail` returns "N of M installed agents" (with singular/plural handling) when updates exist, "All installed agents up to date" when checked and none, "Run Check updates" while unknown, "No installed agents" when none installed. `UpdatesValueTone` returns `Accent` when updates exist, `Success` when checked and none, `Muted` otherwise. Verify the solution compiles.

- [x] 8A.3 Extend `RefreshAvailabilitySummary()` at line 375 to raise property-changed notifications for the four new properties: `OnPropertyChanged(nameof(this.UpdatesAvailableCount))`, `OnPropertyChanged(nameof(this.UpdatesValue))`, `OnPropertyChanged(nameof(this.UpdatesDetail))`, `OnPropertyChanged(nameof(this.UpdatesValueTone))`. Verify the solution compiles.

## 8B. Increment 2: Defaults card and reset command on SettingsViewModel

- [x] 8B.1 Add three new properties to `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` (after the four increment-1 summary properties, around line 380): `ProbePolicyDefaults` (readonly string, formatted like `$"{ProbeTtlWatermark} min · {ProbeTimeoutWatermark} sec"`), `ProbePolicyDefaultsDetail` (readonly string, returns "Used when a field is empty"), `CanResetProbePolicy` (readonly bool, returns true when either `probeTtlText` or `probeTimeoutText` is non-empty). Include XML documentation. Verify the solution compiles.

- [x] 8B.2 Add `ResetProbePolicyCommand` (RelayCommand) to the constructor around line 217, wired to a private `ResetProbePolicy()` method with `CanResetProbePolicy` as the availability gate. Implement `ResetProbePolicy()` to set `ProbeTtlText = string.Empty` and `ProbeTimeoutText = string.Empty`, then call `RaiseProbePolicyChanges()`. Verify the solution compiles.

- [x] 8B.3 Extend `RaiseProbePolicyChanges()` at line 335 to raise property-changed notifications for `CanResetProbePolicy` and `ResetProbePolicyCommand.RaiseCanExecuteChanged()`. Verify the solution compiles.

## 8C. Increment 2: Move shared chip-button style to SettingsStyles.xaml

- [x] 8C.1 Copy the `AgentsHeaderChipButton` style from `src/CLIHub/Views/SettingsAgentsPage.xaml` (lines 50-75) to `src/CLIHub/Themes/SettingsStyles.xaml` (after the existing styles, before the closing `ResourceDictionary` tag). Rename the key to `SectionHeaderChipButton`. Verify the style definition remains byte-for-byte identical except for the key. Verify the solution compiles.

- [x] 8C.2 Delete the local `AgentsHeaderChipButton` style from `src/CLIHub/Views/SettingsAgentsPage.xaml` (lines 50-75). Update the two chip buttons in the AGENTS section header (lines 121 and 137) to use `Style="{StaticResource SectionHeaderChipButton}"` instead of `Style="{StaticResource AgentsHeaderChipButton}"`. Verify the solution compiles.

## 8D. Increment 2: Add UPDATES card to CLI Agents summary row

- [x] 8D.1 Add a third `SummaryCard` to the `AgentsSummaryCards` template in `src/CLIHub/Views/SettingsWindow.xaml` (after the DEFAULT card, before the closing `SpacedPanel` tag around line 240). Bind `Value` to `AgentsState.UpdatesValue`, `Detail` to `AgentsState.UpdatesDetail`, `ValueTone` to `AgentsState.UpdatesValueTone`, set `Label="UPDATES"` and `Icon="{x:Static themes:IconGlyphs.Update}"`. Verify the solution compiles.

## 8E. Increment 2: Add DEFAULTS card and Reset button to Agent Detection page

- [x] 8E.1 Add a third `SummaryCard` to the `AgentDetectionSummaryCards` template in `src/CLIHub/Views/SettingsWindow.xaml` (after the TIMEOUT card, before the closing `SpacedPanel` tag). Bind `Value` to `ProbePolicyDefaults`, `Detail` to `ProbePolicyDefaultsDetail`, set `Label="DEFAULTS"` and `Icon="{x:Static themes:IconGlyphs.Diagnostic}"`. Verify the solution compiles.

- [x] 8E.2 In `src/CLIHub/Views/SettingsAgentDetectionPage.xaml`, wrap the existing `SectionHeader` (line 5, `Text="DETECTION"`) in a `Grid` and add a right-aligned `SpacedPanel` next to it (mirroring the CLI Agents header structure at `SettingsAgentsPage.xaml:105-156`). Add a `Button` with `Command="{Binding ResetProbePolicyCommand}"`, `IsEnabled="{Binding CanResetProbePolicy}"`, `Style="{StaticResource SectionHeaderChipButton}"`, `ToolTip="Restore the built-in defaults"`, containing a `StackPanel` with `IconGlyphs.Refresh` icon and "Reset" label. Verify the solution compiles with 0 warnings/errors.

## 8F. Increment 2: Test updates for reset and updates aggregation

- [x] 8F.1 Add focused coverage for `ResetProbePolicyCommand` to `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelTests.cs`. Add a test method `ResetProbePolicy_ClearsBothFields` verifying that the command is disabled when both fields are empty, enabled when either is non-empty, and that Execute sets both `ProbeTtlText` and `ProbeTimeoutText` to empty and raises probe policy summary changes. Verify tests compile and pass.

- [x] 8F.2 Add focused coverage for updates aggregation to `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`. Add test methods verifying `UpdatesValue`/`UpdatesDetail`/`UpdatesValueTone` for the four states: (1) not checked/unknown (any card availability is null or Unknown), (2) all up to date (every installed card is UpToDate), (3) N updates (N cards with UpdateAvailable), (4) per-card check refresh (calling `CheckCardUpdatesAsync` on one card updates the aggregation). Use `AgentManagementViewModelBuilder` to set up test fixtures. Verify tests compile and pass.

## 8G. Increment 2: Update spec deltas for increment 2

- [x] 8G.1 Update `openspec/changes/agent-detection-settings-page/specs/preferences-ui/spec.md`: modify "CLI Agents page management" to add the UPDATES card to the summary-row enumeration and scenarios; modify "Agent Detection page content" to add the DEFAULTS card to the summary-row enumeration and add the Reset action to the DETECTION section header description and scenarios. Preserve original scenario names. Verify the delta spec is valid markdown.

- [x] 8G.2 Update `openspec/changes/agent-detection-settings-page/specs/settings-theme/spec.md`: modify "Persistent page header" to update CLI Agents to "agents, default-agent, and updates cards" and Agent Detection to "cache TTL, timeout, and defaults cards"; modify "CLI Agents page summary cards" to add the UPDATES card scenarios (value logic, detail logic, tone, refresh triggers); modify "Agent Detection page summary cards" to add the DEFAULTS card scenarios (value, detail). Preserve original scenario names. Verify the delta spec is valid markdown.

## 8H. Increment 2: Update documentation for three-card layout

- [x] 8H.1 Update `docs/architecture/ui.md` line 56: change "on CLI Agents, the agents and default-agent summary cards" to "on CLI Agents, the agents, default-agent, and updates summary cards" and "on Agent Detection, the cache TTL and timeout summary cards" to "on Agent Detection, the cache TTL, timeout, and defaults summary cards". Verify the document remains coherent.

## 9. Build and test gates

- [x] 9.1 Run `dotnet build CLIHub.sln` from the repository root and verify 0 warnings/errors.

- [x] 9.2 Run `dotnet test CLIHub.sln` and verify all tests pass. The test count increases by the new assertions in the navigation and metadata tests.

## 10. Manual smoke verification

### Increment 1

- [ ] 10.1 Launch the application and open Settings. Navigate to the new "Agent Detection" page via the sidebar. Verify:
  - The page appears under "Engines & Repos" immediately after "CLI Agents"
  - The page header shows two summary cards: CACHE TTL (effective value + detail) and TIMEOUT (effective value + detail)
  - The page body shows the DETECTION section with "Cache TTL" and "Version check timeout" fields
  - The fields show the stored values or empty (watermark visible)
  - The fields accept numeric input and reject invalid input (non-numeric, zero, negative)

- [ ] 10.2 From the Agent Detection page, enter a custom cache TTL (e.g., 25) and a custom timeout (e.g., 20). Observe that the summary cards update immediately (if implemented as getters) or remain at their loaded values (if computed in Load). Do not save yet.

- [ ] 10.3 Navigate to the CLI Agents page. Verify:
  - The DETECTION section is gone
  - The MANAGEMENT section (default agent selector) is at the top
  - The AGENTS section header and cards immediately follow MANAGEMENT with no gap
  - The summary row shows two cards: AGENTS and DEFAULT (no DETECTION card)

- [ ] 10.4 Return to Agent Detection. Choose "Save" with valid custom values. Verify the Settings window closes and no validation error appears.

- [ ] 10.5 Reopen Settings → Agent Detection. Verify the custom values are loaded into the fields and the summary cards show "Custom" or similar detail distinguishing them from defaults.

- [ ] 10.6 Clear both fields (empty = use defaults). Choose "Save". Reopen Settings → Agent Detection. Verify the fields are empty (watermarks visible) and the summary cards show "Built-in default" or the default numeric values with appropriate detail.

- [ ] 10.7 Enter an invalid value (e.g., "abc" or "0") in either field and choose "Save". Verify a validation error appears, the window remains open, and the stored preferences are unchanged.

- [ ] 10.8 Choose "Cancel" after entering unsaved values. Reopen Settings → Agent Detection. Verify the fields show the last saved values (changes were discarded).

### Increment 2

- [ ] 10.9 Open Settings → Agent Detection. Verify:
  - The page header now shows three summary cards: CACHE TTL, TIMEOUT, DEFAULTS (left to right)
  - The DEFAULTS card shows the built-in default values (e.g., "15 min · 10 sec") and detail "Used when a field is empty" (or equivalent)
  - The DETECTION section header has a right-aligned "Reset" chip button with the Refresh icon

- [ ] 10.10 With both fields empty (at defaults), verify the Reset button is disabled (dimmed).

- [ ] 10.11 Enter a custom value in either field. Verify the Reset button becomes enabled. Click Reset. Verify both fields are cleared to empty (watermarks visible), the CACHE TTL and TIMEOUT cards update to show "Built-in default", and the Reset button becomes disabled again. Do not save yet.

- [ ] 10.12 Enter custom values in both fields. Click Reset. Verify both fields are cleared. Choose "Cancel". Reopen Settings → Agent Detection. Verify the stored custom values are reloaded (Reset edits the draft; Cancel discarded the draft).

- [ ] 10.13 Navigate to CLI Agents. Verify:
  - The page header now shows three summary cards: AGENTS, DEFAULT, UPDATES (left to right)
  - The UPDATES card shows "Unknown" (or similar) before the first update check, with detail "Run Check updates" (or similar)
  - The UPDATES card uses a muted tone while unknown

- [ ] 10.14 Click "Check updates" in the AGENTS section header. After the check completes, verify:
  - If no updates are available: the UPDATES card shows "None" with detail "All installed agents up to date" and uses a success tone (green)
  - If updates are available: the UPDATES card shows the count (e.g., "2") with detail "2 of 5 installed agents" (or singular "1 of 3 installed agent") and uses an accent tone
  - Per-card update indicators also appear on individual agent cards

- [ ] 10.15 Click "Detect" in the AGENTS section header. After the detection completes, verify the UPDATES card updates to reflect the new agent availability state (stale availability cleared).

- [ ] 10.16 For an agent card with an available update, click its per-card "Check updates" action. Verify the UPDATES card aggregation updates after the per-card check completes.

- [ ] 10.17 Verify the Detect and Check updates chip buttons in the CLI Agents header use the shared ghost-chip style (hover overlay, pressed overlay, dimmed when disabled, consistent with the Reset button on Agent Detection).

- [ ] 10.18 Verify the Reset button on Agent Detection uses the same style as the CLI Agents header chip buttons.

## 11. Validation

- [x] 11.1 Run `openspec validate --all` from the repository root and verify it reports 0 errors. Quote the exact validation result in the implementation report.

## 12. Update graphify (if required by repository tooling)

- [x] 12.1 If the repository's post-edit hook or workflow requires refreshing the graphify knowledge graph, run `graphify update .` from the repository root. Verify the command succeeds with no errors.

---

**Implementation notes:**

- **Increment 1:** Tasks 1-5 are code changes; 6-7 are test and doc updates; 8 covers both spec deltas (preferences-ui and settings-theme); 9-10 are verification; 11 is validation.
- **Increment 2:** Tasks 8A-8H implement the enhanced summary cards (UPDATES, DEFAULTS), the Reset command, the shared chip-button style, spec delta updates, and test/doc updates for the three-card layout. Manual smoke 10.9-10.18 verifies the increment 2 behavior.
- Each task is sized for a fresh session: small, focused, with a compile/test gate.
- The manual smoke tasks verify both increments: the UI reorganization (increment 1) and the enhanced summary cards plus reset action (increment 2).
- Validation (11.1) confirms both spec deltas are correct and the change metadata is valid.
- The graphify step (12.1) is conditional: run it only if the repository's conventions require it (AGENTS.md mentions "After modifying source code, refresh graphify as required by the repository tooling").
