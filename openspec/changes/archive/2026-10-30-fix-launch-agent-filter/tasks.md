# Implementation Tasks

## Stage 1: Delete MenuActionCoordinator

- [x] 1.1 Delete `src/CLIHub/ViewModels/Menu/MenuActionCoordinator.cs`
- [x] 1.2 Remove the `menuCoordinator` field from `src/CLIHub/ViewModels/Launch/LaunchWindowViewModel.cs:27`
- [x] 1.3 Remove the `MenuActionCoordinator` construction from the same file (lines 96-106)
- [x] 1.4 Change `ProjectsActions` property (line 135) to return directly from the owned `LaunchWindowActions`: change `=> this.menuCoordinator.ProjectsActions` to `=> this.actions.Projects`
- [x] 1.5 Change `AgentsActions` property (line 140) to return directly from the owned `LaunchWindowActions`: change `=> this.menuCoordinator.AgentsActions` to `=> this.actions.Agents`
- [x] 1.6 Add a `private readonly LaunchWindowActions actions;` field to hold the instance returned by `actionBuilder.Build(...)`
- [x] 1.7 After line 106 (after the `MenuActionCoordinator` construction is removed), add `this.actions = this.actionBuilder.Build(this.AddProjectCommand, this.RemoveProjectCommand, this.ToggleFavoriteCommand, this.RefreshCommand, this.windowActions.LaunchCommand, this.windowActions.ResumeCommand, this.windowActions.UpdateCommand, this.windowActions.VersionCommand, preferences.ShowOnlyProjectAgents);`
- [x] 1.8 Build: `dotnet build CLIHub.sln`
- [x] 1.9 Test: `dotnet test CLIHub.sln`

## Stage 2: Restore bidirectional filter sync

- [x] 2.1 After the `actions` field assignment in the constructor (added in 1.7), subscribe to the filter action: `this.actions.FilterAction.PropertyChanged += this.OnFilterActionChanged;`
- [x] 2.2 Add the `OnFilterActionChanged` subscription handler after the `OnStatusCoordinatorPropertyChanged` handler (around line 360): `private void OnFilterActionChanged(object? sender, PropertyChangedEventArgs e) { if (e.PropertyName == nameof(MenuAction.IsChecked) && this.actions.FilterAction.IsChecked != this.ShowOnlyProjectAgents) { this.ShowOnlyProjectAgents = this.actions.FilterAction.IsChecked; } }`
- [x] 2.3 In the `ShowOnlyProjectAgents` setter (lines 188-203), after the `RefreshAgents()` call and before the closing brace, add the mirror write-back: `if (this.actions.FilterAction.IsChecked != value) { this.actions.FilterAction.IsChecked = value; }`
- [x] 2.4 In the `Dispose` method (around line 381), before the existing `this.statusCoordinator.PropertyChanged -= this.OnStatusCoordinatorPropertyChanged;` line, add: `this.actions.FilterAction.PropertyChanged -= this.OnFilterActionChanged;`
- [x] 2.5 Build: `dotnet build CLIHub.sln`
- [x] 2.6 Test: `dotnet test CLIHub.sln`

## Stage 3: Add regression tests

- [x] 3.1 Create `tests/CLIHub.Tests/ViewModels/Launch/LaunchWindowViewModelFilterTests.cs` with a test class `LaunchWindowViewModelFilterTests`
- [x] 3.2 Add test `ToggleFilterAction_UpdatesShowOnlyProjectAgents_PersistsPreference_RefreshesAgents`: build a VM with `LaunchWindowViewModelBuilder.BuildWithMocks()`, capture the mocked `IPreferencesStore`, clear mock invocations after construction, locate the filter action via `viewModel.AgentsActions.Single(action => action.IsCheckable)`, toggle its `IsChecked` from `false` to `true`, then assert: `viewModel.ShowOnlyProjectAgents` is `true`; `IPreferencesStore.Update` was called exactly once and the callback sets `ShowOnlyProjectAgents = true`; the agent pane actually refreshed (assert a fresh `IAgentDetectionService.IsInstalledInSystem` call, or an equivalent observable from the mocked catalog/detection pair, occurred exactly once after the clear).
- [x] 3.3 Add test `SetShowOnlyProjectAgents_MirrorsToFilterAction`: build a VM with `ShowOnlyProjectAgents` initially `false`, set `viewModel.ShowOnlyProjectAgents = true`, locate the filter action via `viewModel.AgentsActions.Single(action => action.IsCheckable)`, assert `action.IsChecked` is `true`
- [x] 3.4 Add test `Constructor_SeedsShowOnlyProjectAgents_DoesNotPersist`: build a VM with preferences `ShowOnlyProjectAgents = true` passed to the mocked preferences store, locate the filter action, verify the VM's `ShowOnlyProjectAgents` is `true` and the filter action's `IsChecked` is `true` (the action builder receives the preference value), and verify `preferencesStore.Update` was never called during construction (the `suppressFilterChange` guard prevented persistence)
- [x] 3.5 Add test `ToggleFilterAction_OnePersistenceCall_OneRefresh`: build a VM, count the number of `preferencesStore.Update` calls and agent pane refreshes, toggle the filter action once, assert exactly one `Update` call and one refresh occurred (no loop)
- [x] 3.6 Build: `dotnet build CLIHub.sln`
- [x] 3.7 Test: `dotnet test CLIHub.sln` — confirm all new tests pass and no existing tests regressed

## Stage 4: Verify the spec delta

- [x] 4.1 Open `openspec/changes/fix-launch-agent-filter/specs/agent-availability-display/spec.md` and confirm it is a `## MODIFIED Requirements` delta for "Hide unavailable agents (optional filter)".
- [x] 4.2 Compare it with `openspec/specs/agent-availability-display/spec.md` (lines 65-96): every original scenario name is present verbatim, and the new scenario "Launch window toggle applies filter immediately" is the only addition.
- [x] 4.3 Check that the scenario's WHEN/THEN lines match the implemented behavior (toggle refreshes the list, persists the preference, and does not loop); if the implementation had to differ, update the delta rather than weakening it.

## Stage 5: Final gates

- [x] 5.1 Build: `dotnet build CLIHub.sln`
- [x] 5.2 Test: `dotnet test CLIHub.sln` — confirm 0 failures
- [x] 5.3 Validate: `openspec validate --all` — confirm the change passes validation
