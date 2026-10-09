# Design: Simplify Preference Synchronization

## Architecture Decision

**Selected Approach: Option A - PreferenceSyncHelper**

Create a lightweight helper class that encapsulates the SetProperty → PreferencesStore.Update pattern without modifying base classes or adding framework-level abstractions.

### Why This Approach?

1. **Localized** — all logic in one testable class
2. **No base class pollution** — ObservableObject stays clean and framework-agnostic
3. **Explicit** — callers clearly see they're syncing preferences
4. **Flexible** — easy to add side effects (callbacks) if needed later

### Alternatives Considered

**Option B: Extend ObservableObject**
- ❌ Couples ViewModels base to Core.Configuration
- ❌ Violates separation of concerns
- ❌ Makes ObservableObject less reusable

**Option C: Keep current pattern**
- ✅ Explicit and clear
- ❌ Duplicated code (~15 lines × 2 properties)
- ❌ Maintenance burden

## Component Design

### PreferenceSyncHelper

**Location:** `src/CLIHub/Helpers/PreferenceSyncHelper.cs`

**Responsibility:** Combine property change notification with preference store synchronization in one call.

**Interface:**
```csharp
public static class PreferenceSyncHelper
{
    public static bool SetAndSync<T>(
        ObservableObject owner,
        ref T field,
        T value,
        IPreferencesStore preferencesStore,
        Action<AppPreferences> updatePreference,
        Action? onChanged = null,
        [CallerMemberName] string? propertyName = null);
}
```

**Parameters:**
- `owner` — the ObservableObject (ViewModel) that owns the property
- `field` — backing field for the property
- `value` — new value to set
- `preferencesStore` — preference store to sync with
- `updatePreference` — lambda to update the preference (e.g., `p => p.IsPinned = value`)
- `onChanged` — optional callback for side effects (e.g., status messages, refresh)
- `propertyName` — auto-captured property name for change notification

**Return Value:** `bool` indicating whether value changed (same as SetProperty)

### Usage Pattern

**Before:**
```csharp
public bool IsPinned
{
    get => _isPinned;
    set
    {
        if (!SetProperty(ref _isPinned, value) || _suppressPreferenceUpdates)
        {
            return;
        }

        _preferencesStore.Update(preferences => preferences.PinLaunchWindow = value);
        _statusMessageCoordinator.ShowMessage(
            value ? "Launch window pinned" : "Launch window unpinned",
            isTransient: true);
    }
}
```

**After:**
```csharp
public bool IsPinned
{
    get => _isPinned;
    set => PreferenceSyncHelper.SetAndSync(
        this,
        ref _isPinned,
        value,
        _preferencesStore,
        p => p.PinLaunchWindow = value,
        onChanged: () => _statusMessageCoordinator.ShowMessage(
            value ? "Launch window pinned" : "Launch window unpinned",
            isTransient: true));
}
```

**Benefits:**
- 9 lines → 8 lines (but reads as one logical operation)
- No duplication of SetProperty + Update pattern
- Side effects clearly marked with `onChanged` parameter
- Testable in isolation

## Implementation Plan

### Phase 1: Create Helper (30 min)
1. Create `PreferenceSyncHelper.cs`
2. Implement `SetAndSync` method
3. Add XML documentation

### Phase 2: Add Tests (30 min)
1. Create `PreferenceSyncHelperTests.cs`
2. Test scenarios:
   - Value changes → updates store + raises event
   - Value unchanged → no store update, no event
   - Null owner throws
   - Null store throws
   - onChanged callback invoked when value changes
   - onChanged not invoked when value unchanged
3. Achieve >90% coverage

### Phase 3: Refactor LaunchWindowViewModel (20 min)
1. Replace `IsPinned` setter with `SetAndSync`
2. Replace `ShowOnlyProjectAgents` setter with `SetAndSync`
3. Verify behavior unchanged

### Phase 4: Verify (10 min)
1. Run all tests (expect 100% pass)
2. Manual smoke test: toggle preferences in UI
3. Code review

## Testing Strategy

### Unit Tests (PreferenceSyncHelper)
- Value change scenarios
- Null argument validation
- Callback invocation logic
- PropertyChanged event propagation

### Integration Tests (LaunchWindowViewModel)
- Existing tests should pass unchanged
- Preferences still sync correctly
- UI still responds to property changes

### Manual Testing
- Launch app
- Toggle "Show only project agents"
- Pin/unpin window
- Verify preferences persist across restarts

## Rollback Plan

If issues arise:
1. Revert LaunchWindowViewModel changes
2. Keep PreferenceSyncHelper (no harm in having it)
3. Revisit design or defer indefinitely

## Success Metrics

- ✅ 15-20 lines removed from LaunchWindowViewModel
- ✅ All 402 tests pass
- ✅ PreferenceSyncHelper has >90% coverage
- ✅ No functional regressions
- ✅ Code review approval
