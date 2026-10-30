# Startup and Shutdown

The observable lifecycle requirements are defined by the [`app-lifecycle` specification](../../openspec/specs/app-lifecycle/spec.md). This section records the current implementation order and failure policy as a baseline for future startup refactors; it does not introduce additional runtime requirements.

## Process Bootstrap

`Program.Main` runs `VelopackApp.Build().Run()` before constructing and running the WPF `App`. Velopack bootstrap hooks therefore run before application initialization.

## Application Startup Sequence

Before the host starts, `App.OnStartup` calls the WPF base implementation, applies the stored theme through `PaletteSwitcher` and the stored accent through `AccentApplier` to the application resources, and then delegates to `ApplicationHost.Start`. The first window is therefore created with the persisted appearance. `ApplicationHost.Start` prepares the environment, builds the DI container, and delegates application initialization to `ApplicationBootstrapper`. The bootstrapper then coordinates startup through specialized coordinators:

1. **ApplicationHost** (WPF lifecycle boundary):
   - Acquires the single-instance guard as the first host action, before any environment setup. If another instance owns the mutex, the host signals activation to the running instance, requests WPF shutdown, and returns; no data directories, logging, or services are initialized for the duplicate process, and no tray icon or window is created.
   - Creates the `%APPDATA%\CLIHub\` data layout (`logs`, `plugins`, and `cache`).
   - Configures Serilog using the log-level preference read from `config.json`, then logs application startup.
   - Builds the service provider through `AddClIHubServices`, which composes Core services and WPF services; the pre-acquired guard is registered into the composition so the provider owns guard disposal.
   - Delegates to `ApplicationBootstrapper.Start`.

2. **StartupStateLoader** (required state initialization):
   - Seeds built-in plugin descriptors when the plugins directory is empty, then loads plugin descriptors.
   - Loads preferences, applies the default process runtime, and refreshes the per-user Windows Run registration from `StartWithWindows`.
   - Returns immutable `StartupState` containing the loaded preferences.

3. **ApplicationBootstrapper** (session and hotkey setup):
   - Starts `ApplicationSession`, which delegates shell creation to `ShellCoordinator`. The coordinator creates the startup UI (tray and launch window) and wires workflow-state, download-outcome, update-request, and second-instance activation subscriptions. Download requests use the singleton `IUpdateWorkflow`; downloaded/failed outcomes are dispatched to the startup UI for tray notifications. The session owns the coordinator's disposal lifecycle.
   - Assigns the session's launch window to `Application.MainWindow`.
   - Shows the launch window when `startMinimized` is disabled; otherwise keeps the application in the tray.
   - Parses and registers the configured global hotkey, falling back to the default combination for an invalid configured value.
   - Delegates optional operations to `OptionalStartupCoordinator`.

4. **OptionalStartupCoordinator** (best-effort operations):
   - Evaluates whether release notes should be shown or recorded for the current version.
   - Starts the update check without blocking startup when `CheckForUpdatesOnStartup` is enabled. `UpdateStartupCoordinator` calls the shared workflow, so a concurrent manual check joins the same operation.
   - Logs warnings on failure; does not throw.

5. **ApplicationBootstrapper** (background scanning):
   - Starts the project scanner (`IProjectScanner.Start`), which applies the scanning preferences loaded by `StartupStateLoader`; the DI container disposes it on shutdown.

6. ApplicationBootstrapper logs that startup is complete.

Because theme and accent application happens before the host acquires the single-instance guard, a duplicate launch applies them to its own in-memory resources; it still creates no data directories, logging, services, tray icon, or window, and writes nothing to `%APPDATA%`.

The implementation is in:
- `src/CLIHub/Program.cs` — Velopack entry point
- `src/CLIHub/App.xaml.cs` — WPF lifecycle delegation to `ApplicationHost`
- `src/CLIHub/Startup/ApplicationHost.cs` — environment setup and DI composition
- `src/CLIHub/Startup/ApplicationBootstrapper.cs` — startup orchestration
- `src/CLIHub/Startup/StartupStateLoader.cs` — plugin and preferences initialization
- `src/CLIHub/Startup/OptionalStartupCoordinator.cs` — release notes and update check
- `src/CLIHub/Lifetime/ApplicationSession.cs` — session lifecycle and coordinator ownership
- `src/CLIHub/Startup/ShellCoordinator.cs` — shell creation and event wiring
- `src/CLIHub/Themes/PaletteSwitcher.cs` — stored-theme application before the first window
- `src/CLIHub/Settings/AccentApplier.cs` — stored-accent application before the first window
- `src/CLIHub/Composition/ServiceRegistration.cs` — WPF service registration

Core registrations are grouped in `src/CLIHub.Core/Composition/ServiceCollectionExtensions.cs`.

## Startup Failure Policy

| Operation | Coordinator | Current behavior on failure |
|---|---|---|
| Acquiring the single-instance guard at host startup | ApplicationHost | A duplicate launch signals activation best-effort and exits silently; logging is not configured before guard acquisition, so the duplicate produces no log entries and no AppData artifacts. An exception after acquisition but before service-provider construction disposes the guard and propagates. |
| Velopack bootstrap, directory creation, logger setup, service-provider creation/resolution | ApplicationHost | No application-level recovery boundary is present; an unhandled failure can prevent normal startup. |
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
2. Disposes `ApplicationSession`, which disposes `ShellCoordinator`. The coordinator removes application-level event subscriptions before tracked operations are stopped.
3. Cancels tracked application operations and waits for them within the shutdown timeout.
4. Disposes the service provider. This unregisters the global hotkey, removes its window-message hook, disposes the tray icon, and disposes singleton persistence services, including `ConfigurationRepository` (which flushes pending configuration writes), the logo cache service (which saves dirty cache state), and `SingleInstanceGuard` (which stops its pipe server and releases the mutex).
5. Flushes and closes Serilog, then calls the WPF base implementation.

The order is implemented in:
- `src/CLIHub/App.xaml.cs` — WPF lifecycle delegation to `ApplicationHost`
- `src/CLIHub/Startup/ApplicationHost.cs` — shutdown sequence orchestration
- `src/CLIHub/Lifetime/ApplicationSession.cs` — coordinator disposal
- `src/CLIHub/Startup/ShellCoordinator.cs` — subscription removal

Persistence disposal behavior is implemented by `src/CLIHub.Core/Configuration/ConfigurationRepository.cs` and `src/CLIHub.Core/Infrastructure/Persistence/LogoCacheService.cs`.

The session disposal and the tracked-operation stop are individually guarded: a failure is logged as a
warning and shutdown continues. The service-provider disposal and the Serilog flush are not wrapped, so an
exception there can still prevent later cleanup steps from running.
