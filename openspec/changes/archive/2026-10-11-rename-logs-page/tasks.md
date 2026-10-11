## 1. Rename

- [x] 1.1 Set the `telemetry` page title to "Logs" in `SettingsViewModel.AllPages` and the `PageHeader` text to "Logs".
- [x] 1.2 Rename the page view `SettingsTelemetryPage` → `SettingsLogsPage` (`SettingsLogsPage.xaml` + `.xaml.cs`, `x:Class`, class name, and the `SettingsWindow.xaml` reference); verify the sidebar, page header, and page render as "Logs".

## 2. Verification

- [ ] 2.1 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [ ] 2.2 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [ ] 2.3 Run `openspec validate rename-logs-page --strict` and verify the change is valid.
- [x] 2.4 Smoke-test: the sidebar item and the page header read "Logs", and the log settings still load and save.
