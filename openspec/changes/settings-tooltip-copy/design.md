# Design: settings-tooltip-copy

## Context

Copy pass over the static tooltips of the Settings UI, plus one approved `agent-ui` spec
relaxation. The goal is brief, plain, consistent tooltips. Nothing behavioral may change;
`AutomationProperties.Name` values are byte-identical before and after.

History in the working tree (all verified by `git status --porcelain` and `git diff` on
2026-10-26, HEAD = `3f0dc55 docs(readme): refresh screenshots and add capture harness`):

1. **Pass 1 (applied):** the original plan's twelve XAML sites — including the two Agents header
   tooltips with `; no update check` / `; no re-detect` clauses — were edited, `CHANGELOG.md`
   got one "Changed" line, build and the full test suite passed (1176 passed, 0 failed, recorded
   in tasks.md 6.3), and `graphify update .` ran. Modified files still pending commit:
   `src/CLIHub/Views/SettingsAgentsPage.xaml`, `src/CLIHub/Views/SettingsWindow.xaml`,
   `src/CLIHub/Views/SettingsAppearancePage.xaml`, `CHANGELOG.md`.
2. **User decision (2026-10-26, after reviewing pass 1):** the three CLI Agents action tooltips
   take their shortest plain form, exactly equal to the actions' own automation names, with no
   negative clauses; the `agent-ui` disambiguation sentence is relaxed accordingly. This design
   revision carries that decision.

Original census (at commit 4939441): every `Settings*.xaml` file read, `ToolTip` grepped across
`src/CLIHub/Views/Settings*.xaml` (35 hits, all reproduced below), `tests/` and `docs/` grepped
for pinned copies (zero hits). The census below shows the operative **before** = current working
tree (pass-1 state); the HEAD originals for the three revised sites are noted in the wording
table.

## Verified current state

### Census — every literal tooltip that CHANGES (12 XAML sites, 11 strings, vs HEAD)

| # | File:Line | Attribute | Control | Before (working tree) | After (final) |
|---|---|---|---|---|---|
| 1 | `src/CLIHub/Views/SettingsAgentsPage.xaml:138` | `ToolTip` | Header "Detect" button (`AgentsState.DetectCommand`, automation name "Detect installed agents and versions") | Re-detect installed agents and versions; no update check | Detect installed agents and versions |
| 2 | `src/CLIHub/Views/SettingsAgentsPage.xaml:154` | `ToolTip` | Header "Check updates" button (`AgentsState.CheckUpdatesCommand`, automation name "Check for agent updates") | Check for agent updates; no re-detect | Check for agent updates |
| 3 | `src/CLIHub/Views/SettingsAgentsPage.xaml:212` | `ToolTip` | Per-card update-available chip (Border, automation name "Update available") | A newer version is available from this agent's distribution channel (HEAD) | A newer version is available (already applied in pass 1; untouched by the user decision) |
| 4 | `src/CLIHub/Views/SettingsAgentsPage.xaml:242` | `ToolTip` | Per-card Detect icon button (automation name "Detect agent") | Detect installation and version; no update check | Detect installation and version |
| 5 | `src/CLIHub/Views/SettingsWindow.xaml:163` | `ToolTip` | Project Scanning header add-root button (automation name "Add Directory") | Add a watched root directory (HEAD) | Add a watched root (applied in pass 1) |
| 6 | `src/CLIHub/Views/SettingsWindow.xaml:174` | `ToolTip` | Project Scanning header rescan button (automation name "Scan Now") | Rescan every watched folder (HEAD) | Rescan every watched root (applied in pass 1) |
| 7 | `src/CLIHub/Views/SettingsWindow.xaml:186` | `DetailToolTip` | `ProjectsSummaryCards` PROJECTS card (`Detail={Binding ProjectMixSummary}`) | Projects found by scanning plus projects added manually (HEAD) | Found by scanning or added manually (applied) |
| 8 | `src/CLIHub/Views/SettingsWindow.xaml:209` | `DetailToolTip` | `RegistrySummaryCards` PROJECTS card (`Detail={Binding ProjectRegistry.ProjectMixSummary}`) | same as #7 (HEAD) | same as #7 (applied) |
| 9 | `src/CLIHub/Views/SettingsWindow.xaml:207` | `AlertToolTip` | `RegistrySummaryCards` PROJECTS card alert (`Alert={Binding ProjectRegistry.FolderStatusSummary}`) | Projects whose folder no longer exists (HEAD) | Projects with missing folders (applied) |
| 10 | `src/CLIHub/Views/SettingsWindow.xaml:231` | `DetailToolTip` | `AgentsSummaryCards` AGENTS card (`Detail={Binding AgentsState.AvailabilitySummary}`) | Installed agents and the number of supported plugins (HEAD) | Installed agents and supported plugin count (applied) |
| 11 | `src/CLIHub/Views/SettingsWindow.xaml:244` | `DetailToolTip` | `AgentsSummaryCards` DETECTION card (`Detail={Binding ProbePolicyDetail}`) | Effective detection cache and version-check timeout (HEAD) | Effective cache and version-check timeout (applied) |
| 12 | `src/CLIHub/Views/SettingsAppearancePage.xaml:204` | `ToolTip` | Custom accent color TextBox (visible when `SelectedAccent.IsCustom`, label "Custom color", card title "Accent color") | Enter a custom accent color as #RRGGBB (HEAD) | Custom accent color as #RRGGBB (applied) |

Together with the MODIFIED `agent-ui` requirement this is 13 edited sites / 12 changed strings.

### Census — every literal tooltip deliberately LEFT UNCHANGED (15)

| File:Line | Literal | Reason |
|---|---|---|
| `src/CLIHub/Views/SettingsWindow.xaml:134` | Check for updates | Three words; matches the button's automation name exactly. |
| `src/CLIHub/Views/SettingsWindow.xaml:148` | Open What's New | Three words; names the window it opens. |
| `src/CLIHub/Views/SettingsWindow.xaml:295` | Maximize | One word (explicitly out per scope decision). |
| `src/CLIHub/Views/SettingsWindow.xaml:311` | Close | One word (explicitly out per scope decision). |
| `src/CLIHub/Views/SettingsAgentsPage.xaml:254` | Run the agent's update command | Five plain words; states exactly what the button does. |
| `src/CLIHub/Views/SettingsAgentsPage.xaml:266` | Open the agent's plugin.json | Five words; names the concrete file opened (`preferences-ui` spec scenario "Edit config opens plugin.json"). |
| `src/CLIHub/Views/SettingsProjectsPage.xaml:73` | Scan depth for this folder | Short; "for this folder" distinguishes the per-row chip from the global "Repository scan depth" default below — dropping it would blur that distinction. |
| `src/CLIHub/Views/SettingsProjectsPage.xaml:78` | What counts as a repository in this folder | Plainest possible explanation of the discovery chip; deliberately parallel with line 73's "…in this folder" phrasing. |
| `src/CLIHub/Views/SettingsProjectsPage.xaml:95` | Rescan this folder only | Short; "only" carries the contrast with the header rescan (see `preferences-ui` "Watched directories section", `openspec/specs/preferences-ui/spec.md:361`). |
| `src/CLIHub/Views/SettingsProjectsPage.xaml:107` | Remove watched root | Already the glossary vocabulary ("Watched root", `docs/glossary.md:40`). |
| `src/CLIHub/Views/SettingsProjectsPage.xaml:236` | Remove pattern | Two words (explicitly out per scope decision). |
| `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:183` | Open project folder | Three words; matches its automation name. |
| `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:195` | Remove project | Two words (explicitly out per scope decision). |
| `src/CLIHub/Views/SettingsLogsPage.xaml:75` | Copy path | Two words (explicitly out per scope decision). |
| `src/CLIHub/Views/SettingsLogsPage.xaml:82` | Open log folder | Three words. |

### Census — binding-driven tooltips LEFT UNTOUCHED (7)

Not copy; pinned as-is by scope:

- `src/CLIHub/Views/SettingsWindow.xaml:121` — `ToolTip="{Binding UpdateStatusDetail}"` (Updates status chip)
- `src/CLIHub/Views/SettingsWindow.xaml:197` — `DetailToolTip="{Binding WatcherDetailText}"` (DAEMON card)
- `src/CLIHub/Views/SettingsWindow.xaml:241` — `ValueToolTip="{Binding AgentsState.DefaultAgentName}"` (DEFAULT card)
- `src/CLIHub/Views/SettingsAppearancePage.xaml:163` — `ToolTip="{Binding Name}"` (accent option name)
- `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:149` — `ToolTip="{Binding Name}"` (project name)
- `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:155` — `ToolTip="{Binding SourceLabel}"` (provenance mark)
- `src/CLIHub/Views/SettingsProjectRegistryPage.xaml:168` — `ToolTip="{Binding DisplayPath}"` (project path); same pattern at `src/CLIHub/Views/SettingsProjectsPage.xaml:65` (`ToolTip="{Binding Path}"`).

### Pages with no tooltip at all

`SettingsGeneralPage.xaml`, `SettingsHotkeysPage.xaml`, `SettingsTerminalPage.xaml`,
`SettingsUpdatesPage.xaml`, `SettingsPlaceholderPage.xaml` — zero `ToolTip` occurrences (verified
by grep over `Settings*.xaml`); nothing to do there.

## Wording table (before → after)

"Before" is the current working tree. For rows 1, 2, 4 the HEAD original (pre-pass-1) is noted for
traceability; rows 5–12 were applied verbatim in pass 1 and are final.

| # | Before (chars) | After (chars) | Rationale |
|---|---|---|---|
| 1 | Re-detect installed agents and versions; no update check (55) — HEAD original: `Re-detect installed agents and their versions locally; does not check for updates` (78) | Detect installed agents and versions (37) | User decision (2026-10-26). The tooltip becomes exactly the button's automation name — shortest plain statement of its own work. "Re-detect" vs "Detect" adds nothing for the user; the local-only semantics stay pinned by the requirement's behavioral sentences. |
| 2 | Check for agent updates; no re-detect (38) — HEAD original: `Force a fresh update-availability check for installed agents; does not re-detect installation or version` (103) | Check for agent updates (23) | User decision. Exactly the automation name; the cache-bypass ("force fresh") semantics stay pinned by the requirement's behavioral sentences and the "Check updates bypasses the cached availability" scenario. |
| 3 | A newer version is available from this agent's distribution channel (65, HEAD) | A newer version is available (28) | The chip sits on the agent's own card; the channel is noise. Not an action, so no disambiguation clause. (Applied in pass 1; the user decision did not touch it.) |
| 4 | Detect installation and version; no update check (49) — HEAD original: `Detect installation and version; does not check for updates` (60) | Detect installation and version (31) | User decision; same rationale as #1. The per-card Detect is not a header action, so no requirement ever demanded a negative clause here — the clause was copied from the header pair for symmetry. |
| 5 | Add a watched root directory (29, HEAD) | Add a watched root (18) | "Watched root" is the glossary term (`docs/glossary.md:40`) and the Projects page's own automation-name/tooltip vocabulary ("Remove watched root", "Rescan this watched root"); "root directory" doubles the noun. |
| 6 | Rescan every watched folder (27, HEAD) | Rescan every watched root (25) | Consistency only: "watched folder" appears nowhere else in UI copy or docs. Keeps the every-vs-this-folder contrast with `SettingsProjectsPage.xaml:95`. |
| 7/8 | Projects found by scanning plus projects added manually (55, HEAD) | Found by scanning or added manually (35) | The card label already reads PROJECTS; the tooltip explains the mix, and "plus" is stiffer than "or". Same string at both sites (`SettingsWindow.xaml:186` and `:209`) so the two PROJECTS cards stay identical. |
| 9 | Projects whose folder no longer exists (38, HEAD) | Projects with missing folders (29) | Plainer; echoes the visible chip "Folder not found" (`SettingsProjectRegistryPage.xaml:160`). |
| 10 | Installed agents and the number of supported plugins (52, HEAD) | Installed agents and supported plugin count (43) | Keeps both facts; drops the wordy "the number of". "count" retained so the tooltip still says the detail is a number. |
| 11 | Effective detection cache and version-check timeout (51, HEAD) | Effective cache and version-check timeout (42) | The card label is DETECTION, so "detection" is redundant in the tooltip. "Effective" is kept — it is the load-bearing word (resolved values, defaults applied when the fields are empty). |
| 12 | Enter a custom accent color as #RRGGBB (38, HEAD) | Custom accent color as #RRGGBB (30) | Drops the imperative "Enter a"; the result reads as a format hint and merges the visible card title "Accent color" (`SettingsAppearancePage.xaml:105`) with the field's own placeholder `#RRGGBB` (`:202`). |

All after-strings: sentence case, no trailing period, no jargon beyond terms the visible UI already
uses.

## agent-ui requirement after the relaxation

Requirement "Agent management header actions" (`openspec/specs/agent-ui/spec.md:394-396`) and
scenario "Tooltips disambiguate the header actions" (`:439-442`) currently demand the negative
clause. The user approved relaxing both, in this change's delta
(`specs/agent-ui/spec.md`, one `## MODIFIED Requirements` block):

- Requirement sentence: "Each header action SHALL expose a tooltip and automation name that state
  its own work **and distinguish it from the other header action**." (replaces "…and that the
  other header action's work is not performed.").
- Scenario THEN: "its tooltip and automation name state only that action's work, **using wording
  that distinguishes it from the other header action**" (replaces "…and state that the other
  header action's work is not performed by it").
- Everything else byte-identical: requirement name, the behavioral sentences (local-only Detect,
  cache-bypassing Check updates, mutual exclusion, load-time pass), and all eight scenario names
  and bodies.

Check of the final tooltips against the relaxed requirement:

- "Detect installed agents and versions" — states only its own work; clearly names the detect
  action; distinguishable from "Check for agent updates" (one detects installed versions, the
  other checks for updates). Satisfied.
- "Check for agent updates" — states only its own work; distinguishable from the Detect wording.
  Satisfied. Also byte-identical to its automation name, as the scenario reads on "tooltip and
  automation name" jointly.

This also resolves the pre-existing gap recorded in the first revision of this design: the header
automation names ("Detect installed agents and versions" `SettingsAgentsPage.xaml:134`, "Check for
agent updates" `:150`) never carried the negative clause the old sentence demanded of them, so the
automation-name half of the old scenario was unmet in code. Under the relaxed wording both
automation names state their own work and are mutually distinguishable — the scenario becomes true
of the code as shipped, with no automation-name edit.

## Decisions

- **D1 — Header tooltips equal their automation names verbatim (user decision).** "Detect
  installed agents and versions" / "Check for agent updates" are the shortest plain statements of
  each action's own work and are maximally consistent between tooltip and screen reader.
  Alternative considered and superseded: the pass-1 `; no update check` / `; no re-detect` clauses
  (kept the old requirement true but were still the longest tooltips in Settings).
- **D2 — Relax the `agent-ui` requirement instead of parking the mismatch.** The original brief
  said flag any wording that would violate the spec; the user chose to change the spec itself, so
  the change carries one MODIFIED requirement rather than a parked follow-up. Alternative
  rejected: REMOVED + ADDED pair (the archived `2026-10-26-agent-detect-update-actions` notes
  validate rejects MODIFIED blocks that rename scenarios; here nothing renames, so the lighter
  MODIFIED flow is correct).
- **D3 — "watched root" everywhere (#5, #6).** The glossary defines "Watched root"
  (`docs/glossary.md:40`); the Projects page automation names and tooltips already use it; only
  these two Settings header tooltips said "root directory"/"watched folder". The visible section
  header "WATCHED REPOSITORY DIRECTORIES" (`SettingsProjectsPage.xaml:12`) stays as-is (not a
  tooltip, out of scope). #6 is a consistency edit, not a length edit — still flagged in Open
  questions.
- **D4 — Leave all four Projects page literal tooltips unchanged.** All are short, plain, and
  mutually consistent; the trailing scope phrases ("for this folder" / "in this folder") carry real
  information (per-row override vs global default, pinned by `preferences-ui:361` and the per-root
  scenarios in `project-scanning`).
- **D5 — Keep "Effective" in #11, drop "detection".** The DETECTION card's detail line shows
  resolved values (defaults when the fields are empty); "Effective" is the plainest one-word way
  to say that. "detection" is restated by the card label directly above.
- **D6 — Summary-card tooltips (#7–#11) stay explanatory phrases, not fragments.** They explain
  what the card's Detail/Alert line means; pure fragments ("Scanned + manual") were rejected as
  cryptic for a first-time reader.
- **D7 — No test or docs edits.** Grep of `tests/` for the tooltip vocabulary returns zero hits;
  `docs/` quotes none of these strings (the only "distribution channel" hit is
  `docs/architecture/plugins.md:29`, about the descriptor field, unrelated). The one-line
  `CHANGELOG.md` "Changed" entry was added by pass 1 and stays; it is amended only to mention the
  spec relaxation.
- **D8 — Spec delta = exactly one MODIFIED requirement, derived mechanically.** The delta file was
  generated from the main spec block (`openspec/specs/agent-ui/spec.md:394-442`) by script: the
  extracted 49-line block was verified to start at the requirement heading, each replaced string
  was verified to occur exactly once, and the result was verified to differ from the source in
  exactly two lines (the sentence and the THEN). This guarantees "everything else
  byte-identical" instead of relying on transcription. `skip_specs: true` was removed from
  `.openspec.yaml`.

## Spec delta decision

One `## MODIFIED Requirements` block for `agent-ui` "Agent management header actions" in
`specs/agent-ui/spec.md` of this change (content per the section above). Verified:

- Requirement name matches the main spec byte-for-byte, so the archive merge replaces the block
  in place; all eight scenario names are byte-identical (validated: `openspec validate --all`
  passes with the delta present, and the archived `2026-10-26-agent-detect-update-actions` delta
  documents that validate rejects MODIFIED blocks that drop or rename scenarios).
- No other requirement or scenario anywhere changes. `launch-window-theme:51-65` (launch-window
  tooltip styling/timing) is untouched; `preferences-ui:361,474,560,575` pin behavior and labels,
  not tooltip copy; `settings-theme` contains no tooltip wording (a grep for "tooltip" across
  `openspec/specs/` matches only `launch-window-theme` and `agent-ui`).
- `.openspec.yaml` keeps `schema: spec-driven` and `created: 2026-10-26` with no `skip_specs`.

## Deviations from the brief

1. **Working-tree premise in the revision brief does not match the repository.** The brief said
   the tree contains unrelated README-screenshot work (`App.xaml.cs`, `docs/images`,
   `docs/backlog.md`, `src/CLIHub/Views/ReadmeImageCapture.cs`) to leave alone. `git status
   --porcelain` shows none of those files pending: that work is already committed as `3f0dc55
   docs(readme): refresh screenshots and add capture harness`. The tree is indeed not clean, but
   the pending modifications are this change's own pass-1 files (three Settings XAML files +
   `CHANGELOG.md`) plus the untracked change directory. Nothing unrelated was touched; the
   caution was honored vacuously.
2. **The first design's automation-name deviation is closed by the user decision, not parked.**
   The original brief (and the first revision of this design) treated the automation-name half of
   `agent-ui:442` as a pre-existing gap to flag and leave alone. The user instead approved
   relaxing the requirement, so the gap is resolved inside this change (see the check above); the
   old open question 1 is withdrawn.
3. **Two of the original brief's listed "wording direction" strings remain unchanged**
   (`Rescan this folder only`, `Remove watched root`, along with the other Projects page
   literals) per the brief's own "do not churn text that is already short" rule; recorded because
   the strings appeared in the wording-direction list.
4. **#6 (`Rescan every watched folder` → `Rescan every watched root`) is consistency-motivated,
   not length-motivated** (27 → 25 characters). The goal statement includes "consistent", and the
   glossary fixes the vocabulary, so it stays in scope; flagged because a strict "shorten only"
   reading would leave it alone.

## Open questions

1. **Keep #6?** If the maintainer prefers zero churn on already-short strings, drop the
   `Rescan every watched folder` → `Rescan every watched root` edit (already applied in pass 1);
   nothing else depends on it.
2. **CHANGELOG wording.** The pass-1 "Changed" line says "copy-only string edits with no behavior
   change". After the revision the change also relaxes one `agent-ui` requirement sentence; task
   6.5 amends the line to mention it (default), or the maintainer keeps the line code-scoped.
