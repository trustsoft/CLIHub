## Context

The v1.1.3 Release workflow (run `42979752090`, 2026-11-11) failed `WindowsAgentProcessInspectorTests.GetRunningProcesses_CommandLineDetail_AttachesCommandLineByProcessId` with `Assert.Equal` expecting the injected `node.exe ...` command line, actual null (backlog lines 189-208). The inspector's bulk command-line query runs through `Task.Run` + `Wait(budget)` and degrades to null command lines when the budget expires (`WindowsAgentProcessInspector.cs:170-200`). On the loaded runner (three test assemblies in parallel right after the Release build) the stub query exceeded the default 2-second budget (`WindowsAgentProcessInspector.cs:26`), the degradation path fired, and the happy-path join assertion saw null.

## Goals / Non-Goals

**Goals:**
- Eliminate the test's wall-clock dependency without weakening coverage
- Preserve the 200 ms timeout test's degradation coverage
- Keep the change test-only (no product edits unless a seam genuinely requires it)

**Non-Goals:**
- Changing the inspector's default 2-second timeout (production-calibrated; out of scope)
- Hardening every test (only the flake-prone happy-path test needs it)
- Refactoring the degradation mechanism (it works; the test is the problem)

## Current State

### WindowsAgentProcessInspector timeout seam (verified)

**File:** `src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs`

- **Line 26:** `private static readonly TimeSpan DefaultCommandLineTimeout = TimeSpan.FromSeconds(2);`
- **Line 30:** `private readonly TimeSpan commandLineTimeout;`
- **Lines 40-48:** Constructor accepts `TimeSpan? commandLineTimeout = null` and assigns `this.commandLineTimeout = commandLineTimeout ?? DefaultCommandLineTimeout`
- **Line 47:** The timeout is stored: `this.commandLineTimeout = commandLineTimeout ?? DefaultCommandLineTimeout;`
- **Lines 170-200:** `TryGetCommandLines` runs `queryTask.Wait(this.commandLineTimeout)` and degrades on timeout (line 185-197)

**Conclusion:** The timeout is already injectable via the constructor's optional parameter. **No product code change required.**

### Happy-path test (verified)

**File:** `tests/CLIHub.Core.Windows.Tests/WindowsAgentProcessInspectorTests.cs`

- **Line 25:** `CreateInspector(TimeSpan? commandLineTimeout = null)` factory forwards the timeout to the inspector constructor
- **Lines 130-148:** `GetRunningProcesses_CommandLineDetail_AttachesCommandLineByProcessId` calls `CreateInspector()` with no timeout argument (line 132), so it defaults to 2 seconds
- **Lines 136-137:** The test injects a command line into `this.commandLineSource.CommandLines[taggedProcessId]`
- **Lines 143-147:** The test asserts the tagged PID carries the injected command line and every other process carries null
- **Flake mechanism:** On a loaded runner, `FakeProcessCommandLineSource.GetCommandLines` (line 22 waits on `Block`, which is null in this test) completes slowly enough that `Wait(2 seconds)` expires, degrading to null command lines before the assertion

### Timeout test (verified)

**File:** `tests/CLIHub.Core.Windows.Tests/WindowsAgentProcessInspectorTests.cs`

- **Lines 209-234:** `GetRunningProcesses_SourceExceedsTimeout_Degrades` injects 200 ms timeout (line 211), blocks the fake source with `ManualResetEventSlim` (line 213), asserts the call returns within 5 seconds (line 231), and asserts all command lines are null (line 233)
- **Coverage:** This test covers the degradation path with a controlled timeout and a guaranteed block; it is unaffected by the happy-path change

### FakeProcessCommandLineSource (verified)

**File:** `tests/CLIHub.Core.Windows.Tests/FakeProcessCommandLineSource.cs`

- **Lines 3-24:** Exposes `CallCount` (incremented on line 15), `CommandLines` dictionary (returned on line 23), `Throw` exception (thrown on line 19), and `Block` gate (waited on line 22)
- **Happy-path use:** The test sets `CommandLines[taggedProcessId]` but does not set `Block`, so `GetCommandLines` returns immediately with the dictionary
- **Timeout-test use:** The test sets `Block` to a closed gate, so `GetCommandLines` waits indefinitely

## Decisions

### Decision 1: Inject unbounded timeout for the happy-path test

**Chosen:** Change line 132 of `WindowsAgentProcessInspectorTests.cs` from `var inspector = this.CreateInspector();` to `var inspector = this.CreateInspector(Timeout.InfiniteTimeSpan);`.

**Rationale:**
- The timeout is already injectable via the constructor; no product code change required
- `Timeout.InfiniteTimeSpan` makes the test immune to runner load spikes
- The timeout test (line 211) continues covering the degradation path with 200 ms
- The fake source returns immediately in the happy-path test (no `Block` set), so the unbounded budget has no runtime cost

**Evidence:** `WindowsAgentProcessInspector.cs:40-48` shows the constructor accepts `TimeSpan? commandLineTimeout = null`.

**Alternatives considered:**
- Injecting a large finite timeout (e.g. 30 seconds): Rejected — still a wall-clock dependency; `InfiniteTimeSpan` is the explicit "no limit" sentinel
- Adding a new test-only inspector property to skip the timeout: Rejected — pollutes production code for a test concern; the constructor parameter is the existing seam
- Refactoring the inspector to use cancellation tokens: Rejected — major change for a test flake; synchronous WMI has no reliable cancellation anyway (comment line 22-25)

### Decision 2: Assert the source was called before the join assertion

**Chosen:** Add `Assert.Equal(1, this.commandLineSource.CallCount);` after line 140 (before the join assertion on line 143).

**Rationale:**
- If the timeout somehow fires despite `InfiniteTimeSpan`, the degradation path skips the source call and the assertion catches it
- Documents the test's expectation: the source must complete
- Single-line addition; no behavioral change
- The `CallCount` property already exists (line 7 of `FakeProcessCommandLineSource.cs`)

**Alternatives considered:**
- Asserting `Block` was not set: Rejected — `Block` is null by default; asserting on absence is redundant
- Adding a `DidNotTimeout` flag to the fake: Rejected — overkill; `CallCount` already captures whether the source completed before degradation

## Risks / Trade-offs

**[Risk] Unbounded timeout masks a real hang** → Mitigation: The test uses a fake source that returns immediately (no `Block` set); a hang would require a test bug, not a product bug. The timeout test (line 209-234) continues covering the degradation path with a real block and a tight budget.

**[Risk] The flake reappears if the fake source changes** → Mitigation: The `CallCount` assertion (Decision 2) catches degradation before the join assertion; if the fake blocks, the test hangs and CI times out the test suite (fail-fast).

**[Trade-off] The happy-path test no longer exercises the timeout path** → Accepted: The timeout test (line 209-234) is the dedicated timeout test; the happy-path test is the join correctness test. Separating concerns is standard practice.

## Migration Plan

1. Edit `WindowsAgentProcessInspectorTests.cs` line 132: inject `Timeout.InfiniteTimeSpan`
2. Edit `WindowsAgentProcessInspectorTests.cs` after line 140: assert `CallCount == 1`
3. Run `dotnet test` and verify the test passes
4. Run the test 10 times in a loop to confirm stability
5. Commit with message "test: eliminate release-gate commandline test flake"

No rollback plan needed; reverting the commit restores the original test.

## Open Questions

- **Should we apply the unbounded timeout to other command-line tests?** No — the other tests either use `ExecutableName` detail (no command-line query) or explicitly test degradation with a tight budget. Only the happy-path test combines command-line detail with a join assertion.
- **Should we increase the default 2-second timeout?** No — that is a production timeout calibrated for normal machines; changing it is a separate product decision and out of scope for a test-only fix.
