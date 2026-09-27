## Context

See proposal.md - Why. `agent-detection` already computes `IsAvailableInProject(plugin, projectPath)`; `agent-version` made `AgentItem` implement `INotifyPropertyChanged`; `MainWindow.RefreshAgents` rebuilds the agent list and is called when the current project changes.

## Goals / Non-Goals

**Goals:**
- Clear visual distinction for agents not used in the current project
- No change to commands, selection, or the loaded agent set
- Correct behavior with and without a selected project

**Non-Goals:**
- Hiding/filtering unavailable agents (a later change)
- Disabling commands for dimmed agents
- Dimming based on system-install state (only project availability is used here)

## Decisions

### Decision 1: Project availability drives emphasis

**Chosen:** An agent is "available" when `IsAvailableInProject(plugin, currentProject.Path)` is true; otherwise dim it — but only when a project is selected.

**Rationale:** Matches "unavailable ones" in a project context and reuses the existing detection. With no project, availability is unknown, so nothing is dimmed.

**Alternatives considered:**
- Dim when not installed in system: Rejected — system instalness is a different axis; the mockup's emphasis is project-centric
- Dim when neither system nor project: Deferred — can be revisited

### Decision 2: Derive opacity on `AgentItem`

**Chosen:** Add `bool IsAvailable` and a read-only `double RowOpacity => IsAvailable ? 1.0 : 0.4` to `AgentItem`; the list is rebuilt on project change, so no extra notification is required.

**Rationale:** Simple, testable value; rebuilding on project change already happens. Opacity 0.4 is a conventional "disabled/de-emphasized" value that keeps text legible.

**Alternatives considered:**
- A `DataTrigger` toggling opacity/styles: Rejected — more XAML for the same result
- Gray foreground instead of opacity: Rejected — opacity dims the logo too, which reads better

### Decision 3: Presentation-only

**Chosen:** Dimmed agents remain selectable and their buttons stay enabled.

**Rationale:** Matches the user's "dim for now" intent; command-level validation already reports unsupported/failed commands.

**Alternatives considered:** Disable actions for dimmed agents — rejected for now.

## Risks / Trade-offs

**[Risk] 0.4 opacity too faint on some displays** → Mitigation: value is a single constant, easy to tune.

**[Risk] Users expect dimmed = disabled** → Trade-off: a tooltip or an explicit "not used in this project" suffix could clarify later; kept minimal now.

**[Trade-off] No filtering yet** → Benefit: all agents remain discoverable. Cost: a long list still scrolls; filtering is planned next.

## Migration Plan

1. Add `IsAvailable` + `RowOpacity` to `AgentItem`
2. Set `IsAvailable` in `MainWindow.RefreshAgents` from `IAgentDetectionService`
3. Bind the row template's `Opacity` to `RowOpacity`
4. Tests: unchanged service behavior; a small UI-logic assertion if extracted

Rollback: revert code; no persisted state changes.

## Open Questions

- **Switch from dimming to filtering/hiding?** Planned as a follow-up change.
- **Also dim when the agent is not installed in the system?** Deferred.
