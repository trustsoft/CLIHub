## Context

`TrayIconController` currently creates the `TaskbarIcon`, subscribes to project and update events, refreshes the tray icon menu, and contains all menu construction methods. Menu construction reads current application state and attaches callbacks for project selection, agent launch, settings, release notes, updates, showing the launch window, and application exit.

The previous agent-launch workflow extraction provides an application-level `IAgentCommandWorkflow`, so menu construction can be moved without reintroducing direct process or command-service dependencies.

## Goals / Non-Goals

**Goals:**

- Separate tray menu composition from `TaskbarIcon` lifecycle management.
- Preserve the existing menu hierarchy, labels, enabled states, callbacks, update state handling, and project refresh behavior.
- Keep menu construction in the WPF project and dependent on narrow existing application contracts.
- Make menu construction independently testable through dependency injection and fakes.

**Non-Goals:**

- Change the tray menu UX or reorganize its entries.
- Move `TaskbarIcon` creation, event subscriptions, or refresh scheduling out of `TrayIconController`.
- Introduce a Core tray abstraction.
- Change project selection, agent command validation, update workflow, settings, release notes, or application shutdown behavior.
- Update the legacy `MainWindow` path.

## Decisions

- Add a `TrayMenuBuilder` class with a method that returns a new `ContextMenu` for the current state, for example `Build()`. The exact public contract should follow local naming and XML documentation conventions.
- Move `BuildContextMenu`, `BuildLaunchAgentMenu`, update menu item creation, and their directly related action wiring into the builder.
- Inject the builder into `TrayIconController`; `RefreshMenu` replaces the `TaskbarIcon.ContextMenu` with the builder result.
- Keep state reads and action callbacks in the builder because they define menu state and actions, while the controller remains responsible for when rebuilding is required.
- Register `TrayMenuBuilder` as a singleton. Its dependencies remain the existing singleton services used by the tray.
- Preserve update-state refresh wiring so update menu state is rebuilt on the same events as before.

## Risks / Trade-offs

- **Callbacks may accidentally capture controller state** -> Keep callbacks bound to the existing injected launchers, workflows, project service, and lifetime service; add focused tests for important menu actions.
- **Menu state may become stale** -> Keep all existing `RefreshMenu` triggers in `TrayIconController` and rebuild a fresh menu on each refresh.
- **Constructor dependency count remains high in the builder** -> Accept this as an accurate representation of menu actions; further workflow extraction is outside this change.

## Migration Plan

1. Add the builder contract/implementation and focused tests.
2. Move menu construction methods and related dependencies from `TrayIconController`.
3. Inject and register the builder, then keep controller refresh/lifecycle behavior intact.
4. Run focused tests, `dotnet build CLIHub.sln -c Release`, `dotnet test CLIHub.sln -c Release`, and OpenSpec validation.
5. Rollback consists of restoring menu construction methods to `TrayIconController` and removing the builder registration/type.

## Open Questions

None.
