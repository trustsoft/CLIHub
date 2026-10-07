## 1. Tray Application Boundary

- [x] 1.1 Add tray menu state and action contracts that expose current project, recent projects, launchable agents, update state, and host actions without concrete tray dependencies.
- [x] 1.2 Extract tray application actions over existing project, plugin, agent workflow, update, launcher, lifetime, and notification ports; verify agent actions use `IAgentCommandWorkflow`.

## 2. WPF Host And Menu Projection

- [x] 2.1 Reduce `TrayMenuBuilder` to a state-and-callback menu projection while preserving labels, ordering, disabled states, and update event behavior.
- [x] 2.2 Reduce `TrayIconController` to `TaskbarIcon` ownership, menu refresh, visibility, notifications, and disposal; adapt update notification to a narrow host port.
- [x] 2.3 Update DI registrations and verify no tray action handler depends on `TaskbarIcon` or `TrayIconController`.

## 3. Verification

- [x] 3.1 Add focused tests for state/action forwarding, menu state projection, refresh, and disposal.
- [x] 3.2 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`.
- [x] 3.3 Run `graphify update .`, validate the change, update `improvements.md`, and archive it.
