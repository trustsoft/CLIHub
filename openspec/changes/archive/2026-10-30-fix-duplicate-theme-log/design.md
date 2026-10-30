# Design: Fix Duplicate Theme Log

## Current State

### Duplicate logging

Two call sites log `"Theme set to {Theme}"` at Information level for a single Settings save:

1. **`src/CLIHub/Settings/PreferenceApplier.cs:112-116`** — `ApplyTheme(AppTheme theme)` calls `this.theme.SetTheme(theme)`, then unconditionally logs `this.logger.LogInformation("Theme set to {Theme}", theme)`.

2. **`src/CLIHub/Settings/WpfThemePreferenceTarget.cs:32-49`** — `SetTheme(AppTheme theme)` calls `this.switcher.Apply(...)` and logs either:
   - `this.logger.LogInformation("Theme set to {Theme}", theme)` when `Apply` returns true (line 41), or
   - `this.logger.LogWarning("Could not apply theme {Theme}; the previous palette was kept", theme)` when `Apply` returns false (line 45-47).

Both sites were introduced in commit `1308a4a feat(theme): apply theme changes without a restart`.

Verified by `grep -r "Theme set to" src/` — exactly two matches (lines cited above).

### Misleading success log on failure

`PreferenceApplier.ApplyTheme` logs the success line after calling `SetTheme`, even when the target warns that the palette could not be loaded. The log shows both the warning from the target and the misleading success line from the applier.

### Current contract

**`src/CLIHub/Settings/IThemePreferenceTarget.cs:8-15`**:
```csharp
public interface IThemePreferenceTarget
{
    /// <summary>
    ///   Applies the theme palette.
    /// </summary>
    /// <param name="theme"> The theme whose palette becomes the active overlay. </param>
    void SetTheme(AppTheme theme);
}
```

**Only implementer**: `WpfThemePreferenceTarget` (`src/CLIHub/Settings/WpfThemePreferenceTarget.cs:14`).

**Only caller**: `PreferenceApplier.ApplyTheme` (`src/CLIHub/Settings/PreferenceApplier.cs:114`).

**DI registration**: `src/CLIHub/Composition/ServiceRegistration.cs:166` — `services.AddSingleton<IThemePreferenceTarget, WpfThemePreferenceTarget>()` (auto-wired constructor takes `PaletteSwitcher` and `ILogger<WpfThemePreferenceTarget>`).

### Existing patterns

**`PreferenceApplier.ApplyHotkey` (lines 65-80)** and **`ApplyStartWithWindows` (lines 88-101)** both:
- Call a bool-returning target method.
- Log `LogInformation(...)` on success, `LogWarning(...)` on failure.
- Return the result to the caller.

**`WpfAccentPreferenceTarget` (`src/CLIHub/Settings/WpfAccentPreferenceTarget.cs:9-21`)**:
- Has no logger dependency.
- Calls `AccentApplier.Apply(...)` (void) in the null-guarded `SetAccent` body.
- Logging is done by `PreferenceApplier.ApplyAccent` (line 108), which is void and always logs success (accent application does not fail).

### `PaletteSwitcher.Apply` signature

**`src/CLIHub/Themes/PaletteSwitcher.cs:68`**:
```csharp
public bool Apply(IList<ResourceDictionary> merged, AppTheme theme, bool systemPrefersLight)
```

**Returns** (lines 64-67):
> True when the palette was applied or the base palette was kept; false when the palette dictionary could not be loaded, in which case `merged` is unchanged.

On failure, it logs (lines 83-87):
```csharp
Log.Warning(
    ex,
    "Could not load the {Theme} palette from {PalettePath}; the current palette was kept",
    theme,
    path);
```

So `PaletteSwitcher` provides the bool success/failure semantics we need, and failure detail is already logged by the switcher itself.

### Tests

**`tests/CLIHub.Tests/Services/WpfThemePreferenceTargetTests.cs`**:
- Line 22: `SetTheme_NoApplicationInstance_DoesNotLoadAPaletteOrLog` asserts `Application.Current is null`, creates a target with a logger mock, calls `SetTheme(AppTheme.Light)`, then asserts `logger.VerifyNoOtherCalls()`.
- Class-doc comment (lines 14-18) states the test covers "the null-guard path" only; "applied and failed palette logging needs a real application and is covered by manual smoke."

**`tests/CLIHub.Tests/Services/PreferenceApplierTests.cs`**:
- Lines 78-97: `ApplyTheme_ForwardsToThemeTargetAndLogsTheAppliedTheme` mocks `IThemePreferenceTarget.SetTheme(AppTheme.Blue)` (void), calls `applier.ApplyTheme(AppTheme.Blue)`, verifies the target was called and logs once at Information level.

**`tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs`**:
- Line 58: `Assert.NotNull(provider.GetRequiredService<IThemePreferenceTarget>());` — verifies the interface is resolvable (architecture boundary test).

No other test references `IThemePreferenceTarget` (verified by `grep`).

## Decision

### Contract change

**`IThemePreferenceTarget.SetTheme`** returns `bool`:
- True when the palette was applied or the base palette was intentionally kept (theme is `System` and `PaletteSwitcher.Apply` returned true, or any other theme applied successfully).
- False when the palette dictionary could not be loaded (file missing, malformed, or load exception).

Update the XML doc to match.

### `WpfThemePreferenceTarget` changes

1. Remove the `ILogger<WpfThemePreferenceTarget>` field and constructor parameter.
2. Remove all logging (the target is now silent).
3. Return the `PaletteSwitcher.Apply(...)` result directly when `Application.Current` is not null.
4. When `Application.Current is null`, return **false** — nothing could be applied, so false is the honest answer. The alternative (return true to mean "nothing attempted") would be misleading because the caller logs success in that case.

Rationale for returning false when `Application.Current is null`: The caller (`PreferenceApplier`) logs success when the method returns true. If we returned true, the log would say `"Theme set to {Theme}"` even though no palette was loaded. False accurately reflects that the theme was not applied.

### `PreferenceApplier.ApplyTheme` changes

1. Capture the `SetTheme` result.
2. Log `LogInformation("Theme set to {Theme}", theme)` when true.
3. Log `LogWarning("Could not apply theme {Theme}; the previous palette was kept", theme)` when false.

This matches the `ApplyHotkey` and `ApplyStartWithWindows` patterns.

### DI registration

**No change.** `src/CLIHub/Composition/ServiceRegistration.cs:166` keeps
`services.AddSingleton<IThemePreferenceTarget, WpfThemePreferenceTarget>();`. Constructor injection
resolves the remaining `PaletteSwitcher` parameter from the singleton instance registered at
`ServiceRegistration.cs:167` (`services.AddSingleton(PaletteSwitcher.Shared)`), and dropping the
logger parameter removes nothing that DI needs (`AddLogging` would still supply `ILogger<T>`).
The analogous `WpfAccentPreferenceTarget` (line 165) is registered exactly the same way.

An explicit factory was considered and rejected as unnecessary churn: it duplicates what the
container already infers and departs from the neighbouring registrations.

### Test updates

**`WpfThemePreferenceTargetTests.cs`**:
1. Update the class-doc comment to remove the mention of "applied and failed palette logging" (logging moved to `PreferenceApplier`; the manual smoke now verifies the applier logs, not the target).
2. Remove the logger mock entirely — the target no longer takes a logger.
3. Assert `SetTheme` returns false (the null-Application case); the mock palette factory keeps its `throw`, so the existing assertion still proves no palette load was attempted.

**`PreferenceApplierTests.cs`**:
1. Update `ApplyTheme_ForwardsToThemeTargetAndLogsTheAppliedTheme` to:
   - Mock `SetTheme` to return true (success path).
   - Verify the applier logs at Information level (existing assertion).
2. Add a new test `ApplyTheme_LogsWarning_WhenTargetReturnsFalse`:
   - Mock `SetTheme` to return false.
   - Verify the applier logs at Warning level with the kept-previous-palette message.

## Alternatives Considered

**Keep the target log**: Would mean the applier and target still both log, just coordinated with the bool return. Rejected because the accent target (same responsibility, different palette) has no logger, and the applier is the natural single log site for all preferences.

**Return void, log only in the target**: Contradicts the `ApplyHotkey`/`ApplyStartWithWindows` pattern where the applier logs based on the result. Rejected because consistency matters more than moving the log site.

**Return true when `Application.Current is null`**: Would log a misleading success message. Rejected as documented above.

## Open Questions

None. The change is mechanical and the pattern is established by existing code.
