## MODIFIED Requirements

### Requirement: Update settings in config

The system SHALL store update settings in the `update` section of
`settings.json`, with `checkOnStartup` (boolean, default true) controlling
whether the startup check runs.

#### Scenario: Missing section uses the default

- **WHEN** `settings.json` has no `update` section
- **THEN** `checkOnStartup` is treated as true

#### Scenario: Disabled startup check

- **WHEN** `settings.json` sets `update.checkOnStartup` to false
- **THEN** no update check runs at startup
