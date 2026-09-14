## Context

Today a single `%AppData%\CLIHub\config.json` holds everything, and every
service loads/saves the whole aggregate `Config` through `ConfigStore`
(`ConfigStore.cs`). `ProjectRegistry` caches its `Config` instance from
construction and saves that cached copy on every add/remove — a stale-write
hazard for any other writer. `AgentDetector` already works around the shared
file by reloading the latest config right before saving the probe cache.
A settings window (follow-up change) would add a third writer. See
proposal.md — Why.

## Goals / Non-Goals

**Goals:**

- One owner — one file — one writer, so no writer can revert another's
  changes.
- Reuse the existing persistence guarantees (atomic replace, corrupt backup,
  unknown-field survival) across all three files with no duplicated logic.
- One-time, idempotent migration from the legacy single file.
- Keep behavior and UI untouched.

**Non-Goals:**

- The settings window itself (follow-up change).
- Cross-file transactions; no operation ever needs to write two files.
- Changing what is stored, only where each part lives.
- Hotkey re-registration at runtime (stays read-once-at-startup).

## Decisions

### D1. Owner layout and file names

`settings.json` (runtime, hotkey, probe, update) owned by a new
`SettingsStore`; `projects.json` (projects[]) owned by `ProjectRegistry`;
`agents.json` (agents{}) owned by `AgentDetector`.

Alternative considered: keep the user-facing name `config.json` for settings.
Rejected — three explicit names are self-describing, and `config.json` is
reserved for recognizing the legacy file in migration.

### D2. Per-file `schemaVersion`

Each document carries its own `schemaVersion` (all `1`). The files evolve
independently (e.g. a projects-schema change must not touch settings), and
independent versioning is what the migration already implies.

Alternative: one shared version — couples unrelated evolutions, rejected.

### D3. Document models

Decompose `Config` into `SettingsDocument` (schemaVersion, runtime, hotkey,
probe, update + `AdditionalData`), `ProjectsDocument` (schemaVersion,
projects + `AdditionalData`), `AgentsDocument` (schemaVersion, agents +
`AdditionalData`). The aggregate `Config` remains only inside the migrator
for reading the legacy file, then is deleted from general use.

Alternative: keep one `Config` type for all files with null sections —
invites cross-owner coupling and re-creates the shared-writer confusion in
the type system; rejected.

### D4. Generalized store

Replace `ConfigStore` with a generic `JsonDocumentStore<T>` taking the file
name as a constructor parameter; it implements load-with-defaults, atomic
temp-file replace, corrupt `.bak` backup, and `[JsonExtensionData]`
round-trip once. Three thin typed stores (`SettingsStore`, and instances
for projects/agents) wrap it. `SystemPathProvider` keeps supplying the
directory.

Alternative: three copies of store logic — violates the single
implementation goal; rejected.

### D5. Reader rewiring

- `ProjectRegistry` stops exposing `Runtime`/`Hotkey`; the popup reads the
  runtime default from `SettingsStore`.
- `AgentDetector` depends on `SettingsStore` (read probe tuning) and writes
  only `agents.json`; its reload-before-save workaround becomes unnecessary
  but harmless until removed.
- `UpdateService` reads `update.*` from `SettingsStore`.
- Composition root (`App.xaml.cs`) builds the stores and passes each owner
  its own.

### D6. Migration (split-if-present)

At startup, before any store is read: if `settings.json` does not exist and
legacy `config.json` does, parse it once, write the three owner files
(unknown top-level fields land in `settings.json` via `AdditionalData`),
then rename `config.json` to `config.json.migrated`. The presence of
`settings.json` makes the split one-time; a corrupt legacy file is skipped
( `.bak`-style copy aside is not needed — the file is never overwritten,
only renamed).

Alternative: no migration (fresh start). Rejected — dev machines and any
early hand-installed builds would silently lose data.

## Risks / Trade-offs

- [Migration writes three files non-atomically as a set] → Each file write
  is atomic individually; a crash mid-split leaves the legacy file intact
  and the split re-runs on next start (idempotent because only
  `settings.json` existence gates it; a partial split without
  `settings.json` is re-split, overwriting the partial files from the same
  legacy source).
- [`AdditionalData` of the legacy file goes to settings] → Deliberate: the
  successor of "the config file" is the settings document; documented in
  the app-config delta.
- [Tests touching `config.json` by name churn] → Mechanical rename to the
  new file names; no behavioral test changes.
- [Two files can disagree after a hand edit] → Same exposure as today with
  one file; no mitigation needed for v1.

## Migration Plan

Ship as one release. On first launch after update, the split runs before
stores load; rollback = restore `config.json.migrated` to `config.json` and
delete the three new files (documented in README). No released version
exists yet, so rollback is a dev-machine concern only.
