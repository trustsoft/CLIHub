# Design: agents-detection-wording

## Current state (verified in code)

Working tree at HEAD `f970816`; all line numbers below were read from the files at proposal time.
The page block order is MANAGEMENT → PROBE → AGENTS (archived `2026-10-25-agents-probe-order`).

| Concern | Today | File |
|---|---|---|
| Section header | `<controls:SectionHeader Text="PROBE"/>` | `src/CLIHub/Views/SettingsAgentsPage.xaml:96` |
| Field card 1 | `SettingCard Title="Probe TTL"`, hint "How long agent availability is cached. Empty — use defaults." | `src/CLIHub/Views/SettingsAgentsPage.xaml:98-99` |
| Field card 2 | `SettingCard Title="Probe timeout"`, hint "How long a single probe may take. Empty — use defaults." | `src/CLIHub/Views/SettingsAgentsPage.xaml:109-110` |
| Detect button | `AutomationProperties.Name="Re-detect installed agents"` (:126), `Command="{Binding AgentsState.DetectCommand}"` (:127), `ToolTip="Re-detect installed agents and refresh the lists"` (:130), visible `Text="Refresh"` (:139), glyph `IconGlyphs.Refresh` (:135) | `src/CLIHub/Views/SettingsAgentsPage.xaml:123-141` |
| Per-card diagnostic button | `AutomationProperties.Name="Test agent"` (:212), `ToolTip="Probe installation and version"` (:216) | `src/CLIHub/Views/SettingsAgentsPage.xaml:210-220` |
| Summary card 3 | `Detail="{Binding ProbePolicyDetail}"` (:250), `DetailToolTip="Effective agent probe cache and timeout"` (:251), `Label="PROBE"` (:253), `Value="{Binding ProbePolicySummary}"` (:254) | `src/CLIHub/Views/SettingsWindow.xaml:250-254` |
| Page description | "Installed CLI agents, the default agent, and probe settings." | `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:317` |
| Summary strings | `ProbePolicySummary` => "15 min · 10 s" style; `ProbePolicyDetail` => "Custom probe settings" or "Built-in defaults" | `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:701-709` |
| Validation errors | "Probe TTL must be a positive number of minutes, or empty for the default." and "Probe timeout must be a positive number of seconds, or empty for the default." | `src/CLIHub/Settings/SettingsApplicationService.cs:236,242` |
| Tests pinning the copy | "Built-in defaults" (unchanged), "Custom probe settings", and both validation messages | `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs:838-839,852,867`; `tests/CLIHub.Tests/Services/SettingsApplicationServiceTests.cs:29` |
| Docs naming the labels | "probe summary cards"; "Detection cache" + "Probe" entries; "probe settings" screenshot description | `docs/architecture/ui.md:56`; `docs/glossary.md:105-111`; `docs/images/README.md:14` |

Blast-radius checks performed:

- No other `src/` file shows "PROBE" as a label or "Refresh" as visible text on this page. The
  other `IconGlyphs.Refresh` uses are the Updates page "Check for updates" button
  (`src/CLIHub/Views/SettingsWindow.xaml:137`) and Project Discovery "Scan Now"
  (`src/CLIHub/Views/SettingsWindow.xaml:177`) — unrelated. `MainWindow.xaml:129` has a "Refresh"
  button, but `MainWindow` is the unregistered legacy reference (`docs/project-context.md:51`) —
  out of scope.
- No UI-automation tests reference the detect button's automation name or the "Refresh" label;
  the only string-pinning tests are the four listed above ("Built-in defaults" keeps its value).
- The `agent-detection`, `agent-version`, and `agent-update-detection` specs use "probe" for the
  internal operation (e.g. `openspec/specs/agent-detection/spec.md:89-102`,
  `openspec/specs/agent-version/spec.md:53-61,123-149`); none of them name the Settings UI
  labels, so they are untouched.

## Wording table (old → new)

| # | Element | File:line | Old | New |
|---|---|---|---|---|
| 1 | Section header | `SettingsAgentsPage.xaml:96` | `PROBE` | `DETECTION` |
| 2 | Field card 1 title | `SettingsAgentsPage.xaml:98` | `Probe TTL` | `Cache TTL` |
| 3 | Field card 1 hint | `SettingsAgentsPage.xaml:99` | How long agent availability is cached. Empty — use defaults. | How long detection and version results are cached. Empty — use defaults. |
| 4 | Field card 2 title | `SettingsAgentsPage.xaml:109` | `Probe timeout` | `Version check timeout` |
| 5 | Field card 2 hint | `SettingsAgentsPage.xaml:110` | How long a single probe may take. Empty — use defaults. | How long a single version check may take. Empty — use defaults. |
| 6 | Detect button label | `SettingsAgentsPage.xaml:139` | `Refresh` | `Detect` |
| 7 | Detect button tooltip | `SettingsAgentsPage.xaml:130` | Re-detect installed agents and refresh the lists | Re-detect installed agents, versions, and available updates |
| 8 | Detect button automation name | `SettingsAgentsPage.xaml:126` | Re-detect installed agents | Detect installed agents and updates |
| 9 | Per-card diagnostic tooltip | `SettingsAgentsPage.xaml:216` | Probe installation and version | Detect installation and version |
| 10 | Summary card label | `SettingsWindow.xaml:253` | `PROBE` | `DETECTION` |
| 11 | Summary card detail tooltip | `SettingsWindow.xaml:251` | Effective agent probe cache and timeout | Effective detection cache and version-check timeout |
| 12 | Page description | `SettingsViewModel.cs:317` | Installed CLI agents, the default agent, and probe settings. | Installed CLI agents, the default agent, and detection settings. |
| 13 | Custom-state detail | `SettingsViewModel.cs:708` | Custom probe settings | Custom detection settings ("Built-in defaults" unchanged) |
| 14 | Validation error (minutes) | `SettingsApplicationService.cs:236` | Probe TTL must be a positive number of minutes, or empty for the default. | Cache TTL must be a positive number of minutes, or empty for the default. |
| 15 | Validation error (seconds) | `SettingsApplicationService.cs:242` | Probe timeout must be a positive number of seconds, or empty for the default. | Version check timeout must be a positive number of seconds, or empty for the default. |

Unchanged by design: `AutomationProperties.Name="Test agent"` on the per-card button (it never
said "probe"), `ProbePolicySummary`'s "N min · N s" value format, watermarks (`15` / `10`), the
refresh glyph, and every identifier/config key listed in the proposal's non-goals.

## Spec analysis

Every "probe" mention in the two affected specs was enumerated by search:

- `openspec/specs/settings-theme/spec.md`: lines 46, 50 (Sectioned form layout), 67, 74 (Dark
  input controls), 198 (Cards on the data pages → "CLI Agents page cards"), 260 (Persistent page
  header → "probe cards"), 345, 350, 368, 370-371, 373, 375-376, 380 (CLI Agents page summary
  cards). All are covered by the five MODIFIED requirements in this change's delta.
- `openspec/specs/preferences-ui/spec.md`: lines 5 (Purpose), 23, 45 (Settings sections), 100
  (Save applies preferences), 141-151 (Agents probe fields), 474, 478, 505-507 (CLI Agents page
  management). Line 5 is the Purpose prose ("agents and probing") — accurate as an internal
  description and not modifiable by a requirements delta, so it stays.

MODIFIED requirements keep their original names and scenario names verbatim (repo rule), so
identifiers such as requirement "Agents probe fields" and scenarios "Probe card shows the
effective policy", "Custom probe values", and "Detect action re-probes every supported agent"
survive as spec identifiers while their bodies adopt the user-facing wording. New scenario added:
"Detect action label" under *CLI Agents page management* pins the visible "Detect" label.

## Decisions

- **D1 — Validation error messages follow the field labels (extends the brief's list).** The two
  `SettingsApplicationService` messages name the fields by their card titles; after the rename
  they would cite labels that no longer exist on the page. Whole-page consistency (the user's
  chosen direction) requires "Cache TTL must be…" / "Version check timeout must be…". This is the
  only addition beyond the brief's enumerated strings; recorded here and in the proposal.
- **D2 — "Cache TTL" and "Version check timeout" instead of a literal "Detection TTL/timeout".**
  The cache spans detection *and* version results (`docs/glossary.md:108-110`,
  `AgentVersionService` TTL applies to version lookups), and the timeout bounds the version
  command only (`openspec/specs/agent-version/spec.md:53-61`). Naming each field for what it
  actually bounds avoids reintroducing the umbrella-word problem the rename is fixing. Wording
  taken from the brief as fixed.
- **D3 — "Detect" label, "Re-detect…" tooltip.** The visible label states the action; the
  tooltip and automation name retain the re- prefix because a first detection on a fresh
  configuration is indistinguishable from a re-detection in the UI and the existing strings
  already used it. Exact strings from the brief.
- **D4 — Scenario and requirement names stay, bodies move.** Preserves spec-identifier stability
  (archive diff shows only body edits) at the cost of two scenario names ("Probe card shows the
  effective policy", "Custom probe values") that now read as internal terms. Mandated by the
  OpenSpec rule that MODIFIED requirements keep original scenario names.
- **D5 — Glossary keeps "Probe" as the internal umbrella term** and gains a note that the UI
  labels the section/summary card "DETECTION" and the fields "Cache TTL" / "Version check
  timeout", pointing out that configuration keys keep their `agentProbe*` names. This prevents
  the glossary (internal vocabulary) and the UI (user vocabulary) from silently diverging again.
- **D6 — CHANGELOG entry by default.** Precedent: small user-visible renames get a one-line
  "Changed" entry (latest heading `## 1.0.1 — 2026-10-24`); skipping requires a maintainer opt-out
  at implementation time (task notes it).

## Alternatives considered and rejected

- **"Detect agents" as the button label:** longer; the button sits next to the AGENTS header so
  the noun is redundant; the brief fixed "Detect".
- **Keep "Re-detect" as the visible label:** accurate but reads as a correction of a previous
  action; the user explicitly picked "Detect".
- **Keep "PROBE" / "CACHE" / "CHECKS" as the section-card label:** "PROBE" is jargon (the reason
  for the change); "CACHE" describes only one of the two fields; "CHECKS" is vague. "DETECTION"
  matches `IAgentDetectionService`, the `agent-detection` capability, and the glossary's
  "Detection cache".
- **Rename the requirement "Agents probe fields" via a RENAMED delta:** rejected — the brief
  forbids changing requirement identifiers, and a rename would churn every reference for no
  behavioral gain.
- **Update internal comments/XML docs that say "probe"** (e.g.
  `src/CLIHub/Views/SettingsAgentsPage.xaml.cs:6`, `SettingsViewModel.cs:699,705`): rejected —
  internal terminology is explicitly retained; touching comments widens the diff without
  user-visible benefit.
- **Regenerate `docs/images/settings-agents.png`:** rejected for this change; the capture
  pipeline is a manual Chrome/app-capture step and the README text update suffices until the next
  scheduled capture.

## Deviations

- D1 (validation messages) is a deliberate, recorded extension of the brief's string list; every
  other string in the wording table is taken verbatim from the brief. The brief's verified
  file:line map matched the code exactly at HEAD `f970816` (button block at
  `SettingsAgentsPage.xaml:123-141`, summary card at `SettingsWindow.xaml:250-254`).

## Open questions

None. (D6's CHANGELOG default may be overridden by the maintainer at implementation time without
reopening this change.)
