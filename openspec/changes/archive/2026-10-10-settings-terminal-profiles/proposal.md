## Why

The runtime set is hardcoded to Windows Terminal / Command Prompt / PowerShell, exposing a window emulator (`wt`) as if it were a shell. The agreed model treats the terminal host as the shell itself and offers the shells users actually need. This replaces the stored `defaultRuntime` token set, so existing `wt` values must resolve to a valid shell.

## What Changes

- Replace the runtime set with four hosts (host == shell): **PowerShell** (`ps`), **PowerShell 7** (`ps7`), **Command Prompt** (`cmd`), **Git Bash** (`bash`). Default is **PowerShell**.
- Launch commands: `powershell.exe` / `pwsh.exe` with `-NoExit -EncodedCommand`, `cmd.exe` with `/d /k`, `bash.exe` with `-lic`.
- **BREAKING** for the stored `defaultRuntime` token: `wt` is removed. Existing `wt` / `wt.exe` values resolve to the new default (PowerShell); the legacy terminal-executable mapping maps `wt.exe` to PowerShell.
- The Terminal Profiles page shows a four-option segmented selector with an **icon and a label** per option; PNG icon assets are added.
- No separate shell-profile field: the host is the shell.

## Capabilities

### New Capabilities
<!-- New requirements land in existing capabilities below. -->

### Modified Capabilities
- `agent-commands`: the configured runtime set and default change; alternative runtimes and argument-boundary preservation cover PowerShell 7 and Git Bash.
- `configuration-snapshot`: `wt` is no longer a valid runtime token; unknown/removed runtime values resolve to PowerShell; the legacy `terminalExecutable` mapping targets the available shells.
- `preferences-ui`: the Terminal Profiles page offers the four runtime hosts and defaults to PowerShell.
- `settings-theme`: the runtime segmented selector renders an icon with a label per option.

## Impact

- Core: `RuntimeKind`/`RuntimeKinds`, `WindowsCommandLineBuilder`, `LegacyTerminalPreferenceMigration`, `AppPreferences.DefaultRuntime`, `ProcessLauncher` runtime default, `RuntimeSelector`.
- App: `SettingsViewModel` runtime options, `SettingsTerminalPage` selector, new icon assets + `SettingsStyles.xaml`/`SettingsTheme`.
- Tests: `RuntimeKindsTests`, `ProcessLauncherTests`, `SettingsApplicationServiceTests`, `PreferenceApplier`/`StartupPreferencesApplier` tests, serialization tests.

## Non-goals

- A separate shell-profile field; pure emulators (Alacritty, Tabby, Cmder, ConEmu, Ghostty); plugin-provided terminal hosts. These remain later roadmap items.
