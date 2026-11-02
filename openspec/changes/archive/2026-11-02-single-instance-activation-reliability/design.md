# Design: Single Instance Activation Reliability

## Verified Current State

### Server Loop Startup (`src/CLIHub.Core.Windows/SingleInstanceGuard.cs:59-63`)

The first-instance constructor starts the server with `Task.Run(() => this.ServerLoopAsync(this.cts.Token))`. The `NamedPipeServerStream` is created inside the background task (`:95-100`), so there is a scheduling window in which the guard constructor has returned but no pipe instance exists yet.

### Client Signal Attempts (`src/CLIHub.Core.Windows/SingleInstanceGuard.cs:70-87`)

`SignalActivation()` makes exactly two `Connect(500)` attempts with a 100 ms sleep between them:

```csharp
for (var attempt = 0; attempt < 2; attempt++)
{
    try
    {
        using var client = new NamedPipeClientStream(".", this.pipeName, PipeDirection.Out);
        client.Connect(500);
        using var writer = new StreamWriter(client) { AutoFlush = true };
        writer.WriteLine("SHOW");
        return;
    }
    catch
    {
        Thread.Sleep(100);
    }
}
```

Total time budget is roughly 1.2 seconds (2 × 500 ms connect timeout + 1 × 100 ms sleep). Every exception is swallowed. If both attempts fail, the method returns silently and the activation signal is lost.

### Server Loop (`src/CLIHub.Core.Windows/SingleInstanceGuard.cs:89-128`)

The server loop recreates the `NamedPipeServerStream` at the start of every iteration (`:95`), waits for a connection (`:102`), reads one line (`:105`), then disposes the server stream and repeats. Between one client disconnect and the next `WaitForConnectionAsync`, no pipe instance is listening, so a signal arriving in that gap is lost.

The loop's error handler (`:116-126`) catches all exceptions, delays 200 ms, and retries. Pipe-creation failures do not escape the loop or crash the application.

### Existing Test (`tests/CLIHub.Tests/ApplicationHostTests.cs:46-76`)

`Start_SecondInstance_SignalsActivationAndSkipsEnvironmentSetup` waits 5 seconds for the `ActivationRequested` event (`:70`). It timed out in the v1.0.5 Release run `41374596015` (commit `64e257f`), while the CI run `41374592563` on the same commit passed, demonstrating the intermittent nature of the race. An earlier occurrence is recorded in `docs/backlog.md:464-480` (run `40226957693`, commit `7355819`).

### Production Names

The mutex is `Local\CLIHub.SingleInstance` (`:11`) and the pipe is `CLIHub.SingleInstance` (`:12`). These are unchanged by this proposal.

### Disposal (`src/CLIHub.Core.Windows/SingleInstanceGuard.cs:133-162`)

`Dispose()` cancels the server loop's `CancellationTokenSource` (`:139`), waits up to 500 ms for the server task to complete (`:140`), and releases the mutex (`:153`). The wait is best-effort (exceptions are caught and ignored, `:142-145`).

## Goals / Non-Goals

**Goals:**

- Make pipe readiness deterministic: the first instance's pipe server is listening before the guard constructor returns.
- Prevent activation loss from timing races: a duplicate instance retries signaling for up to ~2 seconds.
- Keep the server loop multi-shot: accept repeated activations throughout the application's lifetime.
- Keep activation best-effort and non-blocking: `SignalActivation()` does not throw and completes within a bounded time.
- Preserve startup degradation policy: pipe-creation failures do not crash the application.

**Non-Goals:**

- Changing the `ISingleInstanceGuard` contract, production names, or disposal ownership.
- Adding public readiness signals, retry-count telemetry, or observable diagnostics.
- Making the server single-shot or queuing activation requests.
- Changing how the application host uses the guard.

## Decisions

### 1. Synchronous pipe creation in the constructor

The first-instance guard creates the `NamedPipeServerStream` synchronously before starting the server loop task, then passes the stream to the loop via a constructor field or local capture. The constructor still acquires the mutex first (to determine `IsFirstInstance`), so pipe creation only happens on the first-instance path.

**Code shape:**

```csharp
if (this.isFirstInstance)
{
    this.cts = new CancellationTokenSource();
    try
    {
        var initialServer = new NamedPipeServerStream(
            this.pipeName,
            PipeDirection.In,
            maxNumberOfServerInstances: 1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);
        this.serverTask = Task.Run(() => this.ServerLoopAsync(this.cts.Token, initialServer));
    }
    catch
    {
        // If pipe creation fails, the loop is not started; the application continues
        // without activation support (degraded but not fatal).
        // The loop's error handler would catch this too, so the failure policy is unified.
        this.serverTask = Task.Run(() => this.ServerLoopAsync(this.cts.Token, null));
    }
}
```

The loop checks for `null` on the first iteration and creates a new stream if needed. Pipe-creation failures inside the loop are already caught by the loop's error handler (`:116-126`), so the safety net is consistent.

**Rationale:**

- The pipe is listening before any duplicate can construct its guard and call `SignalActivation()`.
- Pipe-creation failures remain non-fatal; the existing loop error handler catches later creation attempts.
- No new public API or test seam is introduced.

**Alternative considered and rejected:**

A server-ready `TaskCompletionSource` that `SignalActivation()` awaits with a short timeout. Rejected because:
- It introduces cross-thread coordination (the client must know when to wait vs. when to retry immediately).
- It still requires the client retry for the case when the ready signal is set but the pipe is disposed between iterations.
- It leaks a readiness concept to the test surface (not forbidden, but avoidable).

### 2. Bounded deadline retry with exponential backoff

Replace the two-attempt fixed loop with a deadline-based retry (~2 seconds total). Use exponential backoff starting at 50 ms and capping at 500 ms, so early retries are fast and later retries give the server more time to become ready.

**Code shape:**

```csharp
public void SignalActivation()
{
    var deadline = Environment.TickCount64 + 2000; // monotonic; immune to clock changes.
    var delayMs = 50;
    const int maxDelayMs = 500;

    while (Environment.TickCount64 < deadline)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", this.pipeName, PipeDirection.Out);
            var remaining = (int)Math.Max(1, deadline - Environment.TickCount64);
            client.Connect(Math.Min(500, remaining));
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine("SHOW");
            return;
        }
        catch
        {
            if (Environment.TickCount64 >= deadline)
            {
                return; // Out of time; give up silently.
            }
            Thread.Sleep(delayMs);
            delayMs = Math.Min(delayMs * 2, maxDelayMs);
        }
    }
}
```

The `Connect` timeout is adjusted on the final attempt to not overshoot the deadline by 500 ms.

**Rationale:**

- A server that becomes ready within 2 seconds receives the signal, covering thread-pool startup delays, high load, and the inter-iteration gap.
- The method still does not throw and completes within ~2 seconds worst-case (acceptable for a duplicate launch that will exit immediately after).
- Exponential backoff reduces CPU/logging churn during transient failures.

**Alternative considered and rejected:**

Retry forever until the signal succeeds. Rejected because:
- It can block the duplicate instance indefinitely if the first instance is deadlocked or the pipe is permanently broken.
- A bounded deadline preserves the "exit quickly" contract for duplicate launches.

### 3. Gap-bounded server stream recreation

The server loop disposes the current stream immediately after reading one message and creates a fresh stream on the next iteration, so the gap is limited to the turn between `Dispose` and `WaitForConnectionAsync`. It is not zero (a client can still arrive in the gap), and the client retry covers it. Pre-creating the next stream before disposing the current one is not possible while `maxNumberOfServerInstances` is 1 (see the rejected alternative below).

**Code shape:**

```csharp
private async Task ServerLoopAsync(CancellationToken token, NamedPipeServerStream? initialServer)
{
    var server = initialServer;
    while (!token.IsCancellationRequested)
    {
        try
        {
            if (server == null)
            {
                server = new NamedPipeServerStream(
                    this.pipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
            }

            await server.WaitForConnectionAsync(token);

            string? message;
            using (var reader = new StreamReader(server))
            {
                message = await reader.ReadLineAsync(token);
            }

            if (string.Equals(message, "SHOW", StringComparison.OrdinalIgnoreCase))
            {
                this.ActivationRequested?.Invoke();
            }

            server.Dispose();
            server = null; // Next iteration will create a fresh stream.
        }
        catch (OperationCanceledException)
        {
            server?.Dispose();
            break;
        }
        catch
        {
            server?.Dispose();
            server = null;
            try
            {
                await Task.Delay(200, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
```

**Rationale:**

- The gap is minimized (not eliminated), and the client retry is the safety net.
- The loop is still multi-shot: one client disconnect does not stop the server.
- No change to the server's failure-handling policy.

**Alternative considered:**

Pre-create the next stream before disposing the current one (create two streams in rapid succession). Rejected because:
- `maxNumberOfServerInstances: 1` means only one server instance can exist at a time; creating a second one before disposing the first would throw.
- Changing `maxNumberOfServerInstances` to `2` or `NamedPipeServerStream.MaxAllowedServerInstances` complicates the single-instance contract (a client could connect to either server instance, and the application would need to accept connections on both).

### 4. Test coverage

Add tests in `tests/CLIHub.Core.Windows.Tests/SingleInstanceGuardTests.cs` (new or extended):

- **Race-window test**: Construct a first-instance guard with a unique mutex/pipe name, immediately call `SignalActivation()` from a second-instance guard (or directly via a `NamedPipeClientStream`), and assert the `ActivationRequested` event fires. Repeat this 10 times over fresh unique names to catch scheduling variability. On the current implementation, this test would sometimes fail (the signal is lost); on the fixed implementation, it passes reliably.
- **Pre-connection delivery test**: Construct a first-instance guard, start a client connection before `WaitForConnectionAsync` is called (simulate arriving during the gap), and assert the connection is eventually served and the event fires. This is harder to orchestrate (the gap is small), so the race-window test is the primary coverage.
- **Bounded-latency test** (optional): Call `SignalActivation()` when no server exists and assert it completes within ~2.5 seconds (allowing for scheduling jitter). Verifies the deadline works.

Keep `tests/CLIHub.Tests/ApplicationHostTests.cs:46-76` unchanged; its 5 s wait is already longer than the new 2 s client deadline, so it remains green.

**Rationale:**

- The race-window test would fail on the current implementation and pass on the fixed one, proving the fix.
- No new test seam or public API is needed; the test constructs guards directly using the existing `internal` constructor that takes explicit mutex/pipe names.

**Alternative considered:**

Expose an `internal bool IsServerReady` property for test assertions. Rejected because:
- The race-window test is sufficient without it.
- The property would be a test-only seam with no production use, and the existing `InternalsVisibleTo` pattern is already used for `IsDisposed`.

## Alternatives Considered

### Retry-only (no synchronous pipe creation)

Keep the server loop async-started but extend the client retry to ~5 seconds. Rejected because:
- A 5 s worst-case latency for a duplicate launch is user-visible (the duplicate does not exit for 5 s).
- The root cause (server not ready) is not fixed; the retry merely papers over it.
- The race is still present on fast machines or in tight test loops.

### Server-ready signal

Expose a `Task ServerReady` that completes when the pipe is listening, and have `SignalActivation()` optionally wait for it. Rejected because:
- It introduces cross-thread coordination and a new public or internal API.
- The gap between iterations is still present, so the client still needs retry.
- Synchronous pipe creation is simpler and eliminates the readiness question entirely.

### Single-shot server

Make the pipe server accept one connection, then require manual restart. Rejected because:
- Repeated activations throughout the application's lifetime would not work (second launch after the first activation would fail).
- The `app-lifecycle` spec requires repeated activation support (`:21-23`: "First instance receives activation signal" is not described as one-time).

## Risks / Trade-offs

- **[Pipe creation fails in the constructor]** → The constructor catches the exception and starts the loop with `null`; the loop recreates the stream on the first iteration, so the failure is non-fatal. This matches the existing loop error policy.
- **[Client arrives during the inter-iteration gap]** → The client retry (~2 s) covers the gap. The gap is minimized but not eliminated.
- **[Disposal race: client connects during `Dispose`]** → The `Dispose` method cancels the loop and waits 500 ms for it to complete. A client that connects after cancellation is ignored (the loop breaks on `OperationCanceledException`). This is acceptable; the application is shutting down.
- **[Two-second client latency is too long]** → The client only retries when the server is not ready. On a healthy system, the first connection succeeds immediately (within ~50 ms). The 2 s bound is the worst-case fallback for cold starts or high load.
- **[Test flakiness from the 10-iteration race test]** → If the fix works, the test passes reliably. If the test still flakes, it exposes a remaining race, which is the test's purpose.

## Open Questions

1. Should the client deadline be configurable (e.g., via a constructor parameter for tests)? **Proposed answer:** No. The 2 s deadline is a reasonable fixed value for production and test use. Making it configurable adds API surface for a constant that does not need to vary.
2. Should the server loop log when it recreates a stream after a failure? **Proposed answer:** No. The loop already swallows exceptions and continues; adding logging to the hot path (every activation) is noisy. If diagnostics are needed, they belong in a separate change.
3. Should `SignalActivation()` return a boolean indicating success/failure? **Proposed answer:** No. The method is best-effort by contract (`ISingleInstanceGuard.SignalActivation()` is `void`). Changing the return type would break the interface. Callers already assume the signal might be lost and do not check a result.

## Deviations from Locked Decisions

None. The design follows all six locked decisions:

1. ✓ Deterministic pipe readiness (synchronous creation in constructor).
2. ✓ Bounded deadline retry (~2 s, exponential backoff).
3. ✓ Gap minimized (stream created before disposing the prior one, within the `maxNumberOfServerInstances: 1` constraint).
4. ✓ No API surface changes (mutex/pipe names, `ISingleInstanceGuard`, `IsFirstInstance`, `ActivationRequested`, disposal unchanged).
5. ✓ Tests that fail on the current implementation (race-window test, pre-connection test, existing ApplicationHost test stays green).
6. ✓ No new public API; `internal` seam avoidable (race-window test uses the existing `internal` constructor).
