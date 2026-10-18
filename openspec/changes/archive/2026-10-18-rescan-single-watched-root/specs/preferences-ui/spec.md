## MODIFIED Requirements

### Requirement: Projects and Paths page options

The Projects & Paths page SHALL offer a watched-directories list with add, per-directory scan, and remove actions, a "Repository scan depth" field, an "Ignored directory patterns" list, a "Repository discovery" selector with the options "Git only", "Agents only", and "Git and agents", and a read-only background watcher status. The per-directory scan action SHALL rescan only that directory's watched root, unlike the page-header action that rescans every saved root. The page SHALL show the stored values when opened.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Projects & Paths"
- **THEN** it shows the watched-directories list with add, scan, and remove actions, the "Repository scan depth" field, the "Ignored directory patterns" list, the "Repository discovery" selector with three options, and the watcher status

#### Scenario: Stored values loaded
- **WHEN** the page opens
- **THEN** the watched directories, scan depth, ignored patterns, and discovery mode show the stored values

#### Scenario: Defaults
- **WHEN** no scanning preferences have been stored
- **THEN** the watched-directories list is empty, the scan depth shows the built-in default, the ignored patterns show the built-in defaults, and the discovery mode is "Both"

#### Scenario: Watcher status is read-only
- **WHEN** the watcher status is shown
- **THEN** it reports the watcher state, the last scan time, and the discovered-project count, and it is not user-editable

#### Scenario: Per-directory scan
- **WHEN** the user chooses the scan action on one watched directory
- **THEN** only that watched root is rescanned with the stored scanning settings
- **AND** the other watched roots keep their current discovered projects
