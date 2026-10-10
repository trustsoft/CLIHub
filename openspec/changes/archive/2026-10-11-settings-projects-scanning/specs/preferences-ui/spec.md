## ADDED Requirements

### Requirement: Projects and Paths page options

The Projects & Paths page SHALL offer a watched-directories list with add and remove actions, a "Repository scan depth" field, an "Ignored directory patterns" list, a "Repository discovery" selector with the options "Git repositories", "Agent folders", and "Both", and a read-only background watcher status. It SHALL show the stored values when opened.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Projects & Paths"
- **THEN** it shows the watched-directories list with add/remove, the "Repository scan depth" field, the "Ignored directory patterns" list, the "Repository discovery" selector with three options, and the watcher status

#### Scenario: Stored values loaded
- **WHEN** the page opens
- **THEN** the watched directories, scan depth, ignored patterns, and discovery mode show the stored values

#### Scenario: Defaults
- **WHEN** no scanning preferences have been stored
- **THEN** the watched-directories list is empty, the scan depth shows the built-in default, the ignored patterns show the built-in defaults, and the discovery mode is "Both"

#### Scenario: Watcher status is read-only
- **WHEN** the watcher status is shown
- **THEN** it reports the watcher state, the last scan time, and the discovered-project count, and it is not user-editable

### Requirement: Projects and Paths validation

The Projects & Paths page SHALL validate its input and reject invalid values without saving.

#### Scenario: Scan depth out of range rejected
- **WHEN** the scan depth is not a positive whole number within the allowed range
- **THEN** validation reports an error and no value is written to `config.json`

#### Scenario: Empty or duplicate patterns ignored
- **WHEN** an ignored pattern is blank or duplicates an existing pattern
- **THEN** the blank or duplicate entry is not stored

#### Scenario: Discard on cancel
- **WHEN** the user changes scanning settings and chooses "Cancel"
- **THEN** the stored values are unchanged and reopening Settings shows the stored values

### Requirement: Scanning preferences apply on save

Saving the scanning preferences SHALL persist them and take effect without restarting the application.

#### Scenario: Persist on save
- **WHEN** the user changes a scanning setting and chooses "Save"
- **THEN** the value is written to `config.json`

#### Scenario: Applied without restart
- **WHEN** the user saves changed watched directories, scan depth, ignored patterns, or the discovery mode
- **THEN** the watcher and scanner use the new configuration immediately, without restarting the application

#### Scenario: Empty watched list stops watching
- **WHEN** the user saves an empty watched-directories list
- **THEN** the watcher stops and the status reports that no watched directories are configured
