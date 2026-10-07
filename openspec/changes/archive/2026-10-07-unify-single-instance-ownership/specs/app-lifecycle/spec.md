## ADDED Requirements

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
