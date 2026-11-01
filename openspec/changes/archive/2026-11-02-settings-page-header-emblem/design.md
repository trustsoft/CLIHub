# Design

## Context

Verified current state (re-checked in this session):

- `src/CLIHub/Views/SettingsWindow.xaml:405-479` — the page-header `Grid`, `Margin="24,16,24,0"`, two
  columns (`*`, `Auto`) and three rows (title block / summary row / hairline).
  - Row 0, Column 0 (`:416-443`): a `StackPanel` holding a `WrapPanel` with the title `TextBlock`
    (`Style="{StaticResource PageHeader}"`, bound to `SelectedPage.Title`) plus a `ContentControl`
    slot for `HeaderTitleExtraKey` (margin `10,0,0,0`, visible only when the key is non-empty via
    `EmptyToCollapsedVisibility`), and below the `WrapPanel`, the description `TextBlock`
    (`BasedOn="{StaticResource HintText}"`, margin `0,6,0,0`, collapsed when
    `SelectedPage.Description` is empty).
  - Row 0, Column 1 (`:445-457`): the header action slot (`HeaderActionKey`), margin `16,0,0,0`,
    stretched vertically, visible only when the key is non-empty.
  - Row 1 (`:460-472`): the summary row slot (`SummaryRowKey`), spans both columns, margin
    `0,14,0,0`.
  - Row 2 (`:474-478`): the hairline `Rectangle`, spans both columns, margin `0,14,0,0`.
- `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:26-34` — `SettingsPage` record; `Icon` is
  "The Segoe MDL2 glyph shown before the page title in the sidebar," already a plain `string`.
  `:41-110` — the catalog; every one of the 10 pages sets `Icon` (`IconGlyphs.Settings`,
  `.Keyboard`, `.BulletedList`, `.FolderOpen`, `.Robot`, `.Diagnostic`, `.Terminal`, `.Palette`,
  `.History`, `.Sync`). No page leaves it null — the type is non-nullable `string`, so every page
  already has a glyph to show; no opt-out is representable without changing the record.
- `src/CLIHub/Themes/IconGlyphs.cs:47` — `Settings = "\uE713"` (Segoe MDL2 Assets code point);
  other entries follow the same `\uXXXX` pattern.
- `src/CLIHub/Themes/SettingsStyles.xaml:148-152` — `PageHeader` style: `FontSize=14.5`
  (`Sizing.FontSize.Name`), `FontWeight=SemiBold`, `Foreground={DynamicResource
  TextPrimaryBrush}`; no `TextWrapping` setter, so the title `TextBlock` uses the WPF default
  (`NoWrap`) and overflows/clips rather than wrapping when its column is too narrow.
  `src/CLIHub/Themes/SettingsControls.xaml:25-29` — `HintText` style, used by the description
  `TextBlock` (`BasedOn="{StaticResource HintText}"`): `FontSize={StaticResource
  Sizing.FontSize.Section}` (11), `Foreground={DynamicResource TextMutedBrush}`, and
  `TextWrapping="Wrap"` — the description **does** wrap today, onto as many lines as its column
  width requires.
- `src/CLIHub/Themes/Sizing.xaml:19,28-31` — `Sizing.FontFamily.Icon` = "Segoe MDL2 Assets";
  `Sizing.FontSize.Glyph` = 16 (row/header-action buttons), `Sizing.FontSize.GlyphSmall` = 14 (menu
  items, window-chrome buttons), `Sizing.FontSize.GlyphTiny` = 12. None of the three existing glyph
  sizes is 20 DIP — the mockup's measured size is a new value for this component.
  `Sizing.Spacing.Standard` = 8 — the existing one-dimensional gap token; 12 is a different value,
  used nowhere else as a token.
- `openspec/specs/settings-theme/spec.md:243` — "Persistent page header" requirement text (quoted in
  full in `specs/settings-theme/spec.md` of this change, since MODIFIED must carry the whole block).
- `docs/architecture/ui.md:50-61` — "Settings Page Layout" section documenting the fixed header,
  summary rows, and the `ScrollViewer` boundary; no mention of a per-page glyph today.
- `ui/mockups/settings-page-emblem.html:145-166` — the four CSS variants. Variant B
  (`[data-variant="b"] .emblem`): `display: block; margin-right: 12px;` and `.emblem-glyph { font-size:
  20px; color: var(--muted); }` — no `width`/`height`/`background`/`border` on `.emblem` for B
  (those apply only to A/C). `--muted` maps to `TextMutedBrush` per the mockup's palette comment
  block (`:19-38`, `--muted: #9A9A9A` matching `TextMutedBrush`'s dark-theme value).
- `ui/mockups/README.md:5,44-65` — the mockup's current status line calls it "an open design
  comparison... nothing in it is implemented and no variant is chosen yet," and the dedicated section
  states the decisions it does **not** make: "which variant ships, and whether a shift of the title
  against the content below the divider is acceptable." This change answers both: B ships, and the
  shift (32 DIP, confirmed below) is accepted as part of choosing B.
- Accessibility precedent, corrected after independent verification: `AutomationProperties.Name` is
  used throughout the codebase (22 matches across `SettingsWindow.xaml`, `SettingsAgentsPage.xaml`,
  etc.) to label interactive controls for screen readers.
  `AutomationProperties.AccessibilityView` — the attached property the first revision of this
  document proposed setting to `Raw` — **does not exist in WPF**; it is a WinUI/UWP API. The WPF
  XAML compiler rejects it against the `presentation` namespace with `MC3072: The property
  'AutomationProperties.AccessibilityView' does not exist in XML namespace
  'http://schemas.microsoft.com/winfx/2006/xaml/presentation'`, confirmed twice: by the implementer's
  build failure, and independently by a verifier probe against `Microsoft.WindowsDesktop.App
  10.0.14` reference assemblies. WPF has no XAML-only attached property that removes a single
  element from the automation tree; doing so needs a code-level custom `AutomationPeer` (for example
  a `TextBlock` subclass whose `OnCreateAutomationPeer()` returns `null`). None of the window's other
  decorative glyphs — the sidebar's `.glyph` `TextBlock` (`SettingsWindow.xaml:372-377`), the
  `SettingCard` IconBox glyph, or the summary-card icons — use any such mechanism today; they are
  exposed as ordinary text elements. See Decision 5 for the corrected, verified design.

## Goals / Non-Goals

**Goals:**
- Add the page emblem to the header exactly as mockup variant B renders it: 20 DIP glyph, no tile, no
  border, no accent, `TextMutedBrush`, 12 DIP gap to the title text, vertically centered against the
  title+description block.
- Keep the emblem in its own grid column so the `UpdatesHeaderTitleExtra` chip keeps wrapping with
  the title inside the existing `WrapPanel`, unaffected.
- Keep the header's total height unchanged.
- Make the emblem non-interactive: no keyboard focus, no action, no tooltip. Accept that WPF exposes
  it as a text element in the accessibility tree the same way it already exposes every other
  decorative glyph in the window, rather than promising an exclusion WPF cannot deliver.
- Decide, and record, how the description's existing wrap behavior interacts with the emblem's
  32 DIP column at the window's `MinWidth` (640 DIP).

**Non-Goals:**
- No new `SettingsPage` field, no per-page opt-out (see proposal's Non-Goals).
- No visual variant beyond B (no tile, no accent — A, C, D are closed decisions, not open ones).
- No change to how the sidebar itself renders its glyph.
- No `Sizing.xaml` restructuring beyond whatever this change's two new values earn on their own
  merit (decided below).

## Decisions

### 1. Glyph source: reuse `SettingsPage.Icon` directly, no new metadata

`Icon` already exists, is non-nullable, and every catalog entry sets it
(`SettingsNavigation.cs:41-110`). The header already binds to sibling `SelectedPage.*` properties
(`Title`, `Description`, `HeaderTitleExtraKey`, `HeaderActionKey`, `SummaryRowKey`) the same way this
change will bind to `SelectedPage.Icon`. No new field, no catalog edit, no opt-out mechanism — the
brief's suspicion that a new field is unnecessary holds: the type system already guarantees a glyph
for every page.

**Alternative rejected:** adding an `EmblemIcon`/`ShowEmblem` field to `SettingsPage` to allow a future
per-page override or opt-out. Rejected because nothing in the approved mockup or the user's decision
calls for an opt-out ("applies to every page; no page opts out"), and speculative metadata with no
current consumer violates the project's no-unrequested-abstraction convention
(`AGENTS.md` "Use the existing MVVM... patterns before adding abstractions").

### 2. Variant B over A/C/D

The user already chose B; this is recorded for traceability, not re-opened:
- **A (40×40 tile)** repeats the `SettingCard` IconBox recipe but adds a bordered "button-like" tile
  for a non-interactive element — affordance mismatch, and the mockup's own bottom-note flags the
  risk of competing with card icons directly below.
- **C (accent tile)** competes with the Updates page's existing accent "Check for updates" header
  action and the active sidebar item's accent highlight — two accents visible at once on that page.
- **D (status quo)** is the problem this change fixes.
- **B (bare glyph)** is the smallest visual addition that still orients the user: no new surface, no
  accent, reuses only typography already present in the window (the icon font) in the same muted
  tone the description line already uses — `HintText` sets `Foreground={DynamicResource
  TextMutedBrush}` (`SettingsControls.xaml:25-29`), and the emblem uses `TextMutedBrush` directly
  (see Decision 4).

### 3. Placement: nested leading column inside the title block, not inside the title `WrapPanel`

**Corrected**, replacing the first revision's "new leading `Auto` column on the header `Grid` itself,"
which is the structure Decision 6 identifies as the cause of the Updates centering bug. The current
placement is described fully in Decision 6; this decision keeps the parts of the original rationale
that are unaffected by that fix.

The header `Grid` keeps its original two columns, `*` (title block) and `Auto` (header action) — this
change does not add a column to the header `Grid` itself. Instead, the title block (previously a bare
`StackPanel` in column 0) becomes a `Grid` with two columns of its own, `Auto` (the emblem) and `*`
(the title+description `StackPanel`, unchanged in content, nested one level deeper). The emblem is a
sibling of that `StackPanel` inside the new inner `Grid`, not a child of the `WrapPanel` inside it.

**Why not inside the `WrapPanel`:** the `WrapPanel` already holds the title `TextBlock` and the
`HeaderTitleExtraKey` slot (the Updates page's chip). If the emblem were the `WrapPanel`'s first
child, a narrow window could wrap the emblem onto its own line separately from the title, or — worse
— wrap the chip away from the title while the emblem stays put, changing today's wrapping behavior
for a feature untouched by this change. A sibling column is simpler and has zero interaction with
existing wrap behavior.

**Why not inside the title `StackPanel` (above the `WrapPanel`):** the emblem must align with the
vertical center of the *whole* title+description block (per the mockup), not just the title line.
A `Grid` column with `VerticalAlignment="Center"` set on the emblem element achieves this directly
against the `StackPanel`'s full measured height; nesting it inside the `StackPanel` would require an
inner `Grid` anyway, with no benefit over a sibling column.

**Row span:** the inner title-block `Grid` occupies only Row 0 of the outer header `Grid`, not Row 1
(summary) or Row 2 (hairline). The mockup never extends the emblem into the summary row or past the
hairline; it centers against "заголовок + описание" (title + description) only — rows 1 and 2 are a
different concern (cards, divider) that the emblem must not visually anchor to. See Decision 6 for why
the emblem also no longer shares its containing `Grid` with the header-action column.

### 4. Styling: literal values vs. new `Sizing.xaml` tokens

**Font size (20 DIP):** a new value, not equal to any of `Sizing.FontSize.Glyph` (16),
`.GlyphSmall` (14), or `.GlyphTiny` (12). Decision: add a new token, `Sizing.FontSize.GlyphLarge`
(20), rather than a literal. Rationale: `Sizing.xaml`'s own stated rule is "A value earns a token when
it repeats across consumers or names a UI role" (`Sizing.xaml:14-15`) — this value does name a UI
role ("large decorative glyph," distinct from the existing "row/header-action button glyph" and
"menu/chrome-button glyph" roles already tokenized), and every other glyph size in the file already
has a token following the `Sizing.FontSize.Glyph*` family, so an untokenized literal would break the
file's own established pattern for this exact category of value (icon-font sizes), even though today
it has only one consumer.

**Gap to title (12 DIP):** `Sizing.Spacing.Standard` is 8 DIP and documented as "Spacing between
adjacent controls in simple one-dimensional groups" — a different, smaller value already carrying a
different role (that 8 DIP rhythm appears in `SpacedPanel` usages elsewhere; reusing it here would
either shrink the approved 12 DIP gap or overload the token with two different pixel values depending
on context, which `Sizing.xaml`'s own convention forbids — one token, one value). Decision: this
margin stays a literal `Margin="0,0,12,0"` on the emblem element, matching the existing pattern where
single-use layout margins in `SettingsWindow.xaml` are literals (e.g. `Margin="10,0,0,0"` on the
title-extra `ContentControl`, `Margin="16,0,0,0"` on the header-action slot, `Margin="0,6,0,0"` on the
description) — none of those margins have tokens either, and this gap has exactly one consumer (this
column), so it fails the "repeats across consumers" test that would justify a token.

**Color:** `TextMutedBrush` via `DynamicResource`, matching the mockup's `--muted` mapping and the
existing convention of reading all Settings chrome colors from palette brushes
(`docs/architecture/ui.md:36`, "reference colors exclusively through palette brushes").

### 5. Accessibility: accept WPF's actual behavior, apply no automation attribute

**Corrected decision**, replacing the first revision's `AutomationProperties.AccessibilityView="Raw"`,
which does not exist in WPF (Context, above) and would not have compiled.

Verified facts:
- `AutomationProperties.AccessibilityView` is a WinUI/UWP attached property, absent from WPF's
  `presentation` namespace; setting it fails the XAML compile with `MC3072`.
- `AutomationProperties.Name=""` on a `TextBlock` is a no-op, not a weaker-but-still-useful fallback.
  `FrameworkElementAutomationPeer.GetNameCore()` falls back to `GetPlainText()` when the explicit
  name is empty, so the accessible name becomes the glyph's own private-use code point.
  `TextBlockAutomationPeer.IsControlElementCore()` returns the base (`true`) result for a
  non-templated `TextBlock`, so the element stays in the Content view regardless. A verifier probe
  against a real window whose header mirrors this one confirmed the outcome directly:
  `TextBlockAutomationPeer: Name='\uE713' CtrlType=Text IsCtrl=True IsContent=True`.
- WPF has no XAML-only, per-element switch that removes a node from the automation tree. The only
  correct mechanism is a custom `AutomationPeer`: a small `TextBlock` subclass overriding
  `OnCreateAutomationPeer()` to return `null`.

Decision: apply no automation attribute to the emblem at all. An empty `AutomationProperties.Name`
would misrepresent the element's actual accessible state to anyone reading the XAML later — it looks
like a deliberate accessibility treatment but is inert. Instead, the emblem is left as a plain
`TextBlock` bound to `SelectedPage.Icon`, with a short XAML comment stating that WPF has no
per-element accessibility-view switch, so the emblem is exposed as a text element exactly like the
window's other icon glyphs (Context, above). This is not a regression: every other decorative glyph
in the Settings window — the sidebar icon, the `SettingCard` IconBox glyph, the summary-card icons —
is already exposed the same way today. The emblem adds no new class of accessibility noise; it adds
one more instance of a pattern the window already has roughly twenty of.

**Alternative rejected: a `DecorativeTextBlock` control.** A `CLIHub.Controls` `TextBlock` subclass
overriding `OnCreateAutomationPeer() => null` (roughly 8 lines) would correctly remove a glyph from
the automation tree. Rejected for this change because it would solve exactly one glyph while the
other ~20 identical decorative glyphs in the same window stay exposed — introducing a new control for
a single use, while leaving the inconsistency it was meant to fix everywhere else untouched, is not a
targeted fix, it is a half-applied one. The right scope for that control is an app-wide pass over
every decorative icon glyph, not this change. The residual gap is recorded in `docs/backlog.md`
("UI and theming" section) as the trigger for that future pass; this change's tasks add that entry
but do not implement the control.

### 6. Vertical centering: center against the title+description container, not against row 0

**Corrected decision**, replacing the first revision's "center in the header `Grid`'s row 0,"
which produces a visible bug on the "Updates" page and was caught in manual smoke.

**The bug.** The first revision placed the emblem at `Grid.Row="0" Grid.Column="0"` in the
three-column header `Grid` (`Auto` emblem / `*` title block / `Auto` header action), with
`VerticalAlignment="Center"`. WPF centers an element against the height of the row it occupies, not
against a sibling's height. Row 0's height is `Auto`, i.e. the tallest content in that row across all
three columns. The title+description `StackPanel` (column 1) is ≈40 DIP (title line + 6 DIP margin +
description line). The header-action `ContentControl` (column 2) is `VerticalAlignment="Stretch"` and
hosts whatever `DataTemplate` the active page names via `HeaderActionKey`
(`SettingsWindow.xaml:461-473`). On most pages that template is no taller than the title block — for
example `ProjectsHeaderAction`'s two icon buttons — so row 0's height equals the title block's height
and the emblem's row-centered position happens to coincide with the title block's center. On
"Updates," `UpdatesHeaderAction` (`:111-150`) is a two-row `Grid` stacking a status chip + "Check for
updates" button row and a "View release notes →" link row — taller than the title block — so row 0
grows to fit it, and the emblem, still centered against that taller row, drops below the title
block's optical center. The bug is specific to Updates today only because it is the one page whose
header action happens to be taller than the title block; any future page with a tall header action
would reproduce it.

**The fix: nest the title block in its own top-aligned container, and center the emblem against
that container instead of against the row.** The header `Grid` returns to two columns, `*` (title
block) and `Auto` (header action) — the layout it had before this change. The title block, previously
a `StackPanel` directly in column 0, becomes a `Grid` with `VerticalAlignment="Top"` and its own two
columns: `Auto` (the emblem) and `*` (the existing title+description `StackPanel`, now nested one
level deeper, unchanged in content). The emblem keeps `VerticalAlignment="Center"` and
`Margin="0,0,12,0"`, but now centers against this inner `Grid`'s own height, which is driven only by
its own content — the title+description `StackPanel` — never by the header-action column, because
the header action is no longer a sibling inside the same `Grid`. The outer `Grid`'s `VerticalAlignment="Top"`
on the title-block `Grid` keeps it (and the emblem inside it) pinned to the top of row 0 regardless of
how tall the header action makes that row, matching the pre-existing visual position of the title
block itself (which was never affected by the header action's height, since the `StackPanel` had no
stretch alignment).

The emblem still sits outside the title `WrapPanel` and outside the `StackPanel` — it is a sibling of
the `StackPanel` inside the new inner `Grid`, exactly as Decision 3's rationale for keeping it out of
the `WrapPanel` and out of the `StackPanel` still applies unchanged (the Updates title chip must keep
wrapping with the title; the emblem still needs to span the full title+description height, which the
inner `Grid`'s two columns give it without requiring the emblem to be a `WrapPanel` or `StackPanel`
child).

**Alternative rejected: center against row 0 (the bug).** This is the layout the first revision
shipped. Rejected because it is observably wrong on any page whose header action is taller than the
title block, and "no page shifts the emblem" is a correctness requirement, not a nice-to-have — the
whole point of the emblem is a stable per-page orientation cue; one that visibly jumps on exactly the
page that already has the most header chrome undermines that.

**Alternative rejected: a fixed top margin on the emblem instead of a nested container.** For
example, giving the emblem a top margin computed to visually center it against a title+description
block of "typical" height, without restructuring the grid. Rejected because it is a magic number that
only happens to be correct for the common case (title + description, one line each) and breaks again
the moment a description wraps to a second line (Decision 7) — the block's height is not fixed, so
any margin tuned against one height is wrong against another. The nested top-aligned `Grid` centers
against the title+description block's *actual* measured height on every page, including a page whose
description happens to wrap, with no page-specific tuning.

### 7. Narrow window (640 MinWidth): the description already wraps; the emblem narrows its column

Corrected from the previous revision of this document, which claimed the description does not wrap —
that was wrong. `HintText` sets `TextWrapping="Wrap"` (`SettingsControls.xaml:25-29`), and the header
description `TextBlock` is `BasedOn="{StaticResource HintText}"` (`SettingsWindow.xaml:432-442`), so
the description already wraps onto as many lines as its column requires at any window width, with or
without this change. The title `TextBlock` sets no `TextWrapping` (`PageHeader` style,
`SettingsStyles.xaml:148-152`), so it keeps the WPF default `NoWrap` and overflows/clips instead of
wrapping — this part of the original claim was correct.

The mockup's own CSS description (`.page-desc`, `settings-page-emblem.html:138`) has no wrapping
rule and is not a reliable model for this behavior: the mockup's narrow-window crop
(`settings-page-emblem.html:461-530`) shows the title and description staying on one line each only
because its sample text is short enough to fit, not because the real `HintText` style cannot wrap.

The emblem adds a fixed-width leading column (20 DIP glyph + 12 DIP margin = 32 DIP) inside the
content area, which narrows the title/description text column by that same 32 DIP at every window
width, including the 640 DIP `MinWidth`. For a page whose description is long enough to already be
near its column's wrap point — for example "Project Scanning" ("Watched directories, scanning rules,
and the background watcher.") or "Projects" ("Registered projects, their provenance, and manually
added entries.") — narrowing the column by 32 DIP can push the description onto an additional line
that it does not take today. The title does not gain a line (it does not wrap at all), but a title
close to its column's width can clip or overflow 32 DIP sooner than it does today.

Decision: accept the extra description line as a consequence of adding the emblem, rather than
reserving the emblem's width from some other element or shortening descriptions. The header is a
`RowDefinition Height="Auto"` (`SettingsWindow.xaml:407`), so an extra wrapped line grows the header
by that line's height automatically — this is the existing, already-specified wrap behavior
operating with 32 fewer DIP available, not new layout logic. The emblem's `VerticalAlignment="Center"`
(Decision 6) continues to center it against the inner title-block `Grid`'s full measured height
(driven only by the title+description `StackPanel`, per Decision 6's fix), so it re-centers correctly
whether the description takes one line or two. A scenario is added to the spec delta ("Long
description wraps instead of clipping") to make this explicit, since it is an observable behavior
change at narrow widths that the previous revision of this document incorrectly said could not
happen.

No mitigation is implemented for title overflow at extreme widths (per "no page opts out," no
truncation or ellipsis is added here) — this matches the existing, unspecified behavior of a title
that is already too wide for its column today, which this change does not newly introduce, only
narrows the column that triggers it.

### 8. No change to `SettingsPage`, no new ViewModel surface

Confirmed per Decision 1: implementation binds `SelectedPage.Icon` directly from existing exposed
state; no ViewModel changes, no new converter (the icon font glyph renders directly as `Text`, exactly
as `IconGlyphs.*` values already do throughout `SettingsWindow.xaml`).

## Risks / Trade-offs

- **[Risk]** A new `Sizing.FontSize.GlyphLarge` token has exactly one consumer at ship time, which
  looks like premature tokenization. → **Mitigation:** the token follows an established, already-
  tokenized family (`Sizing.FontSize.Glyph*`) rather than introducing a new category; the file's
  stated token-worthiness test is "repeats... or names a UI role," not "has two consumers today," and
  this value does name a role consistent with its siblings.
- **[Risk]** Optical weight of MDL2 glyphs varies (the mockup's own glyph-grid section flags this: some
  glyphs read "plotnee" / denser than others at larger sizes without a tile to contain them). →
  **Mitigation:** accepted as part of choosing variant B; no mitigation implemented (no per-glyph
  size tuning), consistent with "no page opts out."
- **[Trade-off]** The title block shifts 32 DIP right relative to the scrolling content below the
  divider (which stays at the 24 DIP margin). The mockup's own bottom-note raises this exact question
  ("Не смущает ли сдвиг заголовка...") and leaves it to the variant decision; choosing B (not A) was
  the user's accepted answer — 32 DIP, not A's 54 DIP.
- **[Risk]** Narrowing the description's text column by 32 DIP (Decision 7) can add a wrapped line
  for the longest existing descriptions at or near the 640 DIP `MinWidth`, growing the header's height
  on those pages at that width. → **Mitigation:** accepted, not mitigated — the header row is already
  `Height="Auto"` and already designed to grow with wrapped content; this is the existing mechanism
  running with less available width, not a new failure mode. The manual smoke task asks the user to
  confirm nothing clips and the wrapped result still reads correctly, rather than asserting no page
  ever gains a line.
- **[Residual gap]** The emblem is announced by assistive technology as a plain text element bearing
  its own private-use glyph code point, because WPF provides no per-element way to suppress that
  (Decision 5) short of a custom `AutomationPeer`. This is not unique to the emblem — every other
  decorative icon glyph in the Settings window already has the same property — so this change does
  not make the window's accessibility surface worse, only adds one more instance of an existing,
  pre-existing gap. → **Mitigation:** not fixed in this change; recorded as a `docs/backlog.md`
  entry (task 6.x) describing the `DecorativeTextBlock`-style remedy and naming every glyph site it
  would need to cover, so a future change can address the whole window in one pass instead of one
  glyph at a time.
- **[Risk, found in manual smoke, fixed in this revision]** Centering the emblem against the header
  `Grid`'s row 0 instead of against the title+description block made the emblem drop below the
  title's optical center specifically on the "Updates" page, whose header action
  (`UpdatesHeaderAction`) is taller than the title block. → **Mitigation:** Decision 6 restructures
  the header so the emblem centers against a container whose height is driven only by the
  title+description `StackPanel`, never by the header-action column, for every page including
  Updates. The manual smoke section adds a direct Updates-vs-another-page comparison (task 6.x) so
  this regression is caught again if it recurs.

## Migration Plan

Not applicable — this is a visual-only WPF change with no data, schema, or configuration migration.
Implementation order (detailed in `tasks.md`): add the `Sizing.FontSize.GlyphLarge` token, restructure
the title block into a nested top-aligned `Grid` holding the emblem and the title+description
`StackPanel` (Decision 6), with no automation attribute on the emblem and an explanatory comment,
update `CHANGELOG.md`, `docs/architecture/ui.md`, `ui/mockups/README.md`, and `docs/backlog.md`, run
the automated gate, then hand off to the user for the manual smoke check (visual comparison against
the mockup, the Updates-vs-another-page centering check, the `MinWidth` wrap check, and a
Tab-traversal/tooltip check) before requesting commit/archive approval.

## Open Questions

None. Every decision the brief flagged for planning-time judgment (token vs. literal, accessibility
mechanism, metadata field necessity, narrow-window wrapping) is resolved above with its rationale.
Decision 7 was corrected after the first revision misstated the description's wrapping behavior;
Decision 5 was corrected after independent verification showed `AutomationProperties.AccessibilityView`
does not exist in WPF and an empty `AutomationProperties.Name` is a no-op; Decision 6 was corrected
after manual smoke found the emblem shifting on the "Updates" page.
