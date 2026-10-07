# Configuration Persistence

- **Format:** JSON with camelCase property names
- **Location:** `%APPDATA%\CLIHub\config.json`
- **Atomic writes:** write to a `.tmp` file, then rename (temp-file-then-rename)
- **Schema:** `AppConfigDocument` stores `schemaVersion`, `projects`, `preferences`, and `currentProjectId`. The current schema is version `1`; documents without a version are treated as legacy version `0`. `AppPreferences` includes `hotkey` (`Ctrl+Shift+A` by default), `defaultRuntime` (`wt`/`cmd`/`ps`), `logLevel`, `showOnlyProjectAgents`, `agentProbeTtlMinutes`, `agentProbeTimeoutSeconds`, `checkForUpdatesOnStartup`, `startWithWindows`, `showWindowOnStartup`, `pinLaunchWindow`, `pathDisplayStyle` (`leftTrim` by default, `middleEllipsis` as the alternative; unrecognized values fall back to the default), and `lastSeenReleaseNotesVersion` (the version whose release notes were last shown — written by the application, not editable in Settings; missing or null means the notes have never been shown, which is treated as a first run without opening the What's New window) (all optional; missing values fall back to defaults); the legacy `terminalExecutable` is retained for compatibility and migrated to `defaultRuntime` when a version 0 document is loaded.
- **Migrations:** `ConfigMigrationRunner` applies registered contiguous schema migrations. The current version 0 to version 1 migration maps recognized legacy terminal executables to `wt`, `cmd`, or `ps`; missing or unknown values use `wt`.
- **Forward compatibility:** missing fields deserialize to defaults; unknown fields are ignored. Unsupported future or invalid schema versions use safe in-memory defaults and do not overwrite the source document. Successfully migrated documents are scheduled for persistence in the current schema.

## Configuration Errors

- **Missing config.json:** create defaults on first run
- **Corrupted/invalid config.json:** log the error and fall back to in-memory defaults (the file is rewritten on the next save)
