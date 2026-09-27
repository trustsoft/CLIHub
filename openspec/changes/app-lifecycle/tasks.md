## 1. Core Lifecycle Primitives

- [ ] 1.1 Add `SingleInstanceGuard` in `src/CLIHub.Core/Services/SingleInstanceGuard.cs` using a named `Mutex` (`Local\CLIHub.SingleInstance`), exposing `IsFirstInstance`, `ActivationRequested` event, and `IDisposable`, and verify `CLIHub.Core` compiles
- [ ] 1.2 Implement the named-pipe server loop in `SingleInstanceGuard` (first instance) that raises `ActivationRequested` on receiving a signal, and the client `SignalActivation()` (second instance) with a short timeout and one retry, and verify unit test: two guards → second `IsFirstInstance == false`
- [ ] 1.3 Add `DirectoryInitializer.EnsureAppDataLayout()` in `src/CLIHub.Core` creating `%APPDATA%\CLIHub\` with `logs\`, `plugins\`, `cache\`, and verify unit test creates a missing tree and is idempotent

## 2. DI Registration

- [ ] 2.1 Add `src/CLIHub/ServiceRegistration.cs` with `AddClIHubServices` registering `IConfigService`, `IProjectService` (with `DefaultLogoPath`), `IPluginManager`, `IProcessLauncher`, `SingleInstanceGuard`, `TrayIconController`, and `MainWindow` as singletons, and verify the container resolves each service
- [ ] 2.2 Verify `IConfigService` resolves as a singleton (same instance across resolutions) with a unit test

## 3. Tray Icon Controller

- [ ] 3.1 Add `TrayIconController` in `src/CLIHub` owning the `TaskbarIcon` (icon, tooltip, `ForceCreate()`) and building the context menu that currently lives in `App.xaml.cs` (current project, Recent Projects, Add Project..., Show CLIHub, Exit), and verify the menu still builds from injected services
- [ ] 3.2 Expose `ShowMainWindow()` on the controller and rebuild the menu on `IProjectService.ProjectsChanged`, and verify wiring compiles

## 4. Application Startup/Shutdown Wiring

- [ ] 4.1 Rewrite `App.OnStartup`: create `SingleInstanceGuard`; if not first instance call `SignalActivation()` and shut down; else ensure AppData layout, build the DI container, resolve the tray controller, and show the main window, and verify the app starts
- [ ] 4.2 Subscribe `SingleInstanceGuard.ActivationRequested` to marshal to the UI thread and call `TrayIconController.ShowMainWindow()`, and verify a second launch surfaces the existing window
- [ ] 4.3 Update `OnExit` to dispose the tray controller, dispose the DI container, and dispose the guard, and verify no mutex is left held after exit
- [ ] 4.4 Remove `StartupUri` from `src/CLIHub/App.xaml` (keep `ShutdownMode="OnExplicitShutdown"`), and verify no second window is created

## 5. Constructor Injection

- [ ] 5.1 Change `MainWindow` to take `IPluginManager`, `IProcessLauncher`, and `IProjectService` via constructor injection and remove its use of `AppServices`, and verify the solution compiles
- [ ] 5.2 Delete `src/CLIHub/AppServices.cs` and verify no references remain

## 6. Tests

- [ ] 6.1 Add `SingleInstanceGuardTests` verifying first/second instance detection and `DirectoryInitializer` layout creation, and verify `dotnet test` passes
- [ ] 6.2 Verify existing `ProjectServiceTests` still pass after the DI/composition changes

## 7. Verification

- [ ] 7.1 Build the full solution with 0 warnings/errors
- [ ] 7.2 Manually verify: launching CLIHub twice results in a single tray icon and the existing window is activated
- [ ] 7.3 Manually verify: Exit releases the mutex and relaunching starts normally
- [ ] 7.4 Manually verify: `%APPDATA%\CLIHub\` contains `logs\`, `plugins\`, `cache\`; delete a subfolder and confirm it is recreated on next start
