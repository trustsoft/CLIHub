# plugin-seeding (partial): detached plugin reads

## ADDED Requirements

### Requirement: Detached plugin reads

The plugin catalog SHALL return detached plugin copies from its read methods. A returned copy SHALL clone nested command and detection state, so that mutating a returned plugin — including its commands or detection markers — SHALL NOT change the loaded catalog or any later read.

#### Scenario: Reads return detached copies

- **WHEN** the plugin catalog returns plugins from a read method
- **THEN** mutating a returned plugin changes neither the loaded catalog nor any later read

#### Scenario: Nested command and detection state is cloned

- **WHEN** a caller mutates a returned plugin's command or detection state
- **THEN** the catalog's internal instances keep their own state unchanged
