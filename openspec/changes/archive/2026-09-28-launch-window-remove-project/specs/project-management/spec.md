## MODIFIED Requirements

### Requirement: Project removal

The system SHALL allow removing a registered project without deleting its files.

#### Scenario: Remove project
- **WHEN** a user removes a project
- **THEN** it is removed from configuration and no longer appears in lists

#### Scenario: Files are not deleted
- **WHEN** a project is removed
- **THEN** the project folder and its contents on disk are left untouched

#### Scenario: Remove from the launch window
- **WHEN** a user selects a project in the launch window and activates "Remove Project"
- **THEN** a confirmation prompt is shown, and the project is removed from the list only after the user confirms

#### Scenario: Refresh after removal
- **WHEN** a project is removed from the launch window
- **THEN** the project list and the agent availability display are refreshed to reflect the removal
