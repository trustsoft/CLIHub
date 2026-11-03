# Tasks: revise the Terminal Profiles page copy

## Stage 1: Replace three UI strings

- [x] 1.1 Page description: `"The terminal that CLI agents run in."` (`src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:87`)
- [x] 1.2 Card hint: `"Used for every agent command."` (`src/CLIHub/Views/SettingsTerminalPage.xaml:13`)
- [x] 1.3 Unavailable runtime help text: `"Not installed. Agent commands can't run in this runtime."` (`src/CLIHub/Views/SettingsTerminalPage.xaml:34`)

**Gate:** `dotnet build CLIHub.sln` — expect 0 warnings, 0 errors.

## Stage 2: Full verification gates

- [x] 2.1 Gate: `dotnet build CLIHub.sln`
- [x] 2.2 Gate: `dotnet test CLIHub.sln`
- [x] 2.3 Gate: `openspec validate --all`
- [x] 2.4 Gate: `graphify update .`

## Stage 3: Manual smoke and commit approval

- [x] 3.1 Manual smoke: Launch CLIHub, open Settings → Terminal Profiles, and verify:
  - Page description under the header reads "The terminal that CLI agents run in."
  - "Default runtime" card hint reads "Used for every agent command."
  - No layout issues, truncation, or overflow.
  - The unavailable-runtime text is an `AutomationProperties.HelpText` value, not a visible tooltip. Verify it by inspecting the XAML or with Accessibility Insights/Narrator only if an unavailable runtime is present; do not uninstall a runtime just for smoke.
  - Confirmed by the user on 2026-11-03.
- [x] 3.2 User approval: ask whether to commit and archive this change (wait for explicit approval; do not proceed without it).
