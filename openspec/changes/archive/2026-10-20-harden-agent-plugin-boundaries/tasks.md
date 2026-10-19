# Tasks: harden-agent-plugin-boundaries

Implementation happens in a fresh session with no prior context; `design.md` carries the full
rationale, the verified gating verdict, and file references. Tasks are checked off as they land.
Stages are sequential — each one ends buildable and green; never start the next stage with a red
tree.

## 1. Stage 1 — Core: detached plugin reads

- [x] 1.1 Add `Plugin.Copy()` (`src/CLIHub.Core/Models/Plugins/Plugin.cs`), mirroring
      `Project.Copy()`: scalars assigned; **deep-copy** `Commands` (new `AgentCommands`, one new
      `PluginCommand` per kind via `Get(AgentCommandKind)`) and `Detection` (new `AgentDetection`
      with copied `SystemPaths`/`ProjectIndicators`); XML doc stating the detached-copy contract
- [x] 1.2 `PluginCatalog.GetAllPlugins()` returns
      `this.plugins.Select(plugin => plugin.Copy()).ToArray()`; update `IPluginCatalog` XML docs:
      reads return detached copies (mutating a returned plugin does not affect the catalog), and
      plugin ids are compared with ordinal equality for duplicate detection at load time
- [x] 1.3 Core tests (`tests/CLIHub.Core.Tests/Plugins/PluginCatalogTests.cs`): copy semantics —
      mutating a returned plugin (`LogoPath`, `Commands.Update.Executable`,
      `Detection.SystemPaths`) changes neither later reads nor the live catalog; plugins returned
      before `ReloadPlugins()` keep their values; duplicate-id detection still resolves to the first
      plugin in deterministic order (pins the ordinal contract, see `design.md` Decision 4)
- [x] 1.4 Verify the consumer sweep compiles and behaves unchanged — all `GetAllPlugins()` call
      sites are read-only (`AgentProcessMonitor`, `ProjectScanningService`, `TrayStateProjection`,
      `AgentPaneController`, `AgentManagementViewModel`, legacy `MainWindow`); confirm existing
      shared-instance test mocks (e.g. `AgentManagementViewModelTests.CreateViewModel`) still pass
- [x] 1.5 Gate: `dotnet build CLIHub.sln` (no new warnings) and `dotnet test CLIHub.sln` green

## 2. Stage 2 — App: agent row fidelity and launch-window refresh

- [x] 2.1 `AgentItem` (`src/CLIHub/ViewModels/Agents/AgentItem.cs`): restructure `Update` toward a
      single private `Apply` path (keep the public `Update(Plugin, string, string?, bool)` signature
      and the `required init` accessors); `Apply` raises `CanLaunch`, `CanResume`, `CanUpdate`,
      `CanShowVersion` when the `plugin` reference changed; add the guard comment on
      `Apply`/`SetValue`: property names stay **explicit** — do not introduce a
      `[CallerMemberName]`-attributed helper here, it would raise `"Apply"`/`"Update"` as the
      property name and silently kill row notifications (see `design.md` Decision 2)
- [x] 2.2 `AgentItem.Status`: keep the property; add the comment — bound only by the legacy
      `MainWindow.xaml:118`, intentionally not wired into `Apply`, frozen at construction on
      launch-window rows (documented build-only exception)
- [x] 2.3 Tests (`tests/CLIHub.Tests/ViewModels/Agents/AgentItemTests.cs`): a plugin swap with a
      different declared command set raises all four `Can*` notifications; an update with unchanged
      values raises none; `Plugin`/`Name`/`LogoPath`/`IsAvailable`/`RowOpacity` notifications
      preserved
- [x] 2.4 `LaunchWindow.xaml.cs`: `ShowOnPointerMonitor` → `RefreshProjects()`, then
      `viewModel.RefreshAgents()`, then `ApplyDefaultAgentSelection()` (default-agent lookup must
      see fresh rows); `OnWindowActivated` → `RefreshProjects()` then `RefreshAgents()`; brief
      comment noting version re-population is cache-served within the probe TTL
- [x] 2.5 Gate: `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` green (including
      `AgentListSynchronizerTests`, `AgentPaneControllerTests` unchanged behavior)
- [x] 2.6 Manual smoke: install/uninstall an agent on the host, re-show and re-activate the launch
      window → the row set and dimmed/hidden states track the change; selected agent survives;
      default-agent preselection still applies on show

## 3. Stage 3 — App: card Update availability gating

- [x] 3.1 `AgentManagementViewModel`: inject `AgentProcessMonitor` (Core singleton — DI resolves
      with no registration changes; update `AgentManagementViewModelTests.CreateViewModel` to build
      a real `AgentProcessMonitor` over a mocked `IAgentProcessInspector`, a real
      `AgentProcessMatcher` with a null logger, and the test plugins)
- [x] 3.2 `Load()`: `card.CanUpdate = declaresUpdate && !processMonitor.HasRunningAgents(plugin)`;
      when blocked, set `card.Message` to the explaining text (reuse the service's phrasing
      "Update blocked while the agent is running"); do **not** add an execution-time double check in
      the view model — `AgentManagementService.UpdateAsync` already owns it
      (`preferences-ui` "Update blocked while running" stays green)
- [x] 3.3 Tests (`AgentManagementViewModelTests`): a matching running process makes the card's
      `CanUpdate` false and sets the explaining message; only other agents' processes running keeps
      `CanUpdate` true with no message; no processes → unchanged behavior; the existing
      update-flow tests (Blocked outcome → card message) still pass
- [x] 3.4 Gate: `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln` green
- [x] 3.5 Manual smoke: start an agent, open Settings → CLI Agents → its card's Update button is
      disabled with the explanation; stop the agent and reload the page → Update available again

## 4. Docs check and final validation

- [x] 4.1 Docs check (expected no-op): `docs/architecture/plugins.md` and
      `docs/architecture/processes.md` do not state catalog read semantics or card update
      availability — touch them only if verification shows otherwise; do not touch the legacy
      `MainWindow`
- [x] 4.2 Grep gates: `AgentItem.SetValue` call sites still pass explicit `nameof(...)` names and no
      `[CallerMemberName]` helper was added to that path; `IPluginCatalog`/`Plugin.Copy` XML docs
      state the copy contract
- [x] 4.3 Final `dotnet build CLIHub.sln` with no new warnings and `dotnet test CLIHub.sln` green
- [x] 4.4 `openspec validate --all` passes
- [x] 4.5 `graphify update .` refreshes the code graph
