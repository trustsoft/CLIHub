## ADDED Requirements

### Requirement: Reload the plugin catalog on demand

The system SHALL provide a synchronous manual operation that rescans the plugin directory and replaces the current catalog snapshot using the same discovery, validation, deterministic ordering, duplicate-ID, and logo-resolution rules as initial loading.

#### Scenario: New plugin is available after reload
- **WHEN** a valid plugin descriptor is added after initial loading and the manual reload operation is invoked
- **THEN** the plugin is present in the catalog after the operation completes

#### Scenario: Removed plugin is absent after reload
- **WHEN** a previously loaded plugin descriptor is removed and the manual reload operation is invoked
- **THEN** the plugin is absent from the catalog after the operation completes

### Requirement: Notify consumers after catalog reload

The system SHALL notify subscribed consumers after a manual reload completes and the catalog exposes the resulting snapshot.

#### Scenario: Reload notification is raised after replacement
- **WHEN** a manual reload operation completes
- **THEN** exactly one catalog-changed notification is raised after the new plugin snapshot is available

#### Scenario: Initial load does not create a duplicate reload notification
- **WHEN** the catalog performs its initial startup load
- **THEN** no manual-reload notification is raised solely because of the initial load
