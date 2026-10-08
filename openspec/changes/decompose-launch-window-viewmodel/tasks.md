## 1. Strengthen ProjectPaneController with Commands

- [ ] 1.1 Add `ICommand AddCommand { get; }` property to ProjectPaneController exposing existing AddProject workflow, verify command property compiles and follows interface pattern
- [ ] 1.2 Add `ICommand RemoveCommand { get; }` property to ProjectPaneController exposing existing RemoveProject workflow with CanExecute checking selected project, verify command property and CanExecute logic
- [ ] 1.3 Add `ICommand ToggleFavoriteCommand { get; }` property to ProjectPaneController exposing existing ToggleFavorite workflow with CanExecute checking selected project, verify command property and CanExecute logic
- [ ] 1.4 Implement commands as RelayCommand instances in ProjectPaneController constructor or as lazy properties, verify commands execute existing controller methods

## 2. Strengthen AgentPaneController with Refresh Logic

- [ ] 2.1 Update `AgentPaneController.Refresh(project, filterToAvailable)` signature to accept filter parameter if not already present, verify method signature accepts both project context and availability filter
- [ ] 2.2 Move agent filtering logic from LaunchWindowViewModel.RefreshAgents into AgentPaneController.Refresh implementation, verify filtered agent list matches current behavior (ShowOnlyProjectAgents filter applied)
- [ ] 2.3 Add `ICommand RefreshCommand { get; }` property to AgentPaneController exposing refresh workflow, verify command triggers agent refresh with current filter state

## 3. Reduce LaunchWindowViewModel to Composition Layer

- [ ] 3.1 Replace `AddProjectCommand` in LaunchWindowViewModel with pass-through to `_projectPane.AddCommand` or direct controller exposure, verify XAML binding still works without ViewModel wrapper
- [ ] 3.2 Replace `RemoveProjectCommand` in LaunchWindowViewModel with pass-through to `_projectPane.RemoveCommand`, verify command CanExecute updates on selection changes
- [ ] 3.3 Replace `ToggleFavoriteCommand` in LaunchWindowViewModel with pass-through to `_projectPane.ToggleFavoriteCommand`, verify command CanExecute updates on selection changes
- [ ] 3.4 Simplify `RefreshCommand` in LaunchWindowViewModel to call `_agentPane.RefreshCommand` or `_agentPane.Refresh(...)` with current context, verify refresh behavior preserved
- [ ] 3.5 Update `RefreshAgents()` in LaunchWindowViewModel to call strengthened `_agentPane.Refresh(selectedProject, showOnlyProjectAgents)` instead of manual filter logic, verify agent list updates correctly on filter/selection changes
- [ ] 3.6 Update `SelectedProject` setter to call `_agentPane.Refresh(...)` with new project context, verify agent list refreshes when project selection changes
- [ ] 3.7 Update `ShowOnlyProjectAgents` setter to call `_agentPane.Refresh(...)` with new filter state, verify agent list filters correctly when toggle changes
- [ ] 3.8 Remove duplicate AddProject/RemoveProject/ToggleFavorite/RefreshAgents methods from LaunchWindowViewModel (now handled by controller commands), verify ViewModel no longer contains these private methods
- [ ] 3.9 Verify `BuildActions()` still works with delegated commands (action builder receives correct command references), verify menu actions enable/disable correctly based on selection
- [ ] 3.10 Verify agent command execution (LaunchCommand, ResumeCommand, etc.) still delegates to `_launchCommandCoordinator` without changes, verify commands execute correctly with selected agent context

## 4. Update Tests for Decomposed ViewModel

- [ ] 4.1 Update ProjectPaneController tests to cover new AddCommand, RemoveCommand, ToggleFavoriteCommand properties, verify commands execute expected workflows and CanExecute logic
- [ ] 4.2 Update AgentPaneController tests to cover strengthened Refresh(project, filterToAvailable) method and RefreshCommand property, verify filtering logic moved from ViewModel works correctly
- [ ] 4.3 Update LaunchWindowViewModel tests to verify pass-through commands delegate to controllers instead of executing logic directly, verify ViewModel acts as thin composition layer
- [ ] 4.4 Verify existing integration tests (if any) still pass with decomposed structure, especially tests covering selection changes, menu actions, and agent commands
- [ ] 4.5 Add test for ViewModel Dispose removing event subscriptions (ProjectsChanged, UpdateControl.OutcomeReported), verify disposal doesn't leave dangling handlers

## 5. Build and Verify

- [ ] 5.1 Run `dotnet build CLIHub.sln` from repository root, verify build succeeds with zero errors and zero warnings
- [ ] 5.2 Run `dotnet test CLIHub.sln --no-build` from repository root, verify all tests pass (expect 526+ total)
- [ ] 5.3 Manually launch CLIHub in Debug mode, verify launch window opens normally, verify Projects and Agents panes populate correctly
- [ ] 5.4 Test project commands: Add Project dialog, Remove Project (with selection), Toggle Favorite star, Refresh, verify all work identically to before decomposition
- [ ] 5.5 Test agent filter: toggle "Show only project agents", verify agent list filters correctly, verify preference persists across restarts
- [ ] 5.6 Test agent commands: Launch, Resume, Init, Update, Version with selected agent, verify commands execute through coordinator, verify status messages update correctly
- [ ] 5.7 Test menu actions: verify Projects Actions and Agents Actions menus populate correctly, verify enable/disable states update on selection changes
- [ ] 5.8 Verify path display style changes (via Agents Actions menu), verify preference persists and formatting updates correctly
- [ ] 5.9 Verify window-level commands still work: Open Data Folder, Open Settings, Exit, verify no regressions

## 6. Measure and Document

- [ ] 6.1 Count LaunchWindowViewModel final line count with `(Get-Content src\CLIHub\ViewModels\LaunchWindowViewModel.cs).Count`, verify reduction from 421 lines to ~200-250 lines (~45% reduction target)
- [ ] 6.2 Update any inline documentation or architecture docs referencing LaunchWindowViewModel responsibilities, verify docs reflect new composition-layer-only role
- [ ] 6.3 Verify no console errors or warnings in Debug output during manual testing, verify logs show normal operation
