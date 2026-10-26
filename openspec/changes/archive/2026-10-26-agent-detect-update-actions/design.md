## Context (verified current state)

All file:line references below were verified against the working tree at commit `9d808b1` on
2026-10-26.

### The combined action today

- The AGENTS section header is a `Grid` holding `controls:SectionHeader Text="AGENTS"` and exactly
  one chip button (src/CLIHub/Views/SettingsAgentsPage.xaml:121-142): `Detect`, glyph
  `IconGlyphs.Refresh` (line 135), `AutomationProperties.Name="Detect installed agents and updates"`
  (line 126), `ToolTip="Re-detect installed agents, versions, and available updates"` (line 130),
  `Command="{Binding AgentsState.DetectCommand}"` with `IsEnabled="{Binding AgentsState.CanDetect}"`
  (lines 127-128), styled `AgentsHeaderChipButton` (style defined at lines 41-66, based on
  `IconChipButton`, src/CLIHub/Themes/Controls.xaml:102).
- `DetectCommand` is `new RelayCommand(() => _ = this.RunAsync("Agent detection", this.DetectAsync),
  () => this.CanDetect)` (src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:74).
  `IsDetecting`/`CanDetect` are a single-flag gate
  (AgentManagementViewModel.cs:165-178): `CanDetect => !this.IsDetecting`, and the setter raises
  `CanDetect` on change.
- `DetectAsync` (AgentManagementViewModel.cs:431-478) sets `IsDetecting`, flips every card to
  `Detecting`, probes each card via `management.ProbeAsync`, settles each card through
  `ApplyProbeResultAsync`, and restores pre-pass states if the pass throws.
- `ApplyProbeResultAsync` (AgentManagementViewModel.cs:490-504) is where the actions are combined:
  for an installed agent it settles `InstallState`/`Version` **and** awaits
  `this.availability.CheckAsync(card.Plugin, cancellationToken)` (line 496), assigning
  `card.UpdateAvailability`. Its only callers are `TestAsync` (line 412) and `DetectAsync`
  (line 456) — both are "detect" actions, so after this change **no caller needs the availability
  branch**.
- The per-card first button (src/CLIHub/Views/SettingsAgentsPage.xaml:210-220):
  `AutomationProperties.Name="Test agent"` (line 212), `ToolTip="Detect installation and version"`
  (line 216), glyph `IconGlyphs.Diagnostic` (line 219), bound to `AgentsState.TestCommand`
  (line 213), **no `IsEnabled` binding** — always clickable today. The neighboring Update button
  uses `IconGlyphs.Update` (lines 221-232, glyph at line 231).
- `TestAsync` (AgentManagementViewModel.cs:405-429) mirrors `DetectAsync` for one card: capture
  previous state → `Detecting` → probe → settle via `ApplyProbeResultAsync` (so it also checks
  availability today) → card message (`Installed · version {v}` / `Installed` / `Not installed on
  the host`, lines 413-418) → on throw, restore the captured state (lines 422-428).

### The services the actions sit on

- `AgentManagementService.ProbeAsync` (src/CLIHub.Core/Agents/AgentManagementService.cs:45-70):
  invalidates the three per-agent caches (detection, version, availability — lines 52-54), runs
  detection (line 56), short-circuits to `new AgentProbeResult(false, null)` without a version
  command when not installed (lines 60-64), otherwise awaits `version.GetVersionAsync`
  (line 66). It already is exactly the "detection + version probe" this change needs — no Core
  change required.
- `IAgentUpdateAvailabilityService` (src/CLIHub.Core/Agents/IAgentUpdateAvailabilityService.cs:20,32)
  exposes `CheckAsync(plugin, ct)` and `InvalidateFor(plugin)`.
  `AgentUpdateAvailabilityService.CheckAsync` serves a cached result within the probe TTL
  (src/CLIHub.Core/Agents/AgentUpdateAvailabilityService.cs:48-56); `InvalidateFor` removes only the
  completed cache entry for that agent (lines 86-90, in-flight checks untouched). Inside a check,
  the service resolves the installed version through `versionService.GetVersionAsync`
  (lines 114-116) — cache-served while the version cache is valid.
- `UpdateAsync` already demonstrates the call-site invalidation pattern this change reuses:
  `this.availability.InvalidateFor(card.Plugin)` after a started update
  (AgentManagementViewModel.cs:516-521).

### Load-time population (stays as is)

- `Load(defaultAgentId)` (AgentManagementViewModel.cs:181-229) builds cards from the cached
  detection check, then fires `PopulateBlockedState()` and `PopulateVersions()`.
- `PopulateVersionsAsync` (AgentManagementViewModel.cs:286-340) skips `NotInstalled` cards
  (clearing their `UpdateAvailability`, line 307) and for the rest awaits
  `version.GetVersionAsync` (line 313) then `availability.CheckAsync` (line 321) under the
  generation guard. Decision 5 of the brief keeps this untouched.

### Card model and UI vocabulary

- `AgentInstallState { Detecting, Installed, NotInstalled }` and the observable card state live in
  src/CLIHub/ViewModels/Agents/AgentCardItem.cs (`InstallState` lines 131-142, derived `IsInstalled`
  line 148, `VersionChipText` lines 155-161, nullable `Version` lines 168-178, `UpdateAvailability`
  lines 193-204, `HasUpdateAvailable` line 219).
- Glyphs (src/CLIHub/Themes/IconGlyphs.cs): `Refresh` line 13, `Update` line 15 (`\uE896`), `Diagnostic`
  line 23, `Sync` line 33 (`\uE895`). `IconGlyphs.Sync` is already used as a SettingCard icon on the
  Updates settings page (src/CLIHub/Views/SettingsUpdatesPage.xaml:14), so the glyph is a known
  quantity in this font. Note for the smoke: `\uE895` (Sync) and `\uE896` (Update) are adjacent
  circular-arrow glyphs in Segoe MDL2 Assets — visually similar. They are distinct code points and
  the header button carries a text label ("Check updates") while the per-card Update is a bare icon
  on a card; the brief explicitly offers `IconGlyphs.Sync` as the candidate.
- `controls:SpacedPanel` exists with `Orientation`/`Spacing` (src/CLIHub/Controls/SpacedPanel.cs:14-75);
  the card action row already uses `<controls:SpacedPanel … Orientation="Horizontal" Spacing="2">`
  (SettingsAgentsPage.xaml:206-209).
- Muted text style: `HintText` (src/CLIHub/Themes/SettingsControls.xaml:25-29 — Section font size,
  `TextMutedBrush`, wrap) is used on this very page (SettingsAgentsPage.xaml:195).
  `EmptyToCollapsedVisibility` (null/whitespace → Collapsed) is app-wide
  (src/CLIHub/App.xaml:45) with in-repo binding precedents (src/CLIHub/Views/LaunchWindow.xaml:318).
- Commands are `RelayCommand`/`RelayCommand<T>` with optional `canExecute` predicates whose
  `CanExecuteChanged` rides `CommandManager.RequerySuggested`
  (src/CLIHub/ViewModels/Mvvm/RelayCommand.cs:18,25-29; RelayCommandOfT.cs:19,25-29). The header
  button therefore uses **both** a `canExecute` lambda and an explicit `IsEnabled` binding today
  (AgentManagementViewModel.cs:74 + SettingsAgentsPage.xaml:128) — the established pattern this
  change mirrors. **The smoke falsified this assumption**: WPF caches each button's `CanExecute`
  result and re-queries it only on `CanExecuteChanged`, which the flag setters never triggered, so
  the buttons stayed disabled until page navigation raised `RequerySuggested` again. Fixed in
  Decision 6 (amendment) and covered by tasks.md stage 3b.
- Layout width: the Settings window is `Width="888" MinWidth="640"` with a 220px nav column
  (src/CLIHub/Views/SettingsWindow.xaml:16-17, 325). At the default width the AGENTS row has room
  for the header, two chip buttons, and a ~200px muted summary; at `MinWidth` the row will be tight
  (checked in the manual smoke, tasks.md stage 4).

### Section interface

- `IAgentManagementSection` (src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs) declares
  `TestCommand` (line 73), `DetectCommand` (line 88), `IsDetecting` (line 93), `CanDetect`
  (line 98), `Load` (line 104). New members are additions; the per-card command rename (Decision 7)
  renames the line-73 member.

### Spec state

- `openspec/specs/agent-ui/spec.md:219-287` — requirement "Agent card action availability" contains
  the scenario `Test always available` (lines 223-226: WHEN an agent card is shown THEN "Test" is
  available). The requirement body (line 221) is entirely about Update availability, blocked-state
  population, and process matching; "Test" appears only in that one scenario.
- `openspec/specs/agent-ui/spec.md:289-355` — "Agent card displayed content": the "Detecting state
  shown" scenario (lines 310-314) says "WHEN a detection or test probe is running" — vocabulary,
  not behavior (see Deviations).
- `openspec/specs/agent-update-detection/spec.md:131-162` — "Availability results are cached per
  agent": cached within TTL, refreshed after the interval, **scoped invalidation** (lines 149-152),
  coalesced concurrent checks, Unknown cached.
- `openspec/specs/agent-update-detection/spec.md:180-199` — "Update checks are gated on host
  installation": no check for a not-installed agent, probe clearing the advisory result, installed
  agents still checked. The gate is documented as the caller's (line 182).
- `openspec/specs/agent-version/spec.md` — "Version lookup" (lines 9-32, no version command for a
  not-installed agent) and "On-demand version re-probe" (lines 129-155) already cover what
  `ProbeAsync` does; no delta needed.
- `openspec/specs/agent-availability-display/spec.md` — launch-window dimming/subtitle rules;
  untouched by this change.
- `openspec validate --all` at baseline: **29 passed, 0 failed** (pre-existing long-requirement
  warnings only; re-run at baseline before writing the deltas, 2026-10-26).

### Tests asserting the old combined behavior

`tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`:

- `DetectCommand_WhileProbing_ShowsDetectingThenSettlesEachCard` (lines 803-854) — asserts the
  Detect settle **adds** an availability check for an installed card:
  `availability.Verify(x => x.CheckAsync(a, …), Times.Exactly(2))` (line 853; Load + Detect settle)
  and `CheckAsync(b, …), Times.Once` (line 852). Under the new contract the Detect pass adds no
  check, so `a` becomes `Times.Once` and the surrounding comments change.
- `TestCommand_ProbeUpdatesCardAndAvailability` (lines 392-408) — name encodes the combined
  behavior; the body asserts installation/version/summary only. It is renamed and extended to
  assert the per-card Detect adds no availability check and leaves `UpdateAvailability` untouched.
- `TestCommand_WhenProbeReportsNotInstalled_ClearsStaleAvailabilityWithoutCheck` (857-885),
  `TestCommand_WhenProbeThrows_RestoresPreviousCardState` (888-915),
  `DetectCommand_WhenProbeThrows_RestoresEveryCardToPreviousState` (918-949) — already match the
  new contract (restore-on-throw, not-installed clearing); they follow the command rename and gain
  no behavioral change.
- `DetectCommand_UpdatesCardsSummaryAndOptionAvailability` (748-771) — assertions remain valid
  (installation/version/summary/options); it is extended to verify no `CheckAsync` came from the
  Detect pass.

## Goals / Non-Goals

**Goals:** two honest header actions (local installation+version check; forced update check) with
explicit tooltips/automation names; per-card action named Detect with the same local semantics;
mutual exclusivity with visible busy state; a one-line result summary for the manual update check;
spec deltas that match; tests updated to the new contract, never weakened.

**Non-Goals:** as listed in proposal.md (no load-time change, no TTL change, no descriptor change,
no new preference, no Update-action/blocked-state change, no availability-service change, no batch
progress UI).

## Decisions

1. **Header layout: right-aligned horizontal `SpacedPanel` replacing the single button.** The
   `Grid` (SettingsAgentsPage.xaml:121-142) keeps `SectionHeader` in the left cell; the right side
   becomes `<controls:SpacedPanel Orientation="Horizontal" Spacing="8" Margin="0,18,0,8"
   VerticalAlignment="Center">` containing, in order: the muted summary `TextBlock`, the `Detect`
   button, the `Check updates` button. The panel inherits the button's current margin
   (`0,18,0,8`); the buttons drop their own margins. `SpacedPanel` is chosen over a `StackPanel`
   because it is the repo's established spacing primitive (spaced-panel-layout capability; card
   row already uses it) and avoids per-child margins.
   - *Alternative rejected:* `DockPanel` — no uniform spacing; per-child margins recreate the
     problem `SpacedPanel` solves.

2. **Glyphs: `Detect` keeps `Refresh`; `Check updates` uses `IconGlyphs.Sync`.** Sync
   (IconGlyphs.cs:33, `\uE895`) is distinct from the per-card Update icon (IconGlyphs.cs:15,
   `\uE896`) and already has an in-repo precedent (SettingsUpdatesPage.xaml:14). Both header
   buttons carry text labels, so the visual adjacency of the two circular-arrow glyphs is not
   disambiguating-relevant; flagged for the manual smoke (tasks.md stage 4) anyway.

3. **Tooltips and automation names state the split explicitly.**
   - Header Detect: `AutomationProperties.Name="Detect installed agents and versions"`,
     `ToolTip="Re-detect installed agents and their versions locally; does not check for updates"`.
   - Header Check updates: `AutomationProperties.Name="Check for agent updates"`,
     `ToolTip="Force a fresh update-availability check for installed agents; does not re-detect
     installation or version"` (single line in the actual XAML).
   - Per-card Detect: `AutomationProperties.Name="Detect agent"`,
     `ToolTip="Detect installation and version; does not check for updates"`.
   Each states its own work and that the other work is not performed — decision 1 of the brief
   verbatim.

4. **`ApplyProbeResultAsync` loses its availability branch and becomes a synchronous settle.**
   After this change no caller wants a settle-time availability check (`PopulateVersionsAsync`
   does its own). The settle becomes: installed → `InstallState = Installed`, `Version =
   result.Version` (raw, null stays null), `UpdateAvailability` **untouched**; not installed →
   `InstallState = NotInstalled`, `Version = null`, `UpdateAvailability = null` (the durable
   "missing installation shows no update indicator" rule, agent-ui spec lines 341-349). Keeping the
   method async with no awaits would trip style expectations; the implementer may keep or drop the
   `Async` suffix — the semantic contract is what matters.
   - Nuance recorded: `ProbeAsync` also invalidates the **availability cache**
     (AgentManagementService.cs:54). That stays: invalidating a cache is not checking it. The card
     keeps its last displayed `UpdateAvailability`, and the *next* availability check (manual
     `Check updates`, or a page reload's population pass) fetches fresh. This satisfies "the
     existing `UpdateAvailability` is left untouched (it was not re-checked)".

5. **`CheckUpdatesAsync` shape.** Mirrors `DetectAsync`'s structure: set `IsCheckingUpdates`;
   clear `UpdateCheckSummary` (decision 7 of the brief: cleared when a new check starts); snapshot
   the cards; settle each card in a `Task.WhenAll` pass (the `PopulateVersionsAsync` shape) where:
   `InstallState == Installed` → `availability.InvalidateFor(card.Plugin)` then `await
   availability.CheckAsync(card.Plugin, ct)` then assign `card.UpdateAvailability` (assign only
   when `!this.disposed`); `NotInstalled` → `card.UpdateAvailability = null`, no check. No
   `management.ProbeAsync` call and no direct `version.GetVersionAsync` call — "no version command
   and no detection re-run at the call site". Precision: the availability service internally
   resolves the installed version through the version service (AgentUpdateAvailabilityService.cs:114-116);
   while the version cache is valid that is cache-served, but **after a header Detect pass
   (which invalidates each probed agent's version cache) a subsequent `Check updates` will run
   each installed agent's version command inside the availability service**. That is existing
   service behavior and explicitly out of scope to change (brief decision 8 + non-goal); the spec
   wording says "at the call site" for exactly this reason.
   - *Alternative rejected:* adding a `force` parameter to `CheckAsync` — rejected by the brief
     ("prefer `InvalidateFor` + `CheckAsync` at the call site over changing
     `AgentUpdateAvailabilityService`") and by the non-goal; `InvalidateFor` already has the
     documented scoped-invalidation contract (agent-update-detection spec lines 149-152).

6. **Busy-state rule (chosen, per the brief's invitation).**
   - Two flags: `IsDetecting` (existing) and `IsCheckingUpdates` (new); each is set for the
     duration of its own action only.
   - `CanDetect => !IsDetecting && !IsCheckingUpdates` and `CanCheckUpdates =>
     !IsDetecting && !IsCheckingUpdates` — the two header actions can never run concurrently, and
     each button's `IsEnabled` binding (plus the commands' `canExecute` lambdas, mirroring the
     existing double gate at AgentManagementViewModel.cs:74 + SettingsAgentsPage.xaml:128) reports
     the busy state to the UI. The flag setters raise both `CanDetect` and `CanCheckUpdates`
     change notifications.
   - **Per-card Detect is disabled while either header action runs**: the card button gains
     `IsEnabled` bound to `AgentsState.CanDetect` (same predicate, `RelativeSource` up to the
     UserControl like the Command binding at SettingsAgentsPage.xaml:213) and the command gains a
     defensive `canExecute: _ => this.CanDetect` (RelayCommandOfT.cs:19) so a missed requery can
     only disable, never enable, the wrong action.
   - **Re-query on every busy flip (smoke-found fix).** `RelayCommand`/`RelayCommand<T>` keep the
     `CommandManager.RequerySuggested` subscription for every existing caller and additionally hold
     explicit subscribers; `RaiseCanExecuteChanged()` raises only those. `RaiseBusyStateChanges()`,
     called from both flag setters, raises the `CanDetect`/`CanCheckUpdates` notifications and calls
     `RaiseCanExecuteChanged()` on `DetectCommand`, `CheckUpdatesCommand`, and `DetectCardCommand`,
     so WPF re-queries the cached `CanExecute` at pass start and at pass end — including the
     exception path, which still runs through `finally`. Guarded by the two raise-count tests and
     the `agent-ui` scenario "Header actions become available again when a pass completes".
   - **A per-card Detect does not block header actions** (unchanged from today, where `TestCommand`
     has no gating at all): header Detect during a per-card probe stays possible. Interleaving a
     per-card probe with a header pass can make the header pass's restore-on-throw snapshot
     overwrite a per-card settle — a pre-existing hazard shape (documented in
     docs/backlog.md:142-158), unchanged by this change and not made more likely.
   - Rationale: this is the simplest rule that satisfies "must not run concurrently" without
     inventing a per-card busy registry; header actions are the heavyweight ones and get
     exclusivity.
   - *Alternative rejected:* one shared `IsBusy` flag — loses "both commands report their busy
     state to the UI" (the UI could not tell which action is running).

7. **Per-card command member renamed with the action: `TestCommand` → `DetectCardCommand`,
   `TestAsync` → `DetectCardAsync`** (and the `IAgentManagementSection` member +
   XAML binding + test names follow). The brief mandates the *UI* rename; the member rename is
   this design's addition. Rationale: leaving `TestCommand` behind a button labeled Detect
   recreates the vocabulary lie this change fixes, and `DetectCardCommand` cannot collide with the
   header `DetectCommand`. The rename is mechanical (one interface member, one VM member, one XAML
   binding, test names).
   - *Alternative rejected:* keep `TestCommand` internally — smaller diff, permanently misleading
     name.

8. **Summary line wording (decision 7 of the brief, exact strings).**
   - Counted checks = cards whose `InstallState` was `Installed` when the pass started; updates
     available = cards with `HasUpdateAvailable` after the pass.
   - `n ≥ 1, m = 0` → `Checked {n} agents · no updates`; `n = 1` → `Checked 1 agent · no updates`.
   - `m = 1` → `Checked {n} agents · 1 update available`; `m ≥ 2` → `Checked {n} agents · {m}
     updates available`.
   - `n = 0` → **`No installed agents to check`** (chosen: the pass checked nothing and saying
     "Checked 0 agents · no updates" would read as a completed all-clear, which it is not).
   - The string lives in `UpdateCheckSummary` (`string?`, null = not shown), rendered by a
     `HintText`-styled `TextBlock` with `EmptyToCollapsedVisibility`; null on `Load()` and at
     check start. The `·` separator matches the existing `AvailabilitySummary` style
     (AgentManagementViewModel.cs:92-97).
   - Layout: sits left of the two buttons inside the header `SpacedPanel` (decision 1). At the
     default 888px window it fits; at `MinWidth=640` the row will clip before the buttons do —
     accepted, the settings pages are designed for the default width (smoke checks the default;
     see Open Question 1 for the fallback).

9. **Spec deltas.**
   - `agent-ui` **ADDED** requirement "Agent management header actions" — the two actions, their
     split semantics, mutual exclusion + per-card gating, tooltip/automation-name disambiguation,
     and the summary line with its exact wordings and clear-on-reload/start rule.
   - `agent-ui` "Agent card action availability": handled as **REMOVE + ADD** (repo precedent:
     `2026-10-18-agent-card-compaction` replaced "Agent card content" the same way) so the
     scenario `Test always available` becomes `Detect available on every card` with the busy-gate
     precondition, and a new `Per-card Detect is local only` scenario carries the no-availability
     semantics. All ten other scenario names are preserved verbatim in the ADD. The literal
     same-name REMOVE+ADD does not validate (`Requirement present in both ADDED and REMOVED`,
     validator output quoted below), so the ADD is retitled **"Agent card actions"** — the rename
     the validator demands is the requirement title, not the scenarios.
   - `agent-update-detection` **ADDED** requirement "On-demand update availability check" and
     **MODIFIED** "Update checks are gated on host installation" (all three scenario names
     preserved, one scenario added). The MODIFIED rule from the archived `agent-install-state`
     lesson (`MODIFIED … omits scenario(s) the current spec still has`) applies only to dropped or
     renamed scenarios; adding alongside is allowed.
   - `agent-version` / `agent-availability-display`: no delta (verified above — behavior
     unchanged).

10. **Tests are updated to the new contract, never weakened.**
    - `DetectCommand_WhileProbing_ShowsDetectingThenSettlesEachCard` (lines 803-854):
      `CheckAsync(a)` moves from `Times.Exactly(2)` to `Times.Once` (Load only — the Detect pass
      adds none) with the comment rewritten; the mid-pass `CanDetect == false` assertion stays and
      gains `CanCheckUpdates == false`.
    - `TestCommand_ProbeUpdatesCardAndAvailability` → `DetectCardCommand_UpdatesInstallationAndVersionWithoutAvailabilityCheck`
      (renamed with the command): pre-seed `card.UpdateAvailability`, run the per-card Detect,
      assert `UpdateAvailability` unchanged and `CheckAsync` still `Times.Once` (Load only).
    - `DetectCommand_UpdatesCardsSummaryAndOptionAvailability` (748-771): extended to verify the
      Detect pass makes no `CheckAsync` calls beyond the Load pass.
    - The restore-on-throw and not-installed tests (857-885, 888-915, 918-949) follow the
      `DetectCardCommand` rename; assertions unchanged — they already encode the preserved rules.
    - New coverage: forced check invalidates then checks every installed card
      (`InvalidateFor` `Times.Once` per installed agent, before `CheckAsync`); skips `NotInstalled`
      cards and clears their `UpdateAvailability`; never calls `ProbeAsync` or
      `version.GetVersionAsync`; assigns fresh results to `UpdateAvailability`; summary strings
      (plural, singular `1 agent`/`1 update available`, `no updates`, `No installed agents to
      check`); summary cleared by `Load` and by a second check's start; mutual exclusion while
      either pass is gated (deferred-operation pattern with `SpinWait`, like lines 830-842);
      per-card Detect command `CanExecute` false while a header action runs.
    - No test is deleted without a replacement assertion of equal or stricter strength.

11. **Docs and screenshot.** `docs/backlog.md` entries at lines 142-158 and 160-171 name
    `TestAsync`/the "Test" action; they are reworded to the new `DetectCardAsync` vocabulary
    (behavior they describe is preserved, so the findings stay open). The
    `docs/images/settings-agents.png` capture is refreshed during the smoke stage per
    docs/images/README.md. No other doc describes these buttons (verified: docs/architecture/ui.md
    mentions the summary cards only; README references the page by screenshot).

## Risks / Trade-offs

- **Header Detect no longer refreshes update chips.** A user pressing Detect after an agent update
  may see a stale "update available" chip until Check updates or a reload. This is the agreed
  point of the split; the tooltips state it.
- **Forced check cost:** one source request per installed agent per press (that is its purpose);
  in-flight coalescing and per-agent invalidation already bound the redundancy.
- **Version-command side effect inside the forced check** (Decision 5 precision): after a Detect
  pass invalidated the version caches, a Check updates pass re-runs each installed agent's version
  command inside the availability service. Unavoidable without changing the service (non-goal).
- **Two `CanX` properties are always equal** (`!IsDetecting && !IsCheckingUpdates`). Acceptable:
  they answer different questions ("may this button run now") for different commands, and keeping
  separate names preserves the ability to diverge (e.g. per-card gating) without a breaking rename.
- **MinWidth layout tightness** (Decision 8): accepted; smoke verifies the default width.
- **REMOVE+ADD replaces a requirement wholesale**, so `openspec archive` will not show a
  scenario-level diff for "Agent card action availability" → "Agent card actions". The ten
  preserved scenario names keep review tractable; precedent exists (2026-10-18).

## Deviations from the brief

- **Requirement retitle instead of same-name REMOVE+ADD.** The brief offered "REMOVE + ADD, or the
  scenario name kept with a reworded body". The literal REMOVE+ADD with the *same* title does not
  validate: `openspec validate agent-detect-update-actions` exits 1 with
  `✗ [ERROR] agent-ui/spec.md: Requirement present in both ADDED and REMOVED: "Agent card action
  availability"` (validator output, 2026-10-26). Keeping the scenario name with a reworded body was
  rejected for leaving a permanent "Test always available" misnomer. The chosen third path —
  REMOVE the requirement and ADD it retitled as "Agent card actions" with every scenario name
  preserved except the renamed Detect one — is the repo's established rename pattern.
- **Per-card command member rename** (Decision 7) goes beyond the brief's UI-text rename. Reasoned
  and recorded above; flagged here because it touches `IAgentManagementSection` and test names.
- **Left as is:** the `agent-ui` requirement "Agent card displayed content" scenario "Detecting
  state shown" still says "WHEN a detection or test probe is running for a card"
  (openspec/specs/agent-ui/spec.md:310-314). The word "test" is now stale vocabulary, but the
  scenario describes the Detecting chip, whose behavior is untouched; rewording it would force a
  full MODIFIED reproduction of a 12-scenario requirement for a cosmetic gain. Not changed.
- Precision, not deviation: the brief's decision 6 cites `AgentManagementViewModel.cs:74, 165-178`
  — verified exact. The brief's "around lines 212-220" for the Test button is verified as the
  `<Button>` element at lines 210-220 with the automation name at 212.

## Validator experiment record (2026-10-26)

1. First delta draft: `## REMOVED Requirements` "Agent card action availability" + `## ADDED
   Requirements` "Agent card action availability" (renamed scenario) in the same file. Result:
   `openspec validate agent-detect-update-actions` exited 1 with
   `✗ [ERROR] agent-ui/spec.md: Requirement present in both ADDED and REMOVED: "Agent card action
   availability"` (plus the long-requirement warnings only).
2. Second delta draft (submitted): REMOVED "Agent card action availability" + ADDED **"Agent card
   actions"** (identical body plus the per-card Detect semantics sentence, ten scenario names
   preserved, `Test always available` → `Detect available on every card`, new `Per-card Detect is
   local only`). Result: `openspec validate --all` — `Totals: 30 passed, 0 failed (30 items)`
   including `✓ change/agent-detect-update-actions`; the only new diagnostics are three
   long-requirement warnings on the ADDED requirements, the same warning class the baseline
   already carries across the durable specs.

## Open Questions

1. **Summary line placement at very narrow widths.** The design pins it next to the buttons in the
   header row (brief's wording). If the smoke finds the default-width row crowded (AGENTS + two
   buttons + summary), the fallback is moving the summary to a full-width muted line directly
   under the header — a one-line XAML move, no VM change. Implementer should not preemptively
   choose the fallback.
2. **`DetectCardAsync` settle message wording.** The per-card message (`Installed · version {v}` /
   `Installed` / `Not installed on the host`, AgentManagementViewModel.cs:413-418) is kept as is.
   If reviewers want it to mention that updates were not checked, that is a one-string change —
   left as-is to avoid duplicating the tooltip.
3. **Should `Check updates` also refresh the blocked-state pass?** Out of scope (non-goal: no
   change to the running-agent block), recorded because a reviewer may expect "check everything".
   The forced check deliberately touches availability only.
