# Spec Delta: settings-theme

## Purpose

Defines the shared theme of the Settings window, matching the launch window's visual language — the application palette (dark by default, light when the application theme is light), drawn chrome, card layout, and themed controls — so the window reads as part of the same application rather than a system dialog.

## MODIFIED Requirements

### Requirement: Persistent page header

The Settings window SHALL keep the active page's header visible while page content scrolls. The header SHALL show the active page's title and its one-line description above the content, separated from the content by a hairline. A page MAY add a summary row of summary cards between its description and the hairline: the "Project Scanning" page shows the projects, agents, and watcher cards, the "Projects" page shows the total, manual, and discovered cards, the "CLI Agents" page shows the agents, default-agent, and updates cards, and the "Agent Detection" page shows the cache TTL, timeout, and defaults cards. On the "Project Scanning" page the projects card SHALL show the total number of registered projects with the number of discovered and manually added projects beneath it, and both numbers SHALL refresh whenever the scanner status changes. The watcher card SHALL report the number of folders examined by the last scan after its last scan time, using the singular "folder" for one and the plural "folders" otherwise. The vertical scrollbar SHALL belong to the content region below the header, so the drawn title bar, sidebar, page header, summary row, validation message, and footer remain in place while the content scrolls.

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

#### Scenario: Summary detail lines span the card width

- **WHEN** a summary card shows its detail or alert line
- **THEN** the line's text box extends to the card's right content edge, level with the icon block's right edge, instead of stopping at the icon block's left edge
- **AND** the line's text stays left-aligned
- **AND** the label and value lines keep reserving the icon block's column so they never run under the icon

### Requirement: CLI Agents page summary cards

The Settings window SHALL show three summary cards under the "CLI Agents" page header: installed agents, the selected default agent, and agent update availability. The agents card SHALL show the number of installed agents with the supported count and the number of agents that are not installed. The default card SHALL show the selected default agent name, or "None" when no default is selected, and SHALL show the detected version beneath it when the version is known. The updates card SHALL show the number of installed agents with available updates when known, "None" when every installed agent is up to date (or no agents are installed), or "Unknown" while update availability has not been checked, and SHALL show appropriate detail and tone (accent when updates exist, success when all up to date, muted otherwise). The cards SHALL refresh when the agent cards are rebuilt or a detection pass re-runs, when the default agent changes, and when update availability is checked (load-time pass, header check, per-card checks). The detection card is no longer part of this page; the probe scenarios below record that relocated behavior, which the "Agent Detection page summary cards" requirement provides.

#### Scenario: Cards shown

- **WHEN** the CLI Agents page is shown
- **THEN** the page header shows the "AGENTS", "DEFAULT", and "UPDATES" cards

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

- **WHEN** the Agent Detection page detection cache TTL and version-check timeout fields are empty
- **THEN** the Agent Detection page's detection cards show the built-in default values and state that they are built-in defaults

#### Scenario: Custom probe values

- **WHEN** the Agent Detection page detection cache TTL or version-check timeout field contains a positive value
- **THEN** the Agent Detection page's detection cards show that value and state that the detection settings are custom

#### Scenario: Cards follow live changes

- **WHEN** a detection pass changes an agent's installed state or the default agent is changed or update availability is checked
- **THEN** the affected summary card refreshes without reopening the window

#### Scenario: Updates card shown

- **WHEN** the CLI Agents page is shown
- **THEN** the updates card uses the Update icon and shows the agent update availability aggregation

#### Scenario: Updates card value while unknown

- **WHEN** update availability has not been checked for any installed agent (on page load, before the first check)
- **THEN** the updates card shows "Unknown" and a muted tone

#### Scenario: Updates card value when all up to date

- **WHEN** every installed agent has been checked and has status UpToDate (or no agents are installed)
- **THEN** the updates card shows "None" and a success tone

#### Scenario: Updates card value when updates available

- **WHEN** N installed agents (N > 0) have status UpdateAvailable
- **THEN** the updates card shows the count as a string (e.g., "3") and an accent tone

#### Scenario: Updates card detail while unknown

- **WHEN** the updates card value is "Unknown"
- **THEN** the detail line shows "Run Check updates" or equivalent

#### Scenario: Updates card detail when all up to date

- **WHEN** the updates card value is "None"
- **THEN** the detail line shows "All installed agents up to date" or equivalent

#### Scenario: Updates card detail when updates available

- **WHEN** the updates card value is a count N
- **THEN** the detail line shows "N of M installed agents" with singular/plural handling (e.g., "1 of 3 installed agents" or "3 of 5 installed agents")

#### Scenario: Updates card detail when no installed agents

- **WHEN** no agents are installed
- **THEN** the detail line shows "No installed agents" or equivalent

#### Scenario: Updates card refreshes after load-time availability pass

- **WHEN** the Settings window opens on CLI Agents and the load-time availability pass completes
- **THEN** the updates card updates to show the aggregated update status without reopening the window

#### Scenario: Updates card refreshes after header update check

- **WHEN** the user invokes the "Check updates" action above the agent cards and the check completes
- **THEN** the updates card updates to show the fresh aggregated update status

#### Scenario: Updates card refreshes after per-card update check

- **WHEN** the user invokes a per-card "Check updates" action on one agent card and the check completes
- **THEN** the updates card updates to reflect the fresh availability for that agent

#### Scenario: Updates card refreshes after detection pass

- **WHEN** the user invokes the "Detect" action above the agent cards and the detection completes
- **THEN** the updates card updates to reflect the cleared stale availability

## ADDED Requirements

### Requirement: Agent Detection page summary cards

The Settings window SHALL show three summary cards under the "Agent Detection" page header: cache TTL, timeout, and defaults. The cache TTL card SHALL show the effective detection cache time-to-live in minutes with the "min" unit, resolving an empty field value to the built-in default, and SHALL state whether the value is a built-in default or custom with the default numeric value for comparison. The timeout card SHALL show the effective version-check timeout in seconds with the "sec" unit, resolving an empty field value to the built-in default, and SHALL state whether the value is a built-in default or custom with the default numeric value for comparison. The defaults card SHALL show the built-in default values for both detection parameters in the format "N min · M sec" (e.g., "15 min · 10 sec") and SHALL state that these values are used when a field is empty. All three cards SHALL use the Diagnostic icon. The cache TTL and timeout cards SHALL refresh when either detection field is edited.

#### Scenario: Cards shown

- **WHEN** the Agent Detection page is shown
- **THEN** the page header shows the "CACHE TTL", "TIMEOUT", and "DEFAULTS" cards using the Diagnostic icon

#### Scenario: Cache TTL card shows the effective value

- **WHEN** the Agent Detection page is shown
- **THEN** the cache TTL card shows the effective cache time-to-live with the "min" unit
- **AND** the card resolves an empty field value to the built-in default

#### Scenario: Timeout card shows the effective value

- **WHEN** the Agent Detection page is shown
- **THEN** the timeout card shows the effective version-check timeout with the "sec" unit
- **AND** the card resolves an empty field value to the built-in default

#### Scenario: Built-in default detail

- **WHEN** either detection field is empty (using the built-in default)
- **THEN** the corresponding card's detail line states "Built-in default"

#### Scenario: Custom value detail

- **WHEN** either detection field contains a positive custom value
- **THEN** the corresponding card's detail line states "Custom (default: N min|sec)" with the built-in default numeric value for comparison

#### Scenario: Cards follow live changes

- **WHEN** a detection field is edited
- **THEN** the corresponding summary card refreshes without reopening the window

#### Scenario: Defaults card shown

- **WHEN** the Agent Detection page is shown
- **THEN** the defaults card uses the Diagnostic icon and shows the built-in default values for both detection parameters

#### Scenario: Defaults card value

- **WHEN** the defaults card is visible
- **THEN** the value line shows the built-in defaults formatted as "N min · M sec" (e.g., "15 min · 10 sec") where N is the TTL default and M is the timeout default

#### Scenario: Defaults card detail

- **WHEN** the defaults card is visible
- **THEN** the detail line shows "Used when a field is empty" or equivalent
