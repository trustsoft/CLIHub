# agent-ui Specification

## Purpose
The agent UI exposes only the commands declared by each plugin descriptor, so unavailable actions are not
presented as usable operations for the selected agent.

## Requirements

### Requirement: Update command support detection

The system SHALL detect whether an agent supports the update command.

#### Scenario: Detect Update Command Support
```
GIVEN an agent with plugin.json containing an update command definition
WHEN the system checks CanUpdate property
THEN it SHALL return true
```

#### Scenario: Detect Missing Update Command
```
GIVEN an agent with plugin.json lacking an update command definition
WHEN the system checks CanUpdate property
THEN it SHALL return false
```

---

### Requirement: Version command support detection

The system SHALL detect whether an agent supports the version command.

#### Scenario: Detect Version Command Support
```
GIVEN an agent with plugin.json containing a version command definition
WHEN the system checks CanVersion property
THEN it SHALL return true
```

#### Scenario: Detect Missing Version Command
```
GIVEN an agent with plugin.json lacking a version command definition
WHEN the system checks CanVersion property
THEN it SHALL return false
```

---

### Requirement: Resume command enablement for session-capable agents

The system SHALL enable the Resume command only when the selected agent supports session resumption.

#### Scenario: Enable Resume for Session-Capable Agent
```
GIVEN an agent that supports resume command is selected
WHEN the user opens the Actions menu
THEN the Resume Session menu item SHALL be enabled
```

#### Scenario: Disable Resume for Session-Incapable Agent
```
GIVEN an agent that does not support resume command is selected
WHEN the user opens the Actions menu
THEN the Resume Session menu item SHALL be disabled
```

---

### Requirement: Update command enablement for updatable agents

The system SHALL enable the Update command only when the selected agent supports updates.

#### Scenario: Enable Update for Updatable Agent
```
GIVEN an agent that supports update command is selected
WHEN the user opens the Actions menu
THEN the Update menu item SHALL be enabled
```

#### Scenario: Disable Update for Non-Updatable Agent
```
GIVEN an agent that does not support update command is selected
WHEN the user opens the Actions menu
THEN the Update menu item SHALL be disabled
```

---

### Requirement: Version command enablement for version-capable agents

The system SHALL enable the Show Version command only when the selected agent supports version display.

#### Scenario: Enable Version for Version-Capable Agent
```
GIVEN an agent that supports version command is selected
WHEN the user opens the Actions menu
THEN the Show Version menu item SHALL be enabled
```

#### Scenario: Disable Version for Version-Incapable Agent
```
GIVEN an agent that does not support version command is selected
WHEN the user opens the Actions menu
THEN the Show Version menu item SHALL be disabled
```

---

### Requirement: Dynamic command state updates

The system SHALL update command enabled/disabled states immediately when agent selection changes.

#### Scenario: Update Commands on Selection Change
```
GIVEN agent A is selected with Resume and Update enabled and Version disabled
WHEN the user selects agent B that declares Version but neither Resume nor Update
THEN Resume SHALL become disabled
AND Update SHALL become disabled
AND Version SHALL become enabled
AND the change SHALL occur within 50ms
```

#### Scenario: Maintain Correct States After Rapid Switching
```
GIVEN the user rapidly switches between multiple agents
WHEN the user settles on a final agent selection
THEN the menu commands SHALL reflect that agent's capabilities accurately
```

---

### Requirement: Unconditional launch command availability

The system SHALL always enable the Launch command when an agent is selected, regardless of plugin.json contents.

#### Scenario: Launch Available for Any Selected Agent
```
GIVEN any agent is selected
WHEN the user opens the Actions menu
THEN the Launch menu item SHALL be enabled
BECAUSE all agents support launching by convention
```

---

### Requirement: Visual distinction of disabled commands

The system SHALL visually distinguish disabled menu commands from enabled ones.

#### Scenario: Visual Feedback for Disabled Command
```
GIVEN a menu command is disabled due to agent capability
WHEN the user views the Actions menu
THEN the disabled command SHALL appear grayed out or otherwise visually distinct
AND the command SHALL not be clickable
```

---

### Requirement: Plugin schema compatibility

The capability detection system SHALL work with existing plugin.json files without requiring schema changes.

#### Scenario: Work with Existing Plugin Files
```
GIVEN an existing agent with plugin.json in current format
WHEN the capability-aware UI is activated
THEN the agent SHALL work without modification to its plugin.json
AND capability detection SHALL correctly identify supported commands
```

---

### Requirement: Graceful handling of malformed plugins

The system SHALL handle malformed or incomplete plugin.json files without crashing.

#### Scenario: Handle Missing Commands Property
```
GIVEN an agent with plugin.json lacking a Commands property
WHEN the system checks any capability property
THEN it SHALL return false for all capabilities
AND no exception SHALL be thrown
```

#### Scenario: Handle Null Plugin Reference
```
GIVEN an AgentItem with null Plugin reference
WHEN the system checks any capability property
THEN it SHALL return false
AND no exception SHALL be thrown
```

### Requirement: Menu command enablement based on capabilities

The system SHALL enable menu commands in the Actions menu only when the selected agent supports those commands.

#### Scenario: Enable Launch Command for Capable Agent
```
GIVEN an agent that supports the launch command is selected
WHEN the user opens the Actions menu
THEN the Launch menu item SHALL be enabled
```

#### Scenario: Disable Resume Command for Incapable Agent
```
GIVEN an agent that does not support the resume command is selected
WHEN the user opens the Actions menu
THEN the Resume Session menu item SHALL be disabled
```

#### Scenario: Disable All Commands When No Agent Selected
```
GIVEN no agent is selected
WHEN the user opens the Actions menu
THEN all agent-specific commands SHALL be disabled
```

### Requirement: Agent card displayed content

An agent's management card SHALL present its declared identity and current state: display name, logo, a state chip next to the name that shows the installed version when the agent is installed with a resolved version, an "Installed" state when it is installed without one, a "Not installed" state when it is missing, or a "Detecting…" state while the card's installation state is being probed, and the declared command set listed after a "Supported commands" label. The state chip text SHALL be derived from the card's installation state, never from version placeholder text; the "Not installed" state SHALL keep its danger styling, and the "Detecting…" state SHALL use the chip's default styling. The card SHALL NOT present the plugin folder path or the declared detection markers. When an update is known to be available, the card SHALL also show an advisory update indicator next to the state chip with the installed and latest versions. When availability is unknown or the agent is up to date, the card SHALL NOT show an update indicator, and a probe that reports the agent not installed on the host SHALL clear any update indicator the card was showing. The update indicator SHALL NOT alter the Update action's availability.

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
- **THEN** the card shows `2.0.32 → 2.0.33` next to the state chip

#### Scenario: No indicator when availability is unknown

- **WHEN** availability is `Unknown`
- **THEN** the card shows no update indicator and no placeholder text for it

#### Scenario: No indicator when up to date

- **WHEN** availability is `UpToDate`
- **THEN** the card shows no update indicator

#### Scenario: No indicator for a missing installation

- **WHEN** the agent is not installed on the host
- **THEN** the card shows no update indicator

#### Scenario: Update indicator cleared when a probe reports not installed

- **WHEN** a card previously showed an update indicator and a later probe reports the agent not installed on the host
- **THEN** the card shows no update indicator

#### Scenario: Update indicator is advisory

- **WHEN** the update indicator is shown
- **THEN** the Update action remains available under its existing rules
- **AND** the indicator does not enable, disable, or block the Update action

### Requirement: Default agent selector reflects installation state

The default-agent selector SHALL present each agent option with display text derived from the agent's installation state, and SHALL disable only options for agents not installed on the host. Every other option, including one whose installation state is being detected, SHALL remain selectable, and no option SHALL carry version placeholder text.

#### Scenario: Installed agent with a resolved version

- **WHEN** the selector lists an installed agent whose version resolved to `2.0.32`
- **THEN** the option reads `OpenClaude (2.0.32)` and is selectable

#### Scenario: Installed agent without a resolved version

- **WHEN** the selector lists an installed agent whose version did not resolve
- **THEN** the option reads the agent's name alone, with no parenthesized suffix

#### Scenario: Not-installed agent marked and disabled

- **WHEN** the selector lists an agent that is not installed on the host
- **THEN** the option reads `{Name} (Not installed)` with a capitalized suffix, deliberately different from the default-runtime selector's lowercase `(not installed)`, whose own style stays unchanged
- **AND** the option is disabled and cannot be selected

#### Scenario: Detecting option stays selectable

- **WHEN** the selector lists an agent whose installation state is being re-detected
- **THEN** the option reads the agent's name alone and remains selectable

#### Scenario: Selector text carries no version placeholders

- **WHEN** an agent's version is unresolved or unavailable
- **THEN** the selector option renders no version placeholder text such as `unknown` for it

### Requirement: Agent card actions

The card SHALL present actions appropriate to the agent's declared capabilities and current state. A card SHALL render with "Update" available exactly when the agent declares an `update` command; the running-agent blocked state SHALL be populated asynchronously after the cards render, through a population pass that is cancelled when the list is reloaded or the surface that started it goes away, so that a result from an obsolete pass is not applied. Until then, a card MAY transiently show "Update" as available while a process matching its agent is running; choosing "Update" in that state SHALL NOT start the update — the execution-time running-agent check blocks it and the card reports the block. When no agent declares an `update` command, no running-agent enumeration SHALL run. An enumeration or population failure SHALL be logged and SHALL leave the cards showing "Update" per declaration. A process matches an agent per the `agent-process-matching` capability, including a process hosted inside a shared runtime whose command line identifies the agent. The card's "Detect" action re-detects the agent's host installation and version locally; it SHALL NOT trigger an update-availability check, and it SHALL be unavailable while a header-wide action is running.

#### Scenario: Detect available on every card

- **WHEN** an agent card is shown and no header-wide action is running
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
