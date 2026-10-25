# Tasks: agents-detection-wording

Copy-only change. Edit string literals and their pinned assertions only — if a task seems to need
a command, binding, style, identifier, or configuration-key change, stop and report instead.
Exact old/new strings are in design.md "Wording table"; every task below repeats them so a fresh
session needs no other context.

## 1. Stage 1 — XAML copy

- [x] 1.1 In `src/CLIHub/Views/SettingsAgentsPage.xaml`: line 96 change
      `<controls:SectionHeader Text="PROBE"/>` → `Text="DETECTION"`; line 98 change SettingCard
      `Title="Probe TTL"` → `Title="Cache TTL"`; line 99 change Hint
      `How long agent availability is cached. Empty — use defaults.` → `How long detection and
      version results are cached. Empty — use defaults.` (keep the em dash); line 109 change
      `Title="Probe timeout"` → `Title="Version check timeout"`; line 110 change Hint
      `How long a single probe may take. Empty — use defaults.` → `How long a single version
      check may take. Empty — use defaults.`
- [x] 1.2 In `src/CLIHub/Views/SettingsAgentsPage.xaml` detect button (lines 123-141): line 126
      `AutomationProperties.Name="Re-detect installed agents"` → `"Detect installed agents and
      updates"`; line 130 `ToolTip="Re-detect installed agents and refresh the lists"` →
      `"Re-detect installed agents, versions, and available updates"`; line 139
      `Text="Refresh"` → `Text="Detect"`. Do NOT touch `Command`, `IsEnabled`, the
      `IconGlyphs.Refresh` glyph, or the style key.
- [x] 1.3 In `src/CLIHub/Views/SettingsAgentsPage.xaml` line 216: per-card button
      `ToolTip="Probe installation and version"` → `"Detect installation and version"` (leave
      `AutomationProperties.Name="Test agent"` as is).
- [x] 1.4 In `src/CLIHub/Views/SettingsWindow.xaml` third `AgentsSummaryCards` card
      (lines 250-254): line 251 `DetailToolTip="Effective agent probe cache and timeout"` →
      `"Effective detection cache and version-check timeout"`; line 253 `Label="PROBE"` →
      `Label="DETECTION"`. Do NOT touch the `Detail`/`Value` bindings
      (`ProbePolicyDetail`/`ProbePolicySummary` keep their names) or the other two cards.

## 2. Stage 2 — C# copy and pinned assertions

- [x] 2.1 In `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`: line 317 page description
      `"Installed CLI agents, the default agent, and probe settings."` → `"...and detection
      settings."`; line 708 `"Custom probe settings"` → `"Custom detection settings"`
      (`"Built-in defaults"` on line 709 and the `ProbePolicySummary` format stay unchanged; do
      not rename any property).
- [x] 2.2 In `src/CLIHub/Settings/SettingsApplicationService.cs`: line 236
      `"Probe TTL must be a positive number of minutes, or empty for the default."` →
      `"Cache TTL must be a positive number of minutes, or empty for the default."`; line 242
      `"Probe timeout must be a positive number of seconds, or empty for the default."` →
      `"Version check timeout must be a positive number of seconds, or empty for the default."`
      (design D1).
- [x] 2.3 Update the pinned assertions:
      `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs:852`
      `"Custom probe settings"` → `"Custom detection settings"`; same file line 867
      `"Probe timeout must be..."` → `"Version check timeout must be a positive number of
      seconds, or empty for the default."`;
      `tests/CLIHub.Tests/Services/SettingsApplicationServiceTests.cs:29`
      `"Probe TTL must be..."` → `"Cache TTL must be a positive number of minutes, or empty for
      the default."`. No other test edits; grep `tests/` for `Custom probe settings|Probe TTL
      must|Probe timeout must` afterwards to confirm zero stale hits.

## 3. Gate — build and tests after the copy stages

- [x] 3.1 Gate: `dotnet build CLIHub.sln` — zero errors (XAML compiles).
- [x] 3.2 Gate: `dotnet test CLIHub.sln` — zero failures (the three updated assertions pass;
      everything else is expectation-identical to HEAD).

## 4. Stage 3 — docs

- [x] 4.1 `docs/architecture/ui.md:56`: "the agents, default-agent, and probe summary cards use
      it" → "...and detection summary cards use it" (keep the glossary link).
- [x] 4.2 `docs/glossary.md:105-111`: keep the "Detection cache" entry and the internal umbrella
      term "Probe"; extend the "Probe" entry with a note that the Settings UI labels this area
      "DETECTION" (section header and summary card) with "Cache TTL" and "Version check timeout"
      fields, while the configuration keys keep their `agentProbe*` names (design D5).
- [x] 4.3 `docs/images/README.md:14`: "Settings → CLI Agents (installed agents, default agent,
      probe settings)." → "...detection settings)." (do not regenerate the PNG; it is a non-goal).
- [x] 4.4 `CHANGELOG.md`: add one "Changed" line under the current release heading convention
      (latest today `## 1.0.1 — 2026-10-24`) stating the CLI Agents detection wording rename and
      the Refresh → Detect button label; skip only if the maintainer opts out (design D6).

## 5. Stage 4 — final gates

- [x] 5.1 Gate: `openspec validate --all` — zero failures (baseline at proposal time was
      30 passed, 0 failed; the pre-existing long-requirement warnings are unchanged).
- [x] 5.2 `git status --porcelain` shows only: the four `src/` copy files, the two test files,
      the three docs files, optional `CHANGELOG.md` from 4.4, and
      `openspec/changes/agents-detection-wording/` — nothing else. (Pre-existing edits from other
      work in the tree are not mine; leave them untouched.)
- [x] 5.3 Run `graphify update .` so the knowledge graph reflects the edits and the new change
      directory.

## 6. Stage 5 — manual smoke and wrap-up

- [ ] 6.1 Manual smoke (user-confirmed; blocks commit/archive per repo policy): open
      Settings → CLI Agents and confirm — the third summary card is labelled "DETECTION" (value
      still "15 min · 10 s", detail "Built-in defaults", tooltip "Effective detection cache and
      version-check timeout"); the section header reads "DETECTION"; the field cards read
      "Cache TTL" and "Version check timeout" with the new hints; the button above the list is
      labelled "Detect" and still runs detection (cards, versions, and update chips refresh);
      the per-card diagnostic tooltip reads "Detect installation and version"; typing an invalid
      value (e.g. 0) into "Version check timeout" and saving shows the new "Version check timeout
      must be..." error and does not save; setting a custom value flips the card detail to
      "Custom detection settings".
- [ ] 6.2 Report results and wait for the user's explicit approval before any `git commit` or
      `openspec archive agents-detection-wording` (repo policy; approval for one never carries to
      the other; 6.1 must be confirmed first).
