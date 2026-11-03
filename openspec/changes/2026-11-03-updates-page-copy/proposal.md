# Proposal: Updates Page Copy Revision

## Why

The Settings → Updates page copy includes verbose section headers and hints that can be made more concise while retaining clarity. The user reviewed a mockup (`mockup-settings-updates.html`) that removes the redundant section header and shortens the hints, and approved "variant 1" copy for implementation.

This change applies only approved copy edits. No behavior, layout beyond vertical rhythm, style tokens, control ordering, icons, or settings contract changes are included.

## What Changes

### Page Description (`src/CLIHub/ViewModels/Settings/SettingsNavigation.cs`)

1. Updates page description (line 105):
   - before: `Check for new CLIHub builds, configure update preferences, and view release notes.`
   - after: `Update checks, downloads, and notifications.`

### Release Notes Link (`src/CLIHub/Views/SettingsWindow.xaml`)

2. Release-notes link content (line 110):
   - before: `Content="View release notes  →"` (two spaces before the arrow)
   - after: `Content="Release notes →"`
   - Keep `ToolTip="Open What's New"` unchanged.

### XAML (`src/CLIHub/Views/SettingsUpdatesPage.xaml`)

3. Remove the entire `<controls:SectionHeader Text="UPDATE PREFERENCES"/>` element (line 10).
4. Add `Margin="0,18,0,8"` to the `<controls:SettingsGroup>` opening tag (line 11) to start 18 px below the page divider and leave 8 px below the group, preserving the vertical rhythm.
5. Card 1 hint (line 13): `Look for a new version when CLIHub starts.` → `Runs once, in the background.`
6. Card 2 hint (line 21): `Download an available update in the background.` → `Downloads in the background.`
7. Card 3 hint (line 29): `Show a notification when a stable update is available.` → `Uses Windows notifications.`
8. Card 4 title (line 38): `Notify about pre-release versions` → `Notify about pre-releases`
9. Card 4 hint (line 39): `Not available yet: CLIHub publishes stable releases only.` → `Not available yet. Stable releases only.`

### Explicit Non-Changes

- Card 1–3 titles (`Check for updates on startup`, `Automatically download updates`, `Notify about stable releases`), icons, toggle bindings, comments, ordering, opacity, and the disabled state are unchanged.
- `SettingsViewModel` status-card strings (`UpdateCard` Detail/Title/Badge), badges, header chips, button tooltips, and automation names are unchanged.
- No reformatting of unrelated XAML or re-indenting attributes.

## Impact

- User-facing copy is more concise.
- Vertical rhythm is preserved after removing the section header by setting the first group's top margin to 18 px and bottom margin to 8 px, mirroring the archived `2026-11-03-logs-page-copy` vertical rhythm pattern.
- No behavior contracts change; this is a UI-label and layout alignment.

### Spec Delta

A completeness sweep of `openspec/specs/` for `View release notes`, `UPDATE PREFERENCES`, and the UI label `pre-release versions` found exactly three requirements that quote the old labels:

**`preferences-ui` capability — two MODIFIED requirements:**
- **"Updates page options"** (line 192-229 of `openspec/specs/preferences-ui/spec.md`) quotes `"View release notes"`, the section header `"UPDATE PREFERENCES"`, and the control label `"Notify about pre-release versions"`. The requirement text and scenarios `Options offered` and `Defaults` are updated to reflect the new copy: `"Release notes"`, no section header, and `"Notify about pre-releases"`.
- **"Release notes from Settings"** (line 651-659 of `openspec/specs/preferences-ui/spec.md`) quotes `"View release notes"` in the `Open release notes` scenario. The scenario is updated to `"Release notes"`.

**`settings-theme` capability — one MODIFIED requirement:**
- **"Persistent page header"** (line 241-338 of `openspec/specs/settings-theme/spec.md`) quotes `"View release notes"` in the `Title chip still wraps with the title` scenario (line 328). The scenario is updated to `"Release notes"`.

The pre-existing "from the status card" wording in "Release notes from Settings" is not changed by this change; that discrepancy is parked separately.

## Dependencies

None. This change is independent and has no blocking or dependent changes.
