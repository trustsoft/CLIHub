## MODIFIED Requirements

### Requirement: Manual catalog reload

The system SHALL provide a command to reload the plugin catalog without restarting the application. The reload SHALL re-seed missing built-in descriptors before rescanning the plugins directory.

#### Scenario: Catalog reloaded on demand

- **WHEN** the user clicks "Reload Catalog"
- **THEN** missing built-in descriptors are re-seeded before the rescan
- **AND** the catalog rescans the plugins directory
- **AND** the UI is updated with any newly detected, modified, removed, or restored plugins
