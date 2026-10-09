## Why

LaunchWindowViewModel (435 lines) manages status messages through direct assignment in 10+ locations scattered across the class: selection changes (lines 159, 178), update outcomes (line 434), agent command results (line 326), errors (lines 373, 377, 402), and window-level preferences (line 222). This pattern:

- Duplicates status message logic across the ViewModel
- Makes it hard to track where status changes originate
- Complicates testing of status message behavior
- Mixes UI status coordination with other ViewModel responsibilities

The current approach treats status messages as a primitive property rather than a coordinated cross-concern that responds to multiple event sources (controller events, command outcomes, user actions).

## What Changes

**This is a pure refactoring with no externally observable behavior changes.**

Extract status message coordination into a focused `StatusMessageCoordinator`:

- New `StatusMessageCoordinator` class in `src/CLIHub/ViewModels/`
- Owns the `CurrentMessage` observable property
- Subscribes to events from controllers, update control, and command coordinator
- Centralizes all status message logic in one testable location
- `LaunchWindowViewModel` exposes `StatusMessageCoordinator` for binding
- ViewModel removes 10+ direct `StatusMessage` assignments
- StatusMessage property becomes pass-through to `StatusMessageCoordinator.CurrentMessage`

The coordinator pattern matches existing architecture: `ProjectPaneController`, `AgentPaneController`, `LaunchCommandCoordinator`, `ShellCoordinator` all follow this boundary.

## Capabilities

**This change uses `skip_specs: true`** — it is a pure internal refactoring with zero behavior changes. All externally observable functionality (status bar messages, timing, content) remains identical. No spec-level requirements change.

## Impact

**Code:**
- Added: `StatusMessageCoordinator.cs` (~80-100 lines)
- Modified: `LaunchWindowViewModel.cs` (435 → ~390 lines, ~10% reduction)
- Modified: `ServiceRegistration.cs` (register coordinator as singleton)
- Modified tests: Add `StatusMessageCoordinatorTests.cs`, update ViewModel tests

**Architecture:**
- Clearer separation: coordinator owns status orchestration, ViewModel only binds
- Better testability: status logic testable without full ViewModel setup
- Easier to extend: new status sources add to coordinator, not ViewModel
- Follows established coordinator pattern in the codebase

**Risk:** Low
- Pure refactoring preserving exact behavior
- Isolated functionality (status messages)
- Existing coordinator pattern proven in production
- Tests verify behavior preservation

**Non-Breaking:**
- Zero user-visible changes
- XAML bindings unchanged (StatusMessage property preserved)
- No public API changes
- No Core contract changes
