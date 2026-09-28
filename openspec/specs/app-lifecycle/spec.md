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

The application SHALL configure a dependency-injection container at startup with all required services.

#### Scenario: Container configuration
- **WHEN** the application initializes
- **THEN** `IConfigService`, `IProjectService`, `IPluginManager`, and `IProcessLauncher` are registered as singletons and UI components resolve their dependencies from the container

#### Scenario: Service resolution
- **WHEN** a component requests a service from the container
- **THEN** the container provides the registered implementation with all constructor dependencies injected

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

The application SHALL release resources and clean up state during shutdown.

#### Scenario: Normal shutdown
- **WHEN** the user chooses Exit
- **THEN** the tray icon is disposed, the DI container is disposed, and the single-instance mutex is released

#### Scenario: Shutdown leaves no locked mutex
- **WHEN** the application has fully exited
- **THEN** a subsequent launch acquires the mutex as a fresh first instance

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
