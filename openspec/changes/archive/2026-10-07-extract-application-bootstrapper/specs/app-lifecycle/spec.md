## ADDED Requirements

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
