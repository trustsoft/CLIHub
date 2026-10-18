## MODIFIED Requirements

### Requirement: Popup screen position

The Hotkeys & Quick Launcher page SHALL offer a "Launcher window position" selector under a "LAUNCHER POSITIONING" section header, with the options "Active monitor — centered", "Active monitor — top-center", "Cursor-relative", and "Primary monitor — centered", defaulting to "Active monitor — centered". Options that target a monitor SHALL follow the "<monitor> — <placement>" label pattern so that both the monitor and the placement the launch window will use are explicit. "Cursor-relative" SHALL keep its pointer-relative wording because it does not target a monitor.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Hotkeys & Quick Launcher"
- **THEN** the "LAUNCHER POSITIONING" section shows the "Launcher window position" selector with the four options and the stored value selected
- **AND** the monitor-targeting options are labelled "Active monitor — centered", "Active monitor — top-center", and "Primary monitor — centered"

#### Scenario: Default position
- **WHEN** no launcher window position has been stored
- **THEN** "Active monitor — centered" is selected

#### Scenario: Persist on save
- **WHEN** the user selects a launcher window position and chooses "Save"
- **THEN** the value is written to `config.json`
- **AND** an existing stored primary-monitor position remains selected without migration

#### Scenario: Applied without restart
- **WHEN** the user saves a different launcher window position
- **THEN** the launch window uses it the next time it is shown without restarting the application
