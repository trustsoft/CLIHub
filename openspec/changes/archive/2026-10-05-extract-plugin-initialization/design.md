## Context

`App.OnStartup` resolves `IPluginSeeder` and `IPluginManager` directly and invokes them in sequence. Both interfaces already exist in `CLIHub.Core.Plugins`, and their implementations preserve the required non-fatal/error-isolated behavior. The WPF composition root is the appropriate boundary for coordinating this startup-only workflow.

## Goals / Non-Goals

**Goals:**

- Move plugin initialization orchestration out of `App.xaml.cs`.
- Keep the existing `SeedIfEmpty` then `LoadPlugins` order.
- Make the sequence testable without constructing a WPF `Application`.
- Keep the new service small and limited to coordination.

**Non-Goals:**

- Change plugin discovery, validation, seeding, logo handling, or descriptor format.
- Change startup failure policy.
- Move `IPluginSeeder` or `IPluginManager` between projects.
- Introduce a plugin catalog abstraction or reload behavior.

## Decisions

- Add `IPluginInitializationService` and a WPF application-layer implementation, because the workflow coordinates existing Core services but is part of application startup rather than plugin domain behavior.
- Register the implementation as a singleton and inject the interface into `App` through the existing service provider.
- Keep the service method synchronous because both underlying operations are synchronous and startup currently invokes them synchronously.
- Do not catch exceptions in the coordinator. `PluginSeeder` already contains its documented non-fatal boundary, while unexpected plugin manager failures should retain the current startup behavior rather than be silently changed.
- Test ordering with fakes or mocks and test DI resolution through the WPF composition registration where practical.

## Risks / Trade-offs

- **A coordinator can become a dumping ground** -> Keep it limited to the two plugin initialization calls and reject unrelated startup logic in this change.
- **Changing registration can introduce a startup resolution failure** -> Add a composition test that resolves the new interface and run the full solution build/test.
- **Accidental behavior change in failure handling** -> Do not add or remove exception boundaries; preserve the existing implementations and call order.

## Migration Plan

1. Add the interface and implementation.
2. Register the service in WPF composition.
3. Replace the inline calls in `App.OnStartup`.
4. Run focused tests, full build, and full test suite.
5. Rollback consists of reverting the new service registration and restoring the two inline calls.

## Open Questions

None.
