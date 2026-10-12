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

The system SHALL support the standard log levels `Verbose`, `Debug`, `Information`, `Warning`, `Error`, and `Fatal`.

#### Scenario: Debug level enabled
- **WHEN** the minimum level is Debug
- **THEN** Debug and higher messages are written to file

#### Scenario: Information level configured
- **WHEN** the minimum level is Information
- **THEN** Information, Warning, and Error messages are written and Debug messages are suppressed

#### Scenario: Verbose level enabled
- **WHEN** the minimum level is Verbose
- **THEN** Verbose, Debug, Information, Warning, Error, and Fatal messages are written to file

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

### Requirement: Configurable log retention and file size limit

The system SHALL support configurable log retention (number of retained days) and an optional per-file size limit, applied when the logger is created at startup.

#### Scenario: Default retention and size limit
- **WHEN** no log retention or size-limit preference is stored
- **THEN** log files are retained for 7 days and no per-file size limit is applied

#### Scenario: Configured retention applied
- **WHEN** a log retention preference is stored
- **THEN** log files beyond the configured retention are removed

#### Scenario: Configured size limit applied
- **WHEN** a per-file size limit preference greater than zero is stored
- **THEN** a log file that reaches the limit rolls over to a new file

#### Scenario: Zero size limit means unlimited
- **WHEN** the per-file size limit preference is zero
- **THEN** no size-based rollover is applied

### Requirement: Log file location and open folder

The system SHALL surface the current log file location to the user and let them copy it and open the log folder.

#### Scenario: Location shown
- **WHEN** the Logs page is shown
- **THEN** the current log file path under `%APPDATA%\CLIHub\logs\` is displayed

#### Scenario: Copy path
- **WHEN** the user chooses to copy the log path
- **THEN** the current log file path is placed on the clipboard

#### Scenario: Open log folder
- **WHEN** the user chooses "Open log folder"
- **THEN** the logs directory is opened in the system file browser
