## ADDED Requirements

### Requirement: Telemetry and Logs page options

The Telemetry & Logs page SHALL offer a log-level selector (Verbose, Debug, Information, Warning, Error), a retention-days field, a file-size-limit field, the current log file path with a Copy action, and an "Open log folder" action.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Telemetry & Logs"
- **THEN** it shows the log-level selector, the retention-days and file-size-limit fields, the current log path with a Copy action, and an "Open log folder" action

#### Scenario: Current values loaded
- **WHEN** the page opens
- **THEN** the log level and the retention/size fields show the stored values

#### Scenario: Log level applied on next start
- **WHEN** the user changes the log level and saves
- **THEN** the value is written to `config.json` and takes effect on the next application start

#### Scenario: Invalid retention or size rejected
- **WHEN** the retention-days field is non-positive or the size-limit field is negative
- **THEN** the value is rejected and an error is shown and the window does not save
