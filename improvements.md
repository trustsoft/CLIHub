# Architecture Improvement Roadmap

This file contains only active directions. Completed refactors, their decisions, and their task breakdowns
belong in Git history and [`openspec/changes/archive/`](openspec/changes/archive/).

## Active Directions

### Per-agent configuration

Explore optional plugin settings for custom commands, probe behavior, and working-directory policy. Keep the
current descriptor format and capability-aware command checks stable until a concrete use case justifies a
durable spec change.

### Agent activity notifications

Evaluate notifications for background agent activity without turning CLIHub into an in-process agent host.
The existing external-process boundary must remain intact.

### Graceful process coordination

Extend the current running-agent safety check beyond blocking updates only if the user experience and shutdown
semantics are specified first.

### Integration coverage

Add integration coverage where it validates composition, startup, update, and process-boundary behavior that
unit tests cannot express economically.

## Working Principles

1. Keep each component responsible for one workflow or boundary.
2. Keep Core logic testable without WPF infrastructure.
3. Use existing controllers, coordinators, services, and narrow interfaces before adding abstractions.
4. Preserve observable behavior and the single configuration persistence boundary during refactors.
5. Add a durable OpenSpec requirement before implementing a new user-visible capability.

See [`docs/project-context.md`](docs/project-context.md) for current architecture facts and
[`docs/architecture.md`](docs/architecture.md) for the current subsystem map.
