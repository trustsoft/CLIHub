# Agent update availability: detect real updates before running them

## Why

The Update action currently runs the agent's own update command blindly: the user cannot tell
whether a real update exists, and every invocation potentially mutates the installation. The
installed version is already probed (agent-version), but nothing compares it against the latest
version published by the agent's distribution channel.

A gap analysis of the built-in agents shows the distribution channels are known and machine-
readable: five agents are npm packages (opencode → `@opencode/cli`, cline → `cline`,
pi → `@earendil-works/pi-coding-agent`, openclaude → `@twigpine/openclaude`) and two are
distributed through GitHub releases (Qwen Code standalone → `QwenLM/qwen-code`,
GitHub Copilot CLI → `github/copilot-cli`).

## What Changes

- Optional `updateCheck` block in the plugin descriptor declaring the agent's distribution
  source: `{ "source": "npm", "package": "…" }` or
  `{ "source": "githubReleases", "repo": "owner/repo" }`. Unknown source kinds are ignored
  (same pattern as unknown command kinds). Descriptors without the block keep today's behavior.
- New Core port `IAgentUpdateAvailabilityService` with an HTTP implementation: fetch the latest
  version from the declared source, compare against the probed installed version, and report
  `UpToDate`, `UpdateAvailable(installed, latest)`, or `Unknown`.
- The availability result is advisory only: it never gates the Update action. Offline, failures,
  or unparseable versions degrade to `Unknown` and the UI shows nothing.
- The UI surfaces the result when known (for example `2.0.32 → 2.0.33` near the agent's version);
  the exact presentation is a UI decision.
- Built-in seed descriptors gain `updateCheck` blocks for the six supported agents.

## Impact

- New spec: `agent-update-availability` (distinct from `update-checking`, which covers CLIHub's
  own Velopack updates)
- Affected code (future): `CLIHub.Core` ports/services, plugin descriptor loader and
  documentation, seed descriptors, agents pane UI
- Non-goals: gating the Update action on the check result; polling beyond the existing probe
  cadence; supporting distribution channels other than npm and GitHub releases
