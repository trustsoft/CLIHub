# Proposal: agent-detection-settings-page

## Why

### Increment 1: Separate Agent Detection page

The CLI Agents page currently shows three distinct concerns in its layout:
1. **Management** — the default agent selector
2. **Detection** — cache TTL and version check timeout fields
3. **Agents** — the agent cards with their test/update actions

The DETECTION section with its two numeric fields interrupts the flow between the management header and the primary agent cards. Users primarily visit the CLI Agents page to test agents, change the default agent, or run updates — the detection tuning fields are secondary system-level settings that affect agent probing behavior across the application.

Separating the detection settings into a dedicated page:
- Improves focus: CLI Agents becomes a purely agent-management page (default agent + agent cards)
- Groups detection settings with other system-level pages under "Engines & Repos"
- Matches the Settings structure where system-configuration pages are distinct from active-management pages
- Allows the detection page to show richer context: both fields' effective values (built-in default vs. custom) are displayed in the header summary cards, not just a single combined "DETECTION" card

The two numeric fields remain part of the shared typed draft, validated by `SettingsApplicationService`, and persisted atomically with the rest of the settings. The configuration keys (`agentProbeTtlMinutes`, `agentProbeTimeoutSeconds`) do not change.

### Increment 2: Enhanced summary cards and reset action

After manual smoke verification of increment 1, the following enhancements were added:

1. **CLI Agents third summary card (UPDATES)**: The CLI Agents page now shows agent update availability aggregated across all installed agents. This complements the existing AGENTS card (installed/supported split) and DEFAULT card (selected agent + version) with update-awareness surfaced at the page header level, not just per-card. Users can see at a glance whether any installed agents have available updates without scanning the card list.

2. **Agent Detection third summary card (DEFAULTS)**: The Agent Detection page now shows the built-in default values for both detection parameters in a single card. This provides immediate context for what empty fields resolve to, without requiring users to check watermarks or documentation.

3. **DETECTION section header reset action**: A "Reset" chip button in the DETECTION section header clears both detection fields to empty (built-in defaults) in one action. This mirrors the header action pattern established by the CLI Agents page (Detect/Check updates buttons) and provides a discoverable way to revert custom detection settings without manual field clearing.

## What changes

### Increment 1: New Agent Detection page

**Key:** `agent-detection`  
**Title:** "Agent Detection"  
**Description:** "Caching and timeout behavior for agent detection."  
**Group:** "Engines & Repos"  
**Position:** Immediately after "CLI Agents" in the sidebar (between "CLI Agents" and "Terminal Profiles")  
**Icon:** `IconGlyphs.Diagnostic`

**Page structure:**

1. **Page header summary row** with two cards:
   - **CACHE TTL** — effective value (e.g., "25 min"), detail distinguishes built-in default vs. custom
   - **TIMEOUT** — effective value (e.g., "15 sec"), detail distinguishes built-in default vs. custom

2. **DETECTION section** (body content):
   - "Cache TTL" field with `NumericField` control, bound to `ProbeTtlText`, `ProbeTtlWatermark`, unit "min"
   - "Version check timeout" field with `NumericField` control, bound to `ProbeTimeoutText`, `ProbeTimeoutWatermark`, unit "sec"

The page reuses the existing ViewModel properties (`ProbeTtlText`, `ProbeTimeoutText`, `ProbeTtlWatermark`, `ProbeTimeoutWatermark`) — no ViewModel changes beyond exposing formatted summary values for the header cards.

**CLI Agents page changes:**

**Remove:**
- The DETECTION section (lines 105-128 in `SettingsAgentsPage.xaml`)
- The DETECTION summary card from `AgentsSummaryCards` in `SettingsWindow.xaml` (the third card at lines 243-247)

**Keep:**
- MANAGEMENT section with the default agent selector
- AGENTS section header and cards (test, update, edit config actions)
- AGENTS and DEFAULT summary cards

The CLI Agents page reflows so the AGENTS section immediately follows MANAGEMENT, with no detection fields interrupting.

### Increment 2: Enhanced summary cards and reset action

**CLI Agents page additions:**

1. **UPDATES summary card** (third/right card in header):
   - **Value**: Number of installed agents with an available update when > 0; "None" when every installed card is `UpToDate` (or no installed agents); "Unknown" while any installed card's availability is null or `AgentUpdateStatus.Unknown`
   - **Detail**: "N of M installed agents" (singular/plural handling) when updates exist; "All installed agents up to date" when checked and none; "Run Check updates" while unknown; "No installed agents" when none installed
   - **Tone**: `SummaryCardTone.Accent` when updates exist, `Success` when checked and none, `Muted` otherwise
   - **Icon**: `IconGlyphs.Update`
   - **Live refresh**: After the load-time availability pass (`PopulateVersionsAsync`), the header check (`CheckUpdatesAsync`), a per-card check (`CheckCardUpdatesAsync`), and detection passes that clear stale availability (`DetectAsync`/`DetectCardAsync`)

2. **ViewModel aggregation properties** on `IAgentManagementSection`:
   - `UpdatesAvailableCount` — number of installed agents with `AgentUpdateStatus.UpdateAvailable`
   - `UpdatesValue` — formatted value for the UPDATES card
   - `UpdatesDetail` — formatted detail for the UPDATES card
   - `UpdatesValueTone` — tone for the UPDATES card value
   - Property changes raised from `RefreshAvailabilitySummary()` (called after load, detect passes, update checks)

**Agent Detection page additions:**

1. **DEFAULTS summary card** (third/right card in header):
   - **Value**: Built-in defaults for both parameters, e.g. "15 min · 10 sec" (derived from `AgentVersionService.DefaultTtl` / `DefaultProbeTimeout`, reusing the existing watermarks)
   - **Detail**: "Used when a field is empty"
   - **Icon**: `IconGlyphs.Diagnostic`

2. **Reset chip button** in DETECTION section header:
   - **Label**: "Reset"
   - **Icon**: `IconGlyphs.Refresh`
   - **Tooltip**: "Restore the built-in defaults"
   - **Command**: `ResetProbePolicyCommand` on `SettingsViewModel`
   - **Availability**: `CanResetProbePolicy` property — true when either field is non-empty, false when both are empty (already at defaults)
   - **Execute**: Clears both `ProbeTtlText` and `ProbeTimeoutText` to empty, raises probe policy summary changes
   - **Semantics**: Edits the draft only; Save persists, Cancel discards; no direct configuration write

3. **Shared header-chip button style**:
   - Move `AgentsHeaderChipButton` style from `SettingsAgentsPage.xaml` (local resources) to `Themes/SettingsStyles.xaml` (shared) as `SectionHeaderChipButton`
   - Both CLI Agents (Detect/Check updates) and Agent Detection (Reset) use the shared style
   - Style remains unchanged: `BasedOn="{StaticResource IconChipButton}"`, ghost chip with hover/pressed overlays, dimmed when disabled

### Settings navigation

**Updated catalog order** (`SettingsPageCatalog.AllPages` in `SettingsNavigation.cs`):
1. general
2. hotkeys
3. projects-registry
4. projects
5. agents
6. **agent-detection** (NEW, after agents)
7. terminal
8. appearance
9. telemetry
10. updates

The "Engines & Repos" group now has five pages: Projects, Project Scanning, CLI Agents, **Agent Detection**, Terminal Profiles.

### Files created (increment 1)

- `src/CLIHub/Views/SettingsAgentDetectionPage.xaml` — new page view
- `src/CLIHub/Views/SettingsAgentDetectionPage.xaml.cs` — code-behind (minimal)
- Header summary template `AgentDetectionSummaryCards` added to `SettingsWindow.xaml` resources

### Files modified

**Increment 1:**
- `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs` — add the new page entry
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — add summary card properties (`ProbePolicyCacheTtlSummary`, `ProbePolicyCacheTtlDetail`, `ProbePolicyTimeoutSummary`, `ProbePolicyTimeoutDetail`)
- `src/CLIHub/Views/SettingsWindow.xaml` — add the page host declaration, add the summary template, remove the DETECTION card from `AgentsSummaryCards`
- `src/CLIHub/Views/SettingsAgentsPage.xaml` — remove the DETECTION section (lines 105-128)
- `docs/architecture/ui.md` — update "Adding a Settings Page" section count (now ten pages), update CLI Agents description
- `docs/glossary.md` — update the DETECTION entry to reference the new page
- `README.md` — update Settings section if it enumerates pages; if the Agent Detection page is captured, add it to the images list

**Increment 2:**
- `src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs` — add update aggregation properties (`UpdatesAvailableCount`, `UpdatesValue`, `UpdatesDetail`, `UpdatesValueTone`)
- `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` — implement update aggregation, raise changes from `RefreshAvailabilitySummary()`
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — add `ResetProbePolicyCommand`, `CanResetProbePolicy`, `ProbePolicyDefaults` (summary/detail for DEFAULTS card)
- `src/CLIHub/Views/SettingsWindow.xaml` — add UPDATES card to `AgentsSummaryCards`, add DEFAULTS card to `AgentDetectionSummaryCards`
- `src/CLIHub/Views/SettingsAgentsPage.xaml` — remove local `AgentsHeaderChipButton` style (moved to shared)
- `src/CLIHub/Views/SettingsAgentDetectionPage.xaml` — add Reset chip button in DETECTION section header
- `src/CLIHub/Themes/SettingsStyles.xaml` — add shared `SectionHeaderChipButton` style (moved from `SettingsAgentsPage.xaml`)
- `docs/architecture/ui.md` — update summary-row enumeration for both pages (three cards each)
- Test updates for reset command and updates aggregation

### Test updates

**Increment 1:**
- `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs` — update expected page count (9 → 10), add `agent-detection` to expected keys, add slot metadata assertion
- `tests/CLIHub.Tests/Views/SettingsPageMetadataTests.cs` — test automatically picks up the new page from the catalog and validates the page host declaration

**Increment 2:**
- `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelTests.cs` — add focused coverage for `ResetProbePolicyCommand` (disabled at defaults, clears both fields, raises changes)
- `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs` — add focused coverage for updates summary states (not checked/unknown, all up to date, N updates, per-card check refresh)

## Capabilities

### New Capabilities

None. The page presents existing detection settings that were already part of the preferences-ui capability.

### Modified Capabilities

**preferences-ui** — the Settings sections and pages requirement changes:
- Ten pages instead of nine
- Agent Detection page listed after CLI Agents
- CLI Agents page management requirement updated: detection fields removed, section order reflects new layout
- New requirement for Agent Detection page content and summary cards

**settings-theme** — the Settings page header and summary cards requirements change:
- "Persistent page header" updated: CLI Agents summary row now names "agents and default-agent cards" (not "agents, default-agent, and detection cards"); Agent Detection page added to the summary-row enumeration with "cache TTL and timeout cards"
- "CLI Agents page summary cards" updated: two cards (AGENTS, DEFAULT) instead of three; the detection-card scenarios are retained under their original names (the validator refuses to drop them) and reworded to point at the Agent Detection page
- New requirement "Agent Detection page summary cards": two cards (CACHE TTL, TIMEOUT) with effective values, detail lines distinguishing built-in defaults from custom values, and live refresh when detection fields are edited

## Impact

**New code (increment 1):**
- `src/CLIHub/Views/SettingsAgentDetectionPage.xaml` and `.cs`
- ViewModel summary properties for the header cards
- Page entry in `SettingsNavigation.cs`
- Summary template in `SettingsWindow.xaml`

**Modified code (increment 1):**
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — summary properties
- `src/CLIHub/Views/SettingsWindow.xaml` — page host, summary template, remove DETECTION card
- `src/CLIHub/Views/SettingsAgentsPage.xaml` — remove DETECTION section
- Test updates in `SettingsNavigationTests.cs`
- Documentation updates: `docs/architecture/ui.md`, `docs/glossary.md`, potentially `README.md`

**Modified code (increment 2):**
- `src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs` — update aggregation properties
- `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` — update aggregation implementation
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` — `ResetProbePolicyCommand`, `CanResetProbePolicy`, defaults card properties
- `src/CLIHub/Views/SettingsWindow.xaml` — UPDATES card, DEFAULTS card
- `src/CLIHub/Views/SettingsAgentsPage.xaml` — remove local chip-button style
- `src/CLIHub/Views/SettingsAgentDetectionPage.xaml` — Reset chip button
- `src/CLIHub/Themes/SettingsStyles.xaml` — shared `SectionHeaderChipButton` style
- `docs/architecture/ui.md` — update both pages' summary-row enumeration
- Test updates for reset command and updates aggregation

**Spec deltas:**
- `openspec/changes/agent-detection-settings-page/specs/preferences-ui/spec.md` — updated "Settings sections and pages" (ten pages, Agent Detection added), updated "CLI Agents page management" (detection fields removed, UPDATES card added, Reset action NOT in this spec), new "Agent Detection page content" requirement (DEFAULTS card, Reset action)
- `openspec/changes/agent-detection-settings-page/specs/settings-theme/spec.md` — updated "Persistent page header" (CLI Agents three cards, Agent Detection three cards), updated "CLI Agents page summary cards" (three cards including UPDATES; detection scenarios kept and reworded), new "Agent Detection page summary cards" requirement (three cards including DEFAULTS)

**No breaking changes.** The configuration keys, validation, persistence, and save/cancel semantics are unchanged. The fields remain part of the typed draft. Existing user preferences migrate forward without modification. The Reset command edits the draft only; Save persists, Cancel discards.

## Non-goals

- Changing the configuration keys (`agentProbeTtlMinutes`, `agentProbeTimeoutSeconds`)
- Changing validation rules or watermark defaults (`AgentVersionService.DefaultTtl`, `AgentVersionService.DefaultProbeTimeout`)
- Adding new detection settings beyond the two existing fields
- Changing the Save/Cancel boundary (both fields remain part of the atomic save; Reset edits the draft only)
- Capturing the new page for README images (deferred to a follow-up task if required)
- Adding agent update actions to the UPDATES card (the card is informational only; per-card Update buttons remain the action surface)
- Auto-running Check updates on Settings window open (the update aggregation uses results from the load-time availability pass; manual check remains user-triggered)

## Dependencies

None. The change is purely a UI reorganization with no dependency on other in-flight changes.
