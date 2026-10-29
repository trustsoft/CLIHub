# Introduce ILoggingContextProvider for Structured Operation Scopes

## Why

The codebase has grown several operation-scoped logging patterns that are hand-rolled and inconsistent:

- `ProcessOrchestrator` manually calls `logger.BeginScope` with string templates (`"LaunchInteractive {Runtime} {Executable}"`, `"CaptureOutput {Runtime} {Executable}"`, `"FindRunningAgents {PluginId}"`) at `src/CLIHub.Core/Processes/ProcessOrchestrator.cs:47,111,182,194`
- `ApplicationOperationLifetime` and `AsyncOperationRunner` log operation names as message properties (`{OperationName}`) but lack structured scopes at `src/CLIHub/Lifetime/ApplicationOperationLifetime.cs:100,104` and `src/CLIHub/Lifetime/AsyncOperationRunner.cs:34`
- `AgentProcessMonitor` logs failures at `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:94,129` without operation context, making logs harder to trace when multiple operations run concurrently
- Scanning services (`ProjectScanningService`) log at multiple sites (`src/CLIHub.Core/Scanning/ProjectScanningService.cs:331,364,391,440`) without consistent operation boundaries

This inconsistency makes log correlation difficult when multiple operations run concurrently. Structured operation scopes — carrying an operation name and a short correlation ID — would clarify which log entries belong to the same logical operation across async boundaries.

This change is **the first slice of Proposal 1** (Extract Cross-Cutting Services). It introduces a small Core abstraction for structured logging context and adopts it in the few highest-value surfaces. Persistence (`IPersistenceService<T>`) and caching (`ICacheService`) are **explicit non-goals** — those are separate future changes.

## What Changes

### New Abstraction

- `src/CLIHub.Core/Infrastructure/Logging/ILoggingContextProvider.cs` — interface with one method:
  - `IDisposable BeginOperation(string operationName)` — starts a logging scope with the operation name and a short correlation ID (8-character hex), returning an `IDisposable` that ends the scope on disposal
- `src/CLIHub.Core/Infrastructure/Logging/LoggingContextProvider.cs` — implementation wrapping `Microsoft.Extensions.Logging.ILogger<LoggingContextProvider>.BeginScope`
  - Uses `ILogger<LoggingContextProvider>` to avoid duplicating per-type logger creation (DI already provides `ILogger<T>`)
  - Creates scope dictionaries with `OperationName` and `CorrelationId` keys
  - **No `AsyncLocal` state** — relies on `ILogger.BeginScope` propagating through async contexts automatically per MEL semantics
- DI registration: add `ILoggingContextProvider` as a singleton in `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs` → `AddClIHubCoreServices`

### Adoption Set (4 surfaces)

Adopt in the following surfaces with verified operation boundaries and real concurrency:

1. **ProcessOrchestrator** (`src/CLIHub.Core/Processes/ProcessOrchestrator.cs`)
   - Replace ad-hoc `logger.BeginScope` calls at lines 47, 111, 182, 194 with `loggingContext.BeginOperation(...)`
   - Inject `ILoggingContextProvider` + keep existing `ILogger<ProcessOrchestrator>` for messages
   - Same operation names: `"LaunchInteractive"`, `"CaptureOutput"`, `"FindRunningAgents"`
   - Correlation IDs added automatically

2. **AgentProcessMonitor** (`src/CLIHub.Core/Agents/AgentProcessMonitor.cs`)
   - Add `loggingContext.BeginOperation("EnumerateAgentProcesses")` around lines 82-95 (all-plugins overload)
   - Add `loggingContext.BeginOperation("EnumerateAgentProcesses")` around lines 119-131 (single-plugin overload)
   - Inject `ILoggingContextProvider`
   - Preserves spec: "Process enumeration failures log through the monitor" (no behavior change)

3. **ApplicationOperationLifetime** (`src/CLIHub/Lifetime/ApplicationOperationLifetime.cs`)
   - Add `loggingContext.BeginOperation(operationName)` in `RunObservedAsync` at line 91 (wraps lines 96-105)
   - Inject `ILoggingContextProvider` from `CLIHub` composition (the app already references Core)
   - Operation names come from callers (e.g., "Agent blocked-state population")

4. **ProjectScanningService** (`src/CLIHub.Core/Scanning/ProjectScanningService.cs`)
   - Add `loggingContext.BeginOperation("ProjectScan")` in `TriggerScanAsync` (wraps the scan execution)
   - Inject `ILoggingContextProvider`
   - Keeps existing warning/error logs; adds operation boundary

**Not adopted yet:** `AgentCommandService`, `AgentVersionService`, `UpdateService`, `UpdateDownloader`, or any ViewModel — these are future iterations.

### Tests

- `tests/CLIHub.Core.Tests/Infrastructure/Logging/LoggingContextProviderTests.cs` — unit tests for the provider:
  - `BeginOperation_CreatesScope_WithOperationNameAndCorrelationId`
  - `BeginOperation_MultipleCalls_CreateDistinctCorrelationIds`
  - `BeginOperation_Dispose_EndsScope`
- One integration test per adopted surface proving the operation scope appears in captured logs (use `ILogger` test capture from existing test conventions — `NullLogger<T>.Instance` for tests that don't verify log output, or `TestLogger<T>` if a custom recording logger exists)
- Existing tests remain green (no behavioral changes)

### Documentation

- Update `docs/architecture.md` → Infrastructure ownership: add `Infrastructure/Logging/` owning operation-scoped logging context
- No spec delta (see Non-Goals)

## Impact

### Positive
- **Consistent operation scoping** — same pattern across Core and app-layer operations
- **Log correlation** — correlation IDs make it trivial to trace one operation's logs in a concurrent environment
- **Additive** — no changes to `ILogger<T>` injection; provider is optional
- **No MEL replacement** — wraps existing `ILogger.BeginScope`, doesn't replace Serilog sink

### Risk
- **Adoption scope** — limiting to 4 surfaces risks the pattern being underused; we accept this risk because we're evaluating the abstraction's value
- **Correlation ID collisions** — 8-character hex has 2^32 space (~4B); collision probability is negligible for CLIHub's operation concurrency

## Non-Goals

- **No spec delta**: logging is internal; same log levels, same messages, same file output. Additional structured properties (`OperationName`, `CorrelationId`) are allowed per `openspec/specs/logging/spec.md` (structured format, no format constraints on keys). Set `skip_specs: true`.
- **No removal of `ILogger<T>`** — the provider is additive, not a replacement
- **No AsyncLocal state** — we use MEL's built-in async context propagation
- **No mass adoption** — this is a first slice; persistence and cache abstractions are separate future changes
- **No behavior changes** — same log levels, same messages, same sinks

## Dependencies

- Depends on: `Microsoft.Extensions.Logging` (already in the solution)
- Blocks: none (additive)
- Related: Proposal 1 (Extract Cross-Cutting Services) — this is the first slice
