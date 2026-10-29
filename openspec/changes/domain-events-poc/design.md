## Context

See proposal.md — Why. The architecture backlog (Proposal 2 in `docs/architecture-analysis-2026-10-29.md:196-225`) proposes domain events to decouple components and make workflows observable. This POC validates the approach with one event (`ProjectSelected`) and one logging subscriber.

**Verified current state (file:line):**

Project selection flow:
- `src/CLIHub.Core/Projects/ProjectService.cs:229-241` — `SetCurrentProject(string projectId)` updates `this.projectState.CurrentProjectId`, updates `project.LastUsed`, logs at Debug level, then calls `Persist()` which raises `ProjectsChanged`
- `src/CLIHub.Core/Projects/ProjectService.cs:272-276` — `Persist()` calls `projectStateStore.Update`, then raises `ProjectsChanged` event
- Current behavior: `SetCurrentProject` is called unconditionally; if the project ID is already current, it still updates `LastUsed`, persists, and raises `ProjectsChanged`

Who calls `SetCurrentProject`:
- `src/CLIHub\ViewModels\Projects\ProjectPaneController.cs:107,135` — on project double-click and tray menu selection
- `src/CLIHub\Tray\TrayCommandHandlers.cs:91` — on tray recent project selection
- `src/CLIHub\Views\MainWindow.xaml.cs:231,257` — legacy window (unregistered, retained as reference)

Who reacts to `ProjectsChanged`:
- `src/CLIHub\ViewModels\Projects\ProjectPaneController.cs:42,180,183` — subscribes, refreshes project list, raises its own `ProjectsChanged`
- `src/CLIHub\ViewModels\Launch\LaunchWindowViewModel.cs:109,400,404` — subscribes, refreshes agent availability
- `src/CLIHub\ViewModels\Projects\ProjectRegistryViewModel.cs:53,150,257` — subscribes, reloads registry
- `src/CLIHub\ViewModels\Shared\StatusMessageCoordinator.cs:29,96,113` — subscribes, updates status message
- `src/CLIHub\Tray\TrayStateProjection.cs:36,72` — subscribes, syncs tray menu
- `src/CLIHub\Views\MainWindow.xaml.cs:75` — subscribes, refreshes projects

`ILoggingContextProvider` adoption pattern (commit 7d92bf3):
- `src/CLIHub.Core/Infrastructure/Logging/ILoggingContextProvider.cs:6-17` — `IDisposable BeginOperation(string operationName)`
- `src/CLIHub.Core/Infrastructure/Logging/LoggingContextProvider.cs:8-47` — implementation: generates 8-character hex correlation ID, creates scope with `OperationName` and `CorrelationId` structured properties, returns disposable scope
- Registered as singleton in `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:35`
- Usage pattern: `using (this.loggingContext.BeginOperation("OperationName")) { /* log entries */ }`

OpenSpec `project-management` spec (verified at `openspec/specs/project-management/spec.md:38-51`):
- "Requirement: Current project tracking" — "Scenario: Set current project" — WHEN user selects a project THEN it becomes current and is saved as `currentProjectId`
- No explicit contract about publishing side effects, recency behavior, or event dispatch
- The logging subscriber logs the selection without changing behavior → spec contract is unchanged → `skip_specs: true` with rationale below

## Goals / Non-Goals

**Goals:**
- Validate domain event pattern with one event and one subscriber
- Zero user-visible behavior change (logging subscriber only)
- Synchronous, deterministic, in-order dispatch to all subscribers
- Subscriber exceptions must NOT break the publisher or other subscribers
- Disposable subscriptions with deterministic unsubscribe
- Production services keep one public DI constructor
- Build and test suite green

**Non-Goals:**
- Multiple events or subscribers beyond the single POC pair
- Recency tracking, analytics, logo cache warmup, or any behavior change
- Asynchronous dispatch, queuing, persistence, or retries
- UI integration or observable collections
- Mass adoption (deferred to follow-up)

## Decisions

### Decision 1: Publish on every `SetCurrentProject` call or only on change?

**Chosen:** Publish `ProjectSelected` only when the project ID actually changes (i.e., when `projectId != this.projectState.CurrentProjectId` at the start of `SetCurrentProject`). The no-change guard gates **event publication only** — it does not alter the existing state update.

**Rationale:** The semantic event is "project selection changed." Publishing on no-op calls would emit spurious events and force subscribers to deduplicate. The existing `ProjectsChanged` event fires unconditionally because it also covers Add/Remove/Favorite/Reconcile mutations; the new domain event is narrower and should reflect actual selection changes only. Note that the `project-management` spec pins "last-used timestamp is updated WHEN a project is selected" — so re-selecting the same project must still refresh `LastUsed` and persist; only the event is suppressed.

**Alternatives considered:**
- Publish on every call → rejected: emits redundant events when the same project is re-selected, forcing subscribers to track state
- Never publish if already current → same as chosen, but must be implemented as a publication guard, not an early return (an early return would break the recency contract)
- Let subscribers filter → rejected: pushes deduplication burden onto every subscriber

**Implementation note:** Capture the change flag at the start of `SetCurrentProject`:
```csharp
var selectionChanged = projectId != this.projectState.CurrentProjectId;
```
Run the existing state update (current id, `LastUsed` refresh, persistence, `ProjectsChanged` notification) unchanged. After it completes, publish:
```csharp
if (selectionChanged)
{
    this.eventPublisher.Publish(new ProjectSelected(projectId, project.Path, DateTimeOffset.UtcNow));
}
```
Subscribers therefore observe a fully updated state; no-op re-selection keeps today's recency behavior without emitting an event.

### Decision 2: Subscriber exception handling policy

**Chosen:** Catch all exceptions thrown by subscriber handlers, log them via `ILogger<DomainEventBus>` with the exception, event type, and correlation ID (generated per `Publish` call), and continue dispatching to remaining subscribers. The publisher's `Publish` call does not throw (unless the bus itself is broken).

**Rationale:** A broken subscriber should not break the publisher or prevent other subscribers from running. Logging the exception preserves observability. Synchronous dispatch means exceptions are caught immediately; no background error channels are needed.

**Alternatives considered:**
- Let exceptions propagate to the publisher → rejected: would break `ProjectService.SetCurrentProject` and block project selection
- Collect exceptions and throw `AggregateException` → rejected: still breaks the publisher; the publisher is not responsible for subscriber correctness
- Swallow exceptions silently → rejected: loses observability

### Decision 3: Event dispatch order

**Chosen:** Subscribers are invoked in subscription order (insertion order in the subscription list). No priority mechanism.

**Rationale:** Simple, deterministic, predictable. For the POC with one subscriber, order is irrelevant. If future subscribers need ordering, a priority mechanism can be added later; starting without it keeps the design simple.

**Alternatives considered:**
- Random order → rejected: non-deterministic, harder to reason about
- Priority-based → rejected: adds complexity for no current need
- Reverse subscription order → rejected: counter-intuitive

### Decision 4: Subscription lifetime and disposal

**Chosen:** `Subscribe<TEvent>` returns `IDisposable`; disposing it unregisters the handler. Subscribers that live as long as the application (like `ProjectSelectionLoggingSubscriber`, a singleton) may ignore disposal, but the contract supports scoped subscriptions.

**Rationale:** Standard .NET pattern for subscriptions (cf. `IObservable<T>.Subscribe`, `ILogger.BeginScope`). Disposal is deterministic and testable.

**Alternatives considered:**
- Manual unsubscribe method → rejected: disposal is the .NET convention
- Weak references → rejected: adds GC-dependent behavior and complexity
- No unsubscribe → rejected: prevents scoped subscriptions

### Decision 5: Logging subscriber implementation strategy

**Chosen:** `ProjectSelectionLoggingSubscriber` is a singleton that:
1. Takes `IDomainEventSubscriber`, `ILoggingContextProvider`, and `ILogger<ProjectSelectionLoggingSubscriber>` in its constructor
2. Subscribes to `ProjectSelected` in the constructor: `subscriber.Subscribe<ProjectSelected>(this.OnProjectSelected)`
3. `OnProjectSelected` method:
   ```csharp
   using (this.loggingContext.BeginOperation("ProjectSelected"))
   {
       this.logger.LogDebug("Project selected: {ProjectId} at {Path}", evt.Id, evt.Path);
   }
   ```
4. Implements `IDisposable` to unsubscribe (though as a singleton it's never disposed before shutdown)

**Rationale:** Follows the `ILoggingContextProvider` adoption pattern (commit 7d92bf3). Structured logging with `OperationName` and `CorrelationId` provides observability. `LogDebug` level is appropriate for diagnostic information that is not user-facing.

**Alternatives considered:**
- Subscribe in `OnApplicationStartup` handler → rejected: the subscriber is a singleton, construction is the right time
- `LogInformation` level → rejected: too noisy for a side-effect-free diagnostic log; Debug is appropriate
- Inline subscription in DI registration → rejected: the subscriber type provides a named, testable abstraction

### Decision 6: `skip_specs: true` rationale

**Chosen:** Set `skip_specs: true` in `.openspec.yaml`.

**Rationale:** The POC adds a logging subscriber that is invisible to users - no UI change, no behavior change, no capability contract change. The `project-management` spec (verified at `openspec/specs/project-management/spec.md:38-51`) covers "Set current project" semantics: WHEN user selects a project THEN it becomes current and is saved. The spec does not pin side effects, logging, or event dispatch. No behavioral change: the state update, `LastUsed` refresh, persistence, and `ProjectsChanged` notification are untouched; only the internal event is published conditionally (Decision 1). The recency contract (`project-management`: last-used updates whenever a project is selected) is preserved and pinned by a test. Therefore, no spec delta is required.

**Alternatives considered:**
- Write a delta spec for the no-op guard → rejected: the guard is an internal optimization and does not change the observable contract
- Write a delta for the event infrastructure → rejected: events are internal; the spec is about user-facing behavior

### Decision 7: Event record shape

**Chosen:** `ProjectSelected` record with three properties:
```csharp
public record ProjectSelected(string Id, string Path, DateTimeOffset SelectedAt) : IDomainEvent;
```

**Rationale:** Matches the `Project` model fields verified at `src/CLIHub.Core/Models/Projects/Project.cs:11-40`: `Id` (line 14), `Path` (line 24), and `LastUsed` (line 40, `DateTime` in the model; `DateTimeOffset` in the event for timezone clarity). `SelectedAt` is the selection timestamp, not the project's `LastUsed` (which is mutated after the event is published). Immutable record ensures the event cannot be tampered with after publication.

**Alternatives considered:**
- Include `Name` → rejected: not needed for logging; subscribers can load the project if they need enriched data
- Include the full `Project` record → rejected: events should be minimal and immutable; including a mutable POCO would violate immutability
- Use `DateTime` instead of `DateTimeOffset` → rejected: `DateTimeOffset` is more precise for events that may be logged across timezones

## Open Questions

1. **No-op guard scope (resolved in plan):** the guard gates event publication only — `SetCurrentProject` keeps updating `LastUsed` and persisting on every call, so existing `ProjectServiceTests` expectations are preserved; a new test pins the recency contract explicitly (`SetCurrentProject_AlreadySelected_StillRefreshesLastUsedAndPersists`).

2. **Correlation ID scope:** The logging subscriber uses `ILoggingContextProvider.BeginOperation` to create a correlation ID. Should the event bus generate a single correlation ID per `Publish` call and pass it to all subscribers (for cross-subscriber correlation), or should each subscriber generate its own scope? The current design (Decision 5) generates one per subscriber. If cross-subscriber correlation is valuable, the bus should generate the ID and include it in a logging scope that wraps all subscriber calls. Revisit during implementation.

3. **Thread safety:** The POC assumes single-threaded usage (WPF UI thread for `ProjectService`). If events are published from background threads in the future, the subscriber list needs thread-safe mutations (e.g., `ConcurrentDictionary` or a lock). Document the thread-safety assumption in the implementation and revisit when the first background publisher is added.
