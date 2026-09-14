## ADDED Requirements

### Requirement: Projects pane actions

The **Projects** pane SHALL offer an `Actions` menu with **Add** (choose a folder)
and **Remove** (remove the selected project, after confirmation).

#### Scenario: Add from the Projects pane

- **WHEN** the user chooses Add and picks a folder
- **THEN** the project appears in the list and becomes selected

#### Scenario: Remove from the Projects pane

- **WHEN** the user chooses Remove for the selected project and confirms
- **THEN** the project disappears from the list

#### Scenario: Remove without a selection

- **WHEN** no project is selected
- **THEN** the Remove action is unavailable

### Requirement: Empty Projects state

When no projects are registered, the system SHALL show an empty state that prompts
to add a project instead of a synthetic fallback entry.

#### Scenario: No projects

- **WHEN** the registry is empty
- **THEN** the Projects pane shows an empty state instead of a synthetic current-directory entry
