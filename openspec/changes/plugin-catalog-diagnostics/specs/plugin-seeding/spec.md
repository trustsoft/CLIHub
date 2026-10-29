# plugin-seeding Delta Specification

## ADDED Requirements

### Requirement: Handle unsupported schema versions

The system SHALL reject descriptors with unsupported schema versions and expose the validation error so the UI can display disabled entries with reasons.

#### Scenario: Schema version absent defaults to 1

- **WHEN** a descriptor omits the schemaVersion field
- **THEN** it is treated as version 1 and loaded if other validation passes

#### Scenario: Unsupported schema version is rejected

- **WHEN** a descriptor declares schemaVersion with an integer value greater than 1
- **THEN** it is marked invalid with error "Unsupported schema version X; maximum supported is 1"
- **AND** the entry is retained in the invalid-plugins collection

#### Scenario: Invalid schema version type is rejected

- **WHEN** schemaVersion is present but not an integer (string, null, float, or other non-integer type)
- **THEN** the descriptor is marked invalid with error "Schema version must be an integer"
- **AND** the entry is retained in the invalid-plugins collection

#### Scenario: Zero or negative schema version is rejected

- **WHEN** schemaVersion is an integer less than or equal to zero
- **THEN** the descriptor is marked invalid with error "Schema version must be positive"
- **AND** the entry is retained in the invalid-plugins collection

### Requirement: Expose invalid plugin entries

The catalog SHALL retain invalid plugin entries with their validation errors so the UI can display them as disabled with diagnostic information.

#### Scenario: Invalid entries are accessible

- **WHEN** the catalog loads and encounters invalid descriptors
- **THEN** those entries are available through GetInvalidPlugins()
- **AND** each entry includes the plugin directory and the validation or read error

#### Scenario: Valid plugins remain unchanged

- **WHEN** the catalog exposes invalid entries
- **THEN** GetAllPlugins() continues to return only valid plugins
- **AND** existing consumers see no behavior change

#### Scenario: Reload replaces invalid entries

- **WHEN** the catalog is reloaded
- **THEN** the previous invalid-entries snapshot is replaced with the new scan result
- **AND** fixed descriptors move from invalid to valid, broken ones move from valid to invalid
