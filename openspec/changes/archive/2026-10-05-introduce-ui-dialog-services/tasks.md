## 1. Add UI Dialog Boundaries

- [x] 1.1 Add `IProjectDialogService`, `IUserNotificationService`, and WPF implementations preserving current owner, title, message, button, and icon behavior; verify the service types compile and register through DI.
- [x] 1.2 Inject the services into `LaunchWindowViewModel`, replace direct folder/message-box calls, and preserve `PromptState` scopes; verify a cancelled Add Project command uses the fake dialog without touching WPF.

## 2. Verify Preserved Behavior

- [x] 2.1 Verify that project persistence, status messages, notification text, and out-of-scope tray/legacy dialog paths remain unchanged.
- [x] 2.2 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`; verify the full solution remains green.
- [x] 2.3 Run `openspec validate introduce-ui-dialog-services` and verify the change has no spec deltas.
