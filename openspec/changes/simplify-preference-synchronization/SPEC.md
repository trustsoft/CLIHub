# Specification: Simplify Preference Synchronization

## Overview
Introduce `PreferenceSyncHelper` to eliminate boilerplate in preference-synced properties within ViewModels.

## Scope

### In Scope
- Create `PreferenceSyncHelper` utility class
- Add comprehensive unit tests
- Refactor `LaunchWindowViewModel.IsPinned`
- Refactor `LaunchWindowViewModel.ShowOnlyProjectAgents`

### Out of Scope
- Other ViewModels (only LaunchWindowViewModel has this pattern)
- Modifying `ObservableObject` base class
- Changing preference persistence mechanism
- Abstracting other ViewModel patterns

## Functional Requirements

### FR1: PreferenceSyncHelper.SetAndSync
**Priority:** Must Have

Helper method that combines property change notification with preference store synchronization.

**Signature:**
```csharp
public static bool SetAndSync<T>(
    ObservableObject owner,
    ref T field,
    T value,
    IPreferencesStore preferencesStore,
    Action<AppPreferences> updatePreference,
    Action? onChanged = null,
    [CallerMemberName] string? propertyName = null)
```

**Behavior:**
1. Compare `field` with `value` using `EqualityComparer<T>.Default`
2. If equal, return `false` (no change)
3. If different:
   - Update `field` to `value`
   - Call `owner.OnPropertyChanged(propertyName)` via reflection or protected accessor
   - Call `preferencesStore.Update(updatePreference)`
   - If `onChanged != null`, invoke it
   - Return `true`

**Edge Cases:**
- `owner == null` → throw `ArgumentNullException`
- `preferencesStore == null` → throw `ArgumentNullException`
- `updatePreference == null` → throw `ArgumentNullException`
- `onChanged == null` → skip callback (valid scenario)

### FR2: LaunchWindowViewModel Refactoring
**Priority:** Must Have

Replace existing property setters with `PreferenceSyncHelper.SetAndSync`.

**Properties to Refactor:**

**IsPinned:**
```csharp
// Current (9 lines)
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

// Proposed (8 lines, but single logical operation)
public bool IsPinned
{
    get => _isPinned;
    set
    {
        if (_suppressPreferenceUpdates) return;
        
        PreferenceSyncHelper.SetAndSync(
            this,
            ref _isPinned,
            value,
            _preferencesStore,
            p => p.PinLaunchWindow = value,
            onChanged: () => _statusMessageCoordinator.ShowMessage(
                value ? "Launch window pinned" : "Launch window unpinned",
                isTransient: true));
    }
}
```

**ShowOnlyProjectAgents:**
```csharp
// Current (9 lines)
public bool ShowOnlyProjectAgents
{
    get => _showOnlyProjectAgents;
    set
    {
        if (!SetProperty(ref _showOnlyProjectAgents, value) || _suppressPreferenceUpdates)
        {
            return;
        }

        _preferencesStore.Update(preferences => preferences.ShowOnlyProjectAgents = value);
        RefreshVisibleAgents();
    }
}

// Proposed (8 lines)
public bool ShowOnlyProjectAgents
{
    get => _showOnlyProjectAgents;
    set
    {
        if (_suppressPreferenceUpdates) return;
        
        PreferenceSyncHelper.SetAndSync(
            this,
            ref _showOnlyProjectAgents,
            value,
            _preferencesStore,
            p => p.ShowOnlyProjectAgents = value,
            onChanged: RefreshVisibleAgents);
    }
}
```

**Behavior Preservation:**
- `_suppressPreferenceUpdates` check still happens (before SetAndSync)
- Property change notification still fires
- Preference store still updates
- Side effects (status message, refresh) still execute
- Return value no longer used (not needed in these properties)

## Non-Functional Requirements

### NFR1: Performance
**Priority:** Should Have

- `SetAndSync` must have O(1) complexity
- No measurable performance degradation in UI responsiveness
- Memory allocation comparable to current pattern

### NFR2: Testability
**Priority:** Must Have

- `PreferenceSyncHelper` must be unit-testable in isolation
- Mock `IPreferencesStore` for testing
- Verify callback invocation patterns
- Achieve >90% code coverage

### NFR3: Maintainability
**Priority:** Must Have

- Clear XML documentation
- Obvious parameter names
- No magic strings or reflection tricks
- Follows existing code style

### NFR4: Compatibility
**Priority:** Must Have

- Works with existing `ObservableObject` base
- No breaking changes to public APIs
- All 402 existing tests pass unchanged

## Technical Constraints

### TC1: ObservableObject Access
**Challenge:** `ObservableObject.OnPropertyChanged` is `protected`, not accessible from static helper.

**Solution:** Call `SetProperty` on the owner first to trigger notification, then do preference sync:

```csharp
public static bool SetAndSync<T>(...)
{
    // Use owner's SetProperty to handle notification
    bool changed = owner.SetProperty(ref field, value, propertyName);
    
    if (!changed)
    {
        return false;
    }
    
    // Now sync preference
    preferencesStore.Update(updatePreference);
    onChanged?.Invoke();
    
    return true;
}
```

**Wait!** This requires `SetProperty` to be `public` or `internal`. Let me check ObservableObject:

Actually, looking at the code, `SetProperty` is already `protected` in ObservableObject. The helper can't call it directly.

**Revised Solution:** Make `SetProperty` `internal protected` OR use a different approach:

```csharp
public static bool SetAndSync<T>(...)
{
    // Manual equality check (same as SetProperty logic)
    if (EqualityComparer<T>.Default.Equals(field, value))
    {
        return false;
    }
    
    field = value;
    
    // Call public method that will raise PropertyChanged
    // Owner must expose a way to trigger notification, OR
    // we just duplicate the logic here
    
    // Duplicate logic approach (cleaner, no base class changes):
    preferencesStore.Update(updatePreference);
    onChanged?.Invoke();
    
    return true;
}
```

**Best Solution:** Keep it simple and don't try to reuse `SetProperty`. The caller will handle `SetProperty`, and the helper only does preference sync:

```csharp
// Simplified - caller does SetProperty
public bool IsPinned
{
    get => _isPinned;
    set
    {
        if (_suppressPreferenceUpdates || !SetProperty(ref _isPinned, value))
        {
            return;
        }
        
        PreferenceSyncHelper.SyncPreference(
            _preferencesStore,
            p => p.PinLaunchWindow = value,
            onChanged: () => _statusMessageCoordinator.ShowMessage(...));
    }
}
```

This is cleaner but offers less reduction. Let me reconsider...

**Final Solution:** Create two helper methods:
1. `SetAndSync` — for properties WITHOUT side effects
2. `SetAndSyncWithCallback` — for properties WITH side effects

Both internally call a private helper that does the full logic by directly manipulating the field and manually raising PropertyChanged through reflection or an internal accessor.

Actually, let's keep it simple: **Just extract the preference sync part, leave SetProperty as-is.**

## API Specification

### PreferenceSyncHelper

**Namespace:** `CLIHub.Helpers`

**Class:**
```csharp
/// <summary>
///   Helper for synchronizing ViewModel properties with preference store.
/// </summary>
public static class PreferenceSyncHelper
{
    /// <summary>
    ///   Updates a preference in the store and optionally invokes a callback.
    /// </summary>
    /// <param name="preferencesStore"> The preference store to update. </param>
    /// <param name="updatePreference"> Action to update the preference. </param>
    /// <param name="onChanged"> Optional callback invoked after preference update. </param>
    public static void SyncPreference(
        IPreferencesStore preferencesStore,
        Action<AppPreferences> updatePreference,
        Action? onChanged = null);
}
```

**Behavior:**
1. Validate arguments (`ArgumentNullException` for nulls)
2. Call `preferencesStore.Update(updatePreference)`
3. If `onChanged != null`, invoke it

**Usage:**
```csharp
public bool IsPinned
{
    get => _isPinned;
    set
    {
        if (_suppressPreferenceUpdates || !SetProperty(ref _isPinned, value))
        {
            return;
        }
        
        PreferenceSyncHelper.SyncPreference(
            _preferencesStore,
            p => p.PinLaunchWindow = value,
            () => _statusMessageCoordinator.ShowMessage(
                value ? "Launch window pinned" : "Launch window unpinned",
                isTransient: true));
    }
}
```

This is much simpler and still reduces duplication!

## Acceptance Criteria

### AC1: Helper Implementation
- [ ] `PreferenceSyncHelper.SyncPreference` created
- [ ] Validates `preferencesStore != null`
- [ ] Validates `updatePreference != null`
- [ ] Calls `preferencesStore.Update(updatePreference)`
- [ ] Invokes `onChanged` if provided
- [ ] XML documentation complete

### AC2: Unit Tests
- [ ] `PreferenceSyncHelperTests.cs` created
- [ ] Test: Null store throws `ArgumentNullException`
- [ ] Test: Null updatePreference throws `ArgumentNullException`
- [ ] Test: Calls preferencesStore.Update
- [ ] Test: Invokes onChanged when provided
- [ ] Test: Does not throw when onChanged is null
- [ ] Code coverage >90%

### AC3: LaunchWindowViewModel Refactoring
- [ ] `IsPinned` uses `PreferenceSyncHelper.SyncPreference`
- [ ] `ShowOnlyProjectAgents` uses `PreferenceSyncHelper.SyncPreference`
- [ ] Behavior unchanged (manual verification)

### AC4: Verification
- [ ] All 402 tests pass
- [ ] No build warnings
- [ ] Code review approved

## File Changes

### New Files
- `src/CLIHub/Helpers/PreferenceSyncHelper.cs`
- `tests/CLIHub.Tests/Helpers/PreferenceSyncHelperTests.cs`

### Modified Files
- `src/CLIHub/ViewModels/LaunchWindowViewModel.cs` (2 properties)

### Estimated Lines of Code
- New: ~50 lines (helper + tests)
- Modified: ~10 lines reduction in LaunchWindowViewModel
- Net: +40 lines, but eliminates duplication

## Dependencies
None (uses existing interfaces)

## Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Helper introduces bugs | Medium | Low | Comprehensive unit tests |
| Refactoring breaks behavior | High | Low | Existing tests + manual verification |
| Over-abstraction reduces clarity | Low | Medium | Keep helper simple, well-documented |

## Success Metrics
- LaunchWindowViewModel: 10 lines removed
- Code duplication eliminated
- All tests pass
- No regressions
