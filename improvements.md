# Architecture Improvement Roadmap

This document tracks ongoing and planned architectural improvements for CLIHub.

## Completed

### Phase 8: LaunchWindowViewModel Decomposition (2026-10-08)
**Goal:** Verify and strengthen controller command exposure pattern.

**Status:** ✅ Complete

**Changes:**
- Verified ProjectPaneController already exposes commands (AddCommand, RemoveCommand, ToggleFavoriteCommand, RefreshCommand)
- Verified AgentPaneController already exposes RefreshCommand with filtering
- Verified LaunchWindowViewModel already uses pass-through commands
- Added comprehensive tests for controller commands and ViewModel pass-through
- All 535 tests passing

**Outcome:** Architecture was already in desired state; tests added to verify existing patterns.

**Artifacts:** `openspec/changes/archive/2026-10-08-decompose-launch-window-viewmodel/`

### Phase 9a: Extract StatusMessageCoordinator (2026-10-09)
**Goal:** Centralize status message coordination from LaunchWindowViewModel.

**Status:** ✅ Complete

**Changes:**
- Created `StatusMessageCoordinator` (~114 lines) with centralized status message logic
- Subscribes to ProjectsChanged and UpdateControl.OutcomeReported events
- Exposes methods: ReportProjectSelected, ReportAgentSelected, ReportWindowPinChanged, ReportCommandOutcome, ReportFolderOpen, ReportNoAgentsFound
- Registered as singleton in DI before LaunchWindowViewModel
- LaunchWindowViewModel updated to use coordinator (10+ direct assignments replaced)
- Comprehensive tests added for all coordinator methods
- All tests passing

**Artifacts:** `openspec/changes/archive/2026-10-09-extract-status-message-coordinator/`

### Phase 9b: Extract MenuActionCoordinator (2026-10-09)
**Goal:** Centralize menu action construction and state synchronization.

**Status:** ✅ Complete

**Changes:**
- Created `MenuActionCoordinator` that owns ProjectsActions and AgentsActions
- Subscribes to command CanExecute changes and filter preference changes
- Automatically rebuilds menus when state changes
- ViewModel exposes coordinator properties for binding
- LaunchWindowViewModel simplified significantly
- Comprehensive tests added
- All tests passing

**Artifacts:** `openspec/changes/archive/2026-10-09-extract-menu-action-coordinator/`

### Phase 9c: Extract WindowActionCoordinator (2026-10-09)
**Goal:** Centralize window-level actions and agent commands.

**Status:** ✅ Complete

**Changes:**
- Created `WindowActionCoordinator` (~154 lines) for window-level actions
- Owns all command instances: Launch, Resume, Init, Update, Version, OpenSettings, OpenDataFolder, Exit
- Delegates execution to LaunchCommandCoordinator and StatusMessageCoordinator
- LaunchWindowViewModel uses pass-through pattern for all commands
- Comprehensive tests added for all commands
- All 556 tests passing

**Outcome:** LaunchWindowViewModel successfully decomposed into focused coordinators with clear separation of concerns.

**Artifacts:** `openspec/changes/archive/2026-10-09-extract-window-action-coordinator/`

### Phase 10: UpdateService Refactoring (2026-10-09)
**Goal:** Extract update responsibilities from monolithic UpdateService.

**Status:** ✅ Complete

**Changes:**
- Created `VelopackManagerProvider` to isolate Velopack dependencies
- Extracted `UpdateChecker` for version checking logic
- Extracted `UpdateDownloader` with thread-safe download management
- Extracted `UpdateInstaller` for update application
- UpdateService refactored to orchestrate new components
- All 555 tests passing (387 Core + 168 UI)

**Outcome:** UpdateService successfully decomposed into focused components with clear separation of concerns.

**Artifacts:** `openspec/changes/archive/2026-10-09-update-service-refactoring/`

### Phase 11: Process Subsystem Refinement (2026-10-09)
**Goal:** Separate runtime detection, process spawning, and output capture concerns.

**Status:** ✅ Complete

**Changes:**
- Created RuntimeType enum and RuntimeInfo record
- Extracted `RuntimeSelector` with preference-based selection and caching
- Created `ProcessResult` record for standardized outcomes
- Extracted `WindowsInteractiveProcessRunner` implementing IInteractiveProcessRunner
- Extracted `WindowsProcessOutputRunner` implementing IProcessOutputRunner
- ProcessLauncher refactored to delegate to focused runners
- Updated AgentCommandService and AgentVersionService to use new interfaces
- Comprehensive tests added for all new components
- All 555 tests passing

**Outcome:** Process subsystem successfully decomposed with improved testability and separation of concerns.

**Artifacts:** `openspec/changes/archive/2026-10-09-process-subsystem-refinement/`

---

## Planned Next Steps

### Phase 14: Agent Process Monitoring for Safe Updates
**Goal:** Add monitoring of running agent processes to prevent updates while agents are active.

**Priority:** High (critical for update safety)  
**Estimated Effort:** 4-6 hours  
**Complexity:** Medium-High

**Current Problem:**
- Update command doesn't check if agents are currently running
- Updating while agents are active could cause crashes or data loss
- No visibility into which agent processes are currently running

**Proposed Solution:**

**Phase 1: Core Process Detection (2-3 hours)**
- Create `IAgentProcessInspector` interface for process detection
- Implement `WindowsAgentProcessInspector` using WMI/Process API
- Create `AgentProcessInstance` model (PID, executable path, command line, start time)
- Add unit tests for process detection logic

**Phase 2: Agent Matching Logic (1-2 hours)**
- Create `AgentProcessMatcher` service
- Implement matching rules (executable path, command line patterns)
- Match running processes to registered agents/plugins
- Add tests for matching logic with various edge cases

**Phase 3: Update Command Integration (1-2 hours)**
- Update `UpdateCommand.CanExecute` to check for running agent processes
- Add double-check before starting update (processes might start between check and execution)
- Update UI to show blocking reason when agents are running
- Display list of running agents preventing update
- Add integration tests for update blocking

**Expected Benefits:**
- Prevents update-related crashes and data corruption
- Clear user feedback when update is blocked
- Foundation for future "graceful shutdown" feature

**Dependencies:**
- Existing Infrastructure.Processes subsystem (Phase 11)
- UpdateCommand and UpdateControl

**Testing Strategy:**
- Unit tests for process detection and matching (15+ tests)
- Integration tests with mock processes
- Manual testing with real agent processes

---

### Phase 12: Simplify Preference Synchronization
**Goal:** Reduce boilerplate in preference property setters.

**Priority:** Low  
**Estimated Effort:** 1-2 hours  
**Complexity:** Low

**Current Problem:**
- Pattern `SetProperty → Update PreferencesStore → Side Effect` repeated in multiple properties
- IsPinned, ShowOnlyProjectAgents, DisplayStyle all follow same pattern
- Duplication and potential for inconsistency

**Proposed Solution:**
Option A: Create `PreferenceSyncHelper` utility with generic SetPreference method
Option B: Extend `ObservableObject` base class with preference-aware setter
Option C: Keep current pattern (it's explicit and clear)

**Expected Impact:**
- LaunchWindowViewModel: ~20-30 lines reduction if pursuing A or B
- More DRY code, less duplication
- May reduce clarity (explicit is better than implicit)

**Recommendation:** Consider after current refactoring cycle; may not be worth the abstraction cost.

---

### Phase 13: Agent Subsystem Enhancement
**Goal:** Add agent capability discovery and per-agent configuration.

**Priority:** Medium  
**Estimated Effort:** 6-8 hours  
**Complexity:** High

**Current Problem:**
- All agents treated uniformly (same commands, same timeout, same behavior)
- No way to specify custom commands per agent
- No capability discovery (which commands does this agent support?)
- No per-agent configuration (custom probe timeout, custom working directory)

**Proposed Solution:**
- Extend plugin.json with optional `capabilities` section
- Add `supportedCommands` field (defaults to all if omitted)
- Add `configuration` section for agent-specific settings
- Update AgentDetectionService to expose capabilities
- UI shows only supported commands for each agent
- AgentCommandService validates commands against capabilities

**Expected Impact:**
- More flexible agent integration
- Better UX (no disabled commands that don't work)
- Easier to add new agent types
- Foundation for future enhancements (custom commands, custom UI)

**Example plugin.json enhancement:**
```json
{
  "id": "example-agent",
  "capabilities": {
    "supportedCommands": ["launch", "resume", "version"],
    "supportsProjectContext": true,
    "customCommands": [
      {
        "id": "custom-action",
        "name": "Custom Action",
        "command": "agent-cli custom"
      }
    ]
  },
  "configuration": {
    "probeTimeout": 5000,
    "workingDirectory": "relative/path"
  }
}
```

---

### Phase 12: Testing Infrastructure Improvements
**Goal:** Enhance test infrastructure for better coverage and maintainability.

**Priority:** Medium  
**Estimated Effort:** 4-6 hours  
**Complexity:** Medium

**Proposed Improvements:**

1. **Integration Tests for User Flows**
   - End-to-end scenarios: select project → launch agent → verify outcome
   - Tray interaction scenarios
   - Hotkey scenarios
   - Update check and download flows

2. **Test Builders and Fixtures**
   - ViewModelBuilder for consistent test setup
   - ControllerBuilder for pane controller tests
   - Mock service factories

3. **Property-Based Tests**
   - Configuration migration scenarios
   - Path formatting edge cases
   - Plugin descriptor validation

**Expected Impact:**
- Higher confidence in refactoring
- Catch regression bugs earlier
- Faster test authoring
- Better documentation through tests

---

## Architecture Principles

These principles guide all improvement work:

1. **Separation of Concerns:** Each component has one clear responsibility
2. **Testability:** Business logic is testable without UI infrastructure
3. **Coordinator Pattern:** Use coordinators for cross-cutting concerns (StatusMessageCoordinator, LaunchCommandCoordinator, ShellCoordinator)
4. **Controller Pattern:** Use controllers for workflow boundaries (ProjectPaneController, AgentPaneController)
5. **ViewModel as Composition:** ViewModels compose controllers and coordinators, minimal logic
6. **Progressive Refinement:** Small, incremental improvements with tests
7. **Preserve Behavior:** Refactorings preserve exact external behavior
8. **Follow Existing Patterns:** New code follows established patterns in the codebase

---

## Decision Log

### Why StatusMessageCoordinator before MenuActionCoordinator?
**Decision:** Extract StatusMessageCoordinator first (Phase 9a).

**Rationale:**
- Status messages are more scattered (10+ locations vs 2-3 for menus)
- Higher visibility impact (status bar is always visible)
- Clearer boundaries (event-driven, well-defined inputs)
- Lower risk (isolated functionality)
- Faster implementation

### Why Not Extract DisplayStyle Management?
**Decision:** Keep DisplayStyle in LaunchWindowViewModel for now.

**Rationale:**
- Only one property with minimal logic
- Clear ownership (window-level preference)
- Extraction would add ceremony without clear benefit
- `IPathDisplayStyleTarget` interface already exists for Settings integration

### Why Coordinator Pattern for Status Messages?
**Decision:** Use coordinator pattern matching ShellCoordinator, LaunchCommandCoordinator.

**Rationale:**
- Established pattern in codebase
- Single responsibility (status orchestration)
- Singleton lifetime matches other coordinators
- Event-driven model fits well
- Testable without full ViewModel

---

## Notes

- All phase numbers continue from previous refactoring work
- Phases 1-7 covered startup orchestration, update workflow unification, tray state extraction
- Phases 8-9 covered ViewModel decomposition (controller commands, status coordination, menu coordination, window actions)
- Phases 10-11 covered service decomposition (UpdateService, Process subsystem)
- Current focus: Preference synchronization, agent capabilities, testing infrastructure
- Next focus areas: Agent subsystem enhancements, integration testing

**Last Updated:** 2026-10-09
