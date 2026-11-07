## Why

Favorite projects currently appear mixed among other projects in the Launch Window project list — wherever they were registered in storage order. Users mark projects as favorites to give them priority access, but the current display treats favorites no differently from ordinary projects, making favorited projects harder to find.

The Launch Window project list should display favorite projects first, followed by non-favorites, preserving the insertion order within each group. This makes favorited projects immediately visible and actionable without requiring the user to scan the full list.

## What Changes

- Project list ordering becomes favorites-first with stable relative order within each group
- A pure policy function in CLIHub.Core orders the project list before display
- `ProjectPaneController.Refresh()` applies the ordering before enrichment
- Selection and row identity are preserved through the reorder operation
- Settings page, tray menus, and recent-project ordering are unchanged

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `launch-window-layout`: project rows now appear with favorites first, then the rest

## Impact

**Modified code:**
- `src/CLIHub.Core/Projects/ProjectListOrdering.cs` — new static class with `FavoritesFirst(IReadOnlyList<Project>)` pure policy
- `src/CLIHub/ViewModels/Projects/ProjectPaneController.cs` — apply ordering in `Refresh()` before enrichment
- `tests/CLIHub.Core.Tests/Projects/ProjectListOrderingTests.cs` — unit tests for the ordering policy
- `tests/CLIHub.Tests/ViewModels/ProjectPaneControllerTests.cs` — integration tests for refresh behavior

**Reuses:**
- `ProjectModelCatalog` and `ProjectModelListSynchronizer` (existing row identity preservation)
- `Project.IsFavorite` flag (already persisted and toggled)

**No breaking changes.**
**No schema changes.**

## Non-Goals

- Changing the ordering in Settings → Projects page (remains manual-first then name/path sort)
- Changing tray menu project ordering
- Changing recent-project ordering
- Introducing other sort criteria (last-used, alphabetical, manual before discovered)
- Persisting an explicit display order in configuration
