# Proposal: add-agent-detection

## Why

The popup currently lists every bundled agent regardless of whether it is
installed on the host, shows no versions, and enables `run` whenever the
manifest happens to define it. The vision's core promise — a dynamic agent
list "with respect to what is installed on the host" with action
availability derived from host/project state — has no spec and no code.
Every richer UI feature (versions, `Init ▸` submenu, `Update ▸` submenu)
is blocked on this foundation.

## What Changes

- New Core capability **agent detection**:
  - Host probe: hidden run of the agent's `version` action command —
    exit code 0 means installed, stdout (first line, trimmed) is the
    version. When a manifest defines no `version` action, fall back to a
    PATH check of the `run` command's executable.
  - Probe results are cached in the `agents` section of `config.json`
    (`hostInstalled`, `version`, `lastProbed`) with a TTL; fresh cache
    entries skip re-probing.
  - A lazy background probe round runs at application startup and writes
    the `agents` section once per round (not per agent).
  - Probe settings live in the `probe` section of `config.json`
    (`ttlMinutes`, `timeoutSeconds`) with built-in defaults.
  - Project detection: `detect.project` paths from the manifest are
    checked live against the selected project folder — any-of semantics
    (at least one path exists = initialized), no caching.
  - Action availability is derived: `run`/`resume` require
    host-installed + project-initialized; `init` requires
    host-installed + not-initialized; `update` requires host-installed;
    `version` is information, not an action.
- Minimal popup wiring: the Agents pane lists only host-installed
  agents, shows each agent's version, and enables `run` per the derived
  availability instead of the static "manifest has run" check.
- Manifest format: new optional `detect` section with `project` path
  markers; the bundled `claude` manifest gains its markers
  (`.claude`, `CLAUDE.md`).
- Two new bundled agent plugins: `opencode` and `pi`.

## Capabilities

### New Capabilities

- `agent-detection`: host availability probing with TTL cache, live
  project initialization checks, derived action availability, and the
  `probe`/`agents` config sections.

### Modified Capabilities

- `agent-plugins`: the manifest gains an optional `detect` section
  (project path markers); host detection reuses the existing `version`
  action as its probe.
- `popup`: the Agents pane becomes detection-driven — installed-only
  list, version display, availability-derived `run`.

## Impact

- `src/CLIHub.Core`:
  - new `AgentDetector` service (probe round, TTL logic, project checks,
    availability derivation);
  - `IProcessRunner` gains a probe method (hidden argv, captured stdout,
    timeout, exit code);
  - `AgentManifest` models the `detect` section;
  - `Config` models `probe` and `agents` sections (currently round-tripped
    as unknown fields).
- `src/CLIHub.App`:
  - startup hook for the lazy background probe round;
  - `PopupViewModel`/`AgentItemViewModel` consume detection results.
- `plugins/agents/`: new `opencode`, `pi` manifests; `claude` gains
  `detect.project` markers.
- `tests/CLIHub.Core.Tests`: AgentDetector coverage with fake
  process runner, file system, and clock.
- No breaking changes to config.json (new sections; unknown-field
  preservation already covers hand-edited files).
