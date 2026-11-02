# Spec Delta

## MODIFIED Requirements

### Requirement: Application theme selector

The Theme & Accent page SHALL offer an "Application theme" selector as a row of four preview cards
with the options "Dark Theme", "Light Theme", "Deep Blue Theme", and "System Theme", showing the stored
value. Each card SHALL show a miniature palette preview and the option name; the palette description —
"Fluent Dark (default)", "High contrast daylight profile", "Blue-tinted dark chrome", and "Follow
Windows personalization state" respectively — SHALL be exposed as the card's tooltip and accessible
help text instead of visible body text. The selected card SHALL be marked by painting its border with
the accent color; the selector SHALL NOT draw an additional selection ring or check badge.

#### Scenario: Options offered

- **WHEN** the Settings window opens on "Theme & Accent"
- **THEN** the application theme selector offers "Dark Theme", "Light Theme", "Deep Blue Theme", and
  "System Theme" as preview cards with their option names
- **AND** each card exposes its palette description as a tooltip
- **AND** the stored value is selected and marked by its accent-colored border

#### Scenario: Persist on save

- **WHEN** the user selects a theme and chooses "Save"
- **THEN** the value is written to `config.json` and every open window renders with the selected
  palette immediately, without a restart

#### Scenario: Restart note

- **WHEN** the theme selector is shown
- **THEN** the "System Theme" card's tooltip description indicates that it follows Windows
  personalization state

### Requirement: Accent color selector

The Theme & Accent page SHALL offer an accent color selector as preview cards for the presets Sky, Blue,
Teal, Emerald, Purple, Amber, Indigo, and Tangerine, plus a custom entry. Each card SHALL show a color
dot, the accent name, and its `#RRGGBB` value; the selected card SHALL be marked by painting its border
with the accent color, without a check inside the dot, and the card header SHALL show the active accent
name. The custom `#RRGGBB` field SHALL be shown only while the custom entry is selected. The selector
SHALL remember the last valid custom accent independently of the named presets and restore it whenever
the custom entry is selected, including after the Settings window is closed and reopened.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Theme & Accent"
- **THEN** the accent selector offers Sky, Blue, Teal, Emerald, Purple, Amber, Indigo, and Tangerine as
  preview cards with their hex values
- **AND** the stored value is selected and marked by its accent-colored border
- **AND** the card header shows the active accent name

#### Scenario: Custom field visibility
- **WHEN** a named preset is selected
- **THEN** the custom `#RRGGBB` field is hidden
- **WHEN** the custom entry is selected
- **THEN** the custom field is shown

#### Scenario: Custom value kept independently
- **GIVEN** the user has entered a custom accent
- **WHEN** the user selects a named preset and later selects the custom entry again
- **THEN** the custom field shows the last valid custom accent

#### Scenario: Persist and apply on save
- **WHEN** the user selects an accent and chooses "Save"
- **THEN** the value is written to `config.json` and applied immediately
