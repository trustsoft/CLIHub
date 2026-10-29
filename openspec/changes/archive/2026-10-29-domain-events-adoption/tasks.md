# Tasks: Domain Events Adoption Catalogue + AgentLaunched Event

## Phase 1: Documentation

- [ ] Write `docs/architecture/domain-events.md` following the structure defined in design.md:
  - What is a domain event here (vs UI notification, log line, INotifyPropertyChanged)
  - Existing events: ProjectSelected (publisher, when, subscribers, rationale)
  - Adoption rules (when to add an event, publication semantics, subscriber contract)
  - Candidate catalogue with triggers (ProjectRegistered, ThemeChanged, UpdateApplied — NOT implemented in this change)
  - Exception policy (from DomainEventBus: caught, logged, other subscribers continue)
  - Ordering and thread-safety assumptions (synchronous in-order, single-threaded)
- [ ] Link domain-events.md from `docs/architecture.md` line 86 (insert before "Architecture decision records")
- [ ] Build passes: `dotnet build CLIHub.sln`

## Phase 2: AgentLaunched Event

- [ ] Create `src/CLIHub.Core/Events/AgentLaunched.cs`:
  - Record with fields: `PluginId` (string), `ProjectPath` (string?), `CommandKind` (AgentCommandKind), `LaunchedAt` (DateTimeOffset)
  - Implements `IDomainEvent`
  - XML doc: "Published when a session-launching interactive agent command (Launch or Resume) succeeds. ProjectPath is the launch working directory, or null when there is no project context."
- [ ] Build passes: `dotnet build CLIHub.sln`

## Phase 3: AgentLaunchAuditSubscriber

- [ ] Create `src/CLIHub.Core/Events/AgentLaunchAuditSubscriber.cs`:
  - Constructor: `ILoggingContextProvider loggingContext, ILogger<AgentLaunchAuditSubscriber> logger`
  - Method: `OnAgentLaunched(AgentLaunched evt)` — begins operation scope "AgentLaunch", logs at Debug with PluginId, CommandKind, ProjectPath ?? "<no-project>"
  - XML doc: "Emits structured audit log for agent launches."
- [ ] Wire subscriber in `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs`:
  - Add `services.AddSingleton<AgentLaunchAuditSubscriber>();` after DomainEventBus registration
  - Subscribe in a static local method called after provider is built (follow ProjectSelectionLoggingSubscriber pattern if it exists, or add inline after `BuildServiceProvider` equivalent)
- [ ] Build passes: `dotnet build CLIHub.sln`

## Phase 4: Refactor AgentCommandService Publication Site

- [ ] Add `IDomainEventPublisher eventPublisher` parameter to `AgentCommandService` constructor (after existing parameters)
- [ ] Store as private readonly field
- [ ] Extract private method `LaunchInteractiveCommandAsync(Plugin plugin, string projectPath, AgentCommandKind kind, LaunchRequest request)`:
  - Calls `orchestrator.LaunchInteractiveAsync(request)`
  - On success: publish `AgentLaunched(plugin.Id, projectPath, kind, DateTimeOffset.UtcNow)` ONLY when `kind` is `Launch` or `Resume` (Update is not a launch and publishes nothing); return success result
  - On failure: return failure result (no publication)
- [ ] Update `ExecuteAsync` line 71-75 to call `LaunchInteractiveCommandAsync`
- [ ] The event carries `projectPath` as-is (no project ID lookup in this change); document this in the event XML doc
- [ ] Build passes: `dotnet build CLIHub.sln`

## Phase 5: Update All Construction Sites

- [ ] Update DI registration in `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs`: AgentCommandService constructor now takes eventPublisher (already registered as singleton DomainEventBus)
- [ ] Update all test files that construct AgentCommandService or mock IAgentCommandService:
  - `tests/CLIHub.Core.Tests/Agents/AgentCommandServiceTests.cs` — add mocked `IDomainEventPublisher` to constructor
  - `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs` — mocked IAgentCommandService already isolated
  - `tests/CLIHub.Tests/Services/AgentCommandWorkflowTests.cs` — mocked IAgentCommandService already isolated
- [ ] Build passes: `dotnet build CLIHub.sln`
- [ ] Tests pass: `dotnet test CLIHub.sln`

## Phase 6: New Tests

- [ ] Add test `AgentCommandService_SuccessfulLaunch_PublishesAgentLaunchedEvent`:
  - Setup: mock orchestrator returns success, mock event publisher captures Publish call
  - Execute: Launch command with plugin and project path
  - Assert: Publish called once with AgentLaunched matching plugin.Id, projectPath, Launch kind, recent timestamp
- [ ] Add test `AgentCommandService_FailedLaunch_DoesNotPublishEvent`:
  - Setup: mock orchestrator returns failure
  - Execute: Launch command
  - Assert: Publish never called
- [ ] Add test `AgentCommandService_UpdateCommand_DoesNotPublishAgentLaunchedEvent`:
  - Setup: mock orchestrator returns success
  - Execute: Update command
  - Assert: Publish never called (Update is not a launch)
- [ ] Add test `AgentCommandService_VersionCommand_DoesNotPublishAgentLaunchedEvent`:
  - Setup: mock orchestrator CaptureOutputAsync returns success
  - Execute: Version command
  - Assert: Publish never called (Version uses different path)
- [ ] Tests pass: `dotnet test CLIHub.sln`

## Phase 7: Manual Smoke Test

- [ ] Run application: `dotnet run --project src/CLIHub/CLIHub.csproj`
- [ ] Launch any agent in any project
- [ ] Verify log output contains structured Debug line with operation scope "AgentLaunch", plugin ID, Launch kind, project path (raise `logLevel` to `Debug` temporarily)
- [ ] Verify agent launches successfully (no behavior change)

## Phase 8: Final Verification

- [ ] Build passes: `dotnet build CLIHub.sln`
- [ ] Tests pass: `dotnet test CLIHub.sln`
- [ ] Validate OpenSpec: `openspec validate --all`
- [ ] Update graphify: `graphify update .`

## Commit Message Template

```
feat: domain events adoption catalogue + AgentLaunched event

- Add docs/architecture/domain-events.md with adoption rules and candidate catalogue
- Add AgentLaunched event published after successful interactive launches (Launch/Resume only; Update and Version excluded)
- Add AgentLaunchAuditSubscriber for structured audit logs with operation scope
- Refactor AgentCommandService to publish event on success
- Link domain-events.md from architecture index

Validates event bus under second real event; establishes durable adoption rules.
```
