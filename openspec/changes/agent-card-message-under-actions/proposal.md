# Agent card message line renders under the action panel, and the blocked-update copy is shortened

## Why

On Settings → CLI Agents, every per-agent card renders its action-outcome message (the "status
line") inside the left identity column, stacked under the "Supported commands" hint
(src/CLIHub/Views/SettingsAgentsPage.xaml:220-223). With a screenshot annotation the user asked for
it to move below the action-button panel on the right side of the card.

The message is always the outcome of one of the four action buttons in the card's right column
(Detect / Check updates / Update / Edit config, src/CLIHub/Views/SettingsAgentsPage.xaml:226-278) —
for example a blocked-update explanation, a detect result, or `Up to date`. Rendering it directly
under those buttons ties the outcome to its trigger: short messages hug the right edge beneath the
button panel, and long messages wrap leftward inside the card instead of stretching the identity
column, whose width is shared with the agent name, chips, and hint above it.

Separately, the blocked-update explanation repeats the agent-name list and is needlessly long:
`Update blocked while the agent is running: OpenCode, OpenCode`
(src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:453,463 and
src/CLIHub.Core/Agents/AgentManagementService.cs:93-96). The user asked to shorten it to
`Update blocked while running`; the card's one-line message no longer names the offending processes,
and the duplicate-name noise disappears.

## What Changes

- Wrap the four action buttons (`controls:SpacedPanel`, src/CLIHub/Views/SettingsAgentsPage.xaml:225-277)
  in a vertical `StackPanel` in column 2, top-aligned and right-aligned, and move the message
  `TextBlock` out of the column-1 identity `StackPanel` (src/CLIHub/Views/SettingsAgentsPage.xaml:220-223)
  into that stack after the buttons. The message therefore sits directly under the button row with
  `Margin="0,2,0,0"` — a 2 px vertical gap, per the user's follow-up.
- Message attributes: `HorizontalAlignment="Right"`, `TextAlignment="Right"`,
  `MaxWidth="{StaticResource Sizing.AgentRow.TextMaxWidth}"` (240 DIP, so long messages wrap
  instead of squeezing the identity column), `FontSize="{StaticResource Sizing.FontSize.Section}"`
  (11 DIP — the same size as the "Supported commands" hint under the agent name), plus the existing
  `Style="{StaticResource StatusLine}"`, `Text="{Binding Message}"`, and
  `Visibility="{Binding HasMessage, Converter={StaticResource BoolToVisibility}}"`. The accent
  foreground stays. No `Grid.RowDefinitions` and no spanning row are used.
- Shorten the blocked-update message to exactly `Update blocked while running` in both producers:
  the card blocked-state pass (src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:461-463,
  removing the `blockedNames` join) and the execution-time blocked result
  (src/CLIHub.Core/Agents/AgentManagementService.cs:93-96, removing the `names` join). Update the
  affected expectations in `AgentManagementViewModelTests` and `AgentManagementServiceTests`.
- Everything else on the card stays unchanged: logo, name/state-chip/update-chip block,
  "Supported commands" hint, the four action buttons and their order, glyphs, tooltips, commands,
  and enabled-state bindings. All other message wordings (Detect, Update, Edit config, per-card
  Check updates) are untouched.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. The change moves the message within one `DataTemplate` and shortens one message wording. No
durable spec pins the message line's position on the card: `agent-ui` enumerates the card's displayed
content without the message line (openspec/specs/agent-ui/spec.md:221), and its action requirements
pin only the message line's *content* ("SHALL report its outcome on the card's message line",
openspec/specs/agent-ui/spec.md:319; scenario at openspec/specs/agent-ui/spec.md:408-413). The blocked
scenario requires only that "the card explains that the running agent blocks it"
(openspec/specs/agent-ui/spec.md:338-341); `Update blocked while running` still satisfies that and no
requirement names the blocked processes. The "status line under the update controls" requirement
(openspec/specs/settings-theme/spec.md:91) is the Settings → Updates page's status line, not the agent
card.

Therefore the change declares `skip_specs: true` in `.openspec.yaml`, following
`openspec/changes/archive/2026-10-24-consolidate-clone-siblings/`. Escape hatch: if the
implementation session finds spec text that does pin the card message's placement, stop and convert
the change into a spec delta instead (keeping original scenario names for MODIFIED requirements).

## Impact

- Source: `src/CLIHub/Views/SettingsAgentsPage.xaml` (vertical action stack with the message under the
  buttons inside the per-agent card `DataTemplate`), `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs`
  (blocked message constant, `blockedNames` removal), and `src/CLIHub.Core/Agents/AgentManagementService.cs`
  (blocked result message constant, `names` removal).
- Tests: existing expectations updated in `tests/CLIHub.Tests/ViewModels/Agents/
  AgentManagementViewModelTests.cs` and `tests/CLIHub.Core.Tests/Agents/AgentManagementServiceTests.cs`
  to the new exact sentence; no test is weakened or deleted, and no visual-tree test harness is
  introduced (the layout remains a manual smoke task).
- User-visible impact: on Settings → CLI Agents, each card's message line sits directly under the
  action buttons with a 2 DIP gap, right-aligned to the panel's right edge, at the same 11 DIP font
  size as the "Supported commands" hint, wrapping inside a 240 DIP maximum; the blocked-update
  sentence is now `Update blocked while running` in both the load-time blocked-state pass and the
  execution-time block. Cards without a message keep their current height; all other card content,
  and every other page, are unchanged.
- Docs: `docs/images/settings-agents.png` was regenerated for the review pass. If the smoke requests
  a layout tweak, re-run `docs/images/tools/Capture-ReadmeImages.ps1 -SkipBuild -SkipWebImages` with
  the output directory inside the repository and copy only that image, leaving all other captures
  untouched.

## Non-goals

- No changes to the message's visibility conditions or the `StatusLine` style (font, accent
  foreground, wrap behavior stay as-is), and no changes to the other message wordings (Detect,
  Update, Edit config, per-card Check updates); only the blocked-update sentence changes.
- No changes to the action buttons, their order, glyphs, tooltips, commands, or enabled-state logic.
- No changes to the logo, name, state chip, update chip, or "Supported commands" hint layout.
- No changes to the three summary cards at the top of the CLI Agents page, the header-wide actions,
  or any other Settings page.
- No new shared style, sizing resource, or control; the move stays local to the page's
  `DataTemplate`.
- No automated UI test harness introduction; the visual check is a manual smoke.
- No README regeneration beyond `docs/images/settings-agents.png`; no other image or web asset is
  touched.
- No commit, push, or archive in this change's own tasks; those follow the repo approval policy.

## Dependencies

- None on other active changes: `openspec/changes/` holds no other active change (only `archive/`),
  and the working tree is clean at `2c748a5`.
- Consistent with `agent-ui`'s card content and action requirements
  (openspec/specs/agent-ui/spec.md:219-221,317-419), which this change satisfies unchanged.
