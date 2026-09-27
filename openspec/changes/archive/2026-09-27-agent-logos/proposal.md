## Why

Seeded agents currently show a placeholder because no logos ship with them. Each agent has a recognizable brand mark, so the agent list should display real logos out of the box. The logo assets are available locally (with the official source as fallback for the one that was missing).

## What Changes

- Bundle a logo for each of the six built-in agents (OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, Qwen Code)
- Extend first-run seeding to write each agent's `logo.png` alongside its `plugin.json`
- Emit PNG logos (WPF's `Image` cannot render SVG), rasterizing the SVG-only marks
- Keep the existing plugin logo resolution (descriptor folder `logo.png`, else the default logo)

## Capabilities

### New Capabilities

<!-- None: logo resolution/display already exists; this only seeds logo files. -->

### Modified Capabilities

- `plugin-seeding`: seeding now also writes each built-in agent's logo file, not just its descriptor

## Impact

**New assets:**
- `src/CLIHub.Core/SeedPlugins/<id>/logo.png` for the six agents (open/source: `D:\YandexDisk\Projects\CLIHub 2 UI\assets\logos\agents`, fallback the official project site/repo for Cline)

**Modified code:**
- `src/CLIHub.Core/CLIHub.Core.csproj` — embed `SeedPlugins\**\*.png` as well as `*.json`
- `src/CLIHub.Core/Services/PluginSeeder.cs` — write logo files next to descriptors (pairing logo→plugin id via the descriptor id, since embedded-resource names sanitize `-` to `_`)

**Unchanged:**
- `PluginManager` logo resolution and the UI `PathToImageConverter` (both already handle `logo.png`)

**Data:**
- `%APPDATA%\CLIHub\plugins\<id>\logo.png` created on first run

**No breaking changes.**
