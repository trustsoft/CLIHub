# Domain Events

## What Is a Domain Event

A **domain event** is a published fact about a state change in the domain, decoupled from direct invocation. Domain events enable durable audit trails, multi-subscriber workflows, and cross-cutting concerns (logging, telemetry, recency tracking) without coupling the publisher to every consumer.

**What a domain event is not:**
- **UI notification:** `INotifyPropertyChanged` is a binding-level reactivity mechanism and stays local to the ViewModel layer. Domain events are service-level facts.
- **Log line:** A log line is unstructured diagnostic output. A domain event is a structured record that can drive behavior, while an audit subscriber emits the log line.
- **Direct invocation:** A method call `service.DoThing()` couples the caller to the callee. A domain event `this.eventPublisher.Publish(ThingDone(...))` decouples the publisher from all subscribers.

## Existing Events

### ProjectSelected

**Event:** `ProjectSelected(string Id, string Path, DateTimeOffset SelectedAt)`

**Publisher:** `ProjectService.SetCurrentProject` at line 250, after the project state is updated and persisted.

**Subscribers:**
- `ProjectSelectionLoggingSubscriber` — emits a structured Debug log line with operation scope `"ProjectSelected"` and the project ID and path.

**Rationale:** The project selection is a durable domain fact. The event enables future subscribers (recency tracking, project-switching telemetry, restore-last-project on restart) to react without modifying `ProjectService`.

### AgentLaunched

**Event:** `AgentLaunched(string PluginId, string? ProjectPath, AgentCommandKind CommandKind, DateTimeOffset LaunchedAt)`

**Publisher:** `AgentCommandService.LaunchInteractiveCommandAsync` (extracted method called by `ExecuteAsync` at lines 71-75), after `LaunchInteractiveAsync` succeeds, and only when `CommandKind` is `Launch` or `Resume`. `Update` commands do NOT publish `AgentLaunched` — they are update executions, not launches, and belong to a future `AgentUpdateExecuted` event. `Version` commands use a different execution path (`CaptureOutputAsync`) and are out of scope.

**Subscribers:**
- `AgentLaunchAuditSubscriber` — emits a structured Debug log line with operation scope `"AgentLaunch"`, the plugin ID, command kind, and project path (or `"<no-project>"` when the path is null).

**Rationale:** The agent launch is a durable domain fact. The orchestrator logs process-start time and executable, but does not know plugin identity or project context. The event enables structured audit logs that join plugin ID, project path, and command kind in one record, and future subscribers (recency tracking, launch telemetry, UI feedback for multi-agent orchestration) can react without modifying `AgentCommandService`.

## Adoption Rules

### When to Add a Domain Event

Add a domain event only when:
1. At least one **non-UI consumer** needs the fact (for example, recency tracking, telemetry, cross-service coordination), OR
2. It carries **durable diagnostic value** beyond a plain log line (for example, structured audit with operation scope, or a fact that future features will likely need).

**Do not add an event** if the only consumer is the publisher itself, or the consumer can be invoked directly without breaking the layering (for example, a ViewModel calling a service).

### Publication Semantics

- **Exactly one publisher per event type.** Multiple publishers of the same event indicate a missing abstraction or split domain fact.
- **Publish after state is consistent.** The event must represent a durable fact. Publish after persistence, after the UI is updated, or after the external process reports success — never before the fact is guaranteed.
- **No publication on failure.** If the operation fails, do not publish. Publish success, not intent.

### Subscriber Contract

- **Subscribers MUST be idempotent-safe.** Tolerate duplicate publication (for example, a subscriber is registered twice, or a bus replays events on recovery). A duplicate event is not an error.
- **Subscribers MUST NOT throw.** Exceptions are caught by the bus and logged; other subscribers continue. If a subscriber needs to report a failure, log it and continue.
- **Subscribers MUST NOT change behavior.** Events are audit and diagnostic only for now. A subscriber logs, records telemetry, or updates recency metadata — it does not change the execution path or UI state. (Future: when a subscriber needs to change behavior, document the dependency and revisit this rule.)

## Candidate Catalogue

The following events are candidates for future changes. Each is listed with its trigger (the condition that would justify adding it), publisher location, and consumer that would justify it. **None of these are implemented in this change.**

### ProjectRegistered

**Event:** `ProjectRegistered(string ProjectId, string Path, ProjectProvenance Provenance, DateTimeOffset RegisteredAt)`

**Trigger:** A subscriber needs to track project-added facts (for example, recency tracking, project-discovery telemetry, or a project-added notification in the UI).

**Publisher:** `ProjectService.RegisterProjectAsync` (or equivalent), after the project is persisted to the project state store.

**Consumer:** TBD. Revisit when a subscriber needs the fact.

### ThemeChanged

**Event:** `ThemeChanged(string? OldTheme, string NewTheme, DateTimeOffset ChangedAt)`

**Trigger:** A subscriber needs to track theme-change facts (for example, theme-switching telemetry, or a UI feedback animation that cannot be done in the ViewModel).

**Publisher:** The theme-application service (or preferences store), after the new theme is persisted and applied.

**Consumer:** TBD. Revisit when a subscriber needs the fact.

### UpdateApplied

**Event:** `UpdateApplied(string Version, DateTimeOffset AppliedAt)`

**Trigger:** A subscriber needs to track update-applied facts (for example, update telemetry, or a post-update notification in the UI).

**Publisher:** `UpdateService` (or equivalent), after Velopack's apply completes and before the restart.

**Consumer:** TBD. Revisit when a subscriber needs the fact.

## Exception Policy

Subscriber exceptions are caught by `DomainEventBus` and logged with the event type and correlation ID (see `DomainEventBus.Publish` at line 42-48). Other subscribers continue to receive the event. The bus does not retry, does not stop publication, and does not propagate the exception to the publisher.

A subscriber that throws is a bug in the subscriber, not the publisher or bus. Fix the subscriber.

## Ordering and Thread-Safety Assumptions

- **Synchronous dispatch:** `DomainEventBus.Publish` dispatches events synchronously in subscription order (see line 36-49). A subscriber runs inline with the publisher's call stack.
- **Single-threaded subscription model:** Subscribers are registered at DI-container build time and never unregistered (except for test cleanup via `IDisposable`). The bus does not support concurrent subscription/unsubscription.
- **Subscription order:** Subscribers are notified in the order they subscribed. No priority or ordering configuration is exposed. If ordering matters, revisit this design.

**Thread-safety:** The bus does not guard the subscription dictionary with locks. Concurrent `Publish` calls from different threads targeting the same event type are safe (each call reads the handler list atomically via `ToList()` at line 36), but concurrent `Subscribe` and `Publish` is not supported. Register subscribers at container build time, before `Publish` can be called.
