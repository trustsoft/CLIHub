## Context

See `proposal.md` for motivation. `PreferenceApplier` currently calls `IProcessLauncher`, a concrete `GlobalHotkeyService`, `IStartupService`, and a concrete `LaunchWindowViewModel`. The previous Settings change now delegates save/application to this service, so its constructor boundary is exercised by application code rather than only by a WPF event handler.

The launch window already raises property notifications when its display-style state changes. The hotkey service already owns the important failure behavior: failed re-registration restores the previous combination.

## Goals / Non-Goals

**Goals:**

- Make `PreferenceApplier` constructible with test doubles and no concrete WPF classes.
- Give runtime, hotkey, startup, and display-style application independent ports.
- Preserve logging, return values, and hotkey rollback semantics.
- Keep the existing `LaunchWindowViewModel` display state and bindings unchanged.

**Non-Goals:**

- Moving `PreferenceApplier` into a new project or changing the Core process aggregate yet.
- Redesigning global hotkey registration or changing the Settings workflow.
- Introducing a general notification bus.
- Removing `GlobalHotkeyService` or `LaunchWindowViewModel` from WPF composition.

## Decisions

### Define narrow ports at the application boundary

Use `IRuntimePreferenceTarget`, `IGlobalHotkeyService`, `IStartupService`, and `IPathDisplayStyleTarget`. `PreferenceApplier` depends only on these interfaces. The runtime target adapter delegates to the existing process launcher; the launch ViewModel implements the display-style target and keeps its existing notification behavior.

An alternative was to inject `IServiceProvider` and resolve concrete services lazily. That would hide the dependency graph and make the application service harder to test, while still retaining the same coupling.

### Preserve hotkey failure ownership

`PreferenceApplier.ApplyHotkey` remains a thin adapter around `IGlobalHotkeyService.ReRegister`. It returns the service result and keeps existing success/failure logging. The hotkey service remains responsible for restoring the previous registration.

### Keep runtime adapter small

The runtime target adapter exposes only `SetRuntime`, even though the underlying `IProcessLauncher` supports more operations. This establishes the intended dependency rule without changing the broader Core aggregate contract in this change.

## Risks / Trade-offs

- [Risk] A port registration can accidentally resolve the WPF implementation too early. -> Register adapters explicitly and test service descriptors/port resolution without resolving a launch window.
- [Risk] Display state stops notifying bindings if the ViewModel implementation changes incorrectly. -> Keep the existing property setter and add a direct target test through the ViewModel property notification path.
- [Risk] Hotkey rollback behavior is duplicated in the adapter. -> Do not reimplement rollback; only forward `ReRegister` and its result.

## Migration Plan

1. Add the narrow ports and runtime adapter.
2. Update `PreferenceApplier`, DI, and `LaunchWindowViewModel` interface implementation.
3. Add direct applier and registration tests.
4. Run build, tests, graphify update, validate, and archive the refactor change.
