# Tasks: Single Instance Activation Reliability

## 1. Synchronous pipe creation and server loop handoff

- [x] 1.1 Refactor `SingleInstanceGuard` constructor to create the `NamedPipeServerStream` synchronously on the first-instance path, wrap creation in try/catch, and pass the stream (or `null` on failure) to `ServerLoopAsync` via a parameter; verify `dotnet build CLIHub.sln` succeeds
- [x] 1.2 Update `ServerLoopAsync` signature to accept `NamedPipeServerStream? initialServer`, check for `null` on the first iteration and create a new stream if needed, and preserve the existing loop error handler; verify the solution builds
- [x] 1.3 Run existing tests: `dotnet test CLIHub.sln --filter FullyQualifiedName~SingleInstanceGuard` (if any) and `dotnet test CLIHub.sln --filter FullyQualifiedName~ApplicationHost`; verify they pass (the race may still occur, but the change is backward-compatible)

**Gate:** Solution builds, existing tests pass (or show the same intermittent behavior as before).

## 2. Bounded deadline retry in SignalActivation

- [x] 2.1 Replace the two-attempt fixed loop in `SignalActivation()` with a deadline-based retry (~2 s total) on a monotonic clock (`Environment.TickCount64`, not `DateTime.UtcNow`), using exponential backoff starting at 50 ms and capping at 500 ms, and adjust the final `Connect` timeout to not overshoot the deadline; verify the solution builds
- [x] 2.2 Do not launch the GUI from the implementation session; the duplicate-launch check is the user's manual smoke in task 6.3. Cover the bounded behavior with the test in task 4.3 instead.

**Gate:** Solution builds; the bounded-latency behavior is covered by the stage 4 test.

## 3. Gap-minimizing server stream recreation

- [x] 3.1 Refactor the server loop to dispose the current stream *after* reading one message but set `server = null` so the next iteration creates a fresh stream; verify the solution builds and the server remains multi-shot (accepts repeated activations)
- [x] 3.2 Run the existing `ApplicationHostTests.Start_SecondInstance_SignalsActivationAndSkipsEnvironmentSetup` test multiple times (e.g., 10 runs) to verify it no longer times out; record the result

**Gate:** Solution builds, ApplicationHost test passes reliably over 10 runs.

## 4. Test coverage for race window

- [x] 4.1 Extend the existing `tests/CLIHub.Core.Windows.Tests/SingleInstanceGuardTests.cs` with a test that constructs a first-instance guard with a unique mutex/pipe name, immediately calls `SignalActivation()` from a second-instance guard, asserts the `ActivationRequested` event fires within 3 s, and repeats this 10 times over fresh unique names; write it to fail on the pre-fix ordering (signal before the background server task starts) rather than by temporarily reverting the fix
- [x] 4.2 Re-apply the fix and verify the race-window test passes reliably; run `dotnet test CLIHub.sln` to confirm all tests pass
- [x] 4.3 Add a bounded-latency test that calls `SignalActivation()` with no server running and asserts it returns within ~2.5 s without throwing; verify it passes

**Gate:** `dotnet test CLIHub.sln` passes, race-window test passes reliably (10/10 iterations).

## 5. Backlog closure and documentation

- [x] 5.1 Close the backlog entry at `docs/backlog.md:464-480` by marking it as done and referencing this change (`single-instance-activation-reliability`); verify the edit is accurate and does not alter other backlog entries
- [x] 5.2 Review user-facing documentation (`README.md`, `docs/architecture/startup.md`) to determine if any wording about single-instance behavior needs updating; if none, skip; if any, update the relevant sentence to mention reliability (e.g., "the duplicate instance signals the running instance within a bounded retry" instead of "signals the running instance")

**Gate:** Backlog entry closed, documentation reviewed (and updated if needed).

## 6. Final verification

- [x] 6.1 Run the full test suite: `dotnet test CLIHub.sln`; verify all tests pass
- [x] 6.2 Run `openspec validate --all`; verify it passes
- [x] 6.3 Manual end-to-end smoke: launch CLIHub, launch it again (duplicate), verify the launch window shows and activates; close the window, launch again, verify it shows again; record the result
- [x] 6.4 Run `graphify update .` to refresh the knowledge graph after source changes

**Gate:** All tests pass, `openspec validate --all` passes, manual smoke passes, graph updated.
