## ADDED Requirements

### Requirement: Projects page summary cards

The Settings window SHALL show three summary cards under the "Projects" page header: total projects, manual projects, and discovered projects. The total card SHALL show the manual/discovered split and a folder-health line that names the number of projects whose folder no longer exists using the danger tone; the folder-health line SHALL NOT be shown when every folder exists. The manual card SHALL state that those projects were registered manually, and the discovered card SHALL state that Project Discovery manages them. The counts and the folder-health line SHALL refresh whenever the project registry changes, including a manual add or removal made on the page.

#### Scenario: Cards shown

- **WHEN** the Projects page is shown
- **THEN** the page header shows the "PROJECTS", "MANUAL", and "DISCOVERED" cards
- **AND** each card shows its count

#### Scenario: Total card shows the split

- **WHEN** the Projects page is shown
- **THEN** the total card shows the project count
- **AND** beneath it shows the discovered and manual counts, e.g. "31 discovered · 4 manual"

#### Scenario: Folder health hidden when healthy

- **WHEN** every registered project folder exists
- **THEN** the total card shows no folder-health line

#### Scenario: Missing folders reported

- **WHEN** one or more registered project folders no longer exist
- **THEN** the total card reports the number of missing folders in the danger tone
- **AND** the count uses the singular "folder" for one and the plural "folders" otherwise

#### Scenario: Counts follow registry changes

- **WHEN** the user adds or removes a manual project on the Projects page
- **THEN** the three card counts and the folder-health line refresh without reopening the window
