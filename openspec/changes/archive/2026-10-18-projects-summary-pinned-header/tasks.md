## 1. Pin the summary cards

- [x] 1.1 Move the three summary cards (projects, agents, watcher) from `SettingsProjectsPage.xaml` into a `DataTemplate` in `SettingsWindow.xaml` window resources
- [x] 1.2 Render that template in the fixed page header for the "projects" page only, beneath the title and actions, with `controls` namespace wiring
- [x] 1.3 Move the header hairline below the summary-card row and keep the current spacing for pages without cards
- [x] 1.4 Verify the scroll region now starts at "WATCHED REPOSITORY DIRECTORIES" and the cards stay visible while scrolling

## 2. Verification

- [x] 2.1 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`
- [x] 2.2 Run `openspec validate --all` and confirm the change validates without errors
- [x] 2.3 Launch the app, open Settings → Projects & Paths, and confirm the pinned cards and divider placement
