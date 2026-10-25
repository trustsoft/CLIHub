# Move the PROBE block above the AGENTS block on the Settings → CLI Agents page

## Why

On the Settings → CLI Agents page, the two probe policy fields (Probe TTL, Probe timeout) sit below
the agent list. The probe policy governs how every card in that list obtains its installed/version
state, yet it reads as a footer afterthought: the user scrolls past the whole agent list to reach it.
The user asked for the PROBE block to appear before the AGENTS block, so the page reads
MANAGEMENT → PROBE → AGENTS.

The reorder also matches the governing spec text more closely: the `settings-theme` scenario
*CLI Agents page cards* (`openspec/specs/settings-theme/spec.md:195-198`) says "the default-agent
selector and the agents-probe fields appear as cards under the page description and section headers,
and the agent list follows" — a sentence that lists the probe fields ahead of "the agent list
follows". The current XAML order (AGENTS before PROBE) does not match that reading; the reordered
page does. No requirement pins the opposite order, so this is a presentation fix, not a behavior
change.

## What Changes

A single, verbatim block move in one file, `src/CLIHub/Views/SettingsAgentsPage.xaml`:

- The PROBE block — `<controls:SectionHeader Text="PROBE"/>` (line 227) plus the `SettingsGroup`
  holding the Probe TTL and Probe timeout `SettingCard`s (lines 228-250) — moves up so it sits
  between the MANAGEMENT block (lines 69-94) and the AGENTS block (the header/Refresh `Grid` at
  lines 96-117 followed by the agent-card `ItemsControl` at lines 118-225).
- Resulting order in the page's root `StackPanel` (line 68): **MANAGEMENT** (Default agent) →
  **PROBE** (Probe TTL, Probe timeout) → **AGENTS** (header + Refresh + agent cards).
- Nothing inside any block changes: no XAML property, style, binding, resource, text, or comment
  edits; no code-behind, ViewModel, or behavior changes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a presentational reorder inside one page:

- `openspec/specs/settings-theme/spec.md:195-198` (*Cards on the data pages* → *CLI Agents page
  cards*) does not pin the relative order of the AGENTS and PROBE sections, and the reordered page
  stays within its wording (see design, "Spec analysis").
- The only order this spec does pin for the page is the pinned summary-card row — "the page header
  shows the 'AGENTS', 'DEFAULT', and 'PROBE' cards" (`openspec/specs/settings-theme/spec.md:343-350`,
  rendered by `AgentsSummaryCards` in `src/CLIHub/Views/SettingsWindow.xaml:233-256`). That row is
  out of scope and does not move.

Therefore the change opts out of specs (`skip_specs: true` in `.openspec.yaml`), following the
convention of e.g. `openspec/changes/archive/2026-10-20-fix-settings-open-delay/`.

## Impact

- Code: `src/CLIHub/Views/SettingsAgentsPage.xaml` only — element order inside the root
  `StackPanel`. The page is hosted once, at `src/CLIHub/Views/SettingsWindow.xaml:471`; no other
  view or control is affected.
- Keyboard/tabbable focus: the logical focus order follows the new visual order (probe fields are
  reached before the agent-card buttons). This is the expected consequence of a visual reorder, not
  a behavior change.
- Tests: none exist for this XAML (no reference to `SettingsAgentsPage`, `SectionHeader`, or the
  section labels anywhere under `tests/`); no test changes.
- Docs: at most a one-line `CHANGELOG.md` "Changed" entry (open question 1 in design).
- Specs: none (see Capabilities).
- Risk: minimal — a cut-and-paste of well-formed XAML blocks; build and XAML compilation catch any
  malformation.

## Non-goals

- No change to the pinned summary-card row in `SettingsWindow.xaml` (AGENTS, DEFAULT, PROBE order is
  mandated by `settings-theme` and stays as is).
- No XAML property, style, binding, resource, or text changes; the probe fields remain grouped rows
  (`Grouped="True"`) on their shared `SettingsGroup` surface.
- No ViewModel, code-behind, command, or behavior changes.
- No spec wording edits. If the maintainer wants `settings-theme:198` to name the
  MANAGEMENT → PROBE → AGENTS order explicitly, that cosmetic edit belongs in a separate
  spec-wording change (precedent: `2026-10-25-spec-wording-cosmetics`), not here.
- No changes to other settings pages, the sidebar navigation order, or page hosting.

## Dependencies

- None on other active changes (`openspec/changes/` holds no other active change).
- No conflict with archived history: the AGENTS section header was added by commit `c6d5e3f`
  ("feat(settings): polish the agents list header", no spec edit) on top of the order described by
  archived `2026-10-11-settings-data-pages-cards`; this change only reorders blocks within the page
  that change and `2026-10-11-settings-agents-management` established.
