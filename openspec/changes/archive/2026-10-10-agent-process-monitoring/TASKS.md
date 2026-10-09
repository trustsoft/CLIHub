# Tasks

## Phase 1: Core Process Detection (2-3 hours)

### Task 1.1: Create AgentProcessInstance Model
- [ ] Create `src/CLIHub.Core/Models/AgentProcessInstance.cs`
- [ ] Define record with ProcessId, ExecutablePath, CommandLine, StartTime
- [ ] Add XML documentation

### Task 1.2: Create IAgentProcessInspector Interface
- [ ] Create `src/CLIHub.Core/Infrastructure/Processes/IAgentProcessInspector.cs`
- [ ] Define GetRunningProcesses() method
- [ ] Add XML documentation

### Task 1.3: Add System.Management Package
- [ ] Add `System.Management` NuGet package to `CLIHub.Core.csproj`
- [ ] Version: 8.0.0 or later

### Task 1.4: Implement WindowsAgentProcessInspector
- [ ] Create `src/CLIHub.Core/Infrastructure/Processes/WindowsAgentProcessInspector.cs`
- [ ] Implement GetRunningProcesses() using Process.GetProcesses()
- [ ] Filter out system processes (PID < 100)
- [ ] Extract executable path from MainModule.FileName
- [ ] Extract command line using WMI (Win32_Process)
- [ ] Handle access denied exceptions gracefully
- [ ] Handle process exit race conditions
- [ ] Add logging for warnings

### Task 1.5: Create WindowsAgentProcessInspectorTests
- [ ] Create `tests/CLIHub.Core.Tests/Infrastructure/Processes/WindowsAgentProcessInspectorTests.cs`
- [ ] Test: GetRunningProcesses returns non-empty list
- [ ] Test: Filters out system processes
- [ ] Test: Handles access denied gracefully
- [ ] Test: Returns valid executable paths
- [ ] Test: Handles process exit during enumeration

## Phase 2: Agent Matching Logic (1-2 hours)

### Task 2.1: Create RunningAgent Model
- [ ] Create `src/CLIHub.Core/Models/RunningAgent.cs`
- [ ] Define record with Agent and AgentProcessInstance
- [ ] Add XML documentation

### Task 2.2: Implement AgentProcessMatcher
- [ ] Create `src/CLIHub.Core/Services/AgentProcessMatcher.cs`
- [ ] Implement IsMatch(processInstance, agent)
- [ ] Handle direct executable matching (exact path)
- [ ] Handle runtime-based agent matching (runtime + script in command line)
- [ ] Implement PathEquals helper (case-insensitive on Windows)
- [ ] Add logging for match attempts

### Task 2.3: Create AgentProcessMatcherTests
- [ ] Create `tests/CLIHub.Core.Tests/Services/AgentProcessMatcherTests.cs`
- [ ] Test: Matches direct executable agents
- [ ] Test: Matches Node.js runtime agents
- [ ] Test: Matches Python runtime agents
- [ ] Test: Case-insensitive path comparison
- [ ] Test: No match when paths differ
- [ ] Test: No match when runtime differs
- [ ] Test: No match when script not in command line
- [ ] Test: Handles null/empty command lines

### Task 2.4: Implement AgentProcessMonitor
- [ ] Create `src/CLIHub.Core/Services/AgentProcessMonitor.cs`
- [ ] Inject IAgentProcessInspector, AgentProcessMatcher, IAgentDetectionService
- [ ] Implement HasRunningAgents()
- [ ] Implement GetRunningAgents()
- [ ] Add error handling and logging

### Task 2.5: Create AgentProcessMonitorTests
- [ ] Create `tests/CLIHub.Core.Tests/Services/AgentProcessMonitorTests.cs`
- [ ] Test: HasRunningAgents returns false when no matches
- [ ] Test: HasRunningAgents returns true when agent running
- [ ] Test: GetRunningAgents returns empty when no matches
- [ ] Test: GetRunningAgents returns matched agents
- [ ] Test: Handles inspector exceptions gracefully

## Phase 3: Update Command Integration (1-2 hours)

### Task 3.1: Register Services in DI
- [ ] Edit `src/CLIHub/App.xaml.cs`
- [ ] Register IAgentProcessInspector → WindowsAgentProcessInspector (Singleton)
- [ ] Register AgentProcessMatcher (Singleton)
- [ ] Register AgentProcessMonitor (Singleton)
- [ ] Verify registration order (after IAgentDetectionService)

### Task 3.2: Update UpdateCommand
- [ ] Edit `src/CLIHub/Commands/UpdateCommand.cs`
- [ ] Add AgentProcessMonitor to constructor
- [ ] Update CanExecute() to check HasRunningAgents()
- [ ] Add double-check in ExecuteAsync() before starting update
- [ ] Show MessageBox with running agents list when blocked
- [ ] Raise CanExecuteChanged when monitor state changes

### Task 3.3: Create UpdateCommand Integration Tests
- [ ] Edit `tests/CLIHub.Tests/Commands/UpdateCommandTests.cs`
- [ ] Test: CanExecute returns false when agents running
- [ ] Test: CanExecute returns true when no agents running
- [ ] Test: ExecuteAsync shows blocking message when agents running

### Task 3.4: Manual Verification
- [ ] Run CLIHub
- [ ] Start an agent manually (e.g., launch aider or cline)
- [ ] Check for updates
- [ ] Verify update button is disabled
- [ ] Verify status message shows blocking reason
- [ ] Close agent
- [ ] Verify update button becomes enabled

## Phase 4: Documentation and Cleanup

### Task 4.1: Update improvements.md
- [ ] Mark Phase 14 as complete
- [ ] Add completion date
- [ ] List implemented components
- [ ] Document test results

### Task 4.2: Commit Changes
- [ ] Commit: "feat(update): add agent process monitoring to prevent unsafe updates"
- [ ] Include all new files and tests
- [ ] Verify all 581+ tests pass

## Acceptance Criteria

- [ ] All unit tests pass (20+ new tests)
- [ ] All integration tests pass
- [ ] Manual verification successful
- [ ] UpdateCommand correctly blocks when agents running
- [ ] No false positives (non-agent processes don't block)
- [ ] No false negatives (running agents always detected)
- [ ] Clear user feedback showing which agents are blocking
- [ ] No breaking changes to existing APIs
- [ ] Code follows project style and conventions
