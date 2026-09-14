## MODIFIED Requirements

### Requirement: Runtime resolution

The system SHALL choose the runtime by priority: per-action override from the
manifest, then the global runtime from `settings.json`, then the built-in
default.

#### Scenario: Action override set

- **WHEN** the launched action specifies a runtime
- **THEN** that runtime is used

#### Scenario: No override

- **WHEN** the action does not specify a runtime and config has a global value
- **THEN** the global runtime is used

#### Scenario: Neither override nor global value

- **WHEN** neither the action nor config specifies a runtime
- **THEN** the built-in application default is used
