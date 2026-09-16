## ADDED Requirements

### Requirement: Footer with version and system actions

The popup footer SHALL display the application brand and version, and SHALL
offer three actions: open the data folder, settings, and exit. The status
text SHALL be shown in the footer center, between the brand/version area and
the action area.

#### Scenario: Version displayed

- **WHEN** the popup opens
- **THEN** the footer shows the brand and the application version

#### Scenario: Version fallback when unpackaged

- **WHEN** the application runs without an installed (Velopack) version
- **THEN** the footer shows the assembly informational version instead

#### Scenario: Open data folder

- **WHEN** the user activates the folder action
- **THEN** the stable application data folder (`%AppData%\CLIHub`) opens in
  the system file manager, not the installation directory

#### Scenario: Settings action unavailable

- **WHEN** no settings surface exists
- **THEN** the settings action is visible in the footer but unavailable

#### Scenario: Exit from the footer

- **WHEN** the user activates the exit action
- **THEN** the application terminates

#### Scenario: Status text in the footer center

- **WHEN** a status message is set (project added or removed, launch result)
- **THEN** the message appears in the footer center between the
  brand/version area and the action area
