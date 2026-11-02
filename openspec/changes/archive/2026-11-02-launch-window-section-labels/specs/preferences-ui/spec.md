# Spec Delta

## MODIFIED Requirements

### Requirement: Path display preference

The Settings window SHALL offer a choice of how long project paths are shortened in the launch window,
under a "PROJECT PATHS" section header on the Launch Window page.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Launch Window"
- **THEN** the path display control shows the stored style and offers `Left trim` and `Middle ellipsis`

#### Scenario: Default style
- **WHEN** no path display style has been saved
- **THEN** left trim is used

#### Scenario: Persist on save
- **WHEN** the user selects a path display style and chooses "Save"
- **THEN** the value is written to `config.json`

#### Scenario: Applied without restart
- **WHEN** the user saves a different path display style
- **THEN** the project rows in the launch window use the new style immediately, without restarting the application

#### Scenario: Discarded on cancel
- **WHEN** the user changes the path display style and chooses "Cancel"
- **THEN** the stored style is unchanged and reopening Settings shows the stored value

#### Scenario: Invalid stored value falls back to the default
- **WHEN** `config.json` contains an unrecognized path display style
- **THEN** left trim is used and the invalid value does not prevent the application from starting

### Requirement: Popup screen position

The Launch Window page SHALL offer a "Launcher window position" selector under a "POSITION" section
header, with the options "Active monitor – centered", "Active monitor – top-center",
"Cursor-relative", and "Primary monitor – centered", defaulting to "Active monitor – centered".
Options that target a monitor SHALL follow the "<monitor> – <placement>" label pattern so that both the
monitor and the placement the launch window will use are explicit. "Cursor-relative" SHALL keep its
pointer-relative wording because it does not target a monitor.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Launch Window"
- **THEN** the "POSITION" section shows the "Launcher window position" selector with the four options
  and the stored value selected
- **AND** the monitor-targeting options are labelled "Active monitor – centered", "Active monitor –
  top-center", and "Primary monitor – centered"

#### Scenario: Default position
- **WHEN** no launcher window position has been stored
- **THEN** "Active monitor – centered" is selected

#### Scenario: Persist on save
- **WHEN** the user selects a launcher window position and chooses "Save"
- **THEN** the value is written to `config.json`
- **AND** an existing stored primary-monitor position remains selected without migration

#### Scenario: Applied without restart
- **WHEN** the user saves a different launcher window position
- **THEN** the launch window uses it the next time it is shown without restarting the application
