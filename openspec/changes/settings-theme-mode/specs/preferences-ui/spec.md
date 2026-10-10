## ADDED Requirements

### Requirement: Application theme selector

The Appearance page SHALL offer an "Application theme" selector with the options "Dark", "Light", and "System", showing the stored value.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Appearance"
- **THEN** the application theme selector offers "Dark", "Light", and "System" with the stored value selected

#### Scenario: Persist on save
- **WHEN** the user selects a theme and chooses "Save"
- **THEN** the value is written to `config.json` and takes effect on the next start

#### Scenario: Restart note
- **WHEN** the theme selector is shown
- **THEN** a hint indicates that the theme applies after a restart
