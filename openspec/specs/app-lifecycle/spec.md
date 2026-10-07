# app-lifecycle Specification

## Purpose

Manages application initialization, startup, shutdown, single-instance enforcement, dependency-injection composition, and the AppData directory structure so the application behaves predictably whether launched once or repeatedly.

## Requirements

### Requirement: Single instance enforcement

The application SHALL ensure only one instance runs at a time using a named mutex.

#### Scenario: First instance starts successfully
- **WHEN** the application starts and no other instance holds the mutex
- **THEN** the application acquires the mutex and continues startup

#### Scenario: Second instance detected
- **WHEN** the application starts and another instance already holds the mutex
- **THEN** the second instance signals the first instance and exits without showing its own tray icon or window

#### Scenario: First instance receives activation signal
- **WHEN** a second instance signals the running instance
- **THEN** the running instance shows and activates its main window

### Requirement: Dependency injection container

The application SHALL configure a dependency-injection container at startup with all required services. Each production Core service registered for constructor injection SHALL expose one unambiguous public production constructor.

#### Scenario: Container configuration
- **WHEN** the application initializes
- **THEN** `IConfigurationRepository`, `IProjectStateStore`, `IPreferencesStore`, `IProjectService`, `IPluginManager`, `IProcessLauncher`, `IInteractiveProcessRunner`, `IProcessOutputRunner`, `IAgentCommandService`, `IAgentDetectionService`, and `IAgentVersionService` are registered as singletons and UI components resolve their dependencies from the container; UI components that need only preferences use `IPreferencesStore` rather than the full configuration document

#### Scenario: Service resolution
- **WHEN** a component requests a registered Core service from the container
- **THEN** the container provides the registered implementation with all constructor dependencies injected without an ambiguous-constructor exception

#### Scenario: Application startup after service resolution
- **WHEN** the application initializes its service provider and resolves the tray controller
- **THEN** service resolution completes, the tray controller is created, and startup proceeds to the normal single-instance/window initialization path

#### Scenario: No static service locator
- **WHEN** a class needs a service
- **THEN** it receives it through constructor injection rather than a static accessor

### Requirement: Application data directory initialization

The application SHALL create the required directory structure in %APPDATA% on startup.

#### Scenario: First run directory creation
- **WHEN** the application starts and `%APPDATA%\CLIHub\` does not exist
- **THEN** the directory structure is created (`logs\`, `plugins\`, `cache\`)

#### Scenario: Missing directories restored
- **WHEN** the application starts and one or more required subdirectories are missing
- **THEN** the missing subdirectories are recreated automatically

### Requirement: Graceful shutdown

The application SHALL release resources and clean up state during shutdown, including pending configuration writes and application-scoped asynchronous operations.

#### Scenario: Normal shutdown

- **WHEN** the user chooses Exit
- **THEN** application-scoped operations are cancelled and awaited within the shutdown bound
- **AND** the tray icon is disposed, the DI container is disposed, and the single-instance mutex is released

#### Scenario: Shutdown leaves no locked mutex

- **WHEN** the application has fully exited
- **THEN** a subsequent launch acquires the mutex as a fresh first instance

#### Scenario: Pending configuration writes are flushed

- **WHEN** the application exits with configuration changes still pending to be written
- **THEN** the pending changes are written to `config.json` before the process exits

### Requirement: Start with Windows

The application SHALL let the user enable or disable launching automatically when Windows starts, using the current user's Run registration.

#### Scenario: Enable autostart
- **WHEN** the user enables "Start with Windows" and saves
- **THEN** a per-user Run entry for the current executable is created so CLIHub starts at Windows sign-in

#### Scenario: Disable autostart
- **WHEN** the user disables "Start with Windows" and saves
- **THEN** the per-user Run entry is removed

#### Scenario: Registration reflects current state
- **WHEN** the Settings window opens
- **THEN** the "Start with Windows" toggle reflects whether the Run entry currently exists

#### Scenario: Registration refreshed on startup
- **WHEN** the application starts with "Start with Windows" enabled
- **THEN** the Run entry is updated to point at the current executable path

### Requirement: Startup window visibility

The application SHALL show or hide its main window at startup according to a preference, for both a manual launch and a launch by Windows.

#### Scenario: Show window on startup
- **WHEN** the application starts and "Show window on startup" is enabled
- **THEN** the main window is shown

#### Scenario: Start in the system tray
- **WHEN** the application starts and "Show window on startup" is disabled
- **THEN** the main window is not shown and the application runs in the system tray, still reachable from the tray, the hotkey, and a subsequent launch

### Requirement: Launch window popup shell

The launch window SHALL behave as a popup shell that stays out of the way and can be pinned open.

#### Scenario: No taskbar entry
- **WHEN** the launch window is shown
- **THEN** it does not appear as a taskbar button

#### Scenario: Always on top
- **WHEN** the launch window is shown while other applications are open
- **THEN** it is displayed above non-topmost windows

#### Scenario: Hides when focus is lost
- **WHEN** the launch window is visible, is not pinned, and the user activates another application
- **THEN** the launch window hides instead of staying behind that application

#### Scenario: Pinned window stays visible
- **WHEN** the launch window is pinned and the user activates another application
- **THEN** the launch window stays visible

#### Scenario: Escape hides the window
- **WHEN** the launch window is visible and the user presses Escape
- **THEN** the window hides and the application keeps running

#### Scenario: Pin control in the footer
- **WHEN** the launch window is shown
- **THEN** the footer exposes a Pin control whose state reflects whether the window is pinned

#### Scenario: Pin state persists
- **WHEN** the user pins (or unpins) the window and the application restarts
- **THEN** the window starts in the stored pinned or unpinned state

#### Scenario: Modal dialogs do not hide the window
- **WHEN** a folder picker or a confirmation dialog opened from the launch window is active
- **THEN** losing focus to that dialog does not hide the launch window

#### Scenario: Positioned on the pointer's monitor
- **WHEN** the launch window is shown from the tray, the global hotkey, a second-instance activation, or startup
- **THEN** it is centered in the work area of the monitor that contains the pointer at that moment

### Requirement: Single instance resource ownership

The application SHALL register and resolve exactly one `SingleInstanceGuard` singleton through the application composition root. The service provider SHALL own disposal of the guard and its named mutex and activation-pipe resources.

#### Scenario: Production registration has one owner

- **WHEN** the WPF service collection is built and the guard is resolved more than once
- **THEN** every resolution returns the same `SingleInstanceGuard` instance
- **AND** the application startup path does not construct or dispose a separate guard instance

#### Scenario: Second instance exits through the provider lifecycle

- **WHEN** startup resolves a guard that is not the first instance
- **THEN** the application signals the first instance and requests shutdown
- **AND** the service provider disposes the guard during normal application exit

#### Scenario: First instance releases the mutex through provider disposal

- **WHEN** the provider owning a first-instance guard is disposed
- **THEN** the guard releases its named mutex and signal resources
- **AND** a subsequent guard can acquire the mutex as the first instance

### Requirement: Application startup orchestration

The application SHALL execute first-instance startup through one application bootstrapper boundary after WPF initialization. The bootstrapper SHALL preserve the existing startup order: initialize plugins, load and apply preferences, create the tray host, connect update notifications and activation handling, resolve and optionally show the launch window, register the global hotkey, evaluate release notes, and start the optional update check.

#### Scenario: First instance completes startup

- **WHEN** the application acquires the single-instance guard
- **THEN** the bootstrapper executes the first-instance startup steps in the defined order
- **AND** the application exposes the tray and launch-window behavior configured by preferences

#### Scenario: Second instance does not initialize the application

- **WHEN** the bootstrapper resolves a guard that reports another instance is already running
- **THEN** it signals activation to the running instance and requests application shutdown
- **AND** it does not initialize plugins, preferences, tray, hotkey, release notes, or updates for the second instance

#### Scenario: Fatal startup failure

- **WHEN** a required first-instance startup step fails before the application is usable
- **THEN** the bootstrapper logs the failure and requests application shutdown
- **AND** it does not report startup as completed

#### Scenario: Best-effort startup failure

- **WHEN** an optional startup operation such as release-notes evaluation or the update check fails
- **THEN** the bootstrapper logs the failure according to that operation's existing error policy
- **AND** the tray and launch window remain available when required startup has completed

### Requirement: WPF lifecycle adapter boundary

The WPF application entry point SHALL delegate application startup orchestration to the bootstrapper and SHALL not directly coordinate individual plugin, preference, tray, hotkey, release-notes, or update services.

#### Scenario: WPF startup delegates to the bootstrapper

- **WHEN** WPF raises the startup event
- **THEN** the entry point prepares the application environment and invokes the bootstrapper with the WPF dispatcher-facing callbacks it needs
- **AND** the entry point does not duplicate the bootstrapper's startup ordering

#### Scenario: Shutdown preserves resource cleanup

- **WHEN** WPF raises the exit event after either successful or failed startup
- **THEN** the lifecycle adapter disposes application-owned resources and the service provider
- **AND** logging is flushed after shutdown cleanup

### Requirement: Application operation lifetime

The application SHALL provide one operation-lifetime boundary that owns a cancellation token for application-scoped asynchronous work and tracks operations started by startup, update, and UI workflows. A tracked operation SHALL observe cancellation, preserve its existing result and error semantics, and make unexpected exceptions observable through application logging.

#### Scenario: Operation receives application cancellation

- **WHEN** an application-scoped operation is started through the lifetime boundary
- **THEN** it receives the lifetime cancellation token
- **AND** shutdown cancellation requests the operation to stop

#### Scenario: Fire-and-forget operation is tracked

- **WHEN** a synchronous UI or event handler starts an asynchronous operation
- **THEN** the lifetime boundary records the task until it completes or is cancelled
- **AND** an unexpected exception is logged instead of becoming an unobserved task failure

#### Scenario: Completed operation is removed

- **WHEN** a tracked operation completes successfully, fails, or is cancelled
- **THEN** it no longer prevents application shutdown from completing

### Requirement: Controlled asynchronous shutdown

The application SHALL cancel application-scoped operations when shutdown begins and SHALL wait for tracked operations up to a bounded timeout before disposing the service provider. Cancellation and timeout SHALL be logged distinctly, and shutdown SHALL continue even when an operation does not finish within the bound.

#### Scenario: Shutdown cancels and waits for operations

- **WHEN** the application exits with tracked operations still running
- **THEN** the application requests cancellation and waits for their completion up to the configured bound
- **AND** completed operations are fully observed before service disposal

#### Scenario: Stuck operation does not block process exit indefinitely

- **WHEN** a tracked operation ignores cancellation beyond the shutdown bound
- **THEN** the application logs the timeout
- **AND** disposes the service provider and completes process shutdown without waiting indefinitely
