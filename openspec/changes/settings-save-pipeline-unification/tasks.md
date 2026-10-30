## Stage 1: Nested scanning records + ViewModel + tests

- [x] 1.1 Update `SettingsInput` record signature (lines 37–60) to replace five scanning parameters with `ScanningSettingsInput Scanning` (required, no default); update XML doc to reference the nested member
- [x] 1.2 Update `SettingsDraft` record signature (lines 88–111) to replace five scanning parameters with `ScanningSettingsInput Scanning` (required, no default); update XML doc to reference the nested member
- [x] 1.2a Update `SettingsApplicationService.Load` (lines 189–212) to construct the nested `ScanningSettingsInput` from `preferences.Scanning`: watched roots (fallback `Array.Empty<string>()`), `ProjectScanningDefaults.NormalizeScanDepth(preferences.Scanning.ScanDepth)`, ignored patterns (fallback `ProjectScanningDefaults.IgnoredPatterns`), discovery mode, and per-root settings. Keep `DefaultAgentId` after it.
- [x] 1.2b Update `WatchedDirectoryEditor.Load` (`src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs:328-373`) to read `draft.Scanning.ScanDepth`, `draft.Scanning.WatchedDirectories`, `draft.Scanning.WatchedDirectorySettings`, `draft.Scanning.DiscoveryMode`, and `draft.Scanning.IgnoredPatterns` instead of the flat draft members.
- [x] 1.2c Update `TryCreateDraft` minimally so the build stays green: read `input.Scanning.ScanDepth` (int), `input.Scanning.WatchedDirectories`, `input.Scanning.IgnoredPatterns`, `input.Scanning.DiscoveryMode`, and `input.Scanning.WatchedDirectorySettings`; pass one nested `ScanningSettingsInput` to the `SettingsDraft` constructor. Keep the existing inline validation/normalization logic for now (Stage 2 extracts it); replace the text depth parse with the int range check.
- [x] 1.2d Update `Save`'s persistence block minimally so the build stays green: replace `draft.WatchedDirectories`, `draft.ScanDepth`, `draft.IgnoredPatterns`, `draft.DiscoveryMode`, and `draft.WatchedDirectorySettings` with the `draft.Scanning.*` equivalents. Keep the block inline (Stage 3 extracts `WriteScanning`/`WritePreferences`).
- [x] 1.3 Update `SettingsViewModel.Save` (lines 810–844) to pass `scanning` directly to `SettingsInput(...)` instead of flattening it: replace lines 831–836 with `scanning` (removes the `.ToString()` conversion at line 832)
- [x] 1.4 Update test `Input()` helper (lines 283–300) to pass a default `ScanningSettingsInput(null, ProjectScanningDefaults.DefaultScanDepth, null, ProjectDiscoveryMode.Both, null)` instead of flat scanning defaults
- [x] 1.5 Update test `SettingsDraft` construction at line 42 to pass a default `ScanningSettingsInput(null, ProjectScanningDefaults.DefaultScanDepth, null, ProjectDiscoveryMode.Both, null)` after `"#4CC2FF"`
- [x] 1.6 Update test `SettingsDraft` construction at line 98 to pass a default `ScanningSettingsInput(null, ProjectScanningDefaults.DefaultScanDepth, null, ProjectDiscoveryMode.Both, null)` after `"#4CC2FF"`
- [x] 1.7 Update test `SettingsDraft` construction at line 147 to pass a default `ScanningSettingsInput(null, ProjectScanningDefaults.DefaultScanDepth, null, ProjectDiscoveryMode.Both, null)` between `"#4CC2FF"` and `DefaultAgentId: "opencode"`
- [x] 1.8 Build the solution with `dotnet build CLIHub.sln` and verify 0 warnings/errors
- [x] 1.9 Run tests with `dotnet test CLIHub.sln` and verify all tests pass

> **Note:** After this stage, `SettingsInput` and `SettingsDraft` carry nested scanning and every construction/access site compiles: `Load`, `WatchedDirectoryEditor.Load`, `TryCreateDraft`, the `Save` write block, the VM `Save`, and tests. The inline normalization/write logic is intentionally left in place for Stages 2-3. `TryNormalizeScanning` and `WriteScanning` are extracted in the next stages.

## Stage 2: Shared normalization helper

- [x] 2.1 Add a private static helper `TryNormalizeScanning(ScanningSettingsInput input, out ScanningSettingsInput normalized, out string? error)` after line 433 that:
  - Validates `IsScanDepthInRange(input.ScanDepth)`, returns `false` with `ScanDepthError` if out of range
  - Validates per-root depth from `input.WatchedDirectorySettings` (foreach + `IsScanDepthInRange`), returns `false` with `WatchedDirectoryScanDepthError(entry.Key)` if out of range
  - Normalizes `input.WatchedDirectories` via `NormalizeList`
  - Normalizes `input.WatchedDirectorySettings` via `NormalizeWatchedDirectorySettings` (pass normalized directories)
  - Normalizes `input.IgnoredPatterns` via `NormalizeList`
  - Returns `true` with `normalized = new ScanningSettingsInput(normalizedDirectories, input.ScanDepth, normalizedPatterns, input.DiscoveryMode, normalizedSettings)`
- [x] 2.2 Update `TryCreateDraft` (lines 264–310) to call `TryNormalizeScanning(input.Scanning, out var normalizedScanning, out error)` and replace lines 264–285 validation/normalization with this call; pass `normalizedScanning` to the `SettingsDraft` constructor instead of flat scanning fields
- [x] 2.3 Update `ApplyScanning` (lines 324–368) to call `TryNormalizeScanning(input, out var normalized, out var error)` and replace lines 324–344 validation/normalization with this call; use `normalized.*` in the subsequent persistence block (lines 350–356)
- [x] 2.4 Build the solution with `dotnet build CLIHub.sln` and verify 0 warnings/errors
- [x] 2.5 Run tests with `dotnet test CLIHub.sln` and verify all tests pass (validation behavior is unchanged; existing tests pin this)

> **Note:** After this stage, scanning normalization is centralized in `TryNormalizeScanning` and used by both `TryCreateDraft` and `ApplyScanning`.

## Stage 3: Shared scanning write helper

- [x] 3.1 Add a private static helper `WriteScanning(AppPreferences preferences, ScanningSettingsInput scanning)` after the new `TryNormalizeScanning` that performs the five scanning assignments:
  - `preferences.Scanning.WatchedDirectories = (scanning.WatchedDirectories ?? Array.Empty<string>()).ToList();`
  - `preferences.Scanning.WatchedDirectorySettings = scanning.WatchedDirectorySettings is null ? new Dictionary<string, WatchedDirectoryScanSettings>(StringComparer.OrdinalIgnoreCase) : new Dictionary<string, WatchedDirectoryScanSettings>(scanning.WatchedDirectorySettings, StringComparer.OrdinalIgnoreCase);`
  - `preferences.Scanning.ScanDepth = scanning.ScanDepth;`
  - `preferences.Scanning.IgnoredPatterns = (scanning.IgnoredPatterns ?? Array.Empty<string>()).ToList();`
  - `preferences.Scanning.DiscoveryMode = scanning.DiscoveryMode;`
- [x] 3.2 Update `ApplyScanning` (lines 346–357) to replace lines 350–356 with `WriteScanning(preferences, normalized);` (inside the `Update` lambda)
- [x] 3.3 Extract a private helper `WritePreferences(AppPreferences preferences, SettingsDraft draft)` from `Save`'s lines 468–498; this helper contains the current assignment sequence (lines 470–487) plus a call to `WriteScanning(preferences, draft.Scanning)` for lines 488–496
- [x] 3.4 Update `Save` to call `this.preferencesStore.Update(preferences => WritePreferences(preferences, draft));` instead of the inline lambda (lines 468–498)
- [x] 3.5 Build the solution with `dotnet build CLIHub.sln` and verify 0 warnings/errors
- [x] 3.6 Run tests with `dotnet test CLIHub.sln` and verify all tests pass (persistence behavior is unchanged; existing tests verify correct values)

> **Note:** After this stage, scanning persistence is centralized in `WriteScanning` and reused by both `ApplyScanning` and the new `WritePreferences`.

## Stage 4: Apply/rollback table

- [x] 4.1 Add a private sealed record `AppliedPreference(string Name, Func<bool> Apply, Action Rollback, int RollbackOrder, string FailureMessage)` after line 130 (before the service class)
- [x] 4.2 Replace `Save`'s apply sequence (lines 449–466) with a table construction:
  - Build `var items = new List<AppliedPreference> { ... }` with six entries in the current apply order: Windows startup (RollbackOrder=6, failure message="Could not update the Windows startup registration. Changes were not saved."), runtime (RollbackOrder=1, FailureMessage=""), path display style (RollbackOrder=2, FailureMessage=""), accent (RollbackOrder=3, FailureMessage=""), theme (RollbackOrder=4, FailureMessage=""), hotkey (RollbackOrder=5, failure message="Could not register the global hotkey. Changes were not saved.")
  - For `void` apply methods (runtime, path, accent, theme), wrap in a lambda that returns `true`: `() => { this.applier.ApplyRuntime(draft.Runtime); return true; }`
  - For `bool` apply methods (startup, hotkey), pass the method call directly: `() => this.applier.ApplyStartWithWindows(draft.StartWithWindows)`
  - Each rollback action calls the applier with the `previous*` value: `() => this.applier.ApplyRuntime(previousPreferences.DefaultRuntime)`
- [x] 4.3 Replace the apply loop (lines 449–466) with `foreach (var item in items) { if (!item.Apply()) { Rollback(items); return SettingsApplicationResult.Failed(item.FailureMessage); } }`. Do **not** track a separate `applied` list: the current behavior rolls back all six entries on any failure, including the failed one and not-yet-applied ones.
- [x] 4.4 Replace the `Rollback` method (lines 514–522) with a new signature `Rollback(IReadOnlyList<AppliedPreference> items)` that calls `foreach (var item in items.OrderBy(x => x.RollbackOrder)) { this.TryRollback(item.Name, item.Rollback); }`
- [x] 4.5 Update the catch block (lines 506–511) to call `Rollback(items);` (`items` is built before the `try` block so it is in scope)
- [x] 4.6 Remove the old `Rollback(AppPreferences, bool, HotkeyDefinition)` signature (lines 514–522, now replaced)
- [x] 4.7 Build the solution with `dotnet build CLIHub.sln` and verify 0 warnings/errors
- [x] 4.8 Run tests with `dotnet test CLIHub.sln` and verify all tests pass (apply/rollback behavior is unchanged; existing tests verify rollback calls at lines 120–125)

> **Note:** After this stage, the apply/rollback logic is table-driven. The exact failure messages and rollback order are preserved.

## Stage 5: Verification and validation

- [x] 5.1 Build the full solution with `dotnet build CLIHub.sln` and verify 0 warnings/errors
- [x] 5.2 Run all tests with `dotnet test CLIHub.sln` and verify all tests pass
- [x] 5.3 Run `openspec validate --all` and verify all capabilities pass validation
- [x] 5.4 Run `graphify update .` (graphify-out is gitignored)
- [x] 5.5 Manual smoke (user-confirmed 2026-10-30): open Settings, change scanning settings on the Project Scanning page and verify immediate apply; change a preference and Save; reopen Settings and verify persistence.

> **Final:** All stages complete. The Settings save pipeline is unified: nested scanning records, shared normalization, shared write, and table-driven apply/rollback. No behavior change; all tests pass.

## Notes for Implementation

- **Staging rationale:** Each stage is independently verifiable with build+test. Stage 1 updates the record shapes and their construction sites. Stage 2 unifies normalization. Stage 3 unifies write. Stage 4 unifies apply/rollback. Stage 5 is the final gate.
- **Build/test after each stage:** Do NOT proceed to the next stage if build or tests fail. Diagnose and fix before continuing.
- **No commits:** The brief prohibits commit instructions. The implementer commits when the user approves.
- **Rollback order verification:** The table at stage 4.2 explicitly encodes RollbackOrder. The current order is runtime(1), path(2), accent(3), theme(4), hotkey(5), startup(6), verified from lines 516–521 of the current `Rollback` method.
- **Apply/rollback capture:** Build `items` before the `try` block so both the apply loop and the catch block can call `Rollback(items)`. The current behavior rolls back all six entries on any failure, including the failed hotkey/startup entry — do not track only successfully applied items.
- **`WritePreferences` extraction:** At stage 3.3, extract the exact sequence from lines 470–498, including the two `ShowWindowOnStartup` and `PathDisplayStyle` assignments (lines 486–487) and the scanning block replacement. The extracted helper should match the current order exactly.
- **Test updates:** Stages 1.4–1.7 update test constructions. Verify line numbers before editing; they may have shifted if the source changed. The brief's line numbers are from the current tree.
- **No weakening:** All existing tests remain unchanged in behavior. No tests are renamed, skipped, or deleted. The refactor is a drop-in replacement.
