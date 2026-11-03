## 1. XAML Hints

- [x] 1.1 In `src/CLIHub/Views/SettingsAgentDetectionPage.xaml:37`, change the Cache TTL `Hint` attribute from `"How long detection and version results are cached. Empty — use defaults."` to `"How long detection and version results are cached."` (remove the trailing sentence), and verify the XAML compiles
- [x] 1.2 In `src/CLIHub/Views/SettingsAgentDetectionPage.xaml:48`, change the Version check timeout `Hint` attribute from `"How long a single version check may take. Empty — use defaults."` to `"Maximum time for one version check."` (replace the entire string), and verify the XAML compiles

## 2. Summary Card Tooltips

- [x] 2.1 In `src/CLIHub/Views/SettingsWindow.xaml:245`, remove the `DetailToolTip="Custom cache TTL or the built-in default"` attribute entirely from the CACHE TTL `<controls:SummaryCard>` tag (the whole attribute, not just its value), and verify the XAML compiles
- [x] 2.2 In `src/CLIHub/Views/SettingsWindow.xaml:251`, remove the `DetailToolTip="Custom version check timeout or the built-in default"` attribute entirely from the TIMEOUT `<controls:SummaryCard>` tag, and verify the XAML compiles
- [x] 2.3 In `src/CLIHub/Views/SettingsWindow.xaml:257`, change the DEFAULTS card `DetailToolTip` attribute value from `"Built-in defaults for cache TTL and version check timeout"` to `"Built-in values for cache TTL and version check timeout."` (change "defaults" to "values" and add a period), and verify the XAML compiles

## 3. ViewModel Detail String

- [x] 3.1 In `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:377`, change the `ProbePolicyDefaultsDetail` property getter from `=> "Cache TTL · Checking timeout";` to `=> "Cache TTL · Version timeout";` (U+00B7 middle dot separator; "Checking" becomes "Version"), and verify the solution builds with 0 errors/warnings

## 4. Validation Messages

- [x] 4.1 In `src/CLIHub/Settings/SettingsApplicationService.cs:236`, change the validation `error` assignment from `"Cache TTL must be a positive number of minutes, or empty for the default."` to `"Cache TTL must be at least 1 minute. Leave empty to use the default."`, and verify the solution builds
- [x] 4.2 In `src/CLIHub/Settings/SettingsApplicationService.cs:242`, change the validation `error` assignment from `"Version check timeout must be a positive number of seconds, or empty for the default."` to `"Version check timeout must be at least 1 second. Leave empty to use the default."`, and verify the solution builds

## 5. Test Assertions

- [x] 5.1 In `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs:904`, update the `Assert.Equal` call from `"Cache TTL · Checking timeout"` to `"Cache TTL · Version timeout"` (the expected value for `fixture.ViewModel.ProbePolicyDefaultsDetail`), and verify the test project builds
- [x] 5.2 In `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs:954`, update the `Assert.Equal` call from `"Version check timeout must be a positive number of seconds, or empty for the default."` to `"Version check timeout must be at least 1 second. Leave empty to use the default."` (the expected `ValidationError` value), and verify the test project builds
- [x] 5.3 In `tests/CLIHub.Tests/Services/SettingsApplicationServiceTests.cs:30`, update the `Assert.Equal` call from `"Cache TTL must be a positive number of minutes, or empty for the default."` to `"Cache TTL must be at least 1 minute. Leave empty to use the default."` (the expected `error` value), and verify the test project builds

## 6. Verification

- [x] 6.1 Run `dotnet build CLIHub.sln` from the repository root and verify 0 errors, 0 warnings
- [x] 6.2 Run `dotnet test CLIHub.sln` from the repository root and verify all tests pass (no new failures introduced by the string changes)
- [x] 6.3 Run `openspec validate --all` from the repository root and verify 0 validation errors after the spec deltas are in place
- [x] 6.4 Manually open the Settings → Agent Detection page and verify: the Cache TTL hint reads "How long detection and version results are cached." with no trailing "Empty — use defaults."; the Version check timeout hint reads "Maximum time for one version check."; the DEFAULTS summary card detail line reads "Cache TTL · Version timeout" (hover tooltip says "Built-in values for cache TTL and version check timeout."); the CACHE TTL and TIMEOUT summary cards have no hover tooltip on the detail line; entering an invalid value (e.g., 0) in the Cache TTL field and clicking Save shows "Cache TTL must be at least 1 minute. Leave empty to use the default."; entering 0 in the Version check timeout field shows "Version check timeout must be at least 1 second. Leave empty to use the default." Smoke confirmed by the user.

> After 6.4 passes, all visible copy and validation messages match the approved set, and the change is ready for commit and archive.

## Implementation Notes

- Stages 1–5 applied as written; no deviations. `tasks.md` 6.4 is now ticked: the user confirmed the manual smoke.
- Gate evidence: `dotnet build CLIHub.sln` → `Build succeeded. 0 Warning(s) 0 Error(s)` after every stage and at the final gate. `dotnet test CLIHub.sln` final → CLIHub.Tests 574/574 passed, CLIHub.Core.Windows.Tests 67/67 passed, CLIHub.Core.Tests 703/703 passed (0 failed). `openspec validate --all` → `Totals: 30 passed, 0 failed (30 items)` including `change/agent-detection-copy-refresh`. `graphify update .` → `Code graph updated` (exit code 0).
- The stage-3 and stage-4 test runs were red by construction (1 and 3 failures respectively), each being an assertion that tasks 5.1–5.3 had not yet rewritten. No test was weakened or removed; the final run is green.
- Old strings still present elsewhere and intentionally untouched: `CHANGELOG.md:33` (historical entry), `openspec/specs/preferences-ui/spec.md:774` and `openspec/specs/settings-theme/spec.md:540` (main specs updated only at archive/sync time), and `openspec/changes/archive/` (history).

