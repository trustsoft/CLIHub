# Design

## Context

Verified current state (re-checked in this session):

- `src/CLIHub/Views/SettingsWindow.xaml:91-108` — `DataTemplate x:Key="UpdatesHeaderTitleExtra"`: a horizontal `StackPanel` (`VerticalAlignment="Center"`) holding two `Border Style="{StaticResource HeaderChip}"` chips. First chip: "Version: v{0}" (`:94-99`). Second chip: "Channel: Stable" (`:101-106`), `Margin="8,0,0,0"`. The template is rendered by the title `WrapPanel` (`:443-457`) next to the title text, inside the title block's column-1 `StackPanel` (`:442-469`) that is itself nested in the title block's `Grid` (`:421-470`), per `2026-11-02-settings-page-header-emblem`'s restructuring.
- `src/CLIHub/Views/SettingsWindow.xaml:110-150` — `DataTemplate x:Key="UpdatesHeaderAction"`: a `Grid` with `Grid.RowDefinitions` (`:113-116`) defining two rows, both `Height="Auto"` for row 0 and `Height="*"` for row 1. Row 0 (`:118-139`): a right-aligned horizontal `StackPanel` holding the status `Border Style="{StaticResource StatusChip}"` (`:121-128`) and the "Check for updates" accent `Button` (`:129-138`, `Padding="9,7"`, `Margin="8,0,0,0"`). Row 1 (`:141-148`): the `Button Style="{StaticResource LinkButton}"`, `Grid.Row="1"`, `HorizontalAlignment="Right"`, `VerticalAlignment="Bottom"`, `Margin="0,4,0,0"`, `Content="View release notes  →"`, `ToolTip="Open What's New"`, `Command="{Binding ShowReleaseNotesCommand}"`.
- `src/CLIHub/Themes/SettingsControls.xaml:471-479` — `LinkButton` style: `FontSize` = `Sizing.FontSize.Body`, `Foreground` = `AccentBrush`, `Padding="0"`, `HorizontalContentAlignment="Right"`. It is an application-level keyed style (merged via `App.xaml:30`, `SettingsStyles.xaml` which merges `SettingsControls.xaml`), so it is reachable from the window-scoped `UpdatesHeaderTitleExtra` template without redeclaration.
- `openspec/specs/preferences-ui/spec.md:193` — "Updates page options" requirement text: "...and a 'View release notes' action aligned to the bottom-right of the header."
- `openspec/specs/preferences-ui/spec.md:199` — "Options offered" scenario THEN step: "AND the bottom-right of the header shows 'View release notes'."
- `openspec/specs/settings-theme/spec.md:326-331` — "Title chip still wraps with the title" scenario: "WHEN the 'Updates' page is active, which shows an extra title chip beside its title / THEN the chip wraps together with the title text as it does without the emblem / AND the emblem never moves to a second line with the title or the chip." The scenario's name is fixed by OpenSpec; its WHEN clause says "an extra title chip" (singular) but after this change there are two chips and one link, so the WHEN and THEN clauses must be updated to reflect that all three wrap together, while the emblem never wraps.
- Context from `2026-11-02-settings-page-header-emblem`: the title block (`:421-470`) is a nested `Grid` with `VerticalAlignment="Top"`, two columns (`Auto` for the emblem, `*` for the title+description `StackPanel`). The emblem (`:434-440`) is column 0 with `VerticalAlignment="Center"` and `Margin="0,0,12,0"`, so it centers against the `StackPanel`'s measured height, isolated from the header-action column's height (Decision 6 of that change). The title+description `StackPanel` (`:442-469`) is column 1, holding the `WrapPanel` (title `TextBlock` + `HeaderTitleExtraKey` slot, `:443-457`) and the description `TextBlock` (`:458-468`).
- The title `WrapPanel` (`:443-457`) wraps its children (title `TextBlock` and `HeaderTitleExtraKey` slot) as a unit when the column is too narrow; the `ContentControl` slot's `Margin="10,0,0,0"` (`:445`) is a literal value (not `Sizing.Spacing.Standard`), so the approved gap between title text and the chip template is 10 DIP, not 8. The link will sit inside the `UpdatesHeaderTitleExtra` template alongside the chips, not directly in the `WrapPanel`.
- `Sizing.Spacing.Standard` = 8 (from `src/CLIHub/Themes/Sizing.xaml:41`), already used by the second chip's `Margin="8,0,0,0"` to establish the chip-to-chip gap.
- `docs/images/settings-agent-detection.png`, `settings-agents.png`, `settings-appearance.png`, `settings-discovery.png` exist; no `settings-updates.png` or similar. Verified: no screenshot refresh is expected.
- `CHANGELOG.md:11-23` — "## Unreleased" section with `### Changed` and `### Fixed` groups; `### Changed` holds three bullets for already-delivered changes, each ending with a parenthetical change ID and capability name. The last bullet (`:17`) is `settings-page-header-emblem`, so the new bullet is appended after it.

## Goals / Non-Goals

**Goals:**
- Move the "View release notes →" link from the action column's second row into the title row's chip template, immediately after the Channel chip, with consistent horizontal rhythm (8 or 10 DIP left margin, decided below).
- Keep the link's `Command`, `Content`, `ToolTip`, and `LinkButton` style unchanged — only its container and margins change.
- Set `VerticalAlignment="Center"` on the link to match the chips.
- Collapse `UpdatesHeaderAction` to a single row by removing the `Grid` and its row definitions, since the template now holds only the status/button `StackPanel`.
- Update the `preferences-ui` requirement and scenario to reflect the new placement, and extend the `settings-theme` scenario's wording to cover two chips and the link wrapping together.
- Accept that the link wraps with the chips at narrow widths as a consequence of moving it into the title row (the title-extra slot already wraps as a unit today).

**Non-Goals:**
- No change to the link's text, tooltip, command, or the `LinkButton` style's visual properties.
- No change to the chip layout, the status chip, or the "Check for updates" button.
- No new token for the link's left margin if the existing `Sizing.Spacing.Standard` (8) is chosen.
- No rework of the header `Grid`'s two-column structure or the title block's nested `Grid` established by `2026-11-02-settings-page-header-emblem`.

## Decisions

### 1. Link placement: third child in `UpdatesHeaderTitleExtra`, after the Channel chip

The link becomes the third child in the `UpdatesHeaderTitleExtra` `StackPanel`, after the Channel chip's closing `</Border>` (currently `:106`). This keeps it in the same horizontal flow as the chips, right-aligned as a group by the `StackPanel`'s default stretch behavior (no explicit `HorizontalAlignment` on the `StackPanel` itself, so WPF defaults apply: children flow left-to-right, and the `StackPanel` measures as wide as its parent allows, but each child's measured width is its own).

**Alternative rejected:** placing the link as a fourth child in the title `WrapPanel` itself, after the `ContentControl` slot. Rejected because the link is conceptually part of the Updates metadata alongside the chips, not a generic title decoration for every page that happens to have a `HeaderTitleExtraKey`. The link is Updates-specific, so it belongs *inside* the Updates template, not beside it at a higher scope.

### 2. Left margin on the link: 8 DIP to match the chip-to-chip gap

The second chip already has `Margin="8,0,0,0"` to space it from the first chip (`:101`). The link's left margin will also be `8,0,0,0` to maintain the same rhythm: chip—8—chip—8—link. This is consistent with the established chip-to-chip gap and avoids introducing a new spacing value.

**Alternative rejected:** 10 DIP to match the title-to-chip gap (`ContentControl Margin="10,0,0,0"`, `:445`). Rejected because the 10 DIP gap separates the title *text* from the chips (a different typographic boundary: paragraph text → structured data), while the 8 DIP gap separates two chips (structured data → structured data). The link is closer in role to the chips (metadata/navigation) than to the title text (page name), so the 8 DIP chip-to-chip rhythm is the better match. This also avoids a visual stutter (8, 10, 8) for no functional reason.

**Alternative rejected:** a new token like `Sizing.Spacing.HeaderMetadata`. Rejected because the gap is already expressed by an existing token (`Sizing.Spacing.Standard` = 8), and adding a new token for a value that is identical to an existing token violates the "one token, one value" rule (`Sizing.xaml:14-15`). The gap does not name a distinct UI role that differs from "spacing between adjacent controls in simple one-dimensional groups" — it is exactly that role, applied to a horizontal group of chips and a link.

### 3. Vertical alignment: `VerticalAlignment="Center"` to match the chips

The `UpdatesHeaderTitleExtra` `StackPanel` has `VerticalAlignment="Center"` (`:93`), which centers the entire template against the title `WrapPanel`'s measured height. The link gets `VerticalAlignment="Center"` explicitly set on itself to match the chips' implicit centering within the horizontal `StackPanel` (WPF's default for children of a horizontal `StackPanel` is `Stretch`, but the chips' content drives their height, and they appear centered because their `Border` has no explicit alignment either). Setting `VerticalAlignment="Center"` on the link makes the centering explicit and keeps it aligned with the chips' text baseline.

**Alternative rejected:** leaving the link's vertical alignment unset and relying on the `StackPanel`'s default `Stretch`. Rejected because `LinkButton`'s template (`:480-495`) has a `Border` with a `ContentPresenter` that uses `VerticalAlignment="Center"` (`:485`), so the link's text would center inside its own bounds regardless. An explicit `VerticalAlignment="Center"` on the `Button` itself is clearer and matches the pattern of explicitly setting alignment when mixing controls of different types in one container.

### 4. Collapse `UpdatesHeaderAction` to a single element, no `Grid`

With the link moved out, `UpdatesHeaderAction` holds only one element: the status/button `StackPanel` (`:118-139`). The `Grid` and its two `RowDefinitions` (`:112-116`) are no longer needed. The template becomes a bare `StackPanel` (the existing row 0's content) with no `Grid.Row="0"` attribute.

**Rationale:** the `Grid` existed only to stack two rows (status/button above, link below). With the link gone, a one-element template does not need a `Grid` or row definitions — the single `StackPanel` can be the template's root element directly. This is simpler and eliminates the now-unused `Height="*"` row 1 that the link's `VerticalAlignment="Bottom"` relied on.

**Alternative rejected:** keeping the `Grid` with one row for future extensibility. Rejected because speculative structure with no current consumer violates the project's convention ("use the existing patterns before adding abstractions," `AGENTS.md:12`). If a future change needs to add another element to this header action, that change can restructure the template at that time; today's task is to move the link, not to design a layout for hypothetical future content.

### 5. Wrapping behavior: accept that the link wraps with the chips

The title `WrapPanel` (`:443-457`) wraps its children when the column is too narrow. The `ContentControl` slot (`:445-456`) is one child; the title `TextBlock` is another. When the `WrapPanel` wraps, the slot (which contains `UpdatesHeaderTitleExtra`) wraps as a unit onto a second line below the title text. After this change, that slot holds two chips and the link, so all three wrap together — the link is not independently wrappable from the chips because it sits inside the same `StackPanel` rendered by the slot.

At the window's `MinWidth` (640 DIP), the content area available to the title block (excluding the 24 DIP left/right margins, `:403`, and the emblem's 32 DIP column) is ~584 DIP minus the header-action column's width. The title text "Updates" plus the two chips plus the link is unlikely to exceed that width at common font scaling, so wrapping is rare in practice. If it does wrap, the entire chip+link group moves to a second line, which is the existing, already-specified behavior of the `HeaderTitleExtraKey` slot — this change does not introduce new wrapping logic, only adds a third element to the group that already wraps as a unit.

**Decision:** Accept this consequence as part of moving the link to the title row. The alternative (making the link independently wrappable from the chips) would require restructuring the `WrapPanel` to hold the chips and link as separate direct children, which contradicts Decision 1's rationale (the link belongs *inside* the Updates template, not beside it). Wrapping the chips and link together preserves the visual grouping of the Updates metadata and matches how the slot already behaves today when the chips alone are too wide.

### 6. Spec changes: `preferences-ui` requirement and scenario, `settings-theme` scenario

**`preferences-ui` changes:**
- Requirement text (`:193`): replace "aligned to the bottom-right of the header" with "on the title row after the channel chip" to state the new placement without repeating the full requirement.
- "Options offered" scenario THEN step (`:199`): replace "the bottom-right of the header shows 'View release notes'" with "the title row shows 'View release notes' after the channel chip" to match the new position.

**`settings-theme` changes:**
- "Title chip still wraps with the title" scenario (`:326-331`): 
  - WHEN clause: replace "which shows an extra title chip beside its title" with "which shows two metadata chips and a 'View release notes' link beside its title" to cover all three elements.
  - THEN clause: replace "the chip wraps together with the title text" with "the chips and link wrap together with the title text" to state that all three wrap as a unit.
  - AND clause: replace "the emblem never moves to a second line with the title or the chip" with "the emblem never moves to a second line with the title, chips, or link" to cover the new element.

The scenario's name ("Title chip still wraps with the title") is **not** changed, because OpenSpec MODIFIED scenarios must preserve the original scenario name even when the content evolves. The name is slightly misleading after this change (it says "chip" singular, but the scenario now covers two chips and a link), but that is the specified trade-off to keep scenario identity stable across revisions.

### 7. No height impact analysis required: the header row is `Auto`, and the emblem is isolated

The header row is `RowDefinition Height="Auto"` (`SettingsWindow.xaml:407`), so its height is the maximum of its children's heights. Before this change, the Updates page's header action (two rows: status/button + link) was often taller than the title block (title + description), so the header action drove the row's height. After this change, the header action is shorter (one row: status/button only), so the title block (unchanged in height) becomes the tallest element and drives the row's height instead. The header row may be slightly shorter on the Updates page at some window widths, depending on whether the description wraps.

The emblem (`src/CLIHub/Views/SettingsWindow.xaml:434-440`) centers against the title block's inner `Grid` (`:421-470`), which has `VerticalAlignment="Top"` and is isolated from the header-action column (Decision 6 of `2026-11-02-settings-page-header-emblem`). The header-action column's height does not affect the emblem's vertical position, so shortening the header action has no impact on the emblem's centering. This was already verified by that change's manual smoke check, which confirmed the emblem's position is stable across pages with different header-action heights.

**No new open question or risk:** the layout is already designed to handle variable header-action heights without affecting the title block or emblem, so this change's reduction of the Updates action height is absorbed by the existing `Auto` row definition without requiring new mitigation.

### 8. Action column vertical alignment: center the `StackPanel` to match the other header action

After the link moves out, `UpdatesHeaderAction` becomes a single `StackPanel` (status chip + "Check for updates" button, roughly 32 DIP tall) instead of a two-row `Grid`. The header-action `ContentControl` (`:472-484`) has `VerticalAlignment="Stretch"`, so without an explicit alignment on the `StackPanel` itself, WPF's default stretch would make it fill the row's height vertically, leaving uneven gaps above and below the controls.

`ProjectsHeaderAction` (`:153-180`) is the only other header action in the window, and its root `SpacedPanel` sets `VerticalAlignment="Center"` (`:154`). This is the established precedent for header actions that are shorter than the title block: center them within the row rather than stretching them to fill it.

**Decision:** Set `VerticalAlignment="Center"` on the `UpdatesHeaderAction` `StackPanel` to match `ProjectsHeaderAction`'s pattern. This centers the status chip and button within the header row, visually balancing them against the title block rather than pinning them to the top of the row.

**Alternative rejected:** leaving the `StackPanel`'s vertical alignment unset (default `Stretch`). Rejected because stretching a 32 DIP horizontal group to fill a ~40 DIP row adds uneven internal padding that the controls themselves do not need or specify, and contradicts the precedent set by `ProjectsHeaderAction`. Centering keeps the action controls grouped compactly and aligns with how the other header action is already rendered.

## Risks / Trade-offs

- **[Trade-off]** The link wraps with the chips at narrow widths, rather than staying pinned to the header's right edge as it does today. → **Accepted as part of the goal.** The title-extra slot already wraps as a unit (the chips today), and this change preserves that behavior by adding the link to the same group. Independent wrapping would require a different structure that contradicts the goal of grouping the link with the metadata chips.
- **[Trade-off]** The Updates header row may be slightly shorter after this change, since the header action (now one row) no longer drives the row's height. → **No mitigation.** The row is `Height="Auto"` and already designed to fit variable content; the title block (unchanged) becomes the driver, and the emblem's centering is unaffected (Decision 7). This is a predictable, benign consequence of the layout working as specified.
- **[Risk]** At extreme narrow widths (640 DIP `MinWidth`, high DPI scaling, or long localized strings if localization is added in the future), the two chips plus the link might wrap onto a second line below the title. → **Accepted.** This is the existing wrapping behavior of the `HeaderTitleExtraKey` slot operating with more content; no new wrapping logic is introduced. A scenario documents this behavior ("Title chip still wraps with the title"), so it is specified, not a failure mode.

## Migration Plan

Not applicable — this is a visual-only WPF change with no data, schema, or configuration migration. Implementation order (detailed in `tasks.md`): move the link `Button` into `UpdatesHeaderTitleExtra` as the third child with `Margin="8,0,0,0"` and `VerticalAlignment="Center"`, remove the link from `UpdatesHeaderAction` and collapse that template to a bare `StackPanel`, update the `preferences-ui` and `settings-theme` specs, update `CHANGELOG.md`, run the automated gate (build + test + `openspec validate --all`), then hand off to the user for manual smoke (visual comparison at normal and narrow widths, wrapping check, tooltip/command check, Updates vs. another page comparison).

## Open Questions

None. Every decision flagged in the brief (margin value, spec wording, wrapping consequence) is resolved above with rationale.
