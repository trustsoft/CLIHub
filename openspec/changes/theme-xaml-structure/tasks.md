# Tasks: Theme XAML Structure

Stage 1-2 are implemented, verified and accepted; their boxes are ticked and their text records what
was actually built, including the review corrections. Stage 3 is implemented and ticked. Stages 5-6 are
not started. Every stage ends with `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`,
`openspec validate --all`, and a manual acceptance checkpoint by the user. No stage commits.

---

## Stage 1: Theme Descriptor (accepted)

**Goal**: one WPF-layer source of truth for per-theme data; the Appearance page renders every card from
bindings instead of three `DataTrigger` sets.

- [x] 1.1 Add `src/CLIHub/Themes/ThemeDescriptors.cs`: `ThemeDescriptor(Theme, Name, Description, PalettePath, CardBrush, BarBrush)` and `ThemeDescriptors.All` in picker order (Dark, Light, Blue, System). `DotBrush` was dropped during review — see 2.R1.
- [x] 1.2 Extend `AppThemeOption` (`SettingsViewModel.cs`) with `PreviewCardBrush`, `PreviewBarBrush` and `bool UsesAccentPreviewDot`; build `AppThemeOptions` from `ThemeDescriptors.All`, keeping order and text.
- [x] 1.3 Add `src/CLIHub/Converters/ThemeBrushKeyConverter.cs` (resource key → brush through `Application.TryFindResource`) and register it as `ThemeBrushKey` in `App.xaml`.
- [x] 1.4 Replace the three `DataTrigger` sets in `SettingsAppearancePage.xaml` (card background, dot, bar) with two converter bindings and one `DynamicResource` dot plus a single trigger for the muted System swatch.
- [x] 1.5 Make `ThemeCatalog.ResolvePalettePath` descriptor-driven: resolve System to Dark/Light by the Windows mode first, then look the descriptor up. Dark, System-on-dark and unknown keep the base palette; System never resolves to blue.
- [x] 1.6 Gates: build 0/0, tests 1011/1011, `openspec validate --all` 30/30. Manual smoke confirmed by the user.

---

## Stage 2: Theme Picker Extraction and Merge-Reduction (accepted)

**Goal**: move the picker resources out of `SettingsStyles.xaml`, drop `App.xaml` from five merges to
three, and load `Controls.xaml` once at application level.

- [x] 2.1 Add `src/CLIHub/Themes/ThemePickerData.xaml` holding the eight `ThemePreview*Brush` keys and the four picker styles (`ThemeOptionItem`, `ThemeOptionGroup`, `AccentOptionItem`, `AccentOptionGroup`), moved byte-identically; it merges `/Themes/Sizing.xaml` because the styles reference only tokens.
- [x] 2.2 Remove the moved keys from `SettingsStyles.xaml` and merge `Controls.xaml`, `ThemePickerData.xaml`, then `SettingsControls.xaml`; header and merge comments describe the resulting chain.
- [x] 2.3 Reduce `App.xaml` to three merges (`DarkTheme`, `SlimScrollBar`, `SettingsStyles`) with a comment explaining the application-level entry point and why the settings layer must stay app-level.
- [x] 2.4 Make `SettingsControls.xaml` merge `/Themes/Sizing.xaml` instead of `Controls.xaml` (it uses only tokens), so the shared styles are not loaded twice.
- [x] 2.5 Rewrite the layering paragraph, add the merge-tree block and the rationale in `docs/architecture/ui.md`, and correct the "To add another theme" recipe that still described a `ThemeCatalog` switch arm and preview `DataTrigger`s.
- [x] 2.6 Gates: build 0/0, tests 1011/1011, `XamlResourceReferenceTests` green, `openspec validate --all` 30/30; application-level key set 130 → 131 with only `ThemeBrushKey` added and no key lost.
- [x] 2.R1 Review fix — keep the accent preview dot live: the descriptor no longer carries a dot key, `AppThemeOption.UsesAccentPreviewDot` drives one trigger in the page, and the dot binds `{DynamicResource AccentBrush}`.
- [x] 2.R2 Review fix — load `Controls.xaml` once: `ThemePickerData` and `SettingsControls` merge `Sizing.xaml`, `SettingsStyles` merges `Controls.xaml` itself. Measured result: `Controls.xaml` 1×, `Sizing.xaml` 3×.
- [x] 2.R3 Review fix — add `tests/CLIHub.Tests/Themes/ThemePreviewResourceKeyTests.cs`: preview keys referenced by name (descriptor values + XAML `StaticResource`) must exist in the App-level tree, and a declared-but-unreferenced preview key fails as an orphan. Mutation-verified in both directions.
- [x] 2.R4 Gates after the review fixes: build 0/0, tests 1012/1012, `openspec validate --all` 30/30. Manual smoke confirmed by the user, who also asked for the Dark theme description to read "Fluent Dark (default)" — applied.

---

## Stage 3: SummaryCard

**Goal**: replace the nine near-identical card blocks in `SettingsWindow.xaml` (Window.Resources 31-643;
`ProjectsSummaryCards` at 183, `RegistrySummaryCards` at 336, `AgentsSummaryCards` at 486) with one
control, without touching the header wiring (that is stage 4).

**Verified card inventory** (label / value / detail lines / icon):

| Template | Label | Value | Detail | Icon |
| --- | --- | --- | --- | --- |
| Projects | `PROJECTS` | `TotalProjectCount` | `ProjectMixSummary` + tooltip "Projects found by scanning plus projects added manually" | `FolderOpen` accent |
| Projects | `AGENTS` | `AgentsInstalledCount` | `AgentsSupportSummary` | `Robot` success |
| Projects | `DAEMON` | `WatcherStateLabel` | `WatcherDetailText` + self-tooltip | `Sync` muted |
| Registry | `PROJECTS` | `ProjectRegistry.TotalCount` | `ProjectRegistry.ProjectMixSummary` (tooltip) and `ProjectRegistry.FolderStatusSummary` (tooltip "Projects whose folder no longer exists", visible only when `ProjectRegistry.HasMissingFolders`, danger tone) | `FolderOpen` accent |
| Registry | `MANUAL` | `ProjectRegistry.ManualCount` | "Registered manually" | `FolderAdd` accent |
| Registry | `DISCOVERED` | `ProjectRegistry.DiscoveredCount` | "Managed by Project Discovery" | `Sync` muted |
| Agents | `AGENTS` | `AgentsState.InstalledAgentCount` | `AgentsState.AvailabilitySummary` | `Robot` success |
| Agents | `DEFAULT` | `AgentsState.DefaultAgentName` + tooltip on the value | `AgentsState.DefaultAgentDetail` | `FavoriteStar` accent |
| Agents | `PROBE` | `ProbePolicySummary` | `ProbePolicyDetail` | `Diagnostic` muted |

**Decisions** (from `design.md`): a `Control` with dependency properties, `SummaryCardTone` as a
dedicated tone vocabulary (not `SettingBadgeTone`, whose badges never use danger), two separate detail
slots (the muted `Detail` line and the danger-toned `Alert` line), and `WatcherStateTone` derived in the
view model rather than colours chosen by string triggers in XAML.

- [x] 3.1 Add `src/CLIHub/Controls/SummaryCard.cs` with `public enum SummaryCardTone { Neutral, Accent, Success, Danger }` and `public sealed class SummaryCard : Control`, `DefaultStyleKey = typeof(SummaryCard)`, dependency properties:
  `Icon` (string), `IconTone` (SummaryCardTone, default Neutral), `Label` (string), `Value` (string),
  `ValueTone` (SummaryCardTone, default Neutral), `ValueToolTip` (string?), `Detail` (string?),
  `DetailToolTip` (string?), `Alert` (string?), `AlertToolTip` (string?), `IsAlertVisible` (bool, default false).
  XML docs on every public member; the class docs state that `Neutral` means the muted/text tone and that `Alert` is always the danger tone.
- [x] 3.2 Add the implicit `<Style TargetType="{x:Type controls:SummaryCard}">` to `SettingsStyles.xaml` (after the `SettingsGroup` style). Structure: `Border` with `Padding="9"`, `PopoverBgBrush`, `FieldStrokeBrush` 1px border, `Sizing.CornerRadius.Chunky`; a two-column `Grid` (`*`, `Auto`); the text column is a `StackPanel` with the label (`Sizing.FontSize.Section`, SemiBold, `TextMutedBrush`), the value (`Sizing.FontSize.SummaryValue`, SemiBold, foreground from `ValueTone`), the optional `Detail` line (`HintText` base, `TextWrapping="Wrap"`, `ToolTip="{TemplateBinding DetailToolTip}"`, collapsed when empty) and the optional `Alert` line (`HintText` base, `TextWrapping="Wrap"`, `DangerBrush`, `ToolTip="{TemplateBinding AlertToolTip}"`, visible only while `IsAlertVisible` and `Alert` is non-empty); the icon column is a 24×24 `Border` (`ThumbBgBrush`, `ControlStrokeBrush`, 6px corner) with the glyph (`Sizing.FontFamily.Icon`, 11pt, foreground from `IconTone`). Tone mapping via `Trigger`s on the two tone properties: Neutral → `TextMutedBrush` for the icon and `TextPrimaryBrush` for the value, Accent → `AccentBrush`, Success → `SuccessBrush`, Danger → `DangerBrush`.
- [x] 3.3 Add `public SummaryCardTone WatcherStateTone` to `SettingsViewModel` next to `WatcherStateLabel` (`:1110`), assigned in the same state switch (`:1683`) — Watching → Success, Scanning → Accent, Error → Danger, otherwise Neutral — and raise it with the label in the property-change batch (`:1389`).
- [x] 3.4 Replace the nine `<Border Padding="9">` blocks inside the three summary `DataTemplate`s with `<controls:SummaryCard>` elements bound exactly as in the table above, keeping the three `DataTemplate` keys and their `SpacedPanel` wrappers intact. The DAEMON card binds `ValueTone="{Binding WatcherStateTone}"`; the Registry PROJECTS card binds `Alert="{Binding ProjectRegistry.FolderStatusSummary}"`, `AlertToolTip="Projects whose folder no longer exists"`, `IsAlertVisible="{Binding ProjectRegistry.HasMissingFolders}"`; the DEFAULT card binds `ValueToolTip="{Binding AgentsState.DefaultAgentName}"`; the DAEMON card binds `DetailToolTip="{Binding WatcherDetailText}"`. Two detail tooltips are missing from the table above but exist in the source and must be preserved: `AgentsState.AvailabilitySummary` carries "Installed agents and the number of supported plugins", and `ProbePolicyDetail` carries "Effective agent probe cache and timeout".
- [x] 3.5 Tests: unit-test the `WatcherStateTone` mapping for all four states plus the unknown fallback in the existing settings view-model tests; add a small `SummaryCardTests` that constructs the control and asserts the dependency-property defaults (`IconTone`/`ValueTone` Neutral, `IsAlertVisible` false) so the API cannot silently change.
- [x] 3.6 Gates: build 0/0, tests green, `openspec validate --all` green. Manual smoke: open Settings on the Projects, Projects (registry), and CLI Agents pages; all nine cards must match the previous rendering line for line (label case, value typography, icon glyph and tone, tooltips), the DAEMON card must colour the state for Watching/Scanning/Error, and the registry folder-health line must appear in the danger tone only when a folder is missing.

**Acceptance**: build/tests/validate green, the nine cards visually identical, state-driven tones and the conditional alert line verified by the user, `SettingsWindow.xaml` Window.Resources shrinks by roughly 400 lines.

---

## Stage 4: Settings Page Host

**Goal**: remove the ten per-page visibility `Style` blocks (`SettingsWindow.xaml:866-994`) and the three
header-template trigger chains (`:776-852`) without changing rendering or the eager page lifetime.

**Decisions** (from `design.md`): page metadata carries optional template keys; two small converters
replace the trigger chains and the duplicated per-page styles; pages stay instantiated in XAML, one
attribute each.

- [x] 4.1 Extend `SettingsPage` (`SettingsViewModel.cs:219-225`) with three optional properties — `HeaderTitleExtraKey`, `HeaderActionKey`, `SummaryRowKey` (all `string?`, default null) — and populate them in `AllPages`: `updates` → `UpdatesHeaderTitleExtra` + `UpdatesHeaderAction`; `projects` → `ProjectsHeaderAction` + `ProjectsSummaryCards`; `projects-registry` → `RegistrySummaryCards`; `agents` → `AgentsSummaryCards`. Everything else stays null. Built as three optional positional parameters after `Icon`, each with an XML `<param>` doc; `SettingsNavigationTests` asserts the full nine-page table.
- [x] 4.2 Add `src/CLIHub/Converters/ResourceKeyToTemplateConverter.cs` (resource key → `DataTemplate` through `Application.Current.TryFindResource`, `DependencyProperty.UnsetValue` for null/empty/missing) and `src/CLIHub/Converters/EmptyToCollapsedVisibilityConverter.cs` (null or empty string or unset → `Collapsed`, anything else → `Visible`). Register both in `App.xaml`. Unit-test each with a null, an empty, and a resolving/missing key. See 4.D1: the template converter resolves through the bound element, not the application, because the six templates are window-scoped.
- [x] 4.3 Add `src/CLIHub/Converters/PageKeyVisibilityConverter.cs`: compares the bound `SelectedPage.Key` with the `ConverterParameter` page key and returns `Visible`/`Collapsed`; null-safe in both directions. Unit-test match, mismatch, and null key.
- [x] 4.4 Replace the three header trigger chains with three `ContentControl`s whose `ContentTemplate` binds the corresponding `SettingsPage` key through `ResourceKeyToTemplate`, whose `Visibility` binds the same key through `EmptyToCollapsedVisibility`, and whose `Content="{Binding}"` stays as today. The summary row keeps its `Grid.Row`/`Margin`/`HorizontalContentAlignment` and the hairline below it. `ContentTemplate` is a `MultiBinding` of the key and the slot element itself (`RelativeSource Self`); `Visibility` stays a single binding.
- [x] 4.5 Replace each page's inline `<Style>` block with a single `Visibility` binding through `PageKeyVisibilityConverter` and `ConverterParameter="<page key>"`; keep one `views:SettingsXPage` element per page and the enclosing `Grid`. The placeholder page binds `Visibility` to a new `SettingsViewModel.IsPlaceholderPageVisible` computed property (`!SelectedPage.IsImplemented`), raised wherever `SelectedPage` changes — in the setter and in the `Load()` property-change batch next to `SelectedPage` (a reload resets the page to General without touching the setter; asserted by a test).
- [x] 4.6 Gates: build 0/0, tests 1045/1045 (388 + 52 + 605), `openspec validate --all` 30/30. Manual smoke: navigate to every page (general, hotkeys, terminal, agents, project discovery, projects, appearance, updates, telemetry, plus a placeholder page), confirm the correct content shows and nothing else is visible; confirm the Updates title chips and action row, the Project Discovery header actions, and the three summary rows appear only on their pages; confirm scrolling and the fixed header still behave.
- **4.D1 Deviation — header template lookup scope.** `Application.Current.TryFindResource` cannot see the six templates: they are declared in `SettingsWindow.xaml` `Window.Resources` (`:92`, `:111`, `:153`, `:183`, `:207`, `:233`), and `App.xaml:24-28` plus `XamlResourceReferenceTests` both document that window-scoped keys are invisible to the application-level set. `UpdatesHeaderAction` also depends on the window-scoped `StatusChip`/`StatusChipGlyph`/`StatusChipText`, so moving the templates to the application level would break their `StaticResource` references at load time. `ResourceKeyToTemplateConverter` is therefore an `IMultiValueConverter` (same shape as the existing `PathDisplayConverter`) taking the key and the slot element, and resolves through `FrameworkElement.TryFindResource` — element, window, application, system — with `Application.Current.TryFindResource` as the fallback when no element is supplied. The templates stay untouched in `Window.Resources`, the three slots keep one binding each for `Visibility`, and a runtime probe confirmed a `Window.Resources` template resolves through the real binding path.

**Acceptance**: build/tests/validate green, all pages navigate correctly with per-page header/summary slots, placeholder fallback works, user confirms.

---

## Stage 5: Sizing Token Sweep

**Goal**: tokenize only FontSize and CornerRadius values that repeat or encode a UI role; reorder
`Sizing.xaml` by category; rename the two look-based token names. Every numeric value stays identical.

- [x] 5.1 Measure before editing: re-measured 37 `FontSize` literals (8×1, 9×1, 10×3, 11×3, 12×6, 13×4, 14×8, 15×1, 16×9, 24×1), seven of them in the legacy `MainWindow.xaml`; `CornerRadius` literals are 16 (6×10, 9×2, 0×2, 7×1, 13×1), of which stage 3 leaves three 6px sites. The original 46 count predated stage 3; every editable 12pt site turned out to be an icon glyph.
- [x] 5.2 Decide per value, in `design.md` — see the measured classification table: 16 → `Sizing.FontSize.Glyph`, 14 → `Sizing.FontSize.GlyphSmall`, SummaryCard 6px → `Sizing.CornerRadius.IconBox`; 10/13/15/24/9/8, the four 12pt glyphs, and the 9/13/7/0 radii stay literals.
- [x] 5.3 Reorganized `Sizing.xaml` by category (font family, font size, spacing, corner radius, then four component groups, alphabetical inside each), folded the three "Additional …" sections in, and rewrote the header with the `Sizing.<Category>.<Property>` convention and the repeats-or-role token rule. 47 keys → 50.
- [x] 5.4 Renamed `Sizing.CornerRadius.Chunky` → `Sizing.CornerRadius.Card` (9 references) and `Sizing.Spacing` → `Sizing.Spacing.Standard` (2 references, plus `docs/architecture/ui.md`), then added the three new tokens and replaced their literal consumers (nine 16pt glyphs, seven 14pt glyphs, one 6px radius).
- [x] 5.5 Gates: build 0/0, tests green (including `XamlResourceReferenceTests` and `ThemePreviewResourceKeyTests`), `openspec validate --all` green. Measured 2026-10-22: build 0 warnings / 0 errors, tests 1045/1045 (388 + 52 + 605), `openspec validate --all` 30/30. Manual smoke: side-by-side comparison of every Settings page and the launch window before/after — no visible change in type sizes, corner radii, or spacing. Pending user confirmation.

- [x] 5.R1 Review addition: the four 12 pt literals are icon glyphs repeating in one role, so they use a new `Sizing.FontSize.GlyphTiny` (12) instead of the text `Body` token. Only the two literals inside the untouched legacy `MainWindow.xaml` remain at 12 pt.
**Acceptance**: build/tests/validate green, zero visual change confirmed by the user, `Sizing.xaml` organized by category with a documented naming rule.

---

## Stage 6: Guards and Documentation

**Goal**: add the three static guards the layer lacks, fix drifted comments, document the add-a-page
recipe and the theme data locations, and park the deferred work.

- [x] 6.1 Add `tests/CLIHub.Tests/Themes/ThemeColorLiteralTests.cs`: scan `src/CLIHub/**/*.xaml` for colour literals (`Color|Background|Foreground|Fill|Stroke="#RRGGBB(AA)"` and `<SolidColorBrush Color=`) and fail outside the allowlist — `Themes/*Theme.xaml` (palettes), `Themes/ThemePickerData.xaml` (theme-identity preview swatches) and `Views/MainWindow.xaml` (legacy window documented as untouched in `docs/backlog.md`). Report every hit as `file:line` with the literal.
- [x] 6.2 Add `tests/CLIHub.Tests/Themes/ApplicationResourceKeyTests.cs` with two facts: (a) no `StaticResource` reference resolves to a palette brush key from any `*Theme.xaml` (they must be `DynamicResource`, because the palette is merged at runtime); (b) no `x:Key` appears twice across the non-palette files of the application-level merge tree. Palettes redefine the same keys by design and are excluded from (b), but the walk must still visit them to prove it saw the whole tree.
- [x] 6.2b Add `tests/CLIHub.Tests/Views/SettingsPageMetadataTests.cs` closing the string-key blind spot introduced in stage 4: every non-null `HeaderTitleExtraKey`/`HeaderActionKey`/`SummaryRowKey` in `SettingsPage` metadata must name a `DataTemplate` declared in `SettingsWindow.xaml` resources; every `AllPages` key must appear exactly once as a `PageKeyVisibility` `ConverterParameter` in the page host and vice versa; no page key may be duplicated. Fail with the offending key and both sides of the mismatch, and assert the walk saw nine pages and at least three template keys so a broken regex cannot pass silently.
- [x] 6.3 Fix the drifted `LaunchWindowStyles.xaml` comment that still claims the dark theme applies to that window only; state the current truth (window-scoped styles, palette resolved from the application level).
- [x] 6.4 Update `docs/architecture/ui.md`: add an "Adding a Settings Page" recipe (page UserControl → `SettingsPage` metadata entry → optional header/summary template keys → the page host wiring → smoke) and a "Theme Data Locations" section (Core enum/tokens, `ThemeDescriptors`, palettes, `ThemePickerData`, the three theme tests).
- [x] 6.5 Append two entries to `docs/backlog.md` in the file's standard shape: settings-page registration is still spread over the metadata record and the XAML page host (remedy: a single registration point; trigger: a fifth page group or fifteenth page), and the remaining single-use FontSize/CornerRadius literals are intentionally untokenized (trigger: a second consumer appears).
- [x] 6.6 Gates: build 0/0, tests green (including the three new guard facts), `openspec validate --all` green, `graphify update .` succeeds.
- [x] 6.7 Manual smoke: no visual change anywhere; confirm the guards fail when deliberately violated (temporarily add a colour literal and a duplicate key, see the tests go red, revert).

**Acceptance**: build/tests/validate green, the three guards demonstrated by a deliberate violation and its revert, docs and backlog updated, user confirms.
