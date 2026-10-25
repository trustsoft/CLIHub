# Rename the user-visible agent detection terminology on Settings → CLI Agents

## Why

The Settings → CLI Agents page exposes CLIHub's agent detection feature to the user under two
confusing wordings:

1. The chip button above the agent list is labelled **"Refresh"**
   (`src/CLIHub/Views/SettingsAgentsPage.xaml:139`) but does not refresh a list the user edited —
   it runs `AgentsState.DetectCommand` (`src/CLIHub/Views/SettingsAgentsPage.xaml:127`), which
   detects installed agents, their installed versions, and available updates. The control's own
   automation name and tooltip already say "Re-detect"
   (`src/CLIHub/Views/SettingsAgentsPage.xaml:126,130`); only the visible label lies.
2. The page-header summary card labelled **"PROBE"**
   (`src/CLIHub/Views/SettingsWindow.xaml:253`) and the section header **"PROBE"**
   (`src/CLIHub/Views/SettingsAgentsPage.xaml:96`) present internal jargon ("probe") where the
   rest of the product says detection (the service is `IAgentDetectionService`, the spec
   capability is `agent-detection`, the glossary speaks of "Detection cache").

The user chose **whole-page terminology consistency**: one pass that renames every user-visible
"probe"/"Refresh" wording on this page to detection wording, while leaving internal identifiers
and configuration keys untouched.

## What Changes

A copy-only rename across the page's user-visible strings (full table with file:line evidence in
`design.md`):

- Section header `PROBE` → `DETECTION` (`SettingsAgentsPage.xaml:96`); summary-card label
  `PROBE` → `DETECTION` (`SettingsWindow.xaml:253`).
- Field card `Probe TTL` → `Cache TTL` with the hint "How long detection and version results are
  cached. Empty — use defaults." (`SettingsAgentsPage.xaml:98-99`); field card `Probe timeout` →
  `Version check timeout` with the hint "How long a single version check may take. Empty — use
  defaults." (`SettingsAgentsPage.xaml:109-110`).
- Detect button: visible label `Refresh` → `Detect`, tooltip → "Re-detect installed agents,
  versions, and available updates", automation name → "Detect installed agents and updates"
  (`SettingsAgentsPage.xaml:126,130,139`). Command, icon, and enable-state bindings unchanged.
- Per-card diagnostic button tooltip "Probe installation and version" → "Detect installation and
  version" (`SettingsAgentsPage.xaml:216`).
- Summary-card `DetailToolTip` "Effective agent probe cache and timeout" → "Effective detection
  cache and version-check timeout" (`SettingsWindow.xaml:251`).
- Page description "…and probe settings." → "…and detection settings."
  (`SettingsViewModel.cs:317`); custom-state copy "Custom probe settings" → "Custom detection
  settings" (`SettingsViewModel.cs:708`; "Built-in defaults" stays).
- Validation errors that name the renamed fields follow them: "Probe TTL must be…" → "Cache TTL
  must be a positive number of minutes, or empty for the default." and "Probe timeout must be…"
  → "Version check timeout must be a positive number of seconds, or empty for the default."
  (`src/CLIHub/Settings/SettingsApplicationService.cs:236,242`), with the three test assertions
  that pin these strings updated to match
  (`tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs:852,867`,
  `tests/CLIHub.Tests/Services/SettingsApplicationServiceTests.cs:29`).
- Docs that describe the UI labels: `docs/architecture/ui.md:56`, `docs/glossary.md:105-111`
  (the "Probe" umbrella term stays as an internal term, noted as such), `docs/images/README.md:14`.

Nothing behavioral changes: no command, binding, style, layout, icon, cache, timeout, or
persistence semantics are touched, and no identifier or configuration key is renamed
(`ProbeTtlText`, `ProbePolicySummary`, `agentProbeTtlMinutes`, `IAgentDetectionService`, etc. keep
their names).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `settings-theme`: *CLI Agents page summary cards* — the header card is now the "DETECTION" card
  and its body/scenario wording speaks of the detection cache TTL and version-check timeout
  (scenario names preserved verbatim, including "Probe card shows the effective policy" and
  "Custom probe values"). *Sectioned form layout*, *Dark input controls*, *Cards on the data
  pages*, and *Persistent page header* are reconciled where they name the probe fields or the
  probe cards ("Agents probe" example, "probe TTL and timeout fields", "agents-probe fields",
  "probe cards").
- `preferences-ui`: *CLI Agents page management* — the detect action above the agent cards is
  labelled "Detect" (new scenario; existing scenario names preserved verbatim, including "Detect
  action re-probes every supported agent"). *Agents probe fields* (requirement name unchanged),
  *Settings sections*, and *Save applies preferences* are reconciled where they name the
  agents-probe fields or a probe value.

## Impact

- Code (copy strings only): `src/CLIHub/Views/SettingsAgentsPage.xaml` (7 strings),
  `src/CLIHub/Views/SettingsWindow.xaml` (2 strings),
  `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` (2 strings),
  `src/CLIHub/Settings/SettingsApplicationService.cs` (2 validation messages).
- Tests: three assertion strings in
  `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs` and
  `tests/CLIHub.Tests/Services/SettingsApplicationServiceTests.cs`; no test logic changes.
- Docs: `docs/architecture/ui.md`, `docs/glossary.md`, `docs/images/README.md` (plus the default
  `CHANGELOG.md` "Changed" entry). The `docs/images/settings-agents.png` capture itself still
  shows the old labels until someone regenerates it; regeneration is out of scope.
- Specs: `settings-theme` and `preferences-ui` deltas in this change; no other capability's spec
  mentions the UI labels (the `agent-detection`, `agent-version`, and `agent-update-detection`
  specs use "probe" as the internal operation term and are untouched).
- Risk: minimal — string literals and their pinned assertions; XAML compilation and the test
  suite catch typos. No schema, config-key, or persistence impact (`agentProbeTtlMinutes` /
  `agentProbeTimeoutSeconds` unchanged), so stored configurations remain valid.

## Non-goals

- No change to detection behavior, caching, timeouts, or the `DetectCommand` flow — labels only.
- No configuration-key or identifier rename: `ProbeTtlText`, `ProbeTimeoutText`,
  `ProbePolicySummary`, `ProbePolicyDetail`, `ProbeTtlWatermark`, `ProbeAsync`,
  `IAgentDetectionService`, `AgentProbeTtlMinutes`, `agentProbeTtlMinutes`,
  `agentProbeTimeoutSeconds`, and friends keep their names.
- No layout, icon, grouping, ordering, or style changes; the button keeps the refresh glyph
  (`IconGlyphs.Refresh`) and the `AgentsHeaderChipButton` style.
- No per-agent card redesign and no change to the other two summary cards ("AGENTS", "DEFAULT").
- No renaming of the glossary's internal umbrella term "Probe", of the `agent-detection` /
  `agent-version` / `agent-update-detection` capabilities, or of internal code comments and XML
  doc that speak of probes.
- No changes to other settings pages, the legacy `MainWindow`, or the launch window's agent pane.
- No regeneration of `docs/images/settings-agents.png`.
- No spec requirement or scenario identifier renames — MODIFIED requirements keep their original
  names and scenario names.

## Dependencies

None on other active changes. Self-contained copy edit on surfaces last touched by archived
`2026-10-25-agents-probe-order` (page block order) and the settings pages/summary-card changes it
builds on; those orders and structures are preserved verbatim.
