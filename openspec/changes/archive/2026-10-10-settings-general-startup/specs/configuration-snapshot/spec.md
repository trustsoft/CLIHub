## ADDED Requirements

### Requirement: Startup visibility preference is migrated to start-minimized

The configuration migration layer SHALL convert the schema 1 `showWindowOnStartup` preference into the schema 2 `startMinimized` preference, inverting its value, while preserving unrelated configuration values.

#### Scenario: Show-window enabled maps to start-minimized disabled
- **GIVEN** a schema 1 configuration has `showWindowOnStartup` set to `true`
- **WHEN** the configuration is migrated to the current schema
- **THEN** `startMinimized` is set to `false`

#### Scenario: Show-window disabled maps to start-minimized enabled
- **GIVEN** a schema 1 configuration has `showWindowOnStartup` set to `false`
- **WHEN** the configuration is migrated to the current schema
- **THEN** `startMinimized` is set to `true`

#### Scenario: Migration preserves unrelated values
- **GIVEN** a schema 1 configuration with preferences and projects
- **WHEN** the configuration is migrated to the current schema
- **THEN** all preferences other than the startup-visibility preference and all project state are unchanged
