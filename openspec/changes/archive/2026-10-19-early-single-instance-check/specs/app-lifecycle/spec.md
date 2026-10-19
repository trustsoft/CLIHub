## MODIFIED Requirements

### Requirement: Single instance resource ownership

The application SHALL acquire the single-instance guard at the beginning of host startup, before data-directory creation, log configuration, and service-provider construction. The application SHALL register exactly one `SingleInstanceGuard` singleton instance through the application composition root, and the service provider SHALL own disposal of the guard and its named mutex and activation-pipe resources.

#### Scenario: Production registration has one owner

- **WHEN** the WPF service collection is built and the guard is resolved more than once
- **THEN** every resolution returns the same pre-acquired `SingleInstanceGuard` instance
- **AND** the startup path after guard acquisition does not construct or dispose a separate guard instance

#### Scenario: Duplicate launch exits before environment setup

- **WHEN** host startup acquires the guard and another instance already owns the mutex
- **THEN** the process signals the running instance to activate and exits
- **AND** the duplicate process does not create AppData directories, does not configure logging, does not build the service provider, and does not run application initialization
- **AND** the duplicate process shows no tray icon or application window

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

- **WHEN** the application acquired the single-instance guard as the first instance
- **THEN** the bootstrapper executes the first-instance startup steps in the defined order
- **AND** the application exposes the tray and launch-window behavior configured by preferences

#### Scenario: Second instance does not initialize the application

- **WHEN** host startup determines that another instance already owns the mutex
- **THEN** the second process signals activation to the running instance and exits without running the bootstrapper
- **AND** no plugins, preferences, tray, hotkey, release notes, or updates are initialized for the second instance

#### Scenario: Fatal startup failure

- **WHEN** a required first-instance startup step fails before the application is usable
- **THEN** the bootstrapper logs the failure and requests application shutdown
- **AND** it does not report startup as completed

#### Scenario: Best-effort startup failure

- **WHEN** an optional startup operation such as release-notes evaluation or the update check fails
- **THEN** the bootstrapper logs the failure according to that operation's existing error policy
- **AND** the tray and launch window remain available when required startup has completed
