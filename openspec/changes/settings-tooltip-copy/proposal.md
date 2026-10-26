# Shorten the Settings tooltip copy

## Why

Several static tooltips in the Settings UI read like implementation notes rather than UI copy:
`Force a fresh update-availability check for installed agents; does not re-detect installation or
version` (103 characters), `A newer version is available from this agent's distribution channel`,
`Effective detection cache and version-check timeout`. They are longer than the visible hints next
to them and mix vocabulary ("watched folder" on the Project Scanning header vs "watched root"
everywhere else, including the glossary). The user asked for one copy-only pass that makes every
tooltip brief, plain, and consistent — with zero behavioral, binding, automation-name, or layout
changes.

**User decision (2026-10-26, later the same day):** after reviewing the first applied pass, the
user settled the three CLI Agents action tooltips on the shortest plain form — each exactly equals
its action's automation name — with no `; no update check` / `; no re-detect` clauses, and approved
relaxing the `agent-ui` requirement accordingly: a header tooltip/automation name must state its own
work and be distinguishable from the other header action, but need not state that the other
action's work is not performed.

## What Changes

Thirteen edited sites carrying twelve changed strings: twelve static-tooltip XAML sites (eleven
distinct strings — the project-mix detail tooltip appears twice) across three files, plus one
MODIFIED `agent-ui` requirement. The XAML files: `src/CLIHub/Views/SettingsAgentsPage.xaml` (4),
`src/CLIHub/Views/SettingsWindow.xaml` (6 strings, 7 sites), and
`src/CLIHub/Views/SettingsAppearancePage.xaml` (1). Sentence case, no trailing period.

The first pass (semicolon-clause wording for the two Agents header actions) is already applied in
the working tree; the table's "After" column is the final state, incorporating the user decision.

Concrete before → after table ("before" = current working tree; evidence and rationale in
`design.md`):

| Site | Before | After |
|---|---|---|
| `SettingsAgentsPage.xaml:138` | Re-detect installed agents and versions; no update check | Detect installed agents and versions |
| `SettingsAgentsPage.xaml:154` | Check for agent updates; no re-detect | Check for agent updates |
| `SettingsAgentsPage.xaml:212` | A newer version is available from this agent's distribution channel | A newer version is available |
| `SettingsAgentsPage.xaml:242` | Detect installation and version; no update check | Detect installation and version |
| `SettingsWindow.xaml:163` | Add a watched root directory | Add a watched root |
| `SettingsWindow.xaml:174` | Rescan every watched folder | Rescan every watched root |
| `SettingsWindow.xaml:186` | Projects found by scanning plus projects added manually | Found by scanning or added manually |
| `SettingsWindow.xaml:209` | Projects found by scanning plus projects added manually | Found by scanning or added manually |
| `SettingsWindow.xaml:207` (AlertToolTip) | Projects whose folder no longer exists | Projects with missing folders |
| `SettingsWindow.xaml:231` | Installed agents and the number of supported plugins | Installed agents and supported plugin count |
| `SettingsWindow.xaml:244` | Effective detection cache and version-check timeout | Effective cache and version-check timeout |
| `SettingsAppearancePage.xaml:204` | Enter a custom accent color as #RRGGBB | Custom accent color as #RRGGBB |

Spec: one MODIFIED requirement — `agent-ui` "Agent management header actions". The disambiguation
sentence becomes "Each header action SHALL expose a tooltip and automation name that state its own
work and distinguish it from the other header action." (was: "…and that the other header action's
work is not performed."), and the scenario "Tooltips disambiguate the header actions" THEN becomes
"its tooltip and automation name state only that action's work, using wording that distinguishes it
from the other header action". Every other word of the requirement prose and all eight scenario
names and bodies are byte-identical — the delta was derived mechanically from
`openspec/specs/agent-ui/spec.md:394-442` and verified to differ in exactly two lines. This also
resolves the pre-existing gap where the header automation names never carried the negative clause
the old sentence demanded.

Every other tooltip in scope is deliberately left byte-identical — the full census (changed, left
unchanged with reason, and binding-driven) is in `design.md`. `AutomationProperties.Name` values
are byte-identical before and after; only `ToolTip` / `DetailToolTip` / `AlertToolTip` literal
values change.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `agent-ui` — *Agent management header actions*: the tooltip/automation-name disambiguation
  sentence is relaxed from "state its own work **and that the other header action's work is not
  performed**" to "state its own work **and distinguish it from the other header action**"; the
  matching scenario THEN follows ("state only that action's work, using wording that distinguishes
  it from the other header action"). Requirement name, all eight scenario names, and every other
  WHEN/THEN are unchanged. The behavioral sentences (local-only Detect, cache-bypassing Check
  updates, mutual exclusion, load-time pass) are untouched.

No other requirement reads on this copy: `launch-window-theme` "Themed tooltips"
(`openspec/specs/launch-window-theme/spec.md:51-65`) is about launch-window tooltip styling and is
untouched.

## Impact

- Code: the twelve tooltip attribute values listed above, in three XAML files (nine sites already
  applied in the working tree by the first pass; the three CLI Agents sites get the user-decided
  final strings). No C# file changes.
- Tests: none — no test pins any of these strings (`tests/` grep for the tooltip vocabulary returns
  zero hits; the first pass ran the full suite green: 1176 passed, 0 failed).
- Docs: none quote these strings; the `CHANGELOG.md` "Changed" line added by the first pass stays
  accurate for the code change and is amended to mention the spec relaxation.
- Specs: one MODIFIED `agent-ui` requirement in this change; merges at archive (with explicit user
  approval) by replacing the requirement block in place.
- Risk: minimal — copy-only string edits plus one spec sentence; XAML compilation catches malformed
  attributes, `openspec validate --all` guards the delta shape, and the diff gate confirms no
  command, binding, style, layout, or automation-name line moved.

## Non-goals

- No behavioral change of any kind: commands, bindings, enable-states, styles, icons, layout,
  grouping, ordering, and automation names are byte-identical. This includes every behavioral
  sentence of the modified `agent-ui` requirement (local-only Detect, cache bypass, mutual
  exclusion, busy states, summary line, load-time pass).
- No changes to data-driven tooltips (`{Binding ...}` values such as `UpdateStatusDetail`,
  `WatcherDetailText`, `AgentsState.DefaultAgentName`, `DisplayPath`, `Name`, `SourceLabel`,
  `Path`, `ValueToolTip` bindings) — they are correct as-is and are not copy.
- No changes to tooltips that are already short and plain (`Maximize`, `Close`, `Copy path`,
  `Open log folder`, `Open project folder`, `Remove project`, `Remove pattern`, `Remove watched
  root`, `Rescan this folder only`, `Scan depth for this folder`, `What counts as a repository in
  this folder`, `Check for updates`, `Open What's New`, `Run the agent's update command`, `Open the
  agent's plugin.json`).
- No changes outside Settings: `LaunchWindow.xaml`, `WhatsNewWindow.xaml`, the tray, and every
  other surface are untouched.
- No other spec edits: only the one `agent-ui` requirement's disambiguation sentence and its
  scenario THEN; no requirement or scenario renames, no other capability touched.
- No localization framework, no tooltip styling/timing changes, no new tooltips, no removal of
  tooltips.

## Dependencies

None. There are no active changes under `openspec/changes/` besides this one. The touched strings
and the modified requirement were last set by archived `2026-10-26-agent-detect-update-actions`
(Agents header actions and their requirement) and `2026-10-25-agents-detection-wording` (detection
vocabulary); this change revises both outcomes deliberately, and the MODIFIED flow preserves the
rest of that requirement verbatim.
