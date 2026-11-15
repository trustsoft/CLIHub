# 0017. Synchronous in-process domain event bus

## Status

Accepted — 2026-10-31/11-01 (recorded 2026-11-14). Implements Proposal 2 of the deleted
2026-10-29 architecture analysis, which planned ADRs 0007, 0008, and 0009, none of which was
written — the index in `README.md:26-36` runs from 0006 straight to 0012. Implementing commits:
`b9306e5` "introduce domain event bus and ProjectSelected POC", `49f6511` "add domain events
adoption catalogue and AgentLaunched event", and `577b621` "synchronize DomainEventBus" (2026-11-14),
which added the reader/writer lock described below.

## Context

The 2026-10-29 analysis proposed decoupling domain publishers from audit and diagnostic consumers.
Before the bus, a publisher that needed an audit trail had to call the auditor directly, which
fixed the consumer list at the publisher and made every new consumer an edit to the publisher. The
project needed a way to publish a durable fact — "this project became current", "this agent
launched", "the plugin feed changed" — and let zero or more consumers react without the publisher
knowing them.

The project also had a concrete constraint: three publishers exist on different execution contexts,
including a background sync thread. `ProjectService.SetCurrentProject` publishes on the caller's
thread (`src/CLIHub.Core/Projects/ProjectService.cs:269`), `AgentCommandService` publishes from an
async continuation (`src/CLIHub.Core/Agents/AgentCommandService.cs:90`), and
`PluginFeedSyncService.RefreshAsync` publishes from the background feed refresh
(`src/CLIHub.Core/Plugins/PluginFeedSyncService.cs:296`), whose background nature is documented at
`:223-231`.

The adopted rules were written up as a durable contract rather than left implicit, and the
catalogue of candidate events sits in `docs/architecture/domain-events.md:58-90`.

## Decision

Build an in-process synchronous bus with exactly these properties.

**Thread-safe publish against concurrent subscribe/unsubscribe.**
`DomainEventBus.Publish` snapshots the handler list under a read lock and then invokes handlers
outside the lock (`src/CLIHub.Core/Events/DomainEventBus.cs:35-48`; dispatch at `:55-68`).
`Subscribe` takes the write lock (`:78-92`), and the subscription's `Dispose` takes it again to
remove the handler (`:98-116`), no-opping if the bus is already disposed (`:100-104`). `Dispose`
disposes the lock itself (`:126-137`). `ReaderWriterLockSlim` is declared at `:16`, and the type
summary states the supported concurrency (`:8-10`). Concurrent publish and subscribe are therefore
supported as of `577b621`, the commit that also closed the earlier latent-race item.

**Publish only after the fact is durable.**
`ProjectSelected` is published after the project state is written and only when the selection
actually changed: `Persist()` at `src/CLIHub.Core/Projects/ProjectService.cs:265`, guarded publish
at `:267-270`. `AgentLaunched` is published only after
`LaunchInteractiveAsync` reports success and only for `Launch` or `Resume`, never for `Update`
(`src/CLIHub.Core/Agents/AgentCommandService.cs:84-95`). `PluginFeedUpdatedEvent` is published
after the new index hash is persisted, and only when the hash changed
(`src/CLIHub.Core/Plugins/PluginFeedSyncService.cs:288-296`).

**Subscriber failures are contained.**
Each handler is invoked inside its own `try`/`catch`; an exception is logged with the event type and
the publish's correlation id, and the loop continues (`src/CLIHub.Core/Events/DomainEventBus.cs:55-68`).
The bus does not retry, does not stop the remaining subscribers, and does not propagate the
exception to the publisher.

**One publisher per event type; subscribers are passive.**
Exactly one publisher per event type is an adoption rule (`docs/architecture/domain-events.md:48`),
and subscribers must be idempotent-safe and must not change behavior — events are audit and
diagnostic only for now (`docs/architecture/domain-events.md:54-56`). Each of the three events has a
single publish site: `ProjectService.cs:269`, `AgentCommandService.cs:90`,
`PluginFeedSyncService.cs:296`.

**Subscriptions are container-owned singletons.**
Subscribers are DI singletons that subscribe in their constructors
(`src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:182-183`;
`src/CLIHub.Core/Events/ProjectSelectionLoggingSubscriber.cs:22-32`). Because DI creates singletons
lazily, the publisher registration forces both subscribers to be constructed before the first
publish can occur (`ServiceCollectionExtensions.cs:185-195`). The subscribers implement
`IDisposable` and dispose their subscription (`ProjectSelectionLoggingSubscriber.cs:35-38`), and
the bus itself is a singleton (`ServiceCollectionExtensions.cs:180`), so teardown is the
container's.

**The candidate catalogue is documentation, not code.**
`ProjectRegistered`, `ThemeChanged`, and `UpdateApplied` are described with their triggers,
publisher locations, and missing consumers in `docs/architecture/domain-events.md:58-90`. None of
them exists as a type.

**Known limitation: correlation ids do not span events.**
The bus generates a fresh 8-hex correlation id per publish
(`src/CLIHub.Core/Events/DomainEventBus.cs:33,120-124`), and `LoggingContextProvider` generates
another per operation scope (`src/CLIHub.Core/Infrastructure/Logging/LoggingContextProvider.cs:42-46`).
A publish inside an operation scope therefore does not inherit that scope's id, and a subscriber's
scope does not inherit the publish id.

Rejected alternatives:

- *Asynchronous bus with an outbox* — there is no cross-process consumer, no delivery guarantee the
  product needs, and an outbox would add persistence, replay, and ordering semantics to a
  diagnostic channel.
- *A MediatR-style dispatcher that resolves handlers per publish* — handler resolution per publish
  buys dynamic registration the project does not use, and it would replace a 150-line type with a
  framework dependency plus a service-locator-style lookup.
- *Using events as the orchestration mechanism between services* — that makes the execution path
  depend on subscriber registration order, which is invisible at the call site; the existing
  coordinator and controller patterns own orchestration.
- *Per-event subscriber priorities* — no consumer has needed to order subscribers, and a priority
  knob would hard-code an ordering requirement that currently does not exist.

## Consequences

**Easier:** a new audit or diagnostic consumer is one class plus one DI registration; publishers
never learn about consumers; the adoption rules and candidate catalogue give a reviewer a checklist
instead of a judgment call; a background publisher can publish without coordinating with the UI
thread that subscribes.

**Harder:** the compiler cannot tell you who listens to an event, so a subscriber's blast radius is
found by reading `docs/architecture/domain-events.md` or searching `Subscribe<`; a slow or blocking
subscriber runs on the publisher's thread (for `PluginFeedUpdatedEvent` that is the background sync
thread, for `ProjectSelected` and `AgentLaunched` it is the calling thread); there is no replay, no
durable queue, and no ordering guarantee beyond the order of subscription; a subscriber that throws
is silently downgraded to a log line unless someone reads the log.

**Committed to:** the event types stay audit/diagnostic until a new ADR says otherwise; the bus
stays in-process and synchronous; `ProjectService`, `AgentCommandService`, and
`PluginFeedSyncService` remain the only publishers of their respective events.

## Reopen triggers

1. A subscriber must change behavior — for example, recency tracking that reorders a list or a
   subscriber whose result the publisher consumes. That breaks the passive-subscriber rule and
   needs a decision about failure semantics.
2. A subscriber needs to await asynchronous work, which a synchronous `Action<TEvent>` cannot
   express; that forces an async dispatch contract and its error handling.
3. An event needs cross-process delivery (a second CLIHub process, a helper, or an external
   consumer), which reintroduces the outbox and serialization questions rejected here.
4. Ordering or priority becomes a real requirement rather than a hypothetical, at which point the
   subscription-order guarantee stops being sufficient.
5. Correlation must span publisher and subscriber, which requires a flowing context rather than the
   per-publish random id (see ADR-0019's reopen trigger).
