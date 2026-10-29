# Process Domain Model

## Problem

Process launching, command building, runtime resolution, and process matching are currently scattered across multiple services with unclear boundaries:

- **Runtime resolution is duplicated** across `WindowsInteractiveProcessRunner` and `WindowsProcessOutputRunner`
- **Process matching logic is split** between `AgentProcessMatcher` (Core) and `WindowsAgentProcessInspector` (Windows)
- **No unified process model** — different representations in different layers
- **Testing is complex** — must mock multiple services to test a single workflow
- **Command-line building is separate** from execution context via `WindowsCommandLineBuilder`
- **No explicit process lifecycle** — launch, monitor, match, terminate are disconnected

**Evidence:**
- `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs` — builds command lines but does not execute
- `src/CLIHub.Core.Windows/WindowsInteractiveProcessRunner.cs` — launches terminals and resolves runtimes
- `src/CLIHub.Core.Windows/WindowsProcessOutputRunner.cs` — captures output with duplicate runtime resolution
- `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs` — matches running processes via command-line parsing
- `src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs` — WMI queries with timeout and degradation logic

## Goal

Introduce a unified **Process Domain** with explicit value objects and a single orchestration boundary:

1. **Domain models** in `CLIHub.Core/Processes/`:
   - `RuntimeExecutable` — represents a runtime host (terminal or interpreter)
   - `AgentProcess` — running agent process with execution context
   - `LaunchRequest` — request to launch an agent command in a context
   - `ProcessResult` — result of process execution
   - `ProcessMatchCriteria` — criteria for matching agent processes

2. **Single orchestrator interface**: `IProcessOrchestrator` with methods:
   - `LaunchInteractiveAsync` — launch in terminal
   - `CaptureOutputAsync` — execute and capture output
   - `FindRunningAgentsAsync` — find running processes matching criteria
   - `ResolveRuntimeAsync` — resolve runtime executable

3. **Implementation**: `ProcessOrchestrator` that coordinates existing:
   - `IRuntimeExecutableLocator` (runtime resolution)
   - `WindowsCommandLineBuilder` (command building)
   - `IInteractiveProcessRunner` (interactive launch)
   - `IProcessOutputRunner` (output capture)
   - `IAgentProcessInspector` (process finding)

This is **Phase 1 + Phase 2** from approved `docs/proposals/proposal-004-process-domain-model.md`.

## What Changes

### New Files

**Domain Models:**
- `src/CLIHub.Core/Processes/RuntimeExecutable.cs` — immutable record with `RuntimeKind`, `ExecutablePath`, `Version`
- `src/CLIHub.Core/Processes/AgentProcess.cs` — immutable record with `ProcessId`, `CommandLine`, `Runtime`, `StartTime`
- `src/CLIHub.Core/Processes/LaunchRequest.cs` — immutable record with `WorkingDirectory`, `Command`, `RuntimeKind`, `CancellationToken`
- `src/CLIHub.Core/Processes/ProcessResult.cs` — immutable record with `Kind`, `ExitCode`, `Output`, `Error`, `Duration`
- `src/CLIHub.Core/Processes/ProcessResultKind.cs` — enum: `Success`, `Failure`, `Timeout`, `Cancelled`
- `src/CLIHub.Core/Processes/ProcessMatchCriteria.cs` — immutable record with `Plugin`, `Detail`, `Timeout`

**Supporting Interfaces:**
- `src/CLIHub.Core/Processes/IRuntimeResolver.cs` — resolves `RuntimeKind` to `RuntimeExecutable`
- `src/CLIHub.Core/Processes/ICommandLineBuilder.cs` — builds command lines for runtimes
- `src/CLIHub.Core/Processes/IAgentProcessFinder.cs` — finds running agent processes

**Orchestrator:**
- `src/CLIHub.Core/Processes/IProcessOrchestrator.cs` — unified orchestration interface
- `src/CLIHub.Core/Processes/ProcessOrchestrator.cs` — implementation that coordinates existing runners

**Adapter Implementations:**
- `src/CLIHub.Core/Processes/RuntimeResolver.cs` — adapts `IRuntimeExecutableLocator`
- `src/CLIHub.Core/Processes/CommandLineBuilderAdapter.cs` — adapts `WindowsCommandLineBuilder`
- `src/CLIHub.Core/Processes/AgentProcessFinder.cs` — adapts `IAgentProcessInspector` + `AgentProcessMatcher`

**Tests:**
- `tests/CLIHub.Tests/Core/Processes/ProcessOrchestratorTests.cs` — full orchestrator test coverage
- `tests/CLIHub.Tests/Core/Processes/RuntimeResolverTests.cs`
- `tests/CLIHub.Tests/Core/Processes/AgentProcessFinderTests.cs`

### Modified Files

**DI Registration:**
- `src/CLIHub/DependencyInjection/ProcessesServiceRegistration.cs` — register orchestrator and adapters alongside existing services

## Impact

### Positive
- **Single source of truth** — all process operations through one orchestrator
- **Unified process model** — consistent types across all layers
- **Testability** — mock `IProcessOrchestrator` instead of 5 services
- **Clear boundaries** — Core owns domain, Windows owns platform adapters
- **Future extensibility** — easy to add remote execution, sandboxing, telemetry

### Risk
- **No breaking changes** — new types coexist with old services
- **No consumer migration** in this change — `AgentCommandService`, coordinators unchanged
- **Integration risk** — adapters must correctly wrap existing behavior
- **Test coverage** — comprehensive unit tests required for orchestrator

## Non-Goals

- **No consumer migration** — `AgentCommandService`, `LaunchCommandCoordinator`, `WindowActionCoordinator`, `AgentProcessMonitor` are not modified (Phase 3)
- **No deprecation** of old interfaces (Phase 4, later)
- **No behavioral changes** — orchestrator replicates existing behavior exactly
- **No runtime selection UI** — `RuntimeKind` selection remains in Settings
- **No cross-platform support** — Windows-only focus maintained

## Dependencies

- Depends on: approved `docs/proposals/proposal-004-process-domain-model.md`
- Blocks: Phase 3 (consumer migration to orchestrator)
- Related: `agent-process-matching` spec (process matching behavior preserved)
