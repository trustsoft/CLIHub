# Tasks: Logs Page Copy Revision

## Stage 1: Copy Edits

- [x] 1.1 Remove the `<controls:SectionHeader Text="LOGGING"/>` element (line 11) from `src/CLIHub/Views/SettingsLogsPage.xaml` and add `Margin="0,18,0,8"` to the `<controls:SettingsGroup>` opening tag (line 12) to preserve the vertical rhythm (18 px top gap, 8 px bottom gap)
- [x] 1.2 Change `Hint="Takes effect after the next start."` to `Hint="Applies after restart."` on line 14 of `src/CLIHub/Views/SettingsLogsPage.xaml`
- [x] 1.3 Change `Hint="Daily log files to keep."` to `Hint="Daily files to keep."` on line 31 of `src/CLIHub/Views/SettingsLogsPage.xaml`
- [x] 1.4 Change `Hint="The per-file limit (0 = unlimited)."` to `Hint="Per file. 0 = no limit."` on line 41 of `src/CLIHub/Views/SettingsLogsPage.xaml`
- [x] 1.5 Remove the `<controls:SectionHeader Text="LOG FILE"/>` element (line 51) from `src/CLIHub/Views/SettingsLogsPage.xaml`; verify the standalone SettingCard below it retains its default `Margin="0,0,0,8"` (implicit from the SettingCard style, not set on the page) so the 8 px gap between the group and card is correct
- [x] 1.6 Change `Title="Log file"` to `Title="Current log file"` on line 52 of `src/CLIHub/Views/SettingsLogsPage.xaml`, and remove the `Hint="The current log file path."` attribute from line 53
- [x] 1.7 Change `error = "Log retention must be a positive number of days.";` to `error = "Log retention must be at least 1 day.";` on line 248 of `src/CLIHub/Settings/SettingsApplicationService.cs`
- [x] 1.8 Change `error = "Log file size limit must be zero or a positive number of megabytes.";` to `error = "Log size limit must be 0 MB or more.";` on line 254 of `src/CLIHub/Settings/SettingsApplicationService.cs`

## Stage 2: Verification Gates

- [x] 2.1 Run `dotnet build CLIHub.sln` from the repository root and verify zero errors
- [x] 2.2 Run `dotnet test CLIHub.sln` from the repository root and verify all tests pass
- [x] 2.3 Run `openspec validate --all` from the repository root and verify zero errors
- [x] 2.4 Run `graphify update .` from the repository root to refresh the knowledge graph after source edits

## Stage 3: Manual Smoke Test

- [x] 3.1 Open CLIHub and navigate to Settings → Logs in dark theme; confirm the approved copy is shown with no clipped or overlapping text, and the page rhythm (first group about 18 px below the divider, file card about 8 px below the group) matches the approved mockup
- [x] 3.2 Switch to light theme and re-check Settings → Logs; confirm the copy and spacing are correct in light theme as well
- [x] 3.3 Enter invalid values for log retention (0 or negative) and log size limit (negative); confirm the shortened validation error messages are shown
