## Context

See `proposal.md` — Why. `settings-page-cards` added the `SettingCard` (`Title`, `Hint`, `Icon`, `Badge`, `BadgeTone`, `Placement`, `Grouped`), `SettingsGroup`, and `SectionHeader` primitives and their styles, and converted the six simpler pages. `SettingsProjectsPage.xaml` and `SettingsAgentsPage.xaml` still use the old `FieldLabel` + control + `HintText` stacks; the Agents page also has a custom per-agent card (`Border` + logo `Image` + details + actions) and the probe fields.

## Goals / Non-Goals

**Goals:**

- Projects & Paths and CLI Agents use the same card/description/section vocabulary as the other pages.
- List editors and the agent list are presented consistently without behavior changes.

**Non-Goals:**

- New primitives, behavior/preference changes, per-page Apply, status footer, search, profile card.

## Decisions

### D1 — List editors become stacked cards

The watched-directories, ignored-patterns, and default-agent editors are each a `SettingCard` with `Placement="Stacked"` whose content is the existing `SettingsList` plus its add/remove controls (and the pattern input). **Why:** the title+hint sit on top and the list spans the card, exactly like the other stacked controls. **Alternative rejected:** keeping lists outside cards — inconsistent with the rest of the settings surface.

### D2 — Projects & Paths layout

Page description "Watched directories, discovery rules, and the background watcher." Section `REPOSITORIES`: watched directories (stacked, icon folder), repository scan depth (right, icon zoom), repository discovery (stacked, icon filter), ignored directory patterns (stacked, icon blocked). Section `BACKGROUND WATCHER`: background watcher status (stacked, icon sync). **Why:** mirrors the page's own grouping; only scan depth is a compact control (right), the rest are wide (stacked).

### D3 — CLI Agents layout

Page description "Installed CLI agents, the default agent, and probe settings." Section `MANAGEMENT`: the default-agent card (stacked, icon robot) holding a **combo box** selector, followed by the agent list (unchanged custom cards). Section `PROBE`: the agents-probe card (stacked, icon diagnostic) holding the TTL/timeout fields. **Why:** keeps the two logical areas the user chose; the default agent is a single choice, so a compact dropdown fits better than a list, and the agent list is a list of entities, not a single card.

### D4 — Agent cards align to the card visual language

The custom per-agent card keeps its layout and actions but its surface matches `SettingCard` (Popover surface, `FieldStroke` border, chip corner radius, 12 padding), its title uses the same typography as a `SettingCard` title (body size, medium weight, primary text), and its logo is drawn in a rounded icon block like the card icon block. **Why:** the user asked to align visuals, not rebuild; the richer agent layout (status, version, path, commands, markers, actions) is preserved. **Alternative rejected:** rebuilding from `SettingCard` — loses the multi-field agent layout.

### D5 — Reuse primitives; add only icon glyphs

Use the existing `SettingCard`/`SettingsGroup`/`SectionHeader`; add a small number of `IconGlyphs` constants for the new settings. No new control. **Why:** keeps the vocabulary single-sourced.

## Risks / Trade-offs

- **List cards with nested scroll** → the `SettingsList` template already caps its inner scroll; cards stack content below the title, so nesting is safe.
- **Agent card typography change** → minor; verified in smoke that names stay readable against the details.

## Migration Plan

- None; presentational only.

## Open Questions

- None.
