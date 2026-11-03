# Proposal: Logs Page Copy Revision

## Why

The Settings → Logs page copy includes verbose hints and labels that can be made more concise while
retaining clarity. The user reviewed a mockup (`mockup-settings-logs.html`) that removes the section
headers and shortens the hints, and approved the final copy for implementation.

This change applies only approved copy edits. No behavior, layout, style token, control ordering, or
settings contract changes are included.

## What Changes

### XAML (`src/CLIHub/Views/SettingsLogsPage.xaml`)

1. Remove two `<controls:SectionHeader .../>` elements (lines 11, 51): `Text="LOGGING"` and `Text="LOG FILE"`.
2. `Log level` hint (line 14): `Takes effect after the next start.` → `Applies after restart.`
3. `Log retention` hint (line 31): `Daily log files to keep.` → `Daily files to keep.`
4. `Log size limit` hint (line 41): `The per-file limit (0 = unlimited).` → `Per file. 0 = no limit.`
5. Log file card (lines 52-53): title `Log file` → `Current log file`; remove the `Hint` attribute and
   value entirely (`The current log file path.` is deleted).

### Validation Messages (`src/CLIHub/Settings/SettingsApplicationService.cs`)

6. Retention validation error (line 248): `Log retention must be a positive number of days.` →
   `Log retention must be at least 1 day.`
7. Size validation error (line 254): `Log file size limit must be zero or a positive number of megabytes.` →
   `Log size limit must be 0 MB or more.`

### Explicit Non-Changes

- The page description in `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs` remains exactly
  `Log level, retention, and the log file.` — the user explicitly decided to keep it unchanged.
- All other labels, tooltips, log-level option names, units, icons, controls, margins, order, styles, and
  behavior are unchanged.

## Impact

- User-facing copy is more concise.
- Vertical rhythm after removing section headers: the `SectionHeader` style in `SettingsStyles.xaml` sets
  `Margin="0,18,0,8"`, which gives the 18 px top gap and 8 px bottom gap. Removing both headers requires
  setting `Margin="0,18,0,8"` on the `SettingsGroup` so the first group starts 18 px below the page divider
  and leaves 8 px below the group. The standalone file card keeps its default `Margin="0,0,0,8"` from the
  `SettingCard` style; the group's 8 px bottom margin plus the card's zero top margin yields the required
  8 px gap. This is a local margin on the page, not a new style token or shared style change.
- No specs, tests, or behavior contracts change.

## Dependencies

None. This change is independent and has no blocking or dependent changes.
