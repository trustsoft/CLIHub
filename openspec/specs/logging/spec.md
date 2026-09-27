# logging Specification

## Purpose

Provides structured logging to file with configurable levels, automatic rotation, and integration with Microsoft.Extensions.Logging for diagnostic traceability.

## Requirements

### Requirement: File logging to AppData

The system SHALL write log files to the `%APPDATA%\CLIHub\logs\` directory.

#### Scenario: Log file location
- **WHEN** the application logs a message
- **THEN** the message is written to `%APPDATA%\CLIHub\logs\clihub-YYYYMMDD.log`

#### Scenario: Logs directory creation
- **WHEN** the application starts and the logs directory does not exist
- **THEN** the logs directory is created automatically

### Requirement: Daily log rotation

The system SHALL rotate log files daily and retain the last 7 days of logs.

#### Scenario: New day triggers rotation
- **WHEN** the date changes during application runtime
- **THEN** a new log file is created for the current date

#### Scenario: Old logs cleanup
- **WHEN** more than 7 daily log files exist
- **THEN** log files older than the retention limit are automatically deleted

### Requirement: Structured logging format

The system SHALL use structured logging with timestamp, level, and message.

#### Scenario: Log entry format
- **WHEN** a log message is written
- **THEN** the entry includes a timestamp, a level token, and the message

#### Scenario: Exception logging
- **WHEN** an exception is logged
- **THEN** the exception details and stack trace are included after the message

### Requirement: Configurable log levels

The system SHALL support standard log levels (Debug, Information, Warning, Error).

#### Scenario: Debug level enabled
- **WHEN** the minimum level is Debug
- **THEN** Debug and higher messages are written to file

#### Scenario: Information level configured
- **WHEN** the minimum level is Information
- **THEN** Information, Warning, and Error messages are written and Debug messages are suppressed

### Requirement: Framework noise filtering

The system SHALL suppress low-priority framework logs (`Microsoft.*`, `System.*`) by default.

#### Scenario: Framework log filtering
- **WHEN** `Microsoft.Extensions` or `System` libraries log below Warning
- **THEN** those messages are suppressed from the log file

### Requirement: Service logging integration

The system SHALL make `ILogger<T>` available to application services through dependency injection.

#### Scenario: Service receives a logger
- **WHEN** a service is constructed by the DI container
- **THEN** it receives an `ILogger<T>` writing to the same Serilog pipeline

#### Scenario: Service logs an event
- **WHEN** a service logs via `ILogger<T>`
- **THEN** the entry appears in the current day's log file with the service category name

### Requirement: Clean logger shutdown

The system SHALL flush and close the logger on application exit.

#### Scenario: Shutdown flushes logs
- **WHEN** the application exits normally
- **THEN** buffered log entries are flushed to disk before the process ends
