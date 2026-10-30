# Proposal: Fix Duplicate Theme Log

## Why

A single Settings save that changes the theme produces two identical `Theme set to {Theme}` log lines at Information level because both `PreferenceApplier.ApplyTheme` (line 115) and `WpfThemePreferenceTarget.SetTheme` (line 41) log the same success message.

Both log sites were introduced in commit `1308a4a feat(theme): apply theme changes without a restart`.

A second defect: `PreferenceApplier.ApplyTheme` logs the success line unconditionally after calling `SetTheme`, even when the palette fails to load. In that failure case, the target logs a warning `"Could not apply theme {Theme}; the previous palette was kept"` (line 45-47), yet the applier still logs the misleading success message.

## What

Align theme logging with the existing patterns used by `ApplyHotkey` and `ApplyStartWithWindows` (bool-returning applier methods that log success or failure based on the result) and with `WpfAccentPreferenceTarget` (which has no logger dependency):

1. Change `IThemePreferenceTarget.SetTheme` to return `bool` — true when the palette was applied or the base palette was intentionally kept, false when the palette dictionary could not be loaded.
2. Remove the logger dependency from `WpfThemePreferenceTarget` entirely (like `WpfAccentPreferenceTarget`):
   - Return the `PaletteSwitcher.Apply(...)` result (which returns `bool` with the correct success/failure semantics).
   - Remove all logging (no Information, no Warning).
3. Update `PreferenceApplier.ApplyTheme` to log exactly one line per call:
   - `LogInformation("Theme set to {Theme}", theme)` when `SetTheme` returns true.
   - `LogWarning("Could not apply theme {Theme}; the previous palette was kept", theme)` when false.
4. Keep the auto-wired `IThemePreferenceTarget` registration (`ServiceRegistration.cs` line 166) unchanged — constructor injection resolves the remaining `PaletteSwitcher` parameter from the singleton registered at line 167, so no registration edit is needed.
5. Update affected tests:
   - `WpfThemePreferenceTargetTests.cs`: remove the logger mock, assert the `false` return, and keep the class-doc comment accurate (logging moved to applier).
   - `PreferenceApplierTests.cs`: update `ApplyTheme_ForwardsToThemeTargetAndLogsTheAppliedTheme` to mock the bool-returning `SetTheme` and keep the Information assertion.
   - Add a new test in `PreferenceApplierTests` for the failure path (logs warning, not information).

Note: `PaletteSwitcher.Apply` already logs its own static-Serilog warning with the exception and resolved path on load failure (line 83-87), so failure detail is preserved.

## Impact

- **Code**: `IThemePreferenceTarget`, `WpfThemePreferenceTarget`, `PreferenceApplier`, and the two affected test files. `ServiceRegistration` is unchanged.
- **Behavior**: Users see one theme log line per Settings save instead of two, and the log line matches the actual outcome (success or kept-previous-palette).
- **Tests**: Two existing tests require assertion updates; one new test for the failure path.

## Non-goals

- Do not change how the palette is resolved, merged, or persisted.
- Do not touch `PaletteSwitcher`, `ThemeCatalog`, `SystemThemeFollower`, `App.xaml.cs`, or the accent path.
- No new spec requirements: this is an internal logging/contract fix with no user-visible capability change.

## Dependencies

None. This change is self-contained within the Settings subsystem.
