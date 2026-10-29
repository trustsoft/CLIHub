# Implementation Tasks

## Phase 1: Create the Abstraction

- [ ] 1.1 Create `src/CLIHub.Core/Infrastructure/Logging/ILoggingContextProvider.cs`
  - Interface with `IDisposable BeginOperation(string operationName)` method
  - XML documentation required

- [ ] 1.2 Create `src/CLIHub.Core/Infrastructure/Logging/LoggingContextProvider.cs`
  - Inject `ILogger<LoggingContextProvider>` in constructor
  - `BeginOperation` creates dictionary with `OperationName` and `CorrelationId` (8-char hex from `Random.Shared.Next().ToString("x8")`)
  - Return `logger.BeginScope(dictionary)`
  - `ArgumentException.ThrowIfNullOrWhiteSpace(operationName)` validation

- [ ] 1.3 Register in `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs`
  - Add `services.AddSingleton<ILoggingContextProvider, LoggingContextProvider>();` after `services.AddLogging()` in `AddClIHubCoreServices`

- [ ] 1.4 Build gate: `dotnet build CLIHub.sln` must succeed

## Phase 2: Adopt in ProcessOrchestrator

- [ ] 2.1 Update `src/CLIHub.Core/Processes/ProcessOrchestrator.cs`
  - Inject `ILoggingContextProvider loggingContext` in constructor
  - Line 47: Replace `logger.BeginScope` with `loggingContext.BeginOperation("LaunchInteractive")`
  - Line 111: Replace `logger.BeginScope` with `loggingContext.BeginOperation("CaptureOutput")`
  - Line 182: Replace `logger.BeginScope` with `loggingContext.BeginOperation("FindRunningAgents")`
  - Line 194: Replace `logger.BeginScope` with `loggingContext.BeginOperation("FindRunningAgents")`
  - Add `this.logger.LogInformation("Starting operation")` or equivalent after each `BeginOperation` if operation details were in the old scope template

- [ ] 2.2 Update `tests/CLIHub.Core.Tests/Processes/ProcessOrchestratorTests.cs`
  - Add `ILoggingContextProvider` mock (or use a real instance with `NullLogger<LoggingContextProvider>.Instance`)
  - Pass to `ProcessOrchestrator` constructor
  - Verify existing tests pass

- [ ] 2.3 Build gate: `dotnet build CLIHub.sln` must succeed
- [ ] 2.4 Test gate: `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~ProcessOrchestratorTests"` must pass

## Phase 3: Adopt in AgentProcessMonitor

- [ ] 3.1 Update `src/CLIHub.Core/Agents/AgentProcessMonitor.cs`
  - Inject `ILoggingContextProvider loggingContext` in constructor
  - Add `using var scope = this.loggingContext.BeginOperation("EnumerateAgentProcesses");` at start of `GetRunningAgents()` (all-plugins overload, line ~82)
  - Add `using var scope = this.loggingContext.BeginOperation("EnumerateAgentProcesses");` at start of `GetRunningAgents(Plugin plugin, ...)` (single-plugin overload, line ~118)

- [ ] 3.2 Update `tests/CLIHub.Core.Tests/Agents/AgentProcessMonitorTests.cs` (if it exists)
  - Add `ILoggingContextProvider` mock/real instance
  - Pass to `AgentProcessMonitor` constructor
  - Verify existing tests pass

- [ ] 3.3 Build gate: `dotnet build CLIHub.sln` must succeed
- [ ] 3.4 Test gate: `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~AgentProcessMonitorTests"` must pass (if tests exist)

## Phase 4: Adopt in ApplicationOperationLifetime

- [ ] 4.1 Update `src/CLIHub/Lifetime/ApplicationOperationLifetime.cs`
  - Inject `ILoggingContextProvider loggingContext` in constructor
  - Add `using var scope = this.loggingContext.BeginOperation(operationName);` at start of `RunObservedAsync` (line ~91)

- [ ] 4.2 Update `src/CLIHub/Composition/ServiceRegistration.cs` (if needed)
  - Ensure `ILoggingContextProvider` is available in the WPF app's DI container (it should be, since `AddClIHubCoreServices` is called)

- [ ] 4.3 Update tests for `ApplicationOperationLifetime` (if they exist)
  - Add `ILoggingContextProvider` mock/real instance
  - Pass to constructor
  - Verify existing tests pass

- [ ] 4.4 Build gate: `dotnet build CLIHub.sln` must succeed
- [ ] 4.5 Test gate: `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --filter "FullyQualifiedName~ApplicationOperationLifetime"` must pass (if tests exist)

## Phase 5: Adopt in ProjectScanningService

- [ ] 5.1 Inspect `src/CLIHub.Core/Scanning/ProjectScanningService.cs`
  - Locate the scan-operation entry point (`TriggerScanAsync` or `ScanProjectsAsync`)
  - Determine the correct boundary for one scope per scan

- [ ] 5.2 Update `src/CLIHub.Core/Scanning/ProjectScanningService.cs`
  - Inject `ILoggingContextProvider loggingContext` in constructor
  - Add `using var scope = this.loggingContext.BeginOperation("ProjectScan");` at the scan operation boundary

- [ ] 5.3 Update `tests/CLIHub.Core.Tests/Scanning/ProjectScanningServiceTests.cs` (if it exists)
  - Add `ILoggingContextProvider` mock/real instance
  - Pass to constructor
  - Verify existing tests pass

- [ ] 5.4 Build gate: `dotnet build CLIHub.sln` must succeed
- [ ] 5.5 Test gate: `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~ProjectScanningServiceTests"` must pass (if tests exist)

## Phase 6: Add Unit Tests for LoggingContextProvider

- [ ] 6.1 Create `tests/CLIHub.Core.Tests/Infrastructure/Logging/LoggingContextProviderTests.cs`
  - Test: `BeginOperation_CreatesScope_WithOperationNameAndCorrelationId`
    - Create a recording logger that captures `BeginScope` calls
    - Verify scope dictionary contains `OperationName` and `CorrelationId` keys
  - Test: `BeginOperation_MultipleCalls_CreateDistinctCorrelationIds`
    - Call `BeginOperation` twice, verify correlation IDs differ
  - Test: `BeginOperation_Dispose_EndsScope`
    - Verify the returned `IDisposable` ends the scope on `Dispose()`
  - Test: `BeginOperation_NullOrWhiteSpaceOperationName_ThrowsArgumentException`
    - Verify null/empty/whitespace operation names throw

- [ ] 6.2 Build gate: `dotnet build CLIHub.sln` must succeed
- [ ] 6.3 Test gate: `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~LoggingContextProviderTests"` must pass

## Phase 7: Integration Tests (Optional but Recommended)

- [ ] 7.1 Add integration test for ProcessOrchestrator
  - Capture log output from `ILogger<ProcessOrchestrator>`
  - Call `LaunchInteractiveAsync` or `CaptureOutputAsync`
  - Verify scope properties (`OperationName`, `CorrelationId`) appear in captured log context

- [ ] 7.2 Add integration test for AgentProcessMonitor
  - Capture log output
  - Call `GetRunningAgents`
  - Verify operation scope appears

- [ ] 7.3 Test gate: `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj` must pass

## Phase 8: Documentation

- [ ] 8.1 Update `docs/architecture.md`
  - Add `Infrastructure/Logging/` to Core ownership list: "operation-scoped logging context"

- [ ] 8.2 Create `.openspec.yaml` in change directory with `skip_specs: true`
  - Content:
    ```yaml
    skip_specs: true
    skip_specs_reason: "Internal logging enhancement; no spec-pinned behavior changes. Same sink, same messages, adds structured properties (OperationName, CorrelationId) which are allowed per openspec/specs/logging/spec.md."
    ```

## Phase 9: Final Validation

- [ ] 9.1 Build gate: `dotnet build CLIHub.sln` must succeed
- [ ] 9.2 Test gate: `dotnet test CLIHub.sln` must pass (all tests)
- [ ] 9.3 OpenSpec validation: `openspec validate --all` must pass
- [ ] 9.4 Manual smoke (if desired): Run the application, trigger adopted operations (launch a command, scan projects, tracked operation), inspect log file for `OperationName` and `CorrelationId` properties

## Phase 10: Update graphify

- [ ] 10.1 Run `graphify update .` to refresh the graph after source changes
