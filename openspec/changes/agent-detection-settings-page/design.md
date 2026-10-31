# Design: agent-detection-settings-page

## Context

The CLI Agents Settings page currently presents three concerns: management (default agent selector), detection (cache TTL and version check timeout), and agent cards. The DETECTION section interrupts the flow between management and the agent list. This change moves the detection settings to a dedicated page positioned after CLI Agents in the sidebar, improving focus and matching the pattern where system-configuration pages are distinct from active-management surfaces.

## Current state with file:line evidence

### CLI Agents page layout

**Current structure** (`src/CLIHub/Views/SettingsAgentsPage.xaml`):

1. **MANAGEMENT section** (lines 78-103):
   - Default agent selector card with `SettingCard` + `ComboBox`
   - Bound to `AgentsState.DefaultAgentOptions` and `AgentsState.SelectedDefaultAgent`

2. **DETECTION section** (lines 105-128):
   - Section header "DETECTION" (line 105)
   - `SettingsGroup` containing two `SettingCard` + `NumericField` controls:
     - "Cache TTL" field (lines 107-116): bound to `ProbeTtlText`, placeholder `ProbeTtlWatermark`, unit "min"
     - "Version check timeout" field (lines 118-127): bound to `ProbeTimeoutText`, placeholder `ProbeTimeoutWatermark`, unit "sec"

3. **AGENTS section** (lines 130-296):
   - Section header "AGENTS" with Detect/Check updates buttons (lines 130-176)
   - `ItemsControl` rendering agent cards (lines 177-296)

**Summary cards** (`src/CLIHub/Views/SettingsWindow.xaml:228-249`):

```xaml
<DataTemplate x:Key="AgentsSummaryCards">
    <controls:SpacedPanel Orientation="Horizontal" Spacing="8" StretchChildren="True">
        <controls:SummaryCard Detail="{Binding AgentsState.AvailabilitySummary}" ... Label="AGENTS" .../>
        <controls:SummaryCard Detail="{Binding AgentsState.DefaultAgentDetail}" ... Label="DEFAULT" .../>
        <controls:SummaryCard Detail="{Binding ProbePolicyDetail}" ... Label="DETECTION" .../>
    </controls:SpacedPanel>
</DataTemplate>
```

The third card shows a combined detection summary (`ProbePolicySummary` and `ProbePolicyDetail` from ViewModel).

### ViewModel detection properties

**Current properties** (`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`):

- Lines 86-87: `probeTtlText` and `probeTimeoutText` backing fields
- Lines 319-332: `ProbeTtlText` and `ProbeTimeoutText` properties (string, two-way binding)
- Lines 344-349: `ProbeTtlWatermark` and `ProbeTimeoutWatermark` (readonly, formatted defaults from `AgentVersionService`)
- Lines 583-584 in `Load()`: fields populated from draft or empty string
- Lines 649-653 in `CollectInput()`: raw text collected into `SettingsInput`

**Summary properties** (lines 485-502):
- `ProbePolicySummary` — combined display value (e.g., "15 min, 10 s")
- `ProbePolicyDetail` — distinguishes custom vs. defaults (e.g., "Custom cache and timeout" or "Built-in defaults")

These were designed for the single DETECTION card. The new page needs per-field summaries for two separate header cards.

### Settings page catalog

**Current catalog** (`src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:41-103`):

Nine pages in order:
1. general (line 43)
2. hotkeys (line 49)
3. projects-registry (line 56)
4. projects (line 63)
5. agents (line 71) — `SummaryRowKey: "AgentsSummaryCards"`
6. terminal (line 78)
7. appearance (line 84)
8. telemetry (line 90)
9. updates (line 96)

The new `agent-detection` page must be inserted after `agents` (between lines 76-77).

### Page host in SettingsWindow.xaml

**Current host grid** (`src/CLIHub/Views/SettingsWindow.xaml:457-476`):

Each page is a `UserControl` with `Visibility` bound via `PageKeyVisibility` converter and the page key as `ConverterParameter`. Nine entries currently:

- Line 458: `SettingsGeneralPage` (general)
- Line 460: `SettingsHotkeysPage` (hotkeys)
- Line 462: `SettingsTerminalPage` (terminal)
- Line 464: `SettingsAgentsPage` (agents)
- Line 466: `SettingsProjectRegistryPage` (projects-registry)
- Line 468: `SettingsProjectsPage` (projects)
- Line 470: `SettingsAppearancePage` (appearance)
- Line 472: `SettingsUpdatesPage` (updates)
- Line 474: `SettingsLogsPage` (telemetry)

A new `SettingsAgentDetectionPage` entry with `ConverterParameter=agent-detection` must be added (logically after line 464, but order in the host grid does not matter — visibility is controlled by the converter).

### Tests

**Navigation tests** (`tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs`):

- Line 26: expects 9 pages
- Line 29: enumerates all nine page keys
- Lines 72-84: slot metadata assertion for all pages

**Page metadata tests** (`tests/CLIHub.Tests/Views/SettingsPageMetadataTests.cs`):

- Line 57: expects 9 pages from `ReadAllPages()`
- The test enumerates every page's slot keys and validates against `SettingsWindow.xaml` DataTemplate keys and page host declarations

Both tests derive the expected page set from `SettingsPageCatalog.AllPages`, so adding the new page to the catalog automatically extends the test coverage.

### Documentation

**UI architecture** (`docs/architecture/ui.md`):

- Line 52: "The Settings window renders the active page's title..."
- Lines 54-56: "on Project Scanning, the projects, agents, and watcher summary cards... on CLI Agents, the agents, default-agent, and detection summary cards use it."
- Line 70-84: "Adding a Settings Page" recipe (references nine pages implicitly in step counts)

**Glossary** (`docs/glossary.md`):

- Lines 108-114: DETECTION entry defines the probe cache TTL and timeout, labels the Settings UI area "DETECTION", and notes the configuration keys remain `agentProbe*`

**README** (`README.md`):

If the Settings section enumerates pages or screenshots, it may need updating. The README image capture script (`src/CLIHub/Views/ReadmeImageCapture.cs`) currently captures three pages: appearance (line 95), projects (line 113), and agents (line 114). Whether to add agent-detection is a non-goal for this change.

## Design decisions with rationale

### Page placement: immediately after CLI Agents

**Decision:** Position "Agent Detection" between "CLI Agents" and "Terminal Profiles" in the sidebar.

**Rationale:** Detection settings are conceptually related to agents (they control agent probing behavior), so placing the page immediately after CLI Agents maintains logical grouping. It stays within "Engines & Repos" alongside other system-configuration pages (Projects, Project Scanning, Terminal Profiles).

**Rejected alternative:** Place it at the end of "Engines & Repos" (after Terminal Profiles). This would separate detection from agents visually, losing the conceptual link.

### Summary cards: two separate cards instead of one combined card

**Decision:** The Agent Detection page header shows two summary cards: CACHE TTL (effective value + detail) and TIMEOUT (effective value + detail).

**Rationale:** The CLI Agents page had one combined DETECTION card showing both values in a single line (e.g., "15 min, 10 s"). A dedicated page warrants richer context: each field gets its own card with a formatted effective value ("25 min" / "15 s") and a detail line distinguishing built-in defaults from custom values. This matches the pattern of other summary rows where each metric gets its own card (e.g., Projects page: PROJECTS, MANUAL, DISCOVERED).

**Rejected alternative:** Keep the single combined DETECTION card. This would under-utilize the summary row space and provide less context than the page deserves.

### ViewModel: add per-field summary properties, keep existing text properties

**Decision:** Add four new properties to `SettingsViewModel`:
- `ProbePolicyCacheTtlSummary` — formatted effective TTL (e.g., "25 min")
- `ProbePolicyCacheTtlDetail` — distinguishes custom vs. default (e.g., "Custom (default: 15 min)" or "Built-in default")
- `ProbePolicyTimeoutSummary` — formatted effective timeout (e.g., "15 s")
- `ProbePolicyTimeoutDetail` — distinguishes custom vs. default

Keep the existing `ProbeTtlText`, `ProbeTimeoutText`, `ProbeTtlWatermark`, `ProbeTimeoutWatermark` properties unchanged — they remain the binding targets for the input fields.

**Rationale:** The existing text properties support the typed-draft validation pattern and are already wired to the `NumericField` controls. The new summary properties are read-only display values derived from the text properties and the watermarks, computed in `Load()` or as getters. No configuration keys change; the summaries are presentation-only.

**Implementation note:** The summaries parse the text properties: if non-empty and valid, format with unit suffix; otherwise, format the watermark (default) value with a "Built-in default" detail. The detail distinguishes custom from default explicitly.

### CLI Agents page: remove DETECTION section, keep AGENTS immediately after MANAGEMENT

**Decision:** Delete the DETECTION section (lines 105-128) from `SettingsAgentsPage.xaml`. The MANAGEMENT section (default agent selector) remains at the top, followed immediately by the AGENTS section header and cards.

**Rationale:** With detection settings moved, the CLI Agents page focuses purely on agent management: selecting a default agent and testing/updating/editing individual agents. The AGENTS section header can follow MANAGEMENT with no gap, improving flow.

**No layout reflow complexity:** The DETECTION section is a clean block (one `SectionHeader` + one `SettingsGroup`). Removing lines 105-128 leaves the surrounding structure intact.

### CLI Agents summary row: remove DETECTION card, keep AGENTS and DEFAULT

**Decision:** The `AgentsSummaryCards` template keeps two cards: AGENTS (installed count + availability summary) and DEFAULT (default agent name + detail). Remove the DETECTION card (lines 243-247 in `SettingsWindow.xaml`).

**Rationale:** Detection summary belongs on the Agent Detection page, not on CLI Agents. Keeping two cards on CLI Agents maintains visual balance and focuses the summary on agent management concerns.

### Reuse existing NumericField controls and validation

**Decision:** The new page uses the same `NumericField` control + `SettingsGroup` + `SettingCard` pattern as the old DETECTION section. The fields bind to the same ViewModel properties, so validation, watermarks, and save/cancel semantics are unchanged.

**Rationale:** No new validation logic is needed. The fields are already part of the typed draft validated by `SettingsApplicationService`. Moving them to a new page is a pure UI reorganization with zero behavior change.

### Configuration keys unchanged

**Decision:** The stored keys remain `agentProbeTtlMinutes` and `agentProbeTimeoutSeconds` (camelCase). The glossary and code comments continue to reference "probe" as the umbrella term (detection + version checks).

**Rationale:** The configuration boundary is stable. Renaming keys would require migration logic with no user-facing benefit. The glossary already clarifies that "DETECTION" is the UI label while the keys use "probe."

### settings-theme spec delta required

**Decision:** A second spec delta under `specs/settings-theme/spec.md` is required because that capability defines the Settings page header behavior and summary card requirements, including the specific enumeration of which pages show which summary cards.

**Rationale:** The `settings-theme` capability owns the visual theme and layout patterns for Settings pages, including the page header structure and summary cards. The original spec (lines 242-372 in `openspec/specs/settings-theme/spec.md`) explicitly names "CLI Agents" as showing "agents, default-agent, and detection cards" and defines three CLI Agents summary cards with detection-specific scenarios. The implemented UI now has two CLI Agents cards (AGENTS, DEFAULT) and two Agent Detection cards (CACHE TTL, TIMEOUT). Leaving the settings-theme spec unmodified would create a mismatch: the spec would describe a UI that no longer exists (three CLI Agents cards with detection behavior) and would not describe the new Agent Detection page's summary cards at all.

**Implementation note:** The settings-theme delta modifies the "Persistent page header" requirement to update the summary-row enumeration sentence (CLI Agents now lists "agents and default-agent cards" instead of "agents, default-agent, and detection cards"; Agent Detection added with "cache TTL and timeout cards"). It modifies "CLI Agents page summary cards" to describe two cards instead of three and retires the detection-field part of "Cards follow live changes"; because the validator refuses to drop original scenarios, "Probe card shows the effective policy" and "Custom probe values" keep their names and are reworded to reference the Agent Detection page. It adds a new "Agent Detection page summary cards" requirement describing the two new cards with their effective-value display, detail-line logic, and live refresh behavior.

### Summary property implementation details

**Decision:** The four new ViewModel summary properties (`ProbePolicyCacheTtlSummary`, `ProbePolicyCacheTtlDetail`, `ProbePolicyTimeoutSummary`, `ProbePolicyTimeoutDetail`) were implemented as readonly properties with getters that parse the text field values and format them with units, falling back to watermark (default) values when fields are empty.

**Rationale:** Read-only getters ensure the summaries stay synchronized with the text properties without requiring explicit update logic. The properties are presentation-only display values with no side effects, so recomputation on every access is acceptable.

**Detail wording implemented:** When a field is empty (using the built-in default), the detail line shows "Built-in default". When a field contains a custom value, the detail line shows "Custom (default: N min)" or "Custom (default: N sec)" with the built-in default numeric value for comparison. This wording distinguishes custom from default and provides context for users to understand the baseline.

## Increment 2: Enhanced summary cards and reset action

### Updates aggregation on CLI Agents

**Decision:** Add a third summary card (UPDATES) to the CLI Agents page header, aggregating update availability across all installed agents. The card shows the number of agents with available updates, a detail line with context, and uses accent/success/muted tones to signal state.

**Rationale:** The load-time pass (`PopulateVersionsAsync` at lines 427-481) already checks update availability for every installed agent (line 462). The per-card check (`CheckCardUpdatesAsync` at lines 672-705) and header check (`CheckUpdatesAsync` at lines 617-662) also populate card-level `UpdateAvailability`. Aggregating these results into a header-level summary surfaces update status at a glance, without requiring users to scan individual cards. This complements the existing AGENTS card (install/support split) with a temporal concern (what needs attention now).

**Implementation model** (`src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`):

- **Aggregation source:** Count cards where `card.HasUpdateAvailable` is true (line 218: `UpdateAvailability?.Status == AgentUpdateStatus.UpdateAvailable`)
- **Value logic:**
  - If `UpdatesAvailableCount > 0`: show the count (e.g., "3")
  - If every installed card is `UpToDate` (no installed cards, or all cards checked and none have updates): "None"
  - Otherwise (any card has null or `Unknown` availability): "Unknown"
- **Detail logic:**
  - Updates exist: "N of M installed agents" (singular "1 of 3 installed agent" / plural "3 of 5 installed agents")
  - All up to date: "All installed agents up to date"
  - Unknown: "Run Check updates"
  - No installed agents: "No installed agents"
- **Tone:** `Accent` when updates exist (calls attention), `Success` when checked and none (positive outcome), `Muted` otherwise (neutral/informational)
- **Refresh triggers:** `RefreshAvailabilitySummary()` at line 375 already raises `InstalledAgentCount` / `SupportedAgentCount` / `AvailabilitySummary` / `DefaultAgentDetail`; extend it to raise the four new update-aggregation properties
- **Interface extension:** Add `UpdatesAvailableCount`, `UpdatesValue`, `UpdatesDetail`, `UpdatesValueTone` to `IAgentManagementSection` (lines 8-132) so `SettingsWindow.xaml` can bind through `AgentsState.*`

**Rejected alternative:** Show "N updates" without the "of M installed" context. This would require users to remember how many agents are installed to assess the proportion. The detail line provides that context explicitly.

### Defaults card on Agent Detection

**Decision:** Add a third summary card (DEFAULTS) to the Agent Detection page header, showing the built-in default values for both detection parameters in one card (e.g., "15 min · 10 sec").

**Rationale:** Users looking at empty detection fields see watermarks (15 / 10) inline, but the header summary cards did not surface what those defaults represent. A DEFAULTS card makes the baseline explicit: "this is what empty fields resolve to." The value reuses the existing `ProbeTtlWatermark` and `ProbeTimeoutWatermark` (lines 348 and 353), so no new constants or service calls are needed.

**Implementation model** (`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`):

- **Value:** `$"{ProbeTtlWatermark} min · {ProbeTimeoutWatermark} sec"` — formatted like the old combined `ProbePolicySummary` (line 312) but always showing defaults, not effective values
- **Detail:** "Used when a field is empty" (or equivalent concise wording)
- **Icon:** `IconGlyphs.Diagnostic` (same as the page icon, matching CACHE TTL and TIMEOUT cards)

**Rejected alternative:** Show the defaults inline in the CACHE TTL and TIMEOUT cards' detail lines. This would clutter the custom/default distinction ("Built-in default" becomes "Built-in default: 15 min used when empty"). A separate DEFAULTS card keeps the detail lines concise and groups the "what are the defaults" question in one place.

### Reset chip button

**Decision:** Add a "Reset" chip button in the DETECTION section header on the Agent Detection page, styled as a ghost chip (transparent background, hover/pressed overlays, dimmed when disabled) with the Refresh icon. The button clears both detection fields to empty (built-in defaults) in one action.

**Rationale:** Users with custom detection settings may want to revert to defaults. Without a reset action, they must manually clear both fields (delete text, then tab or click away). A header action button mirrors the CLI Agents pattern (Detect/Check updates buttons at lines 121-156) and provides a discoverable single-click revert. The command is gated: disabled when both fields are already empty (nothing to reset).

**Implementation model** (`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`):

- **Command:** `ResetProbePolicyCommand` (new `RelayCommand` in constructor)
- **Availability:** `CanResetProbePolicy` (new readonly property) — `true` when `!string.IsNullOrWhiteSpace(probeTtlText) || !string.IsNullOrWhiteSpace(probeTimeoutText)`, `false` otherwise
- **Execute:** Set `ProbeTtlText = string.Empty` and `ProbeTimeoutText = string.Empty`, then call `RaiseProbePolicyChanges()` (line 335) to refresh summary cards
- **Semantics:** Edits the draft only; Save persists, Cancel discards. No direct configuration write. Same boundary as field edits.

**XAML pattern** (`src/CLIHub/Views/SettingsAgentDetectionPage.xaml`):

```xaml
<Grid>
    <controls:SectionHeader Text="DETECTION"/>
    <controls:SpacedPanel Margin="0,18,0,8"
                          HorizontalAlignment="Right"
                          VerticalAlignment="Center"
                          Orientation="Horizontal"
                          Spacing="8">
        <Button AutomationProperties.Name="Restore the built-in defaults"
                Command="{Binding ResetProbePolicyCommand}"
                IsEnabled="{Binding CanResetProbePolicy}"
                Style="{StaticResource SectionHeaderChipButton}"
                ToolTip="Restore the built-in defaults">
            <StackPanel Orientation="Horizontal">
                <TextBlock VerticalAlignment="Center"
                           FontFamily="{StaticResource Sizing.FontFamily.Icon}"
                           FontSize="{StaticResource Sizing.FontSize.GlyphTiny}"
                           Text="{x:Static themes:IconGlyphs.Refresh}"/>
                <TextBlock Margin="6,0,0,0"
                           VerticalAlignment="Center"
                           FontSize="{StaticResource Sizing.FontSize.Section}"
                           Text="Reset"/>
            </StackPanel>
        </Button>
    </controls:SpacedPanel>
</Grid>
```

This matches the CLI Agents header chip buttons at lines 105-156 of `SettingsAgentsPage.xaml`: `SpacedPanel` right-aligned next to `SectionHeader`, chip button with icon + label, `IsEnabled` bound to availability gate.

**Rejected alternative:** Add individual "Reset to default" buttons per field. This would require two buttons (clutter) and lose the single-action discoverability. A header action is consistent with other Settings pages where batch operations (Detect all agents, Check updates for all) sit in the section header.

### Shared section-header chip-button style

**Decision:** Move the local `AgentsHeaderChipButton` style from `SettingsAgentsPage.xaml` (lines 50-75) to `Themes/SettingsStyles.xaml` as `SectionHeaderChipButton`, making it available to both CLI Agents (Detect/Check updates) and Agent Detection (Reset) pages.

**Rationale:** The style is a ghost chip with hover/pressed overlays and disabled dimming, `BasedOn="{StaticResource IconChipButton}"` (defined in `Themes/Controls.xaml`), `Padding="10,5"`, and `CornerRadius="{StaticResource Sizing.CornerRadius.Chip}"`. Duplicating it on the Agent Detection page would violate DRY. `SettingsStyles.xaml` already merges `Controls.xaml` (so `IconChipButton` resolves), making it the natural home for Settings-specific shared styles.

**Implementation note:** The style definition stays byte-for-byte identical. Only its key changes: `AgentsHeaderChipButton` → `SectionHeaderChipButton`. Both pages reference the shared key after the move. No visual or behavioral change.

**Evidence:** `src/CLIHub/Themes/SettingsStyles.xaml` at line 2 merges `<ResourceDictionary Source="Controls.xaml"/>`, so `BasedOn="{StaticResource IconChipButton}"` resolves. The merge order ensures `IconChipButton` is available before `SectionHeaderChipButton` is defined.

## Test and documentation updates

### Tests

**SettingsNavigationTests.cs:**
- Update line 26: `Assert.Equal(10, viewModel.Pages.Count);`
- Update line 29: add `"agent-detection"` to the expected keys array (after `"agents"`)
- Update lines 72-84: add the `agent-detection` slot metadata tuple: `("agent-detection", null, null, "AgentDetectionSummaryCards")`

**SettingsPageMetadataTests.cs:**
- Update line 57: `Assert.Equal(10, pages.Length);`
- The test automatically validates the new page's host declaration and summary template key because it enumerates `SettingsPageCatalog.AllPages` and inspects `SettingsWindow.xaml`.

### Documentation

**docs/architecture/ui.md:**
- Line 56: update the CLI Agents summary card list to "agents, default-agent" (remove "detection")
- Line 70-84: "Adding a Settings Page" — update any step that references a count of nine pages (now ten)

**docs/glossary.md:**
- Lines 108-114: update the DETECTION entry to reference the "Agent Detection" page instead of "CLI Agents page," clarify that the fields appear on that dedicated page

**README.md:**
- If the Settings section enumerates pages, add "Agent Detection" after "CLI Agents"
- If README images are listed, decide whether to capture the new page (deferred to manual task or follow-up)

## Alternatives considered and rejected

### Alternative: Keep detection on CLI Agents, add collapsible "Advanced" section

**Rejected.** Collapsing the fields adds UI complexity (expand/collapse state, animation) for minimal benefit. The fields are rarely changed — users set them once or never — so a dedicated page accessed via the sidebar is simpler and clearer than an in-page disclosure.

### Alternative: Place Agent Detection under "System" group instead of "Engines & Repos"

**Rejected.** Detection settings control agent behavior, so they belong conceptually with agent-related pages. The "System" group is for application-level settings (Appearance, Logs, Updates) that don't pertain to agents or projects. "Engines & Repos" is the correct home.

### Alternative: Rename the page "Agent Probing" or "Agent Cache"

**Rejected.** "Agent Detection" is the term already established in the glossary and used in the old summary card label. Users understand "detection" as the process of checking whether agents are installed and getting their versions. "Probing" is internal jargon; "Cache" describes only one field.

### Alternative: Show three summary cards (CACHE TTL, TIMEOUT, COMBINED)

**Rejected.** Redundant. The page header space accommodates two cards comfortably; adding a third combined card would repeat information already shown in the two separate cards.

## Open questions (increment 1)

1. **README image capture:** Should the new Agent Detection page be captured for README screenshots alongside Appearance, Project Scanning, and CLI Agents? The capture script (`src/CLIHub/Views/ReadmeImageCapture.cs`) currently captures three pages. If yes, line 114 would add `await CapturePageAsync(settingsWindow, settingsViewModel, "agent-detection", directory, "settings-detection.png");` and README.md would list the new image.

   **Recommendation:** Defer to a manual task after implementation. The page can be captured later if the maintainer decides the README needs it.

2. **Property naming for summaries:** The proposal uses `ProbePolicyCacheTtlSummary`, `ProbePolicyCacheTtlDetail`, `ProbePolicyTimeoutSummary`, `ProbePolicyTimeoutDetail`. Should they be shortened (e.g., `CacheTtlSummary`) or keep the `ProbePolicy` prefix for consistency with the existing `ProbePolicySummary`?

   **Recommendation:** Keep the `ProbePolicy` prefix for consistency. The existing combined property is `ProbePolicySummary`, so the new per-field properties should follow the same namespace.

3. **Summary card detail wording:** When a field is empty (using the default), the detail line could say "Built-in default" or "Default (15 min)". Which is clearer?

   **Recommendation:** "Built-in default" is concise and matches the pattern used elsewhere (e.g., the Appearance page accent chip says "Sky (default)"). If showing the numeric default helps, use "Built-in default (15 min)" but only for the CACHE TTL card where the numeric value is meaningful. The implementer may refine wording during implementation.

4. **Order of summary cards:** Should CACHE TTL come before TIMEOUT, or the reverse?

   **Recommendation:** CACHE TTL first, TIMEOUT second. This matches the field order on the page (Cache TTL is listed before Version check timeout) and the logical flow (cache duration before individual operation timeout).

## Increment 2: Fixed design decisions (not open for re-litigation)

The following decisions are specified in the requirements and must be implemented as-is:

1. **CLI Agents UPDATES card position and content:** Third/right card, icon `IconGlyphs.Update`, value/detail/tone as specified, refresh triggers from existing passes
2. **Agent Detection DEFAULTS card position and content:** Third/right card, icon `IconGlyphs.Diagnostic`, value reuses watermarks, detail "Used when a field is empty"
3. **Reset button placement and behavior:** DETECTION section header, right-aligned `SpacedPanel`, `IconGlyphs.Refresh`, clears both fields to empty, disabled when both empty
4. **Shared style location:** `SectionHeaderChipButton` in `Themes/SettingsStyles.xaml`, used by both pages
5. **Updates aggregation interface:** Properties on `IAgentManagementSection`, raised from `RefreshAvailabilitySummary()`
6. **Reset command semantics:** Draft-only edit, Save persists, Cancel discards, no direct configuration write
