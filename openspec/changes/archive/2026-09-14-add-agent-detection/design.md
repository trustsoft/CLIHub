# Design: add-agent-detection

## Context

See proposal.md — Why/What. Today the popup builds its agent list from
`PluginLoader` output and computes `run` availability statically
("manifest defines run"). `IProcessRunner` has only `CommandExists` and
`StartDetached`; there is no hidden-probe path. `Config` models
`schemaVersion`, `runtime`, `hotkey`, `projects` and round-trips
everything else through `AdditionalData`, so an `agents` cache written by
a future version already survives saves. `IClock` (`UtcNow`) exists for
TTL testing.

## Goals / Non-Goals

**Goals:**

- Detection pipeline fully in Core, testable with fakes (process runner,
  file system, clock).
- Minimal UI wiring: installed-only list, version display, availability-
  derived `run`, live recompute on project selection.
- Manifest format extension and three exercising plugins (claude updated,
  opencode and pi added).

**Non-Goals:**

- Agents pane redesign: `resume` button, `Update ▸` / `Init ▸` submenus,
  footer (separate change).
- Parallel probing, manual refresh, probe-on-demand per popup open.
- Settings window for `probe` values (config.json hand-edits are fine).

## Decisions

### D1: One probe run per agent — `version` command doubles as host probe

Exit code decides `hostInstalled`; stdout first line (trimmed) is the
version. Alternative — a separate `detect.host` argv — duplicates the
command in the manifest and can drift from `version`. When a manifest has
no `version` action, fall back to `CommandExists` on the `run` executable
(cheap, no version). Rationale: vision states "version is merged with
detect.host".

### D2: `IProcessRunner` gains `RunProbe`, not a new interface

`ProbeResult RunProbe(string fileName, string arguments, int
timeoutSeconds)` with exit code, stdout, and a timed-out flag.
Implementation: `UseShellExecute = false`, `RedirectStandardOutput = true`,
`CreateNoWindow = true`, `WaitForExit(timeout)` + `Kill()` on expiry.
Alternative — a separate `IProbeRunner` — splits one concern (run a
process) across two abstractions; `SystemProcessRunner` stays the single
App-side implementation, and tests fake the same interface they already
fake (`CapturingProcessRunner` in tests).

Windows detail (found during implementation): npm-shim CLIs
(`opencode`, `pi`) live as `.cmd` scripts next to an extensionless sh
script. `CreateProcess` resolves neither, so `RunProbe` resolves the
executable via PATH + PATHEXT (shared with `CommandExists`, extension
hits only when the command already carries one) and wraps `.cmd`/`.bat`
targets in `cmd.exe /c`.

### D3: Config sections modeled explicitly

`probe` (`ttlMinutes`, `timeoutSeconds`) and `agents` (map of agent id to
`hostInstalled`/`version`/`lastProbed`) become typed properties on
`Config` with v1 defaults `ttlMinutes = 1440`, `timeoutSeconds = 10`.
Non-positive or missing values fall back to defaults at read time
(agents rarely change; a day-old cache is fine and keeps startup free of
process spawns). `lastProbed` stored as UTC ISO-8601.

### D4: Sequential probe round, one background task

At startup, App kicks one background task: for each manifest whose cache
entry is fresh — skip; otherwise probe (or PATH-check) sequentially.
After the loop, reload config from the store, replace only the `agents`
section, save once. Sequential keeps the write logic trivial; with 2–4
bundled agents the round stays well under a few seconds. Parallelism is a
later optimization behind the same public surface.

### D5: Availability is a pure Core function

`Availability = f(manifest, hostStatus, projectInitialized)` — no I/O in
the derivation. The ViewModel recomputes it synchronously on project
selection (live path checks are cheap directory/file exists calls), so
selection never waits on probes. Host status comes from an
`AgentDetector` snapshot: cache immediately, refreshed when the round
completes (event the ViewModel marshals to the UI thread).

### D6: Default initialized when no markers declared

A manifest without `detect.project` is treated as initialized everywhere —
otherwise such an agent could never be `run`, which is useless. The
marker list is opt-in strictness.

### D7: Plugin manifests

`claude` gains `"detect": { "project": [".claude", "CLAUDE.md"] }`.
`opencode` and `pi` manifests follow the same shape with their own
commands and markers (exact commands verified during implementation).

## Risks / Trade-offs

- [Probe hangs or is slow on startup] → per-probe timeout + background
  task; UI renders from cache and updates after the round.
- [Version stdout is multi-line/banner] → first-line-trim heuristic may
  capture noise for some agents; acceptable — a manifest can omit
  `version` and fall back to PATH.
- [Concurrent config write: probe round vs ProjectRegistry save] → the
  round reloads config and writes once at the end, touching only
  `agents`; the window between reload and save is milliseconds. If this
  ever proves insufficient, route both through a single config owner.
- [PATH-installed shims (npm) behave differently under hidden run] →
  probe runs the same command string the terminal launch will use, so
  probe result reflects launch reality.

## Migration Plan

New config sections only; existing `config.json` files keep working
(unknown-field preservation). Rollback: sections are ignored by older
code — no migration needed.

## Open Questions

- Exact action commands and `detect.project` markers for `opencode` and
  `pi` — confirm while writing the manifests (does not affect specs or
  code shape).
