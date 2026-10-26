# Tasks: settings-tooltip-copy

Two passes. Pass 1 (2026-10-26, stages 3-6 below, checked) applied the first wording to the
working tree — including `; no update check` / `; no re-detect` clauses on the Agents header
actions — added the CHANGELOG line, ran build/tests/graphify. The user decision later the same
day revises the three CLI Agents action tooltips (stage 1) and relaxes the `agent-ui` requirement
(stage 2; the delta file is already authored in this change). Edit tooltip string literals and the
noted doc line only — if a task seems to need a command, binding, style, layout, or
`AutomationProperties.Name` change, stop and report instead. Sentence case, no trailing period;
none of the final after-strings contain an apostrophe.

## 1. Stage 1 — SettingsAgentsPage.xaml revision (user decision 2026-10-26)

- [x] 1.1 In `src/CLIHub/Views/SettingsAgentsPage.xaml` line 138 (header "Detect" button):
      `ToolTip="Re-detect installed agents and versions; no update check"` →
      `ToolTip="Detect installed agents and versions"`. Do NOT touch
      `AutomationProperties.Name="Detect installed agents and versions"` (line 134), `Command`,
      `IsEnabled`, or the style.
- [x] 1.2 Same file line 154 (header "Check updates" button):
      `ToolTip="Check for agent updates; no re-detect"` →
      `ToolTip="Check for agent updates"`. Do NOT touch
      `AutomationProperties.Name="Check for agent updates"` (line 150), `Command`, `IsEnabled`, or
      the style.
- [x] 1.3 Same file line 212 (update-available chip Border) — applied correctly in pass 1 and
      untouched by the user decision; verify only that
      `ToolTip="A newer version is available"` is present.
- [x] 1.4 Same file line 242 (per-card Detect icon button):
      `ToolTip="Detect installation and version; no update check"` →
      `ToolTip="Detect installation and version"`. Do NOT touch
      `AutomationProperties.Name="Detect agent"` or the Command/CommandParameter.
- [x] 1.5 Lines 254 (`Run the agent's update command`) and 266 (`Open the agent's plugin.json`)
      left byte-identical — already short (design census).

## 2. Stage 2 — agent-ui spec delta (user decision 2026-10-26; delta authored in this change)

- [x] 2.1 Verify this change's `specs/agent-ui/spec.md`: exactly one `## MODIFIED Requirements`
      block for "Agent management header actions"; the disambiguation sentence reads "Each header
      action SHALL expose a tooltip and automation name that state its own work and distinguish it
      from the other header action."; the "Tooltips disambiguate the header actions" THEN reads
      "its tooltip and automation name state only that action's work, using wording that
      distinguishes it from the other header action"; all eight scenario names and every other
      line byte-identical to `openspec/specs/agent-ui/spec.md:394-442`. If the main spec block
      moved since 2026-10-26, regenerate by script like design D8 describes — do not hand-merge.
- [x] 2.2 Confirm `.openspec.yaml` in this change has `schema: spec-driven`, `created:
      2026-10-26`, and NO `skip_specs` line (a delta exists now).
- [ ] 2.3 At archive time only (needs the user's explicit approval per repo policy): the delta
      replaces the main-spec requirement in place; all eight scenario names carry over; nothing
      else in `openspec/specs/` changes.

## 3. Stage 3 — SettingsWindow.xaml (applied in pass 1; verify, do not re-edit)

- [x] 3.1 Line 163 (Project Scanning header add-root button): `ToolTip="Add a watched root
      directory"` → `ToolTip="Add a watched root"`; `AutomationProperties.Name="Add Directory"`
      untouched.
- [x] 3.2 Line 174 (Project Scanning header rescan button): `ToolTip="Rescan every watched
      folder"` → `ToolTip="Rescan every watched root"`; `AutomationProperties.Name="Scan Now"`
      untouched.
- [x] 3.3 Line 186 (`ProjectsSummaryCards` PROJECTS card): `DetailToolTip="Projects found by
      scanning plus projects added manually"` → `DetailToolTip="Found by scanning or added
      manually"`; Detail/Value/Icon bindings untouched.
- [x] 3.4 Line 209 (`RegistrySummaryCards` PROJECTS card): same replacement as 3.3.
- [x] 3.5 Line 207 (same card, alert): `AlertToolTip="Projects whose folder no longer exists"` →
      `AlertToolTip="Projects with missing folders"`; `IsAlertVisible`/`Alert` bindings untouched.
- [x] 3.6 Line 231 (AGENTS summary card): `DetailToolTip="Installed agents and the number of
      supported plugins"` → `DetailToolTip="Installed agents and supported plugin count"`.
- [x] 3.7 Line 244 (DETECTION summary card): `DetailToolTip="Effective detection cache and
      version-check timeout"` → `DetailToolTip="Effective cache and version-check timeout"`.
- [x] 3.8 Lines 121 (`{Binding UpdateStatusDetail}`), 134 (`Check for updates`), 148 (`Open
      What's New`), 197 (`{Binding WatcherDetailText}`), 241 (`ValueToolTip="{Binding
      AgentsState.DefaultAgentName}"`), 295 (`Maximize`), 311 (`Close`) left byte-identical.

## 4. Stage 4 — SettingsAppearancePage.xaml (applied in pass 1; verify, do not re-edit)

- [x] 4.1 Line 204 (custom accent TextBox): `ToolTip="Enter a custom accent color as #RRGGBB"` →
      `ToolTip="Custom accent color as #RRGGBB"`; `Tag="#RRGGBB"` and the `AccentText` binding
      untouched.
- [x] 4.2 No edits in `SettingsGeneralPage.xaml`, `SettingsHotkeysPage.xaml`,
      `SettingsTerminalPage.xaml`, `SettingsUpdatesPage.xaml`, `SettingsPlaceholderPage.xaml`,
      `SettingsProjectRegistryPage.xaml`, `SettingsLogsPage.xaml`, `SettingsProjectsPage.xaml` —
      they hold only bindings or already-short literals (design census).

## 5. Gate — diff review and build

- [x] 5.1 Pass-1 gate: `git diff -- src/` showed only the twelve tooltip attribute values from
      proposal.md's table; zero lines touching `Command`, `Binding`, `Style`, layout attributes,
      or `AutomationProperties.Name`.
- [x] 5.2 Pass-1 gate: `dotnet build CLIHub.sln` — zero errors.
- [x] 5.3 Revision gate: `git diff -- src/CLIHub/Views/SettingsAgentsPage.xaml` shows exactly
      four changed lines versus HEAD — the three Stage 1 tooltip values (1.1, 1.2, 1.4 final
      strings) and the pass-1 chip value at line 212 — with no other edit; then
      `dotnet build CLIHub.sln` — zero errors.

## 6. Stage 6 — confirm nothing pinned the old copy (pass 1; re-check on revision)

- [x] 6.1 Grep `tests/` for `Re-detect installed agents|does not check for updates|distribution
      channel|watched root directory|watched folder|plus projects added manually|no longer exists|
      number of supported plugins|detection cache and version-check|Enter a custom accent` —
      zero hits.
- [x] 6.2 Grep `docs/` for the same patterns — zero relevant hits
      (`docs/architecture/plugins.md:29` "distribution channel" is the descriptor field and
      stays).
- [x] 6.3 Gate: `dotnet test CLIHub.sln` — zero failures. Verified by the orchestrator after
      closing the running app: 478 + 60 + 638 = 1176 passed, 0 failed.
- [x] 6.4 `CHANGELOG.md`: one "Changed" line added under the current heading ("The Settings
      tooltips are shortened to brief, plain sentence-case copy and the Project Scanning tooltips
      are aligned to the \"watched root\" glossary wording; copy-only string edits with no
      behavior change").
- [x] 6.5 Amend that CHANGELOG line to also mention the relaxed `agent-ui` tooltip-wording
      requirement (the change is no longer copy-only once the delta lands), e.g. append "; the
      `agent-ui` header-action tooltip rule now requires only that each action's tooltip and
      automation name state its own work distinctly". Skip only if the maintainer opts out
      (design Open question 2). Re-run the 6.1 grep for the new wording — still zero expected.

## 7. Stage 7 — final gates

- [x] 7.1 Gate: `openspec validate --all` — zero failures with the `agent-ui` delta present
      (observed green at revision time: "Totals: 30 passed, 0 failed (30 items)", including
      `change/settings-tooltip-copy`; the long-requirement warnings on the main specs are
      pre-existing). Re-run after the Stage 1 edits as the final gate.
- [x] 7.2 Gate: `git status --porcelain` shows only this change's files: the three edited XAML
      files, `CHANGELOG.md`, and `openspec/changes/settings-tooltip-copy/` — plus anything
      unrelated that appears; unrelated entries are not mine and stay untouched. (Note: the
      README-screenshot work referenced at revision time is already committed as `3f0dc55`; it is
      not pending in the tree.)
- [x] 7.3 Run `graphify update .` again so the knowledge graph reflects the revised tooltips and
      the delta file.

## 8. Stage 8 — manual smoke (blocks commit/archive)

- [x] 8.1 Manual smoke (user-confirmed; per repo policy `git commit` and `openspec archive
      settings-tooltip-copy` MUST wait for it): run the app, open Settings, and hover each changed
      tooltip —
      CLI Agents page: header "Detect" shows "Detect installed agents and versions"; header
      "Check updates" shows "Check for agent updates"; per-card Detect shows "Detect installation
      and version"; AGENTS header card detail shows "Installed agents and supported plugin count";
      DETECTION header card detail shows "Effective cache and version-check timeout". Project
      Scanning page: add-root button shows "Add a watched root"; rescan button shows "Rescan every
      watched root"; PROJECTS header card detail shows "Found by scanning or added manually".
      Projects (registry) page: PROJECTS card detail shows "Found by scanning or added manually".
      Appearance page: pick the Custom accent so the color box appears; its tooltip shows "Custom
      accent color as #RRGGBB".
      Conditionally visible tooltips — verify in the XAML diff if they cannot be reproduced: the
      update-available chip ("A newer version is available"; needs an available update) and the
      PROJECTS alert ("Projects with missing folders"; needs a project whose folder is gone).
- [x] 8.2 Confirm behavior is untouched during the smoke: Detect and Check updates still run and
      stay mutually exclusive while running; Detect leaves update indicators untouched; Check
      updates still bypasses the cached availability; rescan still rescans; add-root still opens
      the picker; custom accent still applies; Save/Cancel unchanged.
- [x] 8.3 Report results and wait for the user's explicit approval before any `git commit` or
      `openspec archive settings-tooltip-copy` (approval for one never carries to the other; 8.1
      must be confirmed first; archiving applies the `agent-ui` delta to the main spec).

> Smoke result (2026-10-26): confirmed OK by the user on the running application — the revised
> CLI Agents tooltips and the pass-1 Settings tooltips are shown as intended; Detect/Check-updates
> behavior unchanged.
