# Name the fields on the DEFAULTS card detail line

## Why

The DEFAULTS summary card on Settings → Agent Detection shows a generic hint —
`Used when a field is empty` (`ProbePolicyDefaultsDetail`,
`src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:377`) — under its value line
(`15 min · 10 sec`, `ProbePolicyDefaults`, same file:372). The hint explains *when* the
defaults apply but never says *what* they are defaults for, even though the card's own
value line already carries two numbers (TTL and timeout) with no labels attached. The
user asked for the detail line to name the two fields instead, using the same
`label · label` / `value · value` middle-dot convention already used elsewhere on this
page (e.g. the DAEMON card's `Last scan 19:54:57 · 27 folders`,
`src/CLIHub/ViewModels/Settings/WatchedDirectoryEditor.cs:667`).

## What Changes

- `ProbePolicyDefaultsDetail` (`SettingsViewModel.cs:377`) changes from the literal
  `"Used when a field is empty"` to the literal `"Cache TTL · Checking timeout"`
  (U+00B7 middle dot, one space on each side). Its XML doc comment (`:374-376`) is
  updated to describe the new content.
- The DEFAULTS card's `DetailToolTip` in `src/CLIHub/Views/SettingsWindow.xaml:268`
  changes from `"Applied when a detection field is empty"` to a short sentence that
  matches the new detail line instead of contradicting it (exact wording in
  `design.md`).
- The test assertion at
  `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs:904`
  updates to expect the new literal.
- Two durable specs that currently quote the old copy get a MODIFIED requirement each,
  preserving their existing scenario names:
  - `openspec/specs/settings-theme/spec.md` — scenario `Defaults card detail`
    (`:499-502`), under requirement `Agent Detection page summary cards` (`:453`).
  - `openspec/specs/preferences-ui/spec.md` — scenario `DEFAULTS card detail`
    (`:752-755`), under requirement `Agent Detection page content` (`:695`).

## Impact

- Code: one C# string literal + its doc comment (`SettingsViewModel.cs`), one XAML
  attribute value (`SettingsWindow.xaml`). No bindings, commands, layout, control types,
  or other summary-card attributes change.
- Tests: one assertion string updated (`SettingsViewModelApplicationTests.cs:904`); no
  new test is needed since the existing test already pins this property.
- Specs: two MODIFIED requirements (`settings-theme`, `preferences-ui`), each changing
  only the THEN clause of one named scenario; requirement sentences, every other
  scenario, and all scenario names are otherwise byte-identical to the current main
  specs.
- Docs/CHANGELOG: out of scope per the brief; not touched by this proposal (left for the
  implementing session to decide per repo convention if it wants a CHANGELOG line).
- Risk: minimal. Copy-only change to one VM property and one tooltip string; build and
  the existing test suite are the gate.

## Non-goals

- No change to the DEFAULTS card's value line (`ProbePolicyDefaults`, stays
  `"15 min · 10 sec"`).
- No change to the CACHE TTL or TIMEOUT cards, their details, or their tooltips
  (`ProbePolicyCacheTtlDetail`, `ProbePolicyTimeoutDetail`, and the tooltips at
  `SettingsWindow.xaml:256,262`).
- No layout, control, icon, or binding changes to `SummaryCard` or the
  `AgentDetectionSummaryCards` `DataTemplate`.
- No behavior change: the Reset action, field validation, and effective-value
  resolution are untouched.
- No change to any other Settings page or summary card (Projects, Agents, Updates,
  DAEMON, etc.).

## Dependencies

None. The unrelated active change `runtime-test-conditional-skips`
(`skip_specs: true`, 0/33 tasks) touches test conditional-skip infrastructure, not this
page or these specs — no ordering dependency either way.
