# ADR-0012: Process Domain Unification

**Date:** 2026-10-29  
**Status:** Accepted  
**Deciders:** Project maintainers  
**Related:** ADR-0001 (Core/UI boundaries), ADR-0006 (Windows platform library)

## Context

Process launching, command building, runtime resolution, and process matching are currently scattered across multiple services with unclear boundaries:

- `ProcessLauncher` (Core) builds command lines but doesn't execute
- `WindowsInteractiveProcessRunner` (Windows) launches terminals and resolves runtimes
- `WindowsProcessOutputRunner` (Windows) captures output and duplicates runtime resolution
- `AgentProcessMatcher` (Core) matches running processes with command-line parsing
- `WindowsAgentProcessInspector` (Windows) performs WMI queries with timeout/degradation logic
- `AgentCommandService` (Core) orchestrates execution across multiple runners

**Issues:**
1. Runtime resolution logic is duplicated across interactive and output runners
2. Process matching logic is split between matcher (Core) and inspector (Windows)
3. No unified process model — different representations at different layers
4. Complex testing — must mock 5+ services to test a single workflow
5. Command-line building is separate from execution context
6. No explicit process lifecycle — launch, monitor, match, terminate are disconnected

This fragmentation makes the codebase harder to understand, test, and extend.

## Decision

We will introduce a **unified Process Domain** with explicit value objects and a single orchestration boundary:

### Domain Models

```csharp
namespace CLIHub.Core.Processes;

public record RuntimeExecutable      // Execution host (terminal/interpreter)
public record AgentProcess           // Running agent with execution context
public record LaunchRequest          // Request to launch a command
public record ProcessResult          // Execution outcome (Success/Failure/Timeout/Cancelled)
public record ProcessMatchCriteria   // Criteria for finding running agents
```

### Single Orchestrator

```csharp
public interface IProcessOrchestrator
{
    Task<ProcessResult> LaunchInteractiveAsync(LaunchRequest request);
    Task<ProcessResult> CaptureOutputAsync(LaunchRequest request);
    Task<IReadOnlyList<AgentProcess>> FindRunningAgentsAsync(ProcessMatchCriteria criteria);
    Task<RuntimeExecutable?> ResolveRuntimeAsync(RuntimePreference preference, ...);
}
```

The orchestrator coordinates existing platform-specific runners (interactive, output, process finder) and consolidates runtime resolution, command-line building, and process matching under one interface.

### Migration Strategy

1. **Phase 1 (2 days):** Introduce domain models alongside existing code (no breaking changes)
2. **Phase 2 (2 days):** Implement `ProcessOrchestrator` with comprehensive unit tests
3. **Phase 3 (2-3 days):** Migrate consumers one at a time (`AgentCommandService`, coordinators, monitor)
4. **Phase 4 (optional):** Deprecate old interfaces after all consumers migrated

## Consequences

### Positive

- **Single source of truth:** All process operations go through one orchestrator
- **Unified model:** `AgentProcess` and `RuntimeExecutable` shared across all layers
- **Simpler testing:** Mock one orchestrator instead of 5 services
- **Clear boundaries:** Core owns domain models and interface; Windows owns platform runners
- **Easy extensibility:** Can add SSH, Docker, sandboxing, telemetry without fragmenting further
- **Better discoverability:** One interface to learn instead of navigating 5+ services

### Negative

- **Migration effort:** 5-7 days to implement and migrate all consumers
- **Temporary duplication:** Both old and new APIs exist during migration
- **Learning curve:** Team needs to understand the new domain model
- **Risk of regression:** Must ensure no behavior changes during migration

### Neutral

- Existing tests continue to work during migration (no breaking changes until Phase 4)
- Performance should remain the same (orchestrator delegates to existing runners)
- Platform-specific logic remains in Windows layer (no architectural shift)

## Mitigation

- **Breaking changes:** Introduce orchestrator alongside existing services; migrate incrementally
- **Performance:** Benchmark before/after; profile WMI query paths; cache runtime resolution
- **Edge cases:** Comprehensive Windows integration tests with all runtimes (pwsh, bash, cmd, Windows Terminal)
- **Regression:** Full test suite after each consumer migration; beta testing with diverse configurations

## References

- **Analysis:** `docs/architecture-analysis-2026-10-29.md` (Section 3.4)
- **Detailed Proposal (EN):** `docs/proposals/proposal-004-process-domain-model.md`
- **Detailed Proposal (RU):** `docs/proposals/proposal-004-process-domain-model-ru.md`
- **Summary (RU):** `docs/proposals/proposal-004-summary-ru.md`
- **Visualization:** `docs/architecture-visualization.md` (Data Flow section)

## Implementation Notes

- Target: Q2 2027 (Phase 2 of architecture evolution roadmap)
- Depends on: Proposal 1 (Cross-Cutting Services) for `ILoggingContextProvider` reuse
- Success criteria: All 1,221 tests passing, zero new warnings, ≤5% performance variance
- Owner: TBD
