# agent-ui Specification Delta

## MODIFIED Requirements

### Requirement: Update command support detection

The system SHALL detect whether an agent supports the update command. For the launch-window model (`AgentItem`), `CanUpdate` is true exactly when the agent's descriptor declares an `update` command. For the Settings card model (`AgentCardItem`), `CanUpdate` is gated on update availability; see requirement "Agent card actions" for the Settings card rule.

#### Scenario: Detect Update Command Support
```
GIVEN an agent with plugin.json containing an update command definition
WHEN the system checks the launch-window model's CanUpdate property (AgentItem)
THEN it SHALL return true
```

#### Scenario: Detect Missing Update Command
```
GIVEN an agent with plugin.json lacking an update command definition
WHEN the system checks the launch-window model's CanUpdate property (AgentItem)
THEN it SHALL return false
```

#### Scenario: Settings card CanUpdate is gated
```
GIVEN an agent card in Settings → CLI Agents
WHEN the system checks AgentCardItem.CanUpdate property
THEN it SHALL return true only when UpdateAvailability.Status is UpdateAvailable AND the agent declares an update command AND no matching agent process is running
```

---

### Requirement: Agent card displayed content

An agent's management card SHALL present its declared identity and current state: display name, logo, a state chip next to the name that shows the installed version when the agent is installed with a resolved version, an "Installed" state when it is installed without one, a "Not installed" state when it is missing, or a "Detecting…" state while the card's installation state is being probed, and the declared command set listed after a "Supported commands" label. The state chip text SHALL be derived from the card's installation state, never from version placeholder text; the "Not installed" state SHALL keep its danger styling, and the "Detecting…" state SHALL use the chip's default styling. The card SHALL NOT present the plugin folder path or the declared detection markers, and SHALL NOT render a separate origin badge. The agent name's tooltip SHALL present the recorded origin (one of "Built-in", "Installed", or "Unknown") as a labelled line, followed by the descriptor's version, publisher, and homepage as labelled lines when declared. When an update is known to be available, the card SHALL render the update chip in the state chip's position (immediately after the name) with the installed and latest versions, and SHALL NOT render the state chip while the update chip is shown. When availability is unknown or the agent is up to date, the card SHALL show the state chip in that position and SHALL NOT show an update indicator, and a probe that reports the agent not installed on the host SHALL clear any update indicator the card was showing and SHALL show the state chip again. The update indicator and Update action enablement both derive from the card's `UpdateAvailability` result; when availability reports `UpdateAvailable`, the indicator is shown and Update becomes available (subject to declaration and running-agent gating).

#### Scenario: Identity shown

- **WHEN** an agent card is shown
- **THEN** it shows the agent's display name and logo (falling back to the default logo)

#### Scenario: Installation status shown

- **WHEN** an agent card is shown
- **THEN** the state chip next to the agent's name indicates whether the agent is installed on the host
- **AND** when the agent is not installed, the chip shows a danger-styled "Not installed" state

#### Scenario: Installed version shown

- **WHEN** an installed agent's card is shown and the installed version is available
- **THEN** the state chip next to the agent's name shows the installed version
- **AND** when the version is not available, the chip shows an "Installed" state instead

#### Scenario: Detecting state shown

- **WHEN** a detection or test probe is running for a card
- **THEN** the state chip shows "Detecting…" for the duration of the probe
- **AND** the chip keeps the default chip styling rather than the danger styling of the "Not installed" state

#### Scenario: Detecting never persists after a failed probe

- **WHEN** a probe for a card throws or is abandoned
- **THEN** the card does not stay in the "Detecting…" state and returns to its previous installation state

#### Scenario: Command set shown

- **WHEN** an agent card is shown
- **THEN** it lists which named commands the agent declares (launch, resume, version, update)

#### Scenario: Update available indicator shown

- **WHEN** an installed agent's card is shown and availability is `UpdateAvailable` with installed version `2.0.32` and latest version `2.0.33`
- **THEN** the card shows `2.0.32 → 2.0.33` in place of the state chip, immediately after the name
- **AND** the state chip is not shown

#### Scenario: No indicator when availability is unknown

- **WHEN** availability is `Unknown`
- **THEN** the card shows no update indicator and no placeholder text for it
- **AND** the state chip is shown in its usual position

#### Scenario: No indicator when up to date

- **WHEN** availability is `UpToDate`
- **THEN** the card shows no update indicator
- **AND** the state chip is shown in its usual position

#### Scenario: No indicator for a missing installation

- **WHEN** the agent is not installed on the host
- **THEN** the card shows no update indicator
- **AND** the state chip is shown with its danger-styled "Not installed" state

#### Scenario: Update indicator cleared when a probe reports not installed

- **WHEN** a card previously showed an update indicator and a later probe reports the agent not installed on the host
- **THEN** the card shows no update indicator
- **AND** the state chip is shown again, with its danger-styled "Not installed" state

#### Scenario: Update indicator is advisory

- **WHEN** availability reports `UpdateAvailable`
- **THEN** the update indicator is shown
- **AND** the Update action becomes available (subject to the agent declaring an update command and no matching agent process running)
- **WHEN** availability is `Unknown` or `UpToDate`
- **THEN** no update indicator is shown
- **AND** the Update action is unavailable (regardless of declaration or running-agent state)

#### Scenario: State chip returns when no update remains

- **WHEN** a card is showing the update chip in place of the state chip and a later check settles availability to `UpToDate` or `Unknown`
- **THEN** the update chip is no longer shown
- **AND** the state chip is shown again, in its usual position immediately after the name, reflecting the card's current installation state

#### Scenario: Agent identity shown in name tooltip

- **WHEN** an agent card's name is hovered
- **THEN** the tooltip shows the recorded origin as "Origin: Built-in", "Origin: Installed", or "Origin: Unknown"
- **AND** when the descriptor declares a version, publisher, or homepage, each is shown as an additional labelled line ("Version: …", "Publisher: …", "Homepage: …")
- **AND** no separate origin badge is rendered on the card

---

### Requirement: Agent card actions

The card SHALL present actions appropriate to the agent's declared capabilities and current state, in the order "Detect", "Check updates", "Update", "Edit config". A card SHALL render with "Update" available exactly when the agent's latest update-availability result reports `UpdateAvailable` status AND the agent declares an `update` command AND no matching agent process is running. When availability is `Unknown` (including before the first check completes, after a failed or timed-out check, or for an agent that declares no update source) or `UpToDate`, "Update" is unavailable. When the agent is not installed on the host, "Update" is unavailable. The running-agent blocked state SHALL be populated asynchronously after the cards render, through a population pass that is cancelled when the list is reloaded or the surface that started it goes away, so that a result from an obsolete pass is not applied. Until then, a card MAY transiently show "Update" as available while a process matching its agent is running; choosing "Update" in that state SHALL NOT start the update — the execution-time running-agent check blocks it and the card reports the block. When the blocked-state pass completes and a running agent is found, the card shows the block message ("Agent is running — stop it to update"). When no agent declares an `update` command, no running-agent enumeration SHALL run. An enumeration or population failure SHALL be logged and SHALL leave the cards showing "Update" per the availability and declaration rules. A process matches an agent per the `agent-process-matching` capability, including a process hosted inside a shared runtime whose command line identifies the agent. The card's "Detect" action re-detects the agent's host installation and version locally; it SHALL NOT trigger an update-availability check, and it SHALL be unavailable while a header-wide action is running and while the card's own "Detect" or "Check updates" action is running. The card's "Check updates" action SHALL force a fresh update-availability check for that one agent by invalidating only that agent's cached availability result before checking, bypassing the per-agent time-to-live; it SHALL NOT re-run host-installation detection and SHALL NOT issue a version command at the call site. It SHALL be unavailable for an agent whose installation state is not installed, while the card's own "Detect" or "Check updates" action is running, and while a header-wide action is running. Per-card actions SHALL be independent across cards: starting "Detect" or "Check updates" on one card SHALL NOT disable those actions on other cards. "Edit config" SHALL be available when the agent's `plugin.json` path is known; otherwise it is unavailable.

#### Scenario: Detect available on every card

- **WHEN** an agent card is shown, no header-wide action is running, and the card's own "Detect" and "Check updates" actions are not running
- **THEN** "Detect" is available on the card

#### Scenario: Per-card Detect is local only

- **WHEN** the per-card "Detect" action runs
- **THEN** the card is re-probed for host installation and version
- **AND** no update-availability check runs for it and its declared update source is not requested
- **AND** its previously shown update indicator is kept unless the probe reports the agent not installed

#### Scenario: Update requires UpdateAvailable status

- **WHEN** an agent's latest availability result is `UpdateAvailable` and the agent declares an `update` command and no matching agent process is running
- **THEN** "Update" is available on its card

#### Scenario: Unknown availability disables Update

- **WHEN** an agent's latest availability result is `Unknown` (including before the first check, after a failed check, or for an agent with no declared update source)
- **THEN** "Update" is unavailable on its card

#### Scenario: UpToDate availability disables Update

- **WHEN** an agent's latest availability result is `UpToDate`
- **THEN** "Update" is unavailable on its card

#### Scenario: Not installed disables Update

- **WHEN** an agent is not installed on the host
- **THEN** "Update" is unavailable on its card

#### Scenario: Update requires the update command

- **WHEN** an agent does not declare an `update` command
- **THEN** "Update" is unavailable on its card, regardless of availability status

#### Scenario: Update blocked by a running agent

- **WHEN** the blocked-state pass completes and finds a process matching the agent
- **THEN** "Update" is unavailable on its card
- **AND** the card shows the block message "Agent is running — stop it to update"

#### Scenario: Other running agents do not block an update

- **WHEN** processes of other agents are running and the agent being updated has no running processes
- **THEN** "Update" remains available under the availability and declaration rules

#### Scenario: Edit Config requires a plugin file

- **WHEN** the agent's `plugin.json` path is known
- **THEN** "Edit Config" is available; otherwise it is unavailable

#### Scenario: Blocked state populates after the cards render

- **WHEN** the CLI Agents page loads and at least one agent declares an `update` command
- **THEN** the cards render immediately with "Update" available exactly for agents whose availability is `UpdateAvailable`, that declare an `update` command, and whose running-agent state is not yet known to block it
- **AND** when the running-agent enumeration completes, a card whose agent has a running process shows "Update" as unavailable and its block explanation
- **AND** the other cards' "Update" availability reflects their availability and declaration under the gated rule

#### Scenario: Transient declarative availability while the blocked state is pending

- **WHEN** a card is shown while its running-agent enumeration is still pending and a process matching that agent is running and the card's availability is `UpdateAvailable`
- **THEN** the card may show "Update" as available until the enumeration completes
- **AND** choosing "Update" in that state does not start the update; the execution-time running-agent check blocks it and the card reports the block
- **AND** once the enumeration completes, the card shows the block explanation

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
- **AND** the cards keep "Update" available exactly for agents whose availability is `UpdateAvailable` and that declare an `update` command (the block message is not shown)

#### Scenario: Update blocked by a runtime-hosted agent

- **WHEN** the agent being updated runs inside a shared runtime host (for example a `node.exe` process started by the agent's npm shim whose command line names the agent's declared npm package) and no executable-name match exists
- **THEN** once the blocked-state pass settles, the card shows the block message if the agent's availability is `UpdateAvailable`

#### Scenario: Unrelated runtime-hosted processes do not block an update

- **WHEN** runtime-host processes are running whose command lines do not name any declared npm package of the agent being updated (for example an unrelated node script)
- **THEN** "Update" remains available under the availability and declaration rules for that agent

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
- **THEN** the card's message line reports `Update available` when an update is available, `Up to date` when the agent is up to date, and `Update status unknown` when availability could not be determined
- **AND** the header-wide update-check summary line is left unchanged
- **AND** the Update action's availability reflects the fresh availability result

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

#### Scenario: Update clears availability when started

- **WHEN** an update starts successfully
- **THEN** the card's update-availability result is cleared
- **AND** "Update" becomes unavailable until the next availability check resolves a new result
