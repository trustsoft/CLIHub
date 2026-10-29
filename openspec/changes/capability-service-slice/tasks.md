# Implementation Tasks

## Stage 1: Extract capability interfaces and types file

- [ ] 1.1 Create `src/CLIHub.Core/Agents/IAgentProbeService.cs` with ProbeAsync method signature and XML docs copied from IAgentManagementService.cs:58-64
- [ ] 1.2 Create `src/CLIHub.Core/Agents/IAgentUpdateService.cs` with UpdateAsync method signature and XML docs copied from IAgentManagementService.cs:67-73
- [ ] 1.3 Rename `src/CLIHub.Core/Agents/IAgentManagementService.cs` to `AgentManagementTypes.cs`, delete the IAgentManagementService interface (lines 56-74), keep AgentProbeResult (lines 3-10), AgentUpdateOutcome (lines 15-36), AgentUpdateResult (lines 39-51)
- [ ] 1.4 Update namespace usings in files that referenced IAgentManagementService.cs types (AgentManagementService.cs, AgentManagementViewModel.cs, test file) - types now in AgentManagementTypes.cs
- [ ] 1.5 Build: `dotnet build src/CLIHub.Core/CLIHub.Core.csproj`
- [ ] 1.6 Verify types file compiles and exports types correctly

## Stage 2: Update implementation

- [ ] 2.1 Modify `src/CLIHub.Core/Agents/AgentManagementService.cs:11` - change class declaration from `public sealed class AgentManagementService : IAgentManagementService` to `public sealed class AgentManagementService : IAgentProbeService, IAgentUpdateService`
- [ ] 2.2 Update using directives in AgentManagementService.cs to reference IAgentProbeService and IAgentUpdateService
- [ ] 2.3 Verify ProbeAsync (line 45) and UpdateAsync (line 73) method signatures match new interface contracts exactly (return types, parameter names, default values, XML docs)
- [ ] 2.4 Build: `dotnet build src/CLIHub.Core/CLIHub.Core.csproj`
- [ ] 2.5 Run Core tests: `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj`

## Stage 3: Update DI registration

- [ ] 3.1 Modify `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs:118` - delete line `services.AddSingleton<IAgentManagementService, AgentManagementService>();`
- [ ] 3.2 Add three registrations at line 118: `var agentManagement = new AgentManagementService(/* dependencies resolved from sp */);` then `services.AddSingleton<IAgentProbeService>(agentManagement);` then `services.AddSingleton<IAgentUpdateService>(agentManagement);` - wait, that's manual construction, wrong pattern. Use factory lambda instead.
- [ ] 3.3 Correct approach: Add at line 118: `services.AddSingleton<AgentManagementService>();` then `services.AddSingleton<IAgentProbeService>(sp => sp.GetRequiredService<AgentManagementService>());` then `services.AddSingleton<IAgentUpdateService>(sp => sp.GetRequiredService<AgentManagementService>());`
- [ ] 3.4 Build: `dotnet build CLIHub.sln`

## Stage 4: Update production consumer

- [ ] 4.1 Modify `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:26` - replace `private readonly IAgentManagementService management;` with two fields: `private readonly IAgentProbeService probe;` and `private readonly IAgentUpdateService update;`
- [ ] 4.2 Modify constructor signature at line 57 - replace `IAgentManagementService management` parameter with `IAgentProbeService probe, IAgentUpdateService update`
- [ ] 4.3 Update constructor body at lines 63-71 - replace `this.management = management ?? throw new ArgumentNullException(nameof(management));` with probe and update null checks and assignments
- [ ] 4.4 Update call site at line 529 - replace `await this.management.ProbeAsync` with `await this.probe.ProbeAsync`
- [ ] 4.5 Update call site at line 579 - replace `await this.management.ProbeAsync` with `await this.probe.ProbeAsync`
- [ ] 4.6 Update call site at line 760 - replace `await this.management.UpdateAsync` with `await this.update.UpdateAsync`
- [ ] 4.7 Update using directives - add `using CLIHub.Core.Agents;` if not present (for new interfaces)
- [ ] 4.8 Build: `dotnet build src/CLIHub/CLIHub.csproj`

## Stage 5: Update test consumer

- [ ] 5.1 Modify `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs:43` - replace `Mock<IAgentManagementService> management` parameter in CreateViewModel helper with `Mock<IAgentProbeService> probe, Mock<IAgentUpdateService> update`
- [ ] 5.2 Update CreateViewModel body at line 73-82 - replace `management.Object` with `probe.Object, update.Object` in AgentManagementViewModel constructor call
- [ ] 5.3 Update all CreateViewModel call sites (67 total) - replace `new Mock<IAgentManagementService>()` with `new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>()`
- [ ] 5.4 For 19 tests that set up ProbeAsync - update setup calls from `management.Setup` to `probe.Setup`
- [ ] 5.5 For 5 tests that set up UpdateAsync - update setup calls from `management.Setup` to `update.Setup`
- [ ] 5.6 Update using directives - add `using CLIHub.Core.Agents;` if not present
- [ ] 5.7 Build: `dotnet build tests/CLIHub.Tests/CLIHub.Tests.csproj`
- [ ] 5.8 Run ViewModel tests: `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --filter "FullyQualifiedName~AgentManagementViewModelTests"`

## Stage 6: Add contract-preservation tests

- [ ] 6.1 Add test in `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs`: `ProbeAsync_Contract_InvalidatesAndDetectsAndRetrievesVersion` - verifies the exact sequence: InvalidateFor called on detection/version/availability, IsInstalledInSystem called, GetVersionAsync called only when installed returns true
- [ ] 6.2 Add test: `UpdateAsync_Contract_ChecksDeclarationThenProcessesThenLaunches` - verifies the exact sequence: checks plugin.Commands.Update exists, GetRunningAgents called, ExecuteAsync called only when not blocked, InvalidateFor called only on success
- [ ] 6.3 Add test: `UpdateAsync_WhenUnsupported_ReturnsOutcomeWithoutLaunch` - pins the Unsupported outcome when Commands.Update is null (behavioral contract that existed pre-split)
- [ ] 6.4 Add test: `UpdateAsync_WhenBlocked_ReturnsOutcomeWithoutLaunch` - pins the Blocked outcome when running agents found
- [ ] 6.5 Build and run: `dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~AgentManagementServiceTests"`
- [ ] 6.6 Verify all new tests pass (contract preserved across split)

## Stage 7: Full verification

- [ ] 7.1 Build: `dotnet build CLIHub.sln`
- [ ] 7.2 Run all tests: `dotnet test CLIHub.sln`
- [ ] 7.3 Verify zero test failures (all 19 ProbeAsync tests pass, all 5 UpdateAsync tests pass, all unconfigured-mock tests pass)
- [ ] 7.4 Run application: `dotnet run --project src/CLIHub/CLIHub.csproj`
- [ ] 7.5 Smoke test: open Settings -> CLI Agents, click Detect (probe), click Update on an agent with update command (update) - verify both work identically to baseline
- [ ] 7.6 Validate OpenSpec: `openspec validate --all` from repository root
- [ ] 7.7 Verify validation passes with zero errors
- [ ] 7.8 Update graphify: `graphify update .` to refresh knowledge graph with new interface nodes
