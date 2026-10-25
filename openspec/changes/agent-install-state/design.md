## Context (verified current state)

All file:line references below were verified against the working tree at commit `bdd9770` on
2026-10-25.

### The symptom chain

- `AgentOption` is a positional record
  (`public sealed record AgentOption(string? Id, string Name, string? Version = null, bool IsInstalled = true)`,
  src/CLIHub/ViewModels/Agents/AgentCardItem.cs:14) whose display text is:

  ```csharp
  public string DisplayName =>
      AgentVersionText.IsPending(this.Version) ? this.Name : $"{this.Name} ({this.Version})";
  ```

  (src/CLIHub/ViewModels/Agents/AgentCardItem.cs:19-20). It never consults `IsInstalled`. When the
  version string is the literal `"unknown"`, `IsPending` is false, so the selector renders
  `OpenClaude (unknown)`.

- `"unknown"` is written by the settings view model after a probe that cannot produce a version:

  ```csharp
  card.Version = ver ?? AgentVersionText.Unknown;            // PopulateVersionsAsync, line 305
  card.Version = result.Version ?? AgentVersionText.Unknown; // TestAsync, line 398
  card.Version = result.Version ?? AgentVersionText.Unknown; // DetectAsync, line 421
  ```

  (src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:305,398,421). The launch pane and the
  legacy window do the same
  (src/CLIHub/ViewModels/Agents/AgentPaneController.cs:183,
  src/CLIHub/Views/MainWindow.xaml.cs:206 — the latter inlines the literal `"unknown"`).

- The sentinels live in `AgentVersionText` (src/CLIHub/Helpers/AgentVersionText.cs):
  `Pending = "…"` (line 12), `Unknown = "unknown"` (line 17), `IsPending` (lines 24-25),
  `IsKnown` (lines 32-33). `Load()` seeds every card with `AgentVersionText.Pending`
  (AgentManagementViewModel.cs:212) and `AgentItem.Version` defaults to it
  (src/CLIHub/ViewModels/Agents/AgentItem.cs:108), so `"…"` is rendered until a probe finishes
  (launch subtitle: src/CLIHub/Views/LaunchWindow.xaml:317 binds `Version` unconditionally).

- Consumers that branch on the sentinel rather than on state:
  `AgentCardItem.VersionChipText` (AgentCardItem.cs:110-111) via `HasKnownVersion`
  (AgentCardItem.cs:194-195), `AgentManagementViewModel.DefaultAgentDetail`
  (AgentManagementViewModel.cs:124).

### The wasted work

- `PopulateVersionsAsync` (src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:287-329) runs,
  for **every** card regardless of install state: `version.GetVersionAsync` (line 302) then
  `availability.CheckAsync` (line 310). For a not-installed agent the version command is a doomed
  process launch; the comment at lines 308-309 ("adds no extra process launch per card") is true
  only because the null result is cached (src/CLIHub.Core/Agents/AgentVersionService.cs:84,102) and
  the availability re-probe hits that cache.
- `AgentUpdateAvailabilityService.ComputeAvailabilityAsync` probes the installed version itself
  (src/CLIHub.Core/Agents/AgentUpdateAvailabilityService.cs:114-116) and degrades to
  `Unknown` **without a source request** when it cannot be parsed (lines 117-120). So today a
  genuinely missing CLI wastes the process launch, not a network request; a source request happens
  only in the edge case "detection markers missing but version command still emits a parseable
  version". The brief's "potentially making a network request" is accurate in that conditional
  sense.
- `AgentManagementService.ProbeAsync` (src/CLIHub.Core/Agents/AgentManagementService.cs:45-61)
  invalidates the three caches (lines 52-54), then runs detection (line 56) and —
  **unconditionally** — the version command (line 57). Nuance versus the brief: detection runs
  *first*, not after; the defect is that the version result is not consulted (the command runs even
  when `installed` is false).
- After a `ProbeAsync` on a not-installed agent, `TestAsync`/`DetectAsync` call
  `availability.CheckAsync` unconditionally (AgentManagementViewModel.cs:399,422); the probe just
  invalidated the version cache, so that check re-runs the doomed version command inside the
  availability service (AgentUpdateAvailabilityService.cs:114-116) before degrading to `Unknown`.

### The surfaces

- Card chip XAML: `AgentStateChip`/`AgentStateChipText` DataTriggers bind `IsInstalled` = False →
  danger styling (src/CLIHub/Views/SettingsAgentsPage.xaml:13-28); chip text binds
  `VersionChipText` (line 182).
- "Default agent" ComboBox: `ItemContainerStyle` DataTrigger binds `IsInstalled` = False →
  `IsEnabled=False` + muted foreground (src/CLIHub/Views/SettingsAgentsPage.xaml:78-87); item text
  binds `DisplayName` (line 90).
- Launch-window agent row: subtitle TextBlock binds `Version` with no visibility gate
  (src/CLIHub/Views/LaunchWindow.xaml:314-317).
- Legacy window row: `Text="{Binding Version, StringFormat='Version: {0}'}"`
  (src/CLIHub/Views/MainWindow.xaml:117) — a null version renders an empty `Version: ` strip.
- Converters already exist app-wide: `EmptyToCollapsedVisibility` (App.xaml:45;
  null/whitespace → `Collapsed`, src/CLIHub/Converters/EmptyToCollapsedVisibilityConverter.cs) and
  `BoolToVisibility` (App.xaml:47).
- Baseline runtime-selector wording (no longer the agent selector's precedent):
  `RuntimeOption.DisplayName`
  (src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:41) renders `{Name} (not installed)`;
  tests assert it (tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs:58,128).
  The agent selector deliberately diverges with a capitalized suffix — see Decision 4.
- `IsDetecting`/`CanDetect` already gate the Detect button
  (AgentManagementViewModel.cs:166-179); there is no per-card detecting state today.
- The launch list only ever contains installed agents (`AgentListComposer` filter, archived change
  `2026-10-03-hide-uninstalled-agents`), so `AgentPaneController.PopulateVersionsAsync`
  (AgentPaneController.cs:165-194) never sees a not-installed row in practice; the legacy
  `MainWindow.RefreshAgents` builds rows without the filter (MainWindow.xaml.cs:160-180).

### Spec state

- `openspec/specs/agent-version/spec.md:77-83` — requirement "Unknown version display" currently
  mandates a placeholder (`unknown`) for an unknown version; this change deliberately inverts it.
- `openspec/specs/agent-ui/spec.md:289-339` — requirement "Agent card displayed content" describes
  the chip as version-or-"Not installed" (no Detecting state).
- `openspec/specs/agent-update-detection/spec.md:31-34` — "No updateCheck block" is the only
  documented "no check happens" precondition; installation is not a gate there.
- `openspec/specs/agent-availability-display/spec.md:125-136` — "Agent row properties update in
  place" names "version-related properties" but says nothing about unresolved-version rendering.
- `openspec validate --all` passes at baseline (29 passed, 0 failed; pre-existing long-requirement
  warnings only).

## Goals / Non-Goals

**Goals:**

- One explicit, tri-state installation flag shared by the settings card, the default-agent
  selector, and the probes that feed them.
- Version = value-or-nothing; no sentinel text anywhere; no display text derived from version
  string state.
- No version process and no availability check for agents that are not installed; no stale update
  indicator after an uninstall.
- Launch-window subtitle shows a version only when resolved.

**Non-Goals:** as listed in proposal.md (no schema/detection/preference changes, no
`AgentUpdateAvailabilityService` changes, no launch-list hiding changes, no per-card progress
beyond the chip).

## Decisions

1. **`AgentInstallState` enum in the view-model layer, co-located with `AgentCardItem`.**
   `enum AgentInstallState { Detecting, Installed, NotInstalled }` goes in
   `src/CLIHub/ViewModels/Agents/AgentCardItem.cs` next to `AgentOption` (the file already hosts two
   types; Core stays untouched, which respects "CLIHub.Core has no UI-state dependency").
   `AgentCardItem.InstallState` is the observable source of truth; `IsInstalled` becomes
   `bool IsInstalled => this.InstallState == AgentInstallState.Installed;`. Setting `InstallState`
   raises `IsInstalled` and `VersionChipText` change notifications so existing bindings and tests
   keep working. `AgentOption`'s positional `bool IsInstalled = true` parameter becomes
   `AgentInstallState InstallState = AgentInstallState.Installed` with a derived `IsInstalled`
   computed property (records: computed properties are fine as long as they are not positional).
   - *Alternative rejected:* keep `bool` and encode "detecting" in the chip text only — recreates
     exactly the sentinel-in-a-string conflation this change removes.

2. **Version is `string?`; null means unresolved.** `AgentCardItem.Version` (constructor parameter
   and property) and `AgentItem.Version` become nullable, defaulting to null; `Load()` passes null
   instead of `AgentVersionText.Pending` (AgentManagementViewModel.cs:212);
   `PopulateVersionsAsync`, `TestAsync`, `DetectAsync`, `AgentPaneController.PopulateVersionsAsync`,
   and `MainWindow.PopulateVersionsAsync` write the raw result (`ver`, `result.Version`) with no
   `?? sentinel` fallback. `IsPending`/`IsKnown` and their only callers
   (AgentCardItem.cs:19-20,194-195; AgentManagementViewModel.cs:124) are replaced by null checks.
   - *Alternative rejected:* a sentinel enum on the version string — strictly worse than null, which
     the C# type system already communicates.

3. **`AgentVersionText` shrinks to a display-text owner.** It keeps the user-facing strings —
   `"Detecting…"`, `"Not installed"`, `"Installed"` (chip texts) — and may gain small formatting
   helpers if the implementer prefers (e.g. a chip-text formatter taking `(AgentInstallState,
   string?)`). It must own **no** parseable-state predicates (`IsPending`/`IsKnown` are deleted) and
   no `Unknown`/`Pending` constants. Rationale: the class's stated purpose ("sentinel literals live
   in exactly one place") inverts from state parsing to display text.

4. **Selector text derives from state; the suffix is capitalized.**
   `AgentOption.DisplayName` becomes a switch on `InstallState`: NotInstalled →
   `$"{Name} (Not installed)"` (revised decision, 2026-10-25: the suffix is capitalized,
   deliberately different from `RuntimeOption.DisplayName`'s lowercase `(not installed)` at
   src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:41, which keeps its style — the two
   selectors no longer match wording and that is intended); Installed with non-null `Version` →
   `$"{Name} ({Version})"`; otherwise → `Name`. The `NoneOption` (`new(null, "None")`,
   AgentManagementViewModel.cs:21) keeps the default `Installed` state and renders "None"
   unchanged. The ComboBox `ItemContainerStyle` DataTrigger (SettingsAgentsPage.xaml:81-84)
   changes from `{Binding IsInstalled}` Value=False to `{Binding InstallState}` Value=NotInstalled
   so a Detecting option (derived `IsInstalled` = false) stays selectable. WPF DataTrigger
   compares enum values by name, so `Value="NotInstalled"` binds without a converter.
   - *Alternative rejected:* keep binding `IsInstalled` and accept Detecting options being
     disabled — violates decision 3 of the brief ("a Detecting option must stay selectable").

5. **Chip text and styling.** `VersionChipText` becomes a switch: Detecting → `"Detecting…"`;
   NotInstalled → `"Not installed"`; Installed with version → `$"v{version}"`; Installed without →
   `"Installed"`. The chip DataTriggers (SettingsAgentsPage.xaml:15-18 and 24-27) change from
   `IsInstalled`=False to `InstallState`=NotInstalled, so "Not installed" keeps its danger styling
   and **"Detecting…" is styled by the chip's default neutral look** (the `VersionChipBorder` /
   `VersionChipText` base style — the same look as a resolved version chip; no new trigger, no
   accent, no danger). Rationale: Detecting is transient and informational; danger is reserved for
   the settled "missing" state.
   - Note: with the old `IsInstalled` trigger kept, a Detecting card would flash danger-red — this
     is why the trigger must move to `InstallState`.

6. **Honest Detecting transitions.**
   - `Load()` keeps settling `InstallState` synchronously from the cached
     `detection.IsInstalledInSystem` check (AgentManagementViewModel.cs:204) — Installed or
     NotInstalled, never a fake Detecting.
   - `DetectAsync` sets **every** card to Detecting at pass start (before the first probe) and
     settles each card as its probe returns; `IsDetecting` continues to gate the button.
   - `TestAsync` sets only its card to Detecting.
   - Per-card failure handling: before flipping to Detecting, capture the current `InstallState`;
     wrap the probe (and the follow-up availability check) in try/catch; on exception restore the
     captured state, leave `Version`/`UpdateAvailability` untouched, and let the existing logging
     pattern observe the failure (`DetectAsync` already has try/finally for `IsDetecting`;
     `TestAsync` gains its try/catch). No card may remain stuck in Detecting.
   - Counts/summary stay end-of-pass: `RefreshAvailabilitySummary()` runs after the pass
     (AgentManagementViewModel.cs:425) as today, so the mid-pass derived-`IsInstalled`=false never
     reaches `InstalledAgentCount`/`AvailabilitySummary` bindings
     (SettingsWindow.xaml:230,234) — no notification is raised for them mid-pass.

7. **State-gated version/availability work.**
   - `PopulateVersionsAsync`: for each card with `InstallState == NotInstalled`, skip
     `version.GetVersionAsync` and `availability.CheckAsync`, and set `card.UpdateAvailability =
     null` (clearing any stale "update available" chip); the generation guard and
     `RebuildDefaultAgentOptions` call are unchanged.
   - `DetectAsync`/`TestAsync` after `ProbeAsync`: when `result.IsInstalled` is false, set
     `InstallState = NotInstalled`, `Version = null`, `UpdateAvailability = null`, and **do not**
     call `availability.CheckAsync`. When true: `InstallState = Installed`, `Version =
     result.Version` (null stays null → chip "Installed"), then `availability.CheckAsync` as today.
   - `AgentManagementService.ProbeAsync` (Core): after the three `InvalidateFor` calls
     (AgentManagementService.cs:52-54 — they stay, so a stale "installed" cache can never survive a
     probe), if `detection.IsInstalledInSystem(plugin)` is false, return
     `new AgentProbeResult(false, null)` **without** calling `version.GetVersionAsync`, and log the
     same probe line. When installed, behavior is unchanged (version command runs).
   - `AgentUpdateAvailabilityService` is deliberately NOT changed: the call-site gate prevents the
     check for not-installed agents, and the service's existing degrade-to-Unknown path
     (AgentUpdateAvailabilityService.cs:107-120) remains the safety net for installed agents whose
     version cannot be probed. This is the agreed design, recorded here explicitly.

8. **Tests are updated to the new contract, never weakened.**
   - `tests/CLIHub.Tests/Helpers/AgentVersionTextTests.cs` — the `IsPending`/`IsKnown` sentinel
     theories are deleted with the API; replaced by tests over the display texts the class now
     owns.
   - `tests/CLIHub.Tests/ViewModels/Agents/AgentCardItemTests.cs` — chip tests become
     state-driven: NotInstalled → "Not installed"; Installed+version → `v{version}`;
     Installed+null → "Installed"; Detecting → "Detecting…". The `"…"`/`"unknown"`/`""` inline
     theories (lines 28-37) go away with the sentinels; notification tests extend to
     `InstallState`. Sentences are not deleted without replacement.
   - `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs` —
     `"…"` assertions (lines 224, 297) become null assertions; `Load_BuildsCardsCountsAndDefaultOptions`
     (line 135 `Assert.Equal("Version 1.0", ...)`) updates because agent "b" is NotInstalled and no
     longer gets a version probe (its `DefaultAgentDetail` falls back to "Preselected in the launch
     window"); new tests: gating (no `GetVersionAsync`/`CheckAsync` for a NotInstalled card, stale
     `UpdateAvailability` cleared), Detecting transitions and restore-on-throw, selector
     `DisplayName` matrix, option disabled only for NotInstalled.
   - `tests/CLIHub.Tests/ViewModels/Agents/AgentItemTests.cs` — asserts no `Version` behavior today
     (verified: no occurrence of `Version` in the file), so it needs **additions** only: nullable
     default (null) and change notification.
   - `tests/CLIHub.Tests/ViewModels/Agents/AgentPaneControllerTests.cs` — the `"…"` assertion at
     line 179 becomes a null assertion (found during verification; not listed in the brief).
   - `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs` —
     `ProbeAsync_UnknownVersion_ReturnsNullVersion` (line 237) already mocks detection=false and
     asserts `(false, null)`; it is **strengthened** to verify `version.GetVersionAsync` is never
     called (`Times.Never`). Add the counterpart: an installed agent still runs the version command.
     `DetectAll_LeavesEveryProbedAgentCached` (line 188) uses installed agents only — unaffected.

9. **`agent-version`'s "Unknown version display" is a deliberate contract change.** The
   MODIFIED requirement keeps the header and intent ("what the UI does when the version is
   unknown") but inverts the SHALL: no placeholder text; an unresolved version renders no version
   text. The scenario rename the brief anticipated ("Unknown shown" → a name matching the inverted
   behavior) is **not possible**: `openspec validate` refuses a MODIFIED block that omits an
   existing scenario name (`MODIFIED "Unknown version display" omits scenario(s) the current spec
   still has: "Unknown shown"` — validator output, 2026-10-25). The scenario name "Unknown shown"
   is therefore preserved byte-identical with an inverted body, and the inversion is flagged with an
   HTML comment inside the delta itself; the name now reads as "the unknown state is shown (by the
   absence of any version text)". This is recorded as Open Question 1 for reviewer sign-off. All
   other MODIFIED requirements keep their original scenario names and add scenarios alongside.

10. **Launch-window and legacy rendering.**
    - `AgentPaneController.cs:183` → `item.Version = version;` (null stays unresolved).
    - `MainWindow.xaml.cs:206` → `item.Version = version;` (same; legacy window keeps probing every
      plugin — its list is unfiltered — but no longer renders a lie; bringing the composer filter
      into the legacy window is out of scope).
    - `LaunchWindow.xaml:314-317`: the subtitle TextBlock gains
      `Visibility="{Binding Version, Converter={StaticResource EmptyToCollapsedVisibility}}"`.
      The row has a fixed height (`Sizing.AgentRow.Height`, line 289), so collapsing the subtitle
      causes no layout jump.
    - `MainWindow.xaml:117`: the same converter on the legacy subtitle so an empty version renders
      nothing rather than `Version: `.

11. **`TestAsync` message without a version.** Today a successful probe renders
    `$"Installed · version {card.Version}"` (AgentManagementViewModel.cs:401); with a null version
    that would read `Installed · version `. New: `result.IsInstalled` with a resolved version →
    `Installed · version {v}`; installed without → `Installed`; not installed →
    `Not installed on the host` (unchanged). Not specified in the brief; chosen for the same
    "display text must not lie" principle.

## Risks / Trade-offs

- **Derived `IsInstalled` is false while Detecting.** Enumerated consumers: the counts/summary
  (refresh only at pass end — no mid-pass notification, verified `RefreshAvailabilitySummary` call
  sites at AgentManagementViewModel.cs:221,403,425); the XAML triggers (moved to `InstallState` in
  this change); tests (updated). Residual risk: a future binding to `IsInstalled` mid-pass — the
  XML docs on `InstallState` should say "prefer binding `InstallState`".
- **WPF enum DataTrigger matches by name.** `Value="NotInstalled"` requires the enum member name to
  stay stable; renaming an enum member becomes a XAML-breaking change. Accepted — same as any enum
  bound in XAML.
- **`RebuildDefaultAgentOptions` replaces the options list after each pass**
  (AgentManagementViewModel.cs:319,404,426), so a Detecting-phase rebuild would show plain names —
  mitigated because Detecting options also render `{Name}` (no visible difference), and the final
  rebuild happens at pass end anyway.
- **`ProbeAsync` no longer runs the version command for not-installed agents**, so a hypothetical
  caller that used `ProbeAsync` to *test* a version command on a machine where detection markers
  are stale loses that signal. No such caller exists (`ProbeAsync` is called only from
  `TestAsync`/`DetectAsync`, verified via grep), and the app-level "Test" action is about host
  installation first.
- **Baseline spec warnings** (long requirement texts) grow slightly for the MODIFIED card
  requirement; `openspec validate` reports them as warnings only, as it already does today.

## Deviations from the brief

- None behavioral. Two precision corrections, recorded so the implementer is not confused by the
  brief's wording:
  1. `AgentManagementService.ProbeAsync` runs detection **first** and the version command after
     (AgentManagementService.cs:56-57); the defect is the missing gate, not ordering.
  2. For a genuinely missing CLI the current availability path makes **no** network request — it
     degrades to Unknown at AgentUpdateAvailabilityService.cs:117-120 after the doomed version
     probe. The guaranteed waste is the process launch; the network request is the
     markers-missing-but-CLI-present edge case. The brief's "potentially" is accurate.

## Open Questions

1. **Inverted scenario name sign-off.** `agent-version`'s scenario "Unknown shown" keeps its
   original name but its body is inverted (no placeholder is rendered). The rename the brief
   anticipated is impossible: `openspec validate` rejects a MODIFIED block that drops or renames an
   existing scenario (error quoted in Decision 9). The reviewer should confirm they accept the
   preserved name with the inverted body and the in-delta comment; if they insist on a matching
   name, the requirement would have to be REMOVED and re-ADDED, losing scenario continuity.
   **Resolved (orchestrator, 2026-10-25): accepted.** The validator constraint is authoritative;
   keeping the scenario name with the inverted body plus the in-delta HTML comment is the only
   option that preserves scenario continuity, and the archive step will apply it as-is.
2. **Chip text ownership.** Decision 3 of the brief allows either keeping `AgentVersionText` as a
   constants owner or moving full chip formatting into it. The plan prefers minimal constants
   (chip texts) with the switch living on `AgentCardItem`, because `VersionChipText` already lives
   there and tests target it. If the implementer prefers a
   `AgentVersionText.ChipText(AgentInstallState, string?)` formatter, that is an acceptable
   equivalent — tests should then target the formatter.
3. **`Message` on probe failure.** Decision 6 restores state and logs; it does not set a card
   `Message`. Should a failed Test probe surface "Detection failed" on the card? Left open —
   current behavior (exception propagates to `RunAsync`, no card message) is preserved unless the
   user asks otherwise.
