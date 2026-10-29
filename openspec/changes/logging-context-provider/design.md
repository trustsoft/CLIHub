# Design

## Current State (Verified)

### Evidence: Scattered Logging Patterns

**ProcessOrchestrator** — Manual BeginScope calls
- File: `src/CLIHub.Core/Processes/ProcessOrchestrator.cs:47-48`
  ```csharp
  using var scope = this.logger.BeginScope("LaunchInteractive {Runtime} {Executable}",
      request.RuntimeKind, request.Command.Executable);
  ```
- File: `src/CLIHub.Core/Processes/ProcessOrchestrator.cs:111-112`
  ```csharp
  using var scope = this.logger.BeginScope("CaptureOutput {Runtime} {Executable}",
      request.RuntimeKind, request.Command.Executable);
  ```
- File: `src/CLIHub.Core/Processes/ProcessOrchestrator.cs:182`
  ```csharp
  using var scope = this.logger.BeginScope("FindRunningAgents {PluginId}", criteria.Plugin.Id);
  ```
- File: `src/CLIHub.Core/Processes/ProcessOrchestrator.cs:194`
  ```csharp
  using var scope = this.logger.BeginScope("FindRunningAgents {PluginCount}", plugins.Count);
  ```

**ApplicationOperationLifetime** — Operation name in message properties
- File: `src/CLIHub/Lifetime/ApplicationOperationLifetime.cs:91-105`
  ```csharp
  private async Task RunObservedAsync(string operationName,
                                      Func<CancellationToken, Task> operation)
  {
      try
      {
          await operation(this.cancellation.Token);
      }
      catch (OperationCanceledException) when (this.cancellation.IsCancellationRequested)
      {
          this.logger.LogInformation("Application operation cancelled: {OperationName}", operationName);
      }
      catch (Exception ex)
      {
          this.logger.LogError(ex, "Application operation failed: {OperationName}", operationName);
      }
  }
  ```
  - No structured scope wrapping the operation
  - Operation name appears only in failure/cancellation messages

**AsyncOperationRunner** — Operation name in message property only
- File: `src/CLIHub/Lifetime/AsyncOperationRunner.cs:32-35`
  ```csharp
  catch (Exception ex)
  {
      logger.LogError(ex, "Unexpected exception in fire-and-forget operation {OperationName}", operationName);
      reportStatus?.Invoke($"{operationName} failed unexpectedly.");
  }
  ```
  - No scope around the operation
  - Operation name logged only on exception

**AgentProcessMonitor** — No operation context
- File: `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:76-97`
  ```csharp
  public IReadOnlyList<RunningAgent> GetRunningAgents(
      AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine)
  {
      var runningAgents = new List<RunningAgent>();

      try
      {
          var plugins = this.pluginCatalog.GetAllPlugins();
          var matches = this.orchestrator.FindRunningAgentsAsync(plugins, detail)
              .GetAwaiter().GetResult();

          foreach (var match in matches)
          {
              runningAgents.Add(new RunningAgent(match.Plugin, MapToInstance(match.Process)));
          }
      }
      catch (Exception ex)
      {
          this.logger.LogError(ex, "Failed to check for running agents");
      }

      return runningAgents;
  }
  ```
  - Errors logged without operation boundaries
  - Single-plugin overload (lines 111-132) also lacks scope

**ProjectScanningService** — Multiple log sites, no operation boundary
- File: `src/CLIHub.Core/Scanning/ProjectScanningService.cs:331,364,391,440`
  - Line 331: `this.logger.LogWarning("Repository watcher buffer overflow; performing a full rescan");`
  - Line 364: `this.logger.LogError(ex, "Project scan failed");`
  - Line 391: `(root, ex) => this.logger.LogWarning(ex, "Could not scan watched directory {Root}", root)`
  - Line 440: `this.logger.LogError(ex, "Project scan failed for watched directory {Root}", root);`
  - No structured scope tying scan logs together

### Evidence: Logging Infrastructure

**Current logging setup**
- `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:33` — `services.AddLogging();`
- Every service injects `ILogger<T>` from Microsoft.Extensions.Logging
- Serilog is the sink (registered in the WPF app startup); Core sees only MEL contracts
- `openspec/specs/logging/spec.md` pins file location, rotation, levels, structured format — but not the structure property keys

**Test conventions**
- File: `tests/CLIHub.Core.Tests/Processes/ProcessOrchestratorTests.cs:36`
  ```csharp
  this.orchestrator = new ProcessOrchestrator(
      this.mockResolver.Object,
      this.mockBuilder.Object,
      this.mockInteractiveRunner.Object,
      this.mockOutputRunner.Object,
      this.mockFinder.Object,
      NullLogger<ProcessOrchestrator>.Instance);
  ```
  - Uses `NullLogger<T>.Instance` from Microsoft.Extensions.Logging.Abstractions
  - No custom test logger exists for log capture validation

### Evidence: Core/UI Boundaries

From ADR-0001 and ADR-0006:
- `CLIHub.Core` has **no WPF dependency**, targets `net10.0`
- `CLIHub.Core.Windows` has Windows platform adapters, targets `net10.0-windows`
- `CLIHub` (WPF app) references both Core and Core.Windows
- `ILoggingContextProvider` belongs in Core because all adopted surfaces except `ApplicationOperationLifetime` are in Core
- `ApplicationOperationLifetime` is in the WPF app but can reference Core interfaces

## Architecture Decisions

### Decision 1: ILoggingContextProvider in Core.Infrastructure.Logging

**Rationale:** All adopted surfaces except `ApplicationOperationLifetime` are in `CLIHub.Core`. The abstraction is not Windows-specific, so it belongs in Core, not Core.Windows. Logging is infrastructure, so `Core/Infrastructure/Logging/` is the natural home.

**Interface:**
```csharp
namespace CLIHub.Core.Infrastructure.Logging;

/// <summary>
///   Provides structured operation scopes for diagnostic logging.
/// </summary>
public interface ILoggingContextProvider
{
    /// <summary>
    ///   Begins a logging scope with the given operation name and a generated correlation ID.
    /// </summary>
    /// <param name="operationName"> Name of the operation for diagnostic logs. </param>
    /// <returns>
    ///   A disposable scope; all log entries written while the scope is active include the
    ///   operation name and correlation ID as structured properties.
    /// </returns>
    IDisposable BeginOperation(string operationName);
}
```

**Alternatives Rejected:**
- **Add correlation ID parameter** — rejected: auto-generated IDs reduce boilerplate and guarantee uniqueness
- **Add `BeginOperation(string, Dictionary<string, object>)` overload** — rejected: defer custom properties until a need arises
- **Place in `Core/Logging/`** — rejected: logging is infrastructure (like processes, persistence)

### Decision 2: Implementation Wraps ILogger<LoggingContextProvider>.BeginScope

**Rationale:** `Microsoft.Extensions.Logging.ILogger.BeginScope` already propagates scopes through async contexts. We don't need `AsyncLocal` or manual state management — MEL handles it. The provider just wraps `BeginScope` with a consistent dictionary shape.

**Implementation:**
```csharp
namespace CLIHub.Core.Infrastructure.Logging;

using Microsoft.Extensions.Logging;

/// <summary>
///   Default implementation of <see cref="ILoggingContextProvider" /> using Microsoft.Extensions.Logging.
/// </summary>
public sealed class LoggingContextProvider : ILoggingContextProvider
{
    private readonly ILogger<LoggingContextProvider> logger;

    /// <summary>
    ///   Initializes a new instance of the <see cref="LoggingContextProvider" /> class.
    /// </summary>
    /// <param name="logger"> The logger used for scope creation. </param>
    public LoggingContextProvider(ILogger<LoggingContextProvider> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IDisposable BeginOperation(string operationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        var correlationId = GenerateCorrelationId();
        var scope = new Dictionary<string, object>
        {
            ["OperationName"] = operationName,
            ["CorrelationId"] = correlationId
        };

        return this.logger.BeginScope(scope);
    }

    private static string GenerateCorrelationId()
    {
        // 8-character hex from a random int (2^32 space)
        return Random.Shared.Next().ToString("x8");
    }
}
```

**Alternatives Rejected:**
- **Use `AsyncLocal<T>` for state** — rejected: MEL already propagates scopes through async contexts; adding `AsyncLocal` would duplicate that and risk synchronization issues
- **Use GUID for correlation ID** — rejected: 8-character hex is sufficient for CLIHub's concurrency and more readable
- **Inject `ILogger` factory** — rejected: `ILogger<LoggingContextProvider>` is simpler and uses the existing DI setup

### Decision 3: Adopt in ProcessOrchestrator (Replace Existing Scopes)

**Current:** ProcessOrchestrator manually calls `logger.BeginScope` with string templates.

**Change:** Replace with `loggingContext.BeginOperation(...)`.

**Before (line 47):**
```csharp
using var scope = this.logger.BeginScope("LaunchInteractive {Runtime} {Executable}",
    request.RuntimeKind, request.Command.Executable);
```

**After:**
```csharp
using var scope = this.loggingContext.BeginOperation("LaunchInteractive");
this.logger.LogInformation("Launching {Runtime} {Executable}", request.RuntimeKind, request.Command.Executable);
```

**Rationale:** Operation name ("LaunchInteractive") moves to the scope; runtime/executable stay as message properties. This separates operation identity (scope) from operation details (message). Logs gain a correlation ID for tracing across async boundaries.

**Preserved Behavior:** Same operation names, same log messages (except we add one LogInformation at the start of each operation). No behavior changes to launch/capture logic.

**Alternatives Rejected:**
- **Keep string templates in BeginScope** — rejected: loses correlation ID and doesn't demonstrate the abstraction
- **Remove detail properties from messages** — rejected: runtime/executable are useful message details, not operation identity

### Decision 4: Adopt in AgentProcessMonitor (Add Missing Scopes)

**Current:** `GetRunningAgents` logs errors without operation context.

**Change:** Wrap operations with `loggingContext.BeginOperation("EnumerateAgentProcesses")`.

**Implementation (all-plugins overload, line 82):**
```csharp
public IReadOnlyList<RunningAgent> GetRunningAgents(
    AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine)
{
    using var scope = this.loggingContext.BeginOperation("EnumerateAgentProcesses");
    var runningAgents = new List<RunningAgent>();

    try
    {
        var plugins = this.pluginCatalog.GetAllPlugins();
        var matches = this.orchestrator.FindRunningAgentsAsync(plugins, detail)
            .GetAwaiter().GetResult();

        foreach (var match in matches)
        {
            runningAgents.Add(new RunningAgent(match.Plugin, MapToInstance(match.Process)));
        }
    }
    catch (Exception ex)
    {
        this.logger.LogError(ex, "Failed to check for running agents");
    }

    return runningAgents;
}
```

**Rationale:** Errors now log with operation context. Correlation ID traces the operation across `FindRunningAgentsAsync` (which itself has a scope in ProcessOrchestrator).

**Preserved Behavior:** Same error messages, same public API. Spec requirement ("failures log through the monitor") is unchanged; we're just adding context to those failures.

### Decision 5: Adopt in ApplicationOperationLifetime (Add Scope to Tracked Operations)

**Current:** `RunObservedAsync` logs operation name only in failure/cancellation messages.

**Change:** Wrap operation execution with `loggingContext.BeginOperation(operationName)`.

**Implementation (line 91):**
```csharp
private async Task RunObservedAsync(string operationName,
                                    Func<CancellationToken, Task> operation)
{
    using var scope = this.loggingContext.BeginOperation(operationName);

    try
    {
        await operation(this.cancellation.Token);
    }
    catch (OperationCanceledException) when (this.cancellation.IsCancellationRequested)
    {
        this.logger.LogInformation("Application operation cancelled: {OperationName}", operationName);
    }
    catch (Exception ex)
    {
        this.logger.LogError(ex, "Application operation failed: {OperationName}", operationName);
    }
}
```

**Rationale:** All logs from tracked operations (e.g., "Agent blocked-state population") now carry the operation name and correlation ID in scope. Failure/cancellation messages keep the operation name as a message property for clarity.

**Preserved Behavior:** Same operation tracking, same cancellation handling, same error reporting.

### Decision 6: Adopt in ProjectScanningService (Add Operation Boundary)

**Current:** Scan logs appear at multiple sites without a common boundary.

**Change:** Add `loggingContext.BeginOperation("ProjectScan")` in `TriggerScanAsync`.

**Note:** Full implementation location depends on scan flow; inspect `TriggerScanAsync` and `ScanProjectsAsync` to find the right wrapping point. Goal: one scope per scan invocation, not per directory.

**Rationale:** Scan logs (warnings, errors, status) now tie to a single correlation ID, making it easy to see which logs belong to one scan pass.

**Preserved Behavior:** Same scan logic, same warning/error messages.

### Decision 7: DI Registration in AddClIHubCoreServices

**Registration:**
```csharp
// In ServiceCollectionExtensions.AddClIHubCoreServices, after services.AddLogging():
services.AddSingleton<ILoggingContextProvider, LoggingContextProvider>();
```

**Rationale:** Singleton registration; one provider instance services all consumers. The provider is stateless (correlation IDs are per-operation, not per-provider).

**Alternatives Rejected:**
- **Scoped registration** — rejected: CLIHub uses singleton services; scoped lifetime doesn't match
- **Separate registration method** — rejected: one interface, one line; no need for a helper

### Decision 8: Test with NullLogger; Add Integration Test per Surface

**Unit tests for LoggingContextProvider:**
- Use `NullLogger<LoggingContextProvider>.Instance` for tests that don't verify log output
- Add a custom recording logger for tests that verify scope properties (`OperationName`, `CorrelationId`)

**Integration tests for adopted surfaces:**
- ProcessOrchestrator: verify `BeginOperation` called with correct operation names
- AgentProcessMonitor: verify scope created around enumeration
- ApplicationOperationLifetime: verify scope wraps tracked operation
- ProjectScanningService: verify scan logs carry correlation ID

**Recording logger approach:**
```csharp
internal sealed class RecordingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, IReadOnlyList<KeyValuePair<string, object>> Scopes)> Entries { get; } = [];
    
    // Capture BeginScope and log entries
}
```

**Alternatives Rejected:**
- **Mock ILogger** — rejected: MEL logger interface is complex; a recording logger is simpler
- **Skip integration tests** — rejected: we need to prove the scope appears in real usage

### Decision 9: No Spec Delta (skip_specs: true)

**Rationale:** The logging spec (`openspec/specs/logging/spec.md`) pins:
- File location (`%APPDATA%\CLIHub\logs\`)
- Rotation (daily, 7-day retention)
- Levels (`Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`)
- Structured format (timestamp, level, message)
- Exception logging
- Service integration (`ILogger<T>` from DI)

The spec does **not** constrain what structured properties can appear (line 38-39: "structured logging with timestamp, level, and message"). Adding `OperationName` and `CorrelationId` properties is allowed. Same sink (Serilog), same file format, same messages.

**Conclusion:** Set `skip_specs: true` in `.openspec.yaml` because this is an internal change with no spec-visible behavior.

**Alternatives Rejected:**
- **Write a delta spec** — rejected: no spec-pinned behavior changes
- **Update the spec to forbid new properties** — rejected: spec doesn't need tighter constraints

## Implementation Notes

### Correlation ID Collision Risk

8-character hex → 2^32 space (~4.3 billion unique IDs). For CLIHub's operation concurrency (at most dozens of operations per second), collision probability is negligible. If a collision occurs, it's cosmetic (two unrelated operations share an ID in logs) — no functional breakage.

### Async Context Propagation

`ILogger.BeginScope` uses `AsyncLocal<T>` internally (per MEL's ambient-scope design). Scopes automatically flow through `await` boundaries. No manual state passing required.

### Backward Compatibility

All changes are additive:
- Existing `ILogger<T>` injections unchanged
- Existing log messages unchanged (except ProcessOrchestrator gains explicit startup messages)
- Existing tests unchanged (except ProcessOrchestrator tests may need updated mocks if they verify `BeginScope` calls)

## Open Questions

1. **ProjectScanningService adoption point** — Should the scope wrap `TriggerScanAsync` or `ScanProjectsAsync`? Need to inspect the flow to ensure one scope per scan, not one per directory.
   - **Resolution needed:** Read `ProjectScanningService` implementation and determine the operation boundary.

2. **Test logger for scope verification** — Should we add a `RecordingLogger<T>` to `tests/CLIHub.Core.Tests/` for reuse, or inline the recording logic in `LoggingContextProviderTests`?
   - **Recommendation:** Inline for now; extract to shared utility if a second test needs it.

3. **ProcessOrchestrator test impact** — If existing tests mock `ILogger<ProcessOrchestrator>.BeginScope`, they may break. Should we update the tests or make the scope optional?
   - **Recommendation:** Update tests to inject `ILoggingContextProvider` (use a mock or real instance).

4. **Correlation ID length** — Is 8 characters (2^32 space) sufficient, or should we use 16 (2^64)?
   - **Recommendation:** Start with 8; increase if collisions become a problem (they won't for CLIHub's scale).
