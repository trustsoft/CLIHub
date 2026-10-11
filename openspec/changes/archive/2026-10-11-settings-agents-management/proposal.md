## Why

Agent commands (launch, resume, initialize, update, version) live only in the launch window's transient Actions menu, and the CLI Agents settings page shows just the probe TTL/timeout fields. There is no place to inspect what an agent declares, to test it, to update it, or to open its configuration, and no way to choose which agent the launch window should default to. This is the `settings-agents-management` slice of Phase 2; it turns the CLI Agents page into an agent management surface.

## What Changes

- The **CLI Agents** page becomes a list of **agent cards**, one per loaded plugin, each showing the agent **name and logo**, its **host installation status**, its **installed version**, the **plugin folder path**, the declared **command set** (launch/resume/version/update), and the declared **detection markers** (host system paths and project indicators).
- Each card offers three actions:
  - **Test** — re-evaluates the agent: clears cached detection and version, re-checks host installation, and re-probes the version, reporting the outcome on the card.
  - **Update** — runs the agent's `update` command interactively, in the **user profile directory** (agent updates are project-independent). Blocked while a matching agent process is running, reusing the existing rule.
  - **Edit Config** — opens the agent's `plugin.json` with the operating-system default handler.
- The page shows two **metrics**: the number of supported agents (loaded plugins) and the number installed on the host.
- A new **Default agent** preference selects which agent the launch window preselects when it opens.
- The card shows only the installed version; the latest available version, the upgrade marker, the "all up to date" metric, and a default-model field are **out of scope** for this change.
- No agent can be registered or removed from this page; the plugin catalog remains the source of agents. This change is **non-breaking**: the only new persisted field is `defaultAgentId`, additive with a null default.

## Capabilities

### New Capabilities
<!-- None: agent management extends existing agent and preferences capabilities. -->

### Modified Capabilities
- `preferences-ui`: the CLI Agents page presents agent cards with per-agent actions, the supported/installed metrics, and a default-agent selector.
- `agent-ui`: an agent's presentation includes its card fields and the per-agent action availability.
- `agent-detection`: a manual probe re-evaluates and reports an agent's host installation state on demand rather than only through the cached list.
- `agent-version`: a manual probe bypasses the cache and reports the freshly probed version, including the unknown case.
- `agent-commands`: the agent `update` command can run project-independently in the user profile directory.
- `configuration-snapshot`: the new `defaultAgentId` preference defaults safely and round-trips.

## Impact

- Core: detection/version probing already exist; add a small agent-management service (or reuse `IAgentDetectionService`/`IAgentVersionService`/`IAgentCommandService` + `AgentProcessMonitor`) for probe/update/edit-config; `Plugin` gains no new fields.
- App: `SettingsViewModel` + `SettingsAgentsPage` gain agent cards, actions, metrics, and the default-agent selector; `AgentPaneController`/launch window preselect the default agent; `IExternalLauncher` opens `plugin.json`.
- Config: additive `preferences.defaultAgentId` (null default); no `schemaVersion` change.
- Tests: agent-management service tests, Settings view-model tests, default-agent selection tests, persistence round-trip tests.

## Non-goals

- Latest-available-version lookup, upgrade markers, the "all up to date" metric, and a default-model field.
- Registering or removing agents, editing plugins beyond opening `plugin.json`, and in-app plugin validation.
- Passing the current project context to the agent automatically.
- Compact density and other later roadmap items.
