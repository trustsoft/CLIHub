# Tasks

Stages are sized for a fresh session. Each stage ends green: `dotnet build CLIHub.sln` and
`dotnet test CLIHub.sln`. No stage commits; the user decides about commit and archive (and per the
repository policy, both wait for the manual smoke in 5.7).

## Stage 1: Core model, match-detail tier, and matcher semantics

**Goal**: `AgentProcessInstance` carries an optional command line; the matcher implements the
npm-package path-segment rule and tier-aware candidate derivation, all in `CLIHub.Core` with no
Windows dependency.

- [x] 1.1 `src/CLIHub.Core/Models/Agents/AgentProcessInstance.cs`: add the optional parameter
  `string? CommandLine = null` as the fourth positional record parameter with XML documentation
  stating it is supplied only for command-line-detail inspection and is null when command lines
  are not retrieved or could not be read (design D2). Every existing three-argument construction
  site keeps compiling — do not touch them.
- [x] 1.2 New file `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatchDetail.cs`: the
  `AgentProcessMatchDetail` enum (`ExecutableName`, `CommandLine`) with XML docs exactly as
  specified in design D2.
- [x] 1.3 `src/CLIHub.Core/Infrastructure/Processes/AgentProcessMatcher.cs`: add
  `public static IReadOnlySet<string> RuntimeHostProcessNames` containing `node`, `python`,
  `pythonw` (case-insensitive set) with XML docs naming the design decision (npm shims and python
  entry points; shells deliberately excluded).
- [x] 1.4 Same file, change `GetCandidateProcessNames` to
  `GetCandidateProcessNames(IEnumerable<Plugin> plugins, AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine)`:
  keep the existing launch-name derivation verbatim, then when
  `detail == AgentProcessMatchDetail.CommandLine` and any plugin both declares a launch command
  and an npm `updateCheck` (source compared `OrdinalIgnoreCase` per
  `src/CLIHub.Core/Agents/AgentReleaseClient.cs:95`, non-empty `Package`), union
  `RuntimeHostProcessNames` into the result. Update the class and method XML docs: candidates now
  include the runtime hosts exactly when command-line matching is active, so the inspector
  pre-filter still cannot drop a match the matcher would make.
- [x] 1.5 Same file, extend `IsMatch`: keep the executable-name comparison; add the command-line
  rule of design D5 as a private helper — plugin has a launch command; plugin `updateCheck` is npm
  with a non-empty `Package`; the process executable's file name without extension is in
  `RuntimeHostProcessNames`; `processInstance.CommandLine` is non-empty; the command line
  normalized by `/` → `\` contains `node_modules\` + package with `/` → `\` + `\`, compared
  `OrdinalIgnoreCase` (the trailing separator is the boundary). Update the class and member XML
  docs to describe both rules.
- [x] 1.6 `tests/CLIHub.Core.Tests/Infrastructure/Processes/AgentProcessMatcherTests.cs`: keep
  every existing test green (the three-argument `CreateProcess` helper still compiles; command
  lines default to null so the name-rule tests are unchanged), **except**
  `IsMatch_RuntimeHostedProcess_ReturnsFalse` (`:78-91`): retitle it to
  `IsMatch_RuntimeHostedProcessWithoutCommandLine_ReturnsFalse` and extend its arrange comment —
  a host-named process with no command line still deliberately does not match (design D5).
- [x] 1.7 Same test file, add command-line semantics tests (each uses a new
  `CreateProcess(executablePath, commandLine)` overload or optional parameter):
  `IsMatch_NodeShimCommandLineOfDeclaredNpmPackage_ReturnsTrue` (the backlog's exact shape:
  `node.exe` + `C:\Users\Maxim\AppData\Roaming\npm\node_modules\@opencode\cli\bin\opencode.js run`
  against a plugin with launch `opencode` and `updateCheck` `@opencode/cli`);
  `IsMatch_UnscopedNpmPackage_ReturnsTrue` (`cline`); `IsMatch_ForwardSlashCommandLine_ReturnsTrue`;
  `IsMatch_CommandLineCaseInsensitive_ReturnsTrue`;
  `IsMatch_SiblingPackageWithCommonPrefix_ReturnsFalse` (`@opencode/cli` plugin vs a
  `node_modules\@opencode\cli-extra\...` command line);
  `IsMatch_UnrelatedNodeScript_ReturnsFalse` (`node.exe server.js` and an absolute
  `D:\build\script.js`); `IsMatch_PluginWithoutNpmUpdateCheck_ReturnsFalse` (githubReleases
  plugin); `IsMatch_EmptyCommandLine_ReturnsFalse`.
- [x] 1.8 Same test file, add candidate-derivation tests:
  `GetCandidateProcessNames_CommandLineDetail_AddsRuntimeHostsWithNpmPlugin` (asserts `node`,
  `python`, `pythonw` present beside the launch names);
  `GetCandidateProcessNames_ExecutableNameDetail_AddsNoRuntimeHosts`;
  `GetCandidateProcessNames_NoNpmPlugin_AddsNoRuntimeHosts` (launch-only catalog, `CommandLine`
  detail, host names absent).
- [x] 1.9 Run `dotnet build CLIHub.sln` — 0 warnings, 0 errors. Run
  `dotnet test CLIHub.sln` — every suite green.

## Stage 2: Monitor detail plumbing and the Settings-path gate

**Goal**: the monitor exposes the tier and passes it to the inspector;
`AgentManagementService.UpdateAsync` gates on the full tier off the caller's context.

- [x] 2.1 `src/CLIHub.Core/Agents/AgentProcessMonitor.cs`: add the optional
  `AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine` parameter to
  `GetRunningAgents()`, `GetRunningAgents(Plugin)`, and `HasRunningAgents(Plugin)` (the
  parameterless `HasRunningAgents()` at `:41-44` delegates with the default); thread `detail`
  into `GetCandidateProcessNames` (`:72`) and the inspector call (`:73`); update XML docs to name
  the two tiers and that update gating uses `CommandLine`.
- [x] 2.2 `src/CLIHub.Core/Infrastructure/Processes/IAgentProcessInspector.cs`: change the port
  method to `GetRunningProcesses(IReadOnlySet<string> candidateProcessNames, AgentProcessMatchDetail detail)`
  — **no default parameter** (design D2). Update the XML docs: what is supplied per tier, that
  command lines may be null per process even under `CommandLine` detail, and that implementations
  derive no candidates of their own.
- [x] 2.3 `src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs` (mechanical pre-stage-3
  update only): accept the new parameter and ignore it (keep behavior); the real WMI join lands in
  Stage 3. This keeps the solution compiling and the Windows tests meaningful between stages.
- [x] 2.4 `src/CLIHub.Core/Agents/AgentManagementService.cs:76`: replace the synchronous gate
  enumeration with
  `var runningAgents = await Task.Run(() => this.processMonitor.GetRunningAgents(plugin), cancellationToken).ConfigureAwait(false);`
  (design D7) — same `Blocked` outcome and message shape at `:77-83`.
- [x] 2.5 Update every inspector mock/setup to the two-argument signature (mechanical; add
  `It.IsAny<AgentProcessMatchDetail>()`): `tests/CLIHub.Core.Tests/Agents/AgentProcessMonitorTests.cs:34,51,68,86,104`,
  `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs:31`, and the Core-side helpers in
  Stage 4's files can wait — but this stage's builds must pass, so update
  `tests/CLIHub.Tests/Builders/LaunchWindowViewModelBuilder.cs:443` and the
  `AgentManagementViewModelTests` setups (`:55,79,89,497,508,517,528,594,641`) now if the test
  projects are part of the solution build (they are — update them in this stage).
- [x] 2.6 `tests/CLIHub.Core.Tests/Agents/AgentProcessMonitorTests.cs`: add
  `GetRunningAgents_DefaultDetail_MatchesNodeShimCommandLine` (mock returns a `node.exe`
  `AgentProcessInstance` with the opencode package command line; opencode plugin with npm
  `updateCheck`; assert one `RunningAgent`) and
  `GetRunningAgents_ExecutableNameDetail_DoesNotMatchNodeShim` (same setup, call with
  `AgentProcessMatchDetail.ExecutableName`, assert empty; verify the inspector received the
  `ExecutableName` detail).
- [x] 2.7 `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs`: add
  `UpdateAsync_RuntimeHostedAgent_IsBlocked` (monitor over the mocked inspector returning the
  node-shim instance; assert `AgentUpdateOutcome.Blocked` and a message naming the agent) and
  `UpdateAsync_UnrelatedNodeScript_StartsUpdate` (node.exe with an unrelated command line; assert
  the update command executes).
- [x] 2.8 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` — green.

## Stage 3: Windows command-line retrieval (bulk WMI join, hard timeout, degradation)

**Goal**: the Windows inspector supplies command lines for candidate processes under
`CommandLine` detail — one bulk WMI query, a 2-second hard budget, fail-open degradation — with
the WMI dependency behind an internal testable seam.

- [x] 3.1 New file `src/CLIHub.Core.Windows/ProcessCommandLineSource.cs`: the internal
  `IProcessCommandLineSource` interface (design D6) and the sealed
  `WmiProcessCommandLineSource` implementation — one
  `System.Management` `ManagementObjectSearcher` query
  (`SELECT ProcessId, CommandLine FROM Win32_Process` with a `WHERE Name = '…' OR …` clause built
  from the supplied names), `EnumerationOptions.Timeout` set as a best-effort hint, results mapped
  to `IReadOnlyDictionary<int, string>` skipping null/empty command lines; exceptions propagate to
  the inspector's degradation path. Use the already-referenced `System.Management` 8.0.0
  (`CLIHub.Core.Windows.csproj:11`) — add no package.
- [x] 3.2 `src/CLIHub.Core.Windows/WindowsAgentProcessInspector.cs`: take
  `IProcessCommandLineSource` as a second constructor parameter; add
  `private static readonly TimeSpan CommandLineTimeout = TimeSpan.FromSeconds(2);`. Under
  `CommandLine` detail, after the existing snapshot loop: if no candidate-named process was seen,
  skip the query; otherwise run
  `Task.Run(() => source.GetCommandLines(runningCandidateNames)).Wait(CommandLineTimeout)` inside
  its own try/catch — on timeout or any exception, log one warning
  (`"Process command-line retrieval failed or timed out; matching degrades to executable names"`)
  and continue; attach the command line to each returned instance whose PID is in the map
  (`CommandLine = map.TryGetValue(pid, out var cmd) ? cmd : null`). `ExecutableName` detail never
  calls the source. Update the class XML docs (the `:89` comment "command lines are not
  retrieved" is now conditional).
- [x] 3.3 `src/CLIHub.Core.Windows/WindowsServiceCollectionExtensions.cs:44`: register the
  inspector through a factory —
  `services.AddSingleton<IAgentProcessInspector>(sp => new WindowsAgentProcessInspector(new WmiProcessCommandLineSource(...), sp.GetRequiredService<ILogger<WindowsAgentProcessInspector>>()))`
  — giving the WMI source its logger per its constructor.
- [x] 3.4 `tests/CLIHub.Core.Windows.Tests/WindowsAgentProcessInspectorTests.cs`: update the
  constructor to a fake `IProcessCommandLineSource` (new
  `FakeProcessCommandLineSource` in the test project, dictionary-backed, with optional
  throw/block behaviors). Existing tests keep passing a source that is never consulted (they use
  `ExecutableName` detail — pass `AgentProcessMatchDetail.ExecutableName` in the existing calls at
  `:30,41,51,65,79,90,101`).
- [x] 3.5 Same test file, add:
  `GetRunningProcesses_CommandLineDetail_AttachesCommandLineByProcessId` (fake maps a PID to a
  command line; assert the returned instance for that PID carries it; assert instances without a
  map entry carry null);
  `GetRunningProcesses_ExecutableNameDetail_DoesNotQueryCommandLines` (fake counts calls; assert
  zero);
  `GetRunningProcesses_NoCandidateProcesses_DoesNotQueryCommandLines` (non-matching candidates;
  assert zero calls);
  `GetRunningProcesses_SourceThrows_LogsAndDegrades` (fake throws; assert no exception escapes,
  instances return with null command lines; optionally assert the warning through a mock
  logger);
  `GetRunningProcesses_SourceExceedsTimeout_Degrades` (fake blocks on a
  `ManualResetEventSlim` past a short timeout — make the budget injectable for tests via an
  optional constructor parameter defaulting to 2 s, or an internal property; assert the call
  returns within a bounded wait, releases the gate in dispose/finally, and command lines are
  null).
- [x] 3.6 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` — green.

## Stage 4: Launch-window advisory/gate split and Settings-card coverage

**Goal**: the requery path stays cheap; the launch-window click gate is authoritative and
asynchronous; the Settings blocked-state pass proves the runtime-host block end to end.

- [x] 4.1 `src/CLIHub/ViewModels/Shared/WindowActionCoordinator.cs:155`: change
  `CanExecuteUpdate` to call
  `this.processMonitor.HasRunningAgents(agent.Plugin, AgentProcessMatchDetail.ExecutableName)` and
  update the inline comment: the requery check is an advisory early-out; runtime-hosted agents are
  blocked at execution time (design D3).
- [x] 4.2 Same file `:163-182`: restructure `ExecuteUpdate` — keep the synchronous early-outs
  (null agent, `!agent.CanUpdate`, and the cheap name-only fast block via
  `HasRunningAgents(agent.Plugin, AgentProcessMatchDetail.ExecutableName)` reporting blocked), then
  `_ = this.ExecuteUpdateAsync(agent);` where the private `async Task ExecuteUpdateAsync(AgentItem agent)`
  runs `var runningAgents = await Task.Run(() => this.processMonitor.GetRunningAgents(agent.Plugin));`
  and either reports `ReportUpdateBlockedByRunningAgents` (`:176-178` shape preserved) or continues
  into `this.RunAgentCommandAsync(agent, AgentCommandKind.Update)` (`:181`). Wrap unexpected
  failures so nothing escapes a fire-and-forget continuation (observe and log via the class's
  existing status reporting; do not introduce a new logger dependency — reuse the
  `StatusMessageCoordinator` failure report if one exists, otherwise swallow-and-log is not
  available and the operation must stay exception-safe by construction).
- [x] 4.3 Verify `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:362` needs **no**
  change: the default tier is `CommandLine`, so the blocked-state pass becomes full-fidelity
  through Stage 2 alone. Confirm by reading, do not edit.
- [x] 4.4 New `tests/CLIHub.Tests/ViewModels/Shared/WindowActionCoordinatorTests.cs`:
  `CanExecuteUpdate_NameMatchedAgent_ReturnsFalse` and
  `CanExecuteUpdate_NodeShimAgent_QueriesExecutableNameDetailOnly` (mock `AgentProcessMonitor` is
  not possible — it is a sealed concrete class; build the coordinator over a real monitor with the
  mocked `IAgentProcessInspector` seam per the `AgentManagementServiceTests.Monitor` pattern at
  `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs:28-41`, and verify detail via the
  inspector mock's received argument);
  `ExecuteUpdate_NodeShimAgentRunning_BlocksWithoutLaunching` (full-tier match; assert the launch
  coordinator/status reports the block and the external launch path is not invoked);
  `ExecuteUpdate_NoAgentRunning_StartsUpdate` (async settle with a bounded `SpinWait`).
- [x] 4.5 `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`: extend the
  `Processes(...)` helper (`:75-89`) with an optional command-line argument and add
  `Load_NodeShimAgentRunning_DisablesUpdateAndExplainsBlock` — a node.exe instance with the
  opencode package command line; assert the opencode card settles `CanUpdate == false` with the
  "Update blocked while the agent is running: …" message, mirroring
  `Load_MatchingAgentProcessRunning_DisablesUpdateAndExplainsBlock` (`:418-436`), and that an
  unrelated node-script instance does not block its agent.
- [x] 4.6 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` — green; report per-project
  counts.

## Stage 5: Spec alignment, docs, backlog, and final verification

The deltas already exist under `openspec/changes/commandline-agent-blocking/specs/`; correct them
in place, do not recreate them.

- [x] 5.1 Re-read `openspec/specs/agent-ui/spec.md:219-277` (requirement "Agent card action
  availability") and the new `openspec/specs/agent-process-matching/spec.md` against the
  implemented code; correct any statement that does not match. All original `agent-ui` scenario
  names stay verbatim; the new "Update blocked by a runtime-hosted agent" and "Unrelated
  runtime-hosted processes do not block an update" scenarios must match the shipped behavior.
  Same pass for `openspec/specs/preferences-ui/spec.md:535-567` ("Agent card actions") and its new
  "Update blocked by a runtime-hosted agent" scenario.
- [x] 5.2 `docs/architecture/processes.md`: rewrite the agent-process-monitoring section per
  design D10 — the matcher bullet (`:20-22`) gains the command-line rule with the npm package
  marker; the inspector bullet (`:15-19`) gains the bulk-WMI join, the 2-second budget, and the
  degradation; replace the closing limitation paragraph (`:29-33`) with the new contract and the
  known residuals (unreadable elevated/other-session command lines; degraded passes match by
  executable name only; fail-open).
- [x] 5.3 `docs/backlog.md`: replace only the "Runtime-hosted agent blocking (Option B)" entry
  (`:61-71`) with a resolved entry in the format of "Async blocked-state population on the agents
  page (resolved)" (`:20-46`): resolved by the `commandline-agent-blocking` change (record the
  date); resolved model (bulk-WMI command-line join behind the inspector port with a 2 s budget;
  npm-package path-segment matching; two detail tiers; advisory requery + authoritative gates;
  fail-open degradation); note the proactive provenance (pulled from the backlog on 2026-10-24
  before its trigger fired); guarded by the Stage 1-4 tests; trigger for reopening (a report of a
  runtime-hosted agent not blocking, or WMI-timeout pile-up per design open question 5). Leave
  every other entry untouched.
- [x] 5.4 Run `dotnet build CLIHub.sln` — 0 warnings, 0 errors. Run `dotnet test CLIHub.sln` —
  every suite green; quote the summary.
- [x] 5.5 Run `openspec validate --all` — every item passes; quote the output.
- [x] 5.6 Run `graphify update .` from the repository root so the graph reflects the new
  matching, monitor, inspector, and coordinator surfaces.
- [x] 5.7 Manual smoke (user; blocks commit and archive per the repository policy): start a
  runtime-hosted agent (for example
  `node "%APPDATA%\npm\node_modules\@opencode\cli\bin\opencode.js"` or any installed npm agent
  shim); open Settings → CLI Agents and confirm the card's Update is blocked with the running
  explanation; from the launch window confirm the Update button may stay enabled but clicking it
  reports the block and starts nothing; stop the agent and confirm both surfaces recover; also
  run an unrelated node script and confirm it blocks nothing; check the current day's log
  (`%APPDATA%\CLIHub\logs\`) for unexpected command-line-retrieval warnings on a healthy machine.
- [x] 5.8 Report the exact gate outputs, any deviation, and the files changed for review. Do not
  commit; ask the user about archiving and committing.

**Final gate:** every step above passes or is explicitly reported as not run.
