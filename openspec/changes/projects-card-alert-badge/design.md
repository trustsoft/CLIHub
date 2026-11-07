## Context (verified current state)

All file:line references were verified against the working tree at HEAD on 2026-11-08.
`git status --short` shows one unrelated pre-existing delete `D opencode.json`; every file cited
below is clean. The mockup `mockup-projects-summary-card.html` is an untracked file at the repo root.

### SummaryCard control surface (`src/CLIHub/Controls/SummaryCard.cs:1-202`)

A template-agnostic `Control` with `DefaultStyleKey` override at line 104-109. Public dependency
properties (registered at lines 47-102): `Icon`, `Label`, `Value`, `ValueTone`, `ValueToolTip`,
`Detail`, `DetailToolTip`, `Alert` (line 89), `AlertToolTip` (line 95), `IsAlertVisible` (line 101).
The CLR wrapper properties follow at lines 110-201. `SummaryCardTone` enum at lines 9-30.

`Alert` has exactly **one consumer** in the entire repository: the PROJECTS card in
`RegistrySummaryCards` in `src/CLIHub/Views/SettingsWindow.xaml:188-208` (grep confirmed three
matches at lines 190, 191, 194 — all on the same `SummaryCard` element). No other card sets `Alert`,
`AlertToolTip`, or `IsAlertVisible`.

### Template geometry (`src/CLIHub/Themes/Common/SettingsStyles.xaml:352-456`)

The implicit `SummaryCard` style begins at line 352. The `ControlTemplate` contains:

- A `Border` with `Padding="9"` (line 358), card chrome.
- A two-column `Grid` (lines 363-416): columns `*` and `Auto`.
- Row 0 (implicit): column-0 `StackPanel` (line 372) holding `Label` TextBlock (line 373) and
  `ValueText` TextBlock (line 377, `Margin="0,2,0,0"`, `FontSize=Sizing.FontSize.SummaryValue`,
  `SemiBold`, template-bound to `Value` with `ToolTip` to `ValueToolTip`).
- Row 0, column 1: icon `Border` at line 387 (`Width="24"`, `Height="24"`,
  `VerticalAlignment="Top"`).
- Row 1 (`Grid.Row="1"`, `Grid.ColumnSpan="2"`): `StackPanel` at line 402 holding `DetailText`
  (line 404, `Margin="0,2,0,0"`, `Style=HintText`, `ToolTip=DetailToolTip`) and `AlertText`
  (line 409, `Margin="0,2,0,0"`, `Foreground=DangerBrush`, `Style=HintText`,
  `ToolTip=AlertToolTip`, `Visibility="Collapsed"`).
- `ControlTemplate.Triggers` at lines 419-452: four `ValueTone` triggers (lines 421-432); two
  `Detail` null/empty collapse triggers (lines 435-440); `IsAlertVisible=True` shows `AlertText`
  (line 443); `Alert` null/empty re-collapses `AlertText` (lines 446-451).

`Focusable=False` is set at line 354.

### Current card geometry at ~190 DIP

The three cards on the Projects page sit in a `SpacedPanel` with `Spacing="8"` and `StretchChildren="True"` (`SettingsWindow.xaml:189`). The outer content area width is approximately 582 DIP (790 DIP window − sidebar − outer padding). Three cards with two 8 DIP gaps: `(582 − 16) / 3 ≈ 189 DIP` usable card width. With 9 DIP padding each side, the inner content width is ~171 DIP.

When `HasMissingFolders` is true today, `AlertText` appears as a 4th line below the detail line,
growing the card by one `HintText` line height + 2 DIP margin. The other two cards are pushed to the
same height by `StretchChildren`, creating equal but taller cards whenever any folder is missing.

The value row today is: label (11 DIP SemiBold) + 2 DIP margin + value (≈21 DIP SemiBold). A compact
chip at 17 DIP height centered against the 21 DIP value text fits in the row with no height growth.

### Binding site (`src/CLIHub/Views/SettingsWindow.xaml:188-208`)

```
RegistrySummaryCards DataTemplate at line 188:
  PROJECTS SummaryCard (lines 190-196):
    Alert="{Binding ProjectRegistry.FolderStatusSummary}"        line 190
    AlertToolTip="Folders that no longer exist"                   line 191
    Detail="In the launch window"                                 line 192
    Icon=IconGlyphs.Code                                          line 193
    IsAlertVisible="{Binding ProjectRegistry.HasMissingFolders}"  line 194
    Label="PROJECTS"                                              line 195
    Value="{Binding ProjectRegistry.TotalCount}"                  line 196
```

### ViewModel (`src/CLIHub/ViewModels/Projects/ProjectRegistryViewModel.cs`)

Relevant properties (lines 69-83):

```csharp
public int MissingFolderCount => this.Projects.Count(item => !item.IsFolderAvailable); // line 72
public bool HasMissingFolders => this.MissingFolderCount > 0;                          // line 75
public string FolderStatusSummary => this.MissingFolderCount switch                    // line 78
{
    0 => string.Empty,
    1 => "1 folder missing",
    var count => $"{count} folders missing"
};
```

`RaiseSummaryChanges` at line 300 raises `ManualCount`, `DiscoveredCount`, `TotalCount`,
`MissingFolderCount`, `HasMissingFolders`, `FolderStatusSummary`.

Interface `IProjectRegistrySection` (lines 35-40) declares `HasMissingFolders` and
`FolderStatusSummary`. `MissingFolderCount` is declared in the VM only, not on the interface
(verified at `src/CLIHub/ViewModels/Projects/IProjectRegistrySection.cs:1-81`).

### Existing tests (`tests/CLIHub.Tests/ViewModels/Projects/ProjectRegistryViewModelTests.cs:80-122`)

- Line 92 asserts `FolderStatusSummary == "1 folder missing"` — this test MUST remain unchanged.
- Line 105 asserts `FolderStatusSummary == string.Empty`.
- Line 121 asserts `FolderStatusSummary == "2 folders missing"`.

### Durable spec (`openspec/specs/settings-theme/spec.md:339-369`)

Requirement "Projects page summary cards" pins the folder-health behavior with five scenarios. The
"Missing folders reported" scenario (lines 360-364) currently states:
> the total card reports the number of missing folders in the danger tone

This is a durable behavior requirement. Changing from a fourth alert line to a chip + icon badge
changes how the danger tone is expressed, so the spec needs a MODIFIED delta — `skip_specs: true` is
not appropriate here.

---

## Decisions

### D1 — New vs. repurposed DPs on SummaryCard

**Decision: replace** `Alert`/`AlertToolTip`/`IsAlertVisible` with four purpose-built badge
properties: `BadgeText`, `BadgeTooltip`, `IsBadgeVisible`, `BadgeCount`.

**Rationale:** `Alert` is named for the alert-line pattern (a fourth line of text in the danger
tone). The E1 design replaces that pattern with two visual objects (inline chip + overlay badge) that
share one tooltip text. Reusing `Alert` as the chip text would misname the property; `AlertToolTip`
can not serve as the shared tooltip for both indicators without semantic confusion. There is exactly
one consumer of the old trio, so removing dead template code is acceptable and produces cleaner
XML docs with no migration cost.

`BadgeText` drives the chip label (`"1 missing"`), `BadgeCount` drives the icon overlay count
(`"1"`), `BadgeTooltip` is the single source of truth for both tooltip targets, and `IsBadgeVisible`
is the master gate (same pattern as `IsAlertVisible`). The `Detail`/`DetailToolTip` pattern is
directly echoed; the naming stays idiomatic.

**Rejected alternative:** Keep `Alert`/`AlertToolTip`/`IsAlertVisible` as aliases and add new DPs
alongside — creates dead API surface with two sets of semantically overlapping properties. Rejected
because the single consumer is updated in the same change.

### D2 — Compact chip text: new VM property vs. XAML StringFormat

**Decision: new VM property `MissingFolderChip`** on the interface and implementation, following
the `FolderStatusSummary` pattern.

**Rationale:** StringFormat on a binding cannot express the `"1 missing"` / `"{n} missing"` / empty
conditional without a converter (it would produce `"0 missing"` for the zero case unless an
additional visibility trigger is used). A property is more testable, matches existing conventions in
the VM, and separates the concern from the template. Plural/singular logic stays in one place (the
VM), not split between VM and XAML.

The interface gets `MissingFolderChip` alongside `FolderStatusSummary`; they are independent — both
are kept. `BadgeTooltipText` is also a new VM property, building on `FolderStatusSummary` to
compose the combined tooltip string.

### D3 — `BadgeCount` type: `string?`; no new VM count property

**Decision:** `BadgeCount` is a `string?` dependency property. The call site binds
`MissingFolderCount` directly via `StringFormat={}{0}` — no new VM string property is added for the
count.

**Rationale:** `string?` lets the template collapse the overlay via a null/empty trigger, exactly as
`Alert`/`Detail` collapse today. The count value is already available as `MissingFolderCount`
(promoted to the interface in step 1.1 for contract hygiene); a StringFormat binding at the call
site converts it to a string without any VM plumbing. Adding a separate VM string property for
what is essentially `MissingFolderCount.ToString()` would add interface noise with no benefit.

### D4 — XAML layout: value-row chip container

**Decision:** Wrap `ValueText` and the chip `Border` in a `WrapPanel` (not a horizontal
`StackPanel`) inside the column-0 vertical `StackPanel`.

**Rationale:** `ValueText` currently has `TextTrimming="CharacterEllipsis"` (template line 383)
because it sits in a `*`-width column. A horizontal `StackPanel` measures its children with infinite
available width, which would cause `ValueText` to stop trimming — long values on other cards
(agent names, long discovered-folder counts) would overflow under the icon block. A `WrapPanel`
measures each child with the remaining finite line width derived from the parent column's constraint,
so `ValueText` keeps trimming when it is the sole child (all cards without a badge). The chip stays
on the same line as the short numeric value on the PROJECTS card because the two elements together
are much narrower than the ~171 DIP available inner width.

**Constraint noted:** only the PROJECTS card currently combines a chip with a value, and its value is
a short integer count (e.g. `"14"`). If a future card combines a long text value with a badge chip,
the chip may wrap to the next line (acceptable); document this in the comment added in task 4.3.

### D5 — XAML layout: icon overlay

**Decision:** Wrap the icon `Border` and the count overlay `Border` in a `Grid` with `Width="24"`
`Height="24"`. The overlay uses `HorizontalAlignment="Right"`, `VerticalAlignment="Top"`, and
`Margin="0,-5,-5,0"` to overhang the 24×24 icon, with `Grid.ZIndex="1"` so it draws on top.

Geometry (from mockup `.badge-count` style, `mockup-projects-summary-card.html:424-441`):
`min-width: 15`, `height: 15`, `border-radius: 8`, `background: DangerBrush`,
`box-shadow: 0 0 0 2px var(--popover)` → WPF: `BorderBrush="{DynamicResource PopoverBgBrush}"`,
`BorderThickness="2"`, `CornerRadius` from `Sizing.CornerRadius.Chip` (6; final shape is a rounded
square/rectangle per Post-smoke adjustments, not the mockup's circle-like radius 8).

The icon column stays `Width="Auto"`; the wrapping `Grid` owns the `Width`/`Height`.

### D6 — Count-badge text color

**Decision:** `Foreground="{DynamicResource ChromeBgBrush}"`, matching the existing `AccentButton`
pattern for text on a saturated fill.

**Rationale:** `#FF1C1C21` on the light-theme `DangerBrush` (`#C0392B`) has ~2.5:1 contrast ratio,
which fails WCAG AA for small text. `ChromeBgBrush` is `#26262C` (dark) and `#F3F3F5` (light) —
the same token `AccentButton` uses for its label on a saturated background. On dark theme this is
dark-on-red (still accessible); on light theme it is near-white-on-red, providing adequate contrast.
The ring stays `BorderBrush="{DynamicResource PopoverBgBrush}"` to match the card background on both
themes. The literal `#FF1C1C21` from the original draft is removed; no new theme resource is needed.

### D7 — Accessibility mechanism

**Decision:** Set `AutomationProperties.HelpText="{TemplateBinding BadgeTooltip}"` on the chip's
inner `TextBlock` in the template. No `AutomationLabel` VM property; no `AutomationProperties.Name`
on the card.

**Rationale:** `SummaryCard` is a `Control` with no `OnCreateAutomationPeer` override. In WPF,
`AutomationProperties.Name` set on a `Control` element is not propagated to UI Automation unless the
control has a peer that reads it (the default `ControlAutomationPeer` exposes `Name` from the
property, but the card is `Focusable=False` and not otherwise reachable in the automation tree in
practice). By contrast, `TextBlock` has `TextBlockAutomationPeer`, which does expose
`AutomationProperties.HelpText`. Setting `HelpText` on the chip's inner `TextBlock` means that when
the chip is visible the automation tree for that element carries both the chip label (`"1 missing"`,
from its `Text`) and the full description (`BadgeTooltip`, from `HelpText`) — screen readers surface
both without requiring tab focus on the card. The `AutomationLabel` VM property and the call-site
`AutomationProperties.Name` binding are dropped entirely.

### D8 — Chip style: inline in template vs. static resource style

**Decision:** Define the chip geometry inline in the `SummaryCard` template rather than as a shared
`Style` resource.

**Rationale:** The chip appears in exactly one location (inside `SummaryCard`). Extracting a shared
chip style now would require defining it in `SettingsControls.xaml` or a sibling file and importing
it; the inline definition keeps the diff scoped to `SettingsStyles.xaml` only. If a second consumer
appears it can be extracted then.

### D9 — `BadgeTooltipText` format

**Decision:** `"{FolderStatusSummary} · folders that no longer exist"` when `HasMissingFolders`;
`string.Empty` otherwise.

This produces `"1 folder missing · folders that no longer exist"` / `"2 folders missing · folders
that no longer exist"`. The former `AlertToolTip` value was the static string `"Folders that no
longer exist"` (`SettingsWindow.xaml:191`). The combined format provides both the count and the
description in one read; the smoke test found the original em-dash separator rendered too wide, so
the separator is now a spaced middle dot (see Post-smoke adjustments). The middle dot is a compact
informational separator, not a punctuation break in a sentence.

---

## Alternatives Rejected

1. **`skip_specs: true`** — rejected. `openspec/specs/settings-theme/spec.md:360-364` is a durable
   scenario explicitly pinning how missing folders are reported. Changing from an alert line to a
   chip+badge changes what "reports the number of missing folders in the danger tone" looks like; the
   spec delta must record the new observable behavior.

2. **Keep the alert line, add chip and badge on top** — rejected. The brief explicitly states the
   fourth line is removed and the alert DP trio is dead code with one consumer. Keeping both would
   leave unreachable template code and no user-visible value.

3. **`Detail` property as the chip vehicle** — rejected. The `Detail` line is a text row below the
   value, not an inline chip beside the value. The E1 design places the chip on the value row.

4. **`int BadgeCount` DP** — considered. Using a string is simpler at the template level (direct
   TextBlock binding, null collapses via trigger). An int would require a converter or StringFormat
   on the binding. Rejected in favor of string.

5. **Single combined VM property for both chip text and badge count** — rejected. Chip and badge are
   separate visual objects with different sizes and positions; decoupling them gives flexibility and
   matches the independent DPs pattern.

---

## Deviations

- The brief says `BadgeCount` type drives the count badge; the binding site uses
  `MissingFolderCount` with `StringFormat={}{0}` rather than a new VM string property, keeping the
  interface smaller. The count is always the same integer as `MissingFolderCount`; no VM string
  property is needed.
- The chip container is a `WrapPanel`, not a horizontal `StackPanel`, to preserve `TextTrimming`
  on `ValueText` for other cards (see D4). The approved E1 behavior is unchanged.
- Accessibility is provided via `AutomationProperties.HelpText` on the chip's inner `TextBlock`
  rather than `AutomationProperties.Name` on the card (see D7). No `AutomationLabel` VM property
  is added.
- `tests/CLIHub.Tests/Controls/SummaryCardTests.cs` was not listed in the original impact set: its
  default-visibility test asserted the removed `IsAlertVisible`. The test was renamed and retargeted
  to `IsBadgeVisible` (same invariant, new master flag); without it the test project would not
  compile. The proposal impact list and task 6.4 now include this file.

---

## Open Questions

1. **Icon block overlay clip.** The count badge overhangs the 24×24 icon block. WPF `Panel`
   derivatives default to `ClipToBounds="False"`, so the negative-margin overhang should be visible.
   Verify during implementation that no ancestor panel clips it; if one does, set
   `ClipToBounds="False"` explicitly on the column-1 container `Grid`.

2. **`MissingFolderCount` on the interface — contract hygiene.** WPF data binding resolves property
   paths against the runtime object type, not the declared interface type, so the binding
   `ProjectRegistry.MissingFolderCount` would bind successfully at runtime even without promoting the
   property. Promotion is still required as contract hygiene: `MissingFolderCount` is now a member
   the XAML explicitly depends on, and omitting it from the interface would leave a silent coupling
   between the template and the concrete VM class. The only implementer is `ProjectRegistryViewModel`
   and tests use `Mock.Of<IProjectRegistrySection>()`, which will auto-satisfy the new member.

3. **`Sizing.FontSize.Chip` token — verified.** `src/CLIHub/Themes/Common/Sizing.xaml:26` defines
   `<sys:Double x:Key="Sizing.FontSize.Chip">11</sys:Double>`. The token exists and resolves to 11;
   use `FontSize="{StaticResource Sizing.FontSize.Chip}"` on the chip's inner `TextBlock` as stated
   in task 4.1. No open question remains here.

---

## Post-smoke adjustments (2026-11-08)

The first manual smoke of the E1 layout surfaced three visual issues. The fixes below are
presentation-only refinements; the approved E1 behavior (danger chip inline with the value, count
badge overlaid on the icon block's top-right corner, shared tooltip, both collapsing when no folder
is missing, 75 DIP card height) is unchanged.

1. **Tooltip separator: em dash → middle dot.** The rendered em dash (`—`) in the shared tooltip
   read as too wide at the card's font size. `BadgeTooltipText` now composes
   `"{FolderStatusSummary} · folders that no longer exist"` (spaced middle dot, U+00B7). The
   singular/plural behavior is unchanged. Supersedes the em dash quoted in D9 and the mockup.

2. **Icon count badge enlarged.** The 15 DIP badge with its 2 DIP inside-drawn ring left a visible
   red core of only 11 DIP, and the 9 DIP digit was hard to read. The overlay is now
   `MinWidth="18"`, `Height="18"`, `Padding="3,0"`, `CornerRadius` from `Sizing.CornerRadius.Chip`
   (6), `FontSize="10.5"`, with
   `Margin="0,-7,-7,0"` so the enlarged badge keeps roughly the same overlap over the icon glyph
   (left edge 13 DIP from the grid's left, versus 14 before). Fill, ring, text brush, and ZIndex are
   unchanged. Geometry check: the right edge lands 2 DIP inside the card's 9 DIP padding and 3 DIP
   clear of the 1 DIP card border — no clipping, no collision (and no ancestor clips; see open
   question 1).

3. **Chip vertical alignment.** `WrapPanel` places children at the line origin and does not honor
   child `VerticalAlignment` within a line, so the chip sat visibly higher than the value, which
   itself carries `Margin="0,2,0,0"`. The chip's `Margin` is now `"8,3,0,0"` so its ~18.6 DIP box is
   vertically centered against the ~21.2 DIP value line (2 DIP value top margin +
   (21.2 − 18.6) / 2 ≈ 3.3). `VerticalAlignment="Center"` is kept to record intent. The value
   `TextBlock` and the `WrapPanel` are unchanged, so `TextTrimming` behavior (D4) is unaffected.
   Supersedes the `Margin="8,0,0,0"` quoted in task 4.1.

4. **Badge shape: rounded square/rectangle, not a circle.** After the confirmed smoke the count
   badge corner radius is 6 (`Sizing.CornerRadius.Chip`, matching the chip vocabulary): an 18×18
   rounded square for one digit, and a slightly wider rounded rectangle when more digits grow
   `MinWidth`. Height and offset are unchanged. Supersedes the circle-like radii (8, then 9) above.
