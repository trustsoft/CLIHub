## Why

The General & Startup page expresses startup visibility as "Show window on startup" — a positive, default-on flag. The agreed design expresses the intent the user actually chooses: "Start minimized to tray" (off by default). Because this replaces the stored preference, existing configurations must migrate so a user who currently starts in the tray keeps starting in the tray.

## What Changes

- Introduce the `StartMinimized` preference (default `true`) as the active startup-visibility preference, replacing `ShowWindowOnStartup`.
- Persist it in `config.json` as `startMinimized`; bump the configuration schema from version 1 to version 2 and add a migration 1→2 that maps `showWindowOnStartup = true → startMinimized = false` and `showWindowOnStartup = false → startMinimized = true`.
- At startup the application shows the launch window unless `startMinimized` is true; when true it starts in the system tray.
- Settings → General & Startup shows a "Start minimized to tray" toggle; saving persists it and applies it to the next start.
- No other preference is added, removed, or renamed. The change is **breaking for the stored preference shape** (schema 2), handled by migration.

## Capabilities

### New Capabilities
<!-- None: this change reworks an existing preference and its persistence. -->

### Modified Capabilities
- `preferences-ui`: the startup-visibility control on the General & Startup page becomes "Start minimized to tray", and Save applies it.
- `app-lifecycle`: startup window visibility follows the "Start minimized to tray" preference for manual and Windows launches.
- `configuration-snapshot`: the configuration schema advances to version 2 with a migration that converts the window-on-startup preference into start-minimized.

## Impact

- `src/CLIHub.Core/Models/AppConfig.cs` — add `StartMinimized`; retain `ShowWindowOnStartup` as a legacy field for migration.
- `src/CLIHub.Core/Models/AppConfigDocument.cs` — schema `CurrentSchemaVersion = 2`; preferences document carries `StartMinimized` and the legacy `ShowWindowOnStartup`.
- `src/CLIHub.Core/Configuration/` — new migration 1→2; DI registration of the migration.
- `src/CLIHub.Core/Configuration/ConfigurationSnapshot.cs` — clone the new field.
- `src/CLIHub/ApplicationBootstrapper.cs` — show the window unless start-minimized.
- `src/CLIHub/SettingsApplicationService.cs`, `src/CLIHub/ViewModels/SettingsViewModel.cs`, `src/CLIHub/Views/SettingsGeneralPage.xaml` — rename the preference end to end.
- Tests under `tests/CLIHub.Core.Tests` and `tests/CLIHub.Tests`.

## Non-goals

- System notifications (deferred to a later change).
- Any change to the Windows autostart ("Start with Windows") behavior.
