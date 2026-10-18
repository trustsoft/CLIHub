## MODIFIED Requirements

### Requirement: Projects and Paths page options

The Projects & Paths page SHALL offer a watched-directories list with add, per-directory scan, and remove actions, a "Repository scan depth" field, an "Ignored directory patterns" list, a "Repository discovery" selector with the options "Git only", "Agents only", and "Git and agents", and a read-only background watcher status. The per-directory scan action SHALL rescan only that directory's watched root, unlike the page-header action that rescans every saved root. Each watched-directory row SHALL show, beneath its path, a depth chip and a discovery chip that open a menu of allowed values, plus the number of discovered repositories beneath that directory. The "Repository scan depth" and "Repository discovery" fields SHALL act as defaults for a directory added from now on and SHALL NOT change the settings of existing rows. The page SHALL show the stored values when opened.

#### Scenario: Options offered
- **WHEN** the Settings window opens on "Projects & Paths"
- **THEN** it shows the watched-directories list with add, scan, and remove actions, the depth and discovery defaults, the "Ignored directory patterns" list, and the watcher status

#### Scenario: Stored values loaded
- **WHEN** the page opens
- **THEN** the watched directories show their stored depth, discovery mode, and repository count
- **AND** the default depth and discovery show the stored global values

#### Scenario: Defaults
- **WHEN** no scanning preferences have been stored
- **THEN** the watched-directories list is empty, the default depth shows the built-in default, the ignored patterns show the built-in defaults, and the default discovery mode is "Git and agents"

#### Scenario: Row chips change only that row
- **WHEN** the user picks a depth or discovery value from a row's chip menu
- **THEN** only that row's setting changes
- **AND** the global defaults and the other rows keep their values

#### Scenario: New directory starts from the defaults
- **WHEN** the user adds a watched directory
- **THEN** its depth and discovery chips start from the current default values

#### Scenario: Per-directory scan
- **WHEN** the user chooses the scan action on one watched directory
- **THEN** only that watched root is rescanned with that row's settings
- **AND** the other watched roots keep their current discovered projects

#### Scenario: Row summary shows the per-directory count
- **WHEN** a watched directory is listed
- **THEN** it shows the number of discovered repositories beneath that directory
- **AND** the count uses the singular "repository" for one and the plural "repositories" otherwise
- **AND** the count refreshes after a scan completes

#### Scenario: Watcher status is read-only
- **WHEN** the watcher status is shown
- **THEN** it reports the watcher state, the last scan time, and the discovered-project count, and it is not user-editable
