# agent-ui Specification

## Purpose
The agent UI exposes only the commands declared by each plugin descriptor, so unavailable actions are not
presented as usable operations for the selected agent.

## Requirements

### Requirement: REQ-AGENT-CAP-002: Update Command Support Detection

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

### Requirement: REQ-AGENT-CAP-003: Version Command Support Detection

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

### Requirement: REQ-AGENT-CAP-005: Resume Command Capability Check

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

### Requirement: REQ-AGENT-CAP-006: Update Command Capability Check

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

### Requirement: REQ-AGENT-CAP-007: Version Command Capability Check

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

### Requirement: REQ-AGENT-CAP-008: Dynamic Command State Updates

The system SHALL update command enabled/disabled states immediately when agent selection changes.

#### Scenario: Update Commands on Selection Change
```
GIVEN agent A is selected with Resume enabled and Init disabled
WHEN the user selects agent B that has Init but not Resume
THEN Resume SHALL become disabled
AND Init SHALL become enabled
AND the change SHALL occur within 50ms
```

#### Scenario: Maintain Correct States After Rapid Switching
```
GIVEN the user rapidly switches between multiple agents
WHEN the user settles on a final agent selection
THEN the menu commands SHALL reflect that agent's capabilities accurately
```

---

### Requirement: REQ-AGENT-CAP-009: Launch Command Always Available

The system SHALL always enable the Launch command when an agent is selected, regardless of plugin.json contents.

#### Scenario: Launch Available for Any Selected Agent
```
GIVEN any agent is selected
WHEN the user opens the Actions menu
THEN the Launch menu item SHALL be enabled
BECAUSE all agents support launching by convention
```

---

### Requirement: REQ-AGENT-CAP-010: Visual Distinction of Disabled Commands

The system SHALL visually distinguish disabled menu commands from enabled ones.

#### Scenario: Visual Feedback for Disabled Command
```
GIVEN a menu command is disabled due to agent capability
WHEN the user views the Actions menu
THEN the disabled command SHALL appear grayed out or otherwise visually distinct
AND the command SHALL not be clickable
```

---

### Requirement: REQ-AGENT-CAP-011: No Breaking Changes to Plugin Schema

The capability detection system SHALL work with existing plugin.json files without requiring schema changes.

#### Scenario: Work with Existing Plugin Files
```
GIVEN an existing agent with plugin.json in current format
WHEN the capability-aware UI is activated
THEN the agent SHALL work without modification to its plugin.json
AND capability detection SHALL correctly identify supported commands
```

---

### Requirement: REQ-AGENT-CAP-012: Graceful Handling of Malformed Plugins

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

### Requirement: Agent card content

An agent's management card SHALL present its declared identity and state: display name and logo, host installation status, installed version, plugin folder path, declared command set, and declared detection markers.

#### Scenario: Identity shown
- **WHEN** an agent card is shown
- **THEN** it shows the agent's display name and logo (falling back to the default logo)

#### Scenario: Installation status shown
- **WHEN** an agent card is shown
- **THEN** it indicates whether the agent is installed on the host

#### Scenario: Installed version shown
- **WHEN** an agent card is shown
- **THEN** it shows the installed version, or an "unknown" placeholder when the version is not available

#### Scenario: Plugin path shown
- **WHEN** an agent card is shown
- **THEN** it shows the folder the agent's `plugin.json` was loaded from

#### Scenario: Command set shown
- **WHEN** an agent card is shown
- **THEN** it lists which named commands the agent declares (launch, resume, version, update)

#### Scenario: Detection markers shown
- **WHEN** an agent card is shown
- **THEN** it lists the agent's declared host system paths and project indicators

#### Scenario: No detection markers
- **WHEN** an agent declares no detection markers
- **THEN** the card indicates that none are declared without showing an error

### Requirement: Agent card action availability

The card SHALL present actions appropriate to the agent's declared capabilities and current state.

#### Scenario: Test always available
- **WHEN** an agent card is shown
- **THEN** "Test" is available

#### Scenario: Update requires the update command
- **WHEN** an agent does not declare an `update` command
- **THEN** "Update" is unavailable on its card

#### Scenario: Update blocked by a running agent
- **WHEN** a matching agent process is running
- **THEN** "Update" is unavailable and the card explains that a running agent blocks it

#### Scenario: Edit Config requires a plugin file
- **WHEN** the agent's `plugin.json` path is known
- **THEN** "Edit Config" is available; otherwise it is unavailable
