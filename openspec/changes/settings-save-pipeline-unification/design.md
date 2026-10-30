## Verified Current State

All line numbers and facts verified by reading the source.

### `SettingsInput` record (lines 37–60)

Five flat scanning parameters:
- `WatchedDirectories` (line 55, optional)
- `ScanDepthText` (line 56, default `"2"`)
- `IgnoredPatterns` (line 57, optional)
- `DiscoveryMode` (line 58, default `ProjectDiscoveryMode.Both`)
- `WatchedDirectorySettings` (line 60, optional)

### `SettingsDraft` record (lines 88–111)

Five flat scanning parameters with the same names, except `ScanDepth` is `int` (line 107) instead of `ScanDepthText` string.

### `ScanningSettingsInput` record (lines 140–144)

Already exists as a typed value object with five members:
- `WatchedDirectories` (line 140, optional)
- `ScanDepth` (line 141, `int`)
- `IgnoredPatterns` (line 142, optional)
- `DiscoveryMode` (line 143, required)
- `WatchedDirectorySettings` (line 144, optional)

### `TryCreateDraft` (lines 222–312)

Validation and normalization:
- Parse `ScanDepthText` to `int` and validate range (lines 264–268, calls `IsScanDepthInRange`).
- Validate per-root depth from `input.WatchedDirectorySettings` dictionary (lines 270–280).
- Normalize watched directories and per-root settings (lines 282–285: `NormalizeList`, `NormalizeWatchedDirectorySettings`).
- Construct draft with flat scanning parameters (lines 287–310: `watchedDirectories`, `scanDepth`, `NormalizeList(input.IgnoredPatterns)`, `input.DiscoveryMode`, `watchedDirectorySettings`).

Error messages:
- `ScanDepthError` (line 266): `"Repository scan depth must be a whole number between {min} and {max}."`
- `WatchedDirectoryScanDepthError(root)` (line 276): `"Repository scan depth for {root} must be between {min} and {max}."`

### `ApplyScanning` (lines 320–368)

Validation and normalization:
- Validate `input.ScanDepth` range (lines 324–327, calls `IsScanDepthInRange`).
- Validate per-root depth from `input.WatchedDirectorySettings` (lines 329–338).
- Normalize lists and settings (lines 340–344: same `NormalizeList`, `NormalizeWatchedDirectorySettings`).

Persistence write (lines 346–357, inside `preferencesStore.Update`):
- `preferences.Scanning.WatchedDirectories = watchedDirectories.ToList()` (line 350)
- `preferences.Scanning.WatchedDirectorySettings = new Dictionary<...>(watchedDirectorySettings, StringComparer.OrdinalIgnoreCase)` (lines 351–353)
- `preferences.Scanning.ScanDepth = input.ScanDepth` (line 354)
- `preferences.Scanning.IgnoredPatterns = ignoredPatterns.ToList()` (line 355)
- `preferences.Scanning.DiscoveryMode = input.DiscoveryMode` (line 356)

Reload scanner (line 360).

Same error messages as `TryCreateDraft` (lines 326, 335).

### `Save` (lines 439–512)

Apply sequence (lines 449–466):
1. `ApplyStartWithWindows` (lines 449–454, fails with "Could not update the Windows startup registration. Changes were not saved.")
2. `ApplyRuntime` (line 456)
3. `ApplyPathDisplayStyle` (line 457)
4. `ApplyAccent` (line 458)
5. `ApplyTheme` (line 459)
6. `ApplyHotkey` (lines 461–466, fails with "Could not register the global hotkey. Changes were not saved.")

Persistence block (lines 468–498, inside `preferencesStore.Update`):
- Non-scanning writes (lines 470–487)
- Scanning writes (lines 488–496, same five assignments as `ApplyScanning`)

Post-persistence (lines 500–503):
- `ApplyStartupUpdateCheck` (line 500)
- `projectScanner.Reload()` (line 503)

Exception handling (lines 506–511):
- Log warning, call `Rollback`, return `"Could not save settings: {ex.Message}"`

### `Rollback` (lines 514–522)

Calls `TryRollback` for each preference in reverse order:
1. runtime (line 516)
2. path display style (line 517)
3. accent (line 518)
4. theme (line 519)
5. hotkey (line 520)
6. Windows startup (line 521)

### `SettingsViewModel.Save` (lines 810–844)

Constructs `SettingsInput` from flat values (lines 812–836):
- Line 812: `var scanning = this.Scanning.CreateInput();` (returns `ScanningSettingsInput`)
- Line 831: `scanning.WatchedDirectories`
- Line 832: `scanning.ScanDepth.ToString(CultureInfo.InvariantCulture)` (converts `int` to `string` for `ScanDepthText`)
- Line 833: `scanning.IgnoredPatterns`
- Line 834: `scanning.DiscoveryMode`
- Line 836: `scanning.WatchedDirectorySettings`

### `WatchedDirectoryEditor.CreateInput()` (lines 379–387)

Returns a `ScanningSettingsInput` with:
- `WatchedDirectories` (line 380, `int` from `SelectedScanDepth.Value`)
- `ScanDepth` (line 381, `int`)
- `IgnoredPatterns` (line 382)
- `DiscoveryMode` (line 383)
- `WatchedDirectorySettings` (lines 384–387)

### Test `Input()` helper (lines 283–300)

Constructs `SettingsInput` with flat scanning defaults: no watched directories, `"2"` for `ScanDepthText`, no patterns, no per-root settings.

### Test `SettingsDraft` constructions (lines 42, 98, 147)

All pass flat scanning defaults. None access `draft.` scanning fields except line 72 (`draft.AppTheme`, not scanning-related).

## Decisions

### Decision 1: Nested `ScanningSettingsInput` in both DTOs

**Chosen:** Change `SettingsInput` and `SettingsDraft` to carry `ScanningSettingsInput Scanning` instead of five flat parameters.

**Rationale:**
- `ScanningSettingsInput` already exists (lines 140–144) and is the canonical scanning input shape.
- The ViewModel already constructs `ScanningSettingsInput` via `CreateInput()` (line 812), then flattens it to pass to `SettingsInput` (lines 831–836). This flattening is unnecessary duplication.
- Nesting reduces the parameter count from 21 to 17 in `SettingsInput` and from 21 to 17 in `SettingsDraft`.
- Preserves the existing normalization boundary: `SettingsInput.Scanning` is raw input; `SettingsDraft.Scanning` is normalized.
- `SettingsApplicationService.Load` (lines 189-212) also constructs `SettingsDraft` from preferences; it must build the nested `ScanningSettingsInput` from `preferences.Scanning` (watched roots, normalized depth, ignored patterns, discovery mode, per-root settings), preserving the current fallbacks. This construction site is easy to miss and must be updated in Stage 1.

**Record shapes:**

```csharp
public sealed record SettingsInput(
    RuntimeKind Runtime,
    PathDisplayStyle PathDisplayStyle,
    string HotkeyText,
    string ProbeTtlText,
    string ProbeTimeoutText,
    bool StartWithWindows,
    bool StartMinimized,
    bool CheckForUpdatesOnStartup,
    bool AutoDownloadUpdates,
    bool NotifyStableUpdates,
    bool NotifyPrereleaseUpdates,
    string LogLevel,
    string LogRetentionDaysText,
    string LogFileSizeLimitText,
    PopupScreenPosition PopupScreenPosition,
    AppTheme AppTheme,
    string AccentColor,
    ScanningSettingsInput Scanning,
    string? DefaultAgentId = null);

public sealed record SettingsDraft(
    RuntimeKind Runtime,
    PathDisplayStyle PathDisplayStyle,
    HotkeyDefinition Hotkey,
    int? ProbeTtlMinutes,
    int? ProbeTimeoutSeconds,
    bool StartWithWindows,
    bool StartMinimized,
    bool CheckForUpdatesOnStartup,
    bool AutoDownloadUpdates,
    bool NotifyStableUpdates,
    bool NotifyPrereleaseUpdates,
    string LogLevel,
    int LogRetentionDays,
    int LogFileSizeLimitMb,
    PopupScreenPosition PopupScreenPosition,
    AppTheme AppTheme,
    string AccentColor,
    ScanningSettingsInput Scanning,
    string? DefaultAgentId = null);
```

**Impact:**
- `SettingsInput` scanning parameters were optional; now `Scanning` is required.
- `SettingsDraft` scanning parameters had defaults; now `Scanning` is required with no default.
- The ViewModel must pass the full `ScanningSettingsInput` instead of flattening it (removes the `.ToString()` conversion at line 832).
- Tests must construct a default `ScanningSettingsInput` and pass it.

**Alternatives considered:**
- Keep flat parameters: Rejected — perpetuates duplication and requires maintaining five parallel field paths.
- Add a second `ScanningSettingsInputText` with `string ScanDepthText`: Rejected — the ViewModel already has `int ScanDepth`, so the text conversion is unnecessary; moving the conversion to the ViewModel is simpler.

### Decision 2: Remove text-to-int conversion from `TryCreateDraft`

**Chosen:** Accept `int ScanDepth` in `ScanningSettingsInput` (already the case at line 141) and validate the int range in `TryCreateDraft`. Remove the `ScanDepthText` parse (lines 264–268).

**Rationale:**
- The ViewModel's `CreateInput()` already produces `int ScanDepth` (line 381 of `WatchedDirectoryEditor.cs`).
- Line 832 of `SettingsViewModel.cs` converts this `int` **back to string** to pass to `SettingsInput.ScanDepthText`, which `TryCreateDraft` then re-parses. This round-trip is unnecessary.
- The parse-then-validate path (lines 264–268) had two error modes: non-numeric text → parse error; out-of-range int → range error. With `int` input, only the range check remains.
- The non-numeric-text error is not reachable via the UI (the ViewModel always passes a valid int), so dropping it is a behavior-equivalent simplification.

**Error handling:**
- Keep `ScanDepthError` (line 370): `"Repository scan depth must be a whole number between {min} and {max}."`
- The message still says "whole number" because it's user-facing and the range constraint is the salient point.

**Alternatives considered:**
- Keep `ScanDepthText` and parse in `TryCreateDraft`: Rejected — adds a round-trip conversion (int→string→int) with no benefit.
- Change the ViewModel to pass text: Rejected — the ViewModel's internal model is already `int`; converting to text at the last moment is artificial.

**Deviation from brief:** The brief stated "the VM already produces an int" (true) and said to validate the int range instead of parsing text (correct), and noted "document it as a deliberate, behavior-equivalent simplification." This is that simplification. The non-numeric-text path was never reachable from the UI, so removing it does not weaken validation.

### Decision 3: Shared normalization helper

**Chosen:** Add a private static helper `TryNormalizeScanning(ScanningSettingsInput input, out ScanningSettingsInput normalized, out string? error)` that:
1. Validates default scan depth range (calls `IsScanDepthInRange(input.ScanDepth)`).
2. Validates per-root depth from `input.WatchedDirectorySettings` dictionary.
3. Normalizes watched directories (`NormalizeList(input.WatchedDirectories)`).
4. Normalizes per-root settings (`NormalizeWatchedDirectorySettings(input.WatchedDirectorySettings, normalizedDirectories)`).
5. Normalizes ignored patterns (`NormalizeList(input.IgnoredPatterns)`).
6. Returns a new `ScanningSettingsInput` with normalized values, or an error.

Use this helper in both `TryCreateDraft` and `ApplyScanning`.

**Signature:**

```csharp
private static bool TryNormalizeScanning(
    ScanningSettingsInput input,
    out ScanningSettingsInput normalized,
    out string? error)
```

**Rationale:**
- Eliminates duplication between lines 264–285 (`TryCreateDraft`) and lines 324–344 (`ApplyScanning`).
- Preserves exact validation order and error messages.
- Returns a normalized `ScanningSettingsInput`, making the draft's `Scanning` field ready to persist without further transformation.

**Alternatives considered:**
- Keep separate validation blocks: Rejected — violates DRY and risks divergence.
- Normalize in-place instead of returning a new record: Rejected — records are immutable; `with` expressions are clearer.

### Decision 4: Shared scanning write helper

**Chosen:** Add a private static helper `WriteScanning(AppPreferences preferences, ScanningSettingsInput scanning)` that performs the five scanning assignments currently duplicated in `ApplyScanning` (lines 350–356) and `Save` (lines 488–496).

**Signature:**

```csharp
private static void WriteScanning(AppPreferences preferences, ScanningSettingsInput scanning)
{
    preferences.Scanning.WatchedDirectories = (scanning.WatchedDirectories ?? Array.Empty<string>()).ToList();
    preferences.Scanning.WatchedDirectorySettings = scanning.WatchedDirectorySettings is null
        ? new Dictionary<string, WatchedDirectoryScanSettings>(StringComparer.OrdinalIgnoreCase)
        : new Dictionary<string, WatchedDirectoryScanSettings>(
            scanning.WatchedDirectorySettings,
            StringComparer.OrdinalIgnoreCase);
    preferences.Scanning.ScanDepth = scanning.ScanDepth;
    preferences.Scanning.IgnoredPatterns = (scanning.IgnoredPatterns ?? Array.Empty<string>()).ToList();
    preferences.Scanning.DiscoveryMode = scanning.DiscoveryMode;
}
```

**Rationale:**
- Eliminates the duplication between `ApplyScanning` and `Save`.
- Preserves exact write order and null handling (empty list for null watched directories, empty case-insensitive dictionary for null per-root settings).

**Alternatives considered:**
- Keep separate write blocks: Rejected — duplication risk (already identical).
- Assign `preferences.Scanning = new ScanningPreferences { ... }`: Rejected — `AppPreferences.Scanning` is a mutable property bag, not a replaceable object; the current mutation pattern is the contract.

### Decision 5: Extract `WritePreferences` from `Save`

**Chosen:** Extract a private helper `WritePreferences(AppPreferences preferences, SettingsDraft draft)` from `Save`'s lines 468–498. This helper contains the current assignment sequence plus a call to `WriteScanning(preferences, draft.Scanning)` for the scanning block.

**Rationale:**
- Keeps the `preferencesStore.Update` call in `Save` focused: `this.preferencesStore.Update(preferences => WritePreferences(preferences, draft));`.
- Groups all persistence writes in one place, making the apply/rollback table logic clearer.
- Reuses `WriteScanning` for the scanning assignments.

**Alternatives considered:**
- Leave the assignments inline in `Save`: Accepted as viable; extraction is a mild readability win but not mandatory. The implementer may inline `WritePreferences` if it adds no clarity.

### Decision 6: Apply/rollback table

**Chosen:** Introduce a private record `AppliedPreference` and build a list of apply/rollback pairs in `Save` before the apply loop.

**Record shape:**

```csharp
private sealed record AppliedPreference(
    string Name,
    Func<bool> Apply,
    Action Rollback,
    int RollbackOrder,
    string FailureMessage);
```

**Table construction in `Save`:**

```csharp
var items = new List<AppliedPreference>
{
    new("Windows startup",
        () => this.applier.ApplyStartWithWindows(draft.StartWithWindows),
        () => this.applier.ApplyStartWithWindows(previousStartupState),
        6,
        "Could not update the Windows startup registration. Changes were not saved."),
    new("runtime",
        () => { this.applier.ApplyRuntime(draft.Runtime); return true; },
        () => this.applier.ApplyRuntime(previousPreferences.DefaultRuntime),
        1,
        ""),
    new("path display style",
        () => { this.applier.ApplyPathDisplayStyle(draft.PathDisplayStyle); return true; },
        () => this.applier.ApplyPathDisplayStyle(PathDisplayStyles.Parse(previousPreferences.PathDisplayStyle)),
        2,
        ""),
    new("accent",
        () => { this.applier.ApplyAccent(draft.AccentColor); return true; },
        () => this.applier.ApplyAccent(previousPreferences.AccentColor),
        3,
        ""),
    new("theme",
        () => { this.applier.ApplyTheme(draft.AppTheme); return true; },
        () => this.applier.ApplyTheme(previousPreferences.AppTheme),
        4,
        ""),
    new("hotkey",
        () => this.applier.ApplyHotkey(draft.Hotkey),
        () => this.applier.ApplyHotkey(previousHotkey),
        5,
        "Could not register the global hotkey. Changes were not saved.")
};
```

**Apply loop:**

```csharp
foreach (var item in items)
{
    if (!item.Apply())
    {
        Rollback(items);
        return SettingsApplicationResult.Failed(item.FailureMessage);
    }
}
```

**Rollback helper:**

```csharp
private void Rollback(IReadOnlyList<AppliedPreference> items)
{
    foreach (var item in items.OrderBy(x => x.RollbackOrder))
    {
        this.TryRollback(item.Name, item.Rollback);
    }
}
```

**Rationale:**
- Replaces the hand-written apply sequence (lines 449–466) and separate `Rollback` method (lines 514–522) with one data structure.
- **Rolls back all six entries on any failure, exactly like the current method** — including the failed entry and entries not yet applied. This is required for the existing hotkey-failure test, which expects the previous hotkey to be re-applied.
- Preserves the exact apply order (startup, runtime, path, accent, theme, hotkey) and rollback order (runtime=1, path=2, accent=3, theme=4, hotkey=5, startup=6).
- Preserves the exact failure messages for startup and hotkey.
- Makes adding a new preference a single-line addition to the table.
- `RollbackOrder` ensures rollback happens in the documented order (current `Rollback` method lines 516–521).

**Alternatives considered:**
- Keep hand-written apply/rollback: Rejected — adding a new preference requires three edits (apply line, rollback line in `Rollback`, `TryRollback` call).
- Use a builder or fluent API: Rejected — list initializer is simpler for six items.
- Rollback in reverse apply order instead of explicit `RollbackOrder`: Rejected — the current rollback order (lines 516–521) is NOT the reverse of the apply order (lines 449–466), so an explicit order field is necessary.

### Decision 7: Preserve exact behavior

**Chosen:** No validation rule changes, no message changes, no reordering, no parked-defect fixes.

**Behavior preservation checklist:**
- ✅ Validation: same range checks, same error messages.
- ✅ Normalization: same `NormalizeList`, same `NormalizeWatchedDirectorySettings`.
- ✅ Persistence: same five scanning writes in the same structure.
- ✅ Apply order: startup, runtime, path, accent, theme, hotkey (unchanged).
- ✅ Rollback order: runtime=1, path=2, accent=3, theme=4, hotkey=5, startup=6 (unchanged).
- ✅ Failure messages: "Could not update the Windows startup registration. Changes were not saved." (line 453) and "Could not register the global hotkey. Changes were not saved." (line 465).
- ✅ Exception message: "Could not save settings: {ex.Message}" (line 510).
- ✅ Post-persistence order: `ApplyStartupUpdateCheck` (line 500) then `projectScanner.Reload()` (line 503).
- ❌ Parked defects NOT fixed: swallowed watched-directory error, stale agent counts, persist-then-rollback on scanner reload.

**Alternatives considered:**
- Fix parked defects while refactoring: Rejected — the brief explicitly prohibits this, and mixing behavior changes with refactoring increases risk.

## Test Changes

### Update `SettingsDraft` constructions (lines 42, 98, 147)

Replace flat scanning parameters with a default `ScanningSettingsInput`:

```csharp
// Old (line 42, example):
var draft = new SettingsDraft(
    RuntimeKind.PowerShell,
    PathDisplayStyle.MiddleEllipsis,
    HotkeyParser.Default,
    15,
    30,
    true,
    false,
    false,
    true,
    true,
    false,
    "Information",
    7,
    0,
    PopupScreenPosition.Center,
    AppTheme.Dark,
    "#4CC2FF");

// New:
var draft = new SettingsDraft(
    RuntimeKind.PowerShell,
    PathDisplayStyle.MiddleEllipsis,
    HotkeyParser.Default,
    15,
    30,
    true,
    false,
    false,
    true,
    true,
    false,
    "Information",
    7,
    0,
    PopupScreenPosition.Center,
    AppTheme.Dark,
    "#4CC2FF",
    new ScanningSettingsInput(
        null,
        ProjectScanningDefaults.DefaultScanDepth,
        null,
        ProjectDiscoveryMode.Both,
        null));
```

The same pattern applies to lines 98 and 147. Line 165 already passes `DefaultAgentId: "opencode"`, so the scanning parameter comes before it.

### Update `Input()` helper (lines 283–300)

Replace flat scanning defaults with a `ScanningSettingsInput`:

```csharp
// Old:
private static SettingsInput Input(string probeTtlText = "", string probeTimeoutText = "") => new(
    RuntimeKind.PowerShell,
    PathDisplayStyle.LeftTrim,
    "Ctrl+Shift+A",
    probeTtlText,
    probeTimeoutText,
    false,
    true,
    true,
    true,
    true,
    false,
    "Information",
    "7",
    "0",
    PopupScreenPosition.Center,
    AppTheme.Dark,
    "#4CC2FF");

// New:
private static SettingsInput Input(string probeTtlText = "", string probeTimeoutText = "") => new(
    RuntimeKind.PowerShell,
    PathDisplayStyle.LeftTrim,
    "Ctrl+Shift+A",
    probeTtlText,
    probeTimeoutText,
    false,
    true,
    true,
    true,
    true,
    false,
    "Information",
    "7",
    "0",
    PopupScreenPosition.Center,
    AppTheme.Dark,
    "#4CC2FF",
    new ScanningSettingsInput(
        null,
        ProjectScanningDefaults.DefaultScanDepth,
        null,
        ProjectDiscoveryMode.Both,
        null));
```

### No new tests required

The existing tests already cover:
- `TryCreateDraft` validation (line 16: invalid probe, expects error).
- `Save` success path (line 33: apply then persist, expects success).
- `Save` hotkey failure (line 77: rollback, expects failure + rollback verification).
- `Save` default agent ID (line 128: persist value, expects correct value).
- `ApplyScanning` success (line 172: normalize + persist + reload, expects correct values).
- `ApplyScanning` out-of-range depth (line 214: validation failure, expects error).
- `ApplyScanning` out-of-range per-root depth (line 238: validation failure, expects error).

The refactor preserves these behaviors. No new tests are needed for:
- `TryNormalizeScanning`: covered by existing `TryCreateDraft` and `ApplyScanning` tests.
- `WriteScanning`: covered by existing persistence verification.
- Apply/rollback table: covered by existing `Save` failure tests (lines 77, 120–125 verify rollback calls).

**No tests weaken, rename, skip, or delete:** The refactor is a drop-in replacement with the same signatures and behaviors.

## Risks

**[Risk] Missed scanning field access in `SettingsDraft`**
- **Mitigation:** Grep for `draft\.Watched`, `draft\.ScanDepth`, `draft\.Ignored`, `draft\.Discovery` to find all accesses. Only one `draft.` access exists in the test file (line 72: `draft.AppTheme`, not scanning). Build failure would surface any missed access.

**[Risk] Rollback order encoding error**
- **Mitigation:** The table explicitly encodes the current rollback order (runtime=1, path=2, accent=3, theme=4, hotkey=5, startup=6) from lines 516–521. The `OrderBy(x => x.RollbackOrder)` call in `Rollback` ensures the documented order. The existing rollback test (lines 120–125) verifies rollback calls happen.

**[Risk] `WritePreferences` extraction changes call order**
- **Mitigation:** Extract the exact block from lines 468–498 without reordering. The scanning assignments are the last block (lines 488–496), so replacing them with `WriteScanning(preferences, draft.Scanning)` preserves order.

**[Risk] `TryNormalizeScanning` changes validation semantics**
- **Mitigation:** Copy the exact validation sequence from lines 264–285 and 324–344. Preserve `IsScanDepthInRange`, `NormalizeList`, `NormalizeWatchedDirectorySettings` calls and error messages. The existing tests pin validation behavior.

## Open Questions

**Q1:** Should `SettingsViewModel.Save` also be updated to pass `scanning` directly to `SettingsInput`?
- **A1:** Yes — the brief's Decision 1 and the ViewModel section (lines 810–844) require this. Line 832 currently converts `scanning.ScanDepth` to string; the new signature accepts `ScanningSettingsInput` directly, eliminating the conversion.

**Q2:** Should `WritePreferences` be extracted or left inline?
- **A2:** Lean toward extraction for clarity, but inline is acceptable if it doesn't aid readability. The implementer decides.

**Q3:** Should the apply/rollback table be extracted to a separate builder method?
- **A3:** No — the table construction is straightforward and keeping it in `Save` is clearer. If the table grows to 10+ items in the future, reconsider.

**Q4:** Should the `RollbackOrder` field be an enum instead of `int`?
- **A4:** No — `int` is simpler for six items. An enum would add ceremony without benefit.

**Q5:** Does the text-to-int simplification (Decision 2) break any external caller?
- **A5:** No — the only caller is `SettingsViewModel.Save` (line 812), which already has `int ScanDepth` via `CreateInput()`. No other code constructs `SettingsInput` (grep confirms).

## Alternatives Considered

### Alternative 1: Keep flat parameters, only unify normalization/write

**Rejected:** Misses the root cause — `ScanningSettingsInput` already exists and is the canonical shape. Nesting it eliminates five parameters from each DTO and removes the ViewModel's flattening code.

### Alternative 2: Only nest in `SettingsDraft`, keep `SettingsInput` flat

**Rejected:** Asymmetric; the ViewModel still needs to flatten. Nesting both DTOs is simpler and more consistent.

### Alternative 3: Table-driven validation instead of separate `TryNormalizeScanning`

**Rejected:** Validation is straightforward (two range checks + two normalize calls); a table would add complexity without benefit.

### Alternative 4: Replace `RollbackOrder` with a reverse-apply list

**Rejected:** The current rollback order (lines 516–521) is NOT the reverse of the apply order (lines 449–466), so an explicit order is necessary. For example, startup is applied first but rolled back last.

## Summary

This refactor consolidates four sources of duplication in the Settings save pipeline:
1. Nested `ScanningSettingsInput` in DTOs (removes five flat parameters from each).
2. Shared `TryNormalizeScanning` (eliminates lines 264–285 and 324–344 duplication).
3. Shared `WriteScanning` and optional `WritePreferences` (eliminates lines 350–356 and 488–496 duplication).
4. Apply/rollback table (replaces hand-written apply sequence and `Rollback` method with one data structure).

All validation rules, error messages, apply order, rollback order, and parked defects remain unchanged. Tests require only construction updates to pass the new nested `Scanning` parameter.
