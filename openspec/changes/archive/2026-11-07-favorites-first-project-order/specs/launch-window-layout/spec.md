## ADDED Requirements

### Requirement: Project list ordering

The system SHALL display favorite projects before non-favorite projects in the Launch Window project list, preserving the registration order within each group.

#### Scenario: Favorites appear first
- **WHEN** the project list contains both favorite and non-favorite projects
- **THEN** all favorite projects are displayed before all non-favorite projects

#### Scenario: Stable order within favorites
- **WHEN** multiple projects are marked as favorites
- **THEN** they appear in their original registration order relative to each other

#### Scenario: Stable order within non-favorites
- **WHEN** multiple projects are not marked as favorites
- **THEN** they appear in their original registration order relative to each other

#### Scenario: Toggling favorite reorders on refresh
- **WHEN** a project's favorite state is toggled and the project list is refreshed
- **THEN** the project moves to the appropriate group (favorites or non-favorites) while preserving relative order within its new group

#### Scenario: Selection preserved through reorder
- **WHEN** the project list is reordered due to favorite changes
- **THEN** the currently selected project remains selected

#### Scenario: All favorites
- **WHEN** all projects are marked as favorites
- **THEN** they are displayed in their original registration order

#### Scenario: No favorites
- **WHEN** no projects are marked as favorites
- **THEN** they are displayed in their original registration order
