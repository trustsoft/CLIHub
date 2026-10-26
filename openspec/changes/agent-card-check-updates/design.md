# Design: per-agent "Check updates" action and Detect icon parity

## Context (verified current state)

All file:line references verified against the working tree at commit `41400df` on 2026-10-26.

### The card action row today

- The card list is an `ItemsControl` bound to `AgentsState.Cards`; the card `DataTemplate` starts at
  src/CLIHub/Views/SettingsAgentsPage.xaml:168-170. The action row is a
  `<controls:SpacedPanel Orientation="Horizontal" Spacing="2">` at :226-266 with three icon buttons:
  - **Detect** (:230-241): `AutomationProperties.Name="Detect agent"` (:232), tooltip
    `"Detect installation and version"` (:237), glyph `IconGlyphs.Diagnostic` (:240),
    `Command="{Binding DataContext.AgentsState.DetectCardCommand, RelativeSource={RelativeSource AncestorType=UserControl}}"`
    (:233), `CommandParameter="{Binding}"` (:234),
    `IsEnabled="{Binding DataContext.AgentsState.CanDetect, RelativeSource={RelativeSource AncestorType=UserControl}}"`
    (:235) — the card Detect is gated by the *view-model* flag, uniformly across cards.
  - **Update** (:242-253): glyph `IconGlyphs.Update` (:252), `IsEnabled="{Binding CanUpdate}"` (:247)
    — a *card-local* binding, no `canExecute` on the command.
  - **Edit config** (:254-265): glyph `IconGlyphs.Document` (:264),
    `IsEnabled="{Binding CanEditConfig}"` (:259) — card-local.
- The header actions sit at :121-167: header Detect uses `IconGlyphs.Refresh` (:143) with
  `AutomationProperties.Name="Detect installed agents and versions"` and tooltip
  `"Detect installed agents and versions"` (:134-138); header Check updates uses `IconGlyphs.Sync`
  (:159) with name/tooltip `"Check for agent updates"` (:150-154).
- Glyph constants (src/CLIHub/Themes/IconGlyphs.cs): `Refresh` = `\uE72C` (:13), `Update` =
  `\uE896` (:15), `Diagnostic` = `\uE9D9` (:23), `Sync` = `\uE895` (:33). `IconGlyphs.Diagnostic`
  remains in use on this page for the Cache TTL and Version check timeout SettingCard icons
  (:100, :111), so the constant stays.
- The card's message line is `TextBlock … Text="{Binding Message}"` with
  `Visibility="{Binding HasMessage, Converter={StaticResource BoolToVisibility}}"` (:220-223).

### The view model today

- `AgentManagementViewModel` constructor wires the commands at
  src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:72-76. The per-card Detect is the
  `RelayCommand<AgentCardItem>` pattern to copy:
  `new RelayCommand<AgentCardItem>(card => _ = this.RunAsync("Agent detect", ct => this.DetectCardAsync(card, ct)), _ => this.CanDetect)` (:72).
- Busy state: `isDetecting`/`isCheckingUpdates` fields (:37-38); `IsDetecting` (:170-180) and
  `IsCheckingUpdates` (:186-196) setters call `RaiseBusyStateChanges()` on change;
  `CanDetect => !this.IsDetecting && !this.IsCheckingUpdates` (:183) and
  `CanCheckUpdates => !this.IsDetecting && !this.IsCheckingUpdates` (:199).
- `RaiseBusyStateChanges()` (:298-305) raises `OnPropertyChanged` for `CanDetect`/`CanCheckUpdates`
  and `RaiseCanExecuteChanged()` on `DetectCommand`, `CheckUpdatesCommand`, `DetectCardCommand`.
  Its doc comment (:292-297) explains why: WPF buttons cache `ICommand.CanExecute` and re-query only
  on `CanExecuteChanged`. `RelayCommand`/`RelayCommand<T>` both expose `RaiseCanExecuteChanged()`
  (src/CLIHub/ViewModels/Mvvm/RelayCommand.cs:50, RelayCommandOfT.cs:51).
- `CheckUpdatesAsync` (:524-569) is the header forced check and the semantics template: snapshot
  cards; per installed card `availability.InvalidateFor(card.Plugin)` then
  `await availability.CheckAsync(card.Plugin, cancellationToken)`; assign
  `card.UpdateAvailability` guarded by `!this.disposed` (:549-555); not-installed cards are skipped
  and cleared (:541-547); summary rebuilt at :558-563 from `BuildUpdateCheckSummary` (:578-592).
- `UpdateAsync` (:617-633) shows the per-card action shape (message writes at :619-625) and the
  call-site invalidation pattern after a started update (:627-632).
- `Dispose` (:261-272) cancels the population passes and sets `disposed`; in-flight header checks
  are not cancelled but their writes are discarded by the `!this.disposed` guard (:552-555).
- `RunAsync` (:654-655) routes through `IApplicationOperationLifetime`; operation names in use:
  "Agent detect", "Agent update", "Agent detection", "Agent update check".

### The card model today

- `AgentCardItem` (src/CLIHub/ViewModels/Agents/AgentCardItem.cs): `InstallState` (:131-142, raises
  `IsInstalled` and `VersionChipText` on change), derived `IsInstalled` (:148),
  `CanUpdate` (:183-187, settable, written by the VM), `UpdateAvailability` (:193-204, raises
  `UpdateAvailabilityText`/`HasUpdateAvailable`), `UpdateAvailabilityText` (:210-213,
  `"{installed} → {latest}"`), `HasUpdateAvailable` (:219), `CanEditConfig` (:224), `Message`
  (:229-239, raises `HasMessage`).
- `AgentUpdateAvailability` record (src/CLIHub.Core/Agents/AgentUpdateAvailability.cs:32-35):
  `Status`, `InstalledVersion?`, `LatestVersion?`. `AgentUpdateStatus` = `Unknown | UpToDate |
  UpdateAvailable` (:6-23).
- `IAgentUpdateAvailabilityService` (src/CLIHub.Core/Agents/IAgentUpdateAvailabilityService.cs:20-32):
  `CheckAsync(plugin, ct)` — "Failures degrade to `Unknown` instead of throwing; caller cancellation
  still propagates" — plus `Invalidate()` and per-agent `InvalidateFor(plugin)` ("Other agents'
  cached results and already-running checks are unaffected"). No Core change is needed.

### The section interface today

- `IAgentManagementSection` (src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs:70-125) declares
  `DetectCardCommand` (:74), `UpdateCommand` (:79), `EditConfigCommand` (:84), `DetectCommand` (:90),
  `CheckUpdatesCommand` (:97), `IsDetecting` (:102), `CanDetect` (:108), `IsCheckingUpdates` (:113),
  `CanCheckUpdates` (:119), `UpdateCheckSummary` (:125).
- The only implementation is `AgentManagementViewModel`
  (src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:18; registration at
  src/CLIHub/Composition/ServiceRegistration.cs:165). All other usages are
  `Mock.Of<IAgentManagementSection>()` in tests (SharedUpdatePresentationTests.cs:138,
  SettingsViewModelApplicationTests.cs:936, SettingsOperationLifetimeTests.cs:48,
  SettingsNavigationTests.cs:207) — Moq loose mocks absorb added members.

### Spec state

- `openspec/specs/agent-ui/spec.md`:
  - "Agent card displayed content" (:219-285) — the message line, the advisory update indicator, and
    "Update indicator is advisory" (:281-285).
  - "Agent card actions" (:317-392) — requirement body :319; thirteen scenarios :321-392. The last
    body sentence pins the per-card Detect contract ("… unavailable while a header-wide action is
    running"). No glyph is currently specified anywhere.
  - "Agent management header actions" (:394-442) — mutual exclusion at :396, scenario "Header
    actions are mutually exclusive" :428-432 ("the per-card Detect action is unavailable on every
    card"), "Header actions become available again when a pass completes" :434-437, "Tooltips
    disambiguate the header actions" :439-442.
- `openspec/specs/agent-update-detection/spec.md`:
  - "Update checks are gated on host installation" (:180-205) — "The gate applies to every caller —
    the load-time population pass, installation probes, and forced update checks alike" (:182).
  - "On-demand update availability check" (:207-241) — forced check "requested from the management
    UI, that bypasses the per-agent cached result and its time-to-live by invalidating only the
    checked agents' cached results" (:209); scenario "Forced check invalidates only checked agents"
    (:216-219) already covers a single-agent subset.
- Baseline `openspec validate --all` at `41400df`: **29 passed, 0 failed (29 items)**; only
  pre-existing long-requirement warnings.

### Tests today

- `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs` — the forced-check
  contract tests to mirror: `CheckUpdatesCommand_WithInstalledCards_InvalidatesThenChecksEach`
  (:975-1004, invalidation-before-check ordering), `…_WithNotInstalledCard_SkipsCheckAndClearsStaleAvailability`
  (:1007-1042), `…_AssignsFreshResultToInstalledCard` (:1045-1062), the raise-count tests
  `DetectCommand_RaisesCanExecuteChangedAtPassStartAndEnd` (:1235-1282) and
  `CheckUpdatesCommand_RaisesCanExecuteChangedAtPassStartAndEnd` (:1285-1335), and
  `DetectCardCommand_WhileHeaderActionRuns_CanExecuteIsFalse` (:1199-1232). Test helpers:
  `InlineOperationLifetime` (runs synchronously, :1337-1349) and `DeferredOperationLifetime`
  (fire-and-forget, :1351-1359).
- `tests/CLIHub.Tests/ViewModels/Agents/AgentCardItemTests.cs` — notification tests to mirror
  (e.g. `UpdateAvailability_WhenSet_RaisesIndicatorNotifications`, :189-200).
- No UI test harness exists for visual-tree assertions (no test references
  `SettingsAgentsPage` or glyphs — verified by search).

## Goals / Non-Goals

**Goals:** the per-card Detect renders the header Detect glyph; a per-card "Check updates" action
exists with unique accessible naming, the forced-check contract scoped to one agent, install-state
and busy gating with two-way mutual exclusion against the header actions, outcome feedback on the
card's message line, and full unit coverage; spec deltas that match; existing tests untouched and
green.

**Non-Goals:** as listed in proposal.md.

## Decisions

1. **State model: one per-card busy flag plus one view-model flag (the brief's suggested shape),
   with the card flag carrying enablement and the VM flag carrying exclusion.**
   - `AgentCardItem.IsCheckingUpdates` (`bool`, observable, VM-written): the per-card busy flag.
   - `AgentCardItem.CanCheckUpdates` (derived, card-scope): `IsInstalled && !IsCheckingUpdates` —
     raised from the `IsCheckingUpdates` setter and from `InstallState` changes (mirroring how
     `InstallState` already raises `IsInstalled`/`VersionChipText`, AgentCardItem.cs:136-140). This
     is the new button's `IsEnabled` binding, so it covers the card-local concerns exactly.
   - `AgentManagementViewModel.IsCheckingCardUpdates` (`bool`, private set, calls
     `RaiseBusyStateChanges()` on change like the other two flags): true while a per-card check
     runs. Because per-card checks are mutually exclusive, one bool is exact — no counter needed.
   - Name scoping note: `CanCheckUpdates` on the card ("this card may be checked") vs
     `CanCheckUpdates` on the VM ("the header action may run") share a name deliberately; XAML
     disambiguates by binding scope (card-template bindings are card-scope; header bindings go
     through `AgentsState.`), exactly like card `CanUpdate` vs the header-level busy flags.
   - *Alternative rejected:* derive header gating from `Cards.Any(c => c.IsCheckingUpdates)` —
     requires subscribing to every card's `PropertyChanged` for change notifications; strictly more
     machinery than one bool.
   - *Alternative rejected:* no per-card flag, VM flag only — then the card's local `IsEnabled` is
     just `IsInstalled` and its own busy state is invisible to the binding; loses the repo's
     double-gate pattern (binding + `canExecute`) and the brief's suggested model.

2. **Gating predicates.**
   - Header: `CanDetect => !IsDetecting && !IsCheckingUpdates && !IsCheckingCardUpdates` and the
     same expression for `CanCheckUpdates`. This adds the new "while a per-card check runs, the
     header actions are unavailable" rule from the brief.
   - Per-card command: `CheckCardUpdatesCommand = new RelayCommand<AgentCardItem>(card => _ =
     this.RunAsync("Agent card update check", ct => this.CheckCardUpdatesAsync(card, ct)), card =>
     this.CanCheckCardUpdates(card))` with
     `private bool CanCheckCardUpdates(AgentCardItem card) => card.CanCheckUpdates && this.CanDetect;`
     — "the card is checkable (installed, not already being checked) and no management pass is
     running".
   - XAML: the button binds `IsEnabled="{Binding CanCheckUpdates}"` (card-local) *and* the command
     carries the full predicate. WPF combines the two: `ButtonBase.IsEnabledCore` returns
     `base.IsEnabledCore && CanExecute`, so a button whose local `IsEnabled` is true is still
     disabled while the command's `CanExecute` is false — the composition covers card-local state
     through the binding and global exclusion through the command, with the usual defensive benefit
     that a missed re-query can only leave a button disabled, never wrongly enabled.
   - `RaiseBusyStateChanges()` (:298-305) gains `this.CheckCardUpdatesCommand.RaiseCanExecuteChanged()`
     and is called from all three flag setters, so every flip re-queries all four gated commands.

3. **Per-card Detect is also unavailable while a per-card check runs** (beyond the brief's literal
   enumeration, which lists "the header actions and the other per-card checks").
   - Mechanism: free — the per-card Detect already binds `IsEnabled` to `AgentsState.CanDetect`
     (:235) and its command already predicates on `this.CanDetect` (:72), so extending `CanDetect`
     with `!IsCheckingCardUpdates` (Decision 2) gates it automatically with zero extra surface.
   - Rationale: one gate expression for all "pass-shaped" actions instead of a divergent second
     predicate; consistent with the existing philosophy that a heavyweight pass gates the
     lightweight per-card probe (archived `agent-detect-update-actions` design, Decision 6); and it
     closes the same-card interleaving where a mid-check Detect would flip the card to `Detecting`
     and its settle could clear the fresh result the check is about to assign (the hazard shape
     already documented for Detect vs header check in docs/backlog.md:188-200).
   - *Alternative rejected:* keep per-card Detect available during a per-card check — requires a
     separate VM observable (e.g. `CanDetectCards`) and a second predicate diverging from `CanDetect`
     purely to preserve availability nobody asked for. Rejected as complexity without a user need;
     the delta adjusts the "Detect available on every card" scenario precondition accordingly.
   - The reverse (per-card Detect running does not gate a per-card check) is unchanged per-card
     Detect behavior: it sets no VM flag today, and the *same card* is protected because its
     `CanCheckUpdates` is false while `InstallState` is `Detecting`; a check for *another* card may
     start, which is fine (disjoint work).

4. **`CheckCardUpdatesAsync` shape** (mirrors `CheckUpdatesAsync` :524-569 scoped to one card):

   ```csharp
   private async Task CheckCardUpdatesAsync(AgentCardItem card, CancellationToken cancellationToken)
   {
       if (card.InstallState != AgentInstallState.Installed)
       {
           // Defensive gate (command + IsEnabled already refuse not-installed cards): a missing
           // CLI has no update source to check; clear any stale advisory indicator.
           card.UpdateAvailability = null;
           card.Message = "Not installed on the host";
           return;
       }

       card.IsCheckingUpdates = true;
       this.IsCheckingCardUpdates = true;
       try
       {
           // Invalidate first so the check below bypasses the cached result and its TTL.
           this.availability.InvalidateFor(card.Plugin);
           var availability = await this.availability.CheckAsync(card.Plugin, cancellationToken);
           if (!this.disposed)
           {
               card.UpdateAvailability = availability;
               card.Message = availability.Status switch
               {
                   AgentUpdateStatus.UpdateAvailable when availability.InstalledVersion is { } installed
                                                     && availability.LatestVersion is { } latest
                       => $"Update available · {installed} → {latest}",
                   AgentUpdateStatus.UpdateAvailable => "Update available",
                   AgentUpdateStatus.UpToDate => "Up to date",
                   _ => "Update availability unknown"
               };
           }
       }
       finally
       {
           this.IsCheckingCardUpdates = false;
           card.IsCheckingUpdates = false;
       }
   }
   ```

   - The not-installed branch is defensive: `RelayCommand<T>.Execute` does not consult `CanExecute`
     (RelayCommandOfT.cs:58-64), and the card's `InstallState` can flip between the click's
     `CanExecute` check and `Execute`; the handler therefore re-applies the forced-check gate
     (no source request, clear the indicator) exactly like the header pass does (:541-547).
   - `InvalidateFor` touches only this agent (interface doc,
     IAgentUpdateAvailabilityService.cs:27-32), so "cache invalidated only for that agent" falls out
     of the existing service contract; other agents' cached results and in-flight checks are
     untouched.
   - Exception path: `CheckAsync` degrades failures to `Unknown` rather than throwing (interface
     doc :13-15), so throws are cancellation or unexpected; either way the `finally` resets both
     flags and `RunAsync`'s operation logging observes the failure. No state needs restoring (the
     action never writes `InstallState`), so no restore-on-throw branch is needed — unlike Detect.
   - Disposal: no dedicated CTS is needed. The write is guarded by `!this.disposed` (mirroring
     :552-555) so a check that completes after `Dispose` (:261-272) is discarded; the flags still
     reset in `finally`, which is harmless on a disposed object.
   - `UpdateCheckSummary` is never read or written by this action (brief: header passes only).

5. **Message wordings (exact strings), on the card's existing message line.**
   - Update available with both versions (the normal case — `UpdateAvailable` implies both triples
     parsed): `Update available · 2.0.32 → 2.0.33`. Reuses the card indicator's `installed → latest`
     arrow convention (AgentCardItem.cs:210-213) and the page's `·` separator convention
     (AvailabilitySummary, AgentManagementViewModel.cs:94-99; Detect message :459).
   - Update available with a missing version (record-shape defensive arm): `Update available`.
   - Up to date: `Up to date` (matches `AgentUpdateStatus.UpToDate` vocabulary).
   - Unknown (failed source, unparseable version, no source declared): `Update availability unknown`
     — honest about indeterminacy; splitting "no source declared" from "check failed" would need a
     Core change (non-goal).
   - Defensive not-installed path: `Not installed on the host` — the Detect action's existing
     wording verbatim (AgentManagementViewModel.cs:461).
   - No trailing periods: these report a discovered state, matching the Detect card messages
     ("Installed · version 9.9") rather than UpdateAsync's action-outcome strings ("Update
     started.").
   - No in-progress message ("Checking…"): the disabled button is the busy cue, and the header pass
     writes no per-card progress either (see Open Question 1).

6. **Naming: command `CheckCardUpdatesCommand`, handler `CheckCardUpdatesAsync`, VM flag
   `IsCheckingCardUpdates`, card flag `IsCheckingUpdates`, card derived `CanCheckUpdates`.**
   `CheckCardUpdatesCommand` keeps the `…Card…` infix that distinguishes `DetectCardCommand` from
   `DetectCommand`, and reads as "check the card's updates".
   - *Alternative rejected:* `CheckUpdatesCardCommand` — mechanically consistent with
     "DetectCard" but reads worse in code (`CheckUpdatesCard`).
   - UI text: `AutomationProperties.Name="Check this agent for updates"` and
     `ToolTip="Check this agent for updates"` (tooltip and name equal, like the per-card Detect at
     :232/:237). Unique against header "Check for agent updates" (:150-154) and per-card
     "Update agent"/"Run the agent's update command" (:244/:249), and states its own work per the
     brief's example.

7. **XAML placement and glyph parity.** The new button is inserted between the Detect button (ends
   :241) and the Update button (starts :242), giving the order Detect, Check updates, Update, Edit
   config. Glyphs: per-card Detect `Diagnostic` → `Refresh` (:240; same glyph as header Detect
   :143); per-card Check updates `Sync` (same glyph as header Check updates :159). The button reuses
   the `ChipRemoveButton` style, `Sizing.IconButton.Width/Height`, and `Sizing.FontSize.Glyph`
   exactly like its neighbors. Icon parity is judged **durable** (pinned as a parity scenario, not
   by codepoint): the user's confusion was precisely a glyph-identity mismatch between paired
   actions, so "same glyph as its header counterpart" is the contract.

8. **Interface additions.** `IAgentManagementSection` gains `CheckCardUpdatesCommand` (placed after
   `DetectCardCommand` to mirror the UI order) and `IsCheckingCardUpdates`; the XML docs of
   `CanDetect`/`CanCheckUpdates` gain "nor while a per-card update check is running". The
   `Mock.Of<IAgentManagementSection>()` usages absorb the additions (verified: Moq loose mocks).
   No `CanCheckCardUpdates` member is added — the gate is parameterized per card and is expressed by
   `card.CanCheckUpdates` plus the VM busy flags; there is no single VM-level bool to expose.

9. **Spec deltas.**
   - `agent-ui` **MODIFIED "Agent card actions"**: requirement body gains the per-card Check updates
     contract (forced check for that one agent, install-state/busy gating, message-line feedback,
     summary and Update-availability untouched, glyph parity). All thirteen original scenario names
     preserved; "Detect available on every card" has its precondition extended to "no header-wide
     action or per-card update check is running" (body change only, name preserved — the direct
     consequence of Decision 3). Six scenarios added: cache bypass (with per-agent-only
     invalidation), host-installation gate, outcome feedback, mutual exclusion (both directions),
     tooltip disambiguation, icon parity.
   - `agent-ui` **MODIFIED "Agent management header actions"**: the mutual-exclusion sentence is
     extended both ways; "Header actions are mutually exclusive" gains the per-card Check updates
     clause; "Header actions become available again when a pass completes" gains the restored
     per-card check availability (scoped to installed cards); one new scenario "A per-card check
     gates the header actions". All eight original scenario names preserved.
   - `agent-update-detection` **no delta**: "On-demand update availability check" (:207-241) states
     the forced-check contract for *any* management-UI caller and any subset of checked agents
     ("invalidating only the checked agents' cached results", scenario "Forced check invalidates
     only checked agents"), and "Update checks are gated on host installation" (:180-205) says the
     gate "applies to every caller". The per-card action is a new trigger of the existing contract;
     restating it in `agent-ui` would duplicate the contract across capabilities, which the brief
     forbids. The agent-ui delta references the behavior at the UI level (gating, feedback,
     exclusivity) without re-specifying service semantics.
   - MODIFIED (rather than REMOVE+ADD) is correct here because every original scenario name is
     kept; the validator only rejects a requirement present in both ADDED and REMOVED (recorded in
     the archived `agent-detect-update-actions` validator experiment) and MODIFIED bodies that drop
     scenarios — neither applies.

10. **Tests: additions only, existing tests must stay green unmodified.** The change is additive to
    observable behavior: `CanDetect`/`CanCheckUpdates` keep their old values whenever no per-card
    check runs, the header pass code is untouched, and no XAML is test-asserted. Any existing test
    needing an edit is a red flag to investigate, not to weaken. New coverage (detailed in
    tasks.md): install-state and busy-state enablement (including `Detecting` cards), per-agent-only
    invalidation with invalidate-before-check ordering, fresh result assignment, the four message
    wordings, header summary untouched, `PropertyChanged` and `CanExecuteChanged` notifications
    (raise counts at check start and end, mirroring :1235-1335), defensive not-installed skip and
    indicator clear, disposal discarding the write while resetting flags, and header-action gating
    mid-check (deferred-operation + `SpinWait` pattern).

## Risks / Trade-offs

- **`CanDetect`/`CanCheckUpdates` gain a third term** — the two header predicates remain equal to
  each other at all times. Pre-existing shape (they were already identical); kept because they
  answer different questions for different commands.
- **Two `IsCheckingUpdates` flags** (VM header pass, card per-card check) with the same name.
  Scoped by class and by XAML binding path; XML docs state the scope. Accepted for naming
  naturalness on both objects.
- **Same-card Detect-during-check race** is closed by Decision 3; a *header* Detect starting during
  a per-card check is impossible (mutual exclusion), so no new interleaving hazard is introduced.
  The pre-existing backlog hazard (:188-200) is untouched.
- **Per-card checks are serialized** (one at a time). A user wanting fresh checks on three cards
  presses three times sequentially. Accepted: it matches the mutual exclusion the brief mandates and
  the availability service coalesces same-agent checks anyway.
- **Long requirement bodies** in the deltas will add to the validator's long-requirement warnings —
  the same warning class the baseline already carries across the durable specs; scenarios carry the
  specifics.

## Deviations from the brief

- **Per-card Detect gated during a per-card check** (Decision 3). The brief enumerates only "the
  header actions and the other per-card checks" as unavailable; gating per-card Detect too is a
  superset chosen because it is free (existing `CanDetect` bindings), simpler (one predicate set),
  and closes a real interleaving. Flagged for reviewer confirmation; flipping to the literal
  enumeration is a localized change (split predicate + one scenario body).
- **Message-wording style beyond the brief's enumeration**: the brief required wordings for
  available/up-to-date/unknown; this design additionally fixes the defensive not-installed wording
  (`Not installed on the host`, reusing Detect's string) and the version-less `UpdateAvailable`
  fallback arm — both needed for a total decision table.

## Open Questions

1. **In-progress feedback.** No "Checking…" message is written while a per-card check runs (Decision
   5). If slow sources make the action feel dead in the smoke, adding one is a two-line change
   (write before the await, clear on settle); implementer should not preemptively add it.
2. **Decision 3 confirmation.** Per-card Detect unavailable while a per-card check runs — confirm
   during review or the smoke that this superset is acceptable; the fallback (keep Detect available)
   is recorded in Decision 3.
3. **`Update availability unknown` granularity.** The card cannot distinguish "no update source
   declared" from "source unreachable" without a Core contract change (non-goal). If users report
   the wording unhelpful for no-source agents, a follow-up change would extend
   `AgentUpdateAvailability`, not this one.
