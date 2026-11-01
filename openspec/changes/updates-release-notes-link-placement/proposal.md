# Proposal

## Why

The Settings "Updates" page header (`src/CLIHub/Views/SettingsWindow.xaml:111-150`) currently renders a two-row action cluster: row 0 holds the status chip and "Check for updates" button, right-aligned; row 1 holds the "View release notes →" link, also right-aligned, pinned to the bottom of the action column with `VerticalAlignment="Bottom"` and `Margin="0,4,0,0"`. This layout places the link below the action button, visually separated from the title row's metadata (the Version and Channel chips that sit in `UpdatesHeaderTitleExtra`, `:92-108`).

The user wants the "View release notes" link moved onto the title line, right after the "Channel: Stable" chip, so the header's visual structure reads as one line of metadata (title, version chip, channel chip, release-notes link) and one line of actions (status chip + check button). This groups the link with the version information it refers to, rather than parking it in the action column with the update check.

## What Changes

- Move the "View release notes →" `Button Style="{StaticResource LinkButton}"` from `UpdatesHeaderAction`'s second row (currently `Grid.Row="1"`, `:141-148`) into `UpdatesHeaderTitleExtra` as the third child after the Channel chip (`:92-108`).
- The link keeps its `Command="{Binding ShowReleaseNotesCommand}"`, `Content="View release notes  →"`, `ToolTip="Open What's New"`, and `LinkButton` style — this is a placement change, not a behavior change.
- Add a left margin (8 or 10 DIP, decided in `design.md`) on the link so it reads consistently with the 8 DIP gap between the existing two chips.
- Set `VerticalAlignment="Center"` on the link to match the chips' alignment within the horizontal `StackPanel`.
- Collapse `UpdatesHeaderAction` to a single row: remove `Grid.RowDefinitions` and the `Grid.Row="0"` attribute from the status/button `StackPanel`, since it now occupies the entire `DataTemplate` with no second row beneath it.
- Remove `VerticalAlignment="Bottom"` and `Margin="0,4,0,0"` from the link (those existed only to place it at the bottom of the action column; it no longer lives there).

## Capabilities

### Modified Capabilities

- `preferences-ui`: the "Updates page options" requirement (`:193`) states the link is "aligned to the bottom-right of the header," and the "Options offered" scenario's THEN step (`:199`) says "the bottom-right of the header shows 'View release notes'." Both must change to reflect the new title-row placement.
- `settings-theme`: the "Title chip still wraps with the title" scenario (`:326-331`) describes the Updates title extras as "an extra title chip beside its title"; after this change the title row holds two chips *and* the link, so the scenario's wording must cover all three.

## Impact

- `src/CLIHub/Views/SettingsWindow.xaml:92-108` — `UpdatesHeaderTitleExtra`: gains a third child (the link `Button`) after the Channel chip, with left margin and `VerticalAlignment="Center"`.
- `src/CLIHub/Views/SettingsWindow.xaml:111-150` — `UpdatesHeaderAction`: loses its second row definition; the status/button `StackPanel` loses its `Grid.Row="0"` attribute and occupies the whole template.
- The link now wraps with the title and chips inside the title `WrapPanel`'s slot, rather than being pinned to the header's right edge. At narrow widths the entire title-extra slot wraps as a unit (today's behavior, unchanged), so the link wraps with the chips if the window is narrow enough. The decision to accept this consequence is recorded in `design.md`.
- The Updates page header-action column becomes shorter (one row instead of two), so it no longer drives the header row's height on that page. The title block's height (title + description) is now the tallest element in the header row, so the Updates header may be slightly shorter than it is today, depending on the description's wrapped height. The emblem (`src/CLIHub/Views/SettingsWindow.xaml:434-440`, added by `2026-11-02-settings-page-header-emblem`) keeps its vertical centering against the title+description block unaffected, since Decision 6 of that change already nested the title block in its own top-aligned `Grid` to isolate it from the header-action column's height.
- No change to ViewModels, commands, or the release-notes surface itself — the command binding and tooltip stay as-is.
- `CHANGELOG.md` — gains a `### Changed` bullet under `## Unreleased` stating the link's new placement, following the repository's per-change convention.

## Non-Goals

- No change to the link's text, command, tooltip, or the `LinkButton` style.
- No change to the Version and Channel chips' layout or spacing within the `StackPanel`.
- No change to the status chip, "Check for updates" button, or their row.
- No change to how the `WrapPanel` wraps the title-extra slot at narrow widths — the slot already wraps as a unit today, and this change preserves that behavior.
- No rework of the header `Grid`'s column structure or the title block's nested `Grid` (both established by `2026-11-02-settings-page-header-emblem`).
- No screenshot refresh — release preparation owns that (`docs/releasing.md`, step 0), not this change. Verified: `docs/images/` contains no Updates page screenshot.

## Dependencies

- None on in-flight OpenSpec changes. Depends only on the already-delivered `preferences-ui` Updates page and the page-header layout (emblem, title block, header action) documented in `settings-theme` and delivered by `2026-11-02-settings-page-header-emblem`.
