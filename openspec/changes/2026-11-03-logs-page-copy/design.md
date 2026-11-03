# Design: Logs Page Copy Revision

## Verified Current State

All line numbers and strings verified at HEAD `c2fec2b`.

### `src/CLIHub/Views/SettingsLogsPage.xaml`

- **Line 11:** `<controls:SectionHeader Text="LOGGING"/>`
- **Line 14:** `Hint="Takes effect after the next start."`
- **Line 31:** `Hint="Daily log files to keep."`
- **Line 41:** `Hint="The per-file limit (0 = unlimited)."`
- **Line 51:** `<controls:SectionHeader Text="LOG FILE"/>`
- **Line 52:** `Title="Log file"`
- **Line 53:** `Hint="The current log file path."`

### `src/CLIHub/Settings/SettingsApplicationService.cs`

- **Line 248:** `error = "Log retention must be a positive number of days.";`
- **Line 254:** `error = "Log file size limit must be zero or a positive number of megabytes.";`

### `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs`

- **Line 99:** `"Log level, retention, and the log file."`

### Test Coverage

No tests in `tests/CLIHub.Tests/`, `tests/CLIHub.Core.Tests/`, or `tests/CLIHub.Core.Windows.Tests/`
assert any of the changed strings. The validation logic is tested, but the exact error message text is not.

## Decisions

### Remove Section Headers and Set Explicit Margins

The two `SectionHeader` controls (`LOGGING` and `LOG FILE`) are removed entirely. The `SectionHeader`
style in `src/CLIHub/Themes/SettingsStyles.xaml` (line 156) sets `Margin="0,18,0,8"`, which gives the
18 px top gap and 8 px bottom gap. Removing both headers removes that spacing.

To preserve the page's vertical rhythm, the `SettingsGroup` (which has no Margin setter in its implicit
style, lines 301-338 of `SettingsStyles.xaml`) gets an explicit local `Margin="0,18,0,8"` on the page, so
the first settings group starts 18 px below the page divider and leaves 8 px below the group.

The standalone file card keeps its default `Margin="0,0,0,8"` from the `SettingCard` style (line 186 of
`SettingsStyles.xaml`). The group's 8 px bottom margin plus the card's zero top margin yields the required
8 px gap between the group and the card.

This is a local margin set on the page's `SettingsGroup` instance, not a new style token or shared style
change.

**Rationale:** The headers add visual weight without communicating information beyond what the settings
themselves convey. The mockup shows the page remains scannable without them. The explicit margin preserves
the approved vertical rhythm.

### Shorten Hints

The three hints in the settings group are shortened:

- `Takes effect after the next start.` → `Applies after restart.`
- `Daily log files to keep.` → `Daily files to keep.`
- `The per-file limit (0 = unlimited).` → `Per file. 0 = no limit.`

**Rationale:** The shorter copy removes redundant words (`after the next start` vs. `after restart`,
`Daily log files to keep` vs. `Daily files to keep`) and uses a concise zero convention
(`0 = no limit`). The meaning is unchanged.

### Update Log File Card Title and Remove Hint

The log file card title changes from `Log file` to `Current log file`, and the `Hint` attribute
(`The current log file path.`) is removed entirely.

**Rationale:** "Current" disambiguates it from the historical files covered by retention. The hint repeats
what the card itself shows (a file path), so removing it reduces clutter without losing information.

### Shorten Validation Errors

The two validation error messages are shortened:

- `Log retention must be a positive number of days.` → `Log retention must be at least 1 day.`
- `Log file size limit must be zero or a positive number of megabytes.` →
  `Log size limit must be 0 MB or more.`

**Rationale:** "At least 1 day" and "0 MB or more" are more direct and match the numeric-field constraints
shown in the UI.

### Keep Page Description Unchanged

The page description in `SettingsNavigation.cs` remains `Log level, retention, and the log file.` The user
reviewed it alongside the mockup and explicitly decided to keep it unchanged.

**Rationale:** The description is a one-line navigation summary, not a UI label on the page itself. It
remains accurate and concise as written.

## Alternatives Rejected

### Change the Page Description

Rejected by the user. The existing description is accurate and remains unchanged.

### Add New Style Tokens for Vertical Rhythm

Not needed. The existing StackPanel spacing preserves the page rhythm after the section headers are removed.
The mockup confirms this.

## Open Questions

None. All copy and spacing decisions are approved and verified against the mockup.
