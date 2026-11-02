## MODIFIED Requirements

### Requirement: Single instance enforcement

The application SHALL ensure only one instance runs at a time using a named mutex. The first instance SHALL create its activation named pipe before the guard constructor returns, so the pipe is listening before any duplicate instance can attempt to signal it. A duplicate instance SHALL retry signaling within a bounded deadline (~2 seconds) to account for server readiness delays. An activation signal that cannot be delivered SHALL fail silently without blocking the duplicate process or crashing either instance.

#### Scenario: First instance starts successfully
- **WHEN** the application starts and no other instance holds the mutex
- **THEN** the application acquires the mutex and continues startup
- **AND** the activation named pipe is created and listening before the guard constructor returns

#### Scenario: Second instance detected
- **WHEN** the application starts and another instance already holds the mutex
- **THEN** the second instance signals the first instance and exits without showing its own tray icon or window

#### Scenario: First instance receives activation signal
- **WHEN** a second instance signals the running instance
- **THEN** the running instance shows and activates its launch window

#### Scenario: Activation signal is retried on server readiness delay
- **WHEN** a duplicate instance attempts to signal the first instance and the server is not yet ready to accept the connection
- **THEN** the duplicate retries the connection within a bounded deadline (~2 seconds total) using exponential backoff
- **AND** the signal succeeds once the server becomes ready within that window

#### Scenario: Activation signal fails silently after deadline
- **WHEN** a duplicate instance cannot deliver the activation signal within the retry deadline
- **THEN** the duplicate exits silently without blocking indefinitely, without throwing an exception, and without crashing either process

#### Scenario: Server accepts repeated activations
- **WHEN** the first instance's activation server completes serving one client
- **THEN** the server recreates its named pipe and waits for the next connection
- **AND** subsequent duplicate launches are served without restarting the first instance
