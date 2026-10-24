# agent-ui delta

## MODIFIED Requirements

### Requirement: Agent card action availability

The card SHALL present actions appropriate to the agent's declared capabilities and current state. A card SHALL render with "Update" available exactly when the agent declares an `update` command; the running-agent blocked state SHALL be populated asynchronously after the cards render, through a population pass that is cancelled when the list is reloaded or the surface that started it goes away, so that a result from an obsolete pass is not applied. The state-based scenarios below apply to the settled state — once the population pass has completed for the current list. Until then, a card MAY transiently show "Update" as available while a process matching its agent is running; choosing "Update" in that state SHALL NOT start the update — the execution-time running-agent check blocks it and the card reports the block. When no agent declares an `update` command, no running-agent enumeration SHALL run. An enumeration or population failure SHALL be logged and SHALL leave the cards showing "Update" per declaration. A process matches an agent per the `agent-process-matching` capability, including a process hosted inside a shared runtime whose command line identifies the agent.

#### Scenario: Test always available

- **WHEN** an agent card is shown
- **THEN** "Test" is available

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
