# settings-theme Specification Delta

## MODIFIED Requirements

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

**Delta**: The hint changes from "No default agent — choose one" to "Choose an agent below" to avoid duplicating "None" in the detail line.

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
- **THEN** the detail line shows "Check updates"

**Delta**: Changed from `"Run Check updates"` to `"Check updates"` to match the button label without the "Run" prefix.

#### Scenario: Updates card detail when all up to date

- **WHEN** the updates card value is "None"
- **THEN** the detail line shows "All installed agents up to date" or equivalent

#### Scenario: Updates card detail when updates available

- **WHEN** the updates card value is a count N
- **THEN** the detail line shows "N of M agents have updates" with singular/plural handling: "1 of M agent(s) has an update" when N=1, otherwise "N of M agent(s) have updates", where the "agent(s)" part pluralizes based on M (the installed count)

**Delta**: Changed from `"N of M installed agents"` to `"N of M agents have updates"` (adding the verb "have updates" to disambiguate), and from `"1 of M installed agents"` to `"1 of M agent(s) has an update"` (singular verb for count=1). The word "installed" is dropped because the context is update availability for installed agents.

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
