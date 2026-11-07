## MODIFIED Requirements

### Requirement: Projects page summary cards

The Settings window SHALL show three summary cards under the "Projects" page header: total projects,
manual projects, and discovered projects. The total card SHALL show the manual/discovered split and
a folder-health indicator that names the number of projects whose folder no longer exists using the
danger tone; the folder-health indicator SHALL NOT be shown when every folder exists. The manual card
SHALL state that those projects were registered manually, and the discovered card SHALL state that
Project Scanning manages them. The counts and the folder-health indicator SHALL refresh whenever the
project registry changes, including a manual add or removal made on the page.

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
- **THEN** the total card shows no folder-health indicator

#### Scenario: Missing folders reported

- **WHEN** one or more registered project folders no longer exist
- **THEN** the total card shows a compact danger chip immediately after the project count on the value row, using the text "{n} missing" (e.g. "1 missing"), with a visible gap from the count value
- **AND** the icon block carries a danger count badge with the number of missing folders at its top-right corner
- **AND** both the chip and the count badge share one tooltip showing the full text in the form "{n} folder missing · folders that no longer exist", using the singular "folder" for one and the plural "folders" otherwise
- **AND** the card height does not increase compared to a card with no missing folders
- **AND** the status is announced by screen readers without requiring keyboard focus on the card

#### Scenario: Indicators collapse when folders are healthy

- **WHEN** every registered project folder exists (or when the last missing folder is restored)
- **THEN** the compact chip and the icon count badge are both hidden
- **AND** the card returns to its plain three-line form with no visible danger indicator

#### Scenario: Counts follow registry changes

- **WHEN** the user adds or removes a manual project on the Projects page
- **THEN** the three card counts and the folder-health indicator refresh without reopening the window
