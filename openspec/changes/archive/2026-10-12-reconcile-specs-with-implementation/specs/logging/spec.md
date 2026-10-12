## MODIFIED Requirements

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
