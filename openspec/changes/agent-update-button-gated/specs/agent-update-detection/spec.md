# agent-update-detection Specification Delta

## REMOVED Requirements

### Requirement: Update availability is advisory

This requirement is removed. The availability result now gates the Settings → CLI Agents card Update action. See the ADDED requirement "Update availability gates the Settings card Update action" below.

The original requirement stated:

> The availability result SHALL NOT gate, disable, enable, or otherwise alter the Update action. The Update action SHALL remain governed only by its existing rules: the agent declares an update command and no matching agent process is running.

## ADDED Requirements

### Requirement: Update availability gates the Settings card Update action

The availability result SHALL gate the Update action on Settings → CLI Agents cards: the action is enabled exactly when availability reports `UpdateAvailable` status AND the agent declares an `update` command AND no matching agent process is running. When availability is `Unknown` (including before the first check, after a failed or timed-out check, or for an agent with no declared update source) or `UpToDate`, the Update action is disabled. When the agent is not installed on the host, the Update action is disabled. The launch-window Update action (coordinated by `WindowActionCoordinator`) remains unchanged and is not gated by this requirement; its behavior is an explicit non-goal of this change.

#### Scenario: UpdateAvailable enables the action

- **WHEN** availability reports `UpdateAvailable` for an agent that declares an `update` command and no matching agent process is running
- **THEN** the Settings card Update action is enabled

#### Scenario: Unknown disables the action

- **WHEN** availability reports `Unknown` because the check failed, timed out, or the agent declares no update source
- **THEN** the Settings card Update action is disabled

#### Scenario: UpToDate disables the action

- **WHEN** availability reports `UpToDate`
- **THEN** the Settings card Update action is disabled

#### Scenario: Not installed disables the action

- **WHEN** the agent is not installed on the host
- **THEN** the Settings card Update action is disabled and no availability check runs

#### Scenario: No update command disables the action

- **WHEN** an agent does not declare an `update` command
- **THEN** the Settings card Update action is disabled, regardless of availability status

#### Scenario: Launch-window action unchanged

- **WHEN** the launch-window Update action is evaluated
- **THEN** its enablement rules are unchanged and remain independent of this Settings card gating requirement
