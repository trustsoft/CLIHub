## Why

Every state owner writes the whole `config.json` from its own in-memory copy.
`ProjectRegistry` caches a `Config` loaded once and saves it back on every
project add/remove, so any other writer (probe cache round, a future settings
editor) is silently reverted by the next project operation. The probe cache
also rewrites a file the user is expected to hand-edit, mixing machine churn
with user data. Splitting storage by owner removes the two-writer hazard
structurally instead of relying on reload-merge-save discipline, and it is
cheapest now, before the settings window (change 2) adds a third writer.

## What Changes

- **BREAKING (storage format):** `%AppData%\CLIHub\config.json` is replaced by
  three per-owner files in the same directory:
  - `settings.json` — `runtime`, `hotkey`, `probe`, `update` (user settings)
  - `projects.json` — `projects[]` (project registry)
  - `agents.json` — `agents{}` (machine-owned probe cache)
- One-time migration: on load, if the legacy `config.json` exists and the new
  files do not, it is split into the three files and kept as
  `config.json.migrated`; no released version exists yet.
- Each file gets its own `schemaVersion` and preserves unknown fields through
  round-trips; atomic temp-file replace and corrupt-file `.bak` backup apply
  per file.
- `ConfigStore` is generalized into a reusable JSON document store; the
  aggregate `Config` model is decomposed into per-file document models.
- No behavior change: UI, hotkey registration, probe rounds, update checks,
  and launch behavior are unchanged; only the storage layout moves.

## Capabilities

### New Capabilities
- `app-settings`: owns reading and writing `settings.json` — the user-editable
  global settings (runtime default, hotkey, probe tuning, update check flag).

### Modified Capabilities
- `app-config`: becomes the generic JSON document persistence contract
  (atomic write, corrupt backup, unknown-field survival, per-document
  `schemaVersion`) and owns the legacy `config.json` split migration; it no
  longer names a single `config.json`.
- `project-registry`: the registry persists to `projects.json` instead of the
  `projects` section of `config.json`.
- `agent-detection`: the probe cache persists to `agents.json`; probe tuning
  (`ttlMinutes`, `timeoutSeconds`) is read from `settings.json`.
- `global-hotkey`: the hotkey combination is read from `settings.json`.
- `app-update`: update settings are stored in the `update` section of
  `settings.json`.
- `agent-launch`: the global runtime fallback is read from `settings.json`.

## Impact

- **Code:** `ConfigStore` generalized (filename as parameter); new
  `SettingsStore`/`ProjectsStore`/`AgentsStore` (or typed wrappers); `Config`
  decomposed into `SettingsDocument`, `ProjectsDocument`, `AgentsDocument`;
  `ProjectRegistry` stops exposing `Runtime`/`Hotkey` (moves to settings
  read); `AgentDetector` reads probe tuning from settings and writes only its
  cache file; `UpdateService` reads `update.*` from settings; composition
  root in `App.xaml.cs` rewired.
- **Tests:** `ConfigStoreTests`, `ConfigPersistenceTests`, `ProjectRegistryTests`,
  `AgentDetectorTests`, `UpdateServiceTests` updated for the new files;
  migration tests added.
- **Docs:** `docs/vision.md`, `docs/ui.md`, `README.md`, `AGENTS.md` replace
  the single-file config description with the three-file layout.
- **Users:** dev machines and any early installs migrate automatically on
  first launch; hand-edited unknown top-level fields land in
  `settings.json`.
