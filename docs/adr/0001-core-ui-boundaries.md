# 0001. Core/UI boundaries

## Status

Accepted — 2026-10-06. Implemented by the original application scaffold and enforced by `split-core-and-ui-test-projects`.

## Context

CLIHub is a WPF application whose value is Windows integration: system tray, global hotkeys, Windows Terminal spawning, per-user registry startup, and single-instance enforcement. Business logic (configuration, projects, plugins, agents, updates) is independent of any of that presentation detail, but not independent of Windows itself.

If the business logic lives inside the WPF project, it can only be tested through WPF types, and every UI refactor risks breaking persistence or agent handling. Conversely, pretending the application is platform-neutral would hide the Windows integration behind abstractions nobody needs — CLIHub will never run anywhere but Windows.

## Decision

- `CLIHub.Core` targets `net10.0` and has **no WPF dependency**. It contains models, configuration, projects, plugins, agents, updates, and hotkey parsing.
- Core stays **UI-independent but Windows-aware**: registry access, named mutexes/pipes, process spawning, and Windows startup registration are explicit boundaries under `Core/Infrastructure/`, not hidden behind platform-neutral abstractions.
- `CLIHub` (the WPF project) is a thin presentation layer: windows, view models, tray, hotkey registration, converters. It references Core; Core never references it.
- Test projects mirror the rule: `CLIHub.Core.Tests` references Core only (no WPF), `CLIHub.Tests` references both for application/UI coverage.

Rejected alternatives:

- *Fully platform-neutral Core* — rejected: the Windows integration is the product; abstracting it away adds indirection without ever enabling a non-Windows port.
- *Single WPF project* — rejected: business logic becomes testable only through WPF and couples every refactor to the UI.

## Consequences

- Core logic is unit-testable without a WPF runtime (the Core-only test project builds and runs on any .NET 10 SDK).
- WPF specifics (dispatchers, window ownership) stay out of Core contracts; where a UI callback is needed, Core exposes plain delegates or interfaces the UI adapts.
- The dependency direction is enforced by project references, not convention alone: Core cannot see WPF types.
- Windows-specific code is concentrated under `Infrastructure/`, making future seams (or a hypothetical non-Windows port) visible at a glance.
