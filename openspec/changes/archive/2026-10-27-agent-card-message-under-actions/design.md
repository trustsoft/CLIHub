# Design: agent-card-message-under-actions

## Context (verified current state)

All file:line references were verified against the clean working tree at HEAD `2c748a5` on
2026-10-26 (`git log --oneline -1` → `2c748a5 chore(openspec): archive agent-card-check-updates`;
`git status --short` is empty; `openspec/changes/` holds no active change).

### The per-agent card on Settings → CLI Agents

- The per-agent list is an `ItemsControl` over `AgentsState.Cards`
  (src/CLIHub/Views/SettingsAgentsPage.xaml:168) whose `DataTemplate` starts at
  src/CLIHub/Views/SettingsAgentsPage.xaml:170 and renders each card as a `Border` chrome with
  `Padding="12"`, `Background="{DynamicResource PopoverBgBrush}"`,
  `BorderBrush="{DynamicResource FieldStrokeBrush}"`, `BorderThickness="1"`,
  `CornerRadius="{StaticResource Sizing.CornerRadius.Card}"` (lines 171-176).
- The card's root `Grid` (src/CLIHub/Views/SettingsAgentsPage.xaml:177) defines exactly three
  columns — `Auto`, `*`, `Auto` — at lines 178-182 and has **no** `Grid.RowDefinitions`; all
  children live in the implicit single row 0.
- Column 0 holds the plain logo `Image`, 32x32, top-aligned (lines 184-190; unframed since
  `2026-10-26-agent-logo-unframed`).
- Column 1 is the identity `StackPanel` with `Margin="12,0,12,0"` (line 192) stacking: the name
  `TextBlock` (194-198), the state chip `Border` (199-203), the advisory update chip `Border`
  (204-211), the "Supported commands" hint `TextBlock` with `Style="{StaticResource HintText}"`
  (213-218), and then the message `TextBlock` (220-223):

  ```xml
  <TextBlock Margin="0,6,0,0"
             Style="{StaticResource StatusLine}"
             Text="{Binding Message}"
             Visibility="{Binding HasMessage, Converter={StaticResource BoolToVisibility}}"/>
  ```

- Column 2 is the action row: a `controls:SpacedPanel` at src/CLIHub/Views/SettingsAgentsPage.xaml:
  226-278 (`VerticalAlignment="Top"` 227, `Orientation="Horizontal"` 228, `Spacing="2"` 229) holding
  Detect (230-241), Check updates (242-253), Update (254-265), and Edit config (266-277) icon
  buttons. The `Grid` closes at line 279.
- The `BoolToVisibility` converter is already used on this card (lines 208 and 223), so the moved
  element needs no new resource.

### The StatusLine style

`StatusLine` (src/CLIHub/Themes/SettingsControls.xaml:31-35) sets `FontSize` to
`Sizing.FontSize.Body`, `Foreground` to `{DynamicResource AccentBrush}`, and
`TextWrapping="Wrap"`. It sets neither `HorizontalAlignment` nor `TextAlignment`, so the
element-local `HorizontalAlignment="Right"` and `TextAlignment="Right"` added by this change do not
fight any style setter; wrap behavior keeps long messages inside the card.

### Where the message comes from

`AgentCardItem.Message` (src/CLIHub/ViewModels/Agents/AgentCardItem.cs:257-267) is the last action
outcome, and `HasMessage` (line 272) is `!string.IsNullOrEmpty(this.Message)`; setting `Message`
raises `PropertyChanged` for `HasMessage` (lines 262-265). The XAML change does not touch this —
same binding, same visibility gate.

### Spec state (why `skip_specs: true` is safe)

- `agent-ui` "Agent card displayed content" (openspec/specs/agent-ui/spec.md:219-221) enumerates
  display name, logo, state chip, update indicator, and the declared command set — the message line
  and its position are not part of the requirement.
- `agent-ui` "Agent card actions" (openspec/specs/agent-ui/spec.md:317-319) requires per-card
  outcomes to "report ... on the card's message line", and the scenario "Per-card Check updates
  reports its outcome on the card" (openspec/specs/agent-ui/spec.md:408-413) pins the message
  *content* (`Update available · {installed} → {latest}` / `Up to date` /
  `Update availability unknown`). Neither pins where on the card the line renders; all stay true
  after the move.
- `settings-theme` line 91 ("status line under the update controls",
  openspec/specs/settings-theme/spec.md:91, scenario at :95) describes the Settings → Updates page's
  update-check status line, not the CLI Agents card.

No durable contract pins the card message's placement, so a position-only change has no behavior
delta to record; the change declares `skip_specs: true` in `.openspec.yaml` (included explicitly —
a previous change forgot the file).

### Test state

No test under `tests/` references `SettingsAgentsPage`, applies a WPF template, or walks a visual
tree (verified by grep for `SettingsAgentsPage|VisualTree|ApplyTemplate` in `tests/` — no matches).
The change is XAML markup only: no binding path, converter, style, or code-behind change.

### Markup convention (rows before columns)

Every grid in `src/` that defines both puts `Grid.RowDefinitions` before `Grid.ColumnDefinitions`
(verified by scanning all five: src/CLIHub/Themes/SettingsStyles.xaml, src/CLIHub/Views/
MainWindow.xaml, SettingsAppearancePage.xaml, SettingsWindow.xaml, WhatsNewWindow.xaml — e.g.
src/CLIHub/Views/SettingsWindow.xaml:112-116, src/CLIHub/Views/SettingsAppearancePage.xaml:22-26).
Two `RowDefinition Height="Auto"` entries is the established pattern for Auto-growing rows
(src/CLIHub/Views/SettingsAppearancePage.xaml:23-26).

### Baseline gates (captured 2026-10-26 at `2c748a5`)

- `openspec validate --all` → `Totals: 29 passed, 0 failed (29 items)` (run in this session;
  pre-existing length warnings unchanged).
- `dotnet build CLIHub.sln` / `dotnet test CLIHub.sln` → recorded baseline for this brief: build
  clean, 1202 passed, 0 failed at `2c748a5` (not re-run in the preparation session; the stage-1
  gate re-establishes both).

## Decisions

### D1: Action column hosts a vertical stack: buttons, then the message 2 px below

The card `Grid` keeps its three columns and gains no rows. The action `controls:SpacedPanel`
(src/CLIHub/Views/SettingsAgentsPage.xaml:225-277) is wrapped in a vertical `StackPanel` in column 2,
and the message `TextBlock` moves out of the identity `StackPanel` (lines 220-223) into that stack
after the buttons:

```xml
<StackPanel Grid.Column="2" VerticalAlignment="Top">
    <controls:SpacedPanel HorizontalAlignment="Right"
                          Orientation="Horizontal"
                          Spacing="2">
        <!-- four action buttons, unchanged -->
    </controls:SpacedPanel>
    <TextBlock Margin="0,2,0,0"
               MaxWidth="{StaticResource Sizing.AgentRow.TextMaxWidth}"
               HorizontalAlignment="Right"
               Style="{StaticResource StatusLine}"
               FontSize="{StaticResource Sizing.FontSize.Section}"
               Text="{Binding Message}"
               TextAlignment="Right"
               Visibility="{Binding HasMessage, Converter={StaticResource BoolToVisibility}}"/>
</StackPanel>
```

- The message is the button panel's sibling in the same stack, so `Margin="0,2,0,0"` is measured
  directly from the buttons' bottom edge — the 2 DIP gap the user asked for. The rejected first
  iteration placed the message in a second grid row, where row 0's identity height (name, chips,
  hint) pushed it ~20 px below the buttons.
- `MaxWidth="{StaticResource Sizing.AgentRow.TextMaxWidth}"` (240 DIP) caps the `Auto` action
  column's growth: short messages hug the buttons' right edge, long ones wrap inside 240 instead of
  squeezing the identity column without bound.
- `FontSize="{StaticResource Sizing.FontSize.Section}"` (11 DIP) matches `HintText`, the
  "Supported commands" line under the agent name. `StatusLine` sets Body (12 DIP), so the local value
  deliberately overrides it; the accent foreground from `StatusLine` is preserved.
- `HorizontalAlignment="Right"` on the message and on the button panel keeps both right edges
  aligned with the card's content edge; `TextAlignment="Right"` right-justifies wrapped lines.
- `Visibility="Collapsed"` (the converter's false state) leaves the stack at the buttons' height, so
  cards without a message keep their exact current height.
- Attribute order follows the file's convention: `Grid.*` first, then margin/alignment, then the
  style and bindings (e.g. lines 184-190, 192, 225-228).

### D2: Keep the existing `Text="{Binding Message}"` exactly

The existing binding is plain `Text="{Binding Message}"` and is kept byte-identical:
`TextBlock.Text` defaults to one-way binding, so `Mode=OneWay` would be a semantic no-op that only
widens the diff. Same for `Style` and `Visibility` — all carry over verbatim.

### D3: No row definitions and no `Grid.Row` assignments

The card grid keeps its three columns and no rows; the message lives inside the action-column stack,
not in a grid row. No existing child receives a `Grid.Row` attribute, and the columns and their
widths are untouched.

### D4: `skip_specs: true`, and the `.openspec.yaml` is written explicitly

No durable behavior delta (see "Spec state" above): the message's visibility gate and styling are
unchanged, its position is not pinned by any requirement, and the shortened blocked-update sentence
still satisfies the only relevant contract ("the card explains that the running agent blocks it").
Follows the `2026-10-24-consolidate-clone-siblings` `skip_specs` precedent; contrast
`2026-10-26-agent-logo-unframed` D2, which needed a delta only because `settings-theme` explicitly
claimed the framed icon-block treatment there — no comparable claim exists for message placement or
the blocked sentence's exact words. The `.openspec.yaml` (`schema: spec-driven`,
`created: 2026-10-26`, `skip_specs: true`) is included as a real file because a previous change
forgot it.

### D5: No automated test for the layout; existing message assertions updated for the copy

Pure XAML element move with no behavioral code path and no template/visual-tree harness in the repo
(same reasoning as archived `2026-10-26-agent-logo-unframed` D3 and `2026-10-26-summary-card-detail-
width`), so the visual contract is pinned by the stage-3 manual smoke task. The copy change is
covered by the existing exact-message tests, which are updated rather than deleted or loosened:
four expectations in `AgentManagementViewModelTests` and the runtime-hosted assertion in
`AgentManagementServiceTests`.

### D6: Shorten the blocked-update sentence in both producers

Current wording (src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:453,461-463 and
src/CLIHub.Core/Agents/AgentManagementService.cs:93-96):

```text
Update blocked while the agent is running: OpenCode, OpenCode
```

The blocked-state pass joins every matching process's plugin name; with two OpenCode processes the
same name repeats, and the card — now a right-aligned line under the action panel — carries a long,
noisy sentence for no gain. The user asked for exactly `Update blocked while running`.

Both producers change: the card blocked-state pass writes the constant and gates on
`running.Count > 0` (the `blockedNames` join is deleted), and the Core service returns the constant
as the blocked result's message (the `names` join is deleted). Keeping only the proactive pass would
leave the execution-time block (`UpdateCommand_WhenServiceBlocks_SurfacesServiceMessage`) showing the
old long sentence, so the two surfaces would disagree.

Alternatives: keep the names in the card (rejected — source of the duplicate-name noise and the
length); show a count (`Update blocked while running: 2 processes`) — rejected, the user asked for
the exact short sentence and the count adds no actionable information; move the names to a tooltip —
rejected, the message line has no tooltip and the block is fully explained by the actions'
availability plus the state chip.

Tests to update: `UpdateCommand_WhenServiceBlocks_SurfacesServiceMessage`,
`Load_MatchingAgentProcessRunning_DisablesUpdateAndExplainsBlock`,
`Load_NodeShimAgentRunning_DisablesUpdateAndExplainsBlock`, and the gated blocked-state assertion in
`AgentManagementViewModelTests`, plus `UpdateAsync_RuntimeHostedAgent_IsBlocked` in
`AgentManagementServiceTests` (its `Assert.Contains(plugin.Name, ...)` becomes an exact equality
assertion on the new sentence).

### D7: README image refresh is a final manual task, not implementation

`docs/images/settings-agents.png` shows the old layout and is refreshed by the orchestrator for the
review pass (5.1 ticked); re-run the script after the smoke only if the smoke requests a layout
tweak. The runner is
`docs/images/tools/Capture-ReadmeImages.ps1` with `-SkipBuild` (reuse the Debug build produced by
the stage-1 gate) and `-SkipWebImages` (keep banner/workflow/social-preview untouched). The script
writes its whole capture set into `-OutputDirectory`, refuses an output directory outside the
repository (docs/images/tools/Capture-ReadmeImages.ps1:66-69), requires CLIHub not to be running
(:80-83), and backs up/restores the user's `config.json` and logo cache (:90-108) — so the task
directs the output to a scratch folder inside the repo and copies only `settings-agents.png` into
`docs/images/`, leaving every other capture untouched. The implementation stages do not regenerate
any image.

## Alternatives considered

1. **Full-width bottom line (`Grid.ColumnSpan="3"`, left-aligned across the whole card).** Rejected:
   the user annotated the message under the action panel, and the message is the outcome of the
   buttons, not of the logo/identity block; a card-wide footer also re-introduces a wide text block
   at the logo column's left edge, which is what the current layout was criticized for.
2. **Spanning second grid row (the first iteration).** Rejected after the user's follow-up: the
   row starts below row 0, whose height is set by the identity block (name, chips, hint ~41 px), so
   the message landed ~20 px under the 26 px button row instead of the requested 2 px. The vertical
   action stack measures the gap directly from the buttons.
3. **Left-aligned under the buttons.** Rejected: a short message ("Up to date") would float at the
   left edge of the message block, far from the buttons that produced it, reading as a second hint
   line rather than an action outcome; right alignment keeps short results visually tied to the
   buttons and lets long ones wrap left.
4. **Message in column 2 without a width cap.** Rejected: column 2 is `Auto`-width, so a long message
   would size the column to its longest line and squeeze the identity column without bound; the
   240 DIP `MaxWidth` cap keeps the action column bounded while the message wraps.
4. **Keep the message in the identity column (status quo).** Rejected: the user explicitly asked
   for the move (screenshot annotation); not re-opened per the brief.
5. **A shared right-aligned variant of `StatusLine` (new style key).** Rejected: single call site;
   a local attribute pair (`HorizontalAlignment`/`TextAlignment`) is the minimal diff, mirroring the
   no-new-resource reasoning of `2026-10-26-agent-logo-unframed` D4.

## Deviations

- None from the brief. The brief's line anchors were slightly stale against `2c748a5` and are
  corrected here with verified citations: the `DataTemplate` starts at
  src/CLIHub/Views/SettingsAgentsPage.xaml:170 (the `ItemsControl` at :168), and the action
  `SpacedPanel` spans :226-278 (the brief said 226-266; the fourth button ends at :277) — the card
  now carries four buttons since `2026-10-26-agent-card-check-updates`. The code wins; the plan is
  written against the verified anchors.

## Open questions

1. **Wrapped-line justification (non-blocking, confirm in smoke):** `TextAlignment="Right"`
   right-justifies *every* wrapped line, so a two-line message has its first line ragged-left. This
   is the intended "wrap and extend left" look per the brief; the stage-3 smoke must confirm it
   reads acceptably for the longest real message (blocked-update explanation). WPF offers no
   built-in "align last line only", so any change here would need custom layout — park unless the
   user objects.
2. **Commit placement of the refreshed image (for the orchestrator):** should the regenerated
   `docs/images/settings-agents.png` land in the same commit as the XAML move (it belongs to this
   change's visual outcome) or as a follow-up docs commit? The stage-4 task stops after the copy and
   leaves staging to the user's approval per repo policy.
3. **Whether `Message` should ever grow an explicit `Mode=OneWay`** for self-documentation: decided
   against here (D2, no-op semantics); revisit only if a future change makes the binding mode
   load-bearing.
