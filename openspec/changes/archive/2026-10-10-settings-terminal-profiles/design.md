## Context

The runtime is a hardcoded `RuntimeKind` enum (`WindowsTerminal`, `CommandPrompt`, `PowerShell`) mapped to tokens by `RuntimeKinds` and to launch command lines by `WindowsCommandLineBuilder.BuildInteractive` (`wt -d <dir> cmd /d /k …`, `cmd /d /k …`, `powershell -NoExit -EncodedCommand …`). The token is persisted as the string `defaultRuntime` via `AppConfigDocument.PreferencesDocument` and parsed back with `RuntimeKinds.Parse`, whose fallback default is currently `WindowsTerminal`. `ProcessLauncher` defaults its runtime to `WindowsTerminal`.

See `proposal.md` for motivation and the specs deltas for required behavior.

## Goals / Non-Goals

**Goals:**
- Replace the runtime set with four shells (PowerShell, PowerShell 7, Command Prompt, Git Bash); default PowerShell.
- Keep command construction and escaping in code; no descriptor indirection yet.
- Show an icon + label per option in the Terminal Profiles selector.

**Non-Goals:**
- Separate shell-profile field; pure emulators; plugin-provided hosts (later roadmap items).

## Decisions

### RuntimeKind replaces WindowsTerminal with the two new shells
`RuntimeKind` becomes `PowerShell`, `PowerShell7`, `CommandPrompt`, `GitBash`. `WindowsTerminal` is removed. `PowerShell` (Windows PowerShell, `powershell.exe`) stays the default; `PowerShell7` is `pwsh.exe`.

### Tokens `ps`, `ps7`, `cmd`, `bash`; removed tokens fall back
`RuntimeKinds.ToToken`/`TryParse` map the four tokens with aliases (`pwsh`→`ps7`, `gitbash`/`git-bash`→`bash`, `powershell`→`ps`, `command prompt`→`cmd`). The removed `wt`/`wt.exe`/`windows-terminal` aliases are dropped; `Parse` falls back to `PowerShell`. Because the removed token resolves through the fallback, **no schema bump or migration is required** for stored `defaultRuntime`; the next write persists `ps`. Alternative considered: a schema 3 migration converting `wt`→`ps` — rejected because parsing already coerces it and there is no stored-shape change.

### Legacy executable mapping targets the shells
`LegacyTerminalPreferenceMigration` maps `wt.exe` and any unrecognized value to `PowerShell`; `cmd.exe`→`CommandPrompt`; PowerShell executables→`PowerShell`.

### Launch cases
`WindowsCommandLineBuilder.BuildInteractive`: `PowerShell`→`powershell.exe -NoExit -EncodedCommand …`; `PowerShell7`→`pwsh.exe -NoExit -EncodedCommand …`; `CommandPrompt`→`cmd.exe /d /k …`; `GitBash`→`bash.exe -lic "<cmdline>"`. The `wt` case is removed; the switch default is PowerShell.

### Defaults
`AppPreferences.DefaultRuntime`, `ProcessLauncher._runtime`, and `RuntimeKinds.Parse` fallback all become `PowerShell`.

### Icon + label segments
`RuntimeOption` gains an icon reference; the segment template renders an `Image` (from a PNG resource) beside the label. Icons are added as PNG assets under `src/CLIHub/Assets/` (PowerShell, PowerShell 7, Command Prompt, Git Bash) and referenced by pack URI. Alternative considered: monochrome glyphs — rejected per the mockup's brand icons.

## Risks / Trade-offs

- [Tests depend on `RuntimeKind.WindowsTerminal`] → update `RuntimeKindsTests`, `ProcessLauncherTests`, `SettingsApplicationServiceTests`, `PreferenceApplier`/`StartupPreferencesApplier` tests to the new set/default.
- [Git Bash not installed / not on PATH] → launching `bash.exe` fails through the existing launch-failure path (logged, command reports failure); no availability gating in this change.
- [Brand icon licensing] → use recognizable host icons as small PNG resources; note provenance in the commit.

## Migration Plan

No schema version change. On load, a stored `defaultRuntime` of `wt`/unknown resolves to `ps`; the next persistence writes `ps`. Legacy (schema 0) `terminalExecutable` maps as above. Rollback is reverting the code.

## Open Questions

- Exact Git Bash install-path detection (rely on PATH `bash.exe` vs a known Git-for-Windows path) — settle during implementation; does not change the specs or task breakdown.
