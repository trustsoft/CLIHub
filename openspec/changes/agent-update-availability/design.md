# Design: agent update availability

## Sources

| Kind | Request | Latest version field |
|---|---|---|
| `npm` | `GET https://registry.npmjs.org/<package>/latest` | `version` |
| `githubReleases` | `GET https://api.github.com/repos/<repo>/releases/latest` | `tag_name` (leading `v` stripped) |

Unauthenticated requests; the GitHub 60 requests/hour limit is sufficient for six agents once
results are cached with the existing agent probe TTL (`agentProbeTtlMinutes` preference).

## Researched mapping for the built-in agents

| Agent | Source | Reference |
|---|---|---|
| opencode | npm `@opencode/cli` | installed package matches the binary version (2.0.32) |
| cline | npm `cline` | docs.cline.bot CLI reference |
| pi | npm `@earendil-works/pi-coding-agent` | installed package (1.1.0) |
| openclaude | npm `@twigpine/openclaude` | current package after the `@gitlawb` deprecation |
| qwen-code | GitHub releases `QwenLM/qwen-code` | standalone install channel; no npm package installed |
| github-copilot | GitHub releases `github/copilot-cli` | winget/GitHub distribution, not npm |

## Version extraction and comparison

- Installed version comes from the existing `--version` probe output. Formats differ
  (`opencode v2.0.32`, `0.30.0 (OpenClaude)`, `1.0.100`); extraction uses the first
  `\d+\.\d+(\.\d+)+` match.
- v1 comparison is numeric over `major.minor.patch`, ignoring pre-release suffixes: a difference
  in the triple means "update available". Full SemVer precedence (pre-release ordering) is a
  future refinement.
- Unparseable output on either side → `Unknown`.

## Principles

- **Advisory only.** The result never gates the Update action; the agent's own updater remains
  the source of truth for actually updating. Offline or failed checks degrade to `Unknown`.
- **Descriptor-driven.** Only agents that declare an `updateCheck` block are checked; unknown
  `source` values are ignored, mirroring the ignored-unknown-command-kind rule.
- **Cached like probes.** Availability results follow the same TTL behavior as version probes so
  the registry/release APIs are not hammered.

## Architecture

- New port in `CLIHub.Core` (infrastructure ports pattern):
  `IAgentUpdateAvailabilityService.CheckAsync(Plugin, CancellationToken)` returning
  `UpdateAvailability { Status, InstalledVersion, LatestVersion }`.
- HTTP implementation alongside the existing Windows adapters; the npm/GitHub clients are thin
  JSON readers over `HttpClient`.
- Descriptor loader exposes the parsed `updateCheck` block on `Plugin` (nullable).
- Naming stays clear of `update-checking` (CLIHub's own Velopack updates).

## Alternatives considered

- Running the agents' `update` commands in a check-only mode — rejected: none of the six CLIs
  document a dry-run flag; the command itself performs the mutation.
- Parsing agents' internal update-check logs — rejected: not a contract, agent-specific, and
  only observable after the agent has run.
- Hard-coding sources in app code instead of the descriptor — rejected: the descriptor is the
  per-agent metadata home (see the existing `detection` block) and stays user-editable.
