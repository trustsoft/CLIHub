# 0002. Single configuration document

## Status

Accepted — 2026-10-06. Implemented by `introduce-configuration-snapshot`, `add-config-schema-version`, `add-config-migration-runner`, `make-preferences-and-project-updates-explicit`, and `introduce-configuration-repository`.

## Context

CLIHub persists user preferences (hotkey, runtime, filters, update behavior) and project state (tracked projects, current selection). Both change independently and at different moments — a preference toggle in the Settings window, a project selection in the launch window — and both must survive crashes.

The naïve designs each break something: separate files duplicate the persistence machinery and turn "consistent settings + projects" into a distributed problem, while letting every feature write the shared file directly produces interleaved writes and lost updates.

## Decision

- **One document, one file:** `AppConfigDocument` at `%APPDATA%\CLIHub\config.json`, camelCase JSON, is the only persisted configuration. Unknown fields are ignored and missing fields fall back to defaults, so older and newer builds interoperate.
- **One atomic write path:** every save goes through `ConfigurationRepository`, which writes a temp file and renames it. No other component writes the file.
- **Narrow adapters, shared document:** `PreferencesStore` and `ProjectStateStore` expose preference-only and project-state-only views over the same document, so responsibility can be split without splitting the file.
- **Explicit updates over direct mutation:** components call `Update(callback)` on the stores; the repository serializes latest-snapshot updates so concurrent writers cannot lose changes.
- **Versioned schema with migrations:** the document carries a schema version; a migration runner upgrades legacy documents (schema 0 → 1 mapped the legacy `terminalExecutable` to `defaultRuntime`), and unknown or future versions load with defaults instead of corrupting state.

Rejected alternatives:

- *Separate preference and project files* — rejected: duplicates the atomic-write machinery and makes cross-file consistency an unsolved problem.
- *Direct shared-file mutation from feature code* — rejected: lost updates and partial writes; replaced by explicit callbacks plus a serialized snapshot.

## Consequences

- Crash safety comes free from the single temp-file-then-rename path; there is no multi-file recovery scenario.
- Concurrency is centralized: a caller cannot bypass the serialization of updates even by mistake.
- Every persisted field change ships with a migration, keeping installed bases upgradable in place.
- The document grows monolithically; if a future domain (e.g., per-project preferences) explodes in size, splitting files will require a new decision that supersedes this one.
