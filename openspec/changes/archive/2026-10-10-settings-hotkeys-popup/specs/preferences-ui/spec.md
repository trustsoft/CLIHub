## ADDED Requirements

### Requirement: Popup screen position

The Hotkeys & Launchers page SHALL offer a "Popup screen position" selector with the options "Center of active monitor", "Top-center of active monitor", "Cursor-relative", and "Primary screen", defaulting to "Center of active monitor".

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Hotkeys & Launchers"
- **THEN** the popup screen position selector offers the four options with the stored value selected

#### Scenario: Default position
- **WHEN** no popup screen position has been stored
- **THEN** "Center of active monitor" is selected

#### Scenario: Persist on save
- **WHEN** the user selects a popup screen position and chooses "Save"
- **THEN** the value is written to `config.json`

#### Scenario: Applied without restart
- **WHEN** the user saves a different popup screen position
- **THEN** the launch window uses it the next time it is shown without restarting the application
