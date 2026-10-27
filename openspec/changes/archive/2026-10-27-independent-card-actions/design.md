# Design: independent per-agent card actions on Settings → CLI Agents

## Context (verified current state)

All file:line references verified against the clean working tree at commit `847c600` on 2026-10-27.

### The regression path today

- `AgentManagementViewModel` wires the per-card commands in its constructor
  (src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:73-78):
  - `DetectCardCommand` predicate is `_ => this.CanDetect` (:73) — **section-wide**, ignores the
    card parameter.
  - `CheckCardUpdatesCommand` predicate is `card => this.CanCheckCardUpdates(card)` (:74), and
    `CanCheckCardUpdates(card) => card.CanCheckUpdates && this.CanDetect` (:226) — the card-local
    half is ANDed with the section-wide availability.
- Busy flags: fields `isDetecting`/`isCheckingUpdates`/`isCheckingCardUpdates` (:37-39). The
  section-wide derived properties are
  `CanDetect => !this.IsDetecting && !this.IsCheckingUpdates && !this.IsCheckingCardUpdates` (:188)
  and `CanCheckUpdates => !this.IsDetecting && !this.IsCheckingUpdates && !this.IsCheckingCardUpdates`
  (:204). `IsCheckingCardUpdates` (:207-217) is set/cleared only by the per-card Check action.
- `CheckCardUpdatesAsync` (:606-643): after the defensive not-installed gate (:608-615) it sets
  `card.IsCheckingUpdates = true; this.IsCheckingCardUpdates = true;` (:617-618) and resets both in
  `finally` (:640-641). Because `IsCheckingCardUpdates` feeds `CanDetect`, **one** running check
  makes `CanDetect` false, which (a) flips the per-card Detect button's `IsEnabled` on every card
  and (b) makes both per-card predicates false on every card. That is the reported regression.
- `DetectCardAsync` (:476-500): sets only `card.InstallState = AgentInstallState.Detecting`
  (:479); there is **no** per-card busy flag and **no** view-model flag, so a per-card Detect today
  neither gates the header actions nor other cards — but it is itself gated by the section-wide
  predicate whenever a per-card check runs.
- `RaiseBusyStateChanges` (:325-333) raises `OnPropertyChanged` for `CanDetect`/`CanCheckUpdates`
  and `RaiseCanExecuteChanged()` on all four commands (`DetectCommand`, `CheckUpdatesCommand`,
  `DetectCardCommand`, `CheckCardUpdatesCommand`, :329-332). Its doc comment (:319-324) explains
  why: WPF buttons cache `ICommand.CanExecute` and only re-query on `CanExecuteChanged`.
  `RelayCommand`/`RelayCommand<T>` expose `RaiseCanExecuteChanged()`
  (src/CLIHub/ViewModels/Mvvm/RelayCommand.cs:50, RelayCommandOfT.cs:51); `RelayCommand<T>.Execute`
  does **not** consult `CanExecute` (RelayCommandOfT.cs:58-64), which is why the Check handler
  keeps a defensive gate.

### The card model today

- `AgentCardItem` (src/CLIHub/ViewModels/Agents/AgentCardItem.cs): field `isCheckingUpdates`
  (:65); `InstallState` setter raises `IsInstalled`, `VersionChipText`, `CanCheckUpdates`
  (:132-144, raises at :139-141); `IsCheckingUpdates` (:229-239) raises `CanCheckUpdates` (:236);
  derived `CanCheckUpdates => this.IsInstalled && !this.IsCheckingUpdates` (:247). There is **no**
  per-card `IsDetecting` and **no** per-card `CanDetect`.
- `IsInstalled => InstallState == AgentInstallState.Installed` (:150) — false while the state is
  `Detecting`, so a card under its own Detect already loses `CanCheckUpdates` through the
  `InstallState` setter today.

### The XAML today

- Per-card action row (src/CLIHub/Views/SettingsAgentsPage.xaml:221-269):
  - **Detect** button: `IsEnabled="{Binding DataContext.AgentsState.CanDetect, RelativeSource={RelativeSource AncestorType=UserControl}}"`
    (:230) — the **section-wide** binding that greys every card's Detect when any per-card check
    runs.
  - **Check updates** button: `IsEnabled="{Binding CanCheckUpdates}"` (:242) — card-local; the
    command predicate adds the section-wide exclusion.
  - Update (:254) and Edit config (:266) are card-local bindings.
- Header buttons bind `AgentsState.CanDetect` (:136) and `AgentsState.CanCheckUpdates` (:152) —
  section-wide by design, unchanged by this change.
- WPF combines a button's `IsEnabled` binding with its command's `CanExecute` through
  `IsEnabledCore`, so the per-card Check button's pattern (card-local `IsEnabled` + command
  predicate carrying the header gates) is the established pattern to copy for Detect.

### The section interface today

- `IAgentManagementSection` (src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs) declares `DetectCardCommand` (:74),
  `CheckCardUpdatesCommand` (:81), `DetectCommand` (:97), `CheckUpdatesCommand` (:104), `IsDetecting`
  (:109), `CanDetect` (:115), `IsCheckingUpdates` (:120), `CanCheckUpdates` (:127),
  `IsCheckingCardUpdates` (:134, with doc comments at :129-133), `UpdateCheckSummary` (:140).
- The only implementation is `AgentManagementViewModel`
  (src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:18). A repository-wide grep for
  `IsCheckingCardUpdates` finds exactly: the interface (:134), the view model (:188, :204, :207,
  :618, :640), eight assertions in `AgentManagementViewModelTests.cs` (:1484, :1498, :1672, :1681,
  :1714, :1718, :1812, :1818 — all through the **concrete** view model), and archived change docs.
  No XAML and no other source references it, so removing it from the interface breaks no consumer.

### Spec state

- `openspec/specs/agent-ui/spec.md`:
  - "Agent card actions" (:317-432): requirement body :319 pins the current mutual exclusion —
    per-card Detect "SHALL be unavailable while a header-wide action or a per-card update check is
    running"; per-card Check unavailable "… while any header-wide action or other per-card check
    runs". Nineteen scenarios (:321-432), including "Per-card Check updates is mutually exclusive"
    (:415-421) and "Detect available on every card" (:321-324).
  - "Agent management header actions" (:434-491): body :436 — "While a per-card 'Check updates'
    action is running, both header actions SHALL be unavailable." Nine scenarios (:438-491),
    including "A per-card check gates the header actions" (:486-491, whose body also claims the
    per-card Detect action is unavailable on every card — false under the new contract and must be
    reworded).
- Baseline `openspec validate --all` at `847c600`: **29 passed, 0 failed (29 items)**; only
  pre-existing long-requirement and placeholder warnings.

### Tests today

- `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs`:
  - `CheckCardUpdatesCommand_WhilePerCardCheckRuns_CanExecuteIsFalseOnEveryCardAndRestoresAfter`
    (:1453-1501) — pins the regression contract: mid-check it asserts `CanExecute` is false on
    **every** card (:1490-1491) while `cardB.CanCheckUpdates` stays true (:1493, the card-local
    half). Must be rewritten to the independence contract.
  - `CheckCardUpdatesCommand_WhileHeaderDetectRuns_CanExecuteIsFalseOnEveryCard` (:1375-1409) and
    `…WhileHeaderCheckUpdatesRuns…` (:1412-1450) — header passes still disable every card's Check
    under the new contract; unchanged.
  - `CheckCardUpdatesCommand_WhileChecking_GatesHeaderActionsAndRestoresAfter` (:1645-1684),
    `…WhenCheckThrows_RestoresGatesAfterFailure` (:1687-1724), and
    `Dispose_WhilePerCardCheckHeldOpen_DiscardsWritesAndResetsFlags` (:1787-1825) — assert
    `IsCheckingCardUpdates` (:1672, :1714, :1812); swap to `IsCardActionRunning`, otherwise valid.
  - `CheckCardUpdatesCommand_RaisesCanExecuteChangedAtCheckStartAndEnd` (:1727-1784) — exact raise
    counts 1-then-2 for all four commands and mid-check `CanExecute` assertions (:1767-1770) that
    become partly false (other cards' commands must now report true); re-derive.
  - `DetectCardCommand_WhileHeaderActionRuns_CanExecuteIsFalse` (:1199-1232) and the header
    raise-count tests (:1235-1282, :1285-1335) — unchanged behavior, must stay green unmodified.
  - Test helpers: `InlineOperationLifetime` (:1827-1839, runs synchronously) and
    `DeferredOperationLifetime` (:1841-1849, starts the operation and returns) with
    `TaskCompletionSource` gates + `SpinWait`.
- `tests/CLIHub.Tests/ViewModels/Agents/AgentCardItemTests.cs`:
  - `CanCheckUpdates_WhenDerivedFromInstallAndBusyState_IsTrueOnlyForInstalledIdleCard`
    (:218-234, theory over install state × `IsCheckingUpdates`), `IsCheckingUpdates_WhenSet_RaisesCanCheckUpdatesNotification`
    (:236-248), `InstallState_WhenChangedToNotInstalled_RaisesCanCheckUpdatesNotification`
    (:250-261).

## Decisions

Decisions 1-9 below were confirmed by the user before this proposal; they are recorded with their
implementation shape. Decisions 10-12 are implementation rulings this change adds on top.

1. **Per-card actions are independent across cards.** Starting per-card Detect or Check updates on
   card A disables only card A's own Detect and Check updates while it runs; every other card's
   Detect/Check stay enabled (subject to their own install/busy state). Rationale: matches the
   user's mental model of one card = one agent; no shared resource exists between two cards'
   actions (each touches only its own card and per-agent Core services).
2. **Per-card busy state lives on `AgentCardItem`.** Add observable `IsDetecting` and derived
   `CanDetect => !IsDetecting && !IsCheckingUpdates`; change `CanCheckUpdates` to
   `IsInstalled && !IsDetecting && !IsCheckingUpdates`. The `IsDetecting` and `IsCheckingUpdates`
   setters each raise both derived properties, and the `InstallState` setter raises them too (it
   already raises `CanCheckUpdates` at :141; raising `CanDetect` there is defensive — its value
   does not depend on `InstallState`, but the uniform raise costs one no-op re-read and keeps all
   three setters symmetric). Rationale: card-local enablement must be card-local state; this
   mirrors the existing `CanUpdate`/`CanEditConfig` card-local pattern.
3. **Remove `IsCheckingCardUpdates`; track concurrency with a counter.** A `private int
   runningCardActionCount` field and a derived `public bool IsCardActionRunning =>
   runningCardActionCount > 0` replace the flag. Both per-card actions increment before their
   `try` and decrement in `finally`, so two per-card actions on two cards can run at once and the
   header stays gated until the last one ends. Rationale: a boolean cannot express "one of two
   finished"; the counter also keeps the reset correct when a per-card action ends abnormally.
4. **Header actions remain mutually exclusive with each other and with any per-card action.**
   `CanDetect`/`CanCheckUpdates` become `!IsDetecting && !IsCheckingUpdates && !IsCardActionRunning`
   — the same shape as today (:188/:204) with `IsCheckingCardUpdates` replaced and now also
   covering per-card Detect. Header passes continue to disable all per-card actions through the
   per-card command predicates. Rationale: header passes write every card at once; concurrent
   per-card writes would interleave with the pass's snapshots (the backlog hazard below).
5. **Per-card predicates consult only the card and the header flags.** Detect:
   `card => card.CanDetect && !this.IsDetecting && !this.IsCheckingUpdates`; Check:
   `card => card.CanCheckUpdates && !this.IsDetecting && !this.IsCheckingUpdates` (replacing
   `card.CanCheckUpdates && this.CanDetect` at :226, whose `CanDetect` term carried the per-card
   check gate). They never consult `IsCardActionRunning` or other cards' state — that is what makes
   cross-card independence structural rather than incidental.
6. **`DetectCardAsync` owns a try/finally for its busy state.** It sets `card.IsDetecting = true`
   and increments the counter before its existing `try`, and clears both in a `finally` that wraps
   the existing install-state handling (the existing catch at :493-499 still restores
   `InstallState`; the `finally` guarantees the flag and counter reset on every path). Both
   per-card actions raise `DetectCardCommand`/`CheckCardUpdatesCommand` `CanExecuteChanged` at
   start and end (the acting card's `CanExecute` flips; other cards re-query harmlessly), and the
   counter's enter/exit raises `IsCardActionRunning`/`CanDetect`/`CanCheckUpdates` notifications
   plus the two header commands' `RaiseCanExecuteChanged()`. The header flags keep calling the
   existing `RaiseBusyStateChanges()` (all four commands) because a header pass changes every
   command's result.
7. **XAML: one binding change.** Per-card Detect `IsEnabled` becomes `{Binding CanDetect}`
   (card-local, :230); the per-card Check binding stays `{Binding CanCheckUpdates}` (:242); header
   bindings are untouched. During a header pass the card-local `IsEnabled` stays true but the
   command predicate is false, and WPF's `IsEnabledCore` disables the button — the exact pattern
   the per-card Check button already uses.
8. **Spec deltas, not `skip_specs`.** Both requirements live in `agent-ui` and their contracts
   change. Every existing scenario name is preserved; bodies change in place; new scenarios are
   added (see the delta). OpenSpec `RENAMED` works only at requirement level, so no scenario may be
   renamed or removed.
9. **Manual smoke** covers: Check on one card leaves every other card's Detect/Check enabled;
   Detect on one card leaves others enabled; the acting card's own two buttons disable and
   re-enable; header buttons disable while any card action runs and re-enable after; header passes
   still disable all card actions.
10. **`IsCardActionRunning` is exposed on the concrete view model only, not on
    `IAgentManagementSection`.** Nothing binds or reads it through the interface (verified by
    grep: the interface's consumers are the WPF page — which binds only `CanDetect`/
    `CanCheckUpdates`/commands — and `Mock.Of<IAgentManagementSection>()` test doubles), and tests
    assert it through the concrete `AgentManagementViewModel`. The interface loses
    `IsCheckingCardUpdates` and gains nothing, keeping it at the surface's actual consumption.
11. **Plain `int` counter, no `Interlocked`.** Increments happen synchronously in the command
    execute delegate and decrements in `finally` continuations — both on the UI thread in
    production (WPF dispatcher context; no `ConfigureAwait(false)` in this view model) and
    single-threaded in the test hosts. Every existing flag (`IsCheckingCardUpdates` at :618/:640)
    already assumes the same affinity: it mutates and raises `PropertyChanged` from continuations.
    `Interlocked` would protect a mutation path that does not exist.
12. **Counter raises fire on every enter/exit, not only on 0↔1 transitions.** A raise when
    `IsCardActionRunning` did not change value (e.g. 2→1) is a harmless re-query and keeps the
    raise-count tests exact (one header raise per per-card action start/end), mirroring how
    `RaiseBusyStateChanges` fires on every flag flip today.

### Backlog interaction (verified)

`docs/backlog.md:188-200` parks "Per-card Detect interleaving with a header action drops that
card's update indicator" with the proposed remedy "either block the header check while any
per-card detect runs (a shared busy counter), or have the check re-read the settle state per
card". Decision 3 + 4 implement the first remedy exactly: a per-card Detect now blocks the header
actions, and the header predicates already block per-card Detect during a pass. The task list
marks that entry resolved.

## Alternatives rejected

- **Keep `IsCheckingCardUpdates` and relax only the predicates.** The flag would still go true for
  any card's check and `CanDetect`/`CanCheckUpdates` would still carry it, so either the header
  gating breaks or the flag stops meaning "any per-card check" — the counter says what it means.
  Rejected; also the interface member would keep leaking a section-wide per-card concept.
- **Per-card gating only in XAML (keep predicates section-wide).** `IsEnabled` bindings cannot
  express "command CanExecute false" (the button would stay visually enabled and WPF would still
  refuse the click), and tests assert `CanExecute` per card. Rejected.
- **A `HashSet<AgentCardItem>` of busy cards instead of a counter.** Same information, more state
  to keep consistent on exception paths; nothing needs to know *which* cards are busy (each card
  already knows via its own flags). Rejected for simplicity.
- **Raise all four commands from every counter change (reuse `RaiseBusyStateChanges`) and drop the
  explicit per-card raises.** Observably equivalent counts, but it couples the per-card boundary
  raises to the header-flag machinery, and a missing per-card raise would then be invisible to the
  raise-count tests. The split (per-card actions raise the per-card commands; the counter raises
  the header commands) matches Decision 6 and keeps each raise accountable.
- **`Interlocked` counter.** See Decision 11 — no concurrent mutation path exists; plain `int`
  matches the house pattern.
- **Relax the raise-count tests to `>=`.** The exact 1-then-2 counts have caught over- and
  under-raising before (they pin the re-query contract the `RaiseBusyStateChanges` doc comment
  :319-324 explains). Kept exact; re-derived for the new raise structure.

## Deviations and discrepancies

- The task brief cited the archived change as `openspec/changes/archive/2026-10-27-agent-card-check-updates/`;
  the actual archive id is **`2026-10-26-agent-card-check-updates`** (verified). The brief's other
  line references all check out at `847c600`: `IsCheckingCardUpdates` spans :207-217 (brief said
  207-218 — off by one on the closing brace), and the Check handler's flag writes sit at :617-618
  and :640-641 (brief cited :618 and :640 — the `this.` lines).
- The brief described `DetectCardAsync` as ":476 sets only `card.InstallState = Detecting`" —
  confirmed: the method spans :476-500 with the assignment at :479 and no busy flag.
- `IAgentManagementSection.cs` interface members the brief cited as ":74, :81, :115, :127" are
  confirmed at those lines (`DetectCardCommand` :74, `CheckCardUpdatesCommand` :81, `CanDetect`
  :115, `CanCheckUpdates` :127); `IsCheckingCardUpdates` is at :134.

## Open questions

1. **Raise-count expectations for the reworked per-card tests.** The design keeps exact 1-then-2
   counts per command (one explicit per-card raise + one counter/header raise per boundary). If
   the implementer finds a boundary that legitimately needs two raises (e.g. Detect's start also
   flips `InstallState`, which raises card properties but no command), prefer adjusting the count
   with a comment over `>=` assertions — confirm during implementation.
2. **Interface surface.** Decision 10 keeps `IsCardActionRunning` off `IAgentManagementSection`.
   If any future consumer needs it through the interface, add it then; flag if implementation
   reveals an existing consumer I did not find.
3. **Backlog wording.** The task marks `docs/backlog.md:188-200` resolved with a pointer to this
   change. If the maintainer prefers a separate doc-only sweep, drop task 4.1 — it does not gate
   the code.
4. **Concurrent per-card Detect + Check on two cards hitting the same version/availability
   service.** `IAgentUpdateAvailabilityService` already coalesces in-flight per-agent checks and
   the two actions would target different agents, so no new contention is expected; smoke task 5.1
   exercises the two-card Check case, and a unit test covers two concurrent per-card checks. If
   the smoke reveals service-level interference, record it rather than widening the exclusion
   model.
