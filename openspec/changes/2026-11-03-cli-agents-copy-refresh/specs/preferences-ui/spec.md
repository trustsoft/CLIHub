# preferences-ui Specification Delta

## MODIFIED Requirements

### Requirement: CLI Agents page management

The CLI Agents page SHALL present one card per loaded agent, the page-header summary cards showing the supported and installed agent counts, the default agent selection, and the agent update availability aggregation, and a "Default agent" selector. It SHALL show the stored default agent when opened. The "Default agent" selector SHALL offer every loaded agent plus a "None" option, SHALL mark agents that are not installed on the host as disabled entries that cannot be selected, and SHALL refresh the option availability when agent detection runs. The page SHALL offer an agent detection action labelled "Detect" above the agent cards that re-detects every supported agent's installed state and version, and refreshes the agent cards, the availability summary, and the option availability.

The summary cards showing supported and installed agent counts and update availability SHALL update immediately when agent detection completes, without requiring the Settings window to be closed and reopened. The UPDATES summary card SHALL show the number of installed agents with available updates when known, "None" when every installed agent is up to date, or "Unknown" while update availability is not checked, and SHALL refresh after the load-time availability pass, the header update check, per-card update checks, and detection passes that clear stale availability.

#### Scenario: Options offered

- **WHEN** the Settings window opens on "CLI Agents"
- **THEN** it shows the agent cards, the summary cards with the supported and installed counts, default agent selection, and update availability, and the "Default agent" selector

#### Scenario: One card per agent

- **WHEN** the plugin catalog contains agents
- **THEN** the page shows one card for each loaded agent

#### Scenario: Metrics shown

- **WHEN** the page opens
- **THEN** the supported count is the number of loaded agents and the installed count is the number installed on the host

#### Scenario: Metrics update when Detect completes

- **WHEN** agent detection completes while the Settings window is open (from the Detect action or a per-card test)
- **THEN** the supported and installed counts on the CLI Agents page summary cards update immediately without reopening the window

#### Scenario: Stored default agent loaded

- **WHEN** a default agent has been saved
- **THEN** the "Default agent" selector shows it selected

#### Scenario: No stored default agent

- **WHEN** no default agent preference has been stored
- **THEN** the selector offers a "None" option and no agent is selected by default

#### Scenario: Uninstalled agents are not selectable

- **WHEN** the "Default agent" dropdown is opened
- **THEN** agents that are not installed on the host are shown dimmed and disabled
- **AND** they cannot be selected, while "None" and installed agents remain selectable

#### Scenario: Detection refreshes option availability

- **WHEN** an agent detection pass completes while the page is open, whether from a single-agent test or the detect action above the agent cards
- **THEN** the option availability reflects the fresh installed states without reopening the window

#### Scenario: Detect action re-probes every supported agent

- **WHEN** the user invokes the detect action above the agent cards
- **THEN** the installed state and version of every supported agent are re-detected against the host
- **AND** the agent cards, the availability summary, and the option availability refresh without reopening the window
- **AND** the action is unavailable while a detection pass is already running

#### Scenario: Detect action label

- **WHEN** the CLI Agents page is shown
- **THEN** the detection action above the agent cards is labelled "Detect"

#### Scenario: UPDATES card shown

- **WHEN** the CLI Agents page header is visible
- **THEN** the third summary card is labelled "UPDATES" with the Update icon and shows the agent update availability aggregation

#### Scenario: UPDATES card value while unknown

- **WHEN** update availability has not been checked for any installed agent (on page load, before the first check)
- **THEN** the UPDATES card shows "Unknown" and a muted tone

#### Scenario: UPDATES card value when all up to date

- **WHEN** every installed agent has been checked and has status UpToDate (or no agents are installed)
- **THEN** the UPDATES card shows "None" and a success tone

#### Scenario: UPDATES card value when updates available

- **WHEN** N installed agents (N > 0) have status UpdateAvailable
- **THEN** the UPDATES card shows the count as a string (e.g., "3") and an accent tone

#### Scenario: UPDATES card detail while unknown

- **WHEN** the UPDATES card value is "Unknown"
- **THEN** the detail line shows "Check updates"

**Delta**: Changed from `"Run Check updates"` to `"Check updates"` to match the button label without the "Run" prefix.

#### Scenario: UPDATES card detail when all up to date

- **WHEN** the UPDATES card value is "None"
- **THEN** the detail line shows "All installed agents up to date" or equivalent

#### Scenario: UPDATES card detail when updates available

- **WHEN** the UPDATES card value is a count N
- **THEN** the detail line shows "N of M agents have updates" with singular/plural handling: "1 of M agent(s) has an update" when N=1, otherwise "N of M agent(s) have updates", where the "agent(s)" part pluralizes based on M (the installed count)

**Delta**: Changed from `"N of M installed agents"` to `"N of M agents have updates"` (adding the verb "have updates" to disambiguate), and from `"1 of M installed agents"` to `"1 of M agent(s) has an update"` (singular verb for count=1). The word "installed" is dropped because the context is update availability for installed agents.

#### Scenario: UPDATES card detail when no installed agents

- **WHEN** no agents are installed
- **THEN** the detail line shows "No installed agents" or equivalent

#### Scenario: UPDATES card refreshes after load-time availability pass

- **WHEN** the Settings window opens on CLI Agents and the load-time availability pass completes
- **THEN** the UPDATES card updates to show the aggregated update status without reopening the window

#### Scenario: UPDATES card refreshes after header update check

- **WHEN** the user invokes the "Check updates" action above the agent cards and the check completes
- **THEN** the UPDATES card updates to show the fresh aggregated update status

#### Scenario: UPDATES card refreshes after per-card update check

- **WHEN** the user invokes a per-card "Check updates" action on one agent card and the check completes
- **THEN** the UPDATES card updates to reflect the fresh availability for that agent

#### Scenario: UPDATES card refreshes after detection pass

- **WHEN** the user invokes the "Detect" action above the agent cards and the detection completes
- **THEN** the UPDATES card updates to reflect the cleared stale availability
