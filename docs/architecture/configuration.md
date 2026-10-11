# Configuration Persistence

- **Format:** JSON with camelCase property names
- **Location:** `%APPDATA%\CLIHub\config.json`
- **Atomic writes:** write to a `.tmp` file, then rename (temp-file-then-rename)
- **Schema:** `AppConfigDocument` stores `schemaVersion`, `projects`, `preferences`, and `currentProjectId`. The current schema is version `2`; documents without a version are treated as legacy version `0`. `AppPreferences` includes `hotkey` (`Ctrl+Shift+A` by default), `defaultRuntime` (`ps`/`ps7`/`cmd`/`bash`), `logLevel`, `showOnlyProjectAgents`, `agentProbeTtlMinutes`, `agentProbeTimeoutSeconds`, `checkForUpdatesOnStartup`, `startWithWindows`, `startMinimized`, `pinLaunchWindow`, `pathDisplayStyle` (`leftTrim` by default, `middleEllipsis` as the alternative; unrecognized values fall back to the default), `lastSeenReleaseNotesVersion` (the version whose release notes were last shown — written by the application, not editable in Settings; missing or null means the notes have never been shown, which is treated as a first run without opening the What's New window), the project-scanning preferences `watchedDirectories` (default empty), `scanDepth` (default `2`, clamped to `1`–`8`), `ignoredPatterns` (absent uses the built-in set; an explicit empty list ignores nothing), and `discoveryMode` (`git`/`agents`/`both`, default `both`), and the agent-management preference `defaultAgentId` (the agent the launch window preselects; default null) (all optional; missing values fall back to defaults). Each project also stores `source` (`manual`/`discovered`, default `manual`) so scanning can remove only the projects it discovered. The legacy `terminalExecutable` and `showWindowOnStartup` fields are retained for compatibility and migrated on load.
- **Migrations:** `ConfigMigrationRunner` applies registered contiguous schema migrations. The current version 0 to version 1 migration maps recognized legacy terminal executables to `wt`, `cmd`, or `ps`; missing or unknown values use `wt`.
- **Forward compatibility:** missing fields deserialize to defaults; unknown fields are ignored. Unsupported future or invalid schema versions use safe in-memory defaults and do not overwrite the source document. Successfully migrated documents are scheduled for persistence in the current schema.

## Configuration Errors

- **Missing config.json:** create defaults on first run
- **Corrupted/invalid config.json:** log the error and fall back to in-memory defaults (the file is rewritten on the next save)
