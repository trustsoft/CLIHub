# Tasks: agents-probe-order

Single implementation stage plus gates. No ViewModel, code-behind, binding, or text edits anywhere —
if a task seems to need one, stop and report instead. Every gate must pass before the next stage.
Open questions 1-3 (design "Open questions") default as written; apply a contrary decision before its
task instead of mid-stage.

## 1. Stage 1 — move the PROBE block above the AGENTS block

- [x] 1.1 In `src/CLIHub/Views/SettingsAgentsPage.xaml`, cut the PROBE block —
      `<controls:SectionHeader Text="PROBE"/>` (line 227) through the closing
      `</controls:SettingsGroup>` (line 250) — and paste it verbatim between the MANAGEMENT block's
      closing `</controls:SettingCard>` (line 94) and the AGENTS block's opening `<Grid>`
      (line 96), keeping exactly one blank line between adjacent blocks. The pasted block must be
      byte-identical to the cut block apart from its new position; no property, binding, style,
      text, or comment edits inside it.
- [x] 1.2 Verify the resulting child order of the root `StackPanel` (line 68) is exactly:
      `SectionHeader "MANAGEMENT"` + Default agent `SettingCard` → `SectionHeader "PROBE"` +
      `SettingsGroup` (Probe TTL, Probe timeout) → `Grid` with `SectionHeader "AGENTS"` + Refresh
      button → agent-card `ItemsControl`. Verify `UserControl.Resources` (lines 10-67) and its five
      style keys are untouched, and `git diff src/CLIHub/Views/SettingsAgentsPage.xaml` reads as a
      pure block move (one deleted hunk, one identical added hunk).

## 2. Gates

- [x] 2.1 Gate: `dotnet build CLIHub.sln` — zero errors (XAML compiles, so malformed markup fails
      here).
- [x] 2.2 Gate: `dotnet test CLIHub.sln` — zero failures (expectation identical to HEAD; no tests
      reference this page).
- [x] 2.3 Gate: `openspec validate --all` passes — zero failures (baseline at proposal time was
      30 passed, 0 failed), `spec/settings-theme` still ✓, and the pre-existing long-requirement
      warnings on the other specs are unchanged.

## 3. Verification and wrap-up

- [x] 3.1 Manual visual smoke (user-confirmed; blocks commit/archive per repo policy): open
      Settings → CLI Agents and confirm — the page shows MANAGEMENT (Default agent) → PROBE (Probe
      TTL and Probe timeout as grouped rows on the shared surface) → AGENTS (header + Refresh +
      agent cards); the pinned header row still shows the AGENTS, DEFAULT, PROBE summary cards in
      that order; editing a probe field still updates the pinned PROBE card; Refresh still
      re-detects agents. Keyboard tab order follows the new visual order (probe fields before
      agent-card buttons) and is accepted as intended.
- [x] 3.2 Apply open question 1 (default: add the entry): `CHANGELOG.md` — one "Changed" line under
      the current release heading convention stating the CLI Agents page now shows the probe
      settings above the agent list, no behavior change. Skip only if the maintainer opts out.
- [x] 3.3 `git status --porcelain` shows only `src/CLIHub/Views/SettingsAgentsPage.xaml`, optional
      `CHANGELOG.md` from 3.2, and `openspec/changes/agents-probe-order/` — nothing else from
      `src/`, `tests/`, `docs/`, or main specs.
- [x] 3.4 Run `graphify update .` so the knowledge graph reflects the XAML edit and the new change
      directory.
- [x] 3.5 Report results and wait for the user's explicit approval before any `git commit` or
      `openspec archive agents-probe-order` (repo policy; approval for one never carries to the
      other; 3.1 must be confirmed first).
