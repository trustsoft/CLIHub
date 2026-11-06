# Proposal: Remove Agent Origin Badge

## Why

In Settings -> CLI Agents, each agent card currently shows three chips in a row: a state/version chip (showing installed version, "Installed", "Not installed", or "Detecting…"), an origin badge (`Built-in`/`Installed`/`Unknown`), and, when applicable, an update chip that replaces the state chip per the most recent `update-chip-replaces-version` change. When an agent has an unresolved version, the state chip displays "Installed", producing a visual redundancy: two consecutive chips reading "Installed" — the state chip and the origin badge — side by side.

The user asked why the origin badge exists and, when offered the choice, chose to remove the badge entirely. The badge's information (origin label plus the descriptor's declared version, publisher, and homepage) will move into the agent name's tooltip, so the information stays discoverable without requiring a separate visual element in the row.

## What Changes

- **`src/CLIHub/ViewModels/Agents/AgentCardItem.cs`**: replace `OriginBadgeText` and `OriginBadgeTooltip` with a single tooltip property (e.g., `IdentityTooltip`) that presents the recorded origin as a labelled line ("Origin: Built-in", "Origin: Installed", or "Origin: Unknown") followed by the descriptor's version, publisher, and homepage when declared, using the same "Label: value" format currently in `OriginBadgeTooltip`.
- **`src/CLIHub/ViewModels/Agents/DisabledAgentCardItem.cs`**: mirror the same tooltip structure (origin + version; no publisher/homepage available for disabled plugins).
- **`src/CLIHub/Views/SettingsAgentsPage.xaml`**: 
  - Bind `ToolTip="{Binding IdentityTooltip}"` on the agent name `TextBlock` in the active and disabled card templates (lines ~250 and ~395).
  - Delete the two origin `Border` blocks and their child `TextBlock` elements from the active template (~267-273) and disabled template (~401-407).
  - Delete the two now-unused styles `AgentOriginChip` and `AgentOriginChipText` (~55-70).
- **Tests (`tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Plugins.cs`)**: update the four methods that assert origin badge text/tooltip to instead assert the new tooltip contract: `Load_WithDisabledPlugins_PopulatesDisabledCardsAndSectionFlag` (~188), `Load_WithOriginsFromState_PopulatesOriginBadgesAndCatalogSummary` (~224), `Load_OriginBadgeTooltip_ListsDescriptorMetadataWhenDeclared` (~248), `Load_OriginBadgeTooltip_FallsBackToBadgeTextWithoutMetadata` (~272). Rename them where they name "badge" if the new name (e.g., "IdentityTooltip") makes the old name misleading.
- **Spec deltas**:
  - `openspec/specs/agent-ui/spec.md`: MODIFIED `Agent card displayed content` (keep the requirement name and scenario names): add that the card SHALL NOT render a separate origin badge and SHALL present the origin plus descriptor metadata in the agent name's tooltip; reword the two scenarios that anchor positions "before the origin badge" to use "immediately after the name" or "in its usual position immediately after the name" instead.
  - `openspec/specs/plugin-installation/spec.md`: REMOVED `Plugin origin display` (the badge-specific requirement is removed; the replacement behavior is stated in `agent-ui`).

## Non-Goals

- No palette, style, or visual changes to the remaining chips (state chip, update chip).
- No change to the update-chip replacement behavior (it still replaces the state chip when an update is available).
- No new control classes; this is property and XAML edits only.
- The `Origin` property itself stays on `AgentCardItem` — it is still used by `AgentManagementViewModel` to compute the catalog summary counts (lines 156-157 of `AgentManagementViewModel.cs`).

## Impact

**Removed from UI:**
- Two `Border` blocks with `Style="AgentOriginChip"` and child `TextBlock` elements (one in active template, one in disabled template).
- Two XAML styles: `AgentOriginChip`, `AgentOriginChipText`.

**Added to UI:**
- `ToolTip` binding on the agent name `TextBlock` in two card templates.

**ViewModel changes:**
- `AgentCardItem`: remove `OriginBadgeText`, `OriginBadgeTooltip`; add `IdentityTooltip` (or similar) with combined origin + metadata.
- `DisabledAgentCardItem`: remove `OriginBadgeText`; add equivalent identity tooltip (origin + version only).

**Test changes:**
- Four test methods updated to assert the new tooltip contract instead of badge text; do not delete coverage.

**Spec changes:**
- `agent-ui`: One MODIFIED requirement (`Agent card displayed content`); existing scenario names preserved; two scenario THEN clauses reworded to remove "before the origin badge" position anchor; one new scenario added for the tooltip contract.
- `plugin-installation`: One REMOVED requirement (`Plugin origin display`); the badge-display behavior is removed and replaced by the tooltip contract in `agent-ui`.

## Dependencies

None. This change is self-contained and does not depend on or block any other in-flight change.
