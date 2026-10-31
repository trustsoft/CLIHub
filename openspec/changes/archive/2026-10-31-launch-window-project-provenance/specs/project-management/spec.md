# project-management (partial): launch window project provenance

## MODIFIED Requirements

### Requirement: Project provenance

The system SHALL record whether each project was registered manually or discovered by scanning, and SHALL keep that provenance stable once set.

#### Scenario: Manually registered project is marked manual

- **WHEN** a user registers a project by selecting a folder
- **THEN** the project is marked as manually registered

#### Scenario: Discovered project is marked discovered

- **WHEN** a project is registered by repository scanning
- **THEN** the project is marked as discovered

#### Scenario: Manual project is never reclassified

- **WHEN** scanning later encounters a folder that was registered manually
- **THEN** the project's provenance remains manual

#### Scenario: Provenance defaults to manual

- **WHEN** a stored project has no provenance recorded
- **THEN** it is treated as manually registered

#### Scenario: Provenance persists

- **WHEN** a project's provenance is set and the application restarts
- **THEN** the stored provenance is restored

#### Scenario: Launch window shows provenance mark

- **WHEN** a project is displayed in the launch window
- **THEN** the provenance mark ("M" for manual, "D" for discovered) is shown in the project row's right-side marker rail, aligned with the project path line
- **AND** the favorite marker, when present, occupies the same rail on the project name line
- **AND** the full label ("Manual" or "Discovered") is displayed as the mark's tooltip and accessible name
- **AND** the mark uses the ProjectProvenanceMark style for consistent appearance with the Settings Projects page
