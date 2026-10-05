## Context

The launch window ViewModel's Exit command and the tray menu Exit item both call `Application.Current.Shutdown()` directly. `App.OnExit` remains responsible for disposing the hotkey, tray, service provider, single-instance guard, and logger.

## Goals / Non-Goals

**Goals:**

- Remove direct WPF application singleton access from active exit command handlers.
- Preserve the same WPF shutdown operation and `App.OnExit` cleanup path.
- Make the launch window exit command testable with a fake lifetime implementation.

**Non-Goals:**

- Change shutdown cleanup order or exception handling.
- Move shutdown orchestration out of `App.OnExit`.
- Change the retained legacy `MainWindow` path.
- Introduce a cross-platform application host abstraction.

## Decisions

- Add `IApplicationLifetime` with a single `Shutdown()` operation in the WPF application layer.
- Implement it with `WpfApplicationLifetime`, delegating to `Application.Current.Shutdown()`.
- Inject one singleton lifetime service into `LaunchWindowViewModel` and `TrayIconController`.
- Keep the interface deliberately small; no restart, dispatcher, or exit-code operations are included in this change.

## Risks / Trade-offs

- **The WPF adapter may be invoked without an Application** -> This matches the current assumption at both UI entry points; test the ViewModel against a fake instead of constructing WPF Application.
- **One call site could remain coupled** -> Search active source after implementation and verify both commands resolve the interface through DI.

## Migration Plan

1. Add the interface and WPF implementation.
2. Register it in WPF composition and inject it into the two active UI components.
3. Replace direct shutdown calls and add focused tests.
4. Run full build and test suite.
5. Rollback consists of restoring the direct calls and removing the registration/types.

## Open Questions

None.
