## Why

LaunchWindowViewModel (421 lines) mixes multiple responsibilities: project/agent state binding, menu construction, command execution, preferences management, and application lifetime control. This violates Single Responsibility Principle and makes the view model hard to test, extend, and reason about. Most coordination logic already exists in controllers (`ProjectPaneController`, `AgentPaneController`, `LaunchCommandCoordinator`), but the view model still duplicates or wraps functionality instead of pure composition.

## What Changes

**This is a pure refactoring with no externally observable behavior changes.**

- Strengthen existing controllers to fully own their workflows (reduce ViewModel wrapper methods)
- Extract menu action building into focused `LaunchWindowActionBuilder` (already exists, strengthen it)
- `LaunchWindowViewModel` becomes a thin composition layer (~200-250 lines):
  - Binds to pane controller observables (Projects, Agents)
  - Delegates to controllers for state changes (selection, filtering)
  - Uses ActionBuilder for menu construction
  - Retains binding-facing properties only (SelectedProject, SelectedAgent, StatusMessage, IsPinned, DisplayStyle)
  - No business logic, just property change notifications and command delegation

- Remove duplication between ViewModel and controllers
- All IExternalLauncher usage already exists, no new abstractions needed
- No Core changes, no new dependencies

## Capabilities

**This change uses `skip_specs: true`** — it is a pure internal refactoring with zero behavior changes. All externally observable functionality (launch window behavior, commands, menus, state management) remains identical. No spec-level requirements change.

## Impact

**Code:**
- Modified: `LaunchWindowViewModel.cs` (421 → ~200-250 lines, ~45% reduction)
- Potentially modified: `ProjectPaneController.cs`, `AgentPaneController.cs`, `LaunchWindowActionBuilder.cs` (strengthen existing logic)
- Modified tests: Update ViewModel tests to reflect reduced scope

**Architecture:**
- Clearer separation: ViewModel = UI binding layer, Controllers = workflow boundaries
- Better testability: business logic testable without binding infrastructure
- Easier to extend: new commands add to controllers/builders, not ViewModel
- Reduced coupling: ViewModel depends on focused contracts, not broad service interfaces

**Risk:** Low-Medium
- Pure refactoring preserving exact behavior
- Large file but focused changes
- Existing controllers already own most logic
- Tests verify behavior preservation

**Non-Breaking:**
- Zero user-visible changes
- No public API changes
- No Core contract changes
- XAML bindings unchanged
