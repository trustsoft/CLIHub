# Proposal 004: Process Domain Model
**Created:** 2026-10-29  
**Status:** Approved (2026-10-29)  
**Effort:** High (5-7 days)  
**Risk:** Medium  
**Priority:** Phase 2 (Q2 2027)  
**Dependencies:** Proposal 1 (Cross-Cutting Services)  
**Owner:** TBD

## Problem Statement

Process launching, command building, runtime resolution, and process matching are currently scattered across multiple services with unclear boundaries:

### Current Fragmentation

```
ProcessLauncher (Core)
  └─> Builds command lines
  └─> No actual process execution

WindowsInteractiveProcessRunner (Windows)
  └─> Launches terminals
  └─> Resolves runtimes (pwsh, bash, cmd)

WindowsProcessOutputRunner (Windows)
  └─> Captures output
  └─> Duplicate runtime resolution

AgentProcessMatcher (Core)
  └─> Matches running processes
  └─> Command-line parsing logic

WindowsAgentProcessInspector (Windows)
  └─> WMI queries for command lines
  └─> Timeout and degradation logic

AgentCommandService (Core)
  └─> Orchestrates execution
  └─> Depends on multiple runners
```

### Issues

1. **Runtime resolution is duplicated** across interactive and output runners
2. **Process matching logic is split** between matcher (Core) and inspector (Windows)
3. **No unified process model** — different representations in different layers
4. **Testing is complex** — must mock multiple services to test a single workflow
5. **Command-line building is separate** from execution context
6. **No explicit process lifecycle** — launch, monitor, match, terminate are disconnected

## Proposed Solution

Introduce a unified **Process Domain** with explicit value objects and a single orchestration boundary.

### Domain Models

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

/// <summary>
/// Request to launch an agent command in a specific context.
/// </summary>
public record LaunchRequest
{
    public required string WorkingDirectory { get; init; }
    public required AgentCommand Command { get; init; }
    public required RuntimePreference Preference { get; init; }
    public CancellationToken CancellationToken { get; init; }

    public static LaunchRequest Create(
        string workingDirectory,
        AgentCommand command,
        RuntimePreference preference,
        CancellationToken cancellationToken = default)
        => new()
        {
            WorkingDirectory = workingDirectory,
            Command = command,
            Preference = preference,
            CancellationToken = cancellationToken
        };
}

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

### Unified Orchestrator

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
    /// <param name="request">Launch request with working directory, command, and runtime preference.</param>
    /// <returns>Process result with execution outcome.</returns>
    Task<ProcessResult> LaunchInteractiveAsync(LaunchRequest request);

    /// <summary>
    /// Executes an agent command and captures its output.
    /// </summary>
    /// <param name="request">Launch request with working directory, command, and runtime preference.</param>
    /// <returns>Process result with exit code and captured output.</returns>
    Task<ProcessResult> CaptureOutputAsync(LaunchRequest request);

    /// <summary>
    /// Finds all running processes matching the specified plugin.
    /// </summary>
    /// <param name="criteria">Match criteria with plugin, detail level, and optional timeout.</param>
    /// <returns>List of matching agent processes.</returns>
    Task<IReadOnlyList<AgentProcess>> FindRunningAgentsAsync(ProcessMatchCriteria criteria);

    /// <summary>
    /// Resolves the available runtime executable for the specified preference.
    /// </summary>
    /// <param name="preference">Runtime preference (PowerShell, Git Bash, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resolved runtime executable, or null if unavailable.</returns>
    Task<RuntimeExecutable?> ResolveRuntimeAsync(
        RuntimePreference preference,
        CancellationToken cancellationToken = default);
}
```

### Implementation Structure

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
        ILogger<ProcessOrchestrator> logger)
    {
        this.runtimeResolver = runtimeResolver;
        this.commandLineBuilder = commandLineBuilder;
        this.interactiveRunner = interactiveRunner;
        this.outputRunner = outputRunner;
        this.processFinder = processFinder;
        this.logger = logger;
    }

    public async Task<ProcessResult> LaunchInteractiveAsync(LaunchRequest request)
    {
        using var scope = this.logger.BeginScope("LaunchInteractive");

        // Resolve runtime
        var runtime = await this.runtimeResolver.ResolveAsync(
            request.Preference,
            request.CancellationToken);

        if (runtime is null)
        {
            this.logger.LogWarning(
                "Runtime {Preference} not available for command {Command}",
                request.Preference,
                request.Command.Kind);
            return ProcessResult.Failure($"Runtime {request.Preference} not available");
        }

        // Build command line
        var commandLine = this.commandLineBuilder.Build(
            runtime,
            request.Command,
            request.WorkingDirectory);

        // Launch
        try
        {
            var startTime = DateTime.UtcNow;
            await this.interactiveRunner.LaunchAsync(
                runtime.ExecutablePath,
                commandLine,
                request.WorkingDirectory,
                request.CancellationToken);

            var duration = DateTime.UtcNow - startTime;
            this.logger.LogInformation(
                "Launched {Command} in {Runtime} (took {Duration}ms)",
                request.Command.Kind,
                runtime.Kind,
                duration.TotalMilliseconds);

            return ProcessResult.Success(0, duration: duration);
        }
        catch (OperationCanceledException)
        {
            return ProcessResult.Cancelled();
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to launch {Command}", request.Command.Kind);
            return ProcessResult.Failure(ex.Message);
        }
    }

    public async Task<ProcessResult> CaptureOutputAsync(LaunchRequest request)
    {
        using var scope = this.logger.BeginScope("CaptureOutput");

        // Similar structure to LaunchInteractiveAsync but uses outputRunner
        // Returns ProcessResult with captured output
        // Implementation details follow the same pattern
        throw new NotImplementedException("See full implementation");
    }

    public async Task<IReadOnlyList<AgentProcess>> FindRunningAgentsAsync(
        ProcessMatchCriteria criteria)
    {
        using var scope = this.logger.BeginScope("FindRunningAgents");

        try
        {
            return await this.processFinder.FindAsync(criteria);
        }
        catch (Exception ex)
        {
            this.logger.LogError(
                ex,
                "Failed to find running agents for plugin {PluginId}",
                criteria.Plugin.Id);
            return Array.Empty<AgentProcess>();
        }
    }

    public async Task<RuntimeExecutable?> ResolveRuntimeAsync(
        RuntimePreference preference,
        CancellationToken cancellationToken = default)
    {
        return await this.runtimeResolver.ResolveAsync(preference, cancellationToken);
    }
}
```

### Supporting Interfaces

```csharp
namespace CLIHub.Core.Processes;

/// <summary>
/// Resolves runtime executables based on preferences.
/// </summary>
public interface IRuntimeResolver
{
    Task<RuntimeExecutable?> ResolveAsync(
        RuntimePreference preference,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Builds command lines for specific runtimes.
/// </summary>
public interface ICommandLineBuilder
{
    string Build(RuntimeExecutable runtime, AgentCommand command, string workingDirectory);
}

/// <summary>
/// Finds running agent processes matching criteria.
/// </summary>
public interface IAgentProcessFinder
{
    Task<IReadOnlyList<AgentProcess>> FindAsync(ProcessMatchCriteria criteria);
}
```

## Migration Strategy

### Phase 1: Introduce Domain Models (2 days)
1. Create new `CLIHub.Core/Processes/` namespace
2. Add value objects: `RuntimeExecutable`, `AgentProcess`, `LaunchRequest`, `ProcessResult`
3. No breaking changes — new types coexist with old

### Phase 2: Implement Orchestrator (2 days)
1. Create `ProcessOrchestrator` implementing `IProcessOrchestrator`
2. Wire up existing runners/matchers as dependencies
3. Register in DI alongside existing services
4. Write comprehensive unit tests

### Phase 3: Migrate Consumers (2-3 days)
1. Update `AgentCommandService` to use `IProcessOrchestrator`
2. Update `LaunchCommandCoordinator` to use orchestrator
3. Update `WindowActionCoordinator` to use orchestrator
4. Update `AgentProcessMonitor` to use `FindRunningAgentsAsync`
5. Run full test suite after each consumer migration

### Phase 4: Deprecate Old Interfaces (optional, later)
1. Mark old interfaces as `[Obsolete]` after all consumers migrated
2. Remove in next major version

## Benefits

### 1. Single Source of Truth
All process operations go through one orchestrator — easier to reason about, debug, and extend.

### 2. Unified Process Model
`AgentProcess` and `RuntimeExecutable` are shared across all layers — no impedance mismatch.

### 3. Testability
Mock `IProcessOrchestrator` instead of 5 different services. Tests are simpler and more focused.

**Before:**
```csharp
// Must mock multiple services
var interactiveRunner = new Mock<IInteractiveProcessRunner>();
var outputRunner = new Mock<IProcessOutputRunner>();
var matcher = new Mock<IAgentProcessMatcher>();
var inspector = new Mock<IAgentProcessInspector>();
var launcher = new Mock<IProcessLauncher>();

var service = new AgentCommandService(
    interactiveRunner.Object,
    outputRunner.Object,
    launcher.Object,
    ...);
```

**After:**
```csharp
// Mock one orchestrator
var orchestrator = new Mock<IProcessOrchestrator>();
orchestrator
    .Setup(x => x.LaunchInteractiveAsync(It.IsAny<LaunchRequest>()))
    .ReturnsAsync(ProcessResult.Success(0));

var service = new AgentCommandService(orchestrator.Object, ...);
```

### 4. Clear Boundaries
- **Core** owns: domain models, orchestration interface
- **Windows** owns: platform-specific runners, WMI queries
- **UI** uses: orchestrator for all process operations

### 5. Future Extensibility
Easy to add new capabilities:
- Remote process execution (SSH, container)
- Process sandboxing (restricted permissions)
- Process telemetry (metrics, traces)
- Process pooling (reuse long-running agents)

## Example Usage

### Before (Current)

```csharp
public class AgentCommandService
{
    public async Task<AgentCommandResult> ExecuteAsync(...)
    {
        // Scattered logic across multiple services
        var commandLine = this.launcher.Build(...);
        var runtime = this.runtimeResolver.Resolve(...);

        if (command.Kind == AgentCommandKind.Interactive)
        {
            await this.interactiveRunner.LaunchAsync(...);
        }
        else
        {
            var output = await this.outputRunner.CaptureAsync(...);
        }

        // Check for running processes
        var matcher = new AgentProcessMatcher(...);
        var processes = await this.inspector.GetRunningAgentsAsync(...);
        var matches = processes.Where(p => matcher.IsMatch(p, plugin));
    }
}
```

### After (Proposed)

```csharp
public class AgentCommandService
{
    private readonly IProcessOrchestrator processOrchestrator;

    public async Task<AgentCommandResult> ExecuteAsync(
        Plugin plugin,
        AgentCommandKind commandKind,
        string projectPath,
        CancellationToken cancellationToken)
    {
        var command = plugin.Commands.Get(commandKind)
            ?? return AgentCommandResult.Failure("Command not found");

        var request = LaunchRequest.Create(
            projectPath,
            command,
            this.preferencesStore.GetRuntimePreference(),
            cancellationToken);

        var result = commandKind == AgentCommandKind.Interactive
            ? await this.processOrchestrator.LaunchInteractiveAsync(request)
            : await this.processOrchestrator.CaptureOutputAsync(request);

        return result.IsSuccess
            ? AgentCommandResult.Success(result.Output)
            : AgentCommandResult.Failure(result.Error);
    }
}

public class AgentProcessMonitor
{
    private readonly IProcessOrchestrator processOrchestrator;

    public async Task<bool> IsAgentRunningAsync(Plugin plugin)
    {
        var criteria = ProcessMatchCriteria.Create(
            plugin,
            AgentProcessMatchDetail.CommandLine,
            timeout: TimeSpan.FromSeconds(2));

        var processes = await this.processOrchestrator.FindRunningAgentsAsync(criteria);
        return processes.Count > 0;
    }
}
```

## Testing Strategy

### Unit Tests (Core)

```csharp
public class ProcessOrchestratorTests
{
    [Fact]
    public async Task LaunchInteractiveAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var runtimeResolver = new Mock<IRuntimeResolver>();
        runtimeResolver
            .Setup(x => x.ResolveAsync(It.IsAny<RuntimePreference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RuntimeExecutable.Create(RuntimeKind.PowerShell, "pwsh.exe"));

        var commandLineBuilder = new Mock<ICommandLineBuilder>();
        commandLineBuilder
            .Setup(x => x.Build(It.IsAny<RuntimeExecutable>(), It.IsAny<AgentCommand>(), It.IsAny<string>()))
            .Returns("test command");

        var interactiveRunner = new Mock<IInteractiveProcessRunner>();
        interactiveRunner
            .Setup(x => x.LaunchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var orchestrator = new ProcessOrchestrator(
            runtimeResolver.Object,
            commandLineBuilder.Object,
            interactiveRunner.Object,
            Mock.Of<IProcessOutputRunner>(),
            Mock.Of<IAgentProcessFinder>(),
            NullLogger<ProcessOrchestrator>.Instance);

        var request = LaunchRequest.Create(
            "C:\\test",
            new AgentCommand { Kind = AgentCommandKind.Interactive },
            RuntimePreference.PowerShell);

        // Act
        var result = await orchestrator.LaunchInteractiveAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        interactiveRunner.Verify(x => x.LaunchAsync(
            "pwsh.exe",
            "test command",
            "C:\\test",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LaunchInteractiveAsync_WithUnavailableRuntime_ReturnsFailure()
    {
        // Arrange
        var runtimeResolver = new Mock<IRuntimeResolver>();
        runtimeResolver
            .Setup(x => x.ResolveAsync(It.IsAny<RuntimePreference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuntimeExecutable?)null);

        var orchestrator = new ProcessOrchestrator(
            runtimeResolver.Object,
            Mock.Of<ICommandLineBuilder>(),
            Mock.Of<IInteractiveProcessRunner>(),
            Mock.Of<IProcessOutputRunner>(),
            Mock.Of<IAgentProcessFinder>(),
            NullLogger<ProcessOrchestrator>.Instance);

        var request = LaunchRequest.Create(
            "C:\\test",
            new AgentCommand { Kind = AgentCommandKind.Interactive },
            RuntimePreference.GitBash);

        // Act
        var result = await orchestrator.LaunchInteractiveAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not available");
    }

    [Fact]
    public async Task FindRunningAgentsAsync_WithValidCriteria_ReturnsProcesses()
    {
        // Arrange
        var expectedProcesses = new[]
        {
            new AgentProcess
            {
                ProcessId = 1234,
                CommandLine = "node agent.js",
                Runtime = RuntimeExecutable.Create(RuntimeKind.Node, "node.exe"),
                StartTime = DateTime.UtcNow
            }
        };

        var processFinder = new Mock<IAgentProcessFinder>();
        processFinder
            .Setup(x => x.FindAsync(It.IsAny<ProcessMatchCriteria>()))
            .ReturnsAsync(expectedProcesses);

        var orchestrator = new ProcessOrchestrator(
            Mock.Of<IRuntimeResolver>(),
            Mock.Of<ICommandLineBuilder>(),
            Mock.Of<IInteractiveProcessRunner>(),
            Mock.Of<IProcessOutputRunner>(),
            processFinder.Object,
            NullLogger<ProcessOrchestrator>.Instance);

        var criteria = ProcessMatchCriteria.Create(
            new Plugin { Id = "test-agent" },
            AgentProcessMatchDetail.CommandLine);

        // Act
        var processes = await orchestrator.FindRunningAgentsAsync(criteria);

        // Assert
        processes.Should().HaveCount(1);
        processes[0].ProcessId.Should().Be(1234);
        processes[0].IsRuntimeHosted.Should().BeTrue();
    }
}
```

### Integration Tests (Windows)

```csharp
public class ProcessOrchestratorIntegrationTests
{
    [SkippableFact]
    public async Task LaunchInteractiveAsync_WithRealPowerShell_Succeeds()
    {
        Skip.If(!OperatingSystem.IsWindows(), "Windows-only test");

        // Arrange
        var services = new ServiceCollection();
        services.AddCLIHubCoreServices();
        services.AddWindowsInfrastructure();
        var provider = services.BuildServiceProvider();

        var orchestrator = provider.GetRequiredService<IProcessOrchestrator>();

        var request = LaunchRequest.Create(
            Environment.CurrentDirectory,
            new AgentCommand
            {
                Kind = AgentCommandKind.Interactive,
                Executable = "echo",
                Arguments = "test"
            },
            RuntimePreference.PowerShell);

        // Act
        var result = await orchestrator.LaunchInteractiveAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Duration.Should().NotBeNull();
    }
}
```

## Risk Mitigation

### Risk 1: Breaking Existing Consumers
**Mitigation:**
- Introduce orchestrator alongside existing services (no removal)
- Migrate one consumer at a time
- Run full test suite after each migration
- Keep old interfaces until all consumers migrated

### Risk 2: Performance Regression
**Mitigation:**
- Benchmark before/after with BenchmarkDotNet
- Profile WMI query paths (AgentProcessFinder)
- Cache runtime resolution results (already cached in current implementation)
- Monitor launch latency in production

### Risk 3: Platform-Specific Edge Cases
**Mitigation:**
- Comprehensive Windows integration tests
- Real-process tests with all runtimes (pwsh, bash, cmd, Windows Terminal)
- Timeout and degradation paths covered by tests
- Beta testing with diverse terminal configurations

## Alternative Approaches Considered

### Alternative 1: Keep Current Structure, Add Facade
**Pros:** Minimal disruption  
**Cons:** Doesn't solve fragmentation, adds indirection layer

### Alternative 2: Move Everything to Windows Layer
**Pros:** Simplifies Core  
**Cons:** Violates ADR-0001 (Core/UI boundaries), loses cross-platform potential

### Alternative 3: Multiple Orchestrators (LaunchOrchestrator, MatchOrchestrator)
**Pros:** Smaller interfaces  
**Cons:** Still fragmented, unclear which orchestrator to use

**Decision:** Proposal 4 (single unified orchestrator) is the best balance of simplicity and power.

## Success Criteria

✅ All 1,221 tests still passing  
✅ Zero new build warnings  
✅ No performance regression (launch time within 5% of baseline)  
✅ All consumers migrated to `IProcessOrchestrator`  
✅ Integration tests for all runtimes (pwsh, bash, cmd, Windows Terminal)  
✅ ADR-0012 (Process Domain Unification) accepted and published

## Related Documents

- **Analysis:** `docs/architecture-analysis-2026-10-29.md` (Section 3.4)
- **Visualization:** `docs/architecture-visualization.md` (Data Flow section)
- **ADRs:** ADR-0001 (Core/UI boundaries), ADR-0006 (Windows platform library)
- **Current Implementation:**
  - `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs`
  - `src/CLIHub.Core.Windows/WindowsInteractiveProcessRunner.cs`
  - `src/CLIHub.Core.Windows/WindowsProcessOutputRunner.cs`
  - `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs`
  - `src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs`

---

**Status:** Ready for review  
**Owner:** TBD  
**Target:** Q2 2027 (Phase 2)
