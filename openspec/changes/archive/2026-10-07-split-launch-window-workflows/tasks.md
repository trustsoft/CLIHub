## 1. Project Workflow

- [x] 1.1 Add `ProjectPaneController` for project collection synchronization, selection identity, add/remove/favorite operations, and current-project access; verify project identity and mutation tests without WPF windows.

## 2. Agent Workflow

- [x] 2.1 Add `AgentPaneController` for agent composition, filtering, selection identity, cache invalidation, refresh, and linked version population cancellation; verify filtering, selection preservation, refresh generation, and cancellation tests.

## 3. ViewModel Integration

- [x] 3.1 Reduce `LaunchWindowViewModel` to presentation state, command wiring, status/notification mapping, and delegation to pane controllers; preserve its existing public binding properties and command behavior.
- [x] 3.2 Register controllers in the composition root and update direct construction/composition tests; verify no WPF window is required to construct either controller.

## 4. Verification

- [x] 4.1 Run `dotnet build CLIHub.sln -c Release` and verify the solution builds without warnings or errors.
- [x] 4.2 Run `dotnet test CLIHub.sln -c Release` and verify all existing and new tests pass.
- [x] 4.3 Run `graphify update .`, validate the change, update `improvements.md`, and archive the completed change.
