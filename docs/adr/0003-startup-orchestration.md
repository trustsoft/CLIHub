# 0003. Startup orchestration

## Status

Accepted — 2026-10-06. Implemented by the startup coordinator extractions (`extract-plugin-initialization`, `extract-startup-preferences`, `extract-hotkey-startup-registration`, `extract-release-notes-startup`, `extract-update-startup-check`) and recorded as a baseline by `document-application-startup-contract`.

## Context

Startup does a lot in a fixed order: single-instance enforcement, data-layout creation, logging, plugin seeding and loading, preference loading and application, Windows Run registration, tray icon, launch-window visibility, global hotkey registration, release-notes evaluation, and the background update check. Each step has its own failure mode, and some steps are order-sensitive — the runtime must be known before windows open, plugins must be seeded before they load, the log level must be read from preferences before anything logs meaningfully.

A monolithic `App.OnStartup` holding all of this inline was the original state: it worked, but every addition made the method longer, the order less visible, and none of it was testable without launching the app.

## Decision

- **Explicit ordered steps:** `OnStartup` remains a short, numbered sequence (documented in `docs/architecture/startup.md`) so the order is always visible in one place.
- **Each concern gets a coordinator:** plugin initialization (`IPluginInitializationService`), preference application (`IStartupPreferencesApplier`), hotkey registration (`IHotkeyStartupRegistrar`), release notes (`IReleaseNotesStartupCoordinator`), and the update check (`IUpdateStartupCoordinator`) are separate services resolved from DI and invoked in order by `App`.
- **Documented failure policy per step:** the current best-effort behavior (log and continue, or abort startup) is written down per operation as a baseline; changes must update both the code and the table.
- **No pipeline framework:** steps are plain awaited/straight-line calls in `App`, not middleware or a generic job runner.

Rejected alternatives:

- *Keep everything inline in `OnStartup`* — rejected: untestable and the order was implicit in method length.
- *Generic startup-pipeline/middleware abstraction* — rejected: one consumer, ten steps; an explicit list is easier to read, debug, and reorder than a framework.

## Consequences

- Each coordinator is unit-testable in isolation with a fake logger and services; `App` stays a composition shell.
- Reordering or inserting a step is a one-line change at the call site, reviewed against the documented table.
- The failure policy is honest: best-effort steps stay best-effort until a decision deliberately changes them, and the documentation cannot silently drift from intent.
- `App` keeps a long constructor/resolve list; acceptable at this scale, and the explicitness is the point.
