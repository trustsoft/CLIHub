## Why

The System settings page is titled "Telemetry & Logs", but CLIHub collects no telemetry and the page only holds logging settings. The name is misleading; "Logs" matches what the page actually does.

## What Changes

- Rename the page label "Telemetry & Logs" to **"Logs"** in the sidebar and the page header.
- Rename the page view and its file from `SettingsTelemetryPage` to `SettingsLogsPage` (XAML + code-behind) and update the Settings window reference.
- The page key (`telemetry`) and all of its settings and behavior are unchanged.

## Capabilities

### New Capabilities
<!-- None: this renames an existing page label. -->

### Modified Capabilities
- `preferences-ui`: the log settings page is titled "Logs".
- `logging`: the log-file location is surfaced on the "Logs" page.

## Impact

- `src/CLIHub/ViewModels/SettingsViewModel.cs` (`AllPages` title)
- `src/CLIHub/Views/SettingsTelemetryPage.xaml`/`.cs` renamed to `SettingsLogsPage.xaml`/`.cs` (page header and class)
- `src/CLIHub/Views/SettingsWindow.xaml` (page reference)
- No `config.json` change; no `CLIHub.Core` change; no behavior change.

## Non-goals

- Moving or removing any log setting, and any other page rename.
