## Context

LaunchWindowViewModel (421 lines) currently combines:
1. **UI binding state**: Projects, Agents, Selected items, IsPinned, DisplayStyle, StatusMessage
2. **Menu construction**: ProjectsActions, AgentsActions building via action builder
3. **Command delegation**: 13 commands that wrap controller/service calls
4. **Event subscription**: ProjectsChanged, OutcomeReported handling
5. **Refresh coordination**: RefreshProjects(), RefreshAgents() orchestrating pane controllers

Existing controllers already own workflows:
- `ProjectPaneController` — project CRUD, selection, persistence
- `AgentPaneController` — agent refresh, selection, filtering
- `LaunchCommandCoordinator` — agent command execution with context
- `LaunchWindowActionBuilder` — action menu construction logic

Current duplication:
- ViewModel wraps `_projectPane.AddProject()`, `.RemoveProject()`, `.ToggleFavorite()` instead of exposing controller commands
- `BuildActions()` calls action builder but stores result locally
- `RefreshAgents()` manually calls controller and handles filter logic
- Event handlers (OnProjectsChanged, OnUpdateOutcomeReported) update state that could live in controllers

See proposal.md for motivation.

## Goals / Non-Goals

**Goals:**
- Reduce LaunchWindowViewModel to pure composition and binding layer (~200-250 lines)
- Strengthen controllers to fully own their workflows without ViewModel wrappers
- Remove duplication between ViewModel and controllers
- Zero behavior changes — preserve exact command execution, menu content, refresh timing

**Non-Goals:**
- Adding new functionality or commands
- Changing public properties or XAML bindings (breaking UI contract)
- Modifying Core abstractions or introducing new Core dependencies
- Extracting LaunchWindowActionBuilder (already exists and is used)

## Decisions

### Decision 1: Controllers expose Commands directly

**Rationale:** ViewModel currently wraps controller methods in RelayCommands (e.g., `AddProjectCommand = new RelayCommand(AddProject)` which calls `_projectPane.AddProject()`). Controllers should expose their own ICommand properties.

**Chosen:** 
- `ProjectPaneController` exposes `AddCommand`, `RemoveCommand`, `ToggleFavoriteCommand`
- `AgentPaneController` exposes `RefreshCommand`
- ViewModel binds XAML to `_projectPane.AddCommand` via pass-through properties if needed, or XAML binds directly to controllers

**Alternatives considered:**
- Keep ViewModel wrappers → Maintains duplication and doesn't reduce size
- Expose methods and let ViewModel wrap → Current pattern, doesn't achieve goal

### Decision 2: RefreshAgents logic moves to AgentPaneController

**Rationale:** `RefreshAgents()` in ViewModel currently calls `_agentPane.Refresh(project, showOnlyAvailable)`. The filter logic and project context belong in the controller.

**Chosen:** `AgentPaneController.Refresh(project, filterToAvailable)` absorbs this logic. ViewModel calls it when `SelectedProject` or `ShowOnlyProjectAgents` changes.

**Alternatives considered:**
- Keep in ViewModel → Doesn't reduce ViewModel responsibilities
- Create separate FilterController → Over-engineering for one filter

### Decision 3: Menu action building stays coordinated by ViewModel

**Rationale:** `LaunchWindowActionBuilder` needs multiple inputs (selected project, agent, filter state, display style) that span controllers. Keeping coordination in ViewModel is simplest.

**Chosen:** ViewModel calls `_actionBuilder.Build*(...)` and stores results in `ProjectsActions`/`AgentsActions`. No change from current pattern.

**Alternatives considered:**
- Move to separate MenuCoordinator → Adds type without clear benefit
- Expose builder directly → XAML would need complex multi-source binding

### Decision 4: StatusMessage remains ViewModel property

**Rationale:** Status messages come from multiple sources (selection changes, update outcomes, command results). A shared "status bar" naturally belongs to the composition layer.

**Chosen:** Keep `StatusMessage` as ViewModel property, updated by event handlers and selection changes.

**Alternatives considered:**
- Move to separate StatusViewModel → Over-engineering for one string
- Let components set status independently → Loses single notification point

### Decision 5: IsPinned and DisplayStyle remain ViewModel properties

**Rationale:** These are window-level preferences that don't belong to any specific controller. They affect ViewModel behavior (hide on lost focus) and display formatting.

**Chosen:** Keep as ViewModel properties with preferences store synchronization.

**Alternatives considered:**
- Extract to WindowStateController → Premature abstraction
- Move to PreferencesStore directly → Breaks reactive binding

### Decision 6: Disposal remains in ViewModel

**Rationale:** ViewModel owns subscriptions to controller events (`ProjectsChanged`, `UpdateControl.OutcomeReported`). It should dispose them.

**Chosen:** Keep `Dispose()` in ViewModel, remove subscriptions it owns.

**Alternatives considered:**
- Controllers own disposal → They don't know about ViewModel subscriptions
- Remove disposal → Leaks subscriptions and handlers

## Risks / Trade-offs

**[Risk] Controllers exposing Commands increases their public API surface** → Mitigation: Commands are already part of their responsibility, just not exposed yet; tests verify command behavior

**[Risk] Moving logic into controllers could create god controllers** → Mitigation: Each controller stays focused on one pane (Project or Agent); coordination remains in ViewModel

**[Risk] XAML binding changes if switching to controller commands** → Mitigation: Provide pass-through properties in ViewModel to maintain binding contract if needed; verify UI behavior after changes

**[Trade-off] ViewModel size reduction (~45%) vs increased controller complexity** → Accepted: Improved testability and separation of concerns justify the distribution

**[Trade-off] Some coordination logic must stay in ViewModel** → Accepted: Composition layer naturally coordinates multiple focused components

## Open Questions

None — design is fully specified. Implementation will verify whether XAML bindings need pass-through properties or can bind directly to controllers.
