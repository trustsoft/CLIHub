# Design

## Verified Current State

### IAgentManagementService interface and consumers

**Definition:** `src/CLIHub.Core/Agents/IAgentManagementService.cs:56-74`

```csharp
public interface IAgentManagementService
{
    Task<AgentProbeResult> ProbeAsync(Plugin plugin, CancellationToken cancellationToken = default);
    Task<AgentUpdateResult> UpdateAsync(Plugin plugin, CancellationToken cancellationToken = default);
}
```

**Implementation:** `src/CLIHub.Core/Agents/AgentManagementService.cs:11-116`
- Single class implementing both members
- Constructor dependencies (line 29-34): IAgentDetectionService, IAgentVersionService, IAgentUpdateAvailabilityService, IAgentCommandService, AgentProcessMonitor, ILogger
- ProbeAsync (line 45-70): invalidates caches, detects installation, retrieves version if installed
- UpdateAsync (line 73-115): checks for update command declaration, checks running agents, launches update command, invalidates caches on success

**Production consumers:**
- `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:26,57` - field + constructor parameter
  - Uses ProbeAsync at line 529 (per-card detect) and line 579 (header-wide detect pass)
  - Uses UpdateAsync at line 760 (per-card update command)

**DI registration:** `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:118`
- `services.AddSingleton<IAgentManagementService, AgentManagementService>();`

**Test usage:** `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`
- 67 test setups use `new Mock<IAgentManagementService>()`
- 19 setups configure ProbeAsync (tests that exercise probe/detect behavior)
- 5 setups configure UpdateAsync (tests that exercise update-command behavior)
- Remaining tests pass unconfigured mock (neither member exercised - tests cover population, blocked state, availability, card filtering, etc.)

### Existing capability interfaces in agent subsystem

The agent subsystem already has focused capability interfaces:

1. **IAgentDetectionService** (`src/CLIHub.Core/Agents/IAgentDetectionService.cs:8-31`) - 4 members: IsInstalledInSystem, IsAvailableInProject, Invalidate, InvalidateFor - every consumer uses multiple members (no subset-use found)

2. **IAgentVersionService** (`src/CLIHub.Core/Agents/IAgentVersionService.cs:8-27`) - 3 members: GetVersionAsync, Invalidate, InvalidateFor - AgentManagementViewModel uses only GetVersionAsync (line 428), AgentManagementService uses GetVersionAsync + InvalidateFor (line 53, 66, 107) - but version service already focused, no decomposition win

3. **IAgentUpdateAvailabilityService** (`src/CLIHub.Core/Agents/IAgentUpdateAvailabilityService.cs:10-33`) - 3 members: CheckAsync, Invalidate, InvalidateFor - AgentManagementViewModel uses CheckAsync + InvalidateFor (lines 436, 635, 682, 771), AgentManagementService uses InvalidateFor (line 54, 108) - already focused

4. **IAgentCommandService** (`src/CLIHub.Core/Agents/IAgentCommandService.cs:8-18`) - 1 member: ExecuteAsync - single capability, no decomposition possible

**Conclusion:** IAgentManagementService is the only agent interface with subset-use evidence (ViewModel uses both members but most tests stub only the member under test). Other agent interfaces either already focused (IAgentCommandService) or have no consumer using only a strict subset.

### Update subsystem pattern (reference implementation)

**IUpdateService aggregate:** `src/CLIHub.Core/Updates/IUpdateService.cs:6-12`
- Empty marker interface inheriting IUpdateVersionProvider, IUpdateChecker, IUpdateStateSource, IUpdateDownloader, IUpdateInstaller
- DI registration (ServiceCollectionExtensions.cs:142-147): UpdateService singleton registered as IUpdateService + every capability interface forwarded to the same instance

**UpdateWorkflow consumer:** `src/CLIHub/Updates/UpdateWorkflow.cs:16-20,40-44`
- Constructor depends on five capability interfaces separately, NOT the aggregate
- This is the exact pattern Proposal 3 targets for agents

## Chosen Slice: Split IAgentManagementService

**Rationale:**
- AgentManagementViewModel genuinely composes probe and update (uses both at different call sites: detect actions -> probe, update command -> update)
- Tests exhibit clear subset-use: probe tests stub ProbeAsync only, update tests stub UpdateAsync only, unrelated tests stub neither
- Smallest viable slice: 2 interfaces extracted from 1, affecting 1 production consumer + 1 test file + DI wiring
- Matches existing UpdateWorkflow decomposition pattern exactly

**Member-to-interface mapping:**

```
IAgentProbeService (new)
  - Task<AgentProbeResult> ProbeAsync(Plugin plugin, CancellationToken cancellationToken = default)

IAgentUpdateService (new)
  - Task<AgentUpdateResult> UpdateAsync(Plugin plugin, CancellationToken cancellationToken = default)

AgentManagementService (modified)
  - Implements both IAgentProbeService and IAgentUpdateService
  - Single constructor accepts both capability interfaces' collaborators (detection, version, availability, commands, processMonitor, logger) - wait, that's circular. Constructor accepts the existing collaborators (detection, version, availability, commands, processMonitor, logger) - AgentManagementService IS the composition of those.
```

**Wait - design flaw caught:** AgentManagementService cannot depend on IAgentProbeService + IAgentUpdateService because it IS the implementation of those interfaces. The composition point is AgentManagementService itself. ViewModel depends on the NEW capability interfaces; AgentManagementService constructor keeps its existing collaborators unchanged.

**Corrected member-to-interface mapping:**

```
IAgentProbeService (new capability interface)
  - Task<AgentProbeResult> ProbeAsync(Plugin plugin, CancellationToken cancellationToken = default)

IAgentUpdateService (new capability interface)
  - Task<AgentUpdateResult> UpdateAsync(Plugin plugin, CancellationToken cancellationToken = default)

AgentManagementService (modified implementation)
  - Implements IAgentProbeService, IAgentUpdateService
  - Constructor signature unchanged: (IAgentDetectionService, IAgentVersionService, IAgentUpdateAvailabilityService, IAgentCommandService, AgentProcessMonitor, ILogger)
  - Both methods unchanged (existing ProbeAsync, UpdateAsync bodies preserved verbatim)

AgentManagementViewModel (modified consumer)
  - Replace field: IAgentManagementService management -> IAgentProbeService probe + IAgentUpdateService update (OR keep one field if both injected as same instance - design choice)
  - Constructor signature: replace IAgentManagementService parameter with IAgentProbeService + IAgentUpdateService
  - Call sites updated: this.management.ProbeAsync -> this.probe.ProbeAsync (2 sites), this.management.UpdateAsync -> this.update.UpdateAsync (1 site)

DI registration (ServiceCollectionExtensions.cs:118)
  - Register AgentManagementService singleton once
  - Forward IAgentProbeService to that singleton
  - Forward IAgentUpdateService to that singleton
  - Remove IAgentManagementService registration (interface retired)

Test file (AgentManagementViewModelTests.cs)
  - CreateViewModel helper: replace Mock<IAgentManagementService> parameter with Mock<IAgentProbeService> + Mock<IAgentUpdateService>
  - 19 tests that set up ProbeAsync: configure probe mock instead
  - 5 tests that set up UpdateAsync: configure update mock instead
  - Remaining tests: pass unconfigured mocks for both (simplified - no longer need to stub unrelated members)
```

**Design choice - ViewModel field strategy:**

Option A (two fields): `IAgentProbeService probe; IAgentUpdateService update;`
- Pro: explicit capability separation, clear at call sites
- Con: one more field

Option B (one field, reference same instance): keep `IAgentManagementService management` field but cast or keep references
- Con: defeats the purpose - ViewModel still depends on aggregate

Option C (marker aggregate + capability fields): keep IAgentManagementService as marker (like IUpdateService), ViewModel uses capability interfaces
- Con: adds interface without value (IAgentManagementService has no registration reason if not consumed)

**Decision:** Option A (two fields). AgentManagementViewModel constructor accepts IAgentProbeService + IAgentUpdateService separately, stores them in separate fields (this.probe, this.update), uses them at call sites. IAgentManagementService interface DELETED (not kept as marker - unlike IUpdateService which IS consumed by UpdateService's self-registration, IAgentManagementService has no consumer once ViewModel migrates).

## Alternatives Rejected

**B: Retire IUpdateService aggregate for app layer**
- Finding: UpdateWorkflow, ReleaseNotesStartupCoordinator, WhatsNewViewModel, ReleaseNotesLauncher, SettingsViewModel, MainWindow all depend on capability interfaces directly (IUpdateVersionProvider, IUpdateChecker, IUpdateStateSource, IUpdateDownloader, IUpdateInstaller) - `src/CLIHub/Updates/IUpdateWorkflow.cs:8-12` defines IUpdateWorkflow as aggregate of capabilities, but UpdateWorkflow.cs:16-20,40-44 shows it depends on capabilities directly
- Evidence: grep showed no app-layer consumer depending on IUpdateService interface itself - UpdateService.cs:17 implements IUpdateService, but consumers already decomposed
- Conclusion: Already satisfied - no work needed. IUpdateService exists only as DI registration convenience (one class registered, five interfaces forwarded), exactly the pattern we're implementing for agents.

**C: Other agent service decomposition**
- IAgentDetectionService (4 members): no consumer uses strict subset (all consumers use IsInstalledInSystem + invalidation members together)
- IAgentVersionService (3 members): AgentManagementViewModel uses 1 member (GetVersionAsync), AgentManagementService uses 2 (GetVersionAsync + InvalidateFor) - but version service already focused on one capability (version retrieval + cache control), no testability win from splitting synchronous Invalidate stubs
- IAgentUpdateAvailabilityService (3 members): similar pattern (CheckAsync + cache control) - no testability evidence
- IAgentCommandService (1 member): cannot decompose single-member interface

## Deferred Follow-ups

1. **Decompose other role-based Core interfaces** (trigger: after this slice proves the pattern, if evidence found)
   - Audit Core services for role-based interfaces with subset-use patterns
   - Candidates flagged by architecture-analysis-2026-10-29.md but not yet investigated with file:line consumer evidence
   
2. **Extract capability interfaces from UI-layer aggregates** (trigger: when a UI workflow/coordinator has 3+ unrelated responsibilities consumed separately)
   - Pattern: IUpdateWorkflow is app-layer aggregate over Core capabilities - acceptable because it's UI coordination, not Core reuse
   - Revisit if test mocking burden increases

3. **Process domain unification (Proposal 4)** (trigger: when agent process operations need cross-platform abstraction or test isolation)
   - Already completed per ADR-0012 and docs/backlog.md - marked IMPLEMENTED 2026-10-29
   - No action needed

## Open Questions

1. Should IAgentManagementService.cs be deleted or kept with `[Obsolete]` attribute for one release cycle?
   - Decision: Delete immediately - it's an internal Core interface with one consumer (AgentManagementViewModel), no external plugins or extensions depend on it, and the change updates that consumer atomically in the same commit

2. AgentProbeResult and AgentUpdateResult types - keep in IAgentManagementService.cs file or move?
   - Decision: Keep in IAgentManagementService.cs renamed to AgentManagementTypes.cs (or IAgentProbeService.cs for AgentProbeResult, IAgentUpdateService.cs for AgentUpdateResult split) - file naming follows interface, types stay colocated with their contracts

3. XML docs for new interfaces - copy from IAgentManagementService.ProbeAsync/UpdateAsync or write fresh summary?
   - Decision: Copy verbatim from IAgentManagementService members - identical contracts, preserve exact wording that tests may reference

4. Test helper CreateViewModel parameter order - put probe before update, or alphabetical?
   - Decision: Probe before update (matches conceptual flow: detect first, then act) - constructor parameter order mirrors this
