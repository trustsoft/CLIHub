# CLIHub Improvements

## Purpose

Refactor the application layer to simplify code management and make the execution flow explicit, while preserving current user-visible behavior and the existing `CLIHub.Core` boundaries.

The refactoring should be incremental. Each stage must keep the solution buildable and testable, and must preserve the startup order and behavior defined in `openspec/specs/`.

## Baseline Before This Refactoring

- `CLIHub.Core` is already separated from WPF and should remain UI-independent.
- Core subsystems are organized by responsibility: configuration, projects, plugins, agents, updates, and infrastructure.
- DI registration is restricted to composition roots.
- The main complexity is in `src/CLIHub`, especially startup orchestration, update workflows, and the launch-window ViewModel.
- Baseline verification: `dotnet test CLIHub.sln --no-restore` passes with 466 tests.
- The working tree was clean before this plan was created.

## Execution Progress

The baseline and findings below describe the starting point. Archived increments do not by themselves mean every criterion in a phase is complete.

| Area | Delivered | Remaining |
|---|---|---|
| Phase 0 | Startup characterization and initial startup documentation updates. | Reconcile retired compatibility API references throughout current docs/specs. |
| Phase 1 | `ApplicationSession` owns update and activation subscriptions; shutdown removes them before stopping operations. | Further reduce startup sequencing dependencies and review host/factory boundaries. |
| Phase 2 | Manual checks in tray and What's New; original download/restart policies preserved. | Native UI verification of update interactions. |
| Phase 3 | Command coordinator, action builder, external-launcher port, injected update control. | Strengthen composition and external-launcher adapter coverage. |
| Phase 4 | Tray state projection separated from command handlers. | Group DI registrations by feature, reduce non-window factories, and review shared project workflows. |
| Phase 5 | Disposable pane/update/launch owners and documented legacy window; architecture checks added. | Complete event-owner review, queued-callback checks, and documentation reconciliation. |
| Update workflow follow-up | One `IUpdateWorkflow` for startup, tray, What's New, launch-window operations, and Settings checks; concurrent checks share one task. | Verify installed-build notification/restart behavior in the native application. |
| Phase 6 | `ApplicationHost` owns WPF lifecycle (environment, DI, shutdown). `InstanceCoordinator` handles single-instance coordination with `InstanceStatus` enum. `StartupStateLoader` loads plugins and preferences, returns immutable `StartupState`. `OptionalStartupCoordinator` handles release notes and update check with best-effort policy. `ApplicationBootstrapper` reduced from 152 lines/11 deps to 88 lines/6 deps. `App.xaml.cs` reduced from 88 to 36 lines. Documentation and architecture docs updated. Change archived. | Extract `ShellCoordinator` from `ApplicationSession`, decompose `LaunchWindowViewModel`, add architecture tests. |

The update workflow follow-up preserves the established apply policies: tray and What's New download and restart automatically; the launch window offers an explicit restart action. Existing Core contracts remain unchanged. Application tests exercise concurrency, cancellation, result propagation, automatic restart sequencing, and explicit restart policy without network or installer side effects.

## Current Execution Flow

```text
Program / Velopack
    -> App.OnStartup
        -> application data directories and logging
        -> ServiceRegistration
        -> ApplicationBootstrapper.Start
            -> InstanceCoordinator: single-instance check (returns FirstInstance/SecondInstance)
            -> StartupStateLoader: plugin initialization, preferences load/apply â†’ StartupState
            -> ApplicationSession: tray/launch UI creation and update/activation event wiring
            -> optional launch-window display
            -> global hotkey registration
            -> release-notes evaluation (best-effort)
            -> background startup update check (best-effort)

LaunchWindowViewModel / TrayCommandHandlers
    -> IAgentCommandWorkflow
        -> AgentCommandService
            -> ProcessLauncher
                -> Windows Terminal / Command Prompt / PowerShell

Startup / Tray / What's New / Launch window / Settings
    -> IUpdateWorkflow
        -> Core update ports (one UpdateService)
        -> ApplicationSession: automatic-download notifications
```

## Architectural Findings

### 1. ApplicationBootstrapper has too many responsibilities

`ApplicationBootstrapper` currently owns single-instance handling, startup ordering, UI creation, event wiring, background operation registration, dispatcher calls, and fatal/best-effort failure policy.

Its constructor has 14 dependencies and factories. Adding another startup concern increases the size of the central orchestrator and its tests.

Target outcome:

- keep startup order explicit;
- move event wiring and runtime ownership into a session-level component;
- reduce `ApplicationBootstrapper` to sequencing and top-level failure policy.

### 2. LaunchWindowViewModel is an application facade

`LaunchWindowViewModel` currently manages project state, agent state, selection, preferences, menus, command execution, update control, notifications, Explorer launching, and application shutdown.

The ViewModel is 567 lines long and has a large constructor. It also reaches into WPF and operating-system behavior through `Application`, `Process.Start`, and `AppPaths`.

Target outcome:

- keep the ViewModel focused on binding and screen state;
- move feature workflows into focused coordinators;
- isolate OS actions behind application ports;
- keep project and agent pane synchronization independently testable.

### 3. Update execution is split across multiple workflows

There are currently separate update paths:

```text
Startup
    -> UpdateStartupCoordinator
        -> IUpdateChecker

Tray / What's New
    -> UpdateDownloadCoordinator
        -> IUpdateDownloader
        -> notification
        -> tray refresh
        -> installer and restart

Launch window
    -> UpdateControlViewModel
        -> checker
        -> downloader
        -> installer
```

The paths share `IUpdateStateSource`, but check, download, apply, error handling, and user feedback are orchestrated in different places.

Original Phase 2 decision (preserved for that increment):

- do not refactor or unify the existing download/apply/restart workflow yet;
- keep `UpdateService`, `UpdateDownloadCoordinator`, and `UpdateControlViewModel` behavior unchanged;
- add an explicit `Check for updates` action to the tray and What's New surfaces;
- route the new check action through the existing update checker and shared update state;
- defer download/apply workflow unification to a separate future change.

Follow-up `unify-update-workflow` implements that separate change: one application workflow owns shared checking state, concurrent-check coalescing, download exclusion, and automatic restart sequencing. `UpdateStartupCoordinator` remains the startup policy adapter; `UpdateControlViewModel` remains the launch presentation adapter with its explicit restart step. The former download coordinator/notifier are replaced by workflow outcome events wired through `ApplicationSession`.

### 4. Event ownership is implicit

Long-lived event subscriptions are created by the bootstrapper, tray actions, WPF startup UI, pane controllers, and update ViewModel. Some owners implement `IDisposable`, while other subscriptions rely on DI provider disposal.

Target outcome:

- make the owner of application-level subscriptions explicit;
- provide deterministic unsubscription during session shutdown;
- avoid constructor side effects where possible;
- keep UI and Core event sources independent of WPF lifecycle details.

### 5. TrayActions is another broad application facade

`TrayActions` combines project management, agent launch, update state, dialogs, notifications, settings, and release notes.

Target outcome:

- separate tray state construction from action execution;
- reuse application workflows instead of duplicating behavior already used by the launch window;
- keep `TrayMenuBuilder` as a presentation builder, not as a workflow owner.

### 6. Legacy MainWindow remains duplicated code

`Views/MainWindow.xaml.cs` contains the old agent command flow while `LaunchWindowViewModel` contains the active flow. The legacy window is not registered or constructed, but it still increases the maintenance surface.

This is a separate cleanup decision. It must not block the first orchestration refactor because the project previously chose to retain this window temporarily.

### 7. Documentation has drifted from implementation

The startup documentation describes more work as happening directly in `App.OnStartup` than the current implementation actually performs. Some architecture and lifecycle documents still mention compatibility APIs that production source has retired.

Documentation must be synchronized after each structural refactor.

## Target Architecture

```text
App
  -> ApplicationHost
      -> InstanceCoordinator
      -> StartupStateLoader
      -> ApplicationSession
          -> ShellCoordinator
          -> InputCoordinator
          -> OptionalStartupCoordinator
          -> ApplicationTaskRunner

ApplicationSession.Dispose
  -> unsubscribe application events
  -> stop tracked operations
  -> dispose tray, hotkey, and owned windows
  -> dispose the DI provider
  -> flush logging
```

### Target responsibilities

#### `ApplicationHost`

Owns the WPF-facing startup and shutdown boundary. It prepares directories and logging, builds the service provider, starts the application session, and performs final cleanup.

#### `InstanceCoordinator`

Owns first-instance detection, second-instance activation signaling, and the policy for shutting down a second process.

#### `StartupStateLoader`

Owns required startup state preparation:

1. initialize plugins;
2. load preferences;
3. apply startup preferences;
4. return the immutable startup state needed by later stages.

#### `ApplicationSession`

Owns long-lived application resources and application-level event subscriptions. It provides an explicit lifecycle boundary for the running first instance.

#### `ShellCoordinator`

Creates and wires tray, launch-window, update-request, update-state, and second-instance activation behavior. It should expose a small shell port to startup orchestration.

#### `OptionalStartupCoordinator`

Runs release-notes evaluation and the asynchronous startup update check according to their existing best-effort failure policy.

#### `ApplicationTaskRunner`

Owns tracked asynchronous work and cancellation during shutdown. It may be implemented on top of the existing `ApplicationOperationLifetime`, but the UI should depend on a narrower operation-starting boundary where practical.

## Refactoring Sequence

### Phase 0: Characterization and documentation

- Keep one startup integration-style test that verifies the required startup order.
- Add or update focused tests for activation, update entry points, and shutdown ownership where coverage is missing.
- Record the current behavior before moving responsibilities.
- Update startup documentation to match the current `ApplicationBootstrapper` flow.
- Remove references to retired compatibility APIs from architecture docs and specs.

Completion criteria:

- baseline tests remain green;
- current startup and update behavior is represented by tests;
- documentation describes the implementation that actually exists.

### Phase 1: Extract application session and startup wiring

- Extract event subscriptions and update/activation wiring from `ApplicationBootstrapper` into `ApplicationSession` or `ShellCoordinator`.
- Give the new owner an explicit `Dispose` path.
- Keep the existing startup order unchanged.
- Keep `ServiceRegistration` as the only composition root.
- Replace only the broadest callback surface of `ApplicationStartupContext` when a stable host abstraction is available.

Completion criteria:

- `ApplicationBootstrapper` is a small startup sequencer;
- its constructor no longer grows for every event connection;
- startup and second-instance tests remain readable without a large mock fixture.

### Phase 2: Add manual update-check actions

- Add `Check for updates` to the tray when no update is currently known.
- Add `Check for updates` to the What's New window when no update is currently known.
- Use the existing `IUpdateChecker` contract and existing `IUpdateStateSource` notifications.
- Keep startup update checks unchanged.
- Keep `UpdateDownloadCoordinator` unchanged: tray and What's New continue to use the existing download-and-restart behavior.
- Keep `UpdateControlViewModel` unchanged: the launch window remains the reference implementation for its existing state and command behavior.
- Refresh tray and What's New state after a manual check completes.
- Preserve existing failure handling, cancellation, and non-installed behavior.

Explicitly deferred:

- no common `Check`/`Download`/`Apply` application workflow;
- no change from automatic restart to a separate `ReadyToApply` step in tray or What's New;
- no replacement of `IUpdateRequestSource` or the current download coordinator;
- no changes to the Core update contracts.

Completion criteria:

- tray exposes a manual check action while idle;
- What's New exposes the same manual check action while idle;
- a manual check updates the existing shared availability state;
- an available update still uses the current download-and-restart action;
- the launch-window update behavior remains unchanged;
- no existing update download or apply tests require behavioral changes.

### Phase 3: Decompose LaunchWindowViewModel

- Extract `ProjectPaneViewModel` or formalize the existing `ProjectPaneController` as the project-pane boundary.
- Extract `AgentPaneViewModel` or formalize `AgentPaneController` as the agent-pane boundary.
- Extract `LaunchCommandCoordinator` for validation, execution, result mapping, and refresh behavior.
- Keep update presentation in `UpdateControlViewModel`.
- Move menu construction to a focused action builder/factory.
- Introduce an `IExternalLauncher` or `IFileExplorer` boundary for opening the data folder.
- Keep `LaunchWindowViewModel` responsible for screen composition, selected values, and binding-facing properties.

Completion criteria:

- the launch ViewModel has a substantially smaller constructor;
- agent command execution can be tested without constructing the full launch screen;
- project and agent refresh behavior can be tested independently.

### Phase 4: Simplify tray orchestration and DI

- Separate tray state projection from tray command handlers.
- Reuse the same agent/project/update application workflows from tray and launch-window entry points.
- Group registrations in `ServiceRegistration` by feature while retaining the composition-root rule.
- Keep lazy factories only for windows that must be created on demand.
- Avoid introducing service locator access into feature classes.

Completion criteria:

- tray actions no longer act as a second broad application facade;
- DI registrations clearly show feature ownership;
- factories are limited to real lazy-window requirements.

### Phase 5: Legacy cleanup and final architecture enforcement

- Decide whether `MainWindow` should be removed, isolated under a legacy boundary, or retained with an explicit deprecation note.
- Add architecture tests for the new application/session boundaries.
- Review all event owners and disposal paths.
- Update `docs/architecture.md`, `docs/architecture/startup.md`, `docs/repo-structure.md`, and affected OpenSpec specifications.

Completion criteria:

- no active feature depends on legacy window code;
- architecture tests enforce the intended boundaries;
- documentation, specs, and implementation agree.

## Testing Strategy

### Startup and lifecycle

- startup order remains covered by one high-signal test;
- first-instance and second-instance paths are tested separately;
- fatal startup failure shuts down the application;
- optional startup failure does not prevent the shell from becoming available;
- session disposal unsubscribes events and stops tracked operations.

### Updates

- check, download, apply, cancellation, timeout, and failure behavior are tested at the shared workflow boundary;
- tray, What's New, and launch-window entry points are tested as policy adapters;
- all entry points observe the same update state;
- a second download cannot start while one is active.

### Agent commands

- validation of project, plugin command, and command kind stays covered;
- interactive commands and captured version commands retain their existing process contracts;
- launch-window and tray adapters test only result presentation and workflow invocation.

### UI composition

- ViewModel tests should construct feature-level dependencies rather than the entire application graph;
- composition tests should verify registrations and ownership, not application behavior already covered by unit tests.

## Constraints

- Do not introduce a new domain layer solely to hide existing dependencies.
- Do not move WPF types into `CLIHub.Core`.
- Do not introduce a generic event bus or generic middleware pipeline for a small, deterministic startup sequence.
- Preserve the flat configuration format and atomic persistence path.
- Preserve the existing user-visible startup, update, hotkey, tray, and agent-command behavior unless a separate change explicitly approves behavior changes.
- Keep DI registration in composition roots.

## Definition Of Done

- Startup responsibilities are split into explicit, testable application stages.
- `ApplicationBootstrapper` no longer owns all event wiring and runtime details.
- Update execution has one shared application workflow and one authoritative state model.
- `LaunchWindowViewModel` is a screen composition model rather than a cross-feature application facade.
- Tray and launch-window actions reuse the same feature workflows.
- Application-level event subscriptions have explicit owners and disposal paths.
- Legacy code is either removed, isolated, or explicitly documented.
- `dotnet build CLIHub.sln` passes.
- `dotnet test CLIHub.sln` passes.
- Architecture tests pass.
- Architecture documentation and OpenSpec specifications match the implementation.
## Next Steps After Phase 6

### Current Status

**Phase 6 Complete (October 2026):**
- ? ApplicationHost — WPF lifecycle boundary (79 lines)
- ? InstanceCoordinator — single-instance detection (36 lines)
- ? StartupStateLoader — plugin + preferences initialization (43 lines)
- ? OptionalStartupCoordinator — best-effort operations (93 lines)
- ? ApplicationBootstrapper — session orchestration (88 lines, 6 dependencies)
- ? App.xaml.cs — pure WPF delegation (36 lines)
- ? Tests: 516 passing (128 app + 388 Core)

**Current Architecture:**
\\\
App (36 lines)
  -> ApplicationHost (79 lines)
      -> InstanceCoordinator (36 lines)
      -> StartupStateLoader (43 lines)
      -> ApplicationBootstrapper (88 lines, 6 deps)
          -> ApplicationSession (~150 lines)
              -> TrayIconController
              -> LaunchWindow + LaunchWindowViewModel (567 lines)
              -> Update/activation subscriptions
          -> OptionalStartupCoordinator (93 lines)
\\\

**What Remains:**
- ?? ApplicationSession needs extraction — UI creation + event wiring
- ? ShellCoordinator — not yet extracted
- ? LaunchWindowViewModel — needs decomposition (567 ? ~250 lines target)
- ? DI registrations — need feature grouping
- ? Architecture tests — need boundary enforcement
- ? MainWindow legacy cleanup

### Recommended Next Steps

#### Priority 1: Phase 7 - Extract ShellCoordinator

**Goal:** Extract UI creation and event wiring from ApplicationSession into a focused ShellCoordinator.

**Current Problem:**
ApplicationSession (~150 lines) currently:
- Creates tray icon controller
- Creates launch window
- Subscribes to update events
- Subscribes to second-instance activation
- Owns disposal of all resources

**Target:**
\\\
ApplicationSession (thin, ~80 lines)
  -> ShellCoordinator (~150 lines)
      -> Tray creation + wiring
      -> Launch window creation + wiring
      -> Update request/outcome subscriptions
      -> Second-instance activation handling
\\\

**Success Metrics:**
- ApplicationSession < 100 lines
- ShellCoordinator owns all UI creation
- Event subscriptions isolated and testable
- All 516+ tests pass

**Effort:** 2-3 hours | **Risk:** Low | **Complexity:** Medium

**Why Start Here:**
- Natural next step after ApplicationHost extraction
- Low risk — event wiring already explicit
- Quick win — measurable improvement in 2-3 hours
- Unlocks further improvements (LaunchWindowViewModel, Tray)
- Moves directly toward target architecture

---

#### Priority 2: Phase 8 - Decompose LaunchWindowViewModel

**Goal:** Break down monolithic LaunchWindowViewModel (567 lines) into focused components.

**Current Problem:**
LaunchWindowViewModel.cs (567 lines):
- Manages project state, agent state, selection
- Owns menu construction
- Executes agent commands
- Manages update control
- Opens Explorer (Process.Start)
- Initiates shutdown

**Already Exists (can strengthen):**
- ? ProjectPaneController
- ? AgentPaneController
- ? LaunchCommandCoordinator (Phase 3)
- ? UpdateControlViewModel

**Add:**
- IExternalLauncher for opening folders (instead of Process.Start)
- LaunchWindowActionBuilder for menu construction

**Target:**
LaunchWindowViewModel (~200-250 lines):
- Screen composition only
- Selected values binding
- Pane coordination

**Success Metrics:**
- LaunchWindowViewModel < 300 lines
- Agent command execution testable without full screen
- Project/agent refresh independently testable
- External OS actions behind abstraction

**Effort:** 4-5 hours | **Risk:** Medium | **Complexity:** High

**Dependencies:** Recommended after ShellCoordinator

---

#### Priority 3: Quick Win - Manual Update Checks

**Goal:** Add manual "Check for updates" actions to tray and What's New window.

**Current State:**
- ? IUpdateWorkflow already unified (Phase 2 follow-up)
- ? Concurrent checks share one task
- ? Tray/What's New use automatic download+restart
- ? Launch window uses explicit restart action

**Add:**
- Manual "Check for updates" in tray menu (when no update known)
- Manual "Check for updates" in What's New window (when no update known)
- Use existing IUpdateWorkflow contract
- Refresh state after manual check completes

**Success Metrics:**
- Tray exposes manual check action while idle
- What's New exposes manual check action while idle
- Manual check updates shared availability state
- All existing update tests pass

**Effort:** 1-2 hours | **Risk:** Low | **Complexity:** Low

**Why Consider:**
- Visible user feature
- Builds on existing IUpdateWorkflow
- Can be done in parallel with ShellCoordinator

---

#### Priority 4: Simplify Tray Orchestration

**Goal:** Eliminate duplication between tray and launch window actions.

**Current State:**
- ? Phase 4 Delivered: TrayStateProjection separated from handlers
- ?? Still some duplication in command execution

**Target:**
- Reuse LaunchCommandCoordinator from tray
- Eliminate duplicate project/agent/update logic
- Tray actions as thin adapters to shared coordinators

**Success Metrics:**
- No duplicate workflow implementations
- TrayCommandHandlers < 200 lines
- Shared coordinators between tray and launch window

**Effort:** 2-3 hours | **Risk:** Low | **Complexity:** Medium

**Dependencies:** After LaunchWindowViewModel decomposition

---

#### Priority 5: Group DI Registrations by Feature

**Goal:** Improve readability of ServiceRegistration.cs with clear feature grouping.

**Current State:**
ServiceRegistration.cs (99 lines):
- Flat list of registrations
- Unclear feature boundaries
- Many Func<> factories

**Target Feature Groups:**
\\\csharp
// Infrastructure & Logging
// Startup Coordinators
// Session & Shell
// Agent Commands
// Project Management
// Update Workflow
// UI Components
// Windows & Dialogs
\\\

**Success Metrics:**
- Clear feature grouping with comments
- Reduce non-window factories where possible
- Maintain composition-root rule

**Effort:** 1 hour | **Risk:** Very Low | **Complexity:** Low

**Why Consider:**
- Low-hanging fruit
- Zero risk (pure reorganization)
- Better maintainability
- Can be done anytime

---

#### Priority 6: Legacy Cleanup & Architecture Tests

**Goal:** Decide MainWindow fate and add architecture boundary tests.

**Current State:**
MainWindow.xaml(.cs):
- Deprecated legacy reference window
- Not registered or constructed
- Confuses codebase understanding

**Options:**
1. Delete MainWindow entirely
2. Move to Legacy/ directory with DEPRECATED.md
3. Keep with explicit deprecation doc

**Add:**
- Architecture tests for boundaries (Core/UI separation)
- Event ownership verification tests
- Disposal path validation tests

**Success Metrics:**
- Clear legacy status for MainWindow
- Architecture tests enforce boundaries
- No active feature depends on MainWindow

**Effort:** 1-2 hours | **Risk:** Very Low | **Complexity:** Low

**Dependencies:** After all other phases (cleanup phase)

---

#### Priority 7: Extract ApplicationTaskRunner (Optional)

**Goal:** Explicit boundary for tracked async operations.

**Current State:**
IApplicationOperationLifetime:
- Generic infrastructure
- Used directly by many components
- No clear ownership boundary

**Target:**
ApplicationTaskRunner (~60 lines):
- Wraps IApplicationOperationLifetime
- Narrower application-level contract
- Explicit shutdown coordination

**Success Metrics:**
- Components depend on ApplicationTaskRunner, not infrastructure
- Clear tracked-work ownership
- Simplified shutdown sequence

**Effort:** 2 hours | **Risk:** Low | **Complexity:** Low-Medium

**Priority:** Optional — current approach works well

---

### Recommended Execution Order

1. **Phase 7: ShellCoordinator** (2-3 hours) ? **START HERE**
   - Extract UI creation + event wiring from ApplicationSession
   - Low risk, quick win, unlocks further work

2. **Phase 8: LaunchWindowViewModel** (4-5 hours)
   - Decompose into pane controllers + coordinators
   - Requires ShellCoordinator foundation

3. **Quick Win: Manual update checks** (1-2 hours)
   - Can be done in parallel with Phase 7/8
   - Visible user feature, low risk

4. **Tray simplification** (2-3 hours)
   - Reuse coordinators, eliminate duplication
   - After LaunchWindowViewModel decomposition

5. **DI registrations grouping** (1 hour)
   - Low-hanging fruit, anytime
   - Better maintainability

6. **Legacy cleanup** (1-2 hours)
   - Final cleanup phase
   - Architecture tests

7. **ApplicationTaskRunner** (2 hours, optional)
   - If desired, after ShellCoordinator

**Total Time to Target Architecture:** 15-20 hours

---

### Target Architecture Metrics

| Component | Current | Target |
|-----------|---------|--------|
| ApplicationBootstrapper | 88 lines, 6 deps | ~60 lines |
| App.xaml.cs | 36 lines | ~30 lines |
| ApplicationSession | ~150 lines | ~80 lines |
| ShellCoordinator | N/A | ~150 lines |
| LaunchWindowViewModel | 567 lines | ~250 lines |
| *PaneController | ~100 lines each | (existing, strengthen) |
| LaunchCommandCoordinator | (existing) | (existing, reuse) |
| ApplicationTaskRunner | N/A | ~60 lines (optional) |

**Target Benefits:**
- ? Clear separation of concerns
- ? Single responsibility per component
- ? Independent testability
- ? Explicit lifecycle boundaries
- ? Reusable coordinators across tray and launch window
- ? No duplication between entry points
- ? Self-documenting architecture

---

### Alternative Starting Points

**If Time Limited (1 hour):**
? Group DI registrations
- Zero risk, immediate improvement
- Better code navigation

**If User Feature Desired:**
? Manual update checks
- Visible functionality
- Low risk, 1-2 hours
- Good marketing point

**If Architecture Focus:**
? ShellCoordinator (recommended)
- Foundation for further work
- Moves toward target architecture
- 2-3 hours, measurable impact

**Choose Based On:**
- **Available time:** 1h ? DI grouping, 2-3h ? ShellCoordinator
- **Goal:** Architecture ? ShellCoordinator, User ? manual checks
- **Risk tolerance:** Low ? DI grouping, Medium ? ShellCoordinator
