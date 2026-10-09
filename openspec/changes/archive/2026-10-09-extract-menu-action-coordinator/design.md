# Design: Extract MenuActionCoordinator

## Context

LaunchWindowViewModel currently manages menu action construction directly:
- Calls `LaunchWindowActionBuilder.Build()` in constructor
- Stores ProjectsActions, AgentsActions, and _filterAction as fields
- Subscribes to _filterAction.PropertyChanged
- Manually synchronizes ShowOnlyProjectAgents property with filter action state

This creates ~40 lines of menu coordination logic mixed with ViewModel responsibilities.

See proposal.md for motivation.

## Goals / Non-Goals

**Goals:**
- Extract menu construction and lifecycle into MenuActionCoordinator
- Expose filter state via coordinator.ShowOnlyProjectAgents property
- Reduce LaunchWindowViewModel complexity by ~65 lines
- Maintain existing menu behavior with zero functional changes

**Non-Goals:**
- Two-way preference synchronization (PreferencesStore remains in ViewModel)
- Singleton registration (coordinator created in ViewModel constructor)
- Comprehensive coordinator unit tests (deferred - existing tests provide coverage)

## Decisions

### Decision 1: Constructor instantiation vs DI singleton

**Chosen:** Create coordinator in LaunchWindowViewModel constructor

**Rationale:**
- Simpler: No ServiceRegistration changes needed
- Coordinator lifetime bound to ViewModel (clear ownership)
- All dependencies already available in ViewModel constructor

**Alternative considered:** Register as singleton in ServiceRegistration
- Rejected: Adds DI complexity without clear benefit
- Coordinator has no shared state across ViewModels

### Decision 2: One-way vs two-way filter synchronization

**Chosen:** One-way (filter action → coordinator.ShowOnlyProjectAgents property via INotifyPropertyChanged)

**Rationale:**
- PreferencesStore remains in ViewModel (simpler separation)
- Coordinator only owns menu state, not persistence logic
- ViewModel continues managing preference updates

**Alternative considered:** Full two-way sync with PreferencesStore injection
- Rejected: Increases coordinator complexity
- Would require coordinator to understand persistence semantics

### Decision 3: Expose ShowOnlyProjectAgents property on coordinator

**Chosen:** Yes - expose as property that reflects _filterAction.IsChecked

**Rationale:**
- Allows binding to filter state without exposing MenuAction directly
- Coordinator raises PropertyChanged when filter action changes
- Clean API for consumers

**Alternative considered:** Only expose ProjectsActions/AgentsActions collections
- Rejected: ViewModel would need to dig into collections to read filter state

## Risks / Trade-offs

**Risk:** Coordinator created inline in ViewModel constructor (not testable in isolation via mocking)  
→ **Mitigation:** Existing LaunchWindowViewModel tests provide coverage; coordinator logic is simple

**Risk:** No dedicated coordinator unit tests initially  
→ **Mitigation:** Can be added later if behavior becomes complex; current implementation is straightforward

**Trade-off:** PreferencesStore duplication (both ViewModel and filtering logic touch it)  
→ **Accepted:** Keeps coordinator simpler; preference management remains ViewModel concern

## Implementation Summary

**MenuActionCoordinator (123 lines):**
```csharp
public sealed class MenuActionCoordinator : ObservableObject, IDisposable
{
    public IReadOnlyList<MenuAction> ProjectsActions { get; }
    public IReadOnlyList<MenuAction> AgentsActions { get; }
    
    public bool ShowOnlyProjectAgents
    {
        get => _filterAction.IsChecked;
        set => _filterAction.IsChecked = value;
    }
    
    public MenuActionCoordinator(
        ICommand addProject, ICommand removeProject, ICommand toggleFavorite,
        ICommand refresh, ICommand launch, ICommand resume, ICommand initialize,
        ICommand update, ICommand showVersion,
        LaunchWindowActionBuilder actionBuilder,
        bool initialFilterState)
    {
        var actions = actionBuilder.Build(...);
        ProjectsActions = actions.Projects;
        AgentsActions = actions.Agents;
        _filterAction = actions.FilterAction;
        _filterAction.PropertyChanged += OnFilterActionChanged;
    }
    
    private void OnFilterActionChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MenuAction.IsChecked))
            OnPropertyChanged(nameof(ShowOnlyProjectAgents));
    }
    
    public void Dispose()
    {
        _filterAction.PropertyChanged -= OnFilterActionChanged;
    }
}
```

**LaunchWindowViewModel changes (~65 lines removed):**
- Remove _actionBuilder field
- Remove _filterAction field  
- Remove BuildActions() method
- Remove OnFilterActionChanged() handler
- Change ProjectsActions/AgentsActions to expression-bodied properties
- Create coordinator inline in constructor
- Dispose coordinator in Dispose()

**Data flow:**
```
User clicks filter checkbox
  ↓
MenuAction.IsChecked changes
  ↓
MenuActionCoordinator.OnFilterActionChanged fires
  ↓
Raises PropertyChanged(nameof(ShowOnlyProjectAgents))
  ↓
LaunchWindowViewModel can bind to coordinator.ShowOnlyProjectAgents
```
