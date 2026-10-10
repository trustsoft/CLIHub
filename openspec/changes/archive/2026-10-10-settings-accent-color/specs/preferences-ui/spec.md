## ADDED Requirements

### Requirement: Accent color selector

The Appearance page SHALL offer an accent color selector with the preset swatches and a custom `#RRGGBB` field, showing the stored value.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Appearance"
- **THEN** the accent selector offers the presets and a custom field with the stored value selected

#### Scenario: Persist and apply on save
- **WHEN** the user selects an accent and chooses "Save"
- **THEN** the value is written to `config.json` and applied immediately
