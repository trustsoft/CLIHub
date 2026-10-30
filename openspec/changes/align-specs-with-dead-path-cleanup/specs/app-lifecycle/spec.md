## REMOVED Requirements

### Requirement: Single instance resource ownership

## ADDED Requirements

### Requirement: Single instance ownership and disposal

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

#### Scenario: First instance releases the mutex through provider disposal

- **WHEN** the provider owning a first-instance guard is disposed
- **THEN** the guard releases its named mutex and signal resources
- **AND** a subsequent guard can acquire the mutex as the first instance
