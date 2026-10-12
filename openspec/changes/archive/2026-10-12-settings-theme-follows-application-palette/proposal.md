## Why

`application-theme` lets the user pick Dark, Light, or System, and the Settings window renders with the light palette when the effective theme is light. The `settings-theme` capability still asserts a dark-only look in most of its requirements ("dark input", "dark strip", "dark surface", "shared dark palette"), so the spec contradicts the shipped behavior. A previous reconciliation fixed only the chrome requirement and left the rest.

## What Changes

- Correct every remaining dark-only statement in `settings-theme` so the window uses the application palette for the active theme: `Dark input controls`, `Themed footer`, `Update status presentation`, `Validation errors remain readable`, `Sidebar navigation styling`, `Setting cards` (card visual language), and `Cards on the data pages`.
- Keep the layout, chrome, card, and control requirements otherwise unchanged.
- Fix the repository documents that still describe the removed `init` command, Windows Terminal as a runtime, favorites pinned above recents, and a bracketed changelog heading: `docs/glossary.md`, `README.md`, `docs/architecture/startup.md`, `docs/releasing.md`, and the stale phrase in `docs/adr/0001-core-ui-boundaries.md`.

## Capabilities

### New Capabilities
<!-- None. -->

### Modified Capabilities
- `settings-theme`: all input, footer, status, error, sidebar, and card statements follow the application palette instead of a dark-only palette.

## Impact

- `openspec/specs/settings-theme/spec.md` (through this change's delta).
- `docs/glossary.md`, `README.md`, `docs/architecture/startup.md`, `docs/releasing.md`, `docs/adr/0001-core-ui-boundaries.md`.
- No production or test code change; no `config.json` change.

## Non-goals

- Changing runtime behavior, the configuration schema, or any WPF/XAML.
- Renaming `settings-theme` or its requirement titles.
