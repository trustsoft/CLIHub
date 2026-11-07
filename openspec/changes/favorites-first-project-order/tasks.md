## 1. Core ordering policy

- [x] 1.1 Create `src/CLIHub.Core/Projects/ProjectListOrdering.cs` with a static `FavoritesFirst(IReadOnlyList<Project>)` method that partitions projects into favorites first, non-favorites second, preserving relative order within each group
- [x] 1.2 Build `CLIHub.Core` with 0 errors/warnings

## 2. Core tests

- [x] 2.1 Create `tests/CLIHub.Core.Tests/Projects/ProjectListOrderingTests.cs`
- [x] 2.2 Add test `FavoritesFirst_MixedList_FavoritesBeforeNonFavorites` (favorites come before non-favorites)
- [x] 2.3 Add test `FavoritesFirst_AllFavorites_PreservesOrder` (all favorites, original order preserved)
- [x] 2.4 Add test `FavoritesFirst_NoFavorites_PreservesOrder` (no favorites, original order preserved)
- [x] 2.5 Add test `FavoritesFirst_EmptyList_ReturnsEmpty` (empty input returns empty)
- [x] 2.6 Add test `FavoritesFirst_SingleFavorite_ReturnsSingle` (single favorite project)
- [x] 2.7 Add test `FavoritesFirst_StableWithinGroups_PreservesInsertionOrder` (storage order stable within favorites and within non-favorites)
- [x] 2.8 Run `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj` and verify all new tests pass

## 3. Launch Window integration

- [x] 3.1 Wire `ProjectListOrdering.FavoritesFirst` into `ProjectPaneController.Refresh()` between `GetAllProjects()` and enrichment (replace line 86 with ordering call before enrichment)
- [x] 3.2 Build `CLIHub` project with 0 errors/warnings

## 4. WPF integration tests

- [x] 4.1 Extend the existing `tests/CLIHub.Tests/ViewModels/Projects/ProjectPaneControllerTests.cs` (do not recreate it)
- [x] 4.2 Add test `Refresh_MixedFavorites_FavoriteRowsFirst` (favorite project rows appear before non-favorite rows after refresh)
- [x] 4.3 Add test `Refresh_TogglingFavorite_ReordersOnNextRefresh` (toggling favorite and refreshing moves project to correct group)
- [x] 4.4 Add test `Refresh_Reordering_PreservesSelection` (current project remains selected after reorder)
- [x] 4.5 Add test `Refresh_Reordering_PreservesRowIdentity` (same ProjectModel instances after reorder)
- [x] 4.6 Run `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj` and verify all new tests pass

## 5. Settings ordering unchanged gate

- [x] 5.1 Add or extend test in `tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs` asserting Settings page orders manual-first, then by name, then by path (verifies Settings page ordering unchanged)
- [x] 5.2 Run `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --filter "FullyQualifiedName~ProjectRegistryViewModel"` and verify all Settings tests pass

## 6. Build and test gate

- [x] 6.1 Run `dotnet build CLIHub.sln` with 0 errors/warnings
- [x] 6.2 Run `dotnet test CLIHub.sln` and verify all tests pass

## 7. Specs and validation

- [x] 7.1 Confirm the change's delta files are present as planned: `specs/launch-window-layout/spec.md`
- [x] 7.2 Run `openspec validate --all` and verify 0 errors

## 8. Manual smoke (user, not the implementer)

- [x] 8.1 Launch Window shows favorites first after restart
- [x] 8.2 Toggling favorite moves the row after the refresh
- [x] 8.3 Selection stays on the same project through reorder
- [x] 8.4 Settings → Projects order looks unchanged (manual-first, then name/path)

> **Note:** The implementer MUST leave stage 8 checkboxes unticked and MUST NOT claim GUI verification. The orchestrator reports these smoke tests to the user for confirmation.

> Implementation notes:
> - Stage 1-2: Core policy is UI-independent and fully unit-tested
> - Stage 3-4: UI integration reuses existing synchronizer; tests verify refresh behavior
> - Stage 5: Explicit gate ensures Settings page ordering is unchanged
> - Stage 6: Full solution gate before specs
> - Stage 7: Spec deltas validate after implementation is verified
