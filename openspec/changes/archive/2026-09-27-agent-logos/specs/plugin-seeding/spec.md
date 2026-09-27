## ADDED Requirements

### Requirement: Seed built-in agent logos

The system SHALL write each built-in agent's logo file alongside its descriptor when seeding.

#### Scenario: Logo written on first run
- **WHEN** the plugins folder is empty and seeding runs
- **THEN** a `logo.png` is written into each built-in agent's folder in addition to `plugin.json`

#### Scenario: Logo reflects in the UI
- **WHEN** a seeded agent is displayed
- **THEN** its logo is shown instead of the default placeholder

#### Scenario: Logo never overwrites user file
- **WHEN** a `logo.png` already exists in an agent folder
- **THEN** it is left untouched

#### Scenario: Missing logo falls back
- **WHEN** an agent has no bundled or user logo
- **THEN** the default logo is used
