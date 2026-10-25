# settings-theme delta

## MODIFIED Requirements

### Requirement: Cards on the data pages

The Project Scanning and CLI Agents pages SHALL present their settings as cards in the shared palette, with a page description and section headers, consistent with the other settings pages. List editors SHALL be rendered as stacked cards that carry their list and controls. The per-agent cards SHALL follow the shared card visual language (a rounded bordered surface, an icon block, a title, and a hint).

#### Scenario: Projects page cards

- **WHEN** the Project Scanning page is shown
- **THEN** watched directories, scan depth, discovery mode, ignored patterns, and the watcher status each appear as cards under the page description and section headers

#### Scenario: List editor card

- **WHEN** a list setting (watched directories or ignored patterns) is shown
- **THEN** its list and its add/remove controls appear inside a card whose title and hint are on top and the content is stacked

#### Scenario: CLI Agents page cards

- **WHEN** the CLI Agents page is shown
- **THEN** the default-agent selector and the agent detection fields appear as cards under the page description and section headers, and the agent list follows under the management section

#### Scenario: Default agent dropdown

- **WHEN** the default agent is shown
- **THEN** it is a dropdown selector (a combo box) offering "None" and the loaded agents, showing the stored choice

#### Scenario: Agent cards share the visual language

- **WHEN** a per-agent card is shown
- **THEN** it uses the same rounded bordered surface, title typography, and icon block treatment as the other setting cards

### Requirement: Persistent page header

The Settings window SHALL keep the active page's header visible while page content scrolls. The header SHALL show the active page's title and its one-line description above the content, separated from the content by a hairline. A page MAY add a summary row of summary cards between its description and the hairline: the "Project Scanning" page shows the projects, agents, and watcher cards, the "Projects" page shows the total, manual, and discovered cards, and the "CLI Agents" page shows the agents, default-agent, and detection cards. On the "Project Scanning" page the projects card SHALL show the total number of registered projects with the number of discovered and manually added projects beneath it, and both numbers SHALL refresh whenever the scanner status changes. The watcher card SHALL report the number of folders examined by the last scan after its last scan time, using the singular "folder" for one and the plural "folders" otherwise. The vertical scrollbar SHALL belong to the content region below the header, so the drawn title bar, sidebar, page header, summary row, validation message, and footer remain in place while the content scrolls.

#### Scenario: Content taller than the window

- **WHEN** the active page's content is taller than the content region
- **THEN** a vertical scrollbar appears on the content region below the page header
- **AND** scrolling the content leaves the page title, description, and divider visible
- **AND** the drawn title bar, sidebar, and footer do not scroll

#### Scenario: Content fits the window

- **WHEN** the active page's content fits in the content region
- **THEN** no vertical scrollbar is shown and the page header remains in place

#### Scenario: Header follows page switches

- **WHEN** the user selects another page in the sidebar
- **THEN** the fixed header shows that page's title and description
- **AND** the page's content is shown below the same header

#### Scenario: Page without a description

- **WHEN** the active page has no description
- **THEN** the header shows the page title without an empty description line

#### Scenario: Projects summary stays pinned

- **WHEN** the "Project Scanning" page is active and its content is taller than the content region
- **THEN** the three summary cards for projects, agents, and watcher remain visible in the page header while the content scrolls
- **AND** the header hairline sits below the summary cards
- **AND** a page without summary cards, such as "General & Startup", shows no summary row in the header

#### Scenario: Projects card shows the provenance split

- **WHEN** the "Project Scanning" page is shown
- **THEN** the projects card shows the total number of registered projects
- **AND** beneath it shows the number of discovered and manually added projects, e.g. "31 discovered · 4 manual"
- **AND** both numbers refresh whenever the scanner status changes

#### Scenario: Watcher card shows scan coverage

- **WHEN** the "Project Scanning" page is shown after a scan completed
- **THEN** the watcher card shows the last scan time followed by the number of folders the last scan examined
- **AND** the folder count refreshes whenever the scanner status changes
- **AND** the folder count reads "1 folder" for one folder and "N folders" otherwise

#### Scenario: Projects content starts below the header

- **WHEN** the "Project Scanning" page is shown
- **THEN** the scrollable content begins at the "WATCHED REPOSITORY DIRECTORIES" section below the header divider

### Requirement: Projects page summary cards

The Settings window SHALL show three summary cards under the "Projects" page header: total projects, manual projects, and discovered projects. The total card SHALL show the manual/discovered split and a folder-health line that names the number of projects whose folder no longer exists using the danger tone; the folder-health line SHALL NOT be shown when every folder exists. The manual card SHALL state that those projects were registered manually, and the discovered card SHALL state that Project Scanning manages them. The counts and the folder-health line SHALL refresh whenever the project registry changes, including a manual add or removal made on the page.

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
