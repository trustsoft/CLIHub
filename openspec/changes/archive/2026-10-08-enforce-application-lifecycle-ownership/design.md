## Context

See `proposal.md` for motivation. `ServiceProvider` disposes registered singleton instances that implement `IDisposable`, but some event owners currently lack that contract or use anonymous handlers that cannot be removed.

## Goals / Non-Goals

**Goals:**

- Make singleton event subscription owners deterministically disposable.
- Stop pane-owned version population when its owner is disposed.
- Preserve the legacy window source without exposing it through active DI composition.
- Add source-level architecture assertions for these ownership rules.

**Non-Goals:**

- Remove the legacy window in this change.
- Change startup, project, agent, or update user-visible behavior.
- Add a generic lifetime container or event bus.

## Decisions

- Implement idempotent `IDisposable` on `ProjectPaneController`, `AgentPaneController`, `LaunchWindowViewModel`, and `UpdateControlViewModel`. Use named event handlers so disposal can unsubscribe exactly what each instance registered.
- `AgentPaneController.Dispose` cancels and disposes its current version-population cancellation source and unsubscribes from plugin reload notifications.
- `LaunchWindowViewModel.Dispose` unsubscribes its project-pane and update-outcome handlers; other injected singleton services are disposed by DI according to their own ownership.
- Retain `MainWindow` in place as a deprecated legacy reference and assert it is not registered in `ServiceRegistration`.
- Extend the existing `ArchitectureBoundaryTests` so the source policy is tested in the existing architecture test project.

## Risks / Trade-offs

- Disposing shared instances from the ViewModel could duplicate DI ownership → the ViewModel only removes its own subscriptions and does not dispose injected dependencies; the provider disposes each singleton.
- Disposed controllers could still receive queued callbacks → disposal cancels owned work and increments the generation so late results cannot mutate current rows.
- Legacy code can become an accidental active dependency → document the deprecation and test that it remains absent from composition.

## Migration Plan

No migration is required. The DI provider already owns the registered singleton lifecycle; this change adds deterministic cleanup hooks to the instances it disposes.
