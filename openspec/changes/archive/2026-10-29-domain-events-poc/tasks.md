# Domain Events POC: ProjectSelected Event

## Phase 1: Event Infrastructure

- [ ] 1.1 Create `src/CLIHub.Core/Events/IDomainEvent.cs` with marker interface and XML doc, and verify CLIHub.Core compiles
- [ ] 1.2 Create `src/CLIHub.Core/Events/IDomainEventPublisher.cs` with `void Publish<TEvent>(TEvent evt) where TEvent : IDomainEvent` and XML doc, and verify CLIHub.Core compiles
- [ ] 1.3 Create `src/CLIHub.Core/Events/IDomainEventSubscriber.cs` with `IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IDomainEvent` and XML doc, and verify CLIHub.Core compiles
- [ ] 1.4 Create `src/CLIHub.Core/Events/DomainEventBus.cs` implementing both interfaces: synchronous dispatch in subscription order, catch-and-log subscriber exceptions via `ILogger<DomainEventBus>`, generate 8-char hex correlation ID per `Publish` call and log it with exceptions, store subscriptions per event type, return disposable that removes handler on disposal, and verify CLIHub.Core compiles
- [ ] 1.5 Add event services to `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs`: singleton registrations for `IDomainEventPublisher` and `IDomainEventSubscriber` both resolved from `DomainEventBus`, and verify CLIHub.Core compiles
- [ ] 1.6 Run `dotnet build src/CLIHub.Core/CLIHub.Core.csproj` and verify it succeeds with zero warnings

## Phase 2: ProjectSelected Event

- [ ] 2.1 Create `src/CLIHub.Core/Events/ProjectSelected.cs` with record `ProjectSelected(string Id, string Path, DateTimeOffset SelectedAt) : IDomainEvent` and XML doc describing when it is published, and verify CLIHub.Core compiles
- [ ] 2.2 In `src/CLIHub.Core/Projects/ProjectService.cs`, inject `IDomainEventPublisher` in constructor (add parameter after `ILogger<ProjectService>`, store in private field `eventPublisher`)
- [ ] 2.3 In `ProjectService.SetCurrentProject`, capture at the start whether the selection actually changes: `var selectionChanged = projectId != this.projectState.CurrentProjectId;` — do NOT early-return. The existing `LastUsed` refresh, persistence, and `ProjectsChanged` notification keep today's unconditional semantics (spec `project-management`: last-used updates whenever a project is selected, re-selection included)
- [ ] 2.4 After the existing state update completes, publish only on a real change: `if (selectionChanged) { this.eventPublisher.Publish(new ProjectSelected(projectId, project.Path, DateTimeOffset.UtcNow)); }` — subscribers observe the fully updated state
- [ ] 2.5 Update `ServiceCollectionExtensions.AddProjectServices` (line 91) to pass `sp.GetRequiredService<IDomainEventPublisher>()` as the new constructor parameter
- [ ] 2.6 Run `dotnet build src/CLIHub.Core/CLIHub.Core.csproj` and verify it succeeds

## Phase 3: Logging Subscriber

- [ ] 3.1 Create `src/CLIHub.Core/Events/ProjectSelectionLoggingSubscriber.cs`: singleton implementing `IDisposable`, constructor takes `IDomainEventSubscriber`, `ILoggingContextProvider`, `ILogger<ProjectSelectionLoggingSubscriber>`, subscribes to `ProjectSelected` in constructor with handler method `OnProjectSelected`, logs at Debug level within `BeginOperation("ProjectSelected")` scope, implements `Dispose` to unsubscribe, and verify CLIHub.Core compiles
- [ ] 3.2 Add logging subscriber to `ServiceCollectionExtensions.AddEventServices` (created in 1.5): singleton registration for `ProjectSelectionLoggingSubscriber`, and verify CLIHub.Core compiles
- [ ] 3.3 Run `dotnet build CLIHub.sln` and verify it succeeds with zero warnings

## Phase 4: Unit Tests for Event Bus

- [ ] 4.1 Create `tests/CLIHub.Core.Tests/Events/DomainEventBusTests.cs` with test class and test helpers (fake logger, test event records), and verify the file compiles
- [ ] 4.2 Add test: `Publish_InvokesSubscribedHandlers` — subscribe handler, publish event, verify handler invoked with correct event
- [ ] 4.3 Add test: `Publish_InvokesMultipleHandlersInSubscriptionOrder` — subscribe two handlers, publish event, verify both invoked in order
- [ ] 4.4 Add test: `Publish_DoesNotInvokeUnsubscribedHandlers` — subscribe handler, dispose subscription, publish event, verify handler not invoked
- [ ] 4.5 Add test: `Publish_LogsAndContinuesOnSubscriberException` — subscribe handler that throws, subscribe second handler, publish event, verify exception logged and second handler invoked
- [ ] 4.6 Add test: `Subscribe_ReturnsSeparateDisposablesForDifferentHandlers` — subscribe two handlers, dispose one, publish event, verify only undisposed handler invoked
- [ ] 4.7 Add test: `Publish_WithNoSubscribers_DoesNotThrow` — publish event with no subscribers, verify no exception
- [ ] 4.8 Run `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~DomainEventBusTests"` and verify all tests pass

## Phase 5: Unit Tests for Logging Subscriber

- [ ] 5.1 Create `tests/CLIHub.Core.Tests/Events/ProjectSelectionLoggingSubscriberTests.cs` with test class, mock subscriber/logger/context, and verify the file compiles
- [ ] 5.2 Add test: `Constructor_SubscribesToProjectSelected` — verify subscriber subscribes to `ProjectSelected` on construction
- [ ] 5.3 Add test: `OnProjectSelected_LogsEventWithCorrelationScope` — trigger handler with test event, verify `BeginOperation("ProjectSelected")` called and `LogDebug` invoked with event data
- [ ] 5.4 Add test: `Dispose_Unsubscribes` — construct subscriber, dispose it, verify subscription disposed
- [ ] 5.5 Run `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~ProjectSelectionLoggingSubscriberTests"` and verify all tests pass

## Phase 6: Integration Test for ProjectService Event Publishing

- [ ] 6.1 In `tests/CLIHub.Core.Tests/Projects/ProjectServiceTests.cs`, add test: `SetCurrentProject_PublishesProjectSelectedEvent` — set up fake event publisher, call `SetCurrentProject`, verify `Publish<ProjectSelected>` called once with correct event data (Id, Path, SelectedAt within tolerance)
- [ ] 6.2 Add test: `SetCurrentProject_AlreadySelected_DoesNotPublishEvent` — set current project twice with same ID, verify `Publish` called only once (on the first call, not the second)
- [ ] 6.3 Add test: `SetCurrentProject_AlreadySelected_StillRefreshesLastUsedAndPersists` — re-select the same project and verify `LastUsed` is refreshed and the snapshot is saved, exactly as before the change (pins the preserved recency contract from `project-management`)
- [ ] 6.4 Run `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~ProjectServiceTests"` and verify all tests pass (including new tests and existing tests)

## Phase 7: Composition and Integration Verification

- [ ] 7.1 Run `dotnet test CLIHub.sln` and verify all tests pass (expected: ~1223 tests + ~13 new tests = ~1236 tests)
- [ ] 7.2 Run `dotnet build CLIHub.sln /p:TreatWarningsAsErrors=true` and verify the build succeeds with zero warnings
- [ ] 7.3 Manual smoke test: launch the application via `dotnet run --project src/CLIHub/CLIHub.csproj`, enable Debug logging if needed, select a project in the launch window, check logs for structured `ProjectSelected` entry with `OperationName` and `CorrelationId`, verify no exceptions or broken behavior

## Phase 8: Documentation

- [ ] 8.1 Update `docs/architecture.md` to mention the event infrastructure under "Core Ownership" (in the Infrastructure bullet or a new Events bullet)
- [ ] 8.2 Add brief note in `docs/backlog.md` under the architecture improvements section: "Domain events POC complete (ProjectSelected event with logging subscriber); adoption catalogue pending Proposal 2 follow-up."
- [ ] 8.3 Run `openspec validate --all` and verify validation passes
