## 1. Create StatusMessageCoordinator class

- [x] 1.1 Create `StatusMessageCoordinator.cs` in `src/CLIHub/ViewModels/` inheriting from `ObservableObject` and implementing `IDisposable`
- [x] 1.2 Add constructor accepting `ProjectPaneController`, `AgentPaneController`, `UpdateControlViewModel` dependencies
- [x] 1.3 Add `CurrentMessage` property with private setter, initialized to "CLIHub ready"
- [x] 1.4 Subscribe to `_projectPane.ProjectsChanged` and `_updateControl.OutcomeReported` in constructor
- [x] 1.5 Implement `Dispose()` to unsubscribe from both events with disposed guard

## 2. Add status reporting methods to coordinator

- [x] 2.1 Add `ReportProjectSelected(string projectName)` setting `CurrentMessage = $"Current project: {projectName}"`
- [x] 2.2 Add `ReportAgentSelected(string agentName)` setting `CurrentMessage = $"Selected agent: {agentName}"`
- [x] 2.3 Add `ReportWindowPinChanged(bool isPinned)` setting message "Window pinned open" or "Window unpinned"
- [x] 2.4 Add `ReportCommandOutcome(string message)` setting `CurrentMessage = message`
- [x] 2.5 Add `ReportFolderOpen(string path, Exception? error = null)` with success/error message logic
- [x] 2.6 Add `ReportNoAgentsFound()` setting the "No agents found..." message
- [x] 2.7 Add private `OnProjectsChanged` handler setting `CurrentMessage = "Projects refreshed"`
- [x] 2.8 Add private `OnUpdateOutcomeReported` handler setting `CurrentMessage = message`

## 3. Register coordinator in DI

- [x] 3.1 Open `src/CLIHub/ServiceRegistration.cs`
- [x] 3.2 Add `services.AddSingleton<StatusMessageCoordinator>();` before `LaunchWindowViewModel` registration
- [x] 3.3 Verify registration order: coordinator before ViewModel

## 4. Update LaunchWindowViewModel to use coordinator

- [x] 4.1 Add `StatusMessageCoordinator statusCoordinator` constructor parameter (after `externalLauncher`, before `actionBuilder`)
- [x] 4.2 Store coordinator in `_statusCoordinator` readonly field
- [x] 4.3 Remove `_statusMessage` field declaration (line 38)
- [x] 4.4 Remove `UpdateControl.OutcomeReported` subscription from constructor (line 78)
- [x] 4.5 Change `StatusMessage` property to `public string StatusMessage => _statusCoordinator.CurrentMessage;` (remove setter)
- [x] 4.6 Forward coordinator's PropertyChanged to ViewModel's PropertyChanged for "CurrentMessage" → "StatusMessage"
- [x] 4.7 Replace `StatusMessage = $"Current project: {value.Name}"` (line 159) with `_statusCoordinator.ReportProjectSelected(value.Name)`
- [x] 4.8 Replace `StatusMessage = $"Selected agent: {value.Name}"` (line 178) with `_statusCoordinator.ReportAgentSelected(value.Name)`
- [x] 4.9 Replace pin status assignment (line 222) with `_statusCoordinator.ReportWindowPinChanged(value)`
- [x] 4.10 Replace command callback `message => StatusMessage = message` (line 326) with `_statusCoordinator.ReportCommandOutcome`
- [x] 4.11 Replace OpenDataFolder success/error assignments (lines 373, 377) with `_statusCoordinator.ReportFolderOpen(root, ex)`
- [x] 4.12 Replace RefreshAgents message (line 402) with `_statusCoordinator.ReportNoAgentsFound()` when `!hasAgents`
- [x] 4.13 Remove `OnUpdateOutcomeReported` method (line 434) — now handled by coordinator
- [x] 4.14 Remove `UpdateControl.OutcomeReported` unsubscription from `Dispose()` (line 421)

## 5. Write tests for StatusMessageCoordinator

- [x] 5.1 Create `StatusMessageCoordinatorTests.cs` in `tests/CLIHub.Tests/ViewModels/`
- [x] 5.2 Test `ReportProjectSelected` sets correct message format
- [x] 5.3 Test `ReportAgentSelected` sets correct message format
- [x] 5.4 Test `ReportWindowPinChanged` for both pinned and unpinned states
- [x] 5.5 Test `ReportCommandOutcome` forwards message exactly
- [x] 5.6 Test `ReportFolderOpen` with success (no exception)
- [x] 5.7 Test `ReportFolderOpen` with error (exception message included)
- [x] 5.8 Test `ReportNoAgentsFound` sets correct message
- [x] 5.9 Test `OnProjectsChanged` event handler updates message
- [x] 5.10 Test `OnUpdateOutcomeReported` event handler updates message
- [x] 5.11 Test `Dispose` unsubscribes from ProjectsChanged (event not handled after dispose)
- [x] 5.12 Test `Dispose` unsubscribes from OutcomeReported (event not handled after dispose)
- [x] 5.13 Test `CurrentMessage` raises PropertyChanged when updated

## 6. Update LaunchWindowViewModel tests

- [x] 6.1 Update existing ViewModel tests to provide `StatusMessageCoordinator` in constructor
- [x] 6.2 Verify StatusMessage property returns coordinator's CurrentMessage
- [x] 6.3 Update tests that check status messages to verify coordinator methods are called instead of property assignments
- [x] 6.4 Remove tests for `OnUpdateOutcomeReported` in ViewModel (moved to coordinator)

## 7. Build and verify

- [x] 7.1 Run `dotnet build CLIHub.sln` from repository root, verify zero errors and warnings
- [x] 7.2 Run `dotnet test CLIHub.sln --no-build` from repository root, verify all tests pass
- [x] 7.3 Manually launch CLIHub, verify status messages display correctly for all scenarios
- [x] 7.4 Test project selection updates status message
- [x] 7.5 Test agent selection updates status message
- [x] 7.6 Test window pin toggle updates status message
- [x] 7.7 Test agent commands (launch/resume) update status with results
- [x] 7.8 Test Open Data Folder updates status (success and error paths if possible)
- [x] 7.9 Test refresh when no agents found displays correct message
- [x] 7.10 Test update check/download displays status messages

## 8. Measure and document

- [x] 8.1 Count LaunchWindowViewModel line count after refactoring with `(Get-Content src\CLIHub\ViewModels\LaunchWindowViewModel.cs).Count`
- [x] 8.2 Verify ~10% reduction (435 → ~390 lines)
- [x] 8.3 Verify StatusMessageCoordinator is ~80-100 lines
- [x] 8.4 Update inline documentation if any references to status message handling changed
