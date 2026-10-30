# Tasks: Fix Duplicate Theme Log

## Stage 1: Update Interface and Target

- [x] 1.1 Change `IThemePreferenceTarget.SetTheme` signature to return `bool` and update XML doc (`src/CLIHub/Settings/IThemePreferenceTarget.cs`).
- [x] 1.2 Remove `ILogger<WpfThemePreferenceTarget>` field and constructor parameter from `WpfThemePreferenceTarget` (`src/CLIHub/Settings/WpfThemePreferenceTarget.cs`).
- [x] 1.3 Update `WpfThemePreferenceTarget.SetTheme` to return `false` when `Application.Current is null` (line 34-37).
- [x] 1.4 Remove the logging from `WpfThemePreferenceTarget.SetTheme` (lines 40-48) and return the `this.switcher.Apply(...)` result directly.
- [x] 1.5 Build to verify interface contract change compiles.

Gate: `dotnet build CLIHub.sln`

## Stage 2: Update Applier and DI Registration

- [x] 2.1 Update `PreferenceApplier.ApplyTheme` (`src/CLIHub/Settings/PreferenceApplier.cs:112-116`) to capture the `SetTheme` result and log `LogInformation` on success, `LogWarning` on failure with the kept-previous-palette message.
- [x] 2.2 Leave the auto-wired `IThemePreferenceTarget` registration at `ServiceRegistration.cs:166` unchanged; confirm that resolving it still works with the remaining `PaletteSwitcher` dependency (registered at line 167) and no code change is needed.
- [x] 2.3 Build to verify the applier and DI registration changes compile.

Gate: `dotnet build CLIHub.sln`

## Stage 3: Update Tests

- [x] 3.1 Update `WpfThemePreferenceTargetTests.SetTheme_NoApplicationInstance_DoesNotLoadAPaletteOrLog` (`tests/CLIHub.Tests/Services/WpfThemePreferenceTargetTests.cs:22-32`):
  - Remove the logger mock (target no longer has one).
  - Assert `SetTheme` returns `false`.
  - Verify no palette load was attempted (existing `throw` in the mock switcher).
- [x] 3.2 Update the class-doc comment for `WpfThemePreferenceTargetTests` (lines 14-18) to remove mention of "applied and failed palette logging" (moved to applier).
- [x] 3.3 Update `PreferenceApplierTests.ApplyTheme_ForwardsToThemeTargetAndLogsTheAppliedTheme` (`tests/CLIHub.Tests/Services/PreferenceApplierTests.cs:79-97`):
  - Mock `SetTheme` to return `true`.
  - Keep the existing log verification at Information level.
- [x] 3.4 Add a new test `ApplyTheme_LogsWarning_WhenTargetReturnsFalse` in `PreferenceApplierTests`:
  - Mock `SetTheme` to return `false`.
  - Verify the applier logs at Warning level with the kept-previous-palette message.

Gate: `dotnet build CLIHub.sln`

## Stage 4: Final Verification

- [x] 4.1 Run all tests.
- [x] 4.2 Validate OpenSpec.
- [x] 4.3 Update graphify.

Gate: `dotnet test CLIHub.sln`  
Gate: `openspec validate --all`  
Gate: `graphify update .`
