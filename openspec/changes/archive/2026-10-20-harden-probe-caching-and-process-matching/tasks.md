# Tasks: harden-probe-caching-and-process-matching

Implemented and archived (commit `91035c2`): Stage 1 (scoped probe-cache invalidation) and Stage 2
(process-matcher honesty) landed together, with build, tests, and `openspec validate --all` green.
Only the manual host-side smoke (3.5) remained open until verified; it has since been verified with no
issues.

## 1. Stage 1 — scoped probe-cache invalidation

- [x] 1.1 `src/CLIHub.Core/Agents/IAgentDetectionService.cs`: add `void InvalidateFor(Plugin plugin)`
      with XML documentation describing the system-and-project scope and the preservation of other
      agents' entries. Keep the parameterless `Invalidate()` for the full clear.
- [x] 1.2 `src/CLIHub.Core/Agents/AgentDetectionService.cs`: refactor the inline keys into private
      `BuildSystemKey`, `BuildProjectKeyPrefix`, and `BuildProjectKey` helpers used by both lookup and
      invalidation; implement `InvalidateFor` by removing the `sys:<id>` entry and every
      `proj:<id>:` entry. Guard with `ArgumentNullException.ThrowIfNull(plugin)`.
- [x] 1.3 `src/CLIHub.Core/Agents/IAgentVersionService.cs`: add `void InvalidateFor(Plugin plugin)`
      with XML documentation stating that it clears one cached version only and does not disturb other
      agents or an already-running probe.
- [x] 1.4 `src/CLIHub.Core/Agents/AgentVersionService.cs`: implement `InvalidateFor` as
      `cache.TryRemove(plugin.Id, out _)`; do not remove `inFlight` (single-flight coalescing must
      survive).
- [x] 1.5 `src/CLIHub.Core/Agents/AgentManagementService.cs`: replace the two parameterless clears in
      `ProbeAsync` with `InvalidateFor(plugin)` on detection and version. No change is needed in
      `AgentManagementViewModel.DetectAsync`: its per-card `ProbeAsync` calls now invalidate each agent
      exactly once. Add a brief comment in `ProbeAsync` recording that intent.
- [x] 1.6 Tests in `tests/CLIHub.Core.Tests/Agents/AgentDetectionServiceTests.cs`:
      `InvalidateFor_Plugin_RemovesSystemAndProjectEntriesForThatAgentOnly`,
      `InvalidateFor_Plugin_RemovesEntriesForEveryProject`,
      `InvalidateFor_UnknownPlugin_PreservesExistingEntries`. Use the existing `FakeTimeProvider` and
      temporary markers (add a second project directory for the multi-project test); prove B stays
      cached by deleting B's marker after priming.
- [x] 1.7 Tests in `tests/CLIHub.Core.Tests/Agents/AgentVersionServiceTests.cs`: generalize
      `PluginWithVersion` to accept a plugin id. Add
      `InvalidateFor_Plugin_RunsOnlyThatAgentsVersionCommandAgain` (prime A and B, change the fake
      output, invalidate A: A returns the new value, B keeps its cached value, run count +1) and
      `InvalidateFor_UnknownPlugin_DoesNotClearExistingEntries`.
- [x] 1.8 Tests in `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs`:
      update `ProbeAsync_ReturnsInstalledStateAndVersion` to verify `InvalidateFor(plugin)` once and
      `Invalidate()` never on both mocks; add `ProbeAsync_LeavesOtherAgentsCacheServed` and
      `DetectAll_LeavesEveryProbedAgentCached` using real `AgentDetectionService` +
      `AgentVersionService` with `FakeConfigurationRepository`, `FakeProcessOutputRunner`, and
      `FakeTimeProvider`, and add `DetectAll_InvalidatesEachAgentOnce` with the existing Moq mocks.
      Prove the cache by mutating the file system and fake output after priming, then asserting the
      untouched agent still returns the primed values without an extra run.
- [x] 1.9 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`. Both green.

## 2. Stage 2 — process-matcher honesty

- [x] 2.1 `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs`: delete
      `IsRuntimeProcess` and its call. Update the class and `IsMatch` XML documentation to state that
      matching uses the declared launch executable's file name, case-insensitively.
- [x] 2.2 `src/CLIHub.Core/Models/Agents/AgentProcessInstance.cs`: remove the `CommandLine` member and
      its XML documentation, leaving process id, executable path, and start time.
- [x] 2.3 `src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs`: delete `GetCommandLine` and the
      `commandLine` local; construct the snapshot without a command line. Replace the WMI comment with
      one sentence stating that matching is executable-name based.
- [x] 2.4 `src/CLIHub.Core/Infrastructure/Processes/IAgentProcessInspector.cs`: update the interface
      XML documentation to name the supplied fields (process id, executable path, start time).
- [x] 2.5 `tests/CLIHub.Core.Tests/Infrastructure/Processes/AgentProcessMatcherTests.cs`: remove every
      test whose scenario depends on a command line (Node/Python positive cases, the no-marker case,
      and the empty-command-line case), remove the now-unused `CreatePluginWithSystemPath` helper,
      update the process helper to the three-argument constructor, and add
      `IsMatch_RuntimeHostedProcess_ReturnsFalse` with a `node.exe` executable.
- [x] 2.6 `tests/CLIHub.Core.Tests/Agents/AgentProcessMonitorTests.cs`: remove
      `GetRunningAgents_NodeRuntimeProcess_ReturnsMatch`; update `CreateProcess` and its call sites to
      the three-argument constructor; keep per-agent and other-agent coverage.
- [x] 2.7 `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs` and
      `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`: update
      `AgentProcessInstance` constructor calls to remove the command-line argument.
- [x] 2.8 `docs/architecture/processes.md`: replace the runtime-matching paragraph with the
      executable-name contract; replace the WMI note with an explicit statement that the Windows
      inspector does not retrieve command lines and that runtime-hosted agents are not detected, and
      name the follow-up direction without committing to it.
- [x] 2.9 Run `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, and `openspec validate --all`. All
      green.

## 3. Final verification

- [x] 3.1 `dotnet build CLIHub.sln`
- [x] 3.2 `dotnet test CLIHub.sln`
- [x] 3.3 `openspec validate --all`
- [x] 3.4 `graphify update .` after all source edits are complete
- [x] 3.5 Manual smoke check: test one agent in Settings, then show the launch window and confirm only
      that agent's version process runs (other agents are cache-served); confirm an executable-named
      agent process still blocks its update and a `node.exe`-hosted agent does not.

## 4. Out of scope (do not fold into this change)

- The three version-population loops, magic version sentinels, and the general dead-member sweep
  (including `AgentProcessMatcher`'s unused logger).
- Plugin descriptor, configuration, and process-launch pipeline changes.
- WMI, PEB/`NtQueryInformationProcess`, or any command-line retrieval.
- Runtime-hosted agent detection; open a separate change if required.
