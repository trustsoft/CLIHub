# Spec Delta

## MODIFIED Requirements

### Requirement: Favorite projects

The system SHALL allow marking projects as favorites, including from the launch window.

#### Scenario: Mark favorite
- **WHEN** a user marks a project as a favorite
- **THEN** the project's favorite flag is set and persisted

#### Scenario: Unmark favorite
- **WHEN** a user unmarks a favorite project
- **THEN** the project's favorite flag is cleared and persisted

#### Scenario: Toggle from the launch window
- **WHEN** the user invokes the favorite action for the selected project in the launch window
- **THEN** the project's favorite state is toggled, persisted, and reflected in the project list

#### Scenario: Toggle requires a selection
- **WHEN** no project is selected in the launch window
- **THEN** the favorite action is unavailable and no project's favorite state changes
