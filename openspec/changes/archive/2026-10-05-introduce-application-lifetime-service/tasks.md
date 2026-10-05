## 1. Add Application Lifetime Boundary

- [x] 1.1 Add `IApplicationLifetime` and `WpfApplicationLifetime`, then register the implementation as a singleton; verify DI resolves the service.
- [x] 1.2 Inject the lifetime service into `LaunchWindowViewModel` and `TrayIconController` and replace direct shutdown calls; verify the ViewModel Exit command calls the fake lifetime exactly once.

## 2. Verify Preserved Behavior

- [x] 2.1 Verify `App.OnExit` cleanup order and the legacy `MainWindow` path remain unchanged.
- [x] 2.2 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`; verify the full solution remains green.
- [x] 2.3 Run `openspec validate introduce-application-lifetime-service` and verify the change has no spec deltas.
