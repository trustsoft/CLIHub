## Context

After plugin initialization, `App.OnStartup` loads `AppPreferences`, parses `DefaultRuntime`, sets the process launcher runtime, and refreshes the Windows Run registration from `StartWithWindows`. The underlying `IInteractiveProcessRunner` and `IStartupService` contracts already provide the required operations and failure handling.

## Goals / Non-Goals

**Goals:**

- Move startup preference application out of `App.xaml.cs`.
- Keep the current synchronous order: runtime first, Windows startup registration second.
- Make the sequence testable without constructing a WPF `Application`.
- Keep preference loading in `App.OnStartup`, because the same snapshot controls window visibility and optional startup work.

**Non-Goals:**

- Change preference defaults, parsing, persistence, or settings UI behavior.
- Change `IProcessLauncher` or `IStartupService` APIs.
- Add runtime preference change handling; that remains the responsibility of `PreferenceApplier`.
- Add new exception handling around the existing services.

## Decisions

- Add `IStartupPreferencesApplier` and a WPF application-layer implementation accepting `AppPreferences`.
- The implementation depends on `IInteractiveProcessRunner` for `SetRuntime` and calls `SetRuntime(RuntimeKinds.Parse(preferences.DefaultRuntime))`, then `SetEnabled(preferences.StartWithWindows)` on `IStartupService`.
- The service deliberately ignores the boolean result from `SetEnabled`, matching current startup behavior: `StartupService` logs and returns false without throwing, while startup continues.
- Keep `App.OnStartup` responsible for loading preferences once and for using the same preference snapshot for window visibility and update-check decisions.
- Register the service as a singleton and resolve it through DI.

## Risks / Trade-offs

- **Preference application order could change** -> Add a focused ordered-call test and preserve runtime-before-startup ordering.
- **Startup dependency resolution could regress** -> Add a composition test for the new interface and run the full solution build/test.
- **A new service could duplicate runtime settings logic** -> Limit it to startup-only application; do not modify `PreferenceApplier` in this change.

## Migration Plan

1. Add the interface and implementation.
2. Register the implementation in WPF composition.
3. Replace the two inline startup calls with one service call.
4. Run focused tests, full build, and full test suite.
5. Rollback consists of restoring the inline calls and removing the new registration/types.

## Open Questions

None.
