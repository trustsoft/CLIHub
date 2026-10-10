## ADDED Requirements

### Requirement: Configurable log retention and file size limit

The system SHALL support configurable log retention (number of retained files/days) and an optional per-file size limit, applied when the logger is created at startup.

#### Scenario: Default retention and size limit
- **WHEN** no log retention or size-limit preference is stored
- **THEN** log files are retained for 7 files and no per-file size limit is applied

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
- **WHEN** the Telemetry & Logs page is shown
- **THEN** the current log file path under `%APPDATA%\CLIHub\logs\` is displayed

#### Scenario: Copy path
- **WHEN** the user chooses to copy the log path
- **THEN** the current log file path is placed on the clipboard

#### Scenario: Open log folder
- **WHEN** the user chooses "Open log folder"
- **THEN** the logs directory is opened in the system file browser
