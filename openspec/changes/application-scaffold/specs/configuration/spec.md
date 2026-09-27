## Purpose

Manages application settings persistence in JSON format, providing save and load operations for user preferences, project list, and plugin configuration.

## ADDED Requirements

### Requirement: JSON configuration storage

The system SHALL persist configuration to %APPDATA%\CLIHub\config.json.

#### Scenario: Initial config creation
- **WHEN** the application starts for the first time
- **THEN** a default config.json is created with empty projects and default preferences

#### Scenario: Config loading at startup
- **WHEN** the application starts
- **THEN** configuration is loaded from config.json if it exists

### Requirement: Configuration structure

The system SHALL store projects, preferences, and current selection in the configuration.

#### Scenario: Config contains projects list
- **WHEN** configuration is loaded
- **THEN** it includes an array of project objects with id, name, path, and metadata

#### Scenario: Config contains preferences
- **WHEN** configuration is loaded
- **THEN** it includes user preferences like startWithWindows, hotkey, and terminalExecutable

#### Scenario: Config tracks current project
- **WHEN** configuration is loaded
- **THEN** it includes currentProjectId referencing the last selected project

### Requirement: Atomic configuration updates

The system SHALL save configuration atomically to prevent corruption.

#### Scenario: Safe write on update
- **WHEN** configuration is modified
- **THEN** changes are written to a temporary file first, then renamed to config.json

#### Scenario: Partial write protection
- **WHEN** application crashes during config save
- **THEN** the previous valid config.json remains intact

### Requirement: Configuration validation

The system SHALL validate configuration schema on load.

#### Scenario: Invalid JSON handling
- **WHEN** config.json contains invalid JSON
- **THEN** the system logs an error and uses default configuration

#### Scenario: Schema migration
- **WHEN** config.json is from an older version
- **THEN** missing fields are populated with defaults without data loss
