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

### Phase 15: Testing Infrastructure Improvements (2026-10-09)
**Goal:** Enhance test infrastructure for better coverage and maintainability.

**Status:** ✅ Complete

**Changes:**
- Created `LaunchWindowViewModelBuilder` with fluent API for test setup
- Reduced test boilerplate by 70-80% (65 lines → 13 lines in refactored tests)
- Implemented two build modes: `Build()` and `BuildWithMocks()`
- Added 5 comprehensive builder tests
- Refactored 3 existing tests demonstrating the pattern
- Created comprehensive testing guide (`tests/CLIHub.Tests/README.md`)
- All 609 tests passing (425 Core + 184 UI)

**Outcome:** Test builders eliminate boilerplate and improve test readability while centralizing setup logic for complex ViewModels.

**Benefits:**
- 70-80% reduction in test setup code
- Improved test focus (only mock what matters)
- Centralized maintenance (constructor changes only update builder)
- Pattern established for other complex types

**Artifacts:** `docs/phase15-testing-infrastructure.md`

### Phase 14: Agent Process Monitoring for Safe Updates (2026-10-09)
**Goal:** Add monitoring of running agent processes to prevent updates while agents are active.

**Status:** ✅ Complete

**Changes:**
- Registered `AgentProcessMonitor` and dependencies in DI container
- Integrated process monitor into `WindowActionCoordinator`
- Modified `UpdateCommand.CanExecute()` to block updates when agents are running
- Added `ExecuteUpdate()` method with double-check safety for running agents
- Added `ReportUpdateBlockedByRunningAgents()` to `StatusMessageCoordinator`
- Wired process monitor through `LaunchWindowViewModel`
- Updated all tests to include process monitor dependency
- All 604 tests passing (425 Core + 179 UI)

**Outcome:** Update command now prevents unsafe updates when agent processes are detected, with clear user feedback.

**Benefits:**
- Prevents update-related crashes and data corruption
- Clear user feedback when update is blocked
- Foundation for future "graceful shutdown" feature

---

## Planned Next Steps

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
- Phase 14 covered agent process monitoring for safe updates
- Phase 15 covered testing infrastructure improvements with test builders
- Current focus: Preference synchronization, agent capabilities
- Next focus areas: Agent subsystem enhancements, integration testing

**Last Updated:** 2026-10-09
