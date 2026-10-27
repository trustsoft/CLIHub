## MODIFIED Requirements

### Requirement: Agent card actions

The card SHALL present actions appropriate to the agent's declared capabilities and current state, in the order "Detect", "Check updates", "Update", "Edit config". A card SHALL render with "Update" available exactly when the agent declares an `update` command; the running-agent blocked state SHALL be populated asynchronously after the cards render, through a population pass that is cancelled when the list is reloaded or the surface that started it goes away, so that a result from an obsolete pass is not applied. Until then, a card MAY transiently show "Update" as available while a process matching its agent is running; choosing "Update" in that state SHALL NOT start the update — the execution-time running-agent check blocks it and the card reports the block. When no agent declares an `update` command, no running-agent enumeration SHALL run. An enumeration or population failure SHALL be logged and SHALL leave the cards showing "Update" per declaration. A process matches an agent per the `agent-process-matching` capability, including a process hosted inside a shared runtime whose command line identifies the agent. The card's "Detect" action re-detects the agent's host installation and version locally; it SHALL NOT trigger an update-availability check, and it SHALL be unavailable while a header-wide action is running and while the card's own "Detect" or "Check updates" action is running. The card's "Check updates" action SHALL force a fresh update-availability check for that one agent by invalidating only that agent's cached availability result before checking, bypassing the per-agent time-to-live; it SHALL NOT re-run host-installation detection and SHALL NOT issue a version command at the call site. It SHALL be unavailable for an agent whose installation state is not installed, while the card's own "Detect" or "Check updates" action is running, and while a header-wide action is running. Per-card actions SHALL be independent across cards: starting "Detect" or "Check updates" on one card SHALL NOT disable another card's "Detect" or "Check updates" actions, which remain available under their own installation and busy rules. The per-card check SHALL NOT alter the header-wide update-check summary or the Update action's availability, and SHALL report its outcome on the card's message line. The per-card "Detect" action SHALL render the same glyph as the header "Detect" action, and the per-card "Check updates" action the same glyph as the header "Check updates" action.

#### Scenario: Detect available on every card

- **WHEN** an agent card is shown, no header-wide action is running, and the card's own "Detect" and "Check updates" actions are not running
- **THEN** "Detect" is available on the card

#### Scenario: Per-card Detect is local only

- **WHEN** the per-card "Detect" action runs
- **THEN** the card is re-probed for host installation and version
- **AND** no update-availability check runs for it and its declared update source is not requested
- **AND** its previously shown update indicator is kept unless the probe reports the agent not installed

#### Scenario: Update requires the update command

- **WHEN** an agent does not declare an `update` command
- **THEN** "Update" is unavailable on its card

#### Scenario: Update blocked by a running agent

- **WHEN** a process matching the agent being updated is running
- **THEN** "Update" is unavailable and the card explains that the running agent blocks it

#### Scenario: Other running agents do not block an update

- **WHEN** processes of other agents are running and the agent being updated has no running processes
- **THEN** "Update" is available

#### Scenario: Edit Config requires a plugin file

- **WHEN** the agent's `plugin.json` path is known
- **THEN** "Edit Config" is available; otherwise it is unavailable

#### Scenario: Blocked state populates after the cards render

- **WHEN** the CLI Agents page loads and at least one agent declares an `update` command
- **THEN** the cards render immediately with "Update" available exactly for the agents that declare an `update` command
- **AND** when the running-agent enumeration completes, each card's "Update" availability and its block explanation reflect the enumeration result

#### Scenario: Transient declarative availability while the blocked state is pending

- **WHEN** a card is shown while its running-agent enumeration is still pending and a process matching that agent is running
- **THEN** the card may show "Update" as available until the enumeration completes
- **AND** choosing "Update" in that state does not start the update; the execution-time running-agent check blocks it and the card reports the block
- **AND** once the enumeration completes, "Update" is unavailable and the card explains that the running agent blocks it

#### Scenario: Reload supersedes the blocked-state pass

- **WHEN** the page is loaded again while a blocked-state pass from an earlier load is still running
- **THEN** the earlier pass is cancelled and its result is not applied to the new cards
- **AND** the new load's own pass populates the new cards

#### Scenario: Surface exit cancels the blocked-state pass

- **WHEN** the surface showing the cards is closed while its blocked-state pass is still running
- **THEN** the pass is cancelled and its result is not applied
- **AND** a running-agent enumeration already started may complete without being applied to the closed surface

#### Scenario: Blocked-state enumeration failure is logged

- **WHEN** the running-agent enumeration fails
- **THEN** the failure is logged through the application logger
- **AND** the cards keep "Update" available exactly for the agents that declare an `update` command (no block is shown)

#### Scenario: Update blocked by a runtime-hosted agent

- **WHEN** the agent being updated runs inside a shared runtime host (for example a `node.exe` process started by the agent's npm shim whose command line names the agent's declared npm package) and no executable-name match exists
- **THEN** once the blocked-state pass settles, "Update" is unavailable and the card explains that the running agent blocks it

#### Scenario: Unrelated runtime-hosted processes do not block an update

- **WHEN** runtime-host processes are running whose command lines do not name any declared npm package of the agent being updated (for example an unrelated node script)
- **THEN** "Update" stays available for that agent

#### Scenario: Per-card Check updates bypasses the cached availability

- **WHEN** the per-card "Check updates" action runs for an installed agent whose availability result is cached and within the time-to-live
- **THEN** that agent's cached result is invalidated and a fresh check runs, while other agents' cached results remain valid within their time-to-live
- **AND** no host-installation detection runs for it and no version command is issued at the call site
- **AND** the card's update indicator reflects the fresh result

#### Scenario: Per-card Check updates is gated on host installation

- **WHEN** a card's agent is not installed on the host
- **THEN** the per-card "Check updates" action is unavailable
- **AND** no availability check runs for that agent and its declared update source is not requested
- **AND** any previously shown update indicator for its card is cleared

#### Scenario: Per-card Check updates reports its outcome on the card

- **WHEN** a per-card "Check updates" action completes for an installed agent
- **THEN** the card's message line reports `Update available · {installed} → {latest}` when an update is available, `Up to date` when the agent is up to date, and `Update availability unknown` when availability could not be determined
- **AND** the header-wide update-check summary line is left unchanged
- **AND** the Update action's availability is unchanged

#### Scenario: Per-card Check updates is mutually exclusive

- **WHEN** a header-wide Detect or "Check updates" pass is running
- **THEN** the per-card "Check updates" action is unavailable on every card
- **WHEN** a per-card "Check updates" action is running on a card
- **THEN** that card's own "Detect" and "Check updates" actions are unavailable
- **AND** when the per-card check completes, including when it ends abnormally, the card's actions become available again under their own rules

#### Scenario: Per-card Detect is independent of other cards

- **WHEN** a per-card "Detect" action is running on one card
- **THEN** that card's own "Detect" and "Check updates" actions are unavailable
- **AND** the "Detect" and "Check updates" actions on every other card remain available under their own installation and busy rules
- **AND** when the Detect completes, including when it ends abnormally, the card's own actions become available again under their own rules

#### Scenario: Per-card Check updates is independent of other cards

- **WHEN** a per-card "Check updates" action is running on one card
- **THEN** the "Detect" and "Check updates" actions on every other card remain available under their own installation and busy rules
- **AND** while the check runs, a per-card action may be started on another card

#### Scenario: Tooltips disambiguate the per-card Check updates action

- **WHEN** the user inspects the per-card "Check updates" action
- **THEN** its tooltip and automation name state its own work — checking this one agent for updates — using wording that distinguishes it from the header "Check updates" action and from the per-card "Update" action

#### Scenario: Icon parity with the header actions

- **WHEN** the user views a card's action row
- **THEN** the per-card "Detect" action renders the same glyph as the header "Detect" action
- **AND** the per-card "Check updates" action renders the same glyph as the header "Check updates" action

### Requirement: Agent management header actions

The AGENTS section of the CLI Agents page SHALL present two header actions alongside the section header. "Detect" SHALL re-detect every agent's host installation and version locally: it SHALL NOT perform an update-availability check and SHALL NOT request any agent's declared update source. "Check updates" SHALL force a fresh update-availability check for every installed agent by invalidating the agent's cached availability result before checking, bypassing the per-agent time-to-live; it SHALL NOT re-run host-installation detection and SHALL NOT issue a version command at the call site. Each header action SHALL expose a tooltip and automation name that state its own work and distinguish it from the other header action. The two header actions SHALL NOT run concurrently: while either is running, both SHALL be unavailable, and the per-card Detect and per-card "Check updates" actions SHALL be unavailable as well. While any per-card action — Detect or "Check updates" — is running, both header actions SHALL be unavailable. The load-time population pass SHALL remain as it is; "Check updates" is an additional trigger for an availability check, not a replacement for the automatic one.

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
- **AND** the per-card "Check updates" action is unavailable on every card

#### Scenario: Header actions become available again when a pass completes

- **WHEN** a header action's pass completes, including when it ends abnormally
- **THEN** both header actions are available again and the per-card Detect action is available on every card
- **AND** the per-card "Check updates" action is available again on every card whose agent is installed

#### Scenario: Tooltips disambiguate the header actions

- **WHEN** the user inspects either header action
- **THEN** its tooltip and automation name state only that action's work, using wording that distinguishes it from the other header action

#### Scenario: A per-card check gates the header actions

- **WHEN** a per-card "Check updates" action is running
- **THEN** both header actions are unavailable
- **AND** when the per-card check completes, including when it ends abnormally, the header actions become available again under their own rules

#### Scenario: A per-card Detect gates the header actions

- **WHEN** a per-card "Detect" action is running
- **THEN** both header actions are unavailable
- **AND** when the per-card Detect completes, including when it ends abnormally, the header actions become available again under their own rules
