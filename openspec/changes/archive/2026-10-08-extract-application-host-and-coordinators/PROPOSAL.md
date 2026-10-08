# Proposal: Extract ApplicationHost and Startup Coordinators

## Problem

`ApplicationBootstrapper` still mixes multiple concerns (152 lines):
- Single-instance coordination (check + signal)
- Startup state loading (plugins, preferences)
- Session initialization (UI creation, event wiring)
- Input registration (hotkey)
- Optional operations (release notes, update check)

`App.OnStartup` and `App.OnExit` directly handle environment setup and cleanup, mixing WPF lifecycle with application infrastructure.

This makes the startup sequence harder to test in isolation and prevents independent evolution of each concern.

## Solution

Extract startup responsibilities according to the target architecture defined in `improvements.md`:

### 1. InstanceCoordinator
Encapsulates single-instance detection and activation signaling.
- Input: `ISingleInstanceGuard`, `IApplicationLifetime`
- Output: `InstanceStatus` enum (FirstInstance | SecondInstance)
- Responsibility: decide whether to continue or shutdown a second instance

### 2. StartupStateLoader
Encapsulates required startup state preparation.
- Input: plugin initialization, preferences store, startup preferences applier
- Output: immutable `StartupState` record (preferences + metadata)
- Responsibility: initialize plugins, load preferences, apply startup preferences
- Returns structured state for later stages

### 3. OptionalStartupCoordinator
Encapsulates best-effort startup operations.
- Input: release notes coordinator, update startup coordinator
- Responsibility: run release notes evaluation and startup update check with best-effort failure policy
- Logs warnings on failure but does not block startup

### 4. ApplicationHost
Encapsulates WPF-facing startup and shutdown boundary.
- Responsibility: 
  - Environment preparation (directories, logging)
  - DI container building
  - Bootstrapper orchestration
  - Cleanup sequence (session disposal, operation stop, provider disposal, log flush)
- Replaces direct logic in `App.OnStartup` and `App.OnExit`

### 5. ApplicationBootstrapper (simplified)
Becomes a pure sequencer:
- InstanceCoordinator → StartupStateLoader → ApplicationSession.Start → OptionalStartupCoordinator
- No direct service calls except coordination
- Target: < 80 lines

## Incremental Steps

1. **Extract InstanceCoordinator** (Step 6.1)
   - Create `InstanceCoordinator` + `InstanceStatus` enum
   - Move single-instance logic from `ApplicationBootstrapper`
   - Bootstrapper uses result to decide continue/shutdown

2. **Extract StartupStateLoader** (Step 6.2)
   - Create `StartupStateLoader` + `StartupState` record
   - Move plugin init, preferences load/apply
   - Bootstrapper receives immutable state

3. **Extract OptionalStartupCoordinator** (Step 6.3)
   - Create `OptionalStartupCoordinator`
   - Move release notes + update check with best-effort policy
   - Bootstrapper delegates optional work

4. **Extract ApplicationHost** (Step 6.4)
   - Create `ApplicationHost` + `IApplicationHost`
   - Move environment setup from `App.OnStartup`
   - Move cleanup from `App.OnExit`
   - `App` becomes thin WPF shell

5. **Documentation and tests** (Step 6.6)
   - Update `docs/architecture/startup.md`
   - Update `improvements.md` progress table
   - Add architecture tests for new boundaries

## Benefits

- **Single Responsibility**: each coordinator owns one concern
- **Testability**: coordinators can be tested without WPF or full DI graph
- **Clarity**: startup sequence explicit through coordinator names
- **Evolvability**: coordinators can change independently
- **Target Architecture**: aligns with `improvements.md` target structure

## Constraints

- Startup order unchanged
- No behavior changes
- All 497 tests still pass
- Preserve all error handling and logging
- Keep `ServiceRegistration` as only composition root

## Success Criteria

- `ApplicationBootstrapper` < 80 lines (currently 152)
- `App.OnStartup`/`OnExit` delegate to `ApplicationHost`
- Single-instance, startup state, optional operations have dedicated coordinators
- All tests pass
- Documentation reflects new structure
