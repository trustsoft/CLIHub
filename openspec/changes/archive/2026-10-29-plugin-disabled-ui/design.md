# Design: Disabled-plugin UI surface for invalid catalog entries

## Verified Current State

### Agent Management Surface

**File:** `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`

- **Line 293:** `Load(string? defaultAgentId)` calls `catalog.GetAllPlugins()` to populate cards.
- **Line 305-320:** Cards are created from valid plugins only; `AgentCardItem` constructor takes a `Plugin` (line 310), `isInstalled` flag (line 313), and optional version/config path.
- **Line 323:** `RefreshAvailabilitySummary()` is called after populating cards - this updates `InstalledAgentCount` (line 89) and `SupportedAgentCount` (line 92) derived from `Cards` collection.
- **Line 51:** Catalog subscribes to `PluginsChanged` event (not shown in excerpt, but pattern is reload-on-change).

**File:** `src/CLIHub/ViewModels/Agents/AgentCardItem.cs`

- **Line 60-86:** `AgentCardItem` constructor requires a non-null `Plugin` (line 78 throws on null).
- **Line 133-146:** `InstallState` property derives `IsInstalled`, `VersionChipText`, `CanDetect`, `CanCheckUpdates`.
- **Line 159-165:** `VersionChipText` returns "Detecting..." / "Not installed" / "v{version}" / "Installed" based on `InstallState`.
- **Line 186-191:** `CanUpdate` is a simple bool property, written by the view model.
- **Line 269:** `CanDetect` is `!IsDetecting && !IsCheckingUpdates`.
- **Line 278:** `CanCheckUpdates` is `IsInstalled && !IsDetecting && !IsCheckingUpdates`.
- **Line 288-303:** `Message` property is nullable string; `HasMessage` derived from it.

### XAML Card Rendering

**File:** `src/CLIHub/Views/SettingsAgentsPage.xaml`

- **Line 168-287:** `ItemsControl` renders cards via `DataTemplate`.
- **Line 171-176:** Card container is a `Border` with popover background, field stroke, and corner radius.
- **Line 184-190:** Logo renders from `LogoPath` with `PathToImage` converter.
- **Line 192-219:** Left column shows name, state chip, update chip, and "Supported commands" hint text.
- **Line 221-282:** Right column shows action buttons (Detect, Check updates, Update, Edit config) and message line.
- **Line 225-236:** Detect button bound to `DataContext.AgentsState.DetectCardCommand` with `CanDetect` enabling.
- **Line 237-248:** Check updates button bound to `CheckCardUpdatesCommand` with `CanCheckUpdates` enabling.
- **Line 249-260:** Update button bound to `UpdateCommand` with `CanUpdate` enabling.
- **Line 261-272:** Edit Config button bound to `EditConfigCommand` with `CanEditConfig` enabling.
- **Line 274-281:** Message line bound to `Message`, visible when `HasMessage` is true, right-aligned.

### Launch Window Agent Pane (Confirmed Valid-Only)

**File:** `src/CLIHub/ViewModels/Agents/AgentPaneController.cs`

- **Line 99:** `AgentListComposer.Compose()` receives `pluginCatalog.GetAllPlugins()` - consumes valid plugins only.
- Conclusion: Launch window agent selector will remain valid-only after this change; no modifications needed there.

### Validation Result Shapes

**File:** `src/CLIHub.Core/Plugins/InvalidPluginEntry.cs`

- **Line 12-15:** Record with `PluginDirectory` (string), `PartialPlugin` (Plugin?), `ReadResult` (PluginDescriptorReadResult?), `ValidationResult` (PluginValidationResult?)`.
- Exactly one of `ReadResult` or `ValidationResult` is non-null; `PartialPlugin` is null when deserialization failed.

**File:** `src/CLIHub.Core/Plugins/PluginDescriptorReadResult.cs`

- **Line 12:** `record PluginDescriptorReadResult(Plugin? Plugin, Exception? Error)`
- When deserialization fails, `Error` contains the exception.

**File:** `src/CLIHub.Core/Plugins/PluginValidationResult.cs`

- **Line 20:** `IReadOnlyList<string> Errors { get; }` - validation errors in rule evaluation order.
- **Line 25:** `bool IsValid => Errors.Count == 0`.

### Existing Spec Coverage

**File:** `openspec/specs/agent-ui/spec.md`

- **Line 219-286:** "Requirement: Agent card displayed content" defines what a valid agent card shows (name, logo, state chip, commands, update indicator).
- **Line 317-446:** "Requirement: Agent card actions" defines Detect, Check updates, Update, Edit Config button behaviors for valid cards.
- **Line 100-105:** `AvailabilitySummary` derives from `SupportedAgentCount` - this is the "X supported / Y not installed" line.

**Verdict:** The spec has no requirement for invalid-card rendering yet. This change ADDS a new requirement to the `agent-ui` capability.

## Chosen Design

### Invalid Card Model

Create a parallel card type `InvalidAgentCardItem` to avoid polluting `AgentCardItem` (which assumes a non-null `Plugin`) with nullable states:

- **Properties:**
  - `string PluginDirectory` - the directory path (shown when no name is available).
  - `string? Name` - from `PartialPlugin?.Name` or fallback to directory name.
  - `string? LogoPath` - null (no logo for invalid entries; shows default).
  - `string Reason` - composed failure reason.
  - `bool IsEnabled` - always `false`.
  - `string DisabledReasonDetail` - same as `Reason`, or separate if needed for styling.

### Reason Composition

Deterministic composition from `InvalidPluginEntry`:

```csharp
string ComposeReason(InvalidPluginEntry entry)
{
    if (entry.ValidationResult is { Errors: var errors } && errors.Count > 0)
    {
        return string.Join("; ", errors);
    }

    if (entry.ReadResult?.Error is { } ex)
    {
        return ex.Message;
    }

    // Fallback when PartialPlugin is null and no error message
    return $"Plugin directory: {entry.PluginDirectory}";
}
```

### ViewModel Changes

**File:** `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`

1. Add `ObservableCollection<InvalidAgentCardItem> InvalidCards { get; }` property.
2. In `Load()` (line 283):
   - Call `catalog.GetInvalidPlugins()` after `GetAllPlugins()`.
   - Populate `InvalidCards` collection with `InvalidAgentCardItem` instances.
   - Keep valid `Cards` population unchanged.
3. Keep `InstalledAgentCount` and `SupportedAgentCount` unchanged (they derive from `Cards`, which remains valid-only per requirements).
4. Reload path: `PluginsChanged` event already triggers `Load()`, so both valid and invalid sets refresh automatically.

### XAML Changes

**File:** `src/CLIHub/Views/SettingsAgentsPage.xaml`

1. After the existing `ItemsControl` for valid cards (line 168-287), add a second `ItemsControl` bound to `AgentsState.InvalidCards`.
2. Invalid-card template:
   - Same card container shape (Border with popover background, field stroke, corner radius).
   - Grayed-out appearance: set `Opacity="0.6"` on the entire card or use `DisabledTextBrush` for text elements.
   - Show name (or directory path fallback), no state chip (or a "Invalid" chip with danger styling).
   - Show `Reason` text in place of the "Supported commands" line or as a persistent message line.
   - No action buttons (Detect/Check updates/Update/Edit Config all hidden or disabled).
   - No logo (or show default logo with reduced opacity).

### XAML Styling Decision

**Chosen approach:** Render invalid cards with reduced opacity (0.6) and a persistent "Invalid" or "Disabled" state chip styled with danger brush, and show the reason text where the "Supported commands" hint would be. All action buttons are omitted (not just disabled) to reinforce that no actions are possible.

### Spec Delta Mapping

**Capability:** `agent-ui`

**New Requirement:** "Requirement: Invalid plugin entry display"

**Scenario Names (Proposed):**

- "Invalid entry shown as disabled card"
- "Validation errors shown as reason"
- "Read errors shown as reason"
- "Invalid cards are visually distinct"
- "No actions on invalid cards"
- "Valid cards unchanged by invalid entries"
- "Reload refreshes both valid and invalid sets"

These are NEW scenarios; no MODIFIED scenarios (no existing scenario names to preserve).

## Alternatives Rejected

### Alternative 1: Reuse AgentCardItem with nullable Plugin

**Rejected because:** `AgentCardItem` constructor throws on null `Plugin` (line 78), and all its properties (`PluginId`, `Name`, `CommandLabels`) derive from the non-null plugin. Making `Plugin` nullable would require nullable checks throughout and pollute the valid-card model with invalid-card concerns. A separate card type keeps concerns isolated.

### Alternative 2: Show invalid cards inline with valid cards in one list

**Rejected because:** Mixing valid and invalid entries in one `ObservableCollection<AgentCardItem>` would require a common base or interface, complicating card type discrimination in XAML and view-model logic. Separate collections allow XAML to render valid and invalid cards with distinct templates cleanly.

### Alternative 3: Show reason as a tooltip instead of inline text

**Rejected because:** Inline text is more discoverable than a tooltip (user does not need to hover). Persistent reason text ensures the diagnostic is always visible.

### Alternative 4: Allow Edit Config action on invalid cards

**Rejected because:** The config path may not be resolvable when deserialization failed (`PartialPlugin` is null). Allowing Edit Config only when a partial plugin is available adds complexity; simpler to disable all actions consistently.

## Open Questions

1. **Should invalid cards sort before or after valid cards?** Recommendation: After valid cards, in directory order, so valid agents remain at the top.
2. **Should the "Supported agents" count include invalid entries?** Recommendation: No - keep it valid-only per current semantics. Add a separate "(X invalid)" note if count is non-zero, or show invalid count in a separate line.
3. **Should the invalid-card template show a default logo or no logo?** Recommendation: Show the default logo with reduced opacity to maintain card structure consistency.
4. **Should the reason text be truncated with ellipsis if very long?** Recommendation: Yes - apply `TextTrimming="CharacterEllipsis"` and show full text in tooltip. Set `MaxWidth` to match the card's text column width (from existing `Sizing.AgentRow.TextMaxWidth` resource if available).
5. **Should reload log a warning for each invalid entry?** Recommendation: Defer to catalog implementation (change A); view model does not duplicate logging.

## Deviations from Brief

None. The design implements exactly what was specified: invalid entries visible as disabled cards with reasons, valid agents unchanged, reload refreshes both, no launch window changes.
