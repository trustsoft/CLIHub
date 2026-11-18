## MODIFIED Requirements

### Requirement: Tray menu structure

The tray context menu SHALL show, top to bottom: a disabled `Version <version>` header; a separator;
one submenu per favorite project; a separator; `Add Project…`; `Settings`; the `Updates` submenu; a
separator; and `Exit`. The submenu's label is `Updates`; the arrow the platform draws for an item with
children is not part of the label. The menu SHALL NOT contain a `Show CLIHub` item, because the launch
window is already reachable by left-clicking the tray icon and by the global hotkey.

#### Scenario: Header reflects the current project

- **WHEN** a project is current
- **THEN** the first item is a disabled `Version <version>` header, not `Current: <name>`

#### Scenario: Header when no project is selected

- **WHEN** no project is current
- **THEN** the first item is a disabled `Version <version>` header, not `No project selected`

#### Scenario: Header shows the current version

- **WHEN** the menu is shown
- **THEN** the first item is a disabled `Version <version>` header and the header does not show the
  current project

#### Scenario: Tail order is Settings, Updates, separator, Exit

- **WHEN** the menu is shown
- **THEN** `Settings`, the `Updates` submenu, a separator, and `Exit` appear below `Add Project…` in
  that order

#### Scenario: Recent Projects and Launch Agent are gone

- **WHEN** the menu is shown
- **THEN** no `Recent Projects` submenu, no `Launch Agent` submenu, and no `Show CLIHub` item are present

#### Scenario: Separator divides the header from the project list

- **WHEN** the menu is shown
- **THEN** a separator appears directly below the header and directly above the first favorite project
  submenu (or the `(no favorite projects)` item when there are no favorites)

#### Scenario: Separator divides the project list from Add Project

- **WHEN** the menu is shown
- **THEN** a separator appears directly below the last favorite project submenu (or the
  `(no favorite projects)` item) and directly above `Add Project…`

### Requirement: Add Project is placed below the project list

`Add Project…` SHALL appear below the project submenus and the separator that closes that group, and
before `Settings`.

#### Scenario: Add Project position

- **WHEN** the menu is shown
- **THEN** `Add Project…` appears after the last project submenu (or the `(no favorite projects)` item)
  and its separator, before `Settings`

#### Scenario: Add Project behavior is unchanged

- **WHEN** `Add Project…` is invoked
- **THEN** the project-folder workflow runs as before and the new project becomes current
