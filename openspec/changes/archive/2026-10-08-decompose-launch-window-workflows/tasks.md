## 1. Extract screen workflows

- [x] 1.1 Add `LaunchCommandCoordinator` and verify validation, result messages, notifications, operation tracking, and refresh through focused unit tests.
- [x] 1.2 Add `LaunchWindowActionBuilder` and verify the generated action labels, commands, separators, and filter state.
- [x] 1.3 Add `IExternalLauncher` and its Windows adapter, then verify the ViewModel reports open success/failure through the port.

## 2. Compose the launch window

- [x] 2.1 Reduce `LaunchWindowViewModel` to screen state/composition and verify existing project-dialog and selection behavior.
- [x] 2.2 Register the extracted workflows and shared `UpdateControlViewModel` in `ServiceRegistration`; verify composition resolves one launch ViewModel graph.
- [x] 2.3 Preserve pane controller refresh/selection behavior and verify project/agent controller tests.

## 3. Documentation and verification

- [x] 3.1 Update architecture and repository-structure documentation to describe launch-window workflow ownership.
- [x] 3.2 Run `dotnet build CLIHub.sln` and require zero warnings and errors.
- [x] 3.3 Run `dotnet test CLIHub.sln` and `openspec validate --specs`.
