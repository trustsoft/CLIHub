## 1. Core runtime model

- [x] 1.1 Replace `RuntimeKind.WindowsTerminal` with `PowerShell7` and `GitBash`, keeping `PowerShell` and `CommandPrompt`; verify the solution builds.
- [x] 1.2 Update `RuntimeKinds` tokens/aliases (`ps`, `ps7`, `cmd`, `bash`), drop `wt` aliases, and set the parse fallback to `PowerShell`; verify `RuntimeKindsTests` cover the new tokens and the fallback.
- [x] 1.3 Set `AppPreferences.DefaultRuntime` and `ProcessLauncher`'s default runtime to `PowerShell`; verify the app builds.

## 2. Command construction

- [x] 2.1 Update `WindowsCommandLineBuilder.BuildInteractive` for `PowerShell7` (`pwsh.exe -NoExit -EncodedCommand`) and `GitBash` (`bash.exe -lic`), remove the `wt` case, and default to PowerShell; verify `ProcessLauncherTests` build the expected command lines for each runtime.

## 3. Legacy and migration

- [x] 3.1 Update `LegacyTerminalPreferenceMigration` so `wt.exe` and unrecognized values map to `PowerShell`; verify the migration tests cover `wt.exe`→`ps` and missing/unknown→`ps`.

## 4. Settings UI

- [x] 4.1 Update `SettingsViewModel` runtime options to PowerShell, PowerShell 7, Command Prompt, Git Bash, defaulting to PowerShell; verify the settings view-model tests pass.
- [x] 4.2 Add PNG icon assets for the four hosts and render an icon + label per segment on the Terminal Profiles page; verify the selector shows four options with icons.
- [x] 4.3 Verify selecting and saving a runtime persists it and applies to later launches (existing runtime-applied tests, updated for the new set).

## 5. Verification

- [x] 5.1 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 5.2 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [x] 5.3 Run `openspec validate settings-terminal-profiles --strict` and verify the change is valid.
- [x] 5.4 Smoke-test: the Terminal Profiles selector shows four icon+label options; saving PowerShell 7 or Git Bash and launching an agent opens the chosen shell; a config with `defaultRuntime: "wt"` starts using PowerShell.
