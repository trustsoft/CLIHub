## Context

See proposal.md - Why. `agent-availability-display` established dimming via `AgentItem.IsAvailable`/`RowOpacity`. `AppPreferences` (in `AppConfig`) already holds persisted user settings and is read/written through `IConfigService`. `MainWindow` rebuilds the agent list in `RefreshAgents` and is invoked on project change.

## Goals / Non-Goals

**Goals:**
- A single toggle switching the agent list between "dim" and "hide"
- Persist the choice
- Keep the default behavior unchanged (dim)
- Predictable interaction with "no project selected"

**Non-Goals:**
- Per-project filter memory (one global preference)
- Multi-criteria filtering (system install, version, search)
- A full preferences window (separate change)

## Decisions

### Decision 1: Extend `agent-availability-display` rather than a new capability

**Chosen:** Add a requirement to the existing capability's spec via the change delta.

**Rationale:** Filtering is the same concern (presenting availability) with a stricter mode; it reuses `IsAvailable`.

**Alternatives considered:** A new `agent-filtering` capability — rejected as duplicative.

### Decision 2: Persist as `AppPreferences.ShowOnlyProjectAgents`

**Chosen:** `bool ShowOnlyProjectAgents { get; set; }` (default `false`).

**Rationale:** Reuses the existing config pipeline; a missing field deserializes to `false`, so existing configs keep dim behavior. Round-trips with camelCase like the rest of `AppPreferences`.

**Alternatives considered:**
- Session-only state: Rejected — the choice should survive restarts
- Separate preferences file: Rejected — unnecessary

### Decision 3: Filter precedence over dimming

**Chosen:** In `RefreshAgents`, if the filter is on and a project is selected, exclude unavailable agents entirely; otherwise include all and dim the unavailable ones.

**Rationale:** A hidden agent cannot also be dimmed; precedence must be explicit (spec: "Filter precedes dimming").

**Alternatives considered:** Keep a separate "dim" checkbox — rejected; one mode toggle is simpler.

### Decision 4: No-project fallback shows all

**Chosen:** When no project is selected, the filter has nothing to evaluate against, so all agents are shown at normal emphasis.

**Rationale:** Availability is project-relative; hiding everything or showing nothing would be confusing.

### Decision 5: UI toggle with a re-entrancy guard

**Chosen:** A `CheckBox` in the agents panel. On construction, set its value from config while suppressing the change handler so it does not re-save. On user toggle, save the preference and refresh.

**Rationale:** Avoids a needless write at startup and keeps the handler simple.

**Alternatives considered:** Data-binding the checkbox to a view model — deferred until there is a proper view model layer.

## Risks / Trade-offs

**[Risk] User hides all agents and is confused** → Mitigation: all agents are dimmed (reachable) when the filter is off; the toggle is visible with a clear label.

**[Risk] Config field name drift** → Mitigation: camelCase `showOnlyProjectAgents`, consistent with existing preferences.

**[Trade-off] Global (not per-project) filter** → Benefit: simple. Cost: switching projects does not remember distinct filter states; acceptable for now.

## Migration Plan

1. Add `ShowOnlyProjectAgents` to `AppPreferences`
2. Add the toggle to `MainWindow.xaml`; read/save via `IConfigService`
3. Apply the filter in `RefreshAgents`
4. Tests: filtering selection logic (extracted/pure if practical); config default

Rollback: revert code; the extra config field is ignored by older versions.

## Open Questions

- **Replace dimming entirely with the filter later?** The dim mode remains the default; revisit after feedback.
- **Move both into a preferences window?** Planned with `preferences-ui`.
