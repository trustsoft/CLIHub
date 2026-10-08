## Context

ApplicationSession currently handles two responsibilities:
1. **Session lifecycle**: Creating and owning the application shell (tray + launch window) and disposing it
2. **Event orchestration**: Wiring update workflow, single-instance guard, and UI component events

This violates Single Responsibility Principle and makes testing harder. The event wiring logic (~50 lines of handler creation and subscription management) can be extracted into a focused coordinator.

Current structure (ApplicationSession.cs, 147 lines):
```
ApplicationSession
  - Fields: 5 dependencies + 7 event handler fields
  - Start(): Creates UI factories, builds 6 event handlers, subscribes to 6 events
  - Dispose(): Unsubscribes from 6 events, disposes UI
```

Target structure:
```
ApplicationSession (~80 lines)
  - Delegates shell creation to IShellCoordinator
  - Still owns Start/Dispose lifecycle

ShellCoordinator (~150 lines)
  - Creates UI from factories
  - Wires all event handlers
  - Handles disposal
```

See proposal.md - Why for motivation.

## Goals / Non-Goals

**Goals:**
- Extract shell creation and event wiring into IShellCoordinator
- Reduce ApplicationSession to pure lifecycle boundary (~80 lines)
- Make event wiring independently testable
- Zero behavior changes - preserve exact event semantics and disposal order

**Non-Goals:**
- Changing event handler implementations or update workflow behavior
- Modifying IApplicationStartupUi contract
- Refactoring LaunchWindowViewModel (separate Phase 8)
- Adding new capabilities beyond extraction

## Decisions

### Decision 1: ShellCoordinator receives factories, not instances

**Rationale:** ApplicationSession currently receives `Func<IApplicationStartupUi>` and `Func<IUpdateRequestSource>` and calls them during `Start()`. ShellCoordinator should preserve this lazy-creation pattern.

**Chosen:** ShellCoordinator receives the same factories and calls them internally.

**Alternatives considered:**
- ApplicationSession creates instances and passes them → Moves creation responsibility back to session, defeating the extraction
- ShellCoordinator receives pre-built instances → Changes construction timing, could break startup order dependencies

### Decision 2: ShellCoordinator returns IApplicationStartupUi

**Rationale:** ApplicationSession.Start() currently returns the created IApplicationStartupUi to ApplicationBootstrapper, which needs it for window registration and optional display.

**Chosen:** ShellCoordinator exposes `IApplicationStartupUi StartupUi { get; }` property after creation.

**Alternatives considered:**
- Return from a Start method → Adds method when property suffices
- Keep internal → Breaks existing contract where bootstrapper needs the UI reference

### Decision 3: Event handler storage in ShellCoordinator

**Rationale:** Proper event unsubscription requires storing the exact delegate instances used during subscription.

**Chosen:** ShellCoordinator stores 6 event handler fields (same as current ApplicationSession) and uses them for both subscription and unsubscription.

**Alternatives considered:**
- Inline lambdas → Cannot unsubscribe without storing references
- WeakEventManager → Over-engineering for explicit ownership model

### Decision 4: Disposal order preserved exactly

**Rationale:** Current disposal order: unsubscribe update workflow → unsubscribe UI → unsubscribe update source → unsubscribe guard → unsubscribe update outcomes → dispose UI. This order is untested and changing it is risky.

**Chosen:** ShellCoordinator implements IDisposable and replicates the exact current unsubscription order.

**Alternatives considered:**
- Simplify to any order → Risks introducing subtle bugs if order matters
- Reverse order → No evidence this is safer than current order

### Decision 5: ApplicationSession owns ShellCoordinator disposal

**Rationale:** ApplicationSession is the lifecycle owner and should control when shell resources are released.

**Chosen:** ApplicationSession receives IShellCoordinator via DI, calls its creation during Start(), and disposes it in its own Dispose().

**Alternatives considered:**
- ShellCoordinator self-manages lifetime → No clear owner, risks leaks
- Session disposes before unsubscribing → Wrong order, disposed handlers could be invoked

### Decision 6: Coordinator lives in src/CLIHub, not Core

**Rationale:** ShellCoordinator directly depends on IApplicationStartupUi (WPF-bound contract in CLIHub project) and uses WPF dispatcher.

**Chosen:** Place IShellCoordinator.cs and ShellCoordinator.cs in `src/CLIHub/` alongside ApplicationSession.

**Alternatives considered:**
- Move to Core → Violates Core's UI-independence boundary
- Create new project → Over-engineering for a 150-line coordinator

## Risks / Trade-offs

**[Risk] Event handler delegate identity changes** → Mitigation: Store handlers in fields exactly as current code does, copy pattern verbatim

**[Risk] Disposal order matters but isn't documented** → Mitigation: Preserve exact current order, add test verifying disposal completes without exceptions

**[Risk] Dispatcher nullability in tests** → Mitigation: Tests provide non-null Dispatch delegate, matching current test patterns

**[Trade-off] Slightly more types (2 new) for better separation** → Accepted: Improved testability and clarity justify the additional types

**[Trade-off] ApplicationSession still has 6 constructor dependencies** → Deferred: Dependency reduction is a separate concern (could extract ApplicationTaskRunner later)

## Open Questions

None - design is fully specified and ready for implementation.
