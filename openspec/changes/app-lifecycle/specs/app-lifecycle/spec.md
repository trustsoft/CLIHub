## Purpose

Manages application initialization, startup, shutdown, single-instance enforcement, dependency-injection composition, and the AppData directory structure so the application behaves predictably whether launched once or repeatedly.

## ADDED Requirements

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
