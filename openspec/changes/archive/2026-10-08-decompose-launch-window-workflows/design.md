## Context

See `proposal.md` for motivation. `ProjectPaneController` and `AgentPaneController` already own their respective collection synchronization and refresh behavior. The remaining ViewModel-specific workflows include agent command execution, menu construction, and opening the data folder.

## Goals / Non-Goals

**Goals:**

- Preserve binding names, selection identity, status text, dialogs, notifications, and refresh behavior.
- Make agent command validation and result presentation testable without creating the launch-window ViewModel.
- Keep WPF and OS launching behind application-layer ports.
- Reduce direct dependencies and responsibilities in `LaunchWindowViewModel`.

**Non-Goals:**

- Change project or agent pane behavior, update controls, launch-window layout, or menu contents.
- Move WPF presentation code into `CLIHub.Core`.
- Introduce a general-purpose command bus or new external package.

## Decisions

- Keep the existing pane controllers as the explicit project and agent pane boundaries; add focused tests where their behavior is not already directly covered.
- Add `LaunchCommandCoordinator` to own command validation, application-operation tracking, unexpected-failure handling, result-to-status mapping, version notifications, and post-command agent refresh. It receives callbacks for binding-facing status and refresh effects, keeping the coordinator independent of WPF.
- Add `LaunchWindowActionBuilder` to construct the existing per-pane `MenuAction` collections and return the filter action whose checked-state changes remain bound to the ViewModel preference.
- Add `IExternalLauncher` and a Windows implementation that opens a supplied path. The ViewModel retains user-facing success/failure status handling around this port.
- Register the new application services in `ServiceRegistration`. Register `UpdateControlViewModel` there as the launch window's existing shared update-control instance and inject it into `LaunchWindowViewModel` rather than manually constructing it.

## Risks / Trade-offs

- Moving command status mapping can alter user-visible messages → preserve current strings and add coordinator tests for missing inputs, unsupported commands, success, and failure.
- A callback-based coordinator can become a broad UI facade → limit callbacks to status reporting and agent refresh; keep project selection and menus outside it.
- Registering the update control separately could create a second state owner → register one singleton instance and inject that same instance into the singleton launch ViewModel.

## Migration Plan

This is an internal refactor with no persisted-data or user-preference migration. Rollback consists of reverting the application services, ViewModel composition changes, and focused tests together.
