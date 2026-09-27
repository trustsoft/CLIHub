## 1. Preference

- [x] 1.1 Add `bool ShowOnlyProjectAgents { get; set; }` (default false) to `AppPreferences` in `src/CLIHub.Core/Models/AppConfig.cs`, and verify it compiles

## 2. UI

- [x] 2.1 Add a "Only agents available in project" `CheckBox` to the agents panel in `src/CLIHub/Windows/MainWindow.xaml`, and verify the window compiles
- [x] 2.2 Inject `IConfigService` into `MainWindow`; initialize the checkbox from `ShowOnlyProjectAgents` (suppressing the change handler on init) and persist the preference when the user toggles it, and verify it compiles
- [x] 2.3 In `RefreshAgents`, when the filter is on and a project is selected, exclude agents not available in the project; otherwise include all with dimming; and verify the window runs

## 3. Verification

- [x] 3.1 Build the full solution with 0 warnings/errors and verify `dotnet test` still passes
- [x] 3.2 Manually verify: toggling the filter hides/shows unavailable agents for the selected project; with no project all agents show; the choice persists across a restart

> Verified: checkbox hides unavailable agents for the selected project, restores them (dimmed) when unchecked, shows all with no project, and the preference persists; 91/91 tests pass; build 0 warnings/errors. User-confirmed 3.2.
