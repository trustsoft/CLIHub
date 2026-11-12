## Why

The v1.1.3 Release workflow failed its test job (`WindowsAgentProcessInspectorTests.GetRunningProcesses_CommandLineDetail_AttachesCommandLineByProcessId`) when the loaded CI runner starved the 2-second command-line retrieval budget, triggering the inspector's degradation path and causing the happy-path join assertion to fail on null command lines. The test is green locally and passed five previous release runs, but remains vulnerable to CI load spikes. The root cause is the happy-path test's wall-clock dependency: the test expects a completed command-line query but does not control the budget.

## What Changes

Make the happy-path test independent of the wall clock by injecting an effectively unbounded timeout (`Timeout.InfiniteTimeSpan`) for `GetRunningProcesses_CommandLineDetail_AttachesCommandLineByProcessId` only. The existing 200 ms timeout test (`GetRunningProcesses_SourceExceedsTimeout_Degrades`, line 209) continues covering the degradation path with its tight budget. Optionally, assert the fake source was called and did not block before the join assertion, so a timeout cannot masquerade as a join bug.

Test-only change; no product code altered.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None — this is a test-only change with no behavior modification.

## Impact

**Modified tests:**
- `tests/CLIHub.Core.Windows.Tests/WindowsAgentProcessInspectorTests.cs` — inject unbounded timeout for the happy-path test

**Affected source:**
- `src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs` (inspector that accepts the timeout; cited for evidence; not edited)
- `tests/CLIHub.Core.Windows.Tests/FakeProcessCommandLineSource.cs` (fake; cited for evidence; optionally extended if asserting call count)

**No breaking changes.**

## Non-Goals

- Changing the inspector's default 2-second budget (it is calibrated for production; changing it is out of scope)
- Adding timeout injection to every test (only the flake-prone happy-path test needs it)
- Changing the degradation path (it is already covered by a dedicated test and works as designed)

## Dependencies

None. This is a standalone test-hardening change.
