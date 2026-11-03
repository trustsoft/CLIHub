# Capability: Preferences UI

## MODIFIED Requirements

### Requirement: Application theme selector

The Theme & Accent page SHALL offer a "Theme" selector as a row of four preview cards with the options "Dark", "Light", "Deep Blue", and "System", showing the stored value. Each card SHALL show a miniature palette preview and the option name; the palette description — "Default palette", "Bright, high-contrast palette", "Dark with a blue tint", and "Follows the Windows light/dark mode" respectively — SHALL be exposed as the card's tooltip and accessible help text instead of visible body text. The selected card SHALL be marked by painting its border with the accent color; the selector SHALL NOT draw an additional selection ring or check badge.

The accessible name for each theme option SHALL include the word "theme" to provide screen-reader users with full context: "Dark theme", "Light theme", "Deep Blue theme", and "System theme".

#### Scenario: Options offered

- **WHEN** the Settings window opens on "Theme & Accent"
- **THEN** the theme selector offers "Dark", "Light", "Deep Blue", and "System" as preview cards with their option names
- **AND** each card exposes its palette description as a tooltip
- **AND** each card's accessible name includes "theme" for screen readers
- **AND** the stored value is selected and marked by its accent-colored border

#### Scenario: Persist on save

- **WHEN** the user selects a theme and chooses "Save"
- **THEN** the value is written to `config.json` and every open window renders with the selected palette immediately, without a restart

#### Scenario: Restart note

- **WHEN** the theme selector is shown
- **THEN** the "System" card's tooltip description indicates that it follows Windows personalization state
