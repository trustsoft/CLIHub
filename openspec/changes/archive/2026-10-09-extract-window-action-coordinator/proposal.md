## Why

LaunchWindowViewModel (406 lines) still owns window-level commands (OpenDataFolder, OpenSettings, Exit) and creates all agent commands with their CanExecute logic inline. This mixing of concerns:

- Keeps window action coordination logic in the ViewModel instead of a focused coordinator
- Makes the ViewModel responsible for command creation rather than pure composition
- Spreads command construction across constructor and inline lambdas
- Makes it harder to test window actions independently
- Prevents sharing window actions with other UI surfaces (future tray menu improvements)

Following the established coordinator pattern (StatusMessageCoordinator, MenuActionCoordinator, LaunchCommandCoordinator), window-level actions should live in their own coordinator.

## What Changes

**This is a pure refactoring with no externally observable behavior changes.**

Extract window-level action coordination into a focused `WindowActionCoordinator`:

- New `WindowActionCoordinator` class in `src/CLIHub/ViewModels/`
- Owns window-level commands: `OpenDataFolderCommand`, `OpenSettingsCommand`, `ExitCommand`
- Owns agent action commands: `LaunchCommand`, `ResumeCommand`, `InitCommand`, `UpdateCommand`, `VersionCommand`
- Owns parameterized commands: `LaunchAgentCommand`, `ResumeAgentCommand`
- Encapsulates all CanExecute logic (HasSelectedAgent checks)
- Coordinates with LaunchCommandCoordinator for execution
- Coordinates with StatusMessageCoordinator for folder-open outcomes
- `LaunchWindowViewModel` exposes `WindowActionCoordinator` properties as pass-through
- ViewModel constructor delegates command creation to coordinator

The coordinator owns command instances and their execution logic; the ViewModel becomes an even thinner binding surface.

## Capabilities

**This change uses `skip_specs: true`** — it is a pure internal refactoring with zero behavior changes. All externally observable functionality (window commands, agent commands, CanExecute behavior) remains identical. No spec-level requirements change.

## Impact

**Code:**
- Added: `WindowActionCoordinator.cs` (~120-150 lines)
- Modified: `LaunchWindowViewModel.cs` (406 → ~300 lines, ~26% reduction)
- Modified: `ServiceRegistration.cs` (register coordinator as singleton)
- Modified tests: Add `WindowActionCoordinatorTests.cs`, update ViewModel tests

**Architecture:**
- Clearer separation: coordinator owns window/agent commands, ViewModel only binds
- Better testability: command logic testable without full ViewModel setup
- Easier to extend: new window actions add to coordinator, not ViewModel
- Reusability: window actions can be shared with future UI surfaces
- Follows established coordinator pattern (4th coordinator in the architecture)

**Risk:** Low
- Pure refactoring preserving exact behavior
- Isolated functionality (command creation and execution)
- Existing coordinator pattern proven in production
- Tests verify behavior preservation

**Non-Breaking:**
- Zero user-visible changes
- XAML bindings unchanged (command properties preserved as pass-through)
- No public API changes
- No Core contract changes
