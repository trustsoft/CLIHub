# Design: Agent Detection page icon set

## Context

All icon glyph assignments for the Agent Detection page are currently `IconGlyphs.Diagnostic`
(E9D9, a framed pulse trace) except the page/sidebar entry, which uses `IconGlyphs.Process` (E9F3).

Verified current state with exact file:line evidence:

- `src/CLIHub/Theming/IconGlyphs.cs:24` — `Diagnostic = "\uE9D9"` (framed pulse trace; currently used in 5 places on this page)
- `src/CLIHub/Theming/IconGlyphs.cs:25` — `Process = "\uE9F3"` (dashed circle with an arrow; currently the page/sidebar icon)
- `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:82` — `agent-detection` catalog entry uses `IconGlyphs.Process` for both the 13 px sidebar glyph and the 20 px page-header emblem (both read from `SelectedPage.Icon`)
- `src/CLIHub/Views/SettingsAgentDetectionPage.xaml:38` — Cache TTL row: `Icon="{x:Static themes:IconGlyphs.Diagnostic}"`
- `src/CLIHub/Views/SettingsAgentDetectionPage.xaml:49` — Version check timeout row: `Icon="{x:Static themes:IconGlyphs.Diagnostic}"`
- `src/CLIHub/Views/SettingsWindow.xaml:238` — CACHE TTL summary card: `Icon="{x:Static themes:IconGlyphs.Diagnostic}"`
- `src/CLIHub/Views/SettingsWindow.xaml:243` — TIMEOUT summary card: `Icon="{x:Static themes:IconGlyphs.Diagnostic}"`
- `src/CLIHub/Views/SettingsWindow.xaml:249` — DEFAULTS summary card: `Icon="{x:Static themes:IconGlyphs.Diagnostic}"`

None of `Radar` (EB44), `Recent` (E823), `Stopwatch` (E916), or `UpdateRestore` (E777) are declared in
`IconGlyphs.cs`. All four must be added before the XAML references can compile.

The XAML binding mechanism is already correct: `SettingsWindow.xaml` binds the sidebar glyph to
`{Binding Icon}` and the page-header emblem to `{Binding SelectedPage.Icon}`. Changing the constant in
`SettingsNavigation.cs` propagates to both render sizes (13 px sidebar, 20 px emblem) with no XAML edit
needed for those paths. Only the row-card and summary-card XAML attributes need inline literal changes.

Two spec files have stale icon references and must be updated alongside the source:
- `openspec/specs/settings-theme/spec.md` — "Agent Detection page summary cards" requirement states
  "All three cards SHALL use the Diagnostic icon"; its "Cards shown" scenario and "Defaults card shown"
  scenario name the Diagnostic icon explicitly.
- `openspec/specs/preferences-ui/spec.md` — "Agent Detection page content" requirement's "Icon and
  description" scenario names `IconGlyphs.Process` for the sidebar; "DEFAULTS card shown" scenario names
  the Diagnostic icon for the DEFAULTS card.

No logic, configuration schema, persistence, navigation, or test changes are required. No doc update is
required: `docs/architecture/ui.md` describes the icon-binding mechanism generically and does not name
specific glyph constants for any page.

## Goals / Non-Goals

**Goals:**
- Add four new `IconGlyphs` constants: `Radar`, `Recent`, `Stopwatch`, `UpdateRestore`.
- Swap the `agent-detection` catalog entry from `Process` to `Radar`.
- Replace the two `Diagnostic` row-card icon attributes in `SettingsAgentDetectionPage.xaml` with
  `Recent` (Cache TTL row) and `Stopwatch` (Version check timeout row).
- Replace the three `Diagnostic` summary-card icon attributes in `SettingsWindow.xaml` with `Recent`
  (CACHE TTL card), `Stopwatch` (TIMEOUT card), and `UpdateRestore` (DEFAULTS card).
- Update `settings-theme` and `preferences-ui` spec deltas to match.

**Non-Goals:**
- See proposal.md — Explicit non-goals.

## Decisions

### Glyph selection: Radar (EB44) for the page/sidebar icon

The page/sidebar entry and the 20 px page-header emblem use one value (`SelectedPage.Icon`). Radar is the
only Segoe MDL2 candidate that reads unmistakably as "detection" at 20 px and remains legible as a
solid shape at 13 px in the sidebar. Verified by rendering the mockup in Chrome at both sizes.

Rejected candidates with evidence:
- `IconGlyphs.Process` (E9F3, current) — the dashed circle with an arrow reads as "processing", not "detection".
- Scan E8FE — renders as a flag at 13 px; shape is lost.
- Sensor E957 — collapses at 13 px; radial spokes disappear.
- History E81C — visually near-identical to Recent; also already the Logs page icon.

### Glyph selection: Recent (E823) for Cache TTL / CACHE TTL card

Cache TTL is "how long a result stays fresh". Recent (circular arrow with a clock) directly conveys
"time elapsed since last check". The shape is a round arc, distinct from the Stopwatch stem.

Rejected alternative: Library (E8F1) — proposed in Variant D; rejected because its book-stack shape
carries no time connotation.

### Glyph selection: Stopwatch (E916) for Version check timeout / TIMEOUT card

A timeout is a hard time boundary for one operation. Stopwatch (round with a crown/stem at top)
conveys "measuring a bounded duration". The crown distinguishes it from Recent at both 11 px and 13 px
in the rendered mockup.

Accepted trade-off: Recent and Stopwatch are both round glyphs; the distinction is the stem vs. the
arc arrow. The trade-off is accepted because tighter semantics ("cached result age" vs. "time limit
per check") outweigh maximum shape contrast.

### Glyph selection: UpdateRestore (E777) for DEFAULTS card

The DEFAULTS card shows the built-in reset values — what you get when you clear the fields. UpdateRestore
(circular undo arrow) conveys "the values you return to". Shape is distinct from both Recent and Stopwatch.

Rejected alternatives:
- SpeedHigh — collapses at 11 px (Variant B concern confirmed by mockup rendering).
- DateTime EC92 — turns to mush at 11 px (Variant C concern confirmed by mockup rendering).
- Sync E895 — already used by the Updates page icon in the same sidebar; would duplicate a sibling.
- Undo E7A7 — same undo arrow family as UpdateRestore but lacks the circular/restore shape emphasis.

### Row-card icon and summary card share one glyph (invariant)

The user's stated intent is that a setting row and its corresponding summary card use the same icon.
This pairing reinforces the visual relationship between the editable field and its live summary. The
invariant is stated in the `settings-theme` spec delta's modified requirement text and in its "Cards
shown" scenario, rather than as a separate requirement, because it is a constraint on an existing
requirement rather than a standalone capability.

### Spec coverage: two capabilities updated

The icon assignments span two specs: `settings-theme` (the summary-card requirement) and
`preferences-ui` (the sidebar icon and DEFAULTS card scenarios in "Agent Detection page content").
Both must be updated in the same change so the spec set is internally consistent after archiving.

## Risks / Trade-offs

- [New constants not yet in `IconGlyphs.cs`] All four new constants must be added before XAML
  compilation succeeds. → Add them in Stage 1 of implementation so all downstream XAML edits can
  compile in Stage 2.
- [Round-glyph pairing: Recent vs. Stopwatch] Both are round shapes; at 11 px they may look similar.
  → Accepted: semantics differ (arrow vs. crown/stem) and the rendered mockup confirmed them as
  distinguishable at 11 px.

## Open Questions

None. All glyph choices are approved by the user; no ambiguity remains that would change the spec,
approach, or task breakdown.
