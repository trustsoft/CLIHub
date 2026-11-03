## Why

The Settings → Agent Detection page and its summary cards have accumulated inconsistencies: the hints duplicate "Empty — use defaults" in both field cards; the CACHE TTL and TIMEOUT summary cards repeat their visible detail text in a `DetailToolTip`; the DEFAULTS card detail reads "Checking timeout" while the field itself is named "Version check timeout"; and the validation messages use "positive number of minutes|seconds" when the minimum is 1 minute|second. These small differences create unnecessary friction when users read the UI or validation feedback. A single, consistent copy refresh across all Agent Detection strings (hints, tooltips, detail lines, and validation messages) will tighten the UI vocabulary and remove redundant information.

## What Changes

- Shorten the Cache TTL field hint from `"How long detection and version results are cached. Empty — use defaults."` to `"How long detection and version results are cached."` (remove the trailing sentence; the watermark already shows the default).
- Replace the Version check timeout field hint from `"How long a single version check may take. Empty — use defaults."` to `"Maximum time for one version check."` (state the limit rather than the duration; remove "single" and the trailing sentence).
- Change the DEFAULTS summary card detail from `Cache TTL · Checking timeout` to `Cache TTL · Version timeout` (matching the actual field card title).
- Revise the DEFAULTS card `DetailToolTip` from `Built-in defaults for cache TTL and version check timeout` to `Built-in values for cache TTL and version check timeout.` (clearer phrasing).
- Remove the `DetailToolTip` attributes from the CACHE TTL and TIMEOUT summary cards entirely (they duplicate the visible detail line).
- Tighten the validation messages from `positive number of minutes|seconds, or empty for the default` to `at least 1 minute|second. Leave empty to use the default.` (more precise and parallel).

All other UI text (page description, field titles, card labels, the Reset tooltip, and the "Built-in default" / "Custom (default: N min|sec)" detail templates) remain unchanged.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `preferences-ui`: The DEFAULTS card detail line scenario changes from `"Cache TTL · Checking timeout"` to `"Cache TTL · Version timeout"` to match the field card title; no new requirements.
- `settings-theme`: The DEFAULTS card detail line scenario changes identically; the requirement prose ("SHALL name the two detection parameters") still holds because `Cache TTL · Version timeout` names both parameters correctly.

## Impact

**Modified code:**
- `src/CLIHub/Views/SettingsAgentDetectionPage.xaml:37,48` — shorten field hints
- `src/CLIHub/Views/SettingsWindow.xaml:245,251,257` — remove CACHE TTL/TIMEOUT `DetailToolTip` attributes; update DEFAULTS `DetailToolTip`
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:377` — update `ProbePolicyDefaultsDetail` literal
- `src/CLIHub/Settings/SettingsApplicationService.cs:236,242` — revise validation messages

**Modified tests:**
- `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs:904,954` — update assertions for the new detail and validation strings
- `tests/CLIHub.Tests/Services/SettingsApplicationServiceTests.cs:30` — update assertion for the new validation message

**Modified specs:**
- `openspec/specs/preferences-ui/spec.md:774` — scenario "DEFAULTS card detail" changes the expected string
- `openspec/specs/settings-theme/spec.md:540` — scenario "Defaults card detail" changes the expected string

**No breaking changes.** The Settings page retains the same layout, card structure, and field behavior; only display strings change.
