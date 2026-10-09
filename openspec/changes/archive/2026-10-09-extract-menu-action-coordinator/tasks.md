# Tasks: Extract MenuActionCoordinator

## 1. Create MenuActionCoordinator class

- [x] 1.1 Create `src/CLIHub/ViewModels/MenuActionCoordinator.cs` with class inheriting from ObservableObject and implementing IDisposable
- [x] 1.2 Add constructor accepting 9 ICommand parameters (addProject, removeProject, toggleFavorite, refresh, launch, resume, initialize, update, showVersion), LaunchWindowActionBuilder, and bool initialFilterState
- [x] 1.3 Add readonly field: `_filterAction` (MenuAction)
- [x] 1.4 Add public properties: `ProjectsActions` (IReadOnlyList<MenuAction>), `AgentsActions` (IReadOnlyList<MenuAction>)
- [x] 1.5 Add public property: `ShowOnlyProjectAgents` (bool) that returns _filterAction.IsChecked (with setter that updates _filterAction.IsChecked)
- [x] 1.6 In constructor, call `actionBuilder.Build(...)` with all commands and initial filter state
- [x] 1.7 Store result.Projects in ProjectsActions, result.Agents in AgentsActions, result.FilterAction in _filterAction
- [x] 1.8 Subscribe to `_filterAction.PropertyChanged += OnFilterActionChanged`

## 2. Implement filter state notification

- [x] 2.1 Implement `OnFilterActionChanged(object? sender, PropertyChangedEventArgs e)` private method
- [x] 2.2 Check if `e.PropertyName == nameof(MenuAction.IsChecked)`
- [x] 2.3 If true, call `OnPropertyChanged(nameof(ShowOnlyProjectAgents))` to notify subscribers

## 3. Implement disposal

- [x] 3.1 Implement `Dispose()` method with _disposed guard
- [x] 3.2 Unsubscribe from `_filterAction.PropertyChanged -= OnFilterActionChanged`

## 4. Update LaunchWindowViewModel

- [x] 4.1 Remove `_actionBuilder` field (no longer needed)
- [x] 4.2 Remove `_filterAction` field (coordinator owns it now)
- [x] 4.3 Add `_menuCoordinator` field
- [x] 4.4 In constructor, create MenuActionCoordinator with all required dependencies: `new MenuActionCoordinator(AddProjectCommand, RemoveProjectCommand, ToggleFavoriteCommand, RefreshCommand, LaunchCommand, ResumeCommand, InitCommand, UpdateCommand, VersionCommand, _actionBuilder, preferences.ShowOnlyProjectAgents)`
- [x] 4.5 Change `ProjectsActions` property to expression-bodied: `=> _menuCoordinator.ProjectsActions;`
- [x] 4.6 Change `AgentsActions` property to expression-bodied: `=> _menuCoordinator.AgentsActions;`
- [x] 4.7 Remove `BuildActions()` method
- [x] 4.8 Remove `OnFilterActionChanged()` method
- [x] 4.9 Remove `BuildActions()` call from constructor
- [x] 4.10 In `Dispose()` method, add `_menuCoordinator.Dispose();`
- [x] 4.11 In `Dispose()` method, remove `_filterAction.PropertyChanged -= OnFilterActionChanged;` line

## 5. Update LaunchWindowViewModel tests

- [x] 5.1 Open `tests/CLIHub.Tests/ViewModels/LaunchWindowViewModelDialogTests.cs`
- [x] 5.2 Update test fixture to include LaunchWindowActionBuilder in constructor calls
- [x] 5.3 Verify all existing tests still pass

## 6. Build and verify

- [x] 6.1 Run `dotnet build CLIHub.sln` from repository root, verify zero errors and warnings
- [x] 6.2 Run `dotnet test CLIHub.sln --no-build` from repository root, verify all tests pass (535 tests)

## 7. Measure results

- [x] 7.1 Count MenuActionCoordinator line count: 123 lines
- [x] 7.2 Verify LaunchWindowViewModel reduced by ~65 lines
- [x] 7.3 Confirm all menu actions work correctly in manual testing
