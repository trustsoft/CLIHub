# Design

## Current State (Verified)

### Runtime Resolution

**File:** `src/CLIHub.Core/Infrastructure/Processes/IRuntimeExecutableLocator.cs:8-25`

```csharp
public interface IRuntimeExecutableLocator
{
    string? Locate(RuntimeKind kind);
}
```

- Synchronous resolution of `RuntimeKind` to executable path
- Returns `null` when locator cannot resolve (not the same as "not installed")
- Windows implementation resolves `GitBash` and `PowerShell7`; returns `null` for built-in kinds
- **No caching** — re-reads installation sources on every call

**Implementation:** `src/CLIHub.Core.Windows/WindowsRuntimeExecutableLocator.cs` (inferred from interface contract)

### Command Line Building

**File:** `src/CLIHub.Core/Infrastructure/Processes/WindowsCommandLineBuilder.cs:10-224`

```csharp
internal static class WindowsCommandLineBuilder
{
    internal static (string FileName, string Arguments) BuildInteractive(
        RuntimeKind runtime,
        PluginCommand command,
        string workingDirectory,
        string? gitBashExecutable,
        string? powerShell7Executable)
    
    internal static (string FileName, string Arguments) BuildCapturedCommand(
        string executable,
        string? arguments)
    
    internal static string BuildCommandLine(PluginCommand command)
}
```

- **Static class** with internal visibility
- `BuildInteractive`: returns `(FileName, Arguments)` tuple for terminal launch
- `BuildCapturedCommand`: returns `(FileName, Arguments)` for output capture (uses `COMSPEC` or `cmd.exe`)
- Requires resolved executables passed as parameters (`gitBashExecutable`, `powerShell7Executable`)
- Handles runtime-specific quoting/escaping (cmd, PowerShell, bash)

### Process Launching

**File:** `src/CLIHub.Core/Infrastructure/Processes/IInteractiveProcessRunner.cs` (inferred)

```csharp
public interface IInteractiveProcessRunner
{
    Task LaunchAsync(string executable, string arguments, string workingDirectory, CancellationToken cancellationToken);
}
```

**File:** `src/CLIHub.Core/Infrastructure/Processes/IProcessOutputRunner.cs` (inferred)

```csharp
public interface IProcessOutputRunner
{
    Task<ProcessCaptureResult> CaptureAsync(string executable, string arguments, string workingDirectory, CancellationToken cancellationToken);
}
```

- Both are async and take cancellation tokens
- `IInteractiveProcessRunner` launches without capturing (fire-and-forget)
- `IProcessOutputRunner` waits and captures stdout/stderr

**Existing Result Model:** `src/CLIHub.Core/Models/Processes/ProcessCaptureResult.cs:10`

```csharp
public sealed record ProcessCaptureResult(bool Started, int ExitCode, string StdOut, string StdErr);
```

### Process Matching

**File:** `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs:9-182`

```csharp
public sealed class AgentProcessMatcher
{
    public static IReadOnlySet<string> RuntimeHostProcessNames { get; } = 
        new HashSet<string>(["node", "python", "pythonw"], StringComparer.OrdinalIgnoreCase);
    
    public IReadOnlySet<string> GetCandidateProcessNames(
        IEnumerable<Plugin> plugins,
        AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine)
    
    public bool IsMatch(
        AgentProcessInstance processInstance,
        Plugin plugin,
        AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine)
}
```

**File:** `src/CLIHub.Core/Infrastructure/Processes/IAgentProcessInspector.cs` (inferred from `docs/architecture/processes.md:15-17`)

```csharp
public interface IAgentProcessInspector
{
    Task<IReadOnlyList<AgentProcessInstance>> GetRunningAgentsAsync(
        IReadOnlySet<string> candidateNames,
        AgentProcessMatchDetail detail,
        CancellationToken cancellationToken);
}
```

- Returns `AgentProcessInstance` records with `ProcessId`, `ExecutablePath`, `CommandLine`, `StartTime`
- Uses WMI under `CommandLine` detail with 2-second timeout
- Pre-filters by candidate names; degrades to executable-name-only on timeout

**Existing Models:**
- `src/CLIHub.Core/Models/Agents/AgentProcessInstance.cs` — process record
- `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatchDetail.cs` — `ExecutableName` or `CommandLine` enum

### Runtime Model

**File:** `src/CLIHub.Core/Models/Processes/RuntimeKind.cs:6-27`

```csharp
public enum RuntimeKind
{
    PowerShell,      // powershell.exe
    PowerShell7,     // pwsh.exe
    CommandPrompt,   // cmd.exe
    GitBash          // bash.exe
}
```

### Plugin Command Model

**File:** `src/CLIHub.Core/Models/Plugins/PluginCommand.cs:6-22`

```csharp
public class PluginCommand
{
    public string? Name { get; set; }
    public required string Executable { get; set; }
    public string? Arguments { get; set; }
}
```

**File:** `src/CLIHub.Core/Models/Agents/AgentCommands.cs:7-42`

```csharp
public class AgentCommands
{
    public PluginCommand? Launch { get; set; }
    public PluginCommand? Resume { get; set; }
    public PluginCommand? Version { get; set; }
    public PluginCommand? Update { get; set; }
    
    public PluginCommand? Get(AgentCommandKind kind) { ... }
}
```

## Architecture Decisions

### Decision 1: New Namespace, Coexistence Strategy

**Rationale:** Create `CLIHub.Core/Processes/` separate from `Infrastructure/Processes/` to signal the new domain boundary. Old types remain in `Infrastructure/` and continue working. Orchestrator wraps old types through adapters.

**Alternatives Rejected:**
- **Replace existing types in place** — would break all consumers immediately
- **Keep in `Infrastructure/`** — muddles old/new boundary; harder to see migration status

### Decision 2: Immutable Records for Domain Models

**Rationale:** Process lifecycle values (`RuntimeExecutable`, `AgentProcess`, `LaunchRequest`, `ProcessResult`) are immutable snapshots — once created, they don't change. Records with `init`-only properties enforce this and provide value equality.

**Alternatives Rejected:**
- **Mutable classes** — risk of accidental state mutation
- **Readonly structs** — these models contain reference types; records are clearer

### Decision 3: Adapter Pattern for Existing Services

**Rationale:** The orchestrator should not duplicate logic from `WindowsCommandLineBuilder`, `IRuntimeExecutableLocator`, etc. Thin adapters (`RuntimeResolver`, `CommandLineBuilderAdapter`, `AgentProcessFinder`) wrap existing implementations and translate to new domain types.

**Implementation:**
- `RuntimeResolver` wraps `IRuntimeExecutableLocator` and returns `RuntimeExecutable?`
- `CommandLineBuilderAdapter` wraps static `WindowsCommandLineBuilder` methods
- `AgentProcessFinder` wraps `IAgentProcessInspector` + `AgentProcessMatcher` and returns `AgentProcess` list

**Alternatives Rejected:**
- **Rewrite all logic in orchestrator** — duplicates tested code; high risk
- **Make orchestrator depend directly on old types** — leaks infrastructure types into domain

### Decision 4: LaunchRequest Uses RuntimeKind, Not RuntimeExecutable

**Rationale:** Consumers (e.g., `AgentCommandService`) only know the user's preference as a `RuntimeKind`. The orchestrator resolves this to a `RuntimeExecutable` internally. This keeps the request model simple and doesn't force callers to resolve before calling.

**Alternatives Rejected:**
- **LaunchRequest takes RuntimeExecutable** — caller must resolve first; more coupling

### Decision 5: ProcessResult Has Factory Methods

**Rationale:** `ProcessResult.Success(...)`, `ProcessResult.Failure(...)`, `ProcessResult.Timeout()`, `ProcessResult.Cancelled()` make construction intent-revealing and ensure `Kind` matches other fields.

**Alternatives Rejected:**
- **Public constructor only** — risk of inconsistent `Kind` vs fields

### Decision 6: ProcessOrchestrator Is Not Static

**Rationale:** Orchestrator is registered in DI with injected dependencies. This allows testing with mocks and keeps the design extensible.

**Alternatives Rejected:**
- **Static orchestrator** — cannot mock dependencies; violates SOLID

### Decision 7: Skip Specs, Mark as Refactor

**Rationale:** This change introduces **no new user-facing behavior**. It wraps existing process launching, matching, and runtime resolution. The behavior contract is already captured in `agent-process-matching` spec and `docs/architecture/processes.md`. A delta spec would duplicate existing scenarios without adding value.

**Marked:** `skip_specs: true` in `.openspec.yaml`

**Alternatives Rejected:**
- **Write delta spec** — would copy existing scenarios verbatim; waste of effort
- **Modify existing specs** — nothing changes from user/consumer perspective in Phase 1+2

## Component Design

### Domain Models

#### RuntimeExecutable

```csharp
namespace CLIHub.Core.Processes;

/// <summary>
/// Represents an executable runtime host (terminal or interpreter).
/// </summary>
public record RuntimeExecutable
{
    public required RuntimeKind Kind { get; init; }
    public required string ExecutablePath { get; init; }
    public string? Version { get; init; }

    public static RuntimeExecutable Create(RuntimeKind kind, string path, string? version = null)
        => new() { Kind = kind, ExecutablePath = path, Version = version };
}
```

#### AgentProcess

```csharp
namespace CLIHub.Core.Processes;

/// <summary>
/// Represents a running agent process with its execution context.
/// </summary>
public record AgentProcess
{
    public required int ProcessId { get; init; }
    public required string CommandLine { get; init; }
    public required RuntimeExecutable? Runtime { get; init; }
    public required DateTime StartTime { get; init; }

    public bool IsRuntimeHosted => this.Runtime?.Kind is RuntimeKind.Node or RuntimeKind.Python;
}
```

**Note:** `RuntimeKind.Node` and `RuntimeKind.Python` do not exist in current `RuntimeKind` enum. This is a discrepancy in the original proposal. The property should check if the runtime is *detected* as Node/Python from the process, not if the `RuntimeKind` enum has those values. The design must account for this.

**Design Correction:** `IsRuntimeHosted` will check the process executable name against `AgentProcessMatcher.RuntimeHostProcessNames` instead.

#### LaunchRequest

```csharp
namespace CLIHub.Core.Processes;

/// <summary>
/// Request to launch an agent command in a specific context.
/// </summary>
public record LaunchRequest
{
    public required string WorkingDirectory { get; init; }
    public required PluginCommand Command { get; init; }
    public required RuntimeKind RuntimeKind { get; init; }
    public CancellationToken CancellationToken { get; init; }

    public static LaunchRequest Create(
        string workingDirectory,
        PluginCommand command,
        RuntimeKind runtimeKind,
        CancellationToken cancellationToken = default)
        => new()
        {
            WorkingDirectory = workingDirectory,
            Command = command,
            RuntimeKind = runtimeKind,
            CancellationToken = cancellationToken
        };
}
```

#### ProcessResult

```csharp
namespace CLIHub.Core.Processes;

/// <summary>
/// Result of a process execution.
/// </summary>
public record ProcessResult
{
    public required ProcessResultKind Kind { get; init; }
    public int? ExitCode { get; init; }
    public string? Output { get; init; }
    public string? Error { get; init; }
    public TimeSpan? Duration { get; init; }

    public bool IsSuccess => this.Kind == ProcessResultKind.Success;

    public static ProcessResult Success(int exitCode, string? output = null, TimeSpan? duration = null)
        => new()
        {
            Kind = ProcessResultKind.Success,
            ExitCode = exitCode,
            Output = output,
            Duration = duration
        };

    public static ProcessResult Failure(string error)
        => new() { Kind = ProcessResultKind.Failure, Error = error };

    public static ProcessResult Timeout()
        => new() { Kind = ProcessResultKind.Timeout };

    public static ProcessResult Cancelled()
        => new() { Kind = ProcessResultKind.Cancelled };
}

public enum ProcessResultKind
{
    Success,
    Failure,
    Timeout,
    Cancelled
}
```

#### ProcessMatchCriteria

```csharp
namespace CLIHub.Core.Processes;

/// <summary>
/// Criteria for matching agent processes.
/// </summary>
public record ProcessMatchCriteria
{
    public required Plugin Plugin { get; init; }
    public required AgentProcessMatchDetail Detail { get; init; }
    public TimeSpan? Timeout { get; init; }

    public static ProcessMatchCriteria Create(
        Plugin plugin,
        AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine,
        TimeSpan? timeout = null)
        => new() { Plugin = plugin, Detail = detail, Timeout = timeout };
}
```

### Supporting Interfaces

#### IRuntimeResolver

```csharp
namespace CLIHub.Core.Processes;

/// <summary>
/// Resolves runtime executables based on runtime kind.
/// </summary>
public interface IRuntimeResolver
{
    Task<RuntimeExecutable?> ResolveAsync(
        RuntimeKind kind,
        CancellationToken cancellationToken = default);
}
```

#### ICommandLineBuilder

```csharp
namespace CLIHub.Core.Processes;

/// <summary>
/// Builds command lines for specific runtimes.
/// </summary>
public interface ICommandLineBuilder
{
    (string FileName, string Arguments) BuildInteractive(
        RuntimeExecutable runtime,
        PluginCommand command,
        string workingDirectory);
    
    (string FileName, string Arguments) BuildCaptured(
        string executable,
        string? arguments);
}
```

#### IAgentProcessFinder

```csharp
namespace CLIHub.Core.Processes;

/// <summary>
/// Finds running agent processes matching criteria.
/// </summary>
public interface IAgentProcessFinder
{
    Task<IReadOnlyList<AgentProcess>> FindAsync(
        ProcessMatchCriteria criteria,
        CancellationToken cancellationToken = default);
}
```

### Orchestrator Interface

```csharp
namespace CLIHub.Core.Processes;

/// <summary>
/// Orchestrates all process operations: launching, capturing output, and finding running agents.
/// </summary>
public interface IProcessOrchestrator
{
    /// <summary>
    /// Launches an agent command in an interactive terminal.
    /// </summary>
    Task<ProcessResult> LaunchInteractiveAsync(LaunchRequest request);

    /// <summary>
    /// Executes an agent command and captures its output.
    /// </summary>
    Task<ProcessResult> CaptureOutputAsync(LaunchRequest request);

    /// <summary>
    /// Finds all running processes matching the specified plugin.
    /// </summary>
    Task<IReadOnlyList<AgentProcess>> FindRunningAgentsAsync(
        ProcessMatchCriteria criteria);

    /// <summary>
    /// Resolves the available runtime executable for the specified kind.
    /// </summary>
    Task<RuntimeExecutable?> ResolveRuntimeAsync(
        RuntimeKind kind,
        CancellationToken cancellationToken = default);
}
```

### Orchestrator Implementation Outline

```csharp
namespace CLIHub.Core.Processes;

public class ProcessOrchestrator : IProcessOrchestrator
{
    private readonly IRuntimeResolver runtimeResolver;
    private readonly ICommandLineBuilder commandLineBuilder;
    private readonly IInteractiveProcessRunner interactiveRunner;
    private readonly IProcessOutputRunner outputRunner;
    private readonly IAgentProcessFinder processFinder;
    private readonly ILogger<ProcessOrchestrator> logger;

    public ProcessOrchestrator(
        IRuntimeResolver runtimeResolver,
        ICommandLineBuilder commandLineBuilder,
        IInteractiveProcessRunner interactiveRunner,
        IProcessOutputRunner outputRunner,
        IAgentProcessFinder processFinder,
        ILogger<ProcessOrchestrator> logger) { ... }

    public async Task<ProcessResult> LaunchInteractiveAsync(LaunchRequest request)
    {
        // 1. Resolve runtime
        var runtime = await this.runtimeResolver.ResolveAsync(request.RuntimeKind, request.CancellationToken);
        if (runtime is null)
            return ProcessResult.Failure($"Runtime {request.RuntimeKind} not available");

        // 2. Build command line
        var (fileName, arguments) = this.commandLineBuilder.BuildInteractive(
            runtime, request.Command, request.WorkingDirectory);

        // 3. Launch
        try
        {
            var startTime = DateTime.UtcNow;
            await this.interactiveRunner.LaunchAsync(
                fileName, arguments, request.WorkingDirectory, request.CancellationToken);
            var duration = DateTime.UtcNow - startTime;
            return ProcessResult.Success(0, duration: duration);
        }
        catch (OperationCanceledException)
        {
            return ProcessResult.Cancelled();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to launch {Command}", request.Command.Executable);
            return ProcessResult.Failure(ex.Message);
        }
    }

    public async Task<ProcessResult> CaptureOutputAsync(LaunchRequest request)
    {
        // Similar structure: resolve, build, capture, map result
        // Maps ProcessCaptureResult to ProcessResult
    }

    public async Task<IReadOnlyList<AgentProcess>> FindRunningAgentsAsync(
        ProcessMatchCriteria criteria)
    {
        try
        {
            return await this.processFinder.FindAsync(criteria, CancellationToken.None);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to find running agents for plugin {PluginId}",
                criteria.Plugin.Id);
            return Array.Empty<AgentProcess>();
        }
    }

    public async Task<RuntimeExecutable?> ResolveRuntimeAsync(
        RuntimeKind kind,
        CancellationToken cancellationToken = default)
    {
        return await this.runtimeResolver.ResolveAsync(kind, cancellationToken);
    }
}
```

### Adapter Implementations

#### RuntimeResolver

```csharp
namespace CLIHub.Core.Processes;

internal sealed class RuntimeResolver : IRuntimeResolver
{
    private readonly IRuntimeExecutableLocator locator;

    public RuntimeResolver(IRuntimeExecutableLocator locator)
    {
        this.locator = locator;
    }

    public Task<RuntimeExecutable?> ResolveAsync(
        RuntimeKind kind,
        CancellationToken cancellationToken = default)
    {
        var path = this.locator.Locate(kind);
        if (path is null && kind is RuntimeKind.PowerShell or RuntimeKind.CommandPrompt)
        {
            // Built-in Windows shells — locator returns null but they exist
            path = kind == RuntimeKind.PowerShell ? "powershell.exe" : "cmd.exe";
        }

        if (path is null)
        {
            return Task.FromResult<RuntimeExecutable?>(null);
        }

        var runtime = RuntimeExecutable.Create(kind, path, version: null);
        return Task.FromResult<RuntimeExecutable?>(runtime);
    }
}
```

#### CommandLineBuilderAdapter

```csharp
namespace CLIHub.Core.Processes;

internal sealed class CommandLineBuilderAdapter : ICommandLineBuilder
{
    public (string FileName, string Arguments) BuildInteractive(
        RuntimeExecutable runtime,
        PluginCommand command,
        string workingDirectory)
    {
        // Extract resolved executables for GitBash and PowerShell7
        string? gitBashExe = runtime.Kind == RuntimeKind.GitBash ? runtime.ExecutablePath : null;
        string? ps7Exe = runtime.Kind == RuntimeKind.PowerShell7 ? runtime.ExecutablePath : null;

        return WindowsCommandLineBuilder.BuildInteractive(
            runtime.Kind, command, workingDirectory, gitBashExe, ps7Exe);
    }

    public (string FileName, string Arguments) BuildCaptured(
        string executable,
        string? arguments)
    {
        return WindowsCommandLineBuilder.BuildCapturedCommand(executable, arguments);
    }
}
```

#### AgentProcessFinder

```csharp
namespace CLIHub.Core.Processes;

internal sealed class AgentProcessFinder : IAgentProcessFinder
{
    private readonly IAgentProcessInspector inspector;
    private readonly AgentProcessMatcher matcher;
    private readonly IPluginCatalog catalog;

    public AgentProcessFinder(
        IAgentProcessInspector inspector,
        AgentProcessMatcher matcher,
        IPluginCatalog catalog)
    {
        this.inspector = inspector;
        this.matcher = matcher;
        this.catalog = catalog;
    }

    public async Task<IReadOnlyList<AgentProcess>> FindAsync(
        ProcessMatchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var candidateNames = this.matcher.GetCandidateProcessNames(
            new[] { criteria.Plugin }, criteria.Detail);

        var instances = await this.inspector.GetRunningAgentsAsync(
            candidateNames, criteria.Detail, cancellationToken);

        var matches = new List<AgentProcess>();
        foreach (var instance in instances)
        {
            if (this.matcher.IsMatch(instance, criteria.Plugin, criteria.Detail))
            {
                matches.Add(MapToAgentProcess(instance));
            }
        }

        return matches;
    }

    private static AgentProcess MapToAgentProcess(AgentProcessInstance instance)
    {
        RuntimeExecutable? runtime = null;
        var executableName = Path.GetFileNameWithoutExtension(instance.ExecutablePath);
        if (AgentProcessMatcher.RuntimeHostProcessNames.Contains(executableName))
        {
            // Detected as runtime-hosted; map to RuntimeExecutable
            // Note: RuntimeKind doesn't have Node/Python, so we'll leave Runtime null
            // or add a comment that this is a known limitation
        }

        return new AgentProcess
        {
            ProcessId = instance.ProcessId,
            CommandLine = instance.CommandLine,
            Runtime = runtime,  // Limitation: cannot map runtime hosts to RuntimeExecutable
            StartTime = instance.StartTime
        };
    }
}
```

**Open Question:** How to represent Node/Python runtime hosts in `RuntimeExecutable` when `RuntimeKind` only has shell kinds? Options:
1. Leave `Runtime` as `null` for runtime-hosted processes (document limitation)
2. Extend `RuntimeKind` with `Node`, `Python`, `PythonW` (out of scope for this change)
3. Add a separate flag `IsRuntimeHosted` to `AgentProcess` (redundant with property)

**Decision:** Leave `Runtime` as `null` for runtime-hosted processes and document in `AgentProcess.Runtime` XML doc that it's only populated for shell-hosted processes. The `IsRuntimeHosted` computed property already covers the detection.

## Integration Points

### DI Registration

**File:** `src/CLIHub/DependencyInjection/ProcessesServiceRegistration.cs` (create new file)

```csharp
namespace CLIHub.DependencyInjection;

using CLIHub.Core.Processes;

internal static class ProcessesServiceRegistration
{
    internal static IServiceCollection AddProcessOrchestrator(this IServiceCollection services)
    {
        // Adapters
        services.AddSingleton<IRuntimeResolver, RuntimeResolver>();
        services.AddSingleton<ICommandLineBuilder, CommandLineBuilderAdapter>();
        services.AddSingleton<IAgentProcessFinder, AgentProcessFinder>();

        // Orchestrator
        services.AddSingleton<IProcessOrchestrator, ProcessOrchestrator>();

        return services;
    }
}
```

Called from existing DI setup after existing process services are registered.

## Open Questions

1. **RuntimeKind extension:** Should we add `Node`, `Python`, `PythonW` to `RuntimeKind` or document that `AgentProcess.Runtime` is null for runtime-hosted processes?
   - **Recommendation:** Document null for runtime-hosted; extending enum is out of scope

2. **AgentProcess.IsRuntimeHosted accuracy:** Should we add an executable-path check or rely solely on `RuntimeHostProcessNames`?
   - **Recommendation:** Use `RuntimeHostProcessNames`; it's the existing contract

3. **ProcessResult.Output field:** Should we trim it like `ProcessCaptureResult` does?
   - **Recommendation:** Yes, match existing behavior

4. **Timeout handling in CaptureOutputAsync:** Who owns the timeout — the runner or the orchestrator?
   - **Current:** `IProcessOutputRunner` likely has built-in timeout; orchestrator just propagates

5. **Test strategy:** Full integration tests or unit tests with mocks?
   - **Recommendation:** Unit tests in Phase 2; integration tests deferred to Phase 3 consumer migration

## Deviations from Proposal

1. **LaunchRequest uses RuntimeKind, not RuntimePreference:** Proposal referenced `RuntimePreference`, which doesn't exist in codebase. Using `RuntimeKind` which is the actual type.

2. **AgentProcess.IsRuntimeHosted implementation:** Proposal checked `RuntimeKind.Node or RuntimeKind.Python`, but those enum values don't exist. Changed to check process executable name against `RuntimeHostProcessNames`.

3. **AgentProcess.Runtime is nullable:** Proposal showed it as non-nullable with Node/Python runtime kinds. Since those don't exist, making it nullable and documenting that it's null for runtime-hosted processes.

4. **Skip specs marked:** Proposal didn't specify, but this is a pure internal refactor with no behavior change — specs are skipped per decision 7.
