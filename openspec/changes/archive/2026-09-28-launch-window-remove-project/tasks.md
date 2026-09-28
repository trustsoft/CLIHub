## 1. Launch window UI

- [x] 1.1 In `src/CLIHub/Windows/MainWindow.xaml`, place a "Remove Project" button beside "Add Project..." in the Projects pane action row, keeping the row's bottom margin aligned with the AI Agents pane (verify with `dotnet build CLIHub.sln`).
- [x] 1.2 In `src/CLIHub/Windows/MainWindow.xaml.cs`, add the `RemoveProject_Click` handler: require a selected project, show a confirmation prompt, call `IProjectService.RemoveProject`, update the status text, and refresh agents (verify with `dotnet build CLIHub.sln`).

## 2. Verification

- [x] 2.1 Run `dotnet build CLIHub.sln` and `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj`, then in the launch window select a project, click "Remove Project", and confirm: the prompt appears, confirming removes the project and refreshes both panes, and the folder on disk is untouched.
