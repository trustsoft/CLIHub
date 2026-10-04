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
- **THEN** `IConfigService`, `IProjectService`, `IPluginManager`, `IProcessLauncher`, `IAgentCommandService`, `IAgentDetectionService`, and `IAgentVersionService` are registered as singletons and UI components resolve their dependencies from the container

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

The application SHALL release resources and clean up state during shutdown, including pending configuration writes.

#### Scenario: Normal shutdown
- **WHEN** the user chooses Exit
- **THEN** the tray icon is disposed, the DI container is disposed, and the single-instance mutex is released

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
