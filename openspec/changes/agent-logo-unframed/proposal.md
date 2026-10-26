# Agent logos on the CLI Agents page render without the rounded frame

## Why

Every per-agent card on the Settings → CLI Agents page wraps its logo in a 32x32 framed plate: a
`Border` with a 1 px `ControlStrokeBrush` stroke, a 6 px corner radius, and the logo painted as an
`ImageBrush` background clipped to the rounded corners
(src/CLIHub/Views/SettingsAgentsPage.xaml:184-195). The frame copies the shared `SettingCard` icon
block (src/CLIHub/Themes/SettingsStyles.xaml:220-236), which exists to give monochrome glyph icons a
visible plate — but agent logos are full-color images that do not need one. Everywhere else the app
renders agent logos as plain unframed images: the launch window's agent list
(src/CLIHub/Views/LaunchWindow.xaml:299-304) and the legacy `MainWindow` rows
(src/CLIHub/Views/MainWindow.xaml:57,114). The user asked for the rounded framed container to be
removed so the settings card presents the logo the same way.

## What Changes

- Replace the framed `Border` at src/CLIHub/Views/SettingsAgentsPage.xaml:184-195 with a plain
  `Image`: `Width="32"`, `Height="32"`, `VerticalAlignment="Top"`,
  `RenderOptions.BitmapScalingMode="HighQuality"`, `Stretch="Uniform"`, and
  `Source="{Binding LogoPath, Converter={StaticResource PathToImage}}"` — the same binding and
  presentation as the launch window's agent logo, at the card's existing 32x32 footprint.
- No border stroke, no corner-radius clipping, no plate background.
- Everything else on the card is untouched: the rounded card surface, name, state and update chips,
  "Supported commands" hint, message line, and the Detect / Update / Edit Config action buttons keep
  their current layout, margins, and spacing (the 12 px gutter comes from the text column's margin at
  SettingsAgentsPage.xaml:197, not from the logo block).
- Spec delta: the `settings-theme` requirement "Cards on the data pages" is modified — its body and
  its "Agent cards share the visual language" scenario currently claim the framed icon block
  treatment for per-agent cards, which would contradict the implementation; the scenario keeps its
  name and now claims the shared surface and typography with an unframed logo, and one new scenario
  states the unframed presentation precisely.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `settings-theme` — MODIFIED requirement "Cards on the data pages": all five original scenario
  names preserved verbatim ("Projects page cards", "List editor card", "CLI Agents page cards",
  "Default agent dropdown", "Agent cards share the visual language"); the last one drops the framed
  icon block claim; one new scenario ("Agent card logo rendered unframed") is added.

## Impact

Source: `src/CLIHub/Views/SettingsAgentsPage.xaml` only — one element swap inside the per-agent card
`DataTemplate` (lines 170-274). No C#, converter, style, or resource change: the `PathToImage`
converter is application-scoped (src/CLIHub/App.xaml:33, registered at
src/CLIHub/Startup/ApplicationHost.cs:138) and already feeds plain `Image.Source` bindings elsewhere,
and the default-logo fallback resolves upstream of the binding, so it is unaffected.

Tests: none added or changed. The repo has no control-template or visual-tree test harness (no test
applies a template or walks the visual tree), and the change is a pure presentation swap with no
code paths. Design.md "Testing decision" records the rationale; the behavior is pinned by the spec
scenario and a manual smoke task.

User-visible impact: the rounded frame, stroke ring, and plate disappear around each agent logo on
the CLI Agents page; the logo itself renders at the same 32x32 size and position. All other framed
icon blocks (setting cards, summary cards, project tiles) are unchanged.

## Non-goals

- No changes to logo assets, plugin seeding, the logo pipeline, or the default-logo fallback.
- No changes to the Project Scanning / project registry icon tiles.
- No changes to the three summary cards at the top of the CLI Agents page (AGENTS / DEFAULT /
  DETECTION) — they keep the framed icon block.
- No changes to the launch window or the legacy `MainWindow`.
- No changes to the rest of the per-agent card: surface, name, chips, hint, message, action buttons.
- No new shared style, sizing resource key, or control for the logo; the swap stays local to the
  page's `DataTemplate`.

## Dependencies

- None on other active changes: `openspec/changes/` holds no other active change, and the working
  tree is clean at `4550b99`.
- The `settings-theme` spec delta conflicts with nothing in flight; `agent-ui`'s "Agent card
  displayed content" requirement (openspec/specs/agent-ui/spec.md:221) only requires that the card
  present the logo, which stays true, so no `agent-ui` delta is needed.
