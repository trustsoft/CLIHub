## Why

Every preference today lives only in `%APPDATA%\CLIHub\config.json` — users cannot change the launch runtime, the global hotkey, agent probe behavior, or the startup update check from the app. The `ui/mockups/settings.png` mockup defines a Settings window for exactly these options. (The `terminalExecutable` preference is also currently unwired: `IProcessLauncher.SetTerminalExecutable` is never called.)

## What Changes

- Add a **Settings window** (`ui/mockups/settings.png`) opened from the tray, with four sections: **Default runtime**, **Global hotkey**, **Agents probe**, **Updates**, plus a footer with a version pill and **Save** / **Cancel**.
- **Default runtime** (`cmd` | `ps` | `wt`) drives how agents are launched; replaces the unwired `terminalExecutable` path with a 3-way runtime choice.
- **Global hotkey** capture field (Esc cancels) that validates the combination and **re-registers the hotkey at runtime** without a restart.
- **Agents probe** `TTL, minutes` and `Timeout, seconds` (empty → defaults) configure detection/version caching and the probe timeout.
- **Updates**: a "Check for updates on startup" toggle gating the startup check, a manual "Check for updates" button, and the current version shown.
- **Save** persists and applies preferences; **Cancel** / window close discards changes.
- New preferences are added to `AppPreferences` with defaults and forward-compatible loading.

## Capabilities

### New Capabilities
- `preferences-ui`: The Settings window — its sections, validation, and save/cancel/apply semantics.

### Modified Capabilities
- `agent-commands`: agent launch honors the configured default runtime (Windows Terminal, cmd, or PowerShell) instead of always Windows Terminal.
- `agent-detection`: system/project detection results are cached with a configurable TTL.
- `agent-version`: version caching expires on a configurable TTL, and version probes use a configurable timeout.
- `hotkey-support`: a hotkey changed in Settings takes effect immediately (re-registered without restart), not only at startup.
- `update-checking`: the automatic startup check runs only when the "Check for updates on startup" preference is enabled.

## Impact

- `src/CLIHub/Windows/SettingsWindow.xaml(.cs)` + `src/CLIHub/ViewModels/SettingsViewModel.cs` (new, MVVM).
- `src/CLIHub.Core/Models/AppConfig.cs` — new `AppPreferences` fields.
- `src/CLIHub.Core/Services/ProcessLauncher.cs` — runtime-based argument building; wire the runtime preference.
- `src/CLIHub.Core/Services/AgentDetectionService.cs`, `AgentVersionService.cs` — TTL cache + configurable timeout.
- `src/CLIHub/Hotkeys/GlobalHotkeyService.cs` — runtime re-registration.
- `src/CLIHub/TrayIconController.cs` — "Settings" menu entry; `App.xaml.cs` — gate startup update check, open Settings.
- Tests: new/updated Core tests for runtime arg building, TTL cache, timeout, and preferences defaults.
