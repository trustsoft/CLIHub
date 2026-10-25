## 1. Core probe gate (independent, ships alone)

- [ ] 1.1 In `src/CLIHub.Core/Agents/AgentManagementService.cs` `ProbeAsync`, after the three `InvalidateFor` calls and the `IsInstalledInSystem` check, return `new AgentProbeResult(false, null)` without calling `version.GetVersionAsync` when the agent is not installed; keep the existing probe log line; installed agents keep the version probe
- [ ] 1.2 In `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs`, strengthen `ProbeAsync_UnknownVersion_ReturnsNullVersion` (line 237) to also verify `version.GetVersionAsync` is never called (`Times.Never`), and add a counterpart test that an installed agent still runs the version command and returns its version
- [ ] 1.3 Gate: run `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, and `openspec validate --all`; all three must pass before moving on

## 2. Install-state model and sentinel removal (atomic — keeps the solution compiling)

- [ ] 2.1 In `src/CLIHub/ViewModels/Agents/AgentCardItem.cs`: add `enum AgentInstallState { Detecting, Installed, NotInstalled }`; give `AgentCardItem` an observable `InstallState` (raising `IsInstalled` and `VersionChipText` notifications), make `IsInstalled` the derived `InstallState == Installed`, make `Version` nullable (constructor parameter and property, default null), and rewrite `VersionChipText` as the state switch (Detecting → "Detecting…", NotInstalled → "Not installed", Installed+version → `v{version}`, Installed without → "Installed") with XML docs
- [ ] 2.2 In the same file, change `AgentOption` to carry `AgentInstallState InstallState = AgentInstallState.Installed` instead of the positional `bool IsInstalled`, add the derived `IsInstalled`, and rewrite `DisplayName` as the state switch (NotInstalled → `{Name} (not installed)`, Installed+version → `{Name} ({version})`, otherwise `{Name}`)
- [ ] 2.3 Shrink `src/CLIHub/Helpers/AgentVersionText.cs` to a display-text owner: keep/add the chip texts ("Detecting…", "Not installed", "Installed"), delete `Pending`, `Unknown`, `IsPending`, and `IsKnown`; update its XML docs to say display text, not parseable state
- [ ] 2.4 In `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`: `Load()` settles `InstallState` from the cached detection check and passes a null version instead of `AgentVersionText.Pending`; `RebuildDefaultAgentOptions` passes `card.InstallState`; `PopulateVersionsAsync`, `TestAsync`, and `DetectAsync` write the raw version result with no `?? AgentVersionText.Unknown` fallback; `DefaultAgentDetail` uses a null check instead of `IsPending`; `TestAsync`'s message reads `Installed · version {v}` only when a version resolved, otherwise `Installed`
- [ ] 2.5 In `src/CLIHub/ViewModels/Agents/AgentItem.cs`: make `Version` nullable, defaulting to null instead of `AgentVersionText.Pending`
- [ ] 2.6 In `src/CLIHub/ViewModels/Agents/AgentPaneController.cs` (line 183) and `src/CLIHub/Views/MainWindow.xaml.cs` (line 206): write the raw probe result (`item.Version = version;`), no `"unknown"` fallback
- [ ] 2.7 Update `tests/CLIHub.Tests/Helpers/AgentVersionTextTests.cs` to the new display-text contract (sentinel-parsing theories deleted with the API, replaced by display-text coverage)
- [ ] 2.8 Update `tests/CLIHub.Tests/ViewModels/Agents/AgentCardItemTests.cs`: state-driven chip matrix (Detecting / NotInstalled / Installed+version / Installed+null), notification tests for `InstallState`, remove the `"…"`/`"unknown"`/`""` inline theories; add `AgentOption.DisplayName` matrix tests (installed+version, installed without, not installed, detecting)
- [ ] 2.9 Update `tests/CLIHub.Tests/ViewModels/AgentManagementViewModelTests.cs`: `"…"` assertions (lines 224, 297) become null assertions; `Load_BuildsCardsCountsAndDefaultOptions` expects the NotInstalled default agent's detail to fall back to "Preselected in the launch window"; option assertions move to `InstallState`/derived `IsInstalled`
- [ ] 2.10 Add to `tests/CLIHub.Tests/ViewModels/Agents/AgentItemTests.cs`: `Version` defaults to null and announces changes (the file currently asserts no version behavior)
- [ ] 2.11 Update `tests/CLIHub.Tests/ViewModels/Agents/AgentPaneControllerTests.cs`: the `"…"` assertion at line 179 becomes a null assertion
- [ ] 2.12 Gate: run `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, and `openspec validate --all`; all three must pass before moving on (the two XAML trigger updates deliberately land in stage 4, so the old `IsInstalled` triggers keep compiling against the derived property)

## 3. Gating and Detecting dynamics (settings view model)

- [ ] 3.1 In `PopulateVersionsAsync` (`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:287-329`): a card whose `InstallState` is NotInstalled gets no `GetVersionAsync` and no `CheckAsync` call, and its `UpdateAvailability` is set to null so a stale "update available" indicator cannot survive an uninstall; installed/detecting cards keep both calls under the existing generation guard
- [ ] 3.2 In `DetectAsync`: set every card to Detecting at pass start; per card, capture the prior `InstallState`, probe, and on success apply Installed/NotInstalled with the raw version; when the probe reports NotInstalled set `Version = null`, clear `UpdateAvailability`, and skip `availability.CheckAsync`; on a thrown probe restore the captured state and leave version/availability untouched; keep the existing `IsDetecting`/`CanDetect` handling and end-of-pass summary/option rebuild
- [ ] 3.3 In `TestAsync`: set the card to Detecting, apply the same settle/gate/clear rules as `DetectAsync`, restore the prior state if the probe throws, and keep the message rules from task 2.4
- [ ] 3.4 Add `AgentManagementViewModelTests` coverage: NotInstalled card triggers no version or availability calls and clears a stale `UpdateAvailability`; Detect pass shows Detecting then settles (including NotInstalled with null version); Test on a not-installed agent sets NotInstalled/null/cleared availability without an availability call; a throwing probe leaves the card in its previous state (no stuck Detecting)
- [ ] 3.5 Gate: run `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, and `openspec validate --all`; all three must pass before moving on

## 4. XAML: state-keyed triggers and collapsing subtitles

- [ ] 4.1 In `src/CLIHub/Views/SettingsAgentsPage.xaml`: change the `AgentStateChip` and `AgentStateChipText` DataTriggers (lines 15-18, 24-27) from `{Binding IsInstalled}` Value=False to `{Binding InstallState}` Value=NotInstalled so only "Not installed" is danger-styled and "Detecting…" keeps the neutral chip look
- [ ] 4.2 In the same file, change the "Default agent" ComboBox `ItemContainerStyle` DataTrigger (lines 81-84) from `{Binding IsInstalled}` Value=False to `{Binding InstallState}` Value=NotInstalled so only not-installed options are disabled and a Detecting option stays selectable
- [ ] 4.3 In `src/CLIHub/Views/LaunchWindow.xaml` (agent-row subtitle, line 314-317): add `Visibility="{Binding Version, Converter={StaticResource EmptyToCollapsedVisibility}}"` so the subtitle collapses when no version is resolved
- [ ] 4.4 In `src/CLIHub/Views/MainWindow.xaml` (legacy row, line 117): add the same `EmptyToCollapsedVisibility` visibility binding so an empty version never renders a `Version: ` strip
- [ ] 4.5 Gate: run `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, and `openspec validate --all`; all three must pass before moving on

## 5. Final verification

- [ ] 5.1 Re-run the full gate set from the repository root: `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, `openspec validate --all`; confirm zero test failures and a clean validation
- [ ] 5.2 Run `node .opencode/scripts/check-model-policy.mjs` and confirm it passes
- [ ] 5.3 Run `graphify update .` so the knowledge graph reflects the source changes
- [ ] 5.4 Update the `docs/backlog.md` entry "Magic version sentinels (resolved)" (lines 120-133) so it no longer describes the deleted sentinel model: record that `agent-install-state` replaced `Pending`/`Unknown`/`IsPending`/`IsKnown` with `AgentInstallState` plus a nullable version, and point its guard at the new state-driven tests (`AgentCardItemTests`, `AgentManagementViewModelTests`, `AgentManagementServiceTests`). Leave `docs/architecture/ui.md` alone: its "placeholder" mentions are the settings numeric-field placeholder, not the version sentinel (verified at ui.md:85-92)

## 6. Manual smoke (blocks commit/archive)

- [ ] 6.1 Run the app (`dotnet run --project src/CLIHub/CLIHub.csproj`) with at least one agent absent from the host, open Settings → CLI Agents, and verify: the absent agent's card chip reads "Not installed"; the "Default agent" ComboBox shows `{Name} (not installed)` for it and the item is disabled; pressing Detect shows "Detecting…" transiently on the cards and every card settles (none stuck); the launch window's agent rows show a version subtitle only for agents with a resolved version — no "unknown" and no "…" anywhere
- [ ] 6.2 Record the smoke result in the change (update this tasks file), report back, and only then ask the user for `git commit` and `openspec archive` approval — per repo policy both MUST wait for the confirmed smoke result
