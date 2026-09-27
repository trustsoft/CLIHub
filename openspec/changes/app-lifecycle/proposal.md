## Why

The MVP used a manual composition root and had no single-instance guard, so launching CLIHub twice produced two tray icons and two windows (observed during testing). Services are also hand-wired, which will not scale as more services arrive. This change establishes reliable startup/shutdown: one instance, DI-managed services, and a predictable AppData layout.

## What Changes

- Enforce a single running instance using a named mutex; a second launch signals the first to show its window and then exits
- Add inter-process signaling via a named pipe so the running instance surfaces its UI on a duplicate launch
- Introduce a dependency-injection container (`Microsoft.Extensions.DependencyInjection`) and register `IConfigService`, `IProjectService`, `IPluginManager`, `IProcessLauncher`, and the tray/window components
- Replace the static `AppServices` composition root with container resolution
- Initialize the `%APPDATA%\CLIHub\` directory structure (`logs\`, `plugins\`, `cache\`) at startup, recreating anything missing
- Release the mutex and dispose the container on graceful shutdown

## Capabilities

### New Capabilities

- `app-lifecycle`: application startup, single-instance enforcement, dependency-injection composition, AppData initialization, and graceful shutdown

### Modified Capabilities

<!-- None: project-management requirements are unchanged; this only changes how services are constructed. -->

## Impact

**New code:**
- `src/CLIHub.Core/Services/SingleInstanceGuard.cs` — mutex + named-pipe signaling (no WPF dependency)
- `src/CLIHub/ServiceRegistration.cs` — DI container registration
- `src/CLIHub/AppDataInitializer.cs` (or method on a startup service) — directory structure

**Modified code:**
- `src/CLIHub/App.xaml.cs` — single-instance check first; build container; resolve tray icon and main window from container; dispose on exit
- `src/CLIHub/App.xaml` — remove `StartupUri` so the main window is resolved from DI (shown/hidden deliberately), keep `ShutdownMode="OnExplicitShutdown"`
- `src/CLIHub/Windows/MainWindow.xaml.cs` — constructor takes injected services instead of reading `AppServices`
- Remove `src/CLIHub/AppServices.cs`

**Dependencies:**
- `Microsoft.Extensions.DependencyInjection` already referenced in `CLIHub`; no new packages

**Behavioral change:** launching a second instance no longer opens a second tray icon; it activates the existing window and exits.

**No breaking changes** to `project-management` behavior.
