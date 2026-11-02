# Single Instance Activation Reliability

## Why

On a cold or loaded machine, the duplicate instance's `SignalActivation()` can complete its two fixed connection attempts before the first instance's named-pipe server begins listening. This scheduling race causes the second launch to exit silently without bringing the window forward — a real user-facing failure, not just a CI flake. The backlog entry "ApplicationHost second-instance activation test times out on CI" (`docs/backlog.md:464-480`) was deferred until a second CI occurrence or a user report; the second CI occurrence has now happened (Release run 41374596015 on v1.0.5 failed `ApplicationHostTests.Start_SecondInstance_SignalsActivationAndSkipsEnvironmentSetup`).

The current implementation starts the server loop asynchronously via `Task.Run` in the first-instance guard constructor (`src/CLIHub.Core.Windows/SingleInstanceGuard.cs:62`), and the `NamedPipeServerStream` is only created inside that background task (`:95`). A duplicate instance that reaches `SignalActivation()` during this window finds no pipe to connect to. The client makes exactly two `Connect(500)` attempts with a 100 ms sleep between them (`:70-87`), so the activation signal can be lost on thread-pool delays, high system load, or a cold .NET runtime — conditions that are not rare on a user's machine or in CI.

## What Changes

- **Deterministic server readiness**: create the first instance's `NamedPipeServerStream` synchronously in the guard constructor, before the constructor returns, and hand it to the background server loop. A pipe-creation failure will not make application startup fatal; the guard catches exceptions and lets the server loop retry (or equivalent), preserving the existing "start anyway, activation degrades" policy.
- **Bounded client deadline retry**: replace `SignalActivation()`'s two fixed attempts with a deadline-bounded retry (~2 seconds total) using short exponential backoff, so the signal reaches a server that becomes ready within that window. The method remains best-effort: it does not throw and its worst-case latency stays bounded.
- **Keep the inter-iteration gap bounded**: the server loop disposes the completed stream and recreates it on the next iteration, so a brief gap remains between connections; the client deadline retry is the safety net.
- **Test coverage**: add tests that would fail on the current implementation — signal immediately after constructing a fresh first-instance guard (repeated over several unique mutex/pipe names to catch race windows), a test proving a client connecting before `WaitForConnectionAsync` is pending is still delivered, and keep the existing ApplicationHost test green.
- **Backlog closure**: close `docs/backlog.md:464-480` as done.

## Capabilities

### New Capabilities

(none)

### Modified Capabilities

- `app-lifecycle`: the single instance enforcement requirement gains reliability wording — the first instance's activation pipe is listening before a duplicate can signal it; a duplicate's signal is retried within a bounded deadline; an activation that cannot be delivered never blocks or crashes either process.

## Impact

- `src/CLIHub.Core.Windows/SingleInstanceGuard.cs` — synchronous pipe creation before the server-loop task starts, bounded retry with backoff in `SignalActivation()`, and gap-minimizing server stream recreation.
- `tests/CLIHub.Core.Windows.Tests/SingleInstanceGuardTests.cs` (new or extended) — race-window test (immediate signal after guard construction, repeated), pre-connection delivery test, and possibly a bounded-latency assertion.
- `tests/CLIHub.Tests/ApplicationHostTests.cs` — keep `Start_SecondInstance_SignalsActivationAndSkipsEnvironmentSetup` green (it already waits 5 s, so the ~2 s client deadline fits).
- `docs/backlog.md` — close the deferred entry (`:464-480`) as done with a reference to this change.
- No API surface changes: `ISingleInstanceGuard`, `IsFirstInstance`, `ActivationRequested`, and disposal semantics are unchanged. Production mutex and pipe names are unchanged.

## Explicit Non-Goals

- Changing the public `ISingleInstanceGuard` contract, adding public readiness signals, or introducing new observable API.
- Making the pipe server single-shot (a server must still accept repeated activations).
- Persisting activation requests or queuing them — best-effort delivery is preserved.
- Adding download progress, retry counts, or latency telemetry to the guard.
- Changing the production mutex/pipe names or the `InternalsVisibleTo` pattern.

## Dependencies

None. This change is self-contained within the single-instance guard implementation and its tests.
