## Context

See proposal.md - Why. The project list display is governed by `ProjectPaneController.Refresh()` (src/CLIHub/ViewModels/Projects/ProjectPaneController.cs:84-93), which reads projects from `IProjectService.GetAllProjects()`, enriches them, and synchronizes the rows in place. `GetAllProjects()` returns projects in configuration storage order (insertion order). The existing `ProjectModelListSynchronizer` preserves row identity and selection during list updates.

## Current State

**File: src/CLIHub/ViewModels/Projects/ProjectPaneController.cs, Lines: 84-93**

```csharp
public ProjectModel? Refresh()
{
    var models = this.catalog.Sync(this.enricher.Enrich(this.projectService.GetAllProjects()));
    ProjectModelListSynchronizer.Synchronize(this.Projects, models);

    var currentId = this.GetCurrentProject()?.Id;
    return currentId is null
        ? null
        : models.FirstOrDefault(model => model.Id == currentId);
}
```

Projects are enriched and synchronized in the order returned by `GetAllProjects()`. There is no ordering step between retrieval and enrichment. A favorited project appears wherever it was registered in the backing configuration.

**File: src/CLIHub.Core/Projects/IProjectService.cs, Line: 19**

```csharp
IReadOnlyList<Project> GetAllProjects();
```

Returns all registered projects as detached copies in storage order.

**File: src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs, Lines: 136-143**

The Settings → Projects page orders projects manually-first, then by name and path:

```csharp
var models = this.catalog.Sync(this.enricher.Enrich(this.projects.GetAllProjects()));
var ordered = models
    .OrderByDescending(model => model.IsManual)
    .ThenBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
    .ThenBy(model => model.Path, StringComparer.OrdinalIgnoreCase)
    .ToList();
ProjectModelListSynchronizer.Synchronize(this.Projects, ordered);
```

This ordering is local to the Settings page and must remain unchanged.

## Goals / Non-Goals

**Goals:**
- Favorites appear first, non-favorites after, preserving storage order within each group
- One pure policy in CLIHub.Core, reusable and testable independently of UI
- Selection and row identity preserved through reorder (existing synchronizer handles this)
- Settings → Projects page ordering unchanged

**Non-Goals:**
- Changing tray menu or recent-project ordering
- Adding other sort criteria (alphabetical, last-used)
- Persisting an explicit ordering in configuration

## Decisions

### Decision 1: Stable two-group partition

**Chosen:** Partition projects into two groups — favorites (`IsFavorite == true`) come first, followed by non-favorites, with the original storage order preserved within each group.

**Rationale:** The brief specifies "stable partition" with "existing storage order preserved within each group." Stable sort is the most predictable behavior: toggling favorite moves a project between groups but does not reorder projects within a group. Users can predict where a project will land when favorited or unfavorited.

**Alternatives considered:**
- Sort favorites alphabetically: Rejected — the brief requires preserving storage order within each group.
- Sort by last-used within favorites: Rejected — out of scope; this change is ordering-only.

**Evidence:** The orchestrator brief states: "a stable partition — projects with `IsFavorite == true` come first, then the rest, and within each group the existing storage order is preserved."

### Decision 2: Pure policy in CLIHub.Core next to `ProjectPathPolicy`

**Chosen:** Create `src/CLIHub.Core/Projects/ProjectListOrdering.cs` as a static class with a single method:

```csharp
public static IReadOnlyList<Project> FavoritesFirst(IReadOnlyList<Project> projects)
```

Place it in the same directory as the existing `ProjectPathPolicy.cs` (src/CLIHub.Core/Projects/).

**Rationale:** 
- The orchestrator brief suggests "a small pure policy in CLIHub.Core (for example `ProjectListOrdering.FavoritesFirst(IReadOnlyList<Project>)` next to the existing project policies)."
- `ProjectPathPolicy` is the only existing project policy class, establishing naming and placement convention.
- Pure functions (no dependencies, no state) are easily tested and reasoned about.
- Placing the policy in Core rather than the WPF layer makes it reusable across UI surfaces if needed later.

**Alternatives considered:**
- Extension method on `IEnumerable<Project>`: Rejected — static method is simpler and does not pollute extension space.
- LINQ directly in `ProjectPaneController.Refresh()`: Rejected — extracting the policy makes testing and future reuse easier.

**Evidence:** 
- File: src/CLIHub.Core/Projects/ProjectPathPolicy.cs exists and is a static class with pure methods.
- AGENTS.md line 10: "`CLIHub.Core` has no WPF dependency. Keep UI-independent logic in Core and UI composition in `CLIHub`."

### Decision 3: Apply ordering in `ProjectPaneController.Refresh()` before enrichment

**Chosen:** Insert ordering between service read and enrichment:

```csharp
public ProjectModel? Refresh()
{
    var ordered = ProjectListOrdering.FavoritesFirst(this.projectService.GetAllProjects());
    var models = this.catalog.Sync(this.enricher.Enrich(ordered));
    ProjectModelListSynchronizer.Synchronize(this.Projects, models);

    var currentId = this.GetCurrentProject()?.Id;
    return currentId is null
        ? null
        : models.FirstOrDefault(model => model.Id == currentId);
}
```

**Rationale:**
- Ordering before enrichment ensures the enriched models arrive in display order.
- The existing `ProjectModelListSynchronizer` already preserves row identity and selection through list updates; ordering is transparent to the synchronizer.
- Single call site makes the change localized and easy to verify.

**Alternatives considered:**
- Ordering after enrichment: Rejected — ordering `Project` records is clearer than ordering enriched models, and the enricher preserves order.
- Ordering inside `IProjectService.GetAllProjects()`: Rejected — the service returns projects in storage order, which is a neutral, reusable contract. Presentation ordering is a display concern.

**Evidence:**
- The brief states: "The ordering is a display concern computed on every refresh."
- File: src/CLIHub/ViewModels/Projects/ProjectPaneController.cs, line 87 calls `ProjectModelListSynchronizer.Synchronize` which handles row identity and selection.

### Decision 4: Settings → Projects page ordering remains unchanged

**Chosen:** The Settings → Projects page (`ProjectRegistryViewModel.Load()`) continues to use its existing manual-first ordering (lines 138-142), independent of the Launch Window ordering.

**Rationale:** The brief explicitly states: "The Settings → Projects page ordering (manual-first, then name/path in `ProjectRegistryViewModel.Load`) must NOT change."

**Evidence:** 
- File: src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs, lines 138-142.
- Orchestrator brief: "Scope is the Launch Window Projects pane only."

## Risks / Trade-offs

**[Risk] Users expect different ordering (alphabetical, last-used)** → Mitigation: This is the first ordering step; additional criteria can layer on top later. Favorites-first is the most frequent user request.

**[Risk] Toggling favorite on a large list causes visible list churn** → Mitigation: The `ProjectModelListSynchronizer` already moves rows in place efficiently. Reordering on refresh is deferred until the next refresh, so the UI does not flicker mid-toggle.

**[Trade-off] Launch Window and Settings page have different ordering** → Benefit: Each surface can optimize for its use case (quick access vs. comprehensive management). Cost: Users may notice the discrepancy; we accept this as intentional.

## Implementation Plan

1. **Create `ProjectListOrdering.cs` in Core** with `FavoritesFirst` method
2. **Add Core unit tests** for all ordering scenarios
3. **Wire ordering into `ProjectPaneController.Refresh()`** 
4. **Add WPF integration tests** for refresh behavior with favorites
5. **Build and test gate**

Rollback: revert code; no persisted state changes.

## Open Questions

None. All decisions are grounded in verified current state and explicit brief requirements.
