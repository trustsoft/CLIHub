# Process Domain Phase 4: Remove ProcessLauncher and Add Orchestrator Tests

## Phase 1: Remove ProcessLauncher Type and DI Registrations

- [ ] 1.1 Delete `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs` and verify `CLIHub.Core` compiles
- [ ] 1.2 Remove line 57 from `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs` (`services.AddSingleton<ProcessLauncher>();`) and verify `CLIHub.Core.Windows` compiles
- [ ] 1.3 Remove lines 27-31 from `tests/CLIHub.Core.Tests/Composition/ServiceCollectionExtensionsTests.cs` (ProcessLauncher test-only workaround) and verify the test file compiles
- [ ] 1.4 Remove line 22 from `tests/CLIHub.Core.Windows.Tests/WindowsServiceCollectionExtensionsTests.cs` (`Assert.NotNull(provider.GetRequiredService<ProcessLauncher>());`) and verify the test file compiles
- [ ] 1.5 Run `dotnet build CLIHub.sln` and verify it succeeds with zero CLIHUB001 warnings

## Phase 2: Rewrite Dependent Test Files

- [ ] 2.1 In `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs` line 137, replace `ProcessLauncher.BuildStartInfo` with `WindowsCommandLineBuilder.BuildInteractive` and verify the test compiles
- [ ] 2.2 In `tests/CLIHub.Core.Windows.Tests/WindowsInteractiveProcessRunnerTests.cs` line 177, replace `ProcessLauncher.BuildStartInfo` with `WindowsCommandLineBuilder.BuildInteractive` and verify the test compiles
- [ ] 2.3 Run `dotnet test tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj --filter "FullyQualifiedName~WindowsInteractiveProcessRunnerTests"` and verify the two rewritten tests pass
- [ ] 2.4 Rewrite `tests/CLIHub.Core.Windows.Tests/ProcessLauncherCaptureOutputTests.cs` to drive `WindowsProcessOutputRunner` directly with `cmd.exe` runtime, rename class to `WindowsProcessOutputRunnerIntegrationTests`, preserve all 5 test scenarios (capture stdout, missing executable, cancellation, timeout, PATHEXT resolution), and verify the file compiles
- [ ] 2.5 Run `dotnet test tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj --filter "FullyQualifiedName~WindowsProcessOutputRunnerIntegrationTests"` and verify all 5 ported tests pass

## Phase 3: Add ProcessOrchestrator Unit Tests

- [ ] 3.1 Create `tests/CLIHub.Core.Tests/Processes/ProcessOrchestratorTests.cs` with test class and constructor wiring mocked dependencies (following `AgentProcessMonitorTests.CreateOrchestrator` pattern), and verify the file compiles
- [ ] 3.2 Add `LaunchInteractiveAsync` tests: success with exit code and duration recorded, runtime unavailable → `ProcessResult.Failure`, runner exception → `Failure` with message, `OperationCanceledException` → `Cancelled`, and verify tests compile and pass
- [ ] 3.3 Add `CaptureOutputAsync` tests: success with trimmed output and exit code, `TimedOut` flag → `ProcessResult.Timeout`, `InvalidOperationException` → `Failure`, cancellation → `Cancelled`, and verify tests compile and pass
- [ ] 3.4 Add `FindRunningAgentsAsync` tests: single-plugin and batched overloads delegate to finder with correct arguments, and verify tests compile and pass
- [ ] 3.5 Add `ResolveRuntimeAsync` test: delegates to resolver and returns result, and verify test compiles and passes
- [ ] 3.6 Run `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~ProcessOrchestratorTests"` and verify all orchestrator tests pass

## Phase 4: Add RuntimeResolver Unit Tests

- [ ] 4.1 Create `tests/CLIHub.Core.Tests/Processes/RuntimeResolverTests.cs` with test class and mocked locator, and verify the file compiles
- [ ] 4.2 Add tests: PowerShell default when locator returns null, CommandPrompt default when locator returns null, GitBash null when not located, PowerShell7 null when not located, returns `RuntimeExecutable` when located, and verify tests compile and pass
- [ ] 4.3 Run `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~RuntimeResolverTests"` and verify all resolver tests pass

## Phase 5: Add CommandLineBuilderAdapter Unit Tests

- [ ] 5.1 Create `tests/CLIHub.Core.Tests/Processes/CommandLineBuilderAdapterTests.cs` with test class, and verify the file compiles
- [ ] 5.2 Add `BuildInteractive` tests for each `RuntimeKind`: PowerShell, CommandPrompt, GitBash (extracts executable), PowerShell7 (extracts executable), verify all delegate to `WindowsCommandLineBuilder.BuildInteractive` with correct arguments, and verify tests compile and pass
- [ ] 5.3 Add `BuildCaptured` test: delegates to `WindowsCommandLineBuilder.BuildCapturedCommand` and returns result, and verify test compiles and passes
- [ ] 5.4 Run `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~CommandLineBuilderAdapterTests"` and verify all adapter tests pass

## Phase 6: Delete Legacy ProcessLauncherTests

- [ ] 6.1 Delete `tests/CLIHub.Core.Tests/Infrastructure/Processes/ProcessLauncherTests.cs` and verify the solution compiles
- [ ] 6.2 Run `dotnet build CLIHub.sln` and verify zero CLIHUB001 warnings (expected: 0 warnings; was 33 before Phase 1)

## Phase 7: Final Verification

- [ ] 7.1 Run `dotnet test CLIHub.sln` and verify all tests pass (expected: ~1240 tests; was ~1223 before)
- [ ] 7.2 Run `dotnet build CLIHub.sln /p:TreatWarningsAsErrors=true` and verify the build succeeds with zero warnings
- [ ] 7.3 Verify CLIHUB001 count: run `dotnet build CLIHub.sln 2>&1 | Select-String "CLIHUB001" | Measure-Object` and confirm `Count: 0`
- [ ] 7.4 Manual smoke test (optional): launch the application via `dotnet run --project src/CLIHub/CLIHub.csproj`, select a project, launch an agent command, and verify the terminal opens correctly
