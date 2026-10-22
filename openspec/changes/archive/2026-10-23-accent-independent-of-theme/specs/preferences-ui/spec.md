# preferences-ui Specification (Delta)

## MODIFIED Requirements

### Requirement: Application theme selector

The Appearance page SHALL offer an "Application theme" selector as a row of four preview cards with
the options "Dark Theme", "Light Theme", "Blue Theme", and "System Sync", showing the stored value.
Each card SHALL show a miniature palette preview, the option name, and a short description:
"Fluent Dark (default)", "High contrast daylight profile", "Blue-tinted dark chrome",
and "Follow Windows personalization state" respectively. The selected card SHALL be marked with an
accent ring and a check badge.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Appearance"
- **THEN** the application theme selector offers "Dark Theme", "Light Theme", "Blue Theme", and
  "System Sync" as preview cards with their descriptions
- **AND** the stored value is selected and marked with the accent ring and check badge

#### Scenario: Persist on save
- **WHEN** the user selects a theme and chooses "Save"
- **THEN** the value is written to `config.json` and every open window renders with the selected
  palette immediately, without a restart

#### Scenario: Restart note
- **WHEN** the theme selector is shown
- **THEN** the "System Sync" card description indicates that it follows Windows personalization state
