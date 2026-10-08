# Startup and Shutdown

The observable lifecycle requirements are defined by the [`app-lifecycle` specification](../../openspec/specs/app-lifecycle/spec.md). This section records the current implementation order and failure policy as a baseline for future startup refactors; it does not introduce additional runtime requirements.

## Process Bootstrap

`Program.Main` runs `VelopackApp.Build().Run()` before constructing and running the WPF `App`. Velopack bootstrap hooks therefore run before application initialization.

## Application Startup Sequence

`App.OnStartup` delegates to `ApplicationHost.Start`, which prepares the environment, builds the DI container, and delegates application initialization to `ApplicationBootstrapper`. The bootstrapper then coordinates startup through specialized coordinators:

1. **ApplicationHost** (WPF lifecycle boundary):
   - Calls the WPF base implementation.
   - Creates the `%APPDATA%\CLIHub\` data layout (`logs`, `plugins`, and `cache`).
   - Configures Serilog using the log-level preference read from `config.json`, then logs application startup.
   - Builds the service provider through `AddClIHubServices`, which composes Core services and WPF services.
   - Delegates to `ApplicationBootstrapper.Start`.

2. **InstanceCoordinator** (single-instance detection):
   - Resolves the single-instance guard from the service provider.
   - If another instance owns the mutex, sends it a best-effort activation signal, shuts down this process, and returns `InstanceStatus.SecondInstance`.
   - If this is the first instance, returns `InstanceStatus.FirstInstance` and startup continues.
   - The provider owns guard disposal.

3. **StartupStateLoader** (required state initialization):
   - Seeds built-in plugin descriptors when the plugins directory is empty, then loads plugin descriptors.
   - Loads preferences, applies the default process runtime, and refreshes the per-user Windows Run registration from `StartWithWindows`.
   - Returns immutable `StartupState` containing the loaded preferences.

4. **ApplicationBootstrapper** (session and hotkey setup):
   - Starts `ApplicationSession`, which creates the startup UI and owns workflow-state, download-outcome, update-request, and second-instance activation subscriptions. Download requests use the singleton `IUpdateWorkflow`; downloaded/failed outcomes are dispatched to the startup UI for tray notifications.
   - Assigns the session's launch window to `Application.MainWindow`.
   - Shows the launch window when `ShowWindowOnStartup` is enabled; otherwise keeps the application in the tray.
   - Parses and registers the configured global hotkey, falling back to the default combination for an invalid configured value.
   - Delegates optional operations to `OptionalStartupCoordinator`.

5. **OptionalStartupCoordinator** (best-effort operations):
   - Evaluates whether release notes should be shown or recorded for the current version.
   - Starts the update check without blocking startup when `CheckForUpdatesOnStartup` is enabled. `UpdateStartupCoordinator` calls the shared workflow, so a concurrent manual check joins the same operation.
   - Logs warnings on failure; does not throw.

6. ApplicationBootstrapper logs that startup is complete.

The implementation is in:
- `src/CLIHub/Program.cs` — Velopack entry point
- `src/CLIHub/App.xaml.cs` — WPF lifecycle delegation to `ApplicationHost`
- `src/CLIHub/ApplicationHost.cs` — environment setup and DI composition
- `src/CLIHub/ApplicationBootstrapper.cs` — startup orchestration
- `src/CLIHub/InstanceCoordinator.cs` — single-instance coordination
- `src/CLIHub/StartupStateLoader.cs` — plugin and preferences initialization
- `src/CLIHub/OptionalStartupCoordinator.cs` — release notes and update check
- `src/CLIHub/ApplicationSession.cs` — UI creation and subscription ownership
- `src/CLIHub/ServiceRegistration.cs` — WPF service registration

Core registrations are grouped in `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs`.

## Startup Failure Policy

| Operation | Coordinator | Current behavior on failure |
|---|---|---|
| Velopack bootstrap, directory creation, logger setup, service-provider creation/resolution | ApplicationHost | No application-level recovery boundary is present; an unhandled failure can prevent normal startup. |
| Signaling the first instance from a second process | InstanceCoordinator | Connection attempts are best-effort; failures are swallowed and the second process proceeds to shut down. |
| Plugin seeding | StartupStateLoader | The seeder logs a warning and returns; startup continues to plugin loading. |
| Loading an individual plugin descriptor | StartupStateLoader | The manager logs a warning and skips the descriptor that failed to load. |
| Applying the Windows Run registration during startup | StartupStateLoader | The startup service reports failure without throwing; startup continues. |
| Invalid configured hotkey | ApplicationBootstrapper | A warning is logged and the default hotkey is used. If Windows will not register the combination, registration logs a warning and the application continues without that hotkey. |
| Release-notes startup check | OptionalStartupCoordinator | Exceptions are logged as warnings; startup continues. |
| Startup update check | OptionalStartupCoordinator | The operation runs asynchronously; exceptions are logged and do not block the already-running application. |

The failure behavior above describes the current implementation, not a recommendation that all startup operations remain best-effort. Changes to this policy must update this section and the relevant lifecycle requirements when user-observable behavior changes.

## Shutdown Sequence

`App.OnExit` delegates to `ApplicationHost.Shutdown`, which disposes resources in this order:

1. Logs shutdown.
2. Disposes `ApplicationSession`, which removes application-level event subscriptions before tracked operations are stopped.
3. Cancels tracked application operations and waits for them within the shutdown timeout.
4. Disposes the service provider. This unregisters the global hotkey, removes its window-message hook, disposes the tray icon, and disposes singleton persistence services, including `ConfigurationRepository` (which flushes pending configuration writes), the logo cache service (which saves dirty cache state), and `SingleInstanceGuard` (which stops its pipe server and releases the mutex).
5. Flushes and closes Serilog, then calls the WPF base implementation.

The order is implemented in:
- `src/CLIHub/App.xaml.cs` — WPF lifecycle delegation to `ApplicationHost`
- `src/CLIHub/ApplicationHost.cs` — shutdown sequence orchestration
- `src/CLIHub/ApplicationSession.cs` — subscription removal

Persistence disposal behavior is implemented by `src/CLIHub.Core/Configuration/ConfigurationRepository.cs` and `src/CLIHub.Core/Infrastructure/Persistence/LogoCacheService.cs`.

The current shutdown sequence is not wrapped in a per-resource recovery boundary or `finally` block. An exception from a disposal step can therefore prevent later cleanup steps from running.
