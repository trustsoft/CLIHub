# Quick Wins: Design Document

## Verified Current State

### 1. DomainEventBus Synchronization

**File: src/CLIHub.Core/Events/DomainEventBus.cs**

**Lines 1–97:**
- `DomainEventBus` is the synchronous domain event dispatcher.
- Line 12: `private readonly Dictionary<Type, List<Delegate>> subscriptions = new();` — unsynchronized.
- Lines 24–50: `Publish<TEvent>` snapshots handlers with `.ToList()` (line 36) before iterating, which protects the iteration itself but not the snapshot operation.
- Lines 53–74: `Subscribe<TEvent>` adds to the list and returns a disposal token; `Subscription.Dispose` (lines 91–95) removes from the list.
- Evidence (2026-11-14, `plugin-feed-background-sync` verification): every publish before that change ran on the UI thread (`ProjectSelected`, `AgentLaunched`). The new `PluginFeedSyncService` publishes `PluginFeedUpdatedEvent` from a background thread (off-UI), so a background `Publish` racing a UI-thread `Subscribe`/`Dispose` of the same event type can throw `InvalidOperationException` from the snapshot's `.ToList()`.

**File: src/CLIHub.Core/Plugins/PluginFeedSyncService.cs**

**Lines 217–219:**
```csharp
// Change detected: publish the event, which makes agent cards re-resolve their update chip.
this.eventPublisher.Publish(new PluginFeedUpdatedEvent(result.Index!, syncMetadata.IndexHash));
```
This runs on `Task.Run` background thread (line 119: `Task.Run(async () => { ... })` from the periodic timer loop).

**Normal operation safety:** The `SettingsAgentsPage.AgentManagementViewModel` subscribes once at construction (in `InitializeDomainEvents()`) and disposes once at teardown; publishes happen on the first fetch and hourly on change only. The race window is sub-microsecond (subscribe/dispose vs publish-snapshot), and the current subscription lifetimes do not overlap the UI subscribe window — so it is not reachable today.

**Latent risk:** A future background publisher whose subscription lifetime overlaps the UI subscribe window (e.g., a per-page subscriber, or a second background service publishing during page navigation).

### 2. Maximize/Restore Tooltip — already fixed, dropped

The chrome refactor moved the header out of `ChromeWindowHeader`/`ChromeWindowStyles.xaml`
into the shared `src/CLIHub/Views/WindowHeaderBar.xaml` UserControl, and that file already
switches the tooltip with the window state:

- `WindowHeaderBar.xaml:67` — `Setter Property="ToolTip" Value="Maximize"`
- `WindowHeaderBar.xaml:72-74` — `DataTrigger` on `{Binding WindowState, RelativeSource={RelativeSource AncestorType=Window}}` `Value="Maximized"` setting `ToolTip="Restore"`
- `WindowHeaderBar.xaml:83-85` — the same trigger already flips the glyph to `IconGlyphs.Restore`

So the backlog entry (docs/backlog.md, "Settings maximize button keeps a static tooltip when
maximized") describes the pre-refactor tree. No implementation work is needed; Task 3 marks the
entry resolved.

### 3. Plugin-Management Interface Members

**File: src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs**

**Lines 1–159:** The interface defines the agent-management section contract, with 17 properties and 6 commands. Missing members (found on the implementation but not the interface):

1. `ObservableCollection<InvalidAgentCardItem> InvalidCards { get; }` (line 170 of implementation)
2. `ObservableCollection<DisabledAgentCardItem> DisabledCards { get; }` (line 176 of implementation)
3. `bool HasDisabledPlugins => this.DisabledCards.Count > 0;` (line 181 of implementation)
4. `string PluginCatalogSummary` (line 187 of implementation)
5. `RelayCommand<DisabledAgentCardItem> EnablePluginCommand { get; }` (line 336 of implementation)

**File: src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs**

**Lines 170–176, 181, 187, 336:** The five members are implemented on the concrete class only.

**File: src/CLIHub/Views/SettingsAgentsPage.xaml**

**Lines 329–399:** XAML binds `AgentsState.DisabledCards`, `HasDisabledPlugins`, and `EnablePluginCommand`.

**Line 219 (src/CLIHub/Views/SettingsWindow.xaml):** Binds `PluginCatalogSummary`.

**File: src/CLIHub/ViewModels/Settings/SettingsViewModel.cs**

**Line 188:** `public IAgentManagementSection AgentsState { get; }` — the property is typed as the interface, but XAML resolves the concrete members via reflection, so the binding works at runtime despite the type contract not declaring them.

**Impact:** Non-XAML consumers of `AgentsState` (tests, future coordinators, other view models) cannot see these members without narrowing to the concrete type or using reflection, which breaks the abstraction.

**Unused member:** `AvailabilitySummary` (line 32 of `IAgentManagementSection.cs`) was bound by the AGENTS summary card before the card switched to `PluginCatalogSummary`. It is no longer bound anywhere, but its removal is not in scope for this change (deferring to a property-cleanup pass).

---

## Decisions

### D1: DomainEventBus Locking Strategy — ReaderWriterLockSlim

**Decision:** Use `ReaderWriterLockSlim` with read locks for `Publish` (snapshot) and write locks for `Subscribe`/`Unsubscribe` (mutation).

**Rationale:**
- `Publish` only reads the subscriptions map to snapshot the handler list — a read lock allows concurrent publishes (common in high-frequency event scenarios).
- `Subscribe`/`Unsubscribe` mutate the map or the handler list — a write lock serializes them.
- `ReaderWriterLockSlim` is recommended for scenarios with many reads and few writes; it outperforms `lock` (Monitor) when publishes outnumber subscribes, which is true here (subscribes are once per page lifetime, publishes are once per event).
- The lock protects the snapshot operation (`.ToList()`) from a concurrent mutation that would throw `InvalidOperationException`.

**Alternative rejected — `lock` (Monitor):** Simpler but forces publishes to serialize even when no mutation is happening. Given that `Publish` is called far more often than `Subscribe`/`Unsubscribe` (hourly publishes vs. once-per-page subscribes), the read/write split is worth the small complexity increase.

**Alternative rejected — copy-on-write (immutable array swap):** Removes locks from `Publish` entirely, but requires atomicity around the swap (still needs a lock or `Interlocked` for writes) and allocates a new array on every subscribe/unsubscribe. With typical subscription counts of 1–3 per event type, the allocation overhead outweighs the benefit. `ReaderWriterLockSlim` is the standard pattern here.

**Disposal:** `ReaderWriterLockSlim` implements `IDisposable`. Make `DomainEventBus` implement `IDisposable` and dispose the lock. Update the DI registration to call `.Dispose()` on app shutdown (or make it a singleton that lives for the app lifetime; it is already a singleton in practice).

### D2: Maximize/Restore Tooltip — dropped (already implemented)

**Decision:** No code change. `src/CLIHub/Views/WindowHeaderBar.xaml:67-74` already sets
`ToolTip="Maximize"` by default and switches it to `"Restore"` through a `WindowState` data
trigger, alongside the glyph trigger at :83-85.

**Rationale:** The backlog entry predates the shared-chrome refactor. Re-verified on 2026-11-14;
the observed tree already satisfies the requirement. Task 3 records the backlog resolution.

### D3: Plugin-Management Interface Members — Add to Interface

**Decision:** Add the five missing members to `IAgentManagementSection` and update the interface documentation.

**Rationale:**
- The interface is supposed to represent the full agent-management section contract. Omitting members that are actively bound by XAML makes the interface documentation inaccurate and forces consumers to narrow to the concrete type.
- WPF binding via reflection is a runtime convenience, not a design intent. The interface should declare what the contract is.
- Tests and future coordinators can then consume `IAgentManagementSection` without type narrowing or reflection.

**Order of additions (for interface coherence):**
1. `InvalidCards` and `DisabledCards` after `Cards` (line 17), grouped as "card collections".
2. `HasDisabledPlugins` after `DisabledCards`, as a derived property.
3. `PluginCatalogSummary` after `AvailabilitySummary` (line 32), grouped with other summary properties.
4. `EnablePluginCommand` after `CheckUpdatesCommand` (line 100), grouped with other commands.

**AvailabilitySummary:** Leave it in the interface despite being unused. Removing it is a separate decision (deferred to a property-cleanup pass). This change focuses on adding the missing members, not pruning unused ones.

**Alternative rejected — extract a narrower `IPluginManagementSection`:** Creates a second interface for a subset of the same surface, which adds indirection without clear benefit. The agent-management section already owns plugin management as part of its responsibility (the Settings CLI Agents page combines both). Keep one interface.

---

## Deviations from Brief

### Items Dropped (Already Fixed)

1. **PluginFeedClient cancellation handling** (backlog lines 443–458): Already fixed. The code at `src/CLIHub.Core/Plugins/PluginFeedClient.cs:794-796` now throws external cancellation with `catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }` before the general catch, which is exactly what the backlog remedy proposed. No work needed.

2. **Unused IconGlyphs constants** (backlog lines 596–608): Already fixed. `IconGlyphs.Diagnostic` (E9D9) and `IconGlyphs.Process` (E9F3) are no longer in `src/CLIHub/Theming/IconGlyphs.cs`. The file has 52 constants (lines 9-60), none named `Diagnostic` or `Process`. The backlog evidence was correct at the time (2026-11-08), but the constants were removed in a subsequent change. No work needed.

**Scope adjustment:** The change now covers only the three items that remain open (DomainEventBus, maximize tooltip, plugin-management interface). The proposal and tasks reflect this reduced scope.

---

## Open Questions

### OQ-1: Should the DomainEventBus lock be exposed for testing?

**Question:** Should `DomainEventBus` expose a test-only hook to observe lock contention, or should concurrency tests rely on behavior observation only (e.g., no `InvalidOperationException` escapes)?

**Options:**
1. **Behavior-only tests** (preferred): Spawn concurrent subscribes and publishes and assert no exception escapes. The lock is an implementation detail.
2. **Expose lock state**: Add an internal `bool IsLockHeld { get; }` property for tests to observe. More invasive, couples tests to the lock implementation.

**Recommendation:** Behavior-only. A concurrency test that runs 1000 publishes concurrent with 100 subscribes/unsubscribes and asserts no exception is sufficient. The lock is not part of the contract.

### OQ-2: Should the tooltip trigger be in the control template or the window?

**Question:** The maximize/restore button is in the `ChromeWindowHeader` control template. Should the tooltip trigger be in the template (binding to the ancestor `Window.WindowState`), or should `ChromeWindowHeader` expose a `TooltipText` property that the window sets via binding?

**Options:**
1. **In the template** (preferred): The glyph trigger is already in the template, so the tooltip trigger should be alongside it for consistency. The template binds to the ancestor window's `WindowState` via `RelativeSource`.
2. **As a control property**: The window sets `<ChromeWindowHeader MaximizeTooltip="{Binding ...}" />`. More flexible but adds a property that only exists for tooltip binding, and the control's responsibility is not to manage tooltips.

**Recommendation:** In the template. The glyph and tooltip are both view concerns, and the template is the right place for them. The control should not need to know about tooltip text.

### OQ-3: Should `AvailabilitySummary` be removed from the interface?

**Question:** `AvailabilitySummary` (line 32 of `IAgentManagementSection.cs`) is no longer bound anywhere. Should this change remove it, or defer removal to a property-cleanup pass?

**Options:**
1. **Defer removal** (chosen): This change focuses on adding missing members, not pruning unused ones. Removing `AvailabilitySummary` is a separate decision with its own test/validation scope (ensure no hidden consumers exist).
2. **Remove it now**: One less property to maintain, and the change is already touching the interface. But it widens the scope and mixes two intents (add missing vs. remove unused).

**Recommendation:** Defer. The property is harmless (no tests break if it stays), and removing it is a distinct intent. A property-cleanup pass can review all unused interface members at once (including whether other summary properties are unused).
