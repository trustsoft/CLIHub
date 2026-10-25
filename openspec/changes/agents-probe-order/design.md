# Design: agents-probe-order

## Current state (verified in code)

Working tree clean at commit `3ca3ce1`. All line numbers verified by reading the files.

| Concern | Today | File |
|---|---|---|
| Page root | The page is a `UserControl` whose content is one root `StackPanel`; it has no page-title/description of its own (those live in the pinned page header in `SettingsWindow`) | `src/CLIHub/Views/SettingsAgentsPage.xaml:68` (resources at `:10-67` are page-local chip/button styles) |
| Block 1 — MANAGEMENT | `SectionHeader Text="MANAGEMENT"` then the "Default agent" `SettingCard` (ComboBox bound to `AgentsState.DefaultAgentOptions` / `SelectedDefaultAgent`) | `src/CLIHub/Views/SettingsAgentsPage.xaml:69-94` |
| Block 2 — AGENTS header | A `Grid` holding `SectionHeader Text="AGENTS"` plus the right-aligned "Refresh" chip button (`AgentsState.DetectCommand`) | `src/CLIHub/Views/SettingsAgentsPage.xaml:96-117` (header `:97`, button `:98-116`) |
| Block 3 — agent list | `ItemsControl ItemsSource="{Binding AgentsState.Cards}"`; per-agent card with logo, name, state/update chips, test/update/edit-config buttons | `src/CLIHub/Views/SettingsAgentsPage.xaml:118-225` |
| Block 4 — PROBE | `SectionHeader Text="PROBE"` then a `SettingsGroup` with two `Grouped="True"` cards: "Probe TTL" (`ProbeTtlText` / `ProbeTtlWatermark`, unit `min`) and "Probe timeout" (`ProbeTimeoutText` / `ProbeTimeoutWatermark`, unit `sec`) | `src/CLIHub/Views/SettingsAgentsPage.xaml:227-250` (header `:227`, group `:228-250`, TTL `:229-238`, timeout `:240-249`) |
| Desired order | MANAGEMENT → PROBE → AGENTS, i.e. move block 4 between block 1 and block 2 | — |
| Page hosting | The page is embedded exactly once, selected by the `agents` page key | `src/CLIHub/Views/SettingsWindow.xaml:471` |
| Pinned summary row (out of scope) | `AgentsSummaryCards` template: AGENTS card, DEFAULT card, PROBE card — must not move | `src/CLIHub/Views/SettingsWindow.xaml:233-256` (labels `:239`, `:245`, `:253`) |
| Tests pinning the layout | None: no reference to `SettingsAgentsPage`, `SectionHeader`, or the section labels anywhere under `tests/` | grep over `tests/` (0 matches) |

## The exact move

Cut `src/CLIHub/Views/SettingsAgentsPage.xaml:227-250` — the `<controls:SectionHeader Text="PROBE"/>`
line through the closing `</controls:SettingsGroup>` — and paste it verbatim between line 94
(MANAGEMENT block's closing `</controls:SettingCard>`) and line 96 (the AGENTS block's opening
`<Grid>`), preserving exactly one blank line between adjacent blocks (the file currently uses one
blank line at `:95` and `:226`).

Resulting child order of the root `StackPanel`:

1. `SectionHeader Text="MANAGEMENT"` + Default agent `SettingCard` (unchanged, `:69-94` today)
2. `SectionHeader Text="PROBE"` + `SettingsGroup` with Probe TTL and Probe timeout (moved, verbatim)
3. `Grid` with `SectionHeader Text="AGENTS"` + Refresh button (unchanged, `:96-117` today)
4. `ItemsControl` of agent cards (unchanged, `:118-225` today)

Mechanical constraints for the implementer:

- The move is a cut-and-paste of whole elements; the pasted block must be byte-identical to the cut
  block (same indentation once placed at StackPanel child level, same attribute order, same
  comments — the block carries no comments today).
- Nothing in `UserControl.Resources` (`:10-67`) and no other element is touched; the reviewable diff
  should be recognizable as a pure block move.

## Spec analysis (why `skip_specs: true`)

- `openspec/specs/settings-theme/spec.md:195-198` — *Cards on the data pages* → scenario
  **CLI Agents page cards**: "**WHEN** the CLI Agents page is shown / **THEN** the default-agent
  selector and the agents-probe fields appear as cards under the page description and section
  headers, and the agent list follows under the management section".
- History of that sentence (verified with `git log -L`): introduced verbatim by commit `f3a9161`
  (archived `2026-10-11-settings-data-pages-cards`) and never edited since. At that commit the page
  was MANAGEMENT → default-agent card → a plain "Agents" field label → agent list → PROBE; the
  dedicated `SectionHeader Text="AGENTS"` was added later by commit `c6d5e3f` ("feat(settings):
  polish the agents list header", XAML-only, no spec edit), producing today's order.
- Reading: the sentence names the default-agent selector and the agents-probe fields as the cards
  and says "the agent list follows". It does not pin the AGENTS-vs-PROBE relative order, and the
  reordered page (probe cards before the list, list following them) satisfies it at least as well as
  today's order does. The section-header requirement it sits under (`settings-theme:165-179`)
  mandates only that each area gets an uppercase header — both orders comply.
- The one order this page's spec does pin is the pinned summary row:
  `openspec/specs/settings-theme/spec.md:343-350` (*CLI Agents page summary cards*, scenario
  **Cards shown**: "the page header shows the 'AGENTS', 'DEFAULT', and 'PROBE' cards"). That row is
  in `SettingsWindow.xaml`, not in this page, and is explicitly out of scope.

Decision: the reorder changes no requirement, scenario outcome, or observable control behavior — it
is a presentational fix inside one page, so the change declares `skip_specs: true` and creates no
`specs/` delta, following the convention of e.g.
`openspec/changes/archive/2026-10-20-fix-settings-open-delay/.openspec.yaml`.

## Decisions

- **D1 — Move PROBE up, not AGENTS down.** Both produce the same final order, but moving the
  24-line PROBE block (`:227-250`) produces a smaller, easier-to-review diff than moving the
  130-line AGENTS block (`:96-225`), and it keeps the diff anchored between two stable neighbors
  (the Default agent card's close and the AGENTS `Grid`'s open).
- **D2 — Move the SectionHeader together with the SettingsGroup.** The header and the group are one
  visual section; moving the group alone would orphan the "PROBE" header above the agent list.
  Both travel as a unit.
- **D3 — Accept the tab-order consequence.** WPF focus order in a `StackPanel` follows logical
  (visual) order, so keyboard users reach the probe fields before the agent-card buttons after the
  move. For a visual reorder, focus order matching visual order is the desired outcome; no
  `TabIndex` overrides are introduced.
- **D4 — No spec delta (see Spec analysis).** The scenario at `settings-theme:198` tolerates the new
  order; only a maintainer-requested wording tightening would justify a spec edit, and per repo
  precedent (`2026-10-25-spec-wording-cosmetics`) wording cosmetics ride their own change.

## Alternatives considered and rejected

- **Move the AGENTS block below PROBE instead:** same end state, larger diff, two moved units
  (header `Grid` + `ItemsControl`) instead of one — rejected for review clarity (D1).
- **Reorder via new panel structure (e.g., wrap sections in a `StackPanel` per section or use a
  `Grid` with rows):** changes layout structure and XAML shape beyond the requested reorder for no
  benefit — rejected; the repository rule is to use existing patterns and keep the change minimal.
- **Add a spec delta pinning MANAGEMENT → PROBE → AGENTS:** rejected — the existing scenario already
  covers the page's card composition, and pinning visual section order that no requirement depends
  on would add churn without a behavior contract. Recorded as open question 2 in case the
  maintainer disagrees.
- **Also adjust the pinned summary row to match:** explicitly out of scope; `settings-theme:343-350`
  mandates the AGENTS/DEFAULT/PROBE order and the two surfaces serve different purposes (status at a
  glance vs. page content).

## Deviations

- None from the brief. The brief's verified line ranges match the file exactly (MANAGEMENT `~69-94`,
  AGENTS `~96-225`, PROBE `~227-250`).

## Open questions

1. **CHANGELOG entry?** Recent small UI changes did get one-line "Changed" entries under the current
   release heading. Add one ("The CLI Agents page shows the probe settings above the agent list") or
   skip for a pure reorder — follow the maintainer's convention at implementation time; the latest
   heading today is `## 1.0.1 — 2026-10-24`.
2. **Spec wording follow-up?** `settings-theme:198`'s clause "and the agent list follows under the
   management section" predates the dedicated AGENTS section header and is loose about the order.
   After this change the sentence reads fine, but if the maintainer wants it to name the
   PROBE-before-AGENTS order explicitly, schedule it as a separate wording change (D4) rather than
   reopening this one.
3. **Smoke depth.** The visual check (task 3.1) is the only gate that can actually observe the
   reorder; confirm it should remain the commit/archive blocker per repo smoke policy, or whether
   the maintainer accepts a screenshot/description from the implementer.
