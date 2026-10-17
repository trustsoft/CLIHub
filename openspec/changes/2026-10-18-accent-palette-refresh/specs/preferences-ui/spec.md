## MODIFIED Requirements

### Requirement: Accent color selector

The Appearance page SHALL offer an accent color selector as preview cards for the presets Sky, Blue,
Teal, Emerald, Purple, Amber, Indigo, and Tangerine, plus a custom entry. Each card SHALL show a color
dot, the accent name, and its `#RRGGBB` value; the selected card SHALL be marked with the accent ring
and a check inside the dot, and the card header SHALL show the active accent name. The custom
`#RRGGBB` field SHALL be shown only while the custom entry is selected. The selector SHALL remember
the last valid custom accent independently of the named presets and restore it whenever the custom
entry is selected, including after the Settings window is closed and reopened.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Appearance"
- **THEN** the accent selector offers Sky, Blue, Teal, Emerald, Purple, Amber, Lime, and Tangerine as
  preview cards with their hex values
- **AND** the stored value is selected and marked with the accent ring and check
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
