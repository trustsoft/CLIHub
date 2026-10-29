# Tasks

## Stage 1: Migrate AgentCommandService (2-3 hours)

Update the main agent command executor to use the orchestrator.

### Task 1.1: Update AgentCommandService Constructor
- [ ] Edit `src/CLIHub.Core/Agents/AgentCommandService.cs:11-23`
- [ ] Replace `ProcessLauncher processLauncher` with `IProcessOrchestrator orchestrator`
- [ ] Add `IPreferencesStore preferencesStore` parameter
- [ ] Update field declarations
- [ ] Update constructor assignments

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 1.2: Update ExecuteAsync Method
- [ ] Edit `src/CLIHub.Core/Agents/AgentCommandService.cs:26-54`
- [ ] Add line to read runtime: `var runtime = this.preferencesStore.Load().DefaultRuntime;`
- [ ] For version command:
  - [ ] Create `LaunchRequest`: `var request = LaunchRequest.Create(projectPath, command, runtime, cancellationToken);`
  - [ ] Call `var result = await this.orchestrator.CaptureOutputAsync(request);`
  - [ ] Map `ProcessResult` to `AgentCommandResult`:
    - [ ] `result.IsSuccess && result.ExitCode == 0` → `new AgentCommandResult(true, result.Output, null)`
    - [ ] Otherwise → `new AgentCommandResult(false, result.Output, result.Error ?? "Failed to get version")`
- [ ] For interactive commands:
  - [ ] Create `LaunchRequest`: `var request = LaunchRequest.Create(projectPath, command, runtime, cancellationToken);`
  - [ ] Call `var result = await this.orchestrator.LaunchInteractiveAsync(request);`
  - [ ] Map: `result.IsSuccess` → `new AgentCommandResult(true, null, null)`
  - [ ] Map failure: → `new AgentCommandResult(false, null, result.Error ?? $"Failed to launch {plugin.Name}")`
- [ ] Remove `RunVersionAsync` method (inline into `ExecuteAsync` version branch)

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 1.3: Update AgentCommandServiceTests
- [ ] Edit `tests/CLIHub.Core.Tests/Agents/AgentCommandServiceTests.cs`
- [ ] Replace `ProcessLauncher` + `FakeInteractiveProcessRunner` + `FakeProcessOutputRunner` setup with `Mock<IProcessOrchestrator>`
- [ ] Add `Mock<IPreferencesStore>` returning `DefaultRuntime = RuntimeKind.PowerShell`
- [ ] Update test setup:
  - [ ] Mock `orchestrator.LaunchInteractiveAsync` to return `ProcessResult.Success(0)`
  - [ ] Mock `orchestrator.CaptureOutputAsync` to return `ProcessResult.Success(0, output: "1.2.3")`
- [ ] Verify all tests pass
- [ ] Update assertions to expect orchestrator calls instead of runner calls

**Build/Test Gate:**
```powershell
dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~AgentCommandServiceTests"
```

## Stage 2: Migrate AgentVersionService (1-2 hours)

Update version retrieval service to use the orchestrator.

### Task 2.1: Update AgentVersionService Constructor
- [ ] Edit `src/CLIHub.Core/Agents/AgentVersionService.cs:16-48`
- [ ] Replace `ProcessLauncher processLauncher` with `IProcessOrchestrator orchestrator`
- [ ] Keep `IPreferencesStore preferencesStore` (already exists)
- [ ] Update field declaration
- [ ] Update constructor assignment

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 2.2: Update ProbeAndCacheAsync Method
- [ ] Edit `src/CLIHub.Core/Agents/AgentVersionService.cs:79-104`
- [ ] Add line to read runtime: `var runtime = this.preferencesStore.Load().DefaultRuntime;`
- [ ] Create `LaunchRequest`: `var request = LaunchRequest.Create(workingDirectory, command, runtime, CancellationToken.None);`
- [ ] Call `var result = await this.orchestrator.CaptureOutputAsync(request);`
- [ ] Update condition: `if (result.IsSuccess && result.ExitCode == 0)`
- [ ] Extract version from `result.Output` instead of `result.StdOut`
- [ ] Update error log: use `result.Error` instead of `result.StdErr`

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 2.3: Update AgentVersionServiceTests
- [ ] Edit `tests/CLIHub.Core.Tests/Agents/AgentVersionServiceTests.cs`
- [ ] Replace `ProcessLauncher` + `FakeProcessOutputRunner` setup with `Mock<IProcessOrchestrator>`
- [ ] Mock `orchestrator.CaptureOutputAsync` to return `ProcessResult.Success(0, output: "1.2.3")`
- [ ] Verify all tests pass

**Build/Test Gate:**
```powershell
dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~AgentVersionServiceTests"
```

## Stage 3: Migrate ProcessRuntimePreferenceTarget (30 minutes)

Update runtime preference adapter to write directly to preferences store.

### Task 3.1: Update ProcessRuntimePreferenceTarget Constructor
- [ ] Edit `src/CLIHub/Settings/ProcessRuntimePreferenceTarget.cs:9-24`
- [ ] Replace `ProcessLauncher processLauncher` with `IPreferencesStore preferencesStore`
- [ ] Update field declaration
- [ ] Update constructor assignment and null check

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub/CLIHub.csproj
```

### Task 3.2: Update SetRuntime Method
- [ ] Edit `src/CLIHub/Settings/ProcessRuntimePreferenceTarget.cs:23`
- [ ] Replace implementation:
  ```csharp
  public void SetRuntime(RuntimeKind runtime)
  {
      var preferences = this.preferencesStore.Load();
      preferences.DefaultRuntime = runtime;
      this.preferencesStore.Save(preferences);
  }
  ```

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub/CLIHub.csproj
```

### Task 3.3: Update ProcessRuntimePreferenceTargetTests
- [ ] Find or create `tests/CLIHub.Tests/Settings/ProcessRuntimePreferenceTargetTests.cs`
- [ ] Replace `ProcessLauncher` mock with `Mock<IPreferencesStore>`
- [ ] Mock `Load()` to return `AppPreferences { DefaultRuntime = RuntimeKind.PowerShell }`
- [ ] Verify `Save` is called with updated `DefaultRuntime`
- [ ] Verify all tests pass

**Build/Test Gate:**
```powershell
dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --filter "FullyQualifiedName~ProcessRuntimePreferenceTargetTests"
```

## Stage 4: Migrate StartupPreferencesApplier (30 minutes)

Update startup preferences applier to read from preferences store.

### Task 4.1: Update StartupPreferencesApplier Constructor
- [ ] Edit `src/CLIHub/Startup/StartupPreferencesApplier.cs:10-26`
- [ ] Keep `ProcessLauncher` parameter but rename to `_` (unused) OR remove entirely
- [ ] Add `IPreferencesStore preferencesStore` parameter if not present
- [ ] Update field declaration
- [ ] Update constructor assignments

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub/CLIHub.csproj
```

### Task 4.2: Update Apply Method
- [ ] Edit `src/CLIHub/Startup/StartupPreferencesApplier.cs:29-35`
- [ ] Remove `this.processLauncher.SetRuntime(preferences.DefaultRuntime);` line
- [ ] Keep `this.startupService.SetEnabled(preferences.StartWithWindows);` line
- [ ] Add XML doc comment noting that runtime preference is applied by orchestrator consumers on-demand

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub/CLIHub.csproj
```

### Task 4.3: Update StartupPreferencesApplierTests
- [ ] Find or create `tests/CLIHub.Tests/Startup/StartupPreferencesApplierTests.cs`
- [ ] Remove `ProcessLauncher` mock
- [ ] Verify tests no longer assert `SetRuntime` call
- [ ] Verify `StartupService.SetEnabled` is still tested
- [ ] Verify all tests pass

**Build/Test Gate:**
```powershell
dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj --filter "FullyQualifiedName~StartupPreferencesApplierTests"
```

## Stage 5: Migrate AgentProcessMonitor (2-3 hours)

Update process monitor to use orchestrator's FindRunningAgentsAsync.

### Task 5.1: Update AgentProcessMonitor Constructor
- [ ] Edit `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:12-35`
- [ ] Replace `IAgentProcessInspector processInspector` + `AgentProcessMatcher processMatcher` with `IProcessOrchestrator orchestrator`
- [ ] Keep `IPluginCatalog pluginCatalog` and `ILogger<AgentProcessMonitor> logger`
- [ ] Update field declarations
- [ ] Update constructor assignments

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 5.2: Add AgentProcess → AgentProcessInstance Mapper
- [ ] Add private static method in `src/CLIHub.Core/Agents/AgentProcessMonitor.cs`:
  ```csharp
  private static AgentProcessInstance MapToInstance(AgentProcess process)
      => new()
      {
          ProcessId = process.ProcessId,
          CommandLine = process.CommandLine,
          ExecutablePath = process.Runtime?.ExecutablePath ?? string.Empty,
          StartTime = process.StartTime
      };
  ```

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 5.3: Update GetRunningAgents() Method (All Plugins)
- [ ] Edit `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:78-107`
- [ ] Replace implementation:
  ```csharp
  public IReadOnlyList<RunningAgent> GetRunningAgents(
      AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine)
  {
      var runningAgents = new List<RunningAgent>();
  
      try
      {
          var plugins = this.pluginCatalog.GetAllPlugins();
  
          foreach (var plugin in plugins)
          {
              var criteria = ProcessMatchCriteria.Create(plugin, detail);
              var processes = this.orchestrator.FindRunningAgentsAsync(criteria)
                  .GetAwaiter().GetResult();
  
              foreach (var process in processes)
              {
                  runningAgents.Add(new RunningAgent(plugin, MapToInstance(process)));
              }
          }
      }
      catch (Exception ex)
      {
          this.logger.LogError(ex, "Failed to check for running agents");
      }
  
      return runningAgents;
  }
  ```

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 5.4: Update GetRunningAgents(Plugin) Method
- [ ] Edit `src/CLIHub.Core/Agents/AgentProcessMonitor.cs:120-129`
- [ ] Replace implementation:
  ```csharp
  public IReadOnlyList<RunningAgent> GetRunningAgents(
      Plugin plugin,
      AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine)
  {
      ArgumentNullException.ThrowIfNull(plugin);
  
      try
      {
          var criteria = ProcessMatchCriteria.Create(plugin, detail);
          var processes = this.orchestrator.FindRunningAgentsAsync(criteria)
              .GetAwaiter().GetResult();
  
          return processes
              .Select(p => new RunningAgent(plugin, MapToInstance(p)))
              .ToList();
      }
      catch (Exception ex)
      {
          this.logger.LogError(ex, "Failed to check for running agents for plugin {PluginId}", plugin.Id);
          return Array.Empty<RunningAgent>();
      }
  }
  ```

**Build/Test Gate:**
```powershell
dotnet build src/CLIHub.Core/CLIHub.Core.csproj
```

### Task 5.5: Update AgentProcessMonitorTests
- [ ] Edit `tests/CLIHub.Core.Tests/Agents/AgentProcessMonitorTests.cs`
- [ ] Replace `IAgentProcessInspector` + `AgentProcessMatcher` mocks with `Mock<IProcessOrchestrator>`
- [ ] Mock `orchestrator.FindRunningAgentsAsync` to return:
  ```csharp
  new List<AgentProcess>
  {
      new()
      {
          ProcessId = 1234,
          CommandLine = "node agent.js",
          Runtime = RuntimeExecutable.Create(RuntimeKind.Node, "node.exe"),
          StartTime = DateTime.UtcNow
      }
  }
  ```
- [ ] Verify all tests pass
- [ ] Update assertions to expect orchestrator calls

**Build/Test Gate:**
```powershell
dotnet test tests/CLIHub.Core.Tests/CLIHub.Core.Tests.csproj --filter "FullyQualifiedName~AgentProcessMonitorTests"
```

## Stage 6: Mark ProcessLauncher Obsolete (15 minutes)

Signal that ProcessLauncher will be removed in Phase 4.

### Task 6.1: Add Obsolete Attribute
- [ ] Edit `src/CLIHub.Core/Infrastructure/Processes/ProcessLauncher.cs:14`
- [ ] Add attribute above class declaration:
  ```csharp
  [Obsolete("Use IProcessOrchestrator. Will be removed in Phase 4.")]
  public sealed class ProcessLauncher
  ```

**Build/Test Gate:**
```powershell
dotnet build CLIHub.sln
```

### Task 6.2: Verify No New Warnings
- [ ] Check build output for CS0618 warnings
- [ ] Confirm only expected obsolete usages remain (DI registration, tests)
- [ ] Document any remaining usages in design.md "Open Questions"

**Build/Test Gate:**
```powershell
dotnet build CLIHub.sln
```

## Stage 7: Final Verification (30 minutes)

Ensure all tests pass and behavior is preserved.

### Task 7.1: Run Full Build
- [ ] Run `dotnet build CLIHub.sln`
- [ ] Verify zero errors
- [ ] Document any new obsolete warnings

**Build/Test Gate:**
```powershell
dotnet build CLIHub.sln
```

### Task 7.2: Run Full Test Suite
- [ ] Run `dotnet test CLIHub.sln`
- [ ] Verify all 1,221+ tests pass
- [ ] Investigate any failures

**Build/Test Gate:**
```powershell
dotnet test CLIHub.sln
```

### Task 7.3: Validate OpenSpec
- [ ] Run `openspec validate --all`
- [ ] Verify validation passes

**Build/Test Gate:**
```powershell
openspec validate --all
```

### Task 7.4: Update Graphify
- [ ] Run `graphify update .`
- [ ] Verify graph refresh completes without errors

**Build/Test Gate:**
```powershell
graphify update .
```

## Completion Checklist

- [ ] All 5 consumers migrated to `IProcessOrchestrator`
- [ ] All 5 test files updated
- [ ] `ProcessLauncher` marked as `[Obsolete]`
- [ ] All tests passing (`dotnet test CLIHub.sln`)
- [ ] Zero build errors (`dotnet build CLIHub.sln`)
- [ ] OpenSpec validation passes (`openspec validate --all`)
- [ ] Graphify updated (`graphify update .`)
- [ ] Exact behavior preserved (no functional changes)
- [ ] Runtime preference flow verified (Settings UI → Store → Orchestrator)
- [ ] Process monitoring verified (update gating still works)
