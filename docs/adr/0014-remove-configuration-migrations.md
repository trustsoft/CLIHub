# ADR-0014: Remove Configuration Migrations

**Date:** 2026-11-04  
**Status:** Accepted  
**Related:** ADR-0002 (single configuration document; its "Versioned schema with migrations" decision is superseded by this record)

## Context

ADR-0002 introduced a versioned configuration schema with a migration runner: `config.json` carried
a `schemaVersion`, `ConfigurationRepository` validated it, and `ConfigMigrationRunner` applied
ordered migrations (schema 0 → 1 mapped the legacy `terminalExecutable` to `defaultRuntime`;
schema 1 → 2 converted `showWindowOnStartup` into the inverted `startMinimized`).

Two things became clear as the document evolved:

- Only those two migrations ever existed, and both merely mapped a legacy field to its current
  equivalent — exactly what JSON deserialization already does. Missing fields deserialize to
  defaults, unknown fields are ignored, so a version-tolerant read produces the same state without
  any migration code.
- Every persisted-field change paid a tax: a migration class, its tests, and version bookkeeping,
  even when the change was fully backward-compatible (the recent scanning, provenance, and
  default-agent fields all shipped without needing one).

The legacy fields themselves (`terminalExecutable`, `showWindowOnStartup`) existed only to feed the
migrations; no current code reads them. `RuntimeKinds.TryParse` carried an executable/alias table
(`powershell.exe`, `command prompt`, `git bash`, …) whose only consumer was the schema 0 → 1
migration; the write path has always stored canonical tokens.

## Decision

Remove the migration layer and rely on a version-tolerant read.

- `ConfigurationRepository` deserializes the document, converts it to a snapshot, and caches it. It
  performs no schema-version validation, runs no migrations, and never rewrites the file at load;
  the established missing-file and corrupt-file default paths are unchanged.
- The `schemaVersion` metadata, the `terminalExecutable` and `showWindowOnStartup` fields, and the
  whole `Configuration/Migrations/` subsystem (runner, interfaces, result type, both migrations)
  are deleted from the persistence and domain models.
- `RuntimeKinds.TryParse` accepts only the canonical tokens `ps`, `ps7`, `cmd`, `bash`
  (case-insensitive, surrounding whitespace ignored). Unknown or missing values resolve to the
  default PowerShell runtime.

Rejected alternatives:

- *Keep `schemaVersion` as inert metadata* — rejected: it has no function without migrations and
  clutters the document; JSON already tolerates unknown properties.
- *Reject documents from other schema versions* — rejected: breaks existing installations for no
  benefit.

## Consequences

**Easier:** no migration code to write, test, or maintain; the load path is deserialize → convert →
cache; adding a persisted field ships with its default instead of a migration.

**Harder:** old values (`schemaVersion`, `terminalExecutable`, `showWindowOnStartup`) are silently
dropped on the next save, and there is no audit trail of applied migrations. Both are acceptable:
the dropped fields have no current function, and only two migrations ever existed.

**Committed to:** forward compatibility stays a deserialization property — unknown fields ignored,
missing fields defaulted — so older and newer builds keep interoperating without a version
negotiation.
