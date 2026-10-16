## Why

Single-instance detection currently runs inside `ApplicationBootstrapper.Start`, after `%APPDATA%` layout creation, `config.json` parsing for log configuration, and full dependency-injection container construction. A duplicated launch therefore pays real disk I/O, logging setup, and container-build cost before learning it is a duplicate and exiting. Acquiring the mutex at the very beginning of startup makes duplicate launches exit almost immediately and narrows the race window for two simultaneous launches of a fresh instance.

## What Changes

- Acquire the `SingleInstanceGuard` mutex (and start its activation-pipe server) as the first startup action in `ApplicationHost.Start`, before data-directory creation, log configuration, and DI container construction.
- When the process is not the first instance, signal activation to the running instance and exit immediately, skipping directory creation, logging configuration, container construction, and the bootstrapper.
- Register the pre-created guard instance in the service collection so the provider keeps ownership of guard disposal and every resolution returns the same instance.
- Second-instance behavior before exit becomes faster and produces no AppData artifacts; first-instance startup order after guard acquisition is unchanged.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `app-lifecycle`: The "Single instance resource ownership" requirement changes so the guard is acquired at the beginning of host startup before the service provider exists, while the provider still owns disposal through instance registration. The "Application startup orchestration" requirement changes to reflect that a non-first instance exits during host startup before the bootstrapper runs; the in-bootstrapper instance check remains as a defensive no-op confirmation.

## Impact

- `src/CLIHub/Startup/ApplicationHost.cs` — early guard acquisition and second-instance exit before environment setup.
- `src/CLIHub/Composition/ServiceRegistration.cs` (and/or `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs`) — accept and register the pre-created guard instance instead of a lazily constructed singleton.
- `src/CLIHub/Lifetime/InstanceCoordinator.cs` — retains its check; effectively confirms first-instance status for the first instance path.
- Tests: `WindowsServiceCollectionExtensionsTests` (registration assertions), `ApplicationSessionTests` / `SingleInstanceGuardTestIsolation` (guard instance replacement), plus possible `ApplicationHost`-level test for the early-exit path.
- No changes to `Program.cs`, `App.xaml.cs`, Velopack flow, pipe/mutex names, or user-facing UI behavior.
