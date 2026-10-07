## Why

`App` currently creates `SingleInstanceGuard` directly even though the guard is also registered in the WPF composition root. The registered instance is never used, so the application has two possible ownership paths for the named mutex and the activation pipe.

## What Changes

- Register `SingleInstanceGuard` as the single production-owned singleton in the application composition root.
- Resolve the guard from the built service provider in `App.OnStartup`.
- Let service-provider disposal release the guard on every shutdown path, including a second-instance exit.
- Add regression coverage proving the registration is singleton-scoped and that the application path does not manually create or dispose a second guard.

## Capabilities

### Modified Capabilities

- `app-lifecycle`: single-instance resource ownership is delegated to the DI container while preserving first-instance, second-instance, activation, and shutdown behavior.

## Impact

- `src/CLIHub/App.xaml.cs` — resolve the guard from DI and remove direct guard disposal.
- `src/CLIHub/ServiceRegistration.cs` — keep the guard registration as the explicit application-owned singleton.
- `tests/CLIHub.Tests` — verify singleton registration and provider-owned disposal behavior.
- `openspec/specs/app-lifecycle/spec.md` — document the single-owner lifecycle rule.
