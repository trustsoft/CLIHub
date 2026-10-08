## Why

ApplicationSession currently mixes two responsibilities: owning event subscription lifecycle AND creating/wiring the application shell (tray + launch window). This violates single responsibility principle and makes the session harder to test and understand. Extracting shell creation and wiring into a focused ShellCoordinator simplifies ApplicationSession to pure lifecycle management and makes UI orchestration independently testable.

## What Changes

- Create `IShellCoordinator` interface and `ShellCoordinator` implementation that owns:
  - Tray icon controller creation
  - Launch window creation
  - Update request/outcome event wiring
  - Second-instance activation event wiring
  - Disposal of shell resources
- Reduce `ApplicationSession` to a thin lifecycle boundary (~80 lines) that delegates shell operations to `ShellCoordinator`
- ApplicationSession still owns subscription registration/removal but delegates shell creation to the coordinator
- Add comprehensive unit tests for ShellCoordinator in isolation
- Update ServiceRegistration to include IShellCoordinator

## Capabilities

### New Capabilities

- `shell-coordinator`: Coordinates application shell creation (tray + launch window) and event wiring between update workflow, single-instance guard, and UI components

### Modified Capabilities

- `app-lifecycle`: Update Application startup orchestration requirement to reflect ApplicationSession delegation to ShellCoordinator for shell creation while preserving subscription lifecycle ownership

## Impact

**Code:**
- New files: `IShellCoordinator.cs`, `ShellCoordinator.cs`
- Modified: `ApplicationSession.cs` (147 → ~80 lines), `ServiceRegistration.cs`
- New tests: `ShellCoordinatorTests.cs`

**Architecture:**
- Improved separation of concerns: session lifecycle vs shell orchestration
- Better testability: shell creation testable without full session context
- Easier to extend: new shell components add to coordinator, not session

**Risk:** Low
- Pure refactoring, no behavior changes
- Event subscription patterns preserved exactly
- Disposal order maintained
- All existing tests continue passing
