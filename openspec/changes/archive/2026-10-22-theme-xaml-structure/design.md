# Design: Theme XAML Structure

## Change History

**2026-10-22 correction (orchestrator)**: Stage 2 design was originally wrong. The initial plan proposed removing `SettingsControls.xaml`'s merge of `Controls.xaml` and letting it resolve `Sizing.*` tokens from App.xaml's Controls merge. That violates WPF's StaticResource resolution model (XamlResourceReferenceTests.cs:17-19, docs/architecture/ui.md:16): a loose `ResourceDictionary` resolves `StaticResource` only against its own keys plus its own merged dictionaries, never application-level siblings. The corrected design keeps every dictionary's existing merges intact and eliminates redundancy by shortening App.xaml's merge list to 3 entries (DarkTheme, SlimScrollBar, SettingsStyles), relying on the transitive merge contribution of the SettingsStyles→SettingsControls→Controls→Sizing chain. No other stage was affected.

---

## Stage 1 Design (Accepted and Implemented)

### Goal

Consolidate per-theme data into a single `ThemeDescriptors.cs` record; eliminate the three DataTrigger sets in `SettingsAppearancePage.xaml` (45 XAML lines removed).

### Decision

Introduce `src/CLIHub/Themes/ThemeDescriptors.cs` with `ThemeDescriptor` record holding `(AppTheme Theme, string Name, string Description, string? PalettePath, string CardBrush, string DotBrush, string BarBrush)` and `ThemeDescriptors.All` returning 4 entries (Dark, Light, Blue, System). The palette path is nullable because Dark is the base palette (null means no additional merge). System's preview brushes alias Dark's brushes.

Rewrite `SettingsViewModel.cs:349-357` AppThemeOptions initialization to derive from `ThemeDescriptors.All`. Update `SettingsAppearancePage.xaml` card template to bind the three preview brushes directly, removing the 3 DataTrigger sets.

Simplify `ThemeCatalog.cs:19-25` ResolvePalettePath to delegate to `ThemeDescriptors.All`.

### Rejected alternatives

- **Move preview brush definitions to ThemeDescriptors**: Would require brush objects in Core (no WPF dependency) or ThemeDescriptors importing WPF types. Brush keys (strings) in the descriptor + XAML DynamicResource resolution preserves the separation.
- **Keep DataTrigger sets with descriptor lookup inside a converter**: Replaces 3 trigger sets with 3 converter bindings, but the converter still needs theme→brush-key logic (duplicates descriptor). Direct binding is simpler.

### Added guards

`ThemePreviewResourceKeyTests.cs` validates descriptor brush keys bidirectionally: descriptor keys resolve in the App-level merge tree, and every `ThemePreview*Brush` resource key has a descriptor reference.

---

## Stage 2 Design (Accepted and Implemented, Corrected Per Orchestrator Review)

### Goal

Extract theme-picker data to a new `ThemePickerData.xaml` dictionary; reduce `App.xaml` merges from 5 to 3 by leveraging the transitive merge chain. Eliminate duplicate instantiation of Controls.xaml (down to 1 instance) while accepting three Sizing.xaml instances as unavoidable.

### Decision

Create `src/CLIHub/Themes/ThemePickerData.xaml` merging `/Themes/Sizing.xaml`. Move from SettingsStyles.xaml to ThemePickerData.xaml: the 12 preview brushes (ThemePreview*Brush keys) and the 4 picker styles (ThemeOptionItem, ThemeOptionGroup, AccentOptionItem, AccentOptionGroup).

Edit `SettingsStyles.xaml` MergedDictionaries: insert `ThemePickerData.xaml` as first entry, keep Controls and SettingsControls merges.

Edit `App.xaml`: remove individual merges of Controls and SettingsControls; keep only DarkTheme, SlimScrollBar, SettingsStyles.

**Final structure**:
- ThemePickerData.xaml merges Sizing.xaml
- SettingsControls.xaml merges Sizing.xaml (unchanged)
- Controls.xaml merges Sizing.xaml (unchanged)
- SettingsStyles.xaml merges Controls, ThemePickerData, SettingsControls
- App.xaml merges DarkTheme, SlimScrollBar, SettingsStyles (3 total)

Result: Controls.xaml instantiated once (via SettingsStyles), Sizing.xaml instantiated three times (Controls→Sizing, ThemePickerData→Sizing, SettingsControls→Sizing).

### Rejected alternatives

- **Let ThemePickerData merge Controls instead of Sizing**: Would bring Controls twice to SettingsStyles level (once from ThemePickerData, once from SettingsStyles' own Controls merge). The corrected design has SettingsStyles merge Controls for its BasedOn references, and both leaf dictionaries (ThemePickerData, SettingsControls) merge only Sizing.

---

## Stage 3 Design

### Goal

Replace the nine near-identical summary-card blocks in `SettingsWindow.xaml` (Window.Resources spans
31-643) with one control, preserving every binding, glyph, tone, tooltip and conditional line.

### Decision

**`SummaryCard : Control`** in `src/CLIHub/Controls/SummaryCard.cs`, `DefaultStyleKey` set, template as
an implicit style in `SettingsStyles.xaml` (the `SettingCard`/`SectionHeader` precedent). Dependency
properties: `Icon`, `IconTone`, `Label`, `Value`, `ValueTone`, `ValueToolTip`, `Detail`, `DetailToolTip`,
`Alert`, `AlertToolTip`, `IsAlertVisible`.

Why eleven properties rather than the four the first draft assumed:

- The icon glyph carries a tone that is not derivable from the card: accent for Projects/Registry/Agents
  anchor cards, success for the agent cards, muted for the daemon/discovered/probe cards.
- The value carries its own tone: the daemon card colours the watcher state (success/accent/danger) and
  the other cards use the primary text tone.
- Two independent detail slots exist: the muted summary line (with its own tooltip) and the registry's
  folder-health line, which is danger-toned and only visible while folders are missing. A single
  newline-joined `Detail` string could not express the different tone and visibility.

**`SummaryCardTone { Neutral, Accent, Success, Danger, Muted }`** is a new vocabulary in the same file.
`Muted` was added during implementation: the DEFAULT agent card mutes its value while the value is the
"None" placeholder (the old markup had a `DataTrigger` on `HasDefaultAgent`), and `Neutral` deliberately
means the primary text tone. The section view model exposes `DefaultAgentValueTone`, derived from
`HasDefaultAgent`, so the card renders the placeholder tone without a converter or a string trigger.
Rejected: extending `SettingBadgeTone` with `Danger`. A badge tone describes the badge chip inside
`SettingCard`; badges never use the danger tone, and widening that enum would make every existing badge
style branch handle a state it cannot render.

**`WatcherStateTone` on the view model** instead of three colour `DataTrigger`s on the label text. It is
assigned in the same `switch` that produces `WatcherStateLabel` (`SettingsViewModel.cs:1683`), so the
label and its tone cannot disagree. Rejected: keeping the triggers in the template â€” the control would
then hard-code three English label strings, which is exactly the coupling this refactor removes.

The registry alert keeps a fixed danger tone by design (the only alert consumer); its visibility binds
`ProjectRegistry.HasMissingFolders`, so no tone property is needed for it.

### Rejected alternatives

- **Data-driven item template** (view-model card records plus an `ItemsControl`): the card content binds
  to three different sections of the view model (`ProjectRegistry.*`, `AgentsState.*`, root properties),
  so the records would need to carry prepared strings and the templates would duplicate the layout anyway.
- **Keeping the block markup and extracting only a shared `Border` style**: leaves nine copies of the
  text/icon composition, which is where the duplication actually lives.

### Risks

- Binding drift while replacing nine blocks: mitigated by the verified inventory table in `tasks.md` and a
  line-by-line smoke of the three pages.
- A tone that looks right in the dark palette but wrong in light/blue: the tone palette is the same one
  the badges already use, so no new colour pairs are introduced.

## Stage 4 Design

### Goal

Remove the ten per-page visibility `Style` blocks (`SettingsWindow.xaml:866-994`) and the three
header-template trigger chains (`:776-852`) while keeping the eager page lifetime, the placeholder page,
and the per-page header slots.

### Decision

**Page metadata carries template keys.** `SettingsPage` gains `HeaderTitleExtraKey`, `HeaderActionKey`,
`SummaryRowKey` (`string?`, default null); only `updates`, `projects`, `projects-registry` and `agents`
set them. `SettingsViewModel` therefore names the page-specific templates, and the window stops owning a
key-to-template switch.

**Header slots become three bound `ContentControl`s.** `ContentTemplate` resolves the metadata key
through `ResourceKeyToTemplateConverter`; `Visibility` binds the same key through
`EmptyToCollapsedVisibilityConverter`. A null key collapses the slot, which is what the current
`Visibility=Collapsed` defaults do.

The converter is an `IMultiValueConverter` over `[key, slot element]` and resolves through
`FrameworkElement.TryFindResource`, not `Application.TryFindResource`: five of the six templates are
declared in `SettingsWindow.Resources`, and `UpdatesHeaderAction` depends on the window-scoped
`StatusChip` styles, so they cannot move to the application level. `Application.TryFindResource` stays as
the fallback when no element is supplied. Consequence, accepted here and guarded in stage 6: a renamed
template key resolves to null, leaving the slot visible but empty instead of failing at build time.

**Page visibility becomes one attribute per page.**
`Visibility="{Binding SelectedPage.Key, Converter={StaticResource PageKeyVisibility}, ConverterParameter=<key>}"`
with `PageKeyVisibilityConverter` returning `Visible`/`Collapsed`. The placeholder page binds the new
`SettingsViewModel.IsPlaceholderPageVisible` (`!SelectedPage.IsImplemented`).

Why not the alternatives:

- **A shared `Style` with a `DataTrigger` comparing `Tag` to `SelectedPage.Key`**: a `DataTrigger` compares
  one binding to a literal value; it cannot compare two bindings, and `Setter.Value` does not support
  `MultiBinding`. The Style version is not implementable in WPF.
- **`ItemsControl` of pages created by the view model (factories)**: the page UserControls must be
  instantiated while application-level resources are resolvable, and moving view creation into the view
  model reverses the repository's MVVM direction. The remaining "one registration point" idea is parked
  in `docs/backlog.md` instead.
- **A `DataTemplateSelector` keyed on the page key**: it would need the same key-to-view map in a third
  place (the selector), so it replaces three trigger chains with a class plus a dictionary without
  removing the duplication.

### Risks

- A page whose `ConverterParameter` does not match its metadata key would show nothing or two pages at
  once. Mitigation: the ten page keys and template keys are enumerated in `tasks.md` 4.1/4.4/4.5 and the
  smoke test walks every page, including the placeholder.
- `SelectedPage` changes must raise `IsPlaceholderPageVisible`; the property-change batch that already
  raises `SelectedPage` is the one place to extend, and a test asserts it.

## Stage 5 Design

### Goal

Tokenize only FontSize and CornerRadius values that repeat OR encode a UI role, following the "repeats or encodes a role" rule. Reorder `Sizing.xaml` by category. Rename `Sizing.CornerRadius.Chunky` to a role name. Keep every numeric value identical (no visual change).

### Measured tree

FontSize literals in `src/CLIHub/Views/*.xaml` after stage 3: 37 (8×1, 9×1, 10×3, 11×3, 12×6, 13×4,
14×8, 15×1, 16×9, 24×1). Seven of them live in the legacy `MainWindow.xaml`, which is deliberately
untouched. Every editable 12 pt literal is an icon glyph (`IconGlyphs.Add`/`Refresh`/`Palette`), not
text.

| Value | Sites | Role | Action |
| --- | --- | --- | --- |
| 16 | 9 | icon glyph in a chip-sized icon button (row/header actions) | `Sizing.FontSize.Glyph` (new, 16) |
| 14 | 7 (+1 legacy) | icon glyph in a menu item or window-chrome button | `Sizing.FontSize.GlyphSmall` (new, 14) |
| 12 | 4 (+2 legacy) | icon glyph in an inline add/refresh/palette affordance | left literal — deviation, see below |
| 11 | 0 (+3 legacy) | text in `MainWindow.xaml` only | untouched file; `Sizing.FontSize.Section` already exists at 11 |
| 13 | 4 | window title, sidebar nav glyph, registry tile glyph, launch-window glyph | left literal (one-off tier) |
| 10, 9, 8, 15 | 6 | small glyph tier and single-use glyph sizes | left literal (one-off) |
| 24 | 1 | `MainWindow.xaml` display text | untouched file |

CornerRadius literals after stage 3 (nine summary-card icon blocks collapsed into one template):

| Value | Sites | Role | Action |
| --- | --- | --- | --- |
| 6 | 3 | SummaryCard icon block (24×24), SettingCard icon block (28×28), agent logo tile (32×32) | SummaryCard → `Sizing.CornerRadius.IconBox` (new, 6) |
| 9 | 2 | Projects-page project tile, theme-picker dot/selection marker | left literal (decorative/identity) |
| 13 | 1 | accent swatch | left literal (decorative) |
| 7 | 1 | registry badge | left literal (decorative) |
| 0 | 2 | window corners | left literal (structural) |

### Decision

**New tokens**: `Sizing.FontSize.Glyph` = 16, `Sizing.FontSize.GlyphSmall` = 14,
`Sizing.CornerRadius.IconBox` = 6 (the 24×24 icon block of the `SummaryCard` template).

**Renames**: `Sizing.CornerRadius.Chunky` → `Sizing.CornerRadius.Card` (value 8, 9 references) and
`Sizing.Spacing` → `Sizing.Spacing.Standard` (value 8, 2 references), at the definition and every
reference in `Themes/*.xaml` and `Views/*.xaml`; `docs/architecture/ui.md` follows the spacing rename.
No numeric value changes.

**Sizing.xaml reordering**: font family → font size → spacing → corner radius → component groups
(window chrome and panes, content lists and rows, buttons and inputs, toggle), alphabetical within each
group, with the three appended "Additional …" sections folded in. The header states the
`Sizing.<Category>.<Property>` convention, the file shape, and the rule that a value earns a token when
it repeats or names a role — never merely because it exists.

**Deliberately literal**:

- 12 pt glyphs at `SettingsProjectsPage.xaml:136`, `SettingsAgentsPage.xaml:109`,
  `SettingsProjectRegistryPage.xaml:74`, `SettingsAppearancePage.xaml:141` — see the deviation.
- `MainWindow.xaml` (legacy dead window): all seven of its FontSize literals, untouched.
- Font sizes 10, 13, 15, 24, 9 and 8: one-offs that do not repeat in a role.
- Radii 9 (project tile and theme-picker marker), 13 (accent swatch), 7 (registry badge), 0 (window
  corners): decorative or structural, single use.
- The remaining 6 px radii at `SettingsStyles.xaml:229` (28×28 SettingCard icon block) and
  `SettingsAgentsPage.xaml:140` (32×32 agent logo tile) are not the SummaryCard icon block.

### Deviations

- **D5.1 — the 12 → Body rule has no text consumer.** The pre-sweep measurement counted six 12 pt
  literals; two are in `MainWindow.xaml` (untouched), and the four editable ones are all icon glyphs
  (`SettingsProjectsPage.xaml:136` Add, `SettingsAgentsPage.xaml:109` Refresh,
  `SettingsProjectRegistryPage.xaml:74` Add, `SettingsAppearancePage.xaml:141` Palette). `Body` is a
  text role, and the approved glyph tokens cover only 16 and 14 pt. Applying `Body` to glyphs would couple icon sizes to body text. Review decision: those four glyphs repeat in one role, which the token rule allows, so they use a dedicated `Sizing.FontSize.GlyphTiny` (12) instead of a text token. The only 12 pt literals left are in the untouched legacy window.
- **D5.2 — no 11 pt replacement site.** All three 11 pt literals are in the untouched
  `MainWindow.xaml`; `Sizing.FontSize.Section` (11) already exists and is used by the shared styles.

### Rejected alternatives

- **Tokenize all repeated literals regardless of role**: The 9-corner and 13-corner theme-picker radii repeat (2 uses each) but encode theme-picker brand identity, not a reusable role. Tokenizing them as `Sizing.CornerRadius.ThemePickerDot` would pollute Sizing.xaml with page-specific decoration.
- **Tokenize icon FontSize values**: Icon glyphs at 8/9/10/11/13/14/16pt are glyph-specific kerning adjustments (Segoe MDL2 Assets rendering), not text roles. Tokenizing them as `Sizing.FontSize.IconSmall/Medium/Large` would imply they are interchangeable sizes, which they are not (each glyph has a specific size for visual balance). Only the two repeating glyph tiers (16, 14) earned tokens.

---

## Stage 6 Design

### Goal

Add three static test guards (color literals, StaticResource on palette keys, duplicate x:Key); fix drifted comments; update docs with add-page recipe and current theme data locations; park deferred work in backlog.

### Decision

**Test guards**:
1. `ThemeColorLiteralTests.cs`: Scan all XAML in `src/CLIHub/Views/` and `src/CLIHub/Themes/` for color literals (`Color="#`, `Background="#`, etc.). Allowlist: `*Theme.xaml` (palette definitions), `ThemePickerData.xaml` (preview brushes if they contain literals), `MainWindow.xaml` (legacy file with `#2C3E50`, `#BDC3C7`, `#34495E`). Fail with file/line for each violation.

2. `ApplicationResourceKeyTests.cs` with two methods:
   - `RejectsStaticResourceOnPaletteBrushKeys()`: Load all palette files (`*Theme.xaml`), extract SolidColorBrush x:Key values, scan all XAML for `StaticResource <key>` where `<key>` is a palette brush. Fail with file/line.
   - `RejectsApplicationLevelDuplicateKeys()`: Walk `App.xaml` MergedDictionaries recursively (DarkTheme, SlimScrollBar, SettingsStyles + transitive merges), collect all x:Key values per file. Assert no duplicate EXCEPT keys from `*Theme.xaml` files (which intentionally overlap and are merged conditionally). Fail with file1/file2.

**Comment fixes**:
- `LaunchWindowStyles.xaml` (lines 5-11): Replace "dark theme applies to that window only" with "Merged from LaunchWindow.xaml (not from App.xaml) so that styling is scoped to this window. Palette brushes use DynamicResource and resolve from the application-level palette."

**Doc updates**:
- `docs/architecture/ui.md`: Add section "## Adding a Settings Page" with 6-step recipe (create UserControl, extend SettingsPage metadata, add to AllPages, define optional header templates, add instantiation to SettingsWindow page host, smoke test). Add section "## Theme Data Locations" listing Core (AppTheme.cs), WPF descriptor (ThemeDescriptors.cs), palette files, preview data (ThemePickerData.xaml), test guard (ThemePaletteParityTests.cs).

**Backlog entries** (`docs/backlog.md`):
1. **Title**: "Consolidate Settings page registration"; **Domain**: UI; **Severity**: Low (maintenance ergonomics); **Evidence**: SettingsViewModel has separate structures for page metadata (SettingsPage record) and page host setup (per-page UserControl instantiation in SettingsWindow.xaml); adding a page touches 2-3 places; **Remedy**: Single registration point — page metadata includes Func<UserControl> factory, eliminating per-page XAML; **Trigger**: Adding a 5th page group or 15th total page.

2. **Title**: "Migrate remaining FontSize/CornerRadius literals"; **Domain**: UI; **Severity**: Low (consistency); **Evidence**: After Stage 5, some FontSize and CornerRadius literals remain (decorative/single-use values not promoted to tokens per the "repeats or encodes a role" rule); **Remedy**: Audit when a second consumer of the same literal emerges (proving reusable role), then tokenize; **Trigger**: Copy-pasting a View and noticing a repeated literal lacking a token.

### Rejected alternatives

- **Put SummaryCard template in Generic.xaml**: `Generic.xaml` is the standard WPF location for custom control templates in a Themes/ directory, but CLIHub's existing Settings controls (`SettingCard`, `SectionHeader`) have their implicit styles in `SettingsStyles.xaml`. Consistency with existing patterns outweighs the WPF convention here.

---

## Open Questions

- **Single Settings-page registration point.** Deferred: the page metadata record and the XAML page host are still two places, and the remedy (a page factory on the metadata) moves view creation into the view model. Parked in `docs/backlog.md` (stage 6) with a revisit trigger.
- **Header slot keys in metadata.** The three template-key properties are strings resolved at runtime, so a renamed template shows as an empty slot rather than a build error. Accepted for now: the alternative (typed template references) would put WPF view types into the settings view model. Revisit if a second window needs the same per-page slots.

## Impact Summary

**Lines removed**: ~550 (45 from Stage 1 DataTriggers, ~450 from Stage 3 card blocks, ~50 from Stage 4 page trigger blocks, header trigger consolidation).

**Lines added**: ~120 (ThemeDescriptors.cs, SummaryCard.cs + template, metadata properties, test guards).

**Net**: ~430 lines removed from XAML, moved to descriptive structures.

**Runtime impact**: None. Controls.xaml instantiated once (three times before stage 2). Sizing.xaml instantiated three times (was three). SummaryCard template instantiated 9 times at render (was 9 inline blocks). UserControl lifetimes unchanged. Palette brush resolution unchanged (DynamicResource paths preserved). No visual change.

**Maintenance wins**: Adding a 5th theme: 1 descriptor entry (was 7 edit sites). Adding a Settings page: extend record + add to array (was touch 3-4 files for page + header metadata). Adding a summary card: instantiate control (was copy/paste 50-line block).