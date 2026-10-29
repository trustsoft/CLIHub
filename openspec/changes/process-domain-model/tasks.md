# Tasks

## Stage 1: Domain Models (1-2 hours)

Create new `CLIHub.Core/Processes/` namespace with immutable value objects.

### Task 1.1: Create Directory Structure
- [ ] Create `src/CLIHub.Core/Processes/` directory

**Build/Test Gate:** None (directory only)

### Task 1.2: Create RuntimeExecutable Model
- [ ] Create `src/CLIHub.Core/Processes/RuntimeExecutable.cs`
- [ ] Define record with `RuntimeKind Kind`, `string ExecutablePath`, `string? Version`
- [ ] Add `Create` factory method
- [ ] Add XML documentation

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 1.3: Create ProcessResultKind Enum
- [ ] Create `src/CLIHub.Core/Processes/ProcessResultKind.cs`
- [ ] Define enum: `Success`, `Failure`, `Timeout`, `Cancelled`
- [ ] Add XML documentation

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 1.4: Create ProcessResult Model
- [ ] Create `src/CLIHub.Core/Processes/ProcessResult.cs`
- [ ] Define record with `ProcessResultKind Kind`, `int? ExitCode`, `string? Output`, `string? Error`, `TimeSpan? Duration`
- [ ] Add `IsSuccess` computed property
- [ ] Add factory methods: `Success`, `Failure`, `Timeout`, `Cancelled`
- [ ] Add XML documentation

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 1.5: Create AgentProcess Model
- [ ] Create `src/CLIHub.Core/Processes/AgentProcess.cs`
- [ ] Define record with `int ProcessId`, `string CommandLine`, `RuntimeExecutable? Runtime`, `DateTime StartTime`
- [ ] Add `IsRuntimeHosted` computed property (checks `AgentProcessMatcher.RuntimeHostProcessNames`)
- [ ] Add XML documentation noting that `Runtime` is null for runtime-hosted processes

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 1.6: Create LaunchRequest Model
- [ ] Create `src/CLIHub.Core/Processes/LaunchRequest.cs`
- [ ] Define record with `string WorkingDirectory`, `PluginCommand Command`, `RuntimeKind RuntimeKind`, `CancellationToken CancellationToken`
- [ ] Add `Create` factory method
- [ ] Add XML documentation

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 1.7: Create ProcessMatchCriteria Model
- [ ] Create `src/CLIHub.Core/Processes/ProcessMatchCriteria.cs`
- [ ] Define record with `Plugin Plugin`, `AgentProcessMatchDetail Detail`, `TimeSpan? Timeout`
- [ ] Add `Create` factory method
- [ ] Add XML documentation

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
dotnet test CLIHub.sln
```

## Stage 2: Supporting Interfaces (30 minutes)

Define orchestrator's dependency interfaces.

### Task 2.1: Create IRuntimeResolver Interface
- [ ] Create `src/CLIHub.Core/Processes/IRuntimeResolver.cs`
- [ ] Define `Task<RuntimeExecutable?> ResolveAsync(RuntimeKind kind, CancellationToken cancellationToken = default)`
- [ ] Add XML documentation

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 2.2: Create ICommandLineBuilder Interface
- [ ] Create `src/CLIHub.Core/Processes/ICommandLineBuilder.cs`
- [ ] Define `(string FileName, string Arguments) BuildInteractive(RuntimeExecutable, PluginCommand, string workingDirectory)`
- [ ] Define `(string FileName, string Arguments) BuildCaptured(string executable, string? arguments)`
- [ ] Add XML documentation

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 2.3: Create IAgentProcessFinder Interface
- [ ] Create `src/CLIHub.Core/Processes/IAgentProcessFinder.cs`
- [ ] Define `Task<IReadOnlyList<AgentProcess>> FindAsync(ProcessMatchCriteria criteria, CancellationToken cancellationToken = default)`
- [ ] Add XML documentation

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
dotnet test CLIHub.sln
```

## Stage 3: Orchestrator Interface (15 minutes)

Define the unified orchestration contract.

### Task 3.1: Create IProcessOrchestrator Interface
- [ ] Create `src/CLIHub.Core/Processes/IProcessOrchestrator.cs`
- [ ] Define `Task<ProcessResult> LaunchInteractiveAsync(LaunchRequest request)`
- [ ] Define `Task<ProcessResult> CaptureOutputAsync(LaunchRequest request)`
- [ ] Define `Task<IReadOnlyList<AgentProcess>> FindRunningAgentsAsync(ProcessMatchCriteria criteria)`
- [ ] Define `Task<RuntimeExecutable?> ResolveRuntimeAsync(RuntimeKind kind, CancellationToken cancellationToken = default)`
- [ ] Add XML documentation for each method

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
dotnet test CLIHub.sln
```

## Stage 4: Adapter Implementations (1-2 hours)

Create adapters that wrap existing services and translate to new domain types.

### Task 4.1: Implement RuntimeResolver
- [ ] Create `src/CLIHub.Core/Processes/RuntimeResolver.cs`
- [ ] Inject `IRuntimeExecutableLocator`
- [ ] Implement `ResolveAsync`:
  - [ ] Call `locator.Locate(kind)`
  - [ ] Handle `null` for PowerShell/CommandPrompt (use `powershell.exe`/`cmd.exe`)
  - [ ] Return `null` for unresolved runtimes
  - [ ] Map to `RuntimeExecutable` when resolved
- [ ] Mark class `internal sealed`
- [ ] Add XML documentation

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 4.2: Implement CommandLineBuilderAdapter
- [ ] Create `src/CLIHub.Core/Processes/CommandLineBuilderAdapter.cs`
- [ ] Implement `BuildInteractive`:
  - [ ] Extract `gitBashExe` and `ps7Exe` from `RuntimeExecutable`
  - [ ] Call `WindowsCommandLineBuilder.BuildInteractive`
  - [ ] Return tuple
- [ ] Implement `BuildCaptured`:
  - [ ] Call `WindowsCommandLineBuilder.BuildCapturedCommand`
  - [ ] Return tuple
- [ ] Mark class `internal sealed`
- [ ] Add XML documentation

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 4.3: Implement AgentProcessFinder
- [ ] Create `src/CLIHub.Core/Processes/AgentProcessFinder.cs`
- [ ] Inject `IAgentProcessInspector`, `AgentProcessMatcher`, `IPluginCatalog`
- [ ] Implement `FindAsync`:
  - [ ] Get candidate names from matcher
  - [ ] Call inspector with candidates
  - [ ] Filter instances by matcher
  - [ ] Map `AgentProcessInstance` to `AgentProcess` (set `Runtime` to `null` for runtime-hosted)
  - [ ] Return list
- [ ] Mark class `internal sealed`
- [ ] Add XML documentation

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
dotnet test CLIHub.sln
```

## Stage 5: Orchestrator Implementation (2-3 hours)

Implement the main orchestrator that coordinates all adapters.

### Task 5.1: Implement ProcessOrchestrator Shell
- [ ] Create `src/CLIHub.Core/Processes/ProcessOrchestrator.cs`
- [ ] Define class with `public` visibility
- [ ] Add constructor with dependencies:
  - [ ] `IRuntimeResolver runtimeResolver`
  - [ ] `ICommandLineBuilder commandLineBuilder`
  - [ ] `IInteractiveProcessRunner interactiveRunner`
  - [ ] `IProcessOutputRunner outputRunner`
  - [ ] `IAgentProcessFinder processFinder`
  - [ ] `ILogger<ProcessOrchestrator> logger`
- [ ] Add XML documentation
- [ ] Throw `NotImplementedException` in all methods

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 5.2: Implement LaunchInteractiveAsync
- [ ] Resolve runtime with `runtimeResolver.ResolveAsync(request.RuntimeKind, request.CancellationToken)`
- [ ] Return `ProcessResult.Failure` if runtime is null
- [ ] Build command line with `commandLineBuilder.BuildInteractive`
- [ ] Record start time
- [ ] Launch with `interactiveRunner.LaunchAsync`
- [ ] Calculate duration
- [ ] Return `ProcessResult.Success(0, duration: duration)`
- [ ] Handle `OperationCanceledException` → `ProcessResult.Cancelled()`
- [ ] Handle exceptions → `ProcessResult.Failure(ex.Message)` and log error
- [ ] Add scoped logging

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 5.3: Implement CaptureOutputAsync
- [ ] Resolve runtime with `runtimeResolver.ResolveAsync(request.RuntimeKind, request.CancellationToken)`
- [ ] Return `ProcessResult.Failure` if runtime is null
- [ ] Build command line with `commandLineBuilder.BuildCaptured(request.Command.Executable, request.Command.Arguments)`
- [ ] Record start time
- [ ] Capture with `outputRunner.CaptureAsync`
- [ ] Calculate duration
- [ ] Map `ProcessCaptureResult` to `ProcessResult`:
  - [ ] If `!Started` → `ProcessResult.Failure`
  - [ ] If `Started` → `ProcessResult.Success(exitCode, output: StdOut, duration)`
  - [ ] Trim output like `ProcessCaptureResult` does
- [ ] Handle `OperationCanceledException` → `ProcessResult.Cancelled()`
- [ ] Handle exceptions → `ProcessResult.Failure(ex.Message)` and log error
- [ ] Add scoped logging

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 5.4: Implement FindRunningAgentsAsync
- [ ] Call `processFinder.FindAsync(criteria, CancellationToken.None)`
- [ ] Return result
- [ ] Handle exceptions → log error and return `Array.Empty<AgentProcess>()`
- [ ] Add scoped logging

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 5.5: Implement ResolveRuntimeAsync
- [ ] Call `runtimeResolver.ResolveAsync(kind, cancellationToken)`
- [ ] Return result directly

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
dotnet test CLIHub.sln
```

## Stage 6: DI Registration (30 minutes)

Register orchestrator and adapters in dependency injection.

### Task 6.1: Create ProcessesServiceRegistration
- [ ] Create `src/CLIHub/DependencyInjection/ProcessesServiceRegistration.cs`
- [ ] Define `internal static class ProcessesServiceRegistration`
- [ ] Add `AddProcessOrchestrator` extension method on `IServiceCollection`
- [ ] Register `IRuntimeResolver` → `RuntimeResolver` (Singleton)
- [ ] Register `ICommandLineBuilder` → `CommandLineBuilderAdapter` (Singleton)
- [ ] Register `IAgentProcessFinder` → `AgentProcessFinder` (Singleton)
- [ ] Register `IProcessOrchestrator` → `ProcessOrchestrator` (Singleton)
- [ ] Add XML documentation

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub/CLIHub.csproj
```

### Task 6.2: Wire Up Registration
- [ ] Edit `src/CLIHub/App.xaml.cs` (or existing DI registration file)
- [ ] Call `services.AddProcessOrchestrator()` after existing process services registered
- [ ] Verify registration order (after `IAgentProcessInspector`, `AgentProcessMatcher`)

**Build/Test Gate:**
```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
```

## Stage 7: Unit Tests (2-3 hours)

Write comprehensive tests for orchestrator and adapters.

### Task 7.1: Create Test Directory Structure
- [ ] Create `tests/CLIHub.Tests/Core/Processes/` directory

**Build/Test Gate:** None (directory only)

### Task 7.2: Create RuntimeResolverTests
- [ ] Create `tests/CLIHub.Tests/Core/Processes/RuntimeResolverTests.cs`
- [ ] Test: `ResolveAsync_PowerShell_ReturnsExecutable`
- [ ] Test: `ResolveAsync_CommandPrompt_ReturnsExecutable`
- [ ] Test: `ResolveAsync_GitBash_WhenLocated_ReturnsExecutable`
- [ ] Test: `ResolveAsync_GitBash_WhenNotLocated_ReturnsNull`
- [ ] Test: `ResolveAsync_PowerShell7_WhenLocated_ReturnsExecutable`
- [ ] Test: `ResolveAsync_PowerShell7_WhenNotLocated_ReturnsNull`

**Build/Test Gate:**
```powershell
dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --filter "FullyQualifiedName~RuntimeResolverTests"
```

### Task 7.3: Create CommandLineBuilderAdapterTests
- [ ] Create `tests/CLIHub.Tests/Core/Processes/CommandLineBuilderAdapterTests.cs`
- [ ] Test: `BuildInteractive_PowerShell_CallsBuilderWithCorrectParams`
- [ ] Test: `BuildInteractive_GitBash_PassesResolvedExecutable`
- [ ] Test: `BuildInteractive_PowerShell7_PassesResolvedExecutable`
- [ ] Test: `BuildCaptured_CallsBuilderCorrectly`

**Build/Test Gate:**
```powershell
dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --filter "FullyQualifiedName~CommandLineBuilderAdapterTests"
```

### Task 7.4: Create AgentProcessFinderTests
- [ ] Create `tests/CLIHub.Tests/Core/Processes/AgentProcessFinderTests.cs`
- [ ] Test: `FindAsync_NoMatches_ReturnsEmptyList`
- [ ] Test: `FindAsync_WithMatches_ReturnsAgentProcesses`
- [ ] Test: `FindAsync_RuntimeHostedProcess_SetsRuntimeToNull`
- [ ] Test: `FindAsync_InspectorThrows_PropagatesException`

**Build/Test Gate:**
```powershell
dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --filter "FullyQualifiedName~AgentProcessFinderTests"
```

### Task 7.5: Create ProcessOrchestratorTests - LaunchInteractive
- [ ] Create `tests/CLIHub.Tests/Core/Processes/ProcessOrchestratorTests.cs`
- [ ] Test: `LaunchInteractiveAsync_ValidRequest_ReturnsSuccess`
- [ ] Test: `LaunchInteractiveAsync_RuntimeNotAvailable_ReturnsFailure`
- [ ] Test: `LaunchInteractiveAsync_RunnerThrows_ReturnsFailure`
- [ ] Test: `LaunchInteractiveAsync_Cancelled_ReturnsCancelled`
- [ ] Test: `LaunchInteractiveAsync_CallsRunnerWithCorrectParams`

**Build/Test Gate:**
```powershell
dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --filter "FullyQualifiedName~ProcessOrchestratorTests"
```

### Task 7.6: Create ProcessOrchestratorTests - CaptureOutput
- [ ] Test: `CaptureOutputAsync_ValidRequest_ReturnsSuccess`
- [ ] Test: `CaptureOutputAsync_RuntimeNotAvailable_ReturnsFailure`
- [ ] Test: `CaptureOutputAsync_ProcessDidNotStart_ReturnsFailure`
- [ ] Test: `CaptureOutputAsync_Cancelled_ReturnsCancelled`
- [ ] Test: `CaptureOutputAsync_MapsOutputCorrectly`

**Build/Test Gate:**
```powershell
dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --filter "FullyQualifiedName~ProcessOrchestratorTests"
```

### Task 7.7: Create ProcessOrchestratorTests - FindRunningAgents
- [ ] Test: `FindRunningAgentsAsync_ValidCriteria_ReturnsProcesses`
- [ ] Test: `FindRunningAgentsAsync_NoMatches_ReturnsEmptyList`
- [ ] Test: `FindRunningAgentsAsync_FinderThrows_ReturnsEmptyAndLogsError`

**Build/Test Gate:**
```powershell
dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --filter "FullyQualifiedName~ProcessOrchestratorTests"
```

### Task 7.8: Create ProcessOrchestratorTests - ResolveRuntime
- [ ] Test: `ResolveRuntimeAsync_ValidKind_ReturnsRuntime`
- [ ] Test: `ResolveRuntimeAsync_InvalidKind_ReturnsNull`
- [ ] Test: `ResolveRuntimeAsync_PropagatesCancellation`

**Build/Test Gate:**
```powershell
dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --filter "FullyQualifiedName~ProcessOrchestratorTests"
dotnet test CLIHub.sln
```

## Stage 8: Final Verification (15 minutes)

Ensure all tests pass and code compiles.

### Task 8.1: Run Full Build
- [ ] Run `dotnet build CLIHub.sln`
- [ ] Verify zero warnings
- [ ] Verify zero errors

**Build/Test Gate:**
```powershell
dotnet build CLIHub.sln
```

### Task 8.2: Run Full Test Suite
- [ ] Run `dotnet test CLIHub.sln`
- [ ] Verify all existing tests still pass
- [ ] Verify all new tests pass

**Build/Test Gate:**
```powershell
dotnet test CLIHub.sln
```

### Task 8.3: Validate OpenSpec
- [ ] Run `openspec validate --all`
- [ ] Verify validation passes

**Build/Test Gate:**
```powershell
openspec validate --all
```

### Task 8.4: Update Graphify
- [ ] Run `graphify update .`
- [ ] Verify graph refresh completes without errors

**Build/Test Gate:**
```powershell
graphify update .
```

## Completion Checklist

- [ ] All stages completed
- [ ] All tests passing (`dotnet test CLIHub.sln`)
- [ ] Zero build warnings (`dotnet build CLIHub.sln`)
- [ ] OpenSpec validation passes (`openspec validate --all`)
- [ ] Graphify updated (`graphify update .`)
- [ ] No breaking changes to existing consumers
- [ ] All new types are in `CLIHub.Core/Processes/` namespace
- [ ] DI registration wired correctly
- [ ] XML documentation on all public types and members
