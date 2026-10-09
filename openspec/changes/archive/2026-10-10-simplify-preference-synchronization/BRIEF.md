# Brief: Simplify Preference Synchronization

## Overview
Reduce boilerplate in LaunchWindowViewModel preference property setters by introducing a reusable helper that combines property change notification with preference store synchronization.

## Problem
Two properties in LaunchWindowViewModel (`ShowOnlyProjectAgents` and `IsPinned`) repeat the same pattern:
```csharp
public bool PropertyName
{
    get => _field;
    set
    {
        if (!SetProperty(ref _field, value) || _suppressFlag)
        {
            return;
        }
        
        _preferencesStore.Update(preferences => preferences.PropertyName = value);
        
        // Optional side effect
    }
}
```

This pattern is duplicated, verbose, and prone to inconsistency if modified.

## Goals
1. **Reduce duplication** — eliminate repeated SetProperty + Update pattern
2. **Maintain clarity** — solution should be easy to understand and debug
3. **Preserve behavior** — no functional changes, only refactoring
4. **Keep testability** — helper should be unit-testable

## Non-Goals
- Changing preference persistence mechanism
- Modifying ObservableObject base class
- Abstracting side effects (status messages, refresh calls)

## Constraints
- No breaking changes to public APIs
- No new dependencies
- Must work with existing IPreferencesStore interface
- Preserve all existing tests (100% pass rate)

## Success Criteria
1. LaunchWindowViewModel property setters reduced by ~15-20 lines
2. All 402 existing tests pass
3. Helper class has >90% test coverage
4. Code review approval

## Estimated Effort
1-2 hours (Low complexity)

## Priority
Low — quality of life improvement, no functional impact
