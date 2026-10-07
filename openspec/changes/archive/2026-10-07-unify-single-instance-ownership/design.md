## Context

`SingleInstanceGuard` owns a named mutex and, for the first instance, a named-pipe server task. `App.OnStartup` currently constructs it with `new`, while `ServiceRegistration.AddClIHubServices` also registers the concrete type. `App.OnExit` then disposes only the manually-created instance. This leaves the DI registration misleading and makes ownership dependent on the startup path.

## Goals / Non-Goals

**Goals:**

- Establish one owner for the guard and its native resources.
- Preserve the current startup order and second-instance signaling behavior.
- Ensure the provider disposes the guard even when startup identifies a second instance and requests shutdown.

**Non-Goals:**

- Extracting the broader startup orchestration from `App`.
- Changing mutex or named-pipe names, signaling protocol, or retry behavior.
- Introducing an application-wide cancellation boundary for other background work.

## Decisions

- `ServiceRegistration.AddClIHubServices` remains the composition root registration point because the guard is consumed by WPF startup and activation dispatch.
- `App.OnStartup` builds the provider before resolving the guard, assigns the provider to the application field, and immediately checks `IsFirstInstance`.
- `App.OnExit` disposes the service provider, which disposes its singleton guard. The application no longer calls `SingleInstanceGuard.Dispose` directly.
- The guard field remains a typed reference for subscribing to `ActivationRequested`; it is not a second ownership boundary.
- The first-instance and second-instance integration tests continue to use direct guard construction because they test the guard's own mutex contract, while composition tests verify the production ownership path.

## Failure / Shutdown Policy

- If the resolved guard reports a second instance, `SignalActivation` is attempted, `Shutdown` is requested, and normal WPF exit disposes the provider.
- If startup fails after provider construction, WPF still reaches `OnExit` through the existing lifecycle; provider disposal remains the owner of the guard.
- Existing best-effort cleanup inside `SingleInstanceGuard.Dispose` is retained.
