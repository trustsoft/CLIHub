# Tasks: revise the General & Startup page copy

## Stage 1: Replace three UI strings

- [x] 1.1 Page description: `"How CLIHub starts and opens."` (`src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:46`)
- [x] 1.2 First card hint: `"Applies to this Windows account only."` (`src/CLIHub/Views/SettingsGeneralPage.xaml:12`)
- [x] 1.3 Second card hint: `"Starts in the tray without a window."` (`src/CLIHub/Views/SettingsGeneralPage.xaml:19`)

**Gate:** `dotnet build CLIHub.sln` — expect 0 warnings, 0 errors.

## Stage 2: Full verification gates

- [x] 2.1 Gate: `dotnet build CLIHub.sln`
- [x] 2.2 Gate: `dotnet test CLIHub.sln`
- [x] 2.3 Gate: `openspec validate --all`
- [x] 2.4 Gate: `graphify update .`

## Stage 3: Manual smoke and commit approval

- [x] 3.1 Manual smoke: Launch CLIHub, open Settings → General & Startup, and verify:
  - Page description under the header reads "How CLIHub starts and opens."
  - First card ("Start with Windows") hint reads "Applies to this Windows account only."
  - Second card ("Start minimized to tray") hint reads "Starts in the tray without a window."
  - No layout issues, truncation, or overflow.
- [x] 3.2 User approval: ask whether to commit and archive this change (wait for explicit approval; do not proceed without it).
