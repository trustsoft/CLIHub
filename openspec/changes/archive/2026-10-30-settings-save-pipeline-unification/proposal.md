## Why

The Settings save pipeline duplicates scanning-related representation, validation, normalization, and write logic across four locations:

1. `SettingsInput` and `SettingsDraft` each carry five separate scanning parameters (`WatchedDirectories`, `ScanDepth`/`ScanDepthText`, `IgnoredPatterns`, `DiscoveryMode`, `WatchedDirectorySettings`) instead of the already-existing `ScanningSettingsInput` value object.
2. Scanning validation and normalization is duplicated in `TryCreateDraft` (lines 264–285) and `ApplyScanning` (lines 324–344).
3. Scanning persistence writes are duplicated in `ApplyScanning` (lines 350–356) and `Save` (lines 488–496).
4. `Save` has a hand-written apply sequence with a separate rollback list, increasing the effort and risk when adding a new preference.

This duplication increases maintenance burden and creates divergence risk.

## What Changes

### 1. Nested scanning input in DTOs

- `SettingsInput` accepts `ScanningSettingsInput Scanning` instead of five flat parameters (`WatchedDirectories`, `ScanDepthText`, `IgnoredPatterns`, `DiscoveryMode`, `WatchedDirectorySettings`).
- `SettingsDraft` carries `ScanningSettingsInput Scanning` (already normalized) instead of the same five parameters.
- The `ScanningSettingsInput.ScanDepth` member is already `int`, so the text-to-int conversion moves out of `TryCreateDraft` and into the ViewModel's input construction.

### 2. Shared scanning normalization

- Add a private `TryNormalizeScanning(ScanningSettingsInput input, out ScanningSettingsInput normalized, out string? error)` helper that validates scan depth (default + per-root), normalizes lists, and returns a normalized `ScanningSettingsInput`.
- Use it in both `TryCreateDraft` and `ApplyScanning`, eliminating the duplication at lines 264–285 and 324–344.

### 3. Shared scanning persistence

- Add a private `WriteScanning(AppPreferences preferences, ScanningSettingsInput scanning)` helper that performs the five scanning assignments (watched roots, per-root settings dictionary, depth, ignored patterns, discovery mode).
- Extract a private `WritePreferences(AppPreferences preferences, SettingsDraft draft)` from `Save`'s lines 468–498 that includes the `WriteScanning` call.
- Use `WriteScanning` in both `ApplyScanning` and the new `WritePreferences`.

### 4. Apply/rollback table

- Introduce a private record `AppliedPreference(string Name, Func<bool> Apply, Action Rollback, int RollbackOrder, string FailureMessage)` holding one apply/rollback pair.
- Build a list of these in `Save` in the current apply order (Windows startup, runtime, path display style, accent, theme, hotkey) before the apply loop.
- On `false` apply, call a single `Rollback(applied)` that sorts by `RollbackOrder` and invokes each `Rollback` action, then return `SettingsApplicationResult.Failed(item.FailureMessage)`.
- On exception, call `Rollback(applied)` and return `Could not save settings: {ex.Message}`.
- Keep the exact failure messages ("Could not update the Windows startup registration. Changes were not saved.", "Could not register the global hotkey. Changes were not saved.") and the current rollback order (runtime=1, path=2, accent=3, theme=4, hotkey=5, startup=6).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a behavior-preserving internal refactor with no user-facing or spec-level changes, so the change opts out of specs (`skip_specs: true` in `.openspec.yaml`).

## Impact

**Modified code:**
- `src/CLIHub/Settings/SettingsApplicationService.cs` — nested scanning in `SettingsInput`/`SettingsDraft`, shared normalization/write helpers, apply/rollback table
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — pass `ScanningSettingsInput` to `SettingsInput` instead of flattening it
- `tests/CLIHub.Tests/Services/SettingsApplicationServiceTests.cs` — update three `SettingsDraft` constructions and the `Input()` helper to pass `ScanningSettingsInput`

**Reuses:**
- The existing `ScanningSettingsInput` record (already defined at lines 140–144)
- All existing validation, normalization, and persistence behaviors

**No behavior change:**
- No validation rule changes
- No normalization changes
- No apply/rollback order changes
- No error message changes
- Parked defects (swallowed watched-directory error, stale agent counts, persist-then-rollback on scanner reload) remain unfixed

## Dependencies

None. This change does not depend on other changes and is not a prerequisite for planned work.

## Non-Goals

- Fixing the parked Settings defects
- Changing validation rules, normalization logic, or error messages
- Rewriting `Load` or adding new scanning features
- Changing the order of apply/rollback operations
- Performance optimization
