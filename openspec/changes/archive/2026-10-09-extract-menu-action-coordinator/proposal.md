# Proposal: Extract MenuActionCoordinator

## Why

LaunchWindowViewModel currently owns menu action construction and lifecycle management, creating tight coupling between the ViewModel and menu state. Extracting menu concerns into a dedicated coordinator continues the decomposition pattern from Phase 8 (controller commands) and Phase 9a (status messages).

## What Changes

Extract a **MenuActionCoordinator** that:

1. Owns the ProjectsActions and AgentsActions collections
2. Handles menu action construction via LaunchWindowActionBuilder
3. Exposes a ShowOnlyProjectAgents property synchronized with the filter menu action
4. Implements INotifyPropertyChanged for filter state changes
5. Handles its own disposal (unsubscribe from filter action)

### Key Design Points

- **Coordinator lifetime:** Created in LaunchWindowViewModel constructor (not a singleton)
- **Dependencies:** LaunchWindowActionBuilder and all commands needed for menu building
- **One-way sync:** Filter action IsChecked → coordinator.ShowOnlyProjectAgents property (via INotifyPropertyChanged)
- **Observable:** Implements INotifyPropertyChanged for ShowOnlyProjectAgents changes
- **Disposal:** Unsubscribes from filter action PropertyChanged event

### Architecture

```
LaunchWindowViewModel
  ├─ MenuActionCoordinator (created in constructor)
  │   ├─ LaunchWindowActionBuilder (builds menus)
  │   ├─ Commands (passed through from controllers)
  │   └─ ShowOnlyProjectAgents property (reflects filter action state)
  └─ Exposes coordinator.ProjectsActions, coordinator.AgentsActions via properties
```

## Expected Impact

### Line Count Changes
- **LaunchWindowViewModel:** 435 → ~370 lines (~15% reduction)
  - Remove BuildActions() method (~19 lines)
  - Remove OnFilterActionChanged() method (~9 lines)
  - Remove _filterAction field and subscription/unsubscription (~5 lines)
  - Remove _actionBuilder field (~1 line)
  - Simplify constructor (inline coordinator creation, ~5 lines saved)
  - Add coordinator properties as expression-bodied (~2 lines)

- **MenuActionCoordinator:** New file, ~123 lines

### Benefits
1. **Separation of concerns:** Menu logic isolated from ViewModel
2. **Easier testing:** Menu behavior can be tested independently
3. **Clearer responsibility:** ViewModel focuses on pane coordination, coordinator owns menus
4. **Reduced coupling:** ViewModel doesn't manage filter action lifecycle

### Risks
- **Minimal:** This is a straightforward extraction with clear boundaries
- **Note:** PreferencesStore remains in ViewModel (not moved to coordinator)

## Files Affected

### New Files
- `src/CLIHub/ViewModels/MenuActionCoordinator.cs` (123 lines)

### Modified Files
- `src/CLIHub/ViewModels/LaunchWindowViewModel.cs` (435 → ~370 lines)
- `tests/CLIHub.Tests/ViewModels/LaunchWindowViewModelDialogTests.cs` (update constructor calls)

## Success Criteria

1. ✅ MenuActionCoordinator created with constructor instantiation
2. ✅ LaunchWindowViewModel delegates menu ownership to coordinator
3. ✅ Filter action state exposed via coordinator.ShowOnlyProjectAgents property
4. ✅ All menu actions display and execute as before
5. ✅ All existing tests pass
6. ✅ Manual testing confirms menus work identically

## Acceptance Criteria

- [x] MenuActionCoordinator owns ProjectsActions and AgentsActions
- [x] Filter action IsChecked reflected in coordinator.ShowOnlyProjectAgents property
- [x] LaunchWindowViewModel exposes coordinator.ProjectsActions and coordinator.AgentsActions
- [x] All tests pass
- [x] Manual testing: all menu actions work correctly
- [x] LaunchWindowViewModel reduced by ~65 lines
