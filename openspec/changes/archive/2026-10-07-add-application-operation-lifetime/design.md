## Context

See `proposal.md` for the motivation and `specs/app-lifecycle/spec.md` for the behavior contract. The previous fire-and-forget error boundary logs unexpected failures, but it does not own task lifetime or provide a common cancellation token. Current startup, update, command, settings, and version-population paths each start asynchronous work independently.

The application must remain responsive during normal operation and must not make Core process runners responsible for WPF application shutdown. Existing service-level catches and result contracts remain authoritative for expected update and command failures.

## Goals / Non-Goals

**Goals:**

- Give application-scoped async operations one owner and one cancellation token.
- Track operations started by startup/bootstrapper and synchronous UI/event handlers.
- Preserve existing user-facing results, status messages, and update behavior.
- Cancel and bounded-await tracked operations before DI disposal.
- Test completion, cancellation, exception observation, and timeout behavior without a WPF window.

**Non-Goals:**

- Changing Core process termination, timeout, or cancellation semantics.
- Introducing a general job scheduler, retry policy, or persistent background queue.
- Making shutdown wait indefinitely for external processes or network calls.
- Refactoring the next planned launch-window workflow split.

## Decisions

### Use one application-layer lifetime service

Add an `IApplicationOperationLifetime` contract with an application cancellation token, tracked-operation execution, and bounded stop method. The implementation owns one `CancellationTokenSource` and a thread-safe set of running tasks. The composition root registers it as a singleton.

An alternative was to create separate CTS instances in the bootstrapper and each ViewModel. That would preserve local control but cannot provide a reliable shutdown barrier or answer which tasks still use application resources.

### Track operations through an execution helper

The lifetime service will accept an operation name, a token-aware delegate, and an optional status callback. It observes unexpected exceptions and cancellation while preserving existing operation-specific result handling. Existing `AsyncOperationRunner` remains usable for UI status behavior; token-aware operations can be wrapped by the lifetime service rather than creating duplicate task registries.

### Cancel in `App.OnExit` before provider disposal

`App.OnExit` will resolve the singleton lifetime, request bounded stop, then dispose the service provider. The shutdown path remains synchronous at the WPF override boundary by waiting on the bounded async stop operation. The lifetime service must be idempotent so repeated shutdown paths do not create duplicate cancellation or waits.

### Use a finite shutdown bound

Production will use a short fixed bound suitable for UI shutdown, while tests inject the timeout or clock-independent delay behavior. A timeout logs a warning and allows provider disposal; it does not turn shutdown into an indefinite wait.

### Thread application tokens through existing contracts

Startup update checks and update downloads receive optional `CancellationToken` parameters. Agent command workflow calls and version population use the lifetime token. This keeps existing callers source-compatible while allowing the application composition root and ViewModels to participate in coordinated cancellation.

## Risks / Trade-offs

- [Risk] Some third-party or OS operations may ignore cancellation. -> Use the finite shutdown bound and continue disposal after timeout.
- [Risk] Cancellation could be reported as an ordinary failure to users. -> Handle `OperationCanceledException` separately and retain current status behavior for non-cancellation failures.
- [Risk] Double tracking can obscure ownership. -> Register each fire-and-forget entry point once at the outermost application/UI boundary and keep inner workflows token-aware but untracked.
- [Risk] Synchronous `OnExit` waiting can briefly delay process exit. -> Bound the wait, log timeout distinctly, and keep the bound configurable for tests.

## Migration Plan

1. Add the lifetime contract, implementation, and focused tests.
2. Add token parameters to update workflows and integrate the bootstrapper.
3. Route ViewModel fire-and-forget operations through the lifetime service.
4. Cancel and await tracked work in `App.OnExit` before provider disposal.
5. Run build, tests, OpenSpec validation, archive the change, and update `improvements.md`.
