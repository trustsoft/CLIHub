## 1. Harden Happy-Path Test

- [x] 1.1 Edit `tests/CLIHub.Core.Windows.Tests/WindowsAgentProcessInspectorTests.cs` line 132: change `var inspector = this.CreateInspector();` to `var inspector = this.CreateInspector(Timeout.InfiniteTimeSpan);`, and verify the file compiles
- [x] 1.2 Edit `WindowsAgentProcessInspectorTests.cs` after line 140 (before the join assertion): add `Assert.Equal(1, this.commandLineSource.CallCount);` to confirm the source was called and did not timeout, and verify the file compiles
- [x] 1.3 Run `dotnet build CLIHub.sln` and verify 0 warnings/errors
- [x] 1.4 Run `dotnet test tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj` and verify `GetRunningProcesses_CommandLineDetail_AttachesCommandLineByProcessId` passes
- [x] 1.5 Run the test 10 times in a loop (`for ($i=0; $i -lt 10; $i++) { dotnet test tests/CLIHub.Core.Windows.Tests/CLIHub.Core.Windows.Tests.csproj --filter FullyQualifiedName~GetRunningProcesses_CommandLineDetail_AttachesCommandLineByProcessId }`) and verify it passes every time

## 2. Verification

- [x] 2.1 Run `dotnet test CLIHub.sln` and verify all tests pass
- [x] 2.2 Verify the timeout test (`GetRunningProcesses_SourceExceedsTimeout_Degrades`, line 209) still passes and covers the degradation path with its 200 ms budget
- [x] 2.3 Run `openspec validate --all` and verify 0 errors
- [x] 2.4 Run `graphify update .` to refresh the knowledge graph after the test change
