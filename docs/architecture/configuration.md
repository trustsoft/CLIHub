# Configuration Persistence

- **Format:** JSON with camelCase property names
- **Location:** `%APPDATA%\CLIHub\config.json`
- **Atomic writes:** write to a `.tmp` file, then rename (temp-file-then-rename)
- **Schema:** `AppConfig` with `projects`, `preferences`, and `currentProjectId`. `AppPreferences` includes `hotkey` (`Ctrl+Shift+A` by default), `defaultRuntime` (`wt`/`cmd`/`ps`), `logLevel`, `showOnlyProjectAgents`, `agentProbeTtlMinutes`, `agentProbeTimeoutSeconds`, `checkForUpdatesOnStartup`, `startWithWindows`, `showWindowOnStartup`, `pinLaunchWindow`, `pathDisplayStyle` (`leftTrim` by default, `middleEllipsis` as the alternative; unrecognized values fall back to the default), and `lastSeenReleaseNotesVersion` (the version whose release notes were last shown — written by the application, not editable in Settings; missing or null means the notes have never been shown, which is treated as a first run without opening the What's New window) (all optional; missing values fall back to defaults); the legacy `terminalExecutable` is superseded by `defaultRuntime`
- **Forward compatibility:** missing fields deserialize to defaults; unknown fields are ignored. There is no explicit schema-version field yet (adding one is deferred).

## Configuration Errors

- **Missing config.json:** create defaults on first run
- **Corrupted/invalid config.json:** log the error and fall back to in-memory defaults (the file is rewritten on the next save)
