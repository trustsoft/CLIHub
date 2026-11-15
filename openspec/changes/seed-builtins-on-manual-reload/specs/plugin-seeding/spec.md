## MODIFIED Requirements

### Requirement: Reload the plugin catalog on demand

The system SHALL provide a synchronous manual operation that first re-seeds missing built-in descriptors and then rescans the plugin directory, replacing the current catalog snapshot using the same discovery, validation, deterministic ordering, duplicate-ID, and logo-resolution rules as initial loading.

#### Scenario: New plugin is available after reload

- **WHEN** a valid plugin descriptor is added after initial loading and the manual reload operation is invoked
- **THEN** the plugin is present in the catalog after the operation completes

#### Scenario: Removed plugin is absent after reload

- **WHEN** a previously loaded non-built-in plugin descriptor is removed and the manual reload operation is invoked
- **THEN** the plugin is absent from the catalog after the operation completes

#### Scenario: Missing built-in descriptor is restored by manual reload

- **WHEN** a built-in plugin directory is deleted (registry entry exists with origin = "builtin")
- **AND** the plugin is not disabled in the registry
- **AND** the manual reload operation is invoked
- **THEN** the descriptor and logo are re-written before the rescan
- **AND** the plugin is present in the catalog after the operation completes
- **AND** the registry is updated with the current embedded hashes (`embeddedHash`, `embeddedLogoHash`)

#### Scenario: Seeding failure does not block manual reload

- **WHEN** seeding fails during a manual reload operation
- **THEN** the failure is logged and the catalog is still reloaded
- **AND** the operation completes
