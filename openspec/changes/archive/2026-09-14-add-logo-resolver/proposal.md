# Proposal: add-logo-resolver

## Why

The UI contract (`docs/ui.md`) defines list-item identity as logo + caption,
but both popup panes render text only, and the project `logo` field sits
unused in `config.json`. The vision fixes a single resolution mechanism with
different roots (plugin folder for agents, project folder for projects);
turning it into code gives both panes — and every future surface (tray menu,
settings) — one source of visual identity.

## What Changes

- New Core capability **logo resolution**: one priority chain evaluated flat
  against a root directory — `logo.png`, `logo.ico`, `icon.png`, `icon.ico`,
  `favicon.ico`; the first existing file wins.
- Agent root = the plugin's folder; project root = the project folder.
- Project manual override: a stored `logo` path in config wins over the
  chain; relative paths resolve against the project folder; a missing or
  broken override silently falls back to the auto chain.
- Popup wiring: Projects pane and Agents pane items display their logo next
  to the caption; when nothing resolves, the application default logo is
  shown.
- Plugin loading becomes folder-aware so agent logos can resolve from the
  plugin's assets directory.

## Capabilities

### New Capabilities

- `logo-resolution`: unified logo resolution chain with per-root evaluation,
  project override precedence, and a no-candidate result for the caller to
  map to the default visual.

### Modified Capabilities

- `popup`: list items gain logo identity in both panes, with the default
  logo when nothing resolves.

## Impact

- `src/CLIHub.Core`: new `LogoResolver` service (path logic only, filesystem
  abstraction); `PluginLoader` results carry the plugin folder.
- `src/CLIHub.App`: image decoding with a per-path cache and bounded decode
  size, a bundled default logo asset, popup item templates with an image.
- No `config.json` schema changes — `ProjectConfig.logo` already
  round-trips.
- `tests/CLIHub.Core.Tests`: chain order, override semantics, folder
  attachment.

Non-goals: a logo-picker UI for projects (belongs to the future "Edit"
flow), tray-menu logos (blocked on the open tray-menu question), SVG assets.
