## Context

See `proposal.md` — Why and What Changes.

### Verified current state (2026-11-16)

**`AgentCardItem.CanUpdate` (`src/CLIHub/ViewModels/Agents/AgentCardItem.cs:217-220`):**
```csharp
public bool CanUpdate =>
    this.UpdateAvailability?.Status == AgentUpdateStatus.UpdateAvailable
    && this.Plugin.Commands?.Get(AgentCommandKind.Update) is not null
    && !this.IsBlockedByRunningAgent;
```
Three conditions: availability reports `UpdateAvailable`, the agent declares an `update` command,
and no matching process is running. `IsInstalled` is **not** tested. The XML doc comment
(`:212-216`) lists the same three conditions.

**`AgentCardItem.IsInstalled` (`:173`):**
```csharp
public bool IsInstalled => this.InstallState == AgentInstallState.Installed;
```
False for both `NotInstalled` and `Detecting`.

**There is no `IsInstalled` duplication.** `AgentOption.IsInstalled` (`:47`) serves the
`AgentOption` record — a different type used in the default-agent selector dropdown. `AgentCardItem`
has its own `IsInstalled` at `:173`. Two types, two properties; no duplication.

**`AgentCardItem.InstallState` setter (`:138-160`):**
```csharp
set
{
    if (this.SetProperty(ref this.installState, value))
    {
        this.OnPropertyChanged(nameof(this.IsInstalled));
        this.OnPropertyChanged(nameof(this.IsProbeRunning));
        this.OnPropertyChanged(nameof(this.VersionChipText));
        this.OnPropertyChanged(nameof(this.CanDetect));
        this.OnPropertyChanged(nameof(this.CanCheckUpdates));
        // … lastSettledState update (:151-156) …
    }
}
```
Raises `IsInstalled`, `CanDetect`, and `CanCheckUpdates`, but **not** `CanUpdate`.

**`IsBlockedByRunningAgent` setter (`:227-237`):** raises `CanUpdate`.

**`UpdateAvailability` setter (`:243-255`):** raises `CanUpdate` (`:252`).

**Consumers of `CanUpdate`:**

| Consumer | File:line | Effect |
|---|---|---|
| XAML Update button binding | `src/CLIHub/Views/SettingsAgentsPage.xaml:301` | `IsEnabled="{Binding CanUpdate}"` — pure delegation, no caller gate |
| Relay command predicate | `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:155` | `card?.CanUpdate == true` — pure delegation |
| Property-changed listener | `AgentManagementViewModel.cs:746` | `case nameof(AgentCardItem.CanUpdate): UpdateCommand.RaiseCanExecuteChanged()` — reacts to notification, does not gate |

All three consumers delegate to the card. None adds its own installation-level gate on top of
`CanUpdate`.

**Caller-side gates (current production paths, for reference):**

- Load-time population (`AgentManagementViewModel.cs:859-869`): skips availability checks and
  clears `UpdateAvailability` for `NotInstalled` cards. So a not-installed card's `CanUpdate` is
  false today because `UpdateAvailability` is null — the gate is at the call site, not the property.
- Per-card Detect probe clears `UpdateAvailability` to null when it reports not installed
  (`:1164`).
- Per-card Check updates does the same (`:1105`).

**Existing `CanUpdate_*` tests (`AgentCardItemTests.cs:237-298`):** all six tests construct an
installed card via `CreateCard(AgentInstallState.Installed, …)`. No test asserts `CanUpdate == true`
for a not-installed card. The `AgentManagementViewModel` tests that assert `CanUpdate == true` use
`Detection()` which returns `IsInstalledInSystem => true` by default (confirmed at
`AgentManagementViewModelTests.Helpers.cs:61`), so every card is marked installed.

**There is no test or production path that relies on `CanUpdate == true` for a not-installed card.**
The existing assertion `Assert.False(viewModel.Cards.Single(c => c.PluginId == "b").CanUpdate)` at
`AgentManagementViewModelTests.Load.cs:52` expects the current (correct) behavior for the
not-installed card — and remains true after the change, but the true reason shifts from
"availability was cleared by the caller" to "availability was cleared **and** `IsInstalled` is
false."

**Launch-window `AgentItem.CanUpdate` (`src/CLIHub/ViewModels/Agents/AgentItem.cs:67`):**
```csharp
public bool CanUpdate => this.Plugin.Commands?.Get(AgentCommandKind.Update) != null;
```
Independent, tests only the declared command. Out of scope; must not change.

### Spec requirement mapping

- `agent-update-detection/spec.md:230-232`: "When the agent is not installed on the host, the Update
  action is disabled."
- `agent-update-detection/spec.md:249-252` — Scenario "Not installed disables the action": "WHEN the
  agent is not installed on the host / THEN the Settings card Update action is disabled and no
  availability check runs."
- `agent-ui/spec.md:428-431` — Scenario "Not installed disables Update": "WHEN an agent is not
  installed on the host / THEN 'Update' is unavailable on its card."

These three spec locations already require the behavior this change delivers. No spec delta is
needed; `skip_specs: true`.

## Decisions

### Decision 1: Add `&& this.IsInstalled` to `CanUpdate` (DECIDED)

**Current (three conditions):**
```csharp
public bool CanUpdate =>
    this.UpdateAvailability?.Status == AgentUpdateStatus.UpdateAvailable
    && this.Plugin.Commands?.Get(AgentCommandKind.Update) is not null
    && !this.IsBlockedByRunningAgent;
```

**After (four conditions):**
```csharp
public bool CanUpdate =>
    this.UpdateAvailability?.Status == AgentUpdateStatus.UpdateAvailable
    && this.Plugin.Commands?.Get(AgentCommandKind.Update) is not null
    && !this.IsBlockedByRunningAgent
    && this.IsInstalled;
```

**Rationale:**
1. The spec already requires it (see evidence above). This is a conformance fix.
2. `IsInstalled` is a derivation of `InstallState` — zero-cost property access.
3. `IsInstalled` is placed last in the expression so the three existing conditions short-circuit
   identically for an installed card (no behavior change for installed cards).
4. The existing caller-side gates (clearing availability for not-installed) remain in place and are
   harmless — they become redundant but not conflicting.

**Alternative — gate only in the caller (rejected):** keep `CanUpdate` as-is and add a check in
`AgentManagementViewModel.UpdateCommand.CanExecute` or the XAML. Rejected because (a) the spec says
the card's action is disabled when the agent is not installed — the card, not the command, should
own it; (b) keeping the gate call-site-local risks a future consumer forgetting it.

**Alternative — test `InstallState != AgentInstallState.Installed` directly (rejected):** using
`this.InstallState == AgentInstallState.Installed` instead of `this.IsInstalled` is equivalent and
purely stylistic. Rejected because `IsInstalled` is the established, self-documenting derivation.

### Decision 2: Raise `CanUpdate` notification from `InstallState` setter (DECIDED)

Add `this.OnPropertyChanged(nameof(this.CanUpdate));` to the `InstallState` setter, immediately
before the `lastSettledState` block (`:151`). The setter already raises five derived-property
notifications at `:145-149`; `CanUpdate` becomes the sixth.

**Rationale:**
1. `IsInstalled` changes → `CanUpdate` changes. Binding consumers (XAML `IsEnabled`, relay command
   `CanExecute`) must see the change immediately.
2. The `UpdateAvailability` setter already raises `CanUpdate` and the `PropertyChanged` handler in
   `AgentManagementViewModel:746` raises `UpdateCommand.RaiseCanExecuteChanged()` — so the existing
   plumbing works once the notification fires.

**Alternative — raise `CanUpdate` only from `IsInstalled` (rejected):** `IsInstalled` is not a
setter; it derives from `InstallState`. Raising from `InstallState` is the natural place.

### Decision 3: `Detecting` also disables `CanUpdate` (DECIDED)

When `InstallState` is `Detecting`, `IsInstalled` is false, so `CanUpdate` becomes false. This is
correct behavior:
- A card whose installation state is being probed should not allow an update — the probe could
  report "not installed," and launching an update during a probe is a race.
- The header-wide Detect pass (`AgentManagementViewModel.CanDetect`) already disables both header
  actions and per-card actions; this is an additional, narrower guard at the card's own property
  level.
- The `Detecting` state is transient (seconds, not minutes). The card returns to `Installed` or
  `NotInstalled` when the probe settles, and `CanUpdate` re-derives correctly.

**Alternative — allow `CanUpdate` while detecting (rejected):** would permit an update to be
launched against an agent whose probe might then report it missing. The transient unavailability is
the safer choice.

### Decision 4: No change to `AgentManagementViewModel` or XAML (DECIDED)

The `UpdateCommand` relay predicate (`:155`) is `card?.CanUpdate == true` — pure delegation. The
XAML binding (`SettingsAgentsPage.xaml:301`) is `IsEnabled="{Binding CanUpdate}"` — pure
delegation. Both automatically follow the card-level change. No other consumer adds an
installation-level gate on top of `CanUpdate`.

## Consequences

- A not-installed card with all three existing conditions met — availability `UpdateAvailable`,
  declared `update` command, no running process — now has `CanUpdate == false` at the property
  level. This matches the spec.
- The load-time population pass's clearing of availability for not-installed cards
  (`:859-865`) becomes redundant for `CanUpdate` but stays as a correctness guard — it also
  affects `HasUpdateAvailable` and `UpdateAvailabilityText`, which are not gated by `IsInstalled`
  and remain independent.
- Flipping `InstallState` from `Installed` to `NotInstalled` now fires a `CanUpdate` property-changed
  notification. The existing `PropertyChanged` listener at `AgentManagementViewModel:746`
  reacts by calling `UpdateCommand.RaiseCanExecuteChanged()`, so the command transitions correctly.
- The `Detecting` state disables `CanUpdate` transiently. No production path relied on `CanUpdate`
  staying true mid-probe — the header and per-card busy flags already block per-card actions during
  a probe.

## Open Questions

1. **Should the load-time availability clear for not-installed cards (`:859-865`) be removed as
   dead code?** It is now redundant for `CanUpdate` but still affects `HasUpdateAvailable` and
   `UpdateAvailabilityText`, which derive independently from `UpdateAvailability` without an
   `IsInstalled` gate. Removing it would make `HasUpdateAvailable` true for a not-installed card
   with a stale availability result, which the spec scenario "No indicator for a missing
   installation" (`agent-ui/spec.md:282-286`) forbids. **Verdict: keep the clear — it is not dead
   for the chip indicators; `CanUpdate` adds a second gate, not a replacement.**