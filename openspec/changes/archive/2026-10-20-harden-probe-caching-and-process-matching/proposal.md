# Harden probe-cache invalidation and running-process matching

## Why

Two High-severity findings from a read-only defect-class hunt share one failure mode: the agent
surfaces silently do more work than the contract says, or claim a capability the production wiring
does not provide.

1. **Probing one agent clears every agent's caches.** `AgentManagementService.ProbeAsync` calls the
   parameterless `Invalidate()` on both `IAgentDetectionService` and `IAgentVersionService` before
   probing. A single "Test" therefore expires every agent's detection result and version, so the
   next launch-window refresh re-runs `File.Exists` detection for every agent and starts one version
   process per agent. The Settings "Detect" pass calls `ProbeAsync` once per card, so each card also
   clears the entries the previous card just recomputed: after a detect-all only the last card's
   results stay cached.
2. **Runtime process matching is unreachable and the comment claims otherwise.** The Windows
   inspector always supplies an empty `CommandLine`, its private `GetCommandLine` helper is never
   called, and the matcher's Node.js/Python branch therefore always returns false. The branch and
   its tests describe a capability that production never had, while update blocking silently misses
   agents hosted inside a shared runtime.

Both defects are user-visible: the first causes avoidable process churn whenever the launch window is
shown after a probe; the second means "Update blocked while the agent is running" does not hold for
runtime-hosted agents.

## What Changes

- Add a scoped invalidation operation, `InvalidateFor(Plugin plugin)`, to `IAgentDetectionService` and
  `IAgentVersionService`. Detection removes that agent's `sys:` entry and every `proj:<id>:` entry;
  version removes that agent's cache entry. The parameterless `Invalidate()` stays as the full clear
  used by manual refresh and plugin reload.
- `AgentManagementService.ProbeAsync` uses the scoped operations. `DetectAsync` needs no new batch API:
  each per-card probe invalidates exactly its own agent, so a detect-all clears each agent once and
  leaves every agent's fresh result cached.
- Amputate the unreachable runtime-matching capability: remove `AgentProcessMatcher.IsRuntimeProcess`,
  remove `WindowsAgentProcessInspector.GetCommandLine`, remove the unused `CommandLine` member from
  `AgentProcessInstance`, and state the real contract (executable-name matching only) in code comments
  and `docs/architecture/processes.md`.
- Modify the `agent-detection` and `agent-version` specifications to state scoped invalidation.
- Keep the change as one OpenSpec change with two independently reviewable stages.

## Impact

- Public Core API: two interfaces gain `InvalidateFor(Plugin)`; `AgentProcessInstance` loses a record
  member that no production caller reads.
- Affected specs: `agent-detection` (detection cache invalidation), `agent-version` (on-demand version
  re-probe). No `agent-ui` or `preferences-ui` delta: their scenarios say "a process matching the
  agent" without promising command-line or runtime matching, so Stage 2 removes dead code and corrects
  the architecture document rather than changing a stated UI behavior.
- Affected code: `src/CLIHub.Core/Agents/AgentDetectionService.cs`,
  `src/CLIHub.Core/Agents/AgentVersionService.cs`,
  `src/CLIHub.Core/Agents/AgentManagementService.cs`,
  `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs`,
  `src/CLIHub.Core/Models/Agents/AgentProcessInstance.cs`,
  `src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs`, the matching unit tests, and
  `docs/architecture/processes.md`.
- Behavior preserved: a probed agent's fresh result is visible on the next detection and version
  lookup; unrelated agents stay cache-served; full refresh and plugin reload still clear everything.
- Deliberate residual gap: after amputation, an agent running as `node.exe` or `python.exe` behind a
  Windows command shim is still not matched, so its update is not blocked. The change makes that
  limitation explicit instead of implying support; closing it needs a separate process-identity
  design (see design.md, Option B).

## Non-goals

- The other hunt findings: the three version-population loops, magic version sentinels, and the general
  dead-member sweep (including the matcher's unused logger).
- Changing the plugin descriptor format, the configuration document, or the process-launch pipeline.
- Implementing WMI, PEB/`NtQueryInformationProcess`, or any other command-line retrieval.
- Changing cache TTLs, persistence, eviction policy, or the version-probe coalescing behavior.
- Redesigning the Settings or launch-window UI, or changing when process monitoring runs.
- Implementing runtime-hosted agent detection; that is a follow-up proposal with its own risk profile.
