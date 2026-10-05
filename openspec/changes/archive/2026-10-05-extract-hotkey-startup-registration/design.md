## Context

`App.OnStartup` currently reads `IPreferencesStore`, parses the configured hotkey with `HotkeyParser`, logs a warning for invalid input, substitutes `HotkeyParser.Default`, resolves `GlobalHotkeyService`, and registers the definition. `GlobalHotkeyService` requires a WPF window handle, which makes the orchestration difficult to unit test directly.

## Goals / Non-Goals

**Goals:**

- Move hotkey preference parsing and startup registration out of `App.xaml.cs`.
- Preserve valid parsing, invalid-value warning, default fallback, and registration order.
- Keep the already loaded `AppPreferences` snapshot as the input, avoiding another configuration read during startup.
- Introduce the smallest interface needed to mock the global registration boundary.

**Non-Goals:**

- Change `HotkeyParser`, supported hotkey syntax, or the default hotkey.
- Change `GlobalHotkeyService` Win32 behavior, re-registration behavior, or disposal behavior.
- Migrate `PreferenceApplier` to the new interface; that is outside this startup extraction.
- Add hotkey availability retries or user-facing startup notifications.

## Decisions

- Add `IHotkeyStartupRegistrar` with an `Register(AppPreferences preferences)` operation in the WPF application layer.
- Add `IGlobalHotkeyService` as a narrow registration seam implemented by `GlobalHotkeyService`; it exposes only the startup-needed `Register` operation.
- The startup registrar uses `HotkeyParser.TryParse`; on failure it logs the same warning and uses `HotkeyParser.Default`.
- `App.OnStartup` resolves the existing concrete `GlobalHotkeyService` for its shutdown field, then delegates registration to `IHotkeyStartupRegistrar` using the already loaded preferences.
- Register both the concrete hotkey service and its interface alias as the same singleton instance.

## Risks / Trade-offs

- **The new seam could diverge from the concrete hotkey service** -> Bind the interface to the existing singleton and keep `GlobalHotkeyService.Register` as the only implementation.
- **Invalid fallback behavior could change** -> Preserve the exact warning and default definition and add explicit valid/invalid tests.
- **DI registration could create a second hotkey service** -> Register the interface through `GetRequiredService<GlobalHotkeyService>()` rather than constructing another instance.

## Migration Plan

1. Add the startup registrar and global hotkey registration interface.
2. Register the interface alias and startup registrar in WPF composition.
3. Replace `RegisterGlobalHotkey` in `App.OnStartup` while preserving the concrete field for shutdown.
4. Run focused tests, full build, and full test suite.
5. Rollback consists of restoring the inline method and removing the new seam/registrar types.

## Open Questions

None.
