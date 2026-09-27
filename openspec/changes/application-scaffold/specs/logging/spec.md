## Purpose

Provides structured logging to file with configurable levels, automatic rotation, and integration with Microsoft.Extensions.Logging for diagnostic traceability.

## ADDED Requirements

### Requirement: File logging to AppData

The system SHALL write log files to %APPDATA%\CLIHub\logs\ directory.

#### Scenario: Log file location
- **WHEN** the application logs a message
- **THEN** the message is written to %APPDATA%\CLIHub\logs\clihub-YYYYMMDD.log

#### Scenario: Logs directory creation
- **WHEN** the application starts and logs directory does not exist
- **THEN** the logs directory is created automatically

### Requirement: Daily log rotation

The system SHALL rotate log files daily and retain the last 7 days of logs.

#### Scenario: New day triggers rotation
- **WHEN** the date changes during application runtime
- **THEN** a new log file is created for the current date

#### Scenario: Old logs cleanup
- **WHEN** log rotation occurs
- **THEN** log files older than 7 days are automatically deleted

### Requirement: Structured logging format

The system SHALL use structured logging with timestamp, level, and message.

#### Scenario: Log entry format
- **WHEN** a log message is written
- **THEN** the entry includes [timestamp level] message format

#### Scenario: Exception logging
- **WHEN** an exception is logged
- **THEN** the stack trace is included on separate lines after the message

### Requirement: Configurable log levels

The system SHALL support standard log levels (Debug, Info, Warning, Error).

#### Scenario: Debug level logging
- **WHEN** Debug level is enabled
- **THEN** all log levels are written to file

#### Scenario: Info level logging
- **WHEN** Info level is configured
- **THEN** Info, Warning, and Error messages are written, Debug messages are suppressed

### Requirement: Framework noise filtering

The system SHALL suppress low-priority framework logs (Microsoft.*, System.*) by default.

#### Scenario: Framework log filtering
- **WHEN** Microsoft.Extensions or System libraries log at Debug or Info level
- **THEN** these messages are suppressed unless explicitly enabled
