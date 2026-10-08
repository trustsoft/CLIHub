# Startup and Shutdown

The observable lifecycle requirements are defined by the [`app-lifecycle` specification](../../openspec/specs/app-lifecycle/spec.md). This section records the current implementation order and failure policy as a baseline for future startup refactors; it does not introduce additional runtime requirements.

## Process Bootstrap

`Program.Main` runs `VelopackApp.Build().Run()` before constructing and running the WPF `App`. Velopack bootstrap hooks therefore run before application initialization.

## Application Startup Sequence

`App.OnStartup` performs steps 1–4 and delegates the remaining application sequence to `ApplicationBootstrapper`:

1. Calls the WPF base implementation.
2. Creates the `%APPDATA%\CLIHub\` data layout (`logs`, `plugins`, and `cache`).
3. Configures Serilog using the log-level preference read from `config.json`, then logs application startup.
4. Builds the service provider through `AddClIHubServices`, which composes Core services and WPF services.
5. Resolves the single-instance guard from the service provider. If another instance owns the mutex, sends it a best-effort activation signal, shuts down this process, and returns. The provider owns guard disposal on this path.
6. Seeds built-in plugin descriptors when the plugins directory is empty, then loads plugin descriptors.
7. Loads preferences, applies the default process runtime, and refreshes the per-user Windows Run registration from `StartWithWindows`.
8. Starts `ApplicationSession`, which creates the startup UI and owns workflow-state, download-outcome, update-request, and second-instance activation subscriptions. Download requests use the singleton `IUpdateWorkflow`; downloaded/failed outcomes are dispatched to the startup UI for tray notifications.
9. Assigns the session's launch window to `Application.MainWindow`.
10. Shows the launch window when `ShowWindowOnStartup` is enabled; otherwise keeps the application in the tray.
11. Parses and registers the configured global hotkey, falling back to the default combination for an invalid configured value.
12. Evaluates whether release notes should be shown or recorded for the current version.
13. Starts the update check without blocking startup when `CheckForUpdatesOnStartup` is enabled. `UpdateStartupCoordinator` calls the shared workflow, so a concurrent manual check joins the same operation.
14. Logs that startup is complete.

The implementation is in `src/CLIHub/Program.cs`, `src/CLIHub/App.xaml.cs`, `src/CLIHub/ApplicationBootstrapper.cs`, `src/CLIHub/ApplicationSession.cs`, and `src/CLIHub/ServiceRegistration.cs`; Core registrations are grouped in `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs`.

## Startup Failure Policy

| Operation | Current behavior on failure |
|---|---|
| Velopack bootstrap, directory creation, logger setup, service-provider creation/resolution | No application-level recovery boundary is present; an unhandled failure can prevent normal startup. |
| Signaling the first instance from a second process | Connection attempts are best-effort; failures are swallowed and the second process proceeds to shut down. |
| Plugin seeding | The seeder logs a warning and returns; startup continues to plugin loading. |
| Loading an individual plugin descriptor | The manager logs a warning and skips the descriptor that failed to load. |
| Applying the Windows Run registration during startup | The startup service reports failure without throwing; startup continues. |
| Invalid configured hotkey | A warning is logged and the default hotkey is used. If Windows will not register the combination, registration logs a warning and the application continues without that hotkey. |
| Release-notes startup check | Exceptions are logged as warnings; startup continues. |
| Startup update check | The operation runs asynchronously; exceptions are logged and do not block the already-running application. |

The failure behavior above describes the current implementation, not a recommendation that all startup operations remain best-effort. Changes to this policy must update this section and the relevant lifecycle requirements when user-observable behavior changes.

## Shutdown Sequence

`App.OnExit` logs shutdown and then disposes resources in this order:

1. Disposes `ApplicationSession`, which removes application-level event subscriptions before tracked operations are stopped.
2. Cancels tracked application operations and waits for them within the shutdown timeout.
3. Disposes the service provider. This unregisters the global hotkey, removes its window-message hook, disposes the tray icon, and disposes singleton persistence services, including `ConfigurationRepository` (which flushes pending configuration writes), the logo cache service (which saves dirty cache state), and `SingleInstanceGuard` (which stops its pipe server and releases the mutex).
4. Flushes and closes Serilog, then calls the WPF base implementation.

The order is implemented in `src/CLIHub/App.xaml.cs` and `src/CLIHub/ApplicationSession.cs`; persistence disposal behavior is implemented by `src/CLIHub.Core/Configuration/ConfigurationRepository.cs` and `src/CLIHub.Core/Infrastructure/Persistence/LogoCacheService.cs`.

The current shutdown sequence is not wrapped in a per-resource recovery boundary or `finally` block. An exception from a disposal step can therefore prevent later cleanup steps from running.
