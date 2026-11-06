# Tasks: Remove Agent Origin Badge

Sized for a fresh session with no prior context beyond this change's `proposal.md` and `design.md`.

## Stage 1: Update ViewModels and tests together

- [x] 1.1 In `src/CLIHub/ViewModels/Agents/AgentCardItem.cs` (starting at line 320), replace `OriginBadgeText` (~324) and `OriginBadgeTooltip` (~335) with a single property `IdentityTooltip` that:
  - Always starts with `"Origin: {Built-in|Installed|Unknown}"` (using the same switch on `this.Origin` as the current `OriginBadgeText`)
  - Adds `"Version: {value}"` when `this.Plugin.Version` is not null
  - Adds `"Publisher: {value}"` when `this.Plugin.Publisher` is not null
  - Adds `"Homepage: {value}"` when `this.Plugin.Homepage` is not null
  - Joins all lines with `"\n"` (multiline tooltip)
  - See `design.md` Decision 1 for the exact code shape
- [x] 1.2 In `src/CLIHub/ViewModels/Agents/DisabledAgentCardItem.cs` (starting at line 47), replace `OriginBadgeText` (~51) with a property `IdentityTooltip` that:
  - Always starts with `"Origin: {Built-in|Installed|Unknown}"` (using the same switch on `this.Origin`)
  - Adds `"Version: {value}"` when `this.Version` is not null
  - Joins all lines with `"\n"`
  - See `design.md` Decision 1 for the exact code shape
- [x] 1.3 Keep the `Origin` property unchanged in both files; it is still used by `AgentManagementViewModel.cs:156-157` for catalog summary counts.
- [x] 1.4 In `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.Plugins.cs`, method `Load_WithDisabledPlugins_PopulatesDisabledCardsAndSectionFlag` (~line 188):
  - Replace assertions on `installed.OriginBadgeText` and `unknown.OriginBadgeText` (lines 202, 205) with assertions that `installed.IdentityTooltip` starts with `"Origin: Installed"` and `unknown.IdentityTooltip` starts with `"Origin: Unknown"`.
  - Optional: rename the method if the current name is misleading (e.g., to `Load_WithDisabledPlugins_PopulatesDisabledCardsIdentityAndSectionFlag`), or leave it as-is if the name still fits.
- [x] 1.5 In the same file, method `Load_WithOriginsFromState_PopulatesOriginBadgesAndCatalogSummary` (~line 224):
  - Replace assertions on `OriginBadgeText` (lines 242-244) with assertions that the three cards' `IdentityTooltip` properties start with `"Origin: Built-in"`, `"Origin: Installed"`, and `"Origin: Unknown"` respectively.
  - Rename the method to `Load_WithOriginsFromState_PopulatesOriginDataAndCatalogSummary` (see `design.md` Decision 4).
- [x] 1.6 In the same file, method `Load_OriginBadgeTooltip_ListsDescriptorMetadataWhenDeclared` (~line 248):
  - Replace `var tooltip = viewModel.Cards.Single(c => c.PluginId == "a").OriginBadgeTooltip;` with `var tooltip = viewModel.Cards.Single(c => c.PluginId == "a").IdentityTooltip;`.
  - Add an assertion that `tooltip` contains `"Origin: Installed"` (since the state file sets the origin to `Installed` at line 256).
  - Keep the existing three assertions for `"Version: 1.2.0"`, `"Publisher: Community"`, and `"Homepage: https://example.com/a"`.
  - Rename the method to `Load_IdentityTooltip_ListsOriginAndDescriptorMetadataWhenDeclared`.
- [x] 1.7 In the same file, method `Load_OriginBadgeTooltip_FallsBackToBadgeTextWithoutMetadata` (~line 272):
  - Replace the assertion `Assert.Equal(card.OriginBadgeText, card.OriginBadgeTooltip);` with an assertion that `card.IdentityTooltip` equals `"Origin: Unknown"` (the origin label alone, since no descriptor metadata is declared and the state file entry does not exist).
  - Rename the method to `Load_IdentityTooltip_ShowsOriginLabelWhenNoMetadataDeclared`.

**Gate: Build succeeds**

- [x] 1.8 Run `dotnet build CLIHub.sln` Ã¢â‚¬â€ must report 0 errors, 0 warnings. The XAML still references the removed properties in its bindings, but XAML binding errors are runtime-only; the build passes because the C# code is consistent.

**Gate: Tests pass**

- [x] 1.9 Run `dotnet test CLIHub.sln` Ã¢â‚¬â€ must report 0 failed tests. Every test that previously asserted on `OriginBadgeText` or `OriginBadgeTooltip` now asserts on `IdentityTooltip` with the new contract (origin label always present, descriptor metadata when declared).

## Stage 2: Update XAML

The XAML bindings to the removed `OriginBadgeText`/`OriginBadgeTooltip` properties would fail only at runtime (XAML binding errors do not break the build), so Stage 1's build gate is meaningful even though XAML still references those properties. This stage makes XAML consistent with the updated ViewModels.

- [x] 2.1 In `src/CLIHub/Views/SettingsAgentsPage.xaml`, active card template (~line 248): add `ToolTip="{Binding IdentityTooltip}"` to the agent name's `TextBlock` (the first `TextBlock` inside the `StackPanel Orientation="Horizontal"`).
- [x] 2.2 In the same active card template, delete the entire `AgentOriginChip` `Border` block and its child `TextBlock` (~lines 267-273, the third `Border` after the name Ã¢â‚¬â€ starts with `AutomationProperties.Name="Plugin origin"`).
- [x] 2.3 In the disabled card template (~line 390): add `ToolTip="{Binding IdentityTooltip}"` to the agent name's `TextBlock` (the first `TextBlock` inside the `StackPanel Orientation="Horizontal"`).
- [x] 2.4 In the same disabled card template, delete the entire `AgentOriginChip` `Border` block and its child `TextBlock` (~lines 401-407, the second `Border` after the name Ã¢â‚¬â€ starts with `AutomationProperties.Name="Plugin origin"`).
- [x] 2.5 Delete the two now-unused styles from the file's `<UserControl.Resources>` section: `AgentOriginChip` (~line 55) and `AgentOriginChipText` (~line 63). Both styles and their `<Style.Triggers>` blocks can be deleted entirely.
- [x] 2.6 Do not touch the invalid card template (~line 447) Ã¢â‚¬â€ it has no origin badge and gains none in this change.

**Gate: Build succeeds**

- [x] 2.7 Run `dotnet build CLIHub.sln` Ã¢â‚¬â€ must report 0 errors, 0 warnings. The XAML and ViewModels are now fully consistent; all references to the removed properties are gone.

## Stage 3: Verify spec deltas and validate

Two spec deltas were authored during planning: `agent-ui` (MODIFIED requirement) and `plugin-installation` (REMOVED requirement). This stage confirms they match the approved design decisions.

- [x] 3.1 Confirm `openspec/changes/remove-agent-origin-badge/specs/agent-ui/spec.md` exists and contains:
  - The requirement name `### Requirement: Agent card displayed content` (preserved exactly from the main spec)
  - All existing scenario names preserved from `openspec/specs/agent-ui/spec.md:219-297` (Identity shown, Installation status shown, Installed version shown, Detecting state shown, Detecting never persists after a failed probe, Command set shown, Update available indicator shown, No indicator when availability is unknown, No indicator when up to date, No indicator for a missing installation, Update indicator cleared when a probe reports not installed, Update indicator is advisory, State chip returns when no update remains)
  - The two scenarios that previously anchored positions "before the origin badge" ("Update available indicator shown" and "State chip returns when no update remains") reworded to use "immediately after the name" or "in its usual position immediately after the name"
  - One new scenario `#### Scenario: Agent identity shown in name tooltip` describing the tooltip contract (origin label always present, descriptor metadata when declared)
- [x] 3.2 Confirm `openspec/changes/remove-agent-origin-badge/specs/plugin-installation/spec.md` exists and contains:
  - Section `## REMOVED Requirements`
  - Subsection `### Requirement: Plugin origin display` (exact name from `openspec/specs/plugin-installation/spec.md:192`)
  - `**Reason**:` paragraph explaining the badge was removed and the origin now appears in the agent name's tooltip
  - `**Migration**:` paragraph pointing to `agent-ui`'s `Agent card displayed content` requirement for the replacement tooltip contract
- [x] 3.3 If either delta does not match these points, correct it before proceeding to validation; otherwise continue.

**Gate: OpenSpec validation passes**

- [x] 3.4 Run `openspec validate --all` from the repository root Ã¢â‚¬â€ must report success (0 errors) for this change and for every other change/spec in the repository. Quote the exact totals line from the validation output.

**Gate: Full test suite passes**

- [x] 3.5 Run `dotnet test CLIHub.sln` one final time Ã¢â‚¬â€ must report 0 failed tests. This confirms the XAML changes did not introduce runtime regressions in the test-observable ViewModel surface.

## Stage 4: Manual smoke

- [x] 4.1 Manual smoke (requires a human; do not skip or self-certify): launch CLIHub, open Settings -> CLI Agents, and check:
  - The agent name in each card (active, disabled) shows no separate origin badge chip after it Ã¢â‚¬â€ only the state/version chip (or update chip when available) is shown.
  - Hovering over an agent's name shows a tooltip with the origin label plus the descriptor metadata when declared (e.g., "Origin: Installed\nVersion: 1.0.0\nPublisher: TrustSoft\nHomepage: https://example.com").
  - For a built-in agent with no declared version/publisher/homepage, the tooltip shows only "Origin: Built-in".
  - For a disabled agent, the tooltip shows "Origin: {value}" plus the version when available.
  - Compare Dark and Light themes; no visual regression on the remaining chips or card layout.
- [x] 4.2 Run `graphify update .` to refresh the knowledge graph after the source edits.

## Notes

- This change is UI-only: the `Origin` property stays on both ViewModel classes (used by `AgentManagementViewModel` for catalog summary counts); only the badge's dedicated display properties and XAML elements are removed.
- Stage 1 combines ViewModel and test updates so the build and test gates can both pass at the same checkpoint Ã¢â‚¬â€ deleting `OriginBadgeText`/`OriginBadgeTooltip` without updating the tests would break `CLIHub.Tests` compilation, not just produce warnings.
- If Stage 2's build gate reports XAML errors, check that the two `Border` blocks were fully deleted (opening and closing tags plus all attributes and child elements) and that the tooltip binding was added to the correct `TextBlock` (the one that binds `Text="{Binding Name}"`, not a child of a chip `Border`).
