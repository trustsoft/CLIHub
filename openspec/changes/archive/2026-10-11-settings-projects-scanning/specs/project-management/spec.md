## ADDED Requirements

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

### Requirement: Discovered projects remain fully usable

The system SHALL treat discovered projects as ordinary projects for selection, recency, favorites, logo resolution, launch context, and removal.

#### Scenario: Discovered project is selectable
- **WHEN** a discovered project is selected
- **THEN** it becomes the current project and provides launch context like any other project

#### Scenario: Discovered project can be removed
- **WHEN** a user removes a discovered project
- **THEN** it is removed from configuration and its files on disk are left untouched
