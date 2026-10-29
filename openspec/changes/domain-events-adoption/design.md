# Design: Domain Events Adoption Catalogue + AgentLaunched Event

## Current State

### Events Infrastructure (POC)

File: `src/CLIHub.Core/Events/DomainEventBus.cs:1-97`

- Synchronous in-order dispatch with per-event correlation ID
- Subscriber exceptions caught and logged; other subscribers continue
- Subscribe returns IDisposable; unsubscribe removes handler

File: `src/CLIHub.Core/Events/ProjectSelected.cs:1-10`

Published by `ProjectService.SetCurrentProject` at line 250 after state is consistent.

Subscriber: `ProjectSelectionLoggingSubscriber` (diagnostic log line).

### Agent Launch Flow (All Paths Analyzed)

**Path 1: Launch Window**
- `LaunchWindow` → `LaunchCommandCoordinator.RunAsync` (line 49-67)
- → `LaunchCommandCoordinator.ExecuteAsync` (line 69-120)
- → `AgentCommandWorkflow.ExecuteAsync` (line 23-28)
- → `AgentCommandService.ExecuteAsync` (line 33-76)

**Path 2: Window Action Coordinator (toolbar/menu)**
- `WindowActionCoordinator` commands (line 63-81)
- → `WindowActionCoordinator.RunAgentCommandAsync` (line 212-218)
- → `LaunchCommandCoordinator.RunAsync`
- → (same as Path 1)

**Path 3: Tray Menu**
- `TrayCommandHandlers.LaunchAgentAsync` (line 99-106)
- → `AgentCommandWorkflow.ExecuteAsync`
- → `AgentCommandService.ExecuteAsync`

**Path 4: MainWindow (Legacy)**
- `MainWindow.ExecuteAsync` (line 307-349)
- → `AgentCommandService.ExecuteAsync` (line 329) — **direct call, bypasses workflow**

**Path 5: Update (AgentManagementService)**
- `AgentManagementService.UpdateAsync` (line 85-115)
- → `AgentCommandService.ExecuteAsync` (line 99-100) with `AgentCommandKind.Update`

**Path 6: Version (All UI Paths)**
- Same UI paths funnel through `AgentCommandService.ExecuteAsync` line 55-69 (CaptureOutputAsync, not LaunchInteractiveAsync)

### The Single Chokepoint

File: `src/CLIHub.Core/Agents/AgentCommandService.cs:72-75`

```csharp
var launchResult = await this.orchestrator.LaunchInteractiveAsync(request);
return launchResult.IsSuccess
    ? new AgentCommandResult(true, null, null)
    : new AgentCommandResult(false, null, launchResult.Error ?? $"Failed to launch {plugin.Name}");
```

**Every** successful interactive launch (Launch/Resume/Update) passes through this return when `launchResult.IsSuccess` is true.

Success determination: `ProcessOrchestrator.LaunchInteractiveAsync` (line 50-111) returns `ProcessResult.Success` at line 99 after `interactiveRunner.LaunchAsync` completes without exception.

**Version commands do NOT pass through this chokepoint** — they use `CaptureOutputAsync` (line 57) and return at line 61 or 68. Version is explicitly out of scope.

### Current Logging Around Launches

File: `src/CLIHub.Core/Processes/ProcessOrchestrator.cs:53`
```csharp
this.logger.LogInformation("Launching {Runtime} {Executable}", request.RuntimeKind, request.Command.Executable);
```

File: `src/CLIHub.Core/Processes/ProcessOrchestrator.cs:98`
```csharp
this.logger.LogInformation("Launched successfully in {Duration}", duration);
```

File: `src/CLIHub.Core/Agents/AgentCommandService.cs:60` (Version only)
```csharp
this.logger.LogInformation("Retrieved version for {PluginName}: {Version}", plugin.Name, result.Output);
```

**Gap:** no structured log with plugin ID, project ID, command kind, and operation scope together. The orchestrator logs executable names and duration but does not know plugin identity or project context.

## Decisions

### Decision 1: AgentLaunched Event Scope

**Chosen:** Publish `AgentLaunched` only for session-launching interactive commands — `Launch` and `Resume` — after `LaunchInteractiveAsync` succeeds. `Update` is excluded: it is an update execution, not an agent launch, and belongs to the candidate catalogue as an update-related event.

**Rationale:**
- Single chokepoint (line 72-75) covers all UI paths including legacy MainWindow
- Version commands use a different execution model (captured output, not interactive terminal) and have their own log line at AgentCommandService:60
- Update uses the interactive path but is semantically a different fact; publishing `AgentLaunched` for it would mislead future subscribers (e.g., a recency subscriber must not treat an update as a launch)
- Success signal is unambiguous: `launchResult.IsSuccess` after process start

**Rejected Alternative:** Include Version commands
- Reason: Version uses `CaptureOutputAsync`, not `LaunchInteractiveAsync`; treating them the same would split the publication site or force artificial unification

### Decision 2: Event Fields

```csharp
public record AgentLaunched(
    string PluginId,
    string? ProjectPath,
    AgentCommandKind CommandKind,
    DateTimeOffset LaunchedAt
) : IDomainEvent;
```

**Rationale:**
- `PluginId`: plugin.Id (unique identity)
- `ProjectPath`: the working directory the agent was launched in. `AgentCommandService` receives a path, not a project ID, so the event carries the path (consistent with `ProjectSelected.Path`). Nullable: `null` when there is no project context (Settings update launches in the user profile).
- `CommandKind`: Launch/Resume discriminator
- `LaunchedAt`: UTC timestamp (mirroring `ProjectSelected.SelectedAt` pattern)

**Resolved open question:** represent "no project context" as `null` (option 1); no sentinel strings. Do NOT look up a project ID by path in this change — the event carries the launch context as-is, and a future change can add ID resolution if a subscriber needs it.

### Decision 3: Subscriber Justification

**Subscriber:** `AgentLaunchAuditSubscriber`

Emits a structured Debug log line:

```csharp
using var scope = loggingContext.BeginOperation("AgentLaunch");
logger.LogDebug(
    "Agent launched: {PluginId} {CommandKind} in {ProjectPath}",
    evt.PluginId,
    evt.CommandKind,
    evt.ProjectPath ?? "<no-project>");
```

**Why this is not a duplicate log:**
- Orchestrator logs at process-start time (line 53) with runtime + executable, not plugin identity or project
- Orchestrator logs completion (line 98) with duration, not command kind or project
- No existing log combines plugin ID, project path, command kind, and structured operation scope
- The subscriber adds diagnostic value: filtering launch audit by plugin/project/kind in log aggregators
- Debug level keeps the default Information sink quiet (launches already produce Information lines in the orchestrator)

### Decision 4: Publication Site

Refactor `AgentCommandService.ExecuteAsync` to extract the interactive-launch block (lines 71-75) into a private method:

```csharp
private async Task<AgentCommandResult> LaunchInteractiveCommandAsync(
    Plugin plugin,
    string projectPath,
    AgentCommandKind kind,
    LaunchRequest request)
{
    var launchResult = await this.orchestrator.LaunchInteractiveAsync(request);
    if (launchResult.IsSuccess)
    {
        // Only session-launching kinds publish AgentLaunched; Update is not a launch.
        if (kind is AgentCommandKind.Launch or AgentCommandKind.Resume)
        {
            this.eventPublisher.Publish(new AgentLaunched(
                plugin.Id,
                projectPath,
                kind,
                DateTimeOffset.UtcNow));
        }

        return new AgentCommandResult(true, null, null);
    }

    return new AgentCommandResult(false, null, launchResult.Error ?? $"Failed to launch {plugin.Name}");
}
```

**Alternative Considered:** Publish inline at line 73-74 without extraction
- Rejected: extraction keeps the validate-then-publish boundary clear and makes the success path testable

### Decision 5: Adoption Rules (Documented in domain-events.md)

**Rule 1:** An event is added only when:
- At least one non-UI consumer needs the fact, OR
- It carries durable diagnostic value beyond a plain log line

**Rule 2:** Publication semantics
- Exactly one publisher per event type
- Publish after state is consistent (mirroring `ProjectSelected` pattern)
- No publication on failure

**Rule 3:** Subscriber contract
- Subscribers MUST be idempotent-safe (tolerate duplicate publication)
- Subscribers MUST NOT throw (caught by bus, other subscribers continue)
- Subscribers MUST NOT change behavior (audit/diagnostic only for now)

### Decision 6: Catalogue Structure (domain-events.md)

Sections:
1. What Is a Domain Event Here (vs UI notification, log line, INotifyPropertyChanged)
2. Existing Events (ProjectSelected, AgentLaunched with publisher/when/subscribers/rationale)
3. Adoption Rules (decisions 5 above)
4. Candidate Catalogue (not implemented in this change):
   - `ProjectRegistered(ProjectId, Path, Provenance, RegisteredAt)` — when: project added; consumer: TBD
   - `ThemeChanged(OldTheme, NewTheme, ChangedAt)` — when: theme applied; consumer: structured theme-change audit if analytics added
   - `UpdateApplied(Version, AppliedAt)` — when: Velopack apply completes; consumer: structured update audit
   - Each with: publisher location, consumer that would justify it, trigger to unblock it
5. Exception Policy (from DomainEventBus implementation)
6. Ordering/Thread-Safety Assumptions (synchronous in-order, single-threaded subscription model)

## Open Questions

1. **Project context in AgentLaunched (resolved):** the event carries `ProjectPath` (the launch working directory) and `null` when there is no project context. No path→ID lookup in this change; a future change can add ID resolution if a subscriber needs it.

2. **Path to project ID (resolved):** not resolved in this change by design — the event carries the path as-is (consistent with `ProjectSelected.Path`). Revisit only if a subscriber needs stable IDs.

3. **MainWindow legacy path:** Should we refactor MainWindow to use `AgentCommandWorkflow` for consistency, or leave it as-is since it still hits the same chokepoint?
   - **Recommendation:** Leave as-is; the chokepoint guarantees publication regardless of caller

## Alternatives Rejected

### Alternative 1: Publish from ProcessOrchestrator

**Rejected:** ProcessOrchestrator is infrastructure and does not know plugin identity, project context, or command kind. Moving that context down would invert the dependency (Core infrastructure depending on domain).

### Alternative 2: Publish Multiple Events (LaunchStarted, LaunchSucceeded)

**Rejected:** No consumer need for the started/succeeded pair; one post-success event is sufficient for audit.

### Alternative 3: Include All Command Kinds (Launch/Resume/Version/Update)

**Rejected:** Version uses a different execution path (CaptureOutputAsync) and already has a distinct log line. Forcing unification would complicate the publication site without adding value.

## Implementation Notes

1. Add `AgentCommandService` constructor parameter `IDomainEventPublisher eventPublisher`
2. Update all construction sites: DI registration (Core composition), all test files
3. Extract `LaunchInteractiveCommandAsync` method with publication
4. Add `AgentLaunched.cs` event record
5. Add `AgentLaunchAuditSubscriber.cs` with DI registration and lifetime subscription in Core composition
6. Write `docs/architecture/domain-events.md` following the tone/structure of `docs/architecture/processes.md`
7. Link it from `docs/architecture.md` line 86 (before "Architecture decision records")

## Verification

- Build passes
- All existing AgentCommandService tests pass (mocked event publisher)
- New test: `AgentCommandService_SuccessfulLaunch_PublishesAgentLaunchedEvent`
- New test: `AgentCommandService_FailedLaunch_DoesNotPublishEvent`
- New test: `AgentCommandService_VersionCommand_DoesNotPublishAgentLaunchedEvent`
- Manual smoke: launch an agent, verify structured log line with operation scope appears
