## Context (verified current state)

All file:line references were verified against the working tree at HEAD `5b58407` on 2026-10-25.
The in-flight `agent-install-state` change has uncommitted edits, but none of the files cited below
is dirty (`git status --short` lists only `agent-install-state`'s agent sources/tests, its own
tasks.md, and `docs/backlog.md`); `SettingsStyles.xaml`, `SettingsWindow.xaml`, `SummaryCard.cs`,
`SummaryCardTests.cs`, and the `settings-theme` spec are clean. The approved mockup
`summary-card-layout.html` is an untracked file in the repository root.

### Template geometry today

- The shared summary-card style is `Style TargetType="{x:Type controls:SummaryCard}"` at
  `src/CLIHub/Themes/SettingsStyles.xaml:352-449`, introduced by the comment at lines 346-351
  ("Page-header summary card: label, value, optional detail and alert lines, icon block").
- The card chrome is a `Border` with `Padding="9"` (line 358) holding one `Grid` (lines 363-410).
  The grid defines only columns — `*` and `Auto` (lines 364-367) — so it is a single implicit row.
- One `StackPanel` sits in implicit column 0 (line 368) and stacks all four text elements:
  the Label `TextBlock` (lines 369-372), `ValueText` (lines 373-381, `Margin="0,2,0,0"` at 374),
  `DetailText` (lines 382-386, `Margin="0,2,0,0"`, `Style="{StaticResource HintText}"`), and
  `AlertText` (lines 387-393, `Margin="0,2,0,0"`, `Foreground="{DynamicResource DangerBrush}"`,
  `Style="{StaticResource HintText}"`, `Visibility="Collapsed"`).
- The icon `Border` sits in column 1 (`Grid.Column="1"` at line 395) with `Width="24"`,
  `Height="24"` (lines 396-397) and `VerticalAlignment="Top"` (line 398).

### The 24 px loss

Because `DetailText` and `AlertText` are children of the column-0 `StackPanel`, their layout slot is
column 0, whose width is the card content width minus the `Auto` icon column (24). Their right edge
is the icon block's left edge, and the 24 px under the icon stay empty on every card. The mockup
mirrors this geometry 1:1 at 100% DPI (padding 9, icon 24x24 — `summary-card-layout.html:11-14`)
and reproduces the reported wrap: its "current" variant gives the detail line an icon-width right
inset (`summary-card-layout.html:206-208`), so at the 188 px card width "Managed by Project
Scanning" breaks into two lines (`summary-card-layout.html:425,644-646`).

### Card height is already independent of the icon block

Row 0's height is `max(label + value stack, 24)`. The label line (11 px SemiBold per the mockup
metrics, `summary-card-layout.html:176-182`) plus the 2 px margin plus the value line (16 px
SemiBold, `summary-card-layout.html:184-190`) is ~37 px, well above the 24 px top-aligned icon
(`SettingsStyles.xaml:396-398`). So the detail line already starts below the icon block, and moving
it into an explicit row 1 does not change its top offset or the card height — confirmed by the
mockup's variant A note (`summary-card-layout.html:649-653`).

### Nine call sites, none template-aware

`src/CLIHub/Views/SettingsWindow.xaml` holds the only `SummaryCard` instances, three per page:

- `ProjectsSummaryCards` (lines 183-202): PROJECTS (185-189), AGENTS (191-194), DAEMON (196-200).
- `RegistrySummaryCards` (lines 204-225): PROJECTS with `Alert`/`IsAlertVisible` (206-213), MANUAL
  (215-218), DISCOVERED with `Detail="Managed by Project Scanning"` (220-223).
- `AgentsSummaryCards` (lines 228-249): AGENTS (230-234), DEFAULT (236-241, uses `ValueTone` and
  `ValueToolTip`), DETECTION (243-247).

All nine only set dependency properties; none reaches into the template, so the template change
applies to every card with no call-site edit.

### Control surface

`src/CLIHub/Controls/SummaryCard.cs` (lines 42-202) is a template-agnostic `Control`: dependency
properties `Icon`, `Label`, `Value`, `ValueTone`, `ValueToolTip`, `Detail`, `DetailToolTip`,
`Alert`, `AlertToolTip`, `IsAlertVisible` (registered at lines 47-102) and the `DefaultStyleKey`
override (lines 104-109). It contains no layout logic, so it needs no change.

### Spec state

`openspec/specs/settings-theme/spec.md` introduces the summary rows in requirement "Persistent page
header" (lines 258-309; the summary-row sentence is in the body at line 260) and details their
contents in "Projects page summary cards" (lines 311-341) and "CLI Agents page summary cards"
(lines 343-381). No requirement states the summary card's internal layout, so the detail-line width
is currently unspecified behavior.

## Decision

Implement mockup variant A (user-approved; not re-litigated): move `DetailText` and `AlertText` out
of the column-0 `StackPanel` into a second grid row that spans both columns.

### Target grid structure

The grid in `SettingsStyles.xaml:363-410` becomes (attributes of the moved elements stay
byte-identical; ellipses stand for the unchanged attribute sets):

```xml
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
    </Grid.ColumnDefinitions>
    <StackPanel>
        <!-- row 0, column 0 (implicit): unchanged Label TextBlock + ValueText -->
    </StackPanel>
    <Border Grid.Column="1"
            Width="24"
            Height="24"
            VerticalAlignment="Top"
            ...>
        <!-- row 0, column 1 (implicit row): unchanged icon block -->
    </Border>
    <StackPanel Grid.Row="1"
                Grid.ColumnSpan="2">
        <!-- NEW row 1: DetailText and AlertText moved here, attributes unchanged -->
    </StackPanel>
</Grid>
```

This matches the mockup's XAML sketch (`summary-card-layout.html:665-688`). The detail/alert right
edge becomes the card's right content edge — the icon block's right edge; the text stays
left-aligned (no `TextAlignment="Right"`).

### What stays untouched

- All `ControlTemplate.Triggers` (lines 412-445) keep working unchanged: the four `ValueTone`
  triggers (414-425), the `Detail` null/empty collapse (428-433), and `IsAlertVisible` plus the
  `Alert` null/empty collapse (436-444). They address the elements by `TargetName`, and the
  `x:Name`s (`ValueText`, `DetailText`, `AlertText`) move with the elements.
- All template bindings (`Label`, `Value`, `Detail`, `Alert`, `Icon`, and the three tooltip
  properties) are untouched.
- Margins: `DetailText`/`AlertText` keep `Margin="0,2,0,0"`; as the first children of the row-1
  `StackPanel`, the 2 px still separates the value line from the detail line exactly as it does
  inside today's shared `StackPanel`.
- The comment above the style (lines 346-351) gains one English sentence recording the layout rule
  (label/value reserve the icon column; detail/alert span both columns below the icon row).

## Alternatives considered

1. **Variant B — same width plus `TextAlignment="Right"` on detail/alert.** Rejected by the user:
   the card would mix two text alignments (label/value left, detail/alert right), which the mockup
   flags as inconsistent (`summary-card-layout.html:654-657`). Not chosen.
2. **Negative right margin on `DetailText`/`AlertText`.** Would visually overflow column 0 without
   changing its layout slot; text clipping and hit-testing get subtle, and the intent is invisible
   in the markup. Rejected as fragile.
3. **Shorter detail strings at the nine call sites.** Treats the symptom: the wording
   ("Managed by Project Scanning") is agreed content, and the 24 px would stay lost on every other
   long line, including future ones. Rejected.
4. **Do nothing (two-line wrap is acceptable).** The reported screenshot shows an unnecessary wrap
   for a line that fits; the user classified it as a defect and approved variant A. Rejected.

## Testing decision

No automated test is added, for three reasons recorded honestly:

- The existing `SummaryCardTests` (tests/CLIHub.Tests/Controls/SummaryCardTests.cs:1-55) asserts
  dependency-property defaults on an STA thread only; it never applies a template.
- Repo-wide, no test applies a control template or walks the visual tree (a search for
  `ApplyTemplate`/`FindName`/`VisualTreeHelper` under `tests/` returns zero matches), so this change
  would have to introduce a new harness (loading `SettingsStyles.xaml` plus its `Sizing.xaml` /
  `SettingsControls.xaml` dependencies and driving `ApplyTemplate` manually) for a single assertion.
- The meaningful assertions are either structural (the detail host spans both columns — a
  tautological restatement of the XAML) or pixel-based (right edges equal, one-line fit at 188 DIP —
  dependent on Segoe UI Variable availability, DPI, and text shaping, hence flaky across machines
  and CI).

The durable guarantee is the new spec scenario below; the visual check is a manual smoke task in
tasks.md stage 3.

## Spec delta decision

The layout rule is durable, user-visible behavior that a future template refactor could silently
regress, so it gets a spec statement: a MODIFIED "Persistent page header" requirement in
`settings-theme` — the requirement that introduces the summary rows for all three pages. All eight
original scenario names and bodies are preserved verbatim; one scenario is added ("Summary detail
lines span the card width"). The requirement body is deliberately not extended: it is already
over 500 characters and already triggers the validator's length warning, so the rule lives in the
new scenario instead.

## Deviations

- The mockup's XAML sketch (`summary-card-layout.html:675`) shows explicit `Grid.Row="0"`
  `Grid.Column="0"` markers on the existing `StackPanel`; the implementation keeps the minimal diff
  and leaves the two existing children in implicit row 0. Behavior is identical; only the sketch is
  illustrative.
- The spec delta adds a scenario without touching the requirement body (see above).

## Open questions

None.
