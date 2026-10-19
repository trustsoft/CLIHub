# agent-availability-display (partial): in-place row fidelity and refresh triggers

## MODIFIED Requirements

### Requirement: Agent row properties update in place

The launch-window agent row SHALL update availability, status, logo, and version-related properties without replacing the row when the plugin ID remains present. When a refresh supplies a different plugin descriptor for the same row, the row SHALL also announce its derived command-capability flags through property notifications.

#### Scenario: Availability changes
- **WHEN** project selection or filtering changes an agent's availability
- **THEN** the existing row reflects the new state through property notifications

#### Scenario: Capability flags follow a plugin change
- **WHEN** a refresh supplies a plugin descriptor whose declared commands differ from the row's previous descriptor
- **THEN** the row announces its derived command-capability flags through property notifications
- **AND** the announced values match the new descriptor's declared commands

## ADDED Requirements

### Requirement: Agent pane refreshes when the launch window is shown or activated

The launch window SHALL refresh the agent pane when the window is shown or activated, symmetrically to project display-model refresh, so agent availability reflects current host and project state without a manual refresh.

#### Scenario: Window shown

- **WHEN** the launch window is shown
- **THEN** the agent pane is refreshed after the project pane and before any default-agent selection is applied

#### Scenario: Window activated

- **WHEN** the launch window is activated
- **THEN** the agent pane is refreshed

#### Scenario: Version population stays cache-served

- **WHEN** the show or activation refresh runs
- **THEN** version resolution is served by the version cache within its validity window rather than bypassing it
