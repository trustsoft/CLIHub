## Why

`ProjectService.SetCurrentProject` changes application state that several surfaces depend on (launch window, tray menu, status message). Currently, surfaces react via the `ProjectsChanged` event — a coarse-grained notification that signals any project mutation. This couples the service to its consumers and makes it difficult to add new behaviors (analytics, logging, recency tracking) without editing the service. The architecture backlog (Proposal 2 in `docs/architecture-analysis-2026-10-29.md`) proposes domain events as a decoupling mechanism. This POC validates the approach with one event and one subscriber before mass adoption.

## What Changes

**A. Event infrastructure in `CLIHub.Core` (UI-independent, synchronous, in-order, singleton):**
- `IDomainEvent` marker interface
- `IDomainEventPublisher` — `void Publish<TEvent>(TEvent evt) where TEvent : IDomainEvent`
- `IDomainEventSubscriber` — `IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent`
- Implementation: synchronous dispatch to registered handlers in subscription order; subscriber exceptions are logged via `ILoggingContextProvider` and do not interrupt other handlers or the publisher
- Singleton registration in Core composition

**B. First event:**
- `ProjectSelected` record: `Id`, `Path`, `SelectedAt` (matches `Project` model fields verified at `src/CLIHub.Core/Models/Projects/Project.cs:11-40`)
- Published by `ProjectService.SetCurrentProject` exactly once per effective selection change (only when the project ID actually changes)
- Logged once at `Debug` level in a structured scope via `ILoggingContextProvider` by a logging-only subscriber

**C. Exactly ONE subscriber:**
- `ProjectSelectionLoggingSubscriber` in `CLIHub.Core` — subscribes during construction, logs the event via `ILoggingContextProvider.BeginOperation` and `ILogger<T>.LogDebug`, disposes the scope
- Registered as singleton, subscribes once at startup

## Capabilities

### Modified Capabilities

- None (pure internal refactor — the logging subscriber is invisible to users)

## Impact

**New code:**
- `src/CLIHub.Core/Events/IDomainEvent.cs` — marker interface
- `src/CLIHub.Core/Events/IDomainEventPublisher.cs` — publisher contract
- `src/CLIHub.Core/Events/IDomainEventSubscriber.cs` — subscriber contract
- `src/CLIHub.Core/Events/DomainEventBus.cs` — synchronous in-memory implementation
- `src/CLIHub.Core/Events/ProjectSelected.cs` — first event record
- `src/CLIHub.Core/Events/ProjectSelectionLoggingSubscriber.cs` — logging subscriber
- `tests/CLIHub.Core.Tests/Events/DomainEventBusTests.cs` — unit tests for bus behavior
- `tests/CLIHub.Core.Tests/Events/ProjectSelectionLoggingSubscriberTests.cs` — subscriber tests

**Modified code:**
- `src/CLIHub.Core/Projects/ProjectService.cs:237-240` — publish `ProjectSelected` when ID changes (verify during implementation whether to publish on every call or only on change)
- `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs` — register event bus and logging subscriber

**Expected result:**
- Build and test suite green
- No user-visible behavior change (logging is diagnostic-only)
- One structured log entry per project selection: `OperationName="ProjectSelected"`, `CorrelationId=<guid>`, level Debug

## Non-Goals

- Multiple events or subscribers beyond the single POC pair (`ProjectSelected` + logging subscriber)
- Recency tracking, analytics, logo cache warmup, or any user-visible behavior change
- Asynchronous event dispatch, queuing, persistence, or retries
- UI integration or observable collections
- Mass adoption across the codebase (adoption catalogue deferred to Proposal 2 follow-up)
