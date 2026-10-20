# Agent update detection: tell the user when a newer agent version exists

## Why

The Update action currently runs the agent's own update command blindly. The user cannot tell
whether a newer version exists, so every invocation is a guess that may mutate an installation for
no reason. The installed version is already probed and cached (`agent-version`), but nothing
compares it with the version published by the agent's distribution channel.

The channels are known and machine-readable. Verified on 2026-10-20: five built-in agents publish
to npm (`@opencode/cli` 2.0.32, `cline` 3.0.73, `@earendil-works/pi-coding-agent` 1.3.0,
`@twigpine/openclaude` 0.33.0, `@qwen-code/qwen-code` 0.25.3) and `github/copilot-cli` publishes
GitHub releases (`releases/latest` = `v1.0.100`).

## What Changes

- Optional `updateCheck` block in the plugin descriptor declaring the agent's distribution source:
  `{ "source": "npm", "package": "…" }` or `{ "source": "githubReleases", "repo": "owner/repo" }`.
  Unknown source kinds are ignored with a warning, matching the existing ignored-unknown-command
  pattern. Descriptors without the block keep today's behavior.
- A nullable, immutable `PluginUpdateCheck` value on `Plugin`, cloned by `Plugin.Copy()` so the
  catalog's detached-read contract keeps holding.
- New Core ports: `IAgentReleaseClient` (latest published version from the declared source) and
  `IAgentUpdateAvailabilityService` (installed-versus-latest comparison with a per-agent TTL
  cache). Both are registered in the existing `AddAgentServices` feature module.
- Results are `Unknown`, `UpToDate`, or `UpdateAvailable(installed, latest)`. Network, HTTP, JSON,
  and parsing failures degrade to `Unknown` without throwing.
- Advisory only: the result never gates or alters the Update action.
- The settings agent card shows a small update chip next to the state chip (`2.0.32 → 2.0.33`)
  only when an update is actually available; `Unknown` and up-to-date show nothing.
- The six built-in seed descriptors gain `updateCheck` blocks, and the plugin descriptor docs gain
  the block.

## Impact

- New capability: `agent-update-detection` (distinct from `update-checking`, which covers CLIHub's
  own Velopack updates, and from `agent-availability-display`, which dims agents unavailable in a
  project).
- Modified capability: `agent-ui` — the "Agent card displayed content" requirement gains the
  update indicator.
- Affected code (future): `CLIHub.Core` plugin model/loader, new agent services and HTTP adapter,
  DI feature module, seed descriptors, `AgentManagementViewModel`/`AgentCardItem`, and
  `SettingsAgentsPage.xaml`.
- No new package reference: the adapter uses the framework `System.Net.Http.HttpClient`.
- Dependencies: builds on the version probe (`agent-version`), the shared probe TTL preference,
  the detached-read catalog contract (`IPluginCatalog`), the landed DI feature modules, and the
  scoped probe invalidation commit `91035c2`. None of them is an open change.

## Non-goals

- Gating, disabling, or changing the Update action based on the check result.
- Polling beyond the existing agent probe cadence; there is no background updater.
- Supporting distribution channels other than npm and GitHub releases.
- Showing availability in the launch-window agent row; this change covers the settings card only.
- Parsing pre-release ordering or build metadata; comparison is a numeric triple.
- Reworking the existing version-population loops or version sentinels (see `design.md`).
