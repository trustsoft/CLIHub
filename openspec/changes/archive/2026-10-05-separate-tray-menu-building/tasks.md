## 1. Add Tray Menu Builder

- [x] 1.1 Add the builder contract/implementation and move current-project, recent-project, agent-launch, settings, release-notes, update, show-window, and exit menu composition into it without changing labels or behavior.
- [x] 1.2 Add focused tests or composition checks covering menu creation, project selection callbacks, agent-launch callbacks, update item state, and exit/show actions.

## 2. Integrate With Tray Lifecycle

- [x] 2.1 Inject the builder into `TrayIconController`, preserve all existing refresh triggers and `TaskbarIcon` lifecycle behavior, and register the builder as a singleton.
- [x] 2.2 Verify legacy `MainWindow`, project service, agent workflow, update workflow, settings, release notes, and application lifetime contracts remain unchanged.

## 3. Verify Preserved Behavior

- [x] 3.1 Run `dotnet build CLIHub.sln -c Release`.
- [x] 3.2 Run `dotnet test CLIHub.sln -c Release`.
- [x] 3.3 Run `openspec validate separate-tray-menu-building` and verify the change remains explicitly spec-free.
