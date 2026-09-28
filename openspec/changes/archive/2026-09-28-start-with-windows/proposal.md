## Why

`AppPreferences.StartWithWindows` already exists in the config but is never used: the app cannot register or remove itself from Windows startup, and it always opens its main window at launch — including when started by Windows, which would pop the window at every sign-in.

## What Changes

- Add a **Startup** section to the Settings window with two toggles: **Start with Windows** and **Show window on startup**.
- **Start with Windows** registers/removes a per-user Windows Run entry (no admin) for the current executable, and the window toggle reflects the current registration state.
- Add a **Show window on startup** preference (default on). When off, the application starts in the system tray without showing the main window — for both a manual launch and a Windows autostart. This single preference governs autostart visibility.
- On startup with autostart enabled, the Run entry is refreshed to the current executable path (so it survives an update moving the install).

## Capabilities

### New Capabilities
<!-- None. -->

### Modified Capabilities
- `app-lifecycle`: adds "Start with Windows" (per-user Run registration) and "Startup window visibility" (show/tray per preference) requirements.
- `preferences-ui`: adds a "Startup" section and an autostart-applied behavior to the Settings window.

## Impact

- `src/CLIHub.Core/Models/AppConfig.cs` — add `ShowWindowOnStartup` (default true); `StartWithWindows` becomes authoritative.
- `src/CLIHub.Core/Interfaces/IStartupService.cs`, `src/CLIHub.Core/Services/StartupService.cs` (new) — read/write the current-user Run entry.
- `src/CLIHub/App.xaml.cs` — respect `ShowWindowOnStartup`; refresh the Run entry when enabled.
- `src/CLIHub/ViewModels/SettingsViewModel.cs`, `src/CLIHub/Windows/SettingsWindow.xaml`, `IPreferenceApplier`/`PreferenceApplier` — Startup section, load/save/apply.
- `src/CLIHub.Core/ServiceCollectionExtensions.cs` — register `IStartupService`.
- Tests: new `StartupServiceTests` (abstracted registry access or a test seam).
