# Design: remove-availability-summary

## Verified Current State

All citations verified against the current tree. Working tree is clean except an unrelated untracked `code-review.md` (ignored for this change).

### What is being removed

| Concern | Today | Evidence |
|---|---|---|
| Interface declaration | `string AvailabilitySummary { get; }` with doc comment "The supported/not-installed split shown under the installed count." | `src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs:45-48` |
| Implementation | `public string AvailabilitySummary => this.SupportedAgentCount switch { 0 => "No agent plugins found", var supported when this.NotInstalledAgentCount == 0 => $"{supported} supported · all installed", var supported => $"{supported} supported · {this.NotInstalledAgentCount} not installed" };` | `AgentManagementViewModel.cs:243-249` |
| Backing counter | `public int NotInstalledAgentCount => this.SupportedAgentCount - this.InstalledAgentCount;` with doc comment | `AgentManagementViewModel.cs:238-241` |
| `NotInstalledAgentCount` consumers | Only `AvailabilitySummary` (lines 247-248) and its own `OnPropertyChanged` (line 804). Grep across the repo: 4 matches, all in `AgentManagementViewModel.cs`. No interface member, no XAML, no test, no other consumer. | grep `NotInstalledAgentCount` |
| Raise sites | `RefreshAvailabilitySummary()` (line 800) raises both deleted members at lines 804 (`NotInstalledAgentCount`) and 805 (`AvailabilitySummary`). | `AgentManagementViewModel.cs:800-813` |

### Zero bindings confirmed

- `grep AvailabilitySummary --include *.xaml` → **no matches**.
- AGENTS summary card: `Value="{Binding AgentsState.InstalledAgentCount}"`, `Detail="{Binding AgentsState.PluginCatalogSummary}"` | `SettingsWindow.xaml:201-204`
- General card: `Value="{Binding AgentsInstalledCount}"`, `Detail="{Binding AgentsSupportSummary}"` | `SettingsWindow.xaml:162-165`
- `SettingsViewModel.AgentsSupportSummary` derives from `AgentsSupportedCount` (→ `AgentsState.SupportedAgentCount`), not from `AvailabilitySummary` | `SettingsViewModel.cs:503-510`
- `SettingsViewModel.OnAgentsStatePropertyChanged` reacts only to `InstalledAgentCount` / `SupportedAgentCount` | `SettingsViewModel.cs:691-702`

No spec references the property: `grep AvailabilitySummary openspec/specs` → **no matches**.

### Test inventory (8 assertions)

| File:line | Test | Today |
|---|---|---|
| `Detection.cs:27` | `DetectCardCommand_UpdatesInstallationAndVersionWithoutAvailabilityCheck` | `Assert.Equal("1 supported · all installed", viewModel.AvailabilitySummary);` (pre-detect) |
| `Detection.cs:40` | same test | `Assert.Equal("1 supported · all installed", ...)` (post-detect) |
| `Detection.cs:64` | `DetectCommand_UpdatesCardsSummaryAndOptionAvailability` | `Assert.Equal("2 supported · 1 not installed", ...)` (pre-detect) |
| `Detection.cs:71` | same test | `Assert.Equal("2 supported · all installed", ...)` (post-detect) |
| `Detection.cs:286` | `DetectCommand_WhenProbeThrowsAfterACardSettled_KeepsThatCardAndRestoresTheRest` | `Assert.Contains(nameof(AgentManagementViewModel.AvailabilitySummary), raised);` (PropertyChanged) |
| `Load.cs:37` | `Load_BuildsCardsCountsAndDefaultOptions` | `Assert.Equal("2 supported · 1 not installed", ...)` |
| `Load.cs:161` | `Load_WithEveryAgentInstalled_ReportsAllInstalled` | `Assert.Equal("2 supported · all installed", ...)` |
| `Load.cs:174` | `Load_WithNoPlugins_ReportsNoAgentPlugins` | `Assert.Equal("No agent plugins found", ...)` |

### Refresh method

`RefreshAvailabilitySummary()` (line 800) is defined once and called from 10 sites (lines 599, 686, 903, 988, 1038, 1054, 1138, 1153, 1181, 1198). It raises 11 property-changed notifications:
`InstalledAgentCount`, `SupportedAgentCount`, `NotInstalledAgentCount`, `AvailabilitySummary`, `DefaultAgentDetail`, `UpdatesAvailableCount`, `UpdatesValue`, `UpdatesDetail`, `UpdatesValueTone`, `HasDisabledPlugins`, `PluginCatalogSummary`.

## Decisions

### D1 — Remove `AvailabilitySummary` and `NotInstalledAgentCount` (DECIDED: remove both)

`AvailabilitySummary` has no binding, no consumer, and a stale doc comment. `NotInstalledAgentCount` exists only to feed it. Removing both is the backlog entry's proposed remedy, user-approved. `SupportedAgentCount` and `InstalledAgentCount` stay — `SettingsViewModel` reads them for the two summary cards, and tests must keep pinning them.

### D2 — Rename `RefreshAvailabilitySummary()` → `RefreshSummarySurfaces()` (DECIDED: rename)

After removal, the method name would reference a deleted member. Its real job is raising change notifications for every derived summary surface: the two counts, the default-agent detail, the four Update-aggregation properties, the disabled-plugin visibility gate, and the plugin-catalog summary. `RefreshSummarySurfaces()` is truthful. The rename is fully mechanical: the method is `private`, all 10 call sites and the definition are in one file, and no test references it by name. No doc-comment change beyond the method name is needed (the method has no dedicated summary beyond the existing class-level behavior it triggers).

Alternatives rejected:
- **Keep the name** — misleading after the removed member, invites a future "why is this called AvailabilitySummary?".
- **Rename to `RefreshDerivedSummaries()` / `RefreshHeaderCounts()`** — `RefreshSummarySurfaces()` better captures that it also raises `DefaultAgentDetail` (not a count) and `PluginCatalogSummary`.

### D3 — Test migration (DECIDED: assert counts, never delete coverage)

Each string assertion is rewritten to assert `SupportedAgentCount` / `InstalledAgentCount`, which are the invariants the removed string encoded:

| File:line | Migration |
|---|---|
| `Load.cs:37` | **Delete the line.** Counts are already asserted at lines 35-36 (`InstalledAgentCount == 1`, `SupportedAgentCount == 2`); the string adds nothing. |
| `Load.cs:161` | Replace with `Assert.Equal(2, viewModel.SupportedAgentCount);` and `Assert.Equal(2, viewModel.InstalledAgentCount);` |
| `Load.cs:174` | **Delete the line.** Counts are already asserted at lines 172-173 (`InstalledAgentCount == 0`, `SupportedAgentCount == 0`). |
| `Detection.cs:27` | Replace with `Assert.Equal(1, viewModel.SupportedAgentCount);` and `Assert.Equal(1, viewModel.InstalledAgentCount);` (pins the pre-detect state). |
| `Detection.cs:40` | Replace with `Assert.Equal(1, viewModel.InstalledAgentCount);` (post-detect; `SupportedAgentCount` is unchanged and already pinned at line 27). |
| `Detection.cs:64` | Replace with `Assert.Equal(2, viewModel.SupportedAgentCount);` and `Assert.Equal(1, viewModel.InstalledAgentCount);` (b not installed pre-detect). |
| `Detection.cs:71` | Replace with `Assert.Equal(2, viewModel.InstalledAgentCount);` (post-detect, both installed). |
| `Detection.cs:286` | Replace `AvailabilitySummary` with `InstalledAgentCount` → `Assert.Contains(nameof(AgentManagementViewModel.InstalledAgentCount), raised);`. This preserves the assertion's real purpose: that `RefreshSummarySurfaces()` is re-run after an aborted pass, and `InstalledAgentCount` is still raised by it. |

Where a count assertion already exists on the same `Act` (Load.cs:37, Load.cs:174), the string assertion is removed outright rather than duplicated — the coverage is the count, already present. This is not "weakening a test to pass" (principle 21): no invariant is dropped, the redundant string form of an already-pinned invariant is removed.

### D4 — `skip_specs: true` (DECIDED)

The removed property has no spec-level contract (`grep AvailabilitySummary openspec/specs` → no matches) and no user-visible behavior. Per the repository's documented escape hatch (see `2026-10-24-hygiene-sweep` precedent), the change opts out of a spec delta via `.openspec.yaml`.

### D5 — Backlog resolution

Append `**Status: Resolved by remove-availability-summary.**` to the entry at `docs/backlog.md:327-339` (the trigger "next change touching `IAgentManagementSection`" fired). The entry is retained with the status line, matching the repo's practice of keeping resolved entries for history rather than deleting them.

## Deviations

None.

## Open Questions

- **OQ-1** — Whether the resolver prefers removing the backlog entry outright rather than adding a `**Status:**` line. The brief specifies "Resolve ... with a `**Status: Resolved by remove-availability-summary.**` line", so the plan follows that; the implementer does not need to ask unless the user reopens it.
- **OQ-2** — The exact replacement method name (`RefreshSummarySurfaces`). The brief permits keeping the old name or choosing a truthful one. D2 chooses `RefreshSummarySurfaces`; the implementer should confirm with the orchestrator only if that name is contested, otherwise proceed.