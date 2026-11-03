# Design: Updates Page Copy Revision

## Verified Current State

All line numbers and strings verified at HEAD `fc62e92`.

### `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs`

- **Line 105:** `"Check for new CLIHub builds, configure update preferences, and view release notes."`

### `src/CLIHub/Views/SettingsWindow.xaml`

- **Line 110:** `Content="View release notes  →"` (two spaces before the arrow)
- **Line 112:** `ToolTip="Open What's New"` (unchanged)

### `src/CLIHub/Views/SettingsUpdatesPage.xaml`

- **Line 10:** `<controls:SectionHeader Text="UPDATE PREFERENCES"/>`
- **Line 11:** `<controls:SettingsGroup>` (no Margin attribute)
- **Line 13:** `Hint="Look for a new version when CLIHub starts."`
- **Line 21:** `Hint="Download an available update in the background."`
- **Line 29:** `Hint="Show a notification when a stable update is available."`
- **Line 38:** `Title="Notify about pre-release versions"`
- **Line 39:** `Hint="Not available yet: CLIHub publishes stable releases only."`

### Test Coverage

No tests in `tests/CLIHub.Tests/`, `tests/CLIHub.Core.Tests/`, or `tests/CLIHub.Core.Windows.Tests/` assert any of the changed strings. The update workflow logic is tested, but the exact UI copy text is not.

## Decisions

### Remove Section Header and Set Explicit Margins

The `<controls:SectionHeader Text="UPDATE PREFERENCES"/>` is removed entirely. The `SectionHeader` style in `src/CLIHub/Themes/SettingsStyles.xaml` (line 156) sets `Margin="0,18,0,8"`, which gives the 18 px top gap and 8 px bottom gap. Removing the header removes that spacing.

To preserve the page's vertical rhythm, the `SettingsGroup` (which has no Margin setter in its implicit style) gets an explicit local `Margin="0,18,0,8"` on the page, so the first settings group starts 18 px below the page divider and leaves 8 px below the group.

This is a local margin set on the page's `SettingsGroup` instance, not a new style token or shared style change. This mirrors the archived `2026-11-03-logs-page-copy` vertical rhythm pattern.

**Rationale:** The section header is redundant: the page title is "Updates" and the settings clearly relate to update preferences. The header adds visual weight without communicating information beyond what the settings themselves convey. The explicit margin preserves the approved vertical rhythm.

### Shorten Page Description

The page description changes from `Check for new CLIHub builds, configure update preferences, and view release notes.` to `Update checks, downloads, and notifications.`

**Rationale:** The new description is concise and covers the same functional areas (checking, downloading, notifying) without the verbose phrasing. It matches the tone of other archived copy changes.

### Shorten Release Notes Link

The release notes link content changes from `View release notes  →` (two spaces before the arrow) to `Release notes →`.

**Rationale:** The verb "View" is redundant for a link; the link itself implies the action. One space before the arrow is consistent with other links in the application. The tooltip remains `Open What's New`.

### Shorten Hints

The four hints in the settings group are shortened:

- `Look for a new version when CLIHub starts.` → `Runs once, in the background.`
- `Download an available update in the background.` → `Downloads in the background.`
- `Show a notification when a stable update is available.` → `Uses Windows notifications.`
- `Not available yet: CLIHub publishes stable releases only.` → `Not available yet. Stable releases only.`

**Rationale:** The shorter copy removes redundant words and focuses on the essential information. The new hints describe the mechanism or constraint rather than repeating the card title.

### Verify Design Context Claims

The brief claims:

1. `Runs once, in the background.` describes the startup check: `OptionalStartupCoordinator.RunOptionalStartup` launches one tracked background check per launch.
   - **Verified:** `src/CLIHub/Startup/OptionalStartupCoordinator.cs` line 36-54 shows `RunOptionalStartup` calls `RunStartupUpdateCheckAsync` once per launch, wrapped in `operationLifetime.RunAsync` for background execution.

2. `Uses Windows notifications.` describes the delivery channel: `UpdateStartupCoordinator` calls `notifyUpdateAvailable`, surfaced through `TrayIconController.ShowNotification` (taskbar notification).
   - **Verified:** `src/CLIHub/Startup/UpdateStartupCoordinator.cs` line 60 invokes the `notifyUpdateAvailable` callback. This callback is bound to `startupUi.NotifyUpdateAvailable` (`src/CLIHub/Startup/OptionalStartupCoordinator.cs` line 65), which delegates to `tray.NotifyUpdateAvailable` (`src/CLIHub/Startup/WpfApplicationStartupUi.cs` line 40), which calls `taskbarIcon.ShowNotification` (`src/CLIHub/Tray/TrayIconController.cs` line 83).

Both claims are accurate.

### Shorten Pre-Release Card Title

The pre-release card title changes from `Notify about pre-release versions` to `Notify about pre-releases`.

**Rationale:** "Pre-releases" is unambiguous and shorter. The hint provides the context that the option is not yet available.

## Alternatives Rejected

### Keep the Section Header

Rejected. The header is redundant and adds visual weight without value. The user approved its removal in the mockup.

### Remove Hints Entirely

Not approved. The user's mockup shortens the hints but does not remove them. The hints provide context about when or how the setting applies.

## Open Questions

None. All copy and spacing decisions are approved and verified against the mockup. The design context claims are verified against the source code.

## Spec Delta Required

During implementation, planning gaps were discovered: three requirements in two specifications quote the UI labels this change modifies.

### First Planning Gap: `preferences-ui` "Updates page options"

The `preferences-ui` specification (`openspec/specs/preferences-ui/spec.md`, line 192-229) quotes the UI labels this change modifies:

- **Line 194:** `"View release notes" action on the title row after the channel chip`
- **Line 194:** `under an "UPDATE PREFERENCES" section`
- **Line 194, 201:** `"Notify about pre-release versions"` control label
- **Line 200:** `"View release notes"` (in Scenario "Options offered")
- **Line 201:** `under "UPDATE PREFERENCES"` (in Scenario "Options offered")
- **Line 206:** `"Notify about pre-release versions"` (in Scenario "Defaults")

**Decision:** The requirement "Updates page options" is MODIFIED to align with the implemented UI:
- `"View release notes"` → `"Release notes"`
- `"Notify about pre-release versions"` → `"Notify about pre-releases"`
- Remove the phrase `under an "UPDATE PREFERENCES" section` and `under "UPDATE PREFERENCES"` because the section header is removed.

### Second Planning Gap: `preferences-ui` "Release notes from Settings"

The independent verifier found a second reference in `preferences-ui` (`openspec/specs/preferences-ui/spec.md`, line 651-659):

- **Line 657:** `"View release notes"` (in Scenario "Open release notes")

**Decision:** The requirement "Release notes from Settings" is MODIFIED to update the scenario:
- `"View release notes"` → `"Release notes"`

The requirement body (`... open the application release notes from the status card.`) is unchanged. The pre-existing "from the status card" wording is not changed by this change; that discrepancy is parked in `docs/backlog.md`.

### Third Planning Gap: `settings-theme` "Persistent page header"

The independent verifier found a reference in `settings-theme` (`openspec/specs/settings-theme/spec.md`, line 241-338):

- **Line 328:** `"View release notes"` link reference (in Scenario "Title chip still wraps with the title")

**Decision:** The requirement "Persistent page header" is MODIFIED to update the scenario:
- `"View release notes"` → `"Release notes"`

### Completeness Sweep

A full sweep of `openspec/specs/` for `View release notes`, `UPDATE PREFERENCES`, and the UI label `pre-release versions` confirmed exactly these three requirements contain the old labels. No other durable specs quote them.

The behavior contract remains unchanged: the page still offers the same controls with the same functionality. These are UI-label and layout alignment deltas only.

**Rationale:** The original proposal incorrectly stated `skip_specs: true` based on the assumption that UI strings were not quoted in any durable specification. The implementer and verifier found that three requirements across two capabilities do quote these labels, so deltas are required to keep the specs synchronized with the implemented UI.
