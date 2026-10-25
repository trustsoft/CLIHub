# settings-theme delta

## MODIFIED Requirements

### Requirement: Sectioned form layout

The settings content SHALL be laid out as sections introduced by uppercase muted section labels, with fields grouped under them and a short hint line under non-obvious fields, matching the mockup's rhythm.

#### Scenario: Section labels
- **WHEN** a section such as "Default runtime", "Global hotkey", "Agents detection", or "Updates" is rendered
- **THEN** it is introduced by an uppercase muted label in the section-label style shared with the launch window

#### Scenario: Hint lines
- **WHEN** the hotkey or agent detection fields are rendered
- **THEN** their existing hint text appears under the fields in the muted hint style

### Requirement: Dark input controls

The hotkey capture field, detection cache TTL and version-check timeout fields, checkboxes, and the "Check for updates" button SHALL use the themed input and control styling for the active palette, consistent with the launch window.

#### Scenario: Hotkey chips
- **WHEN** a combination is captured or loaded
- **THEN** the hotkey field shows the combination as bordered key chips (for example Ctrl, Alt, Space) on the themed input surface, with the capture hint below

#### Scenario: Text fields remain readable
- **WHEN** the detection cache TTL and version-check timeout fields hold values
- **THEN** the values are rendered as text that contrasts with the input surface and has visible field boundaries

#### Scenario: Checkbox and button styling
- **WHEN** the startup, window-visibility, and update checkboxes and the "Check for updates" button are rendered
- **THEN** they use the themed control styling with the accent for the checked and enabled states

### Requirement: Cards on the data pages

The Project Discovery and CLI Agents pages SHALL present their settings as cards in the shared palette, with a page description and section headers, consistent with the other settings pages. List editors SHALL be rendered as stacked cards that carry their list and controls. The per-agent cards SHALL follow the shared card visual language (a rounded bordered surface, an icon block, a title, and a hint).

#### Scenario: Projects page cards

- **WHEN** the Project Discovery page is shown
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

The Settings window SHALL keep the active page's header visible while page content scrolls. The header SHALL show the active page's title and its one-line description above the content, separated from the content by a hairline. A page MAY add a summary row of summary cards between its description and the hairline: the "Project Discovery" page shows the projects, agents, and watcher cards, the "Projects" page shows the total, manual, and discovered cards, and the "CLI Agents" page shows the agents, default-agent, and detection cards. On the "Project Discovery" page the projects card SHALL show the total number of registered projects with the number of discovered and manually added projects beneath it, and both numbers SHALL refresh whenever the scanner status changes. The watcher card SHALL report the number of folders examined by the last scan after its last scan time, using the singular "folder" for one and the plural "folders" otherwise. The vertical scrollbar SHALL belong to the content region below the header, so the drawn title bar, sidebar, page header, summary row, validation message, and footer remain in place while the content scrolls.

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

### Requirement: CLI Agents page summary cards

The Settings window SHALL show three summary cards under the "CLI Agents" page header: installed agents, the selected default agent, and the effective detection policy. The agents card SHALL show the number of installed agents with the supported count and the number of agents that are not installed. The default card SHALL show the selected default agent name, or "None" when no default is selected, and SHALL show the detected version beneath it when the version is known. The detection card SHALL show the effective detection cache TTL in minutes and version-check timeout in seconds, resolving empty values to the built-in defaults, and SHALL state whether the values are built-in defaults or custom. The cards SHALL refresh when the agent cards are rebuilt or a detection pass re-runs, when the default agent changes, and when the detection fields are edited.

#### Scenario: Cards shown

- **WHEN** the CLI Agents page is shown
- **THEN** the page header shows the "AGENTS", "DEFAULT", and "DETECTION" cards

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

- **WHEN** the detection cache TTL and version-check timeout fields are empty
- **THEN** the detection card shows the built-in default values and states that they are built-in defaults

#### Scenario: Custom probe values

- **WHEN** the detection cache TTL or version-check timeout field contains a positive value
- **THEN** the detection card shows that value and states that the detection settings are custom

#### Scenario: Cards follow live changes

- **WHEN** a detection pass changes an agent's installed state, the default agent is changed, or a detection field is edited
- **THEN** the affected summary card refreshes without reopening the window
