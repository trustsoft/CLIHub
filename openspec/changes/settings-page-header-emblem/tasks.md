# Tasks

Tasks in sections 1-5 are implementer tasks: they can be completed and verified without observing a
running desktop window. Section 6 is a manual smoke test the user runs; `git commit` and
`openspec archive` wait for the user's confirmation of that section, per `AGENTS.md`'s Change,
Commit, and Archive Policy. Section 7 is the final automated gate.

## 1. Sizing token

- [x] 1.1 Add `Sizing.FontSize.GlyphLarge` (20) to `src/CLIHub/Themes/Sizing.xaml`, in the Font sizes
      group next to the existing `Sizing.FontSize.Glyph*` entries, with a one-line comment describing
      its role ("Large decorative glyph, e.g. the Settings page-header emblem"), following the file's
      existing comment style for sibling tokens. Verify: `dotnet build CLIHub.sln` succeeds (XAML
      resource dictionaries are validated at build).

## 2. Page-header emblem markup

**Revised after manual smoke found a bug** (see `design.md` Decision 6): the first implementation
placed the emblem in a leading column of the header `Grid` itself, which centers it against that
row's height — and the header-action column can make that row taller than the title block (it does
on "Updates"), dropping the emblem below the title's optical center. The tasks below describe the
fixed structure: the header `Grid` keeps its original two columns, and the title block itself becomes
the container the emblem centers against.

- [x] 2.1 In `src/CLIHub/Views/SettingsWindow.xaml`, keep the page-header `Grid` (currently at
      `:405-479`) at its original two columns, `*` (title block) and `Auto` (header action) — do
      **not** add a third column to this `Grid`. If a prior session already added a third `Auto`
      column here, remove it and restore the original two-column `Grid.ColumnDefinitions`.
- [x] 2.2 Convert the title block from a bare `StackPanel` in column 0 to a `Grid` with
      `VerticalAlignment="Top"`, still in `Grid.Row="0" Grid.Column="0"` of the outer header `Grid`.
      Give this inner `Grid` two columns: `Auto` (the emblem) and `*` (the existing title+description
      `StackPanel`, unchanged in content, now nested one level deeper as the inner `Grid`'s column-1
      child).
- [x] 2.3 Restore the header-action `ContentControl`'s `Grid.Column` to `1` (it was `2` under the
      buggy structure) on the outer header `Grid`, keeping its existing `Margin="16,0,0,0"` and
      `VerticalAlignment="Stretch"`.
- [x] 2.4 Restore the summary-row `ContentControl` and the hairline `Rectangle`, both of which should
      span the outer header `Grid`'s two columns, to `Grid.ColumnSpan="2"` (they were `"3"` under the
      buggy structure).
- [x] 2.5 Add a `TextBlock` as the inner title-block `Grid`'s column-0 child (`Grid.Column="0"`,
      implicit `Grid.Row="0"`), bound to `{Binding SelectedPage.Icon}`, with:
      `FontFamily="{StaticResource Sizing.FontFamily.Icon}"`,
      `FontSize="{StaticResource Sizing.FontSize.GlyphLarge}"`,
      `Foreground="{DynamicResource TextMutedBrush}"`, `VerticalAlignment="Center"`, and
      `Margin="0,0,12,0"`. Centering against this inner `Grid` (not against the outer header `Grid`'s
      row) is what fixes the Updates bug — do not move the emblem back into the outer `Grid`. Do
      **not** add `AutomationProperties.AccessibilityView` — that attached property does not exist in
      WPF (`MC3072` at compile time; it is a WinUI/UWP API) — and do not add
      `AutomationProperties.Name=""` either, since an empty name is a no-op
      (`FrameworkElementAutomationPeer.GetNameCore()` falls back to the glyph's own text). Add a
      short XAML comment above the element stating both: (a) WPF has no per-element
      accessibility-view switch, so the emblem is exposed as a text element the same way the window's
      other icon glyphs already are, and (b) the emblem lives inside the title block's own `Grid`,
      not the outer header `Grid`, specifically so its height — and the emblem's centering — is never
      affected by the header-action column. Verify: the element compiles, `dotnet build CLIHub.sln`
      succeeds, and the title+description `StackPanel`'s existing content (the `WrapPanel` with the
      title and `HeaderTitleExtraKey` slot, and the description `TextBlock` with its empty-description
      `DataTrigger`) is otherwise unchanged by the nesting.

## 3. Tests

- [x] 3.1 Check whether `tests/CLIHub.Tests/Views/SettingsPageMetadataTests.cs` or any other existing
      test enumerates `SettingsWindow.xaml`'s header grid structure (column/row counts, slot
      bindings); if so, update it to expect the nested title-block `Grid` from task 2.2-2.5 (the
      outer header `Grid` keeps its original two columns; only the title block's internal structure
      changes), run `dotnet test CLIHub.sln`, and verify it passes. If no existing test covers header
      grid structure, no new test is required — this change adds no new bindable state (reuses
      `SelectedPage.Icon`) and no new ViewModel logic for `SettingsPageMetadataTests.cs`'s page/slot
      consistency checks to exercise.

## 4. Documentation

- [x] 4.1 Add a sentence to the "Settings Page Layout" section of `docs/architecture/ui.md`
      (`:50-61`) stating that the fixed header also shows the active page's icon as a plain glyph
      before the title, reusing `SettingsPage.Icon`, with no background or accent. Verify: the
      sentence reads consistently with the rest of the section's style and does not duplicate the
      "Adding a Settings Page" section's existing mention of `icon` in step 2.
- [x] 4.2 Update `ui/mockups/README.md`: change the status line's `settings-page-emblem.html` clause
      from "an open design comparison... no variant is chosen yet" to state it is delivered with
      variant B chosen, and update the `## \`settings-page-emblem.html\`` section's heading/text
      (currently "(MOCKUP, not implemented)" and the closing "Decisions this mockup does not make"
      paragraph) to reflect that variant B shipped and the title-shift question was resolved in favor
      of B's 32 DIP shift. Verify: the file still parses as valid Markdown and the "Related changes"
      section's convention (delivered/not-delivered annotations) is followed for consistency with
      how `popup-split.png`, `settings.png`, etc. are annotated there.
- [x] 4.3 Add a `### Changed` bullet under the `## Unreleased` heading in `CHANGELOG.md`
      (`:11-21`, already the current post-1.0.4 section — do not create a new heading), describing
      that the Settings page header now shows the active page's icon as a plain glyph before the
      title, ending the bullet with `(settings-page-header-emblem, settings-theme)`, matching the
      style of the existing `### Changed` and `### Fixed` bullets already under that heading. Do not
      move, reorder, or remove any existing bullet. Verify: `CHANGELOG.md`'s existing bullets under
      `## Unreleased` are unchanged except for the new line, and the new bullet reads consistently
      with its neighbors' tone and capability-reference style.
- [x] 4.4 Add an entry to `docs/backlog.md`'s "UI and theming" section (`:632` onward), following the
      file's existing entry format (title, `- Severity:`, `- Evidence:` with `file:line` citations,
      `- Proposed remedy:`, `- Trigger:`) used by neighboring entries. Title it around "Decorative
      icon glyphs are exposed to assistive technology across the Settings and launch windows."
      Severity: Low (no interactive control is affected; the glyphs are already non-focusable text).
      Evidence: cite the Settings page-header emblem (`SettingsWindow.xaml`, this change's added
      `TextBlock`), the sidebar glyph (`SettingsWindow.xaml:372-377`), and any other decorative icon
      `TextBlock`/glyph sites found while implementing this change, each with its `file:line`, noting
      that WPF exposes an undecorated `TextBlock` in the automation tree with its own text as the
      accessible name (verified via `FrameworkElementAutomationPeer.GetNameCore()` falling back to
      `GetPlainText()`, and `TextBlockAutomationPeer.IsControlElementCore()` returning `true`).
      Proposed remedy: a `CLIHub.Controls` `TextBlock` subclass (e.g. `DecorativeTextBlock`)
      overriding `OnCreateAutomationPeer()` to return `null`, applied across every decorative glyph
      site named in Evidence in one pass, not one glyph at a time. Trigger: the next change that
      touches Settings or launch-window chrome accessibility, or a user-reported screen-reader
      complaint about icon-glyph noise. Verify: the entry follows the same bullet structure as its
      neighbors (e.g. the "Dead-path residue after tier 2" entry at `:634-646`) and does not duplicate
      an existing open entry — if one already covers decorative glyphs, extend it instead of adding a
      second entry.

## 5. Automated gate

- [x] 5.1 Run `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, and `openspec validate --all`;
      confirm all three succeed with this change's files included.
- [x] 5.2 Run `graphify update .` to refresh the knowledge graph after the source and documentation
      edits.

## 6. Manual smoke (user)

The implementer cannot observe a running desktop window or inspect the live UI Automation tree; the
user performs this section after section 5 passes. `git commit` and `openspec archive` wait for the
user's confirmation here before the orchestrator asks for commit/archive approval.

- [x] 6.1 Run `dotnet run --project src/CLIHub/CLIHub.csproj`, open Settings, and visually compare
      every page's header against `ui/mockups/settings-page-emblem.html` (`data-variant="b"`): the
      emblem glyph matches the sidebar icon for that page, sits 12 DIP before the title, is muted
      (no accent), and has no tile or border.
- [x] 6.2 On the "Updates" page, confirm its title chip (`UpdatesHeaderTitleExtra`) still wraps with
      the title exactly as before this change, and the emblem does not move or wrap when the chip is
      present.
- [x] 6.2a Regression check for the bug found in the previous smoke round: open the "Updates" page
      (its header action, the status chip + "Check for updates" button + "View release notes" link,
      is taller than the title block) and compare the emblem's vertical position against its title
      to the same comparison on a page with a short or no header action, such as "General & Startup"
      or "Project Scanning." Confirm the emblem sits at the same height relative to the title on both
      pages — it must not sit lower on Updates than it does on the other page.
- [x] 6.3 Resize the Settings window down to its `MinWidth` (640) and check every page's title and
      description. Confirm no title is clipped or overlapped in a way that looks broken (a title
      already too wide for its column may overflow — this is pre-existing behavior the emblem's
      column narrows but does not introduce). Confirm any description that wraps onto a second line
      (for example on the "Project Scanning" or "Projects" page) reads correctly and that the header
      grows to fit it without clipping, and that the emblem stays centered against the whole
      title-and-description block when that happens.
- [x] 6.4 Confirm the emblem is not an interactive element: Tab through the page header's controls
      and confirm the emblem is never a focus stop (focus moves from the sidebar directly to the
      next real control, skipping the emblem), and confirm hovering the emblem shows no tooltip and
      clicking it has no effect. Do not expect the emblem to be absent from the accessibility tree —
      WPF exposes it as an ordinary text element, like the window's other icon glyphs, and that is
      the accepted, documented behavior for this change (`design.md` Decision 5).
- [x] 6.5 Report the smoke result (pass, or what failed) back to the orchestrator before requesting
      archive/commit approval.

## Workflow follow-up

- Report the automated gate results from section 5, then wait for the user's section 6 smoke
  confirmation before asking whether to archive this change and commit, per `AGENTS.md`'s Change,
  Commit, and Archive Policy.
