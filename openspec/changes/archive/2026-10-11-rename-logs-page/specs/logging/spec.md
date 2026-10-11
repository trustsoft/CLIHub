## MODIFIED Requirements

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
