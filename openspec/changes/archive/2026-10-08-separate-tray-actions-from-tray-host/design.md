## Context

The tray controller currently receives `TaskbarIcon`, `TrayMenuBuilder`, and logging, then wires the icon's menu and tooltip. The builder itself owns project selection, agent launch, project-folder dialog, settings, release notes, update download signaling, launch-window display, and application shutdown. `UpdateDownloadNotifier` also reaches into the concrete tray controller.

The launch window already invokes the shared `IAgentCommandWorkflow`; the tray action layer must continue using that same workflow rather than creating a second launch path.

## Goals / Non-Goals

**Goals:**

- Keep WPF host ownership and UI-thread concerns in the tray host.
- Make application actions independently testable with ports and fakes.
- Have menu creation consume a prepared `TrayMenuState` containing current project, recent projects, launchable agents, and update state.
- Preserve refresh and disposal behavior.

**Non-Goals:**

- Redesigning the tray menu or changing user-visible labels.
- Moving WPF `ContextMenu` construction out of the application entirely.
- Changing agent workflow, update download, or project persistence contracts.

## Decisions

### Use a prepared state and action handler

Introduce a `TrayMenuState` model and `ITrayActions` application-facing port. `TrayActions` gathers state from existing services and forwards commands to the existing workflows and launchers. `TrayMenuBuilder` receives the state and action callbacks needed to construct WPF menu items; it no longer owns business service dependencies.

The handler owns project-folder dialog and warning notification through narrow ports/adapters, keeping those concerns out of the host while preserving current behavior.

### Keep host lifecycle explicit

`TrayIconController` remains the owner of `TaskbarIcon` and its menu instance. It asks the action handler for state, builds the menu, and disposes the icon and subscriptions. `UpdateDownloadNotifier` depends on a narrow tray UI port rather than the concrete host.

### Preserve command identity

Tray agent actions call `IAgentCommandWorkflow.ExecuteAsync` with the same selected project, plugin, and `AgentCommandKind.Launch` values used today. The handler does not duplicate launch logic.

## Risks / Trade-offs

- [Risk] WPF event handlers can capture stale state. -> Build menus from a fresh `TrayMenuState` on every refresh.
- [Risk] Host disposal can leave event subscriptions alive. -> Keep the existing update event subscription lifecycle in the host and cover disposal in tests.
- [Risk] Moving folder-dialog handling changes exception behavior. -> Preserve current warning text and cancellation behavior in the action adapter.

## Migration Plan

1. Extract tray state/action contracts and handler from the current builder/controller behavior.
2. Reduce `TrayMenuBuilder` to menu projection over prepared state and callbacks.
3. Reduce `TrayIconController` to host lifecycle and connect the narrow update UI port.
4. Register adapters, add focused tests, run verification, and archive the change.
