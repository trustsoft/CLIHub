## ADDED Requirements

### Requirement: CLI Agents page summary cards

The Settings window SHALL show three summary cards under the "CLI Agents" page header: installed agents, the selected default agent, and the effective probe policy. The agents card SHALL show the number of installed agents with the supported count and the number of agents that are not installed. The default card SHALL show the selected default agent name, or "None" when no default is selected, and SHALL show the detected version beneath it when the version is known. The probe card SHALL show the effective probe TTL in minutes and timeout in seconds, resolving empty values to the built-in defaults, and SHALL state whether the values are built-in defaults or custom. The cards SHALL refresh when the agent cards are rebuilt or re-probed, when the default agent changes, and when the probe fields are edited.

#### Scenario: Cards shown

- **WHEN** the CLI Agents page is shown
- **THEN** the page header shows the "AGENTS", "DEFAULT", and "PROBE" cards

#### Scenario: Agents card shows the availability split

- **WHEN** the CLI Agents page is shown
- **THEN** the agents card shows the installed-agent count
- **AND** beneath it shows the supported count and the number of agents that are not installed

#### Scenario: Default agent shown

- **WHEN** a default agent is selected
- **THEN** the default card shows its name and, when known, its detected version

#### Scenario: No default agent

- **WHEN** no default agent is selected
- **THEN** the default card shows "None" and a hint to choose an agent

#### Scenario: Probe card shows the effective policy

- **WHEN** the probe TTL and timeout fields are empty
- **THEN** the probe card shows the built-in default values and states that they are built-in defaults

#### Scenario: Custom probe values

- **WHEN** the probe TTL or timeout field contains a positive value
- **THEN** the probe card shows that value and states that the probe settings are custom

#### Scenario: Cards follow live changes

- **WHEN** an agent probe changes an agent's installed state, the default agent is changed, or a probe field is edited
- **THEN** the affected summary card refreshes without reopening the window

## MODIFIED Requirements

### Requirement: Persistent page header

The Settings window SHALL keep the active page's header visible while page content scrolls. The header SHALL show the active page's title and its one-line description above the content, separated from the content by a hairline. A page MAY add a summary row of summary cards between its description and the hairline: the "Project Discovery" page shows the projects, agents, and watcher cards, the "Projects" page shows the total, manual, and discovered cards, and the "CLI Agents" page shows the agents, default-agent, and probe cards. On the "Project Discovery" page the projects card SHALL show the total number of registered projects with the number of discovered and manually added projects beneath it, and both numbers SHALL refresh whenever the scanner status changes. The watcher card SHALL report the number of folders examined by the last scan after its last scan time, using the singular "folder" for one and the plural "folders" otherwise. The vertical scrollbar SHALL belong to the content region below the header, so the drawn title bar, sidebar, page header, summary row, validation message, and footer remain in place while the content scrolls.

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

- **WHEN** the "Project Discovery" page is active and its content is taller than the content region
- **THEN** the three summary cards for projects, agents, and watcher remain visible in the page header while the content scrolls
- **AND** the header hairline sits below the summary cards
- **AND** a page without summary cards, such as "General & Startup", shows no summary row in the header

#### Scenario: Projects card shows the provenance split

- **WHEN** the "Project Discovery" page is shown
- **THEN** the projects card shows the total number of registered projects
- **AND** beneath it shows the number of discovered and manually added projects, e.g. "31 discovered · 4 manual"
- **AND** both numbers refresh whenever the scanner status changes

#### Scenario: Watcher card shows scan coverage

- **WHEN** the "Project Discovery" page is shown after a scan completed
- **THEN** the watcher card shows the last scan time followed by the number of folders the last scan examined
- **AND** the folder count refreshes whenever the scanner status changes
- **AND** the folder count reads "1 folder" for one folder and "N folders" otherwise

#### Scenario: Projects content starts below the header

- **WHEN** the "Project Discovery" page is shown
- **THEN** the scrollable content begins at the "WATCHED REPOSITORY DIRECTORIES" section below the header divider

