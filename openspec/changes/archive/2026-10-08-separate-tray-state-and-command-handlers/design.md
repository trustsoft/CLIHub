## Context

See `proposal.md` for motivation. `TrayMenuBuilder` already receives prepared state and commands, while `TrayIconController` owns the WPF `TaskbarIcon`. The remaining mixed responsibility is the `TrayActions` implementation that both reads state and executes workflows.

## Goals / Non-Goals

**Goals:**

- Keep `TrayIconController` as the tray-control owner and `TrayMenuBuilder` as a presentation builder.
- Make state projection independently testable from command execution.
- Preserve the current `ITrayActions` event and command surface for existing consumers.

**Non-Goals:**

- Change tray menu contents, labels, update semantics, or notification behavior.
- Introduce a service locator, generic event bus, or new Core dependency.
- Move WPF controls or tray-specific workflows into `CLIHub.Core`.

## Decisions

- Add `TrayStateProjection` to read projects, plugins, and update state and expose `GetState()` plus a state-change event. It owns subscriptions to the three source events and implements deterministic disposal.
- Add `TrayCommandHandlers` to own action delegates for project dialogs, agent launch, update checks/download requests, settings, release notes, and application exit. It receives the existing focused application ports and operation lifetime.
- Keep `TrayActions` as a thin adapter implementing `ITrayActions`: it composes projection and handlers, exposes their state/events, and forwards `TrayMenuCommands`.
- Keep update download requests as an event raised by the command handlers. `ApplicationSession` remains the owner of the shared download coordinator wiring.

## Risks / Trade-offs

- More small types increase composition registrations → register them as application singletons and keep `TrayActions` as the single existing consumer-facing adapter.
- Event forwarding can accidentally duplicate notifications → projection is the only owner of source subscriptions, and adapter tests verify disposal and one notification per source change.
- Handler delegates can hide dependencies → expose constructor parameters as the existing narrow ports and keep behavior-specific tests at the handler boundary.

## Migration Plan

No persisted-data or deployment migration is required. Rollback consists of reverting the tray application services and their composition/tests together.
