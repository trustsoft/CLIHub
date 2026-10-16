## Context

Today `SingleInstanceGuard` is registered lazily (`WindowsServiceCollectionExtensions.AddWindowsInfrastructure`) and is constructed at the first resolve inside `ApplicationBootstrapper.Start`. A duplicate launch therefore completes `%APPDATA%` layout creation, `config.json` parsing for log configuration, Serilog setup, and full DI container construction before `InstanceCoordinator.Coordinate()` detects the duplicate and exits. See proposal.md for motivation; the spec delta defines the target behavior.

Constraints that shape the design:

- `CLIHub` already references `CLIHub.Core.Windows`, so the host may construct `SingleInstanceGuard` directly.
- The spec keeps provider-owned disposal of the guard; ownership must not leak to `Program.Main`.
- The current spec text restricts guard construction to the composition root; the delta relaxes this to "acquired at the beginning of host startup".
- DI container construction happens inside `ApplicationHost.Start`; anything created before it cannot resolve dependencies, so the guard must be handed to the container as a pre-acquired instance.

## Goals / Non-Goals

**Goals:**

- Duplicate launch exits before touching the disk, logging, or DI.
- Mutex ownership is captured at the earliest possible moment, minimizing the double-launch race window.
- Provider retains ownership of guard disposal on the normal first-instance path.
- Keep existing test call sites working with minimal churn.

**Non-Goals:**

- No changes to `Program.cs`, the Velopack flow, mutex/pipe names, or `SignalActivation` retry behavior.
- No parallelization or reordering of post-acquisition startup steps (plugins, preferences, shell, hotkey, optional operations).
- No removal of `InstanceCoordinator`; it stays as a defensive confirmation on the first-instance path.
- No logging rework: the duplicate launch exits before Serilog is configured and stays silent, consistent with the existing best-effort second-instance policy.

## Decisions

### D1: Guard creation moves to the first statement of `ApplicationHost.Start`

`ApplicationHost.Start` acquires the guard before `DirectoryInitializer.EnsureAppDataLayout()`, `ConfigureLogging()`, and DI build. If the guard reports a second instance, the host calls `SignalActivation()` and returns; `WpfApplicationLifetime`shutdown of the not-yet-started WPF app is not required because the WPF `Application` is still starting up — the bootstrapper simply never runs and `App.OnStartup` completes normally.

Alternatives considered:

- *`Program.Main` before WPF init*: exits even earlier (before the WPF `App` object exists), but forces guard lifetime into `Program` and violates the provider-ownership requirement; rejected.
- *Keep DI-lazy creation*: no improvement; rejected.

`App.xaml` uses `ShutdownMode="OnExplicitShutdown"`, so a normal `OnStartup` return without any window would leave the duplicate process running forever. Because the WPF adapter is never reached in the duplicate path, the host itself requests application shutdown (`Application.Current?.Shutdown()`) before returning; in non-WPF test hosts `Application.Current` is null and the call is a no-op. `App.xaml.cs` remains unchanged.

### D2: Pre-acquired guard handed to the container as an instance registration, with an explicit disposal verification test

`AddWindowsInfrastructure` and `AddClIHubServices` gain an optional parameter:

```csharp
AddClIHubServices(this IServiceCollection services, SingleInstanceGuard? guard = null)
AddWindowsInfrastructure(this IServiceCollection services, SingleInstanceGuard? guard = null)
```

- `guard == null` (all existing test call sites and no production caller changes): current lazy `AddSingleton<SingleInstanceGuard>()` registration, unchanged behavior.
- `guard != null` (production `ApplicationHost`): registers the instance, preserving exactly one guard singleton.

Microsoft.Extensions.DependencyInjection tracks `IDisposable` objects resolved from container call sites, including constant (instance) singletons, but this nuance is easy to get wrong across versions. A dedicated unit test asserts provider disposal disposes the pre-acquired guard (mutex released, pipe server stopped). If the test proves the container does **not** dispose instance registrations under the used version, the documented fallback is a provider-created owner wrapper (`AddSingleton(sp => new GuardOwner(guard))` with `ISingleInstanceGuard` forwarding to `GuardOwner.Guard`) so the container-created owner owns disposal deterministically.

### D3: Early-failure disposal in `ApplicationHost.Start`

After acquiring the guard, the remaining pre-provider steps (directory init, logging config, DI build) are wrapped so that an exception before the provider exists disposes the guard and rethrows. The OS releases the mutex at process death regardless, but this keeps the "no locked mutex" guarantee observable within the application, matching the existing graceful-shutdown requirement.

### D4: Second instance exits silently

Serilog is configured after guard acquisition, so the duplicate launch produces no log lines. The current failure policy treats second-instance signaling as best-effort with swallowed errors; silence adds no user-visible change. `docs/architecture/startup.md` is updated to record this (the doc, not the spec, owns the implementation-order baseline).

## Risks / Trade-offs

- [DI disposal of instance registrations is version-nuanced] → Verification test with a documented owner-wrapper fallback (D2).
- [Exception between guard acquisition and provider creation] → try/dispose/rethrow in `ApplicationHost.Start` (D3); OS still releases the mutex on process death as final guarantee.
- [Duplicate launch no longer logs "CLIHub starting"] → acceptable under the existing best-effort second-instance policy; failure-policy table in startup.md updated.
- [Registration signature churn in tests] → default parameter keeps every existing `AddClIHubServices()` call site compiling; only new tests assert the instance-registration path.
- [`InstanceCoordinator`'s second-instance branch becomes unreachable in production] → kept deliberately as a defensive check; unit tests continue to exercise it in isolation.

## Migration Plan

Single-commit behavior change; no data or configuration migration. Rollback is a revert. Post-implementation manual check: launch CLIHub, launch it again, confirm the second process exits immediately and the first is activated, and that `%APPDATA%\CLIHub\` gains no new artifacts from the duplicate.

## Open Questions

None.
