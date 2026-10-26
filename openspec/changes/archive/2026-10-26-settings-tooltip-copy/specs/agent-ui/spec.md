## MODIFIED Requirements

### Requirement: Agent management header actions

The AGENTS section of the CLI Agents page SHALL present two header actions alongside the section header. "Detect" SHALL re-detect every agent's host installation and version locally: it SHALL NOT perform an update-availability check and SHALL NOT request any agent's declared update source. "Check updates" SHALL force a fresh update-availability check for every installed agent by invalidating the agent's cached availability result before checking, bypassing the per-agent time-to-live; it SHALL NOT re-run host-installation detection and SHALL NOT issue a version command at the call site. Each header action SHALL expose a tooltip and automation name that state its own work and distinguish it from the other header action. The two header actions SHALL NOT run concurrently: while either is running, both SHALL be unavailable, and the per-card Detect action SHALL be unavailable as well. The load-time population pass SHALL remain as it is; "Check updates" is an additional trigger for an availability check, not a replacement for the automatic one.

#### Scenario: Header Detect leaves update indicators untouched

- **WHEN** the header "Detect" action runs and a card's agent is installed
- **THEN** the card settles with its freshly probed version and its previously shown update indicator is left as it was
- **AND** no update-availability check ran for it and its declared update source was not requested

#### Scenario: Header Detect settles every card

- **WHEN** the header "Detect" action runs
- **THEN** each card shows "Detecting…" for the duration of its probe and then settles — Installed with the freshly probed version, or Not installed with no version and no update indicator

#### Scenario: Check updates bypasses the cached availability

- **WHEN** the header "Check updates" action runs and a card's agent is installed with an availability result that is cached and within the time-to-live
- **THEN** the cached result is invalidated for that agent and a fresh check runs
- **AND** the card's update indicator reflects the fresh result

#### Scenario: Check updates skips not-installed agents

- **WHEN** the header "Check updates" action runs and a card's agent is not installed on the host
- **THEN** no availability check runs for that agent and its declared update source is not requested
- **AND** any previously shown update indicator for its card is cleared

#### Scenario: Check updates reports a summary

- **WHEN** a "Check updates" pass completes
- **THEN** a muted summary line next to the header actions reports `Checked {n} agents · {m} updates available`, `Checked {n} agents · 1 update available`, or `Checked {n} agents · no updates`, with `agent`/`agents` also singular when one agent was checked
- **AND** when no agent is installed the line reads `No installed agents to check`
- **AND** the line is cleared when the page reloads and when a new check starts

#### Scenario: Header actions are mutually exclusive

- **WHEN** either header action is running
- **THEN** both header actions are unavailable and report their busy state
- **AND** the per-card Detect action is unavailable on every card

#### Scenario: Header actions become available again when a pass completes

- **WHEN** a header action's pass completes, including when it ends abnormally
- **THEN** both header actions are available again and the per-card Detect action is available on every card

#### Scenario: Tooltips disambiguate the header actions

- **WHEN** the user inspects either header action
- **THEN** its tooltip and automation name state only that action's work, using wording that distinguishes it from the other header action
