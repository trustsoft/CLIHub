## REMOVED Requirements

### Requirement: REQ-AGENT-CAP-001: Agent Capability Detection

**Reason**: The `init` command is removed, so the `CanInit` capability it detected no longer exists.

**Migration**: No replacement is needed; agents no longer expose an initialize action. The remaining capabilities (launch, resume, update, version) are unchanged.

### Requirement: REQ-AGENT-CAP-004: Menu Command Enablement Based on Capabilities

**Reason**: Its scenarios were specific to the removed `init` command.

**Migration**: Replaced by "Menu command enablement based on capabilities" below, which keeps the capability-based enablement for the remaining commands and the no-selection rule.

## ADDED Requirements

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
