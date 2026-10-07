## ADDED Requirements

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

## MODIFIED Requirements

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
