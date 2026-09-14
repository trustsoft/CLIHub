# Tasks: add-agent-detection

## 1. Core contracts and models

- [x] 1.1 Add `ProbeResult` (exit code, stdout, timed-out flag) and
  `RunProbe(fileName, arguments, timeoutSeconds)` to `IProcessRunner`;
  implement in `SystemProcessRunner` (hidden window, redirected stdout,
  kill on timeout). Verify: solution builds and existing tests pass.
- [x] 1.2 Model the `detect` section (`project` path list) on
  `AgentManifest`. Verify: PluginLoader test loading a manifest with
  `detect.project` passes; manifests without `detect` still load.
- [x] 1.3 Model `probe` (`ttlMinutes`, `timeoutSeconds`) and `agents`
  (id -> `hostInstalled`, `version`, `lastProbed`) on `Config` with
  built-in defaults for missing/non-positive values. Verify: ConfigStore
  round-trip test covering both sections and the defaults fallback.

## 2. AgentDetector (Core)

- [x] 2.1 Host probe: run `version` action command hidden, exit code 0 =
  installed, first stdout line trimmed = version; timeout = not
  installed. Verify: unit tests with fake runner (success, failure,
  timeout, multi-line stdout).
- [x] 2.2 PATH fallback for manifests without a `version` action
  (installed = `run` executable found, no version). Verify: unit tests
  with fake runner (found / not found).
- [x] 2.3 Startup round: skip agents with fresh cache (TTL vs
  `lastProbed`), probe the rest, write the `agents` section exactly once
  after the round. Verify: unit tests with fake clock — fresh entry not
  probed, stale entry probed, single save call.
- [x] 2.4 Project detection: any-of path check against the project
  folder, live, no caching; no declared paths = initialized. Verify:
  unit tests with fake file system (marker file present, marker
  directory present, none present, no paths declared).
- [x] 2.5 Availability derivation as a pure function of (manifest, host
  status, project initialized). Verify: unit tests covering the full
  table (installed+initialized, installed+not, not installed) for
  run/resume/init/update.

## 3. App wiring

- [x] 3.1 Start the lazy background probe round at application startup
  (App.xaml.cs) without blocking the UI; round completion refreshes the
  detector snapshot. Verify: manual run — popup opens instantly,
  `agents` section appears in `%AppData%\CLIHub\config.json` after the
  round.
- [x] 3.2 Rework `PopupViewModel`/`AgentItemViewModel`: agents list
  filtered to installed, version displayed, `run` enabled by derived
  availability, recompute on project selection and on round completion
  (marshalled to the UI thread). Verify: build + manual checklist —
  hidden uninstalled agent, version visible, run disabled in a project
  without markers, enabled in a project with them.

## 4. Plugin manifests

- [x] 4.1 Add `"detect": { "project": [".claude", "CLAUDE.md"] }` to the
  `claude` manifest. Verify: PluginLoader loads it; marker check works
  against a real folder with `CLAUDE.md`.
- [x] 4.2 Add `plugins/agents/opencode/agent.json` (run/resume/version/
  update commands and `detect.project` markers verified against the real
  CLI). Verify: loader test or manual load shows the agent with
  detection paths.
- [x] 4.3 Add `plugins/agents/pi/agent.json` (same verification as
  4.2).

## 5. Final verification

- [x] 5.1 Run `dotnet test CLIHub.sln` and `dotnet build CLIHub.sln` —
  everything green, no new warnings.
- [x] 5.2 Manual end-to-end pass: fresh config (no `agents` section) ->
  start app -> popup from cache-empty state -> round completes -> list
  and versions update -> switch projects -> availability recomputes.
