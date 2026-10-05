## 1. Add Shared Agent Workflow

- [x] 1.1 Add `IAgentCommandWorkflow` and its delegation implementation; verify plugin, project path, command kind, and cancellation token forwarding with focused tests.
- [x] 1.2 Register the workflow and route active launch-window and tray agent paths through it while preserving status and warning presentation; verify DI resolution and existing UI result mapping.

## 2. Verify Preserved Behavior

- [x] 2.1 Verify `IAgentCommandService`, legacy `MainWindow`, command result semantics, and project preconditions remain unchanged.
- [x] 2.2 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`; verify the full solution remains green.
- [x] 2.3 Run `openspec validate extract-agent-launch-workflow` and verify the change has no spec deltas.
