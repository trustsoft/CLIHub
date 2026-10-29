# Tasks: Disabled-plugin UI surface for invalid catalog entries

## Stage 1: Model and ViewModel

- [ ] 1.1 Create `InvalidAgentCardItem` model in `src/CLIHub/ViewModels/Agents/InvalidAgentCardItem.cs` with properties: `PluginDirectory`, `Name`, `LogoPath`, `Reason`, `IsEnabled` (always false).
- [ ] 1.2 Add static reason-composition helper `ComposeReason(InvalidPluginEntry)` to `InvalidAgentCardItem` - joins `ValidationResult.Errors` with semicolons, falls back to `ReadResult.Error.Message`, or returns directory path.
- [ ] 1.3 Add `ObservableCollection<InvalidAgentCardItem> InvalidCards { get; }` property to `AgentManagementViewModel`.
- [ ] 1.4 In `AgentManagementViewModel.Load()` after line 293, call `catalog.GetInvalidPlugins()` and populate `InvalidCards` collection with `InvalidAgentCardItem` instances (use `ComposeReason`).
- [ ] 1.5 Verify `RefreshAvailabilitySummary()` still derives counts from `Cards` only (valid-only semantics preserved).

**Gate:** `dotnet build CLIHub.sln` succeeds.

## Stage 2: XAML Invalid-Card Rendering

- [ ] 2.1 In `src/CLIHub/Views/SettingsAgentsPage.xaml` after line 287 (end of valid-cards `ItemsControl`), add a second `ItemsControl` bound to `AgentsState.InvalidCards`.
- [ ] 2.2 Define invalid-card `DataTemplate`: same card `Border` shape (popover background, field stroke, corner radius) with `Opacity="0.6"`.
- [ ] 2.3 Show logo column with default logo placeholder (or `LogoPath` if available, though likely null).
- [ ] 2.4 Show name (from `Name` property; falls back to directory basename in reason).
- [ ] 2.5 Show a danger-styled "Invalid" state chip next to the name (reuse `AgentStateChip` style with `NotInstalled` trigger or create similar).
- [ ] 2.6 Show `Reason` text where "Supported commands" line appears in valid cards, styled as hint text, with `TextTrimming="CharacterEllipsis"` and `MaxWidth` bound to `Sizing.AgentRow.TextMaxWidth` if that resource exists, or a fixed width otherwise. Add tooltip showing full `Reason` text.
- [ ] 2.7 Omit all action buttons (Detect, Check updates, Update, Edit Config) from the invalid-card template.

**Gate:** `dotnet build CLIHub.sln` succeeds; XAML has no errors.

## Stage 3: Tests

- [ ] 3.1 Add view-model test in `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`: `Load_WithInvalidPlugins_PopulatesInvalidCards` - mock `GetInvalidPlugins()` returning one `InvalidPluginEntry` with validation errors; assert `InvalidCards` has one item with composed reason.
- [ ] 3.2 Add test: `Load_WithValidAndInvalidPlugins_SeparatesCardSets` - mock both `GetAllPlugins()` (1 valid) and `GetInvalidPlugins()` (1 invalid); assert `Cards` has 1 valid, `InvalidCards` has 1 invalid.
- [ ] 3.3 Add test: `Load_WithInvalidPlugins_ValidCountsUnchanged` - mock invalid plugins; assert `SupportedAgentCount` and `InstalledAgentCount` derive from `Cards` only, ignoring `InvalidCards`.
- [ ] 3.4 Add test: `Load_AfterReload_RefreshesBothSets` - mock catalog, load, modify `GetInvalidPlugins()` return, raise `PluginsChanged`, assert `InvalidCards` reflects new result.
- [ ] 3.5 Add test: `ComposeReason_FromValidationErrors_JoinsWithSemicolon` - call `InvalidAgentCardItem.ComposeReason()` with 2 validation errors; assert result is "Error1; Error2".
- [ ] 3.6 Add test: `ComposeReason_FromReadError_UsesExceptionMessage` - call with `ReadResult.Error`; assert result is exception message.
- [ ] 3.7 Add test: `ComposeReason_FallbackToDirectory_WhenNoError` - call with null `PartialPlugin`, null errors; assert result contains directory path.

**Gate:** `dotnet test CLIHub.sln` passes; all new tests green.

## Stage 4: Spec Delta

- [ ] 4.1 Create `openspec/changes/plugin-disabled-ui/specs/agent-ui/spec.md` delta file.
- [ ] 4.2 Add new requirement: "Requirement: Invalid plugin entry display" with scenarios covering invalid card visibility, reason composition, visual distinction, no actions, valid cards unchanged, reload behavior.
- [ ] 4.3 Ensure scenario names match the proposal (line-by-line behavior pins).

**Gate:** `openspec validate --all` passes with zero errors.

## Stage 5: Manual Smoke Test

- [ ] 5.1 Create a broken descriptor in `%APPDATA%\CLIHub\plugins\broken-test\plugin.json` with invalid JSON (e.g., missing required `id` field).
- [ ] 5.2 Create a descriptor in `%APPDATA%\CLIHub\plugins\schema-test\plugin.json` with `"schemaVersion": 2`.
- [ ] 5.3 Launch CLIHub, navigate to Settings -> CLI Agents page.
- [ ] 5.4 Verify `broken-test` appears as a disabled card with reason text explaining the validation error (e.g., "Missing required field: id").
- [ ] 5.5 Verify `schema-test` appears as a disabled card with reason "Unsupported schema version 2; maximum supported is 1".
- [ ] 5.6 Verify valid agent cards render normally above the invalid cards, with all actions available.
- [ ] 5.7 Verify "Supported agents" count excludes the invalid entries.
- [ ] 5.8 Verify no action buttons are clickable on invalid cards.
- [ ] 5.9 Delete one invalid descriptor directory, reload the page (or restart CLIHub), verify that invalid card disappears.
- [ ] 5.10 Fix one invalid descriptor (correct the JSON), reload, verify it moves to the valid-cards list.

**Gate:** All smoke steps pass; user confirms invalid entries are visible and distinguishable, valid agents unchanged.

## Stage 6: Finalization

- [ ] 6.1 Run `graphify update .` to refresh the knowledge graph.
- [ ] 6.2 Run full build and test suite: `dotnet build CLIHub.sln && dotnet test CLIHub.sln`.
- [ ] 6.3 Verify `openspec validate --all` passes with zero errors.
- [ ] 6.4 Review that `InvalidCards` collection is properly disposed/cleared in `AgentManagementViewModel.Dispose()` if needed (likely no-op, but confirm no resource leak).

**Gate:** All verification commands succeed; change is ready for approval and commit.
