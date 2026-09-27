## Purpose

Discovers, loads, validates, and manages AI agent CLI tool plugins from JSON descriptors with logo assets in per-plugin subdirectories.

## ADDED Requirements

### Requirement: Plugin directory structure

The system SHALL load plugins from %APPDATA%\CLIHub\plugins\ with one subdirectory per plugin.

#### Scenario: Plugin subdirectory layout
- **WHEN** plugins are loaded
- **THEN** each plugin is in its own subdirectory containing plugin.json and logo.png

#### Scenario: Missing logo handling
- **WHEN** a plugin subdirectory lacks logo.png
- **THEN** the plugin loads successfully with a default logo placeholder

### Requirement: Plugin JSON descriptor loading

The system SHALL parse plugin.json files and populate plugin metadata.

#### Scenario: Valid plugin loading
- **WHEN** a plugin subdirectory contains a valid plugin.json
- **THEN** the plugin is loaded with id, name, commands, and detection rules

#### Scenario: Invalid plugin.json handling
- **WHEN** plugin.json is malformed or missing required fields
- **THEN** the plugin is skipped with a logged warning

### Requirement: Plugin metadata validation

The system SHALL validate that each plugin has required fields (id, name, commands).

#### Scenario: Missing required field
- **WHEN** plugin.json lacks id or name
- **THEN** the plugin is rejected and not added to the loaded plugins list

#### Scenario: Duplicate plugin IDs
- **WHEN** two plugins have the same id
- **THEN** the second plugin is rejected with a logged warning

### Requirement: Plugin enumeration

The system SHALL provide a list of all loaded plugins.

#### Scenario: Get all plugins
- **WHEN** the application requests all plugins
- **THEN** a collection of loaded plugin objects is returned

#### Scenario: Get plugin by ID
- **WHEN** a specific plugin is requested by id
- **THEN** the matching plugin object is returned, or null if not found

### Requirement: Plugin reload capability

The system SHALL support reloading plugins without restarting the application.

#### Scenario: Manual plugin reload
- **WHEN** plugins are reloaded
- **THEN** all plugin subdirectories are rescanned and plugins list is refreshed

#### Scenario: New plugin detection
- **WHEN** a new plugin subdirectory is added while the application runs
- **THEN** the new plugin is loaded after reload without affecting existing plugins
