## Context

See `proposal.md` — Why. Existing building blocks this design reuses:

- `CLIHub.Core.Plugins.IPluginCatalog` — the loaded agent descriptors (`Plugin` with `Commands`, `Detection`, `PluginDirectory`).
- `CLIHub.Core.Agents` — `IAgentDetectionService` (host/project availability, cache + `Invalidate`), `IAgentVersionService` (version probe, cache + `Invalidate`), `IAgentCommandService` (named command execution).
- `CLIHub.Core.Services.AgentProcessMonitor` — running-agent discovery used to block updates.
- `CLIHub.Core.Infrastructure.Processes.ProcessLauncher` — interactive/captured process execution.
- `CLIHub` — `SettingsViewModel`/`SettingsAgentsPage` (currently only probe fields), `AgentPaneController` (launch-window selection), `IExternalLauncher`, `IApplicationOperationLifetime`.

## Goals / Non-Goals

**Goals:**

- A Core agent-management service for probe and project-independent update, and an app-side card presentation with per-agent actions.
- A persisted default agent that the launch window preselects.
- Additive configuration only; nothing removed or renamed.

**Non-Goals:**

- Latest-available-version lookup, upgrade markers, "all up to date" metric, default-model field.
- Registering/removing agents, in-app plugin editing/validation, automatic project-context passing, compact density.

## Decisions

### D1 — Core `IAgentManagementService` for probe and update

Add `CLIHub.Core.Agents.IAgentManagementService` with:
- `Task<AgentProbeResult> ProbeAsync(Plugin plugin, CancellationToken)` — invalidates detection and version for the probe, re-checks host installation, re-probes the version, and returns `(bool IsInstalled, string? Version)`.
- `AgentUpdateResult UpdateAsync(Plugin plugin)` — rejects when the plugin declares no `update` command; blocks when `AgentProcessMonitor.GetRunningAgents()` is non-empty; otherwise runs the update command interactively with the user profile directory as the working directory.

**Why Core:** it composes existing Core services (detection, version, command, process monitor) with no UI dependency, keeping it testable. **Alternative rejected:** an app-side service — would duplicate command/process logic in the UI layer against the architecture boundary.

### D2 — Reuse existing command execution with an explicit working directory

`AgentCommandService.ExecuteAsync(plugin, kind, workingDirectory, ct)` already accepts a working directory and only requires it to exist. The management service passes the user profile directory for updates, so no new execution path is needed. This is the behavior added to `agent-commands` (project-independent update).

### D3 — App-side `AgentCardItem` view model

A dedicated `AgentCardItem` (not the launch-window `AgentItem`) carries the card fields (name, logo, installed, version, plugin path, command labels, host/project markers, per-action availability, last probe message) and raises change notifications. **Why:** the launch-window row is intentionally minimal; the card has a different, larger surface. **Alternative rejected:** extending `AgentItem` — would bloat the launch pane and couple two surfaces.

### D4 — Actions and their guards

- **Test** calls `ProbeAsync` and writes the result onto the card.
- **Update** is available only when the plugin declares `update` and `AgentProcessMonitor` reports no running agent; the action re-checks at execution time (mirroring `WindowActionCoordinator`) and reports blocked/unsupported outcomes.
- **Edit Config** opens `Path.Combine(plugin.PluginDirectory, "plugin.json")` through `IExternalLauncher`; unavailable when the path is unknown.

**Why:** consistent with the existing launch-window gating and safety rule (`agent-ui`/process-monitor contract). **Alternative rejected:** a separate update pipeline — redundant.

### D5 — Default agent preference and launch-window preselect

Add `AppPreferences.DefaultAgentId` (`string?`, default null). The launch window preselects the matching agent when it opens; a missing/unknown id means no preselect. The Settings selector persists the id on Save.

**Why:** matches the agreed scope ("preselect in launch window") and the additive-config rule. **Alternative rejected:** storing a default project too — out of scope.

### D6 — Metrics computed in the view model

The supported count is the plugin count; the installed count is the number of plugins whose host installation check passes. Both are recomputed when the page loads. **Why:** cheap and needs no new service. **Alternative rejected:** caching metrics in Core — unnecessary state.

### D7 — Threading and long-running work

Probe and update run through `IApplicationOperationLifetime` (tracked, cancellable) and marshal results to the UI thread; the card exposes the outcome text. **Why:** matches every other Settings action and avoids blocking the UI.

## Risks / Trade-offs

- **Probe invalidates the whole detection/version cache** → acceptable; the next refresh re-populates. A per-plugin invalidation could be added later if it becomes noisy.
- **Opening `plugin.json` for a seeded built-in** → the file exists on disk after seeding; if the path is missing, Edit Config is disabled.
- **Update in the user profile may not be where an agent expects to run** → updates are install-scoped, not project-scoped; the profile directory is the documented working directory for this action.
- **Default agent id drift after plugin changes** → handled as "no preselect", never an error.

## Migration Plan

- Additive `defaultAgentId`; ordinary load yields null (no preselect). No schema migration or bump.
- Rollback: older builds ignore the unknown JSON field.

## Open Questions

- None blocking; latest-version and default-model are explicitly deferred and may become their own change if the descriptor gains a source.
