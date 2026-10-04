## MODIFIED Requirements

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
