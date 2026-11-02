# Spec Delta

## MODIFIED Requirements

### Requirement: Application theme selector

The Theme & Accent page SHALL offer an "Application theme" selector as a row of four preview cards
with the options "Dark Theme", "Light Theme", "Deep Blue Theme", and "System Theme", showing the stored
value. Each card SHALL show a miniature palette preview and the option name; the palette description —
"Fluent Dark (default)", "High contrast daylight profile", "Blue-tinted dark chrome", and "Follow
Windows personalization state" respectively — SHALL be exposed as the card's tooltip and accessible
help text instead of visible body text. The selected card SHALL be marked with an accent ring and a
check badge.

#### Scenario: Options offered

- **WHEN** the Settings window opens on "Theme & Accent"
- **THEN** the application theme selector offers "Dark Theme", "Light Theme", "Deep Blue Theme", and
  "System Theme" as preview cards with their option names
- **AND** each card exposes its palette description as a tooltip
- **AND** the stored value is selected and marked with the accent ring and check badge

#### Scenario: Persist on save

- **WHEN** the user selects a theme and chooses "Save"
- **THEN** the value is written to `config.json` and every open window renders with the selected
  palette immediately, without a restart

#### Scenario: Restart note

- **WHEN** the theme selector is shown
- **THEN** the "System Theme" card's tooltip description indicates that it follows Windows
  personalization state
