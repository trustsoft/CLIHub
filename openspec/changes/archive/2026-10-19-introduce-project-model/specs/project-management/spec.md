# project-management (partial): enriched display models and detached reads

## ADDED Requirements

### Requirement: Enriched project display models

UI surfaces SHALL display project rows through enriched display models combining the project record's identity with an enriched snapshot: the final logo path (default-logo fallback already applied by the project logo resolver) and whether the project folder currently exists. The launch window and the Settings project registry SHALL share one display-model instance per project ID, and refresh SHALL update it in place rather than rebuild rows.

#### Scenario: Model carries the final logo path

- **WHEN** a project row is built or refreshed
- **THEN** its display model holds the resolved logo path with the default-logo fallback already applied
- **AND** the view renders it through the shared logo image converter

#### Scenario: Snapshot refresh triggers

- **WHEN** the project set changes, the launch window is shown or activated, or the Settings Projects page loads
- **THEN** the display models are re-enriched: folder existence is recomputed and the logo path is read through the project logo resolver and its persistent cache

#### Scenario: No live folder monitoring

- **WHEN** a project folder is created or deleted without any refresh trigger occurring
- **THEN** the displayed folder availability stays at its last snapshot until the next trigger

#### Scenario: One shared instance per project

- **WHEN** the same project is displayed by the launch window and the Settings registry
- **THEN** both surfaces display the same display-model instance
- **AND** a refresh updates that instance in place so both surfaces reflect the change without rebuilding their lists

### Requirement: Detached project reads

The project service SHALL return detached project copies from its read methods. UI surfaces SHALL bind project display models, not project records returned by the service, and mutating a returned project record SHALL NOT change registered project state.

#### Scenario: Reads return detached copies

- **WHEN** the project service returns projects from a read method
- **THEN** mutating a returned project changes neither later reads nor persisted state

#### Scenario: UI binds display models

- **WHEN** the launch window or the Settings registry renders project rows
- **THEN** the bound rows are enriched display models, not project records
