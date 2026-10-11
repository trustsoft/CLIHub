## ADDED Requirements

### Requirement: Project-independent agent update

The system SHALL run an agent's `update` command in the user profile directory when the update is requested outside a project context, such as from the Settings window.

#### Scenario: Update without a project
- **WHEN** an agent update is requested and no project is current
- **THEN** the update command runs interactively with the user profile directory as the working directory

#### Scenario: Update with a project context
- **WHEN** an agent update is requested from a surface where a project is current
- **THEN** the update command runs in that project's folder

#### Scenario: Update still respects capabilities
- **WHEN** an agent does not declare an `update` command
- **THEN** the update request is rejected with a message that the agent does not support it
