## Context

See `proposal.md` — Why. The `init` command kind is referenced by: `AgentCommandKind.Init`, `AgentCommands.Init`, `AgentItem.CanInit`, `WindowActionCoordinator.InitCommand`, `LaunchWindowViewModel.InitCommand`, `LaunchWindowActionBuilder`/`MenuActionCoordinator` (the `initialize` parameter and the "Initialize" menu entry), the legacy `MainWindow` Init button/click, `IconGlyphs.Initialize`, the seeded `github-copilot` descriptor, and the `agent-commands`/`agent-ui`/`main-window-layout` specs.

## Goals / Non-Goals

**Goals:**

- Remove `init` end to end with no behavior change to the remaining commands.
- Keep descriptor loading tolerant of an existing `init` entry.

**Non-Goals:**

- Any storage migration; no persisted field references the command kind.
- Rebalancing the remaining command set.

## Decisions

### D1 — Remove the model member rather than leave it unused

Drop `AgentCommandKind.Init` and `AgentCommands.Init` (and its `Get` mapping). **Why:** an unused enum member and property would keep tempting the UI to reintroduce the action. **Alternative rejected:** leaving the model and hiding only the UI — the command set is the contract, so it should match.

### D2 — Descriptor compatibility by ignoring the field

Removing the `Init` property means a JSON `"init"` entry is deserialized as an unknown field and ignored; the plugin still loads. No validator change is required. **Why:** existing user plugin files must not break. **Alternative rejected:** rejecting descriptors with `init` — unnecessarily breaking.

### D3 — Remove the menu and legacy wiring, keep the pipeline generic

Delete the Initialize entry/parameter through `LaunchWindowActionBuilder`, `MenuActionCoordinator`, `WindowActionCoordinator`, `LaunchWindowViewModel`, and the legacy `MainWindow`. The remaining actions keep the same wiring. **Why:** a clean menu with no dead command path. **Alternative rejected:** leaving a disabled "Initialize" entry — confusing.

## Risks / Trade-offs

- **A user relied on `init`** → the command is not launched by CLIHub; they can still run it manually. Documented by the non-goal.
- **Missed reference** → the build (compile) and `AgentCommandServiceTests`/`AgentItemTests`/`LaunchWindowActionBuilderTests` fail fast on any leftover.

## Migration Plan

- No configuration or data migration. Descriptors keep loading; the `init` field is ignored.

## Open Questions

- None.
