# Tasks: Simplify Preference Synchronization

## Overview
Create PreferenceSyncHelper utility to eliminate preference synchronization boilerplate in LaunchWindowViewModel.

**Total Estimated Time:** 1.5 hours

---

## Phase 1: Create Helper (30 min)

### Task 1.1: Create PreferenceSyncHelper Class
**Estimated Time:** 15 min  
**Status:** Pending

Create `src/CLIHub/Helpers/PreferenceSyncHelper.cs`:

```csharp
namespace CLIHub.Helpers;

using CLIHub.Core.Configuration;

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
    /// <exception cref="ArgumentNullException"> Thrown when required parameters are null. </exception>
    public static void SyncPreference(
        IPreferencesStore preferencesStore,
        Action<AppPreferences> updatePreference,
        Action? onChanged = null)
    {
        ArgumentNullException.ThrowIfNull(preferencesStore);
        ArgumentNullException.ThrowIfNull(updatePreference);

        preferencesStore.Update(updatePreference);
        onChanged?.Invoke();
    }
}
```

**Acceptance Criteria:**
- File created in `src/CLIHub/Helpers/` directory
- XML documentation complete
- Argument validation included
- Follows project code style

---

## Phase 2: Add Tests (30 min)

### Task 2.1: Create PreferenceSyncHelperTests
**Estimated Time:** 30 min  
**Status:** Pending

Create `tests/CLIHub.Tests/Helpers/PreferenceSyncHelperTests.cs` with tests:

1. **SyncPreference_NullStore_ThrowsArgumentNullException**
2. **SyncPreference_NullUpdateAction_ThrowsArgumentNullException**
3. **SyncPreference_ValidArguments_CallsUpdate**
4. **SyncPreference_WithCallback_InvokesCallback**
5. **SyncPreference_WithoutCallback_DoesNotThrow**
6. **SyncPreference_CallbackThrows_PropagatesException**

**Acceptance Criteria:**
- All 6 tests pass
- Code coverage >90%
- Uses Moq for IPreferencesStore mocking
- Follows existing test patterns

**Test Example:**
```csharp
[Fact]
public void SyncPreference_ValidArguments_CallsUpdate()
{
    // Arrange
    var mockStore = new Mock<IPreferencesStore>();
    var updateCalled = false;
    Action<AppPreferences> update = p => { updateCalled = true; };

    // Act
    PreferenceSyncHelper.SyncPreference(mockStore.Object, update);

    // Assert
    mockStore.Verify(s => s.Update(It.IsAny<Action<AppPreferences>>()), Times.Once);
    Assert.True(updateCalled);
}
```

---

## Phase 3: Refactor LaunchWindowViewModel (20 min)

### Task 3.1: Refactor IsPinned Property
**Estimated Time:** 10 min  
**Status:** Pending

Update `src/CLIHub/ViewModels/LaunchWindowViewModel.cs`:

**Current (lines 212-226):**
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

**Updated:**
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

        PreferenceSyncHelper.SyncPreference(
            _preferencesStore,
            preferences => preferences.PinLaunchWindow = value,
            () => _statusMessageCoordinator.ShowMessage(
                value ? "Launch window pinned" : "Launch window unpinned",
                isTransient: true));
    }
}
```

**Acceptance Criteria:**
- Property uses PreferenceSyncHelper
- Behavior unchanged
- Code compiles

### Task 3.2: Refactor ShowOnlyProjectAgents Property
**Estimated Time:** 10 min  
**Status:** Pending

Update `src/CLIHub/ViewModels/LaunchWindowViewModel.cs`:

**Current (lines 192-206):**
```csharp
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
```

**Updated:**
```csharp
public bool ShowOnlyProjectAgents
{
    get => _showOnlyProjectAgents;
    set
    {
        if (!SetProperty(ref _showOnlyProjectAgents, value) || _suppressPreferenceUpdates)
        {
            return;
        }

        PreferenceSyncHelper.SyncPreference(
            _preferencesStore,
            preferences => preferences.ShowOnlyProjectAgents = value,
            RefreshVisibleAgents);
    }
}
```

**Acceptance Criteria:**
- Property uses PreferenceSyncHelper
- Behavior unchanged
- Code compiles

---

## Phase 4: Verification (10 min)

### Task 4.1: Run All Tests
**Estimated Time:** 5 min  
**Status:** Pending

```bash
dotnet test
```

**Acceptance Criteria:**
- All 408 tests pass (402 existing + 6 new)
- No test failures
- No test warnings

### Task 4.2: Manual Smoke Test
**Estimated Time:** 5 min  
**Status:** Pending

Manual verification:
1. Launch CLIHub
2. Toggle "Show only project agents" checkbox
   - Verify agent list filters correctly
   - Close and relaunch → verify preference persisted
3. Click pin icon
   - Verify status message appears
   - Verify window stays on top
   - Close and relaunch → verify pin state persisted

**Acceptance Criteria:**
- All preference changes persist
- UI responds correctly
- No exceptions in logs

---

## Task Summary

| Phase | Tasks | Time | Status |
|-------|-------|------|--------|
| 1. Create Helper | 1 | 15 min | Pending |
| 2. Add Tests | 1 | 30 min | Pending |
| 3. Refactor ViewModel | 2 | 20 min | Pending |
| 4. Verification | 2 | 10 min | Pending |
| **Total** | **6** | **1.5 hours** | **0/6 Complete** |

---

## Dependencies
None — all work can proceed sequentially.

---

## Rollback Plan
If verification fails:
1. Revert Task 3.1 and 3.2 changes to LaunchWindowViewModel
2. Keep PreferenceSyncHelper and tests (no harm)
3. Investigate issues
4. Re-attempt refactoring after fixes

---

## Notes
- This is a pure refactoring — no functional changes
- All existing tests must continue passing
- Focus on code clarity and maintainability
