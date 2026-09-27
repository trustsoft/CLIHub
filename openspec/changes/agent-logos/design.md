## Context

See proposal.md - Why. `plugin-seeding` (archived change `agent-plugins-seed`) embeds `SeedPlugins/<id>/plugin.json` and writes them to `%APPDATA%\CLIHub\plugins\<id>\plugin.json` when the folder is empty. `PluginManager.LoadPluginLogo` already prefers `<pluginDir>\logo.png` and falls back to the default. `PathToImageConverter` renders a `logo.png` path into an `ImageSource` (PNG only — WPF cannot decode SVG).

## Goals / Non-Goals

**Goals:**
- Real logos for all six built-in agents after a fresh install
- Extend the existing seeding path with minimal new code
- Keep the pairing of logo → plugin correct despite embedded-resource name sanitization

**Non-Goals:**
- SVG rendering support in the app (logos are shipped as PNG)
- User-editable logo selection UI
- Rasterizing assets for non-built-in (user) plugins

## Decisions

### Decision 1: Ship PNG logos (not SVG)

**Chosen:** Bundle `logo.png` per agent.

**Rationale:**
- WPF `BitmapImage`/`Image` cannot decode SVG without an extra library
- PNG keeps `PluginManager` and `PathToImageConverter` unchanged
- Raster once at authoring time, not at runtime

**Alternatives considered:**
- Add an SVG library (`SharpVectors`/`Svg.Skia`) and render `.svg`: Rejected — a new runtime dependency and converter/key changes for a small feature
- Ship both SVG and PNG: Rejected — unused SVG adds weight

**Asset provenance (120–160 px, transparent where applicable):**
| Agent | Source |
|-------|--------|
| OpenCode | `assets/logos/agents/opencode.png` (local) |
| Pi | `assets/logos/agents/pi.svg` → rasterized (local) |
| Cline CLI | `raw.githubusercontent.com/cline/cline/main/assets/icons/icon.png` (official repo; not in the local set) |
| GitHub Copilot | `assets/logos/agents/copilot.png` (local) |
| OpenClaude | `assets/logos/agents/openclaude.svg` → rasterized (local) |
| Qwen Code | `assets/logos/agents/qwen.svg` → rasterized (local) |

### Decision 2: Pair logos to plugins by descriptor id, not resource name

**Chosen:** In `PluginSeeder`, first read every `*.plugin.json` resource and parse its `id` (as today). Then, for each `*.logo.png` resource, resolve the target plugin via the sanitized key → id map built from the descriptors.

**Rationale:**
- Embedded-resource names sanitize `-` to `_` (`cline-cli` → `cline_cli`), so the folder cannot be recovered from the logo resource name
- The descriptor already carries the canonical id; reusing it keeps a single source of truth

**Alternatives considered:**
- Rename agent folders to avoid hyphens: Rejected — ids are already established and user-visible
- Hardcode the id list in the seeder: Rejected — duplicates the data

### Decision 3: Write-once, same guards as descriptors

**Chosen:** A logo is written only during first-run seeding and only when the target file does not exist; failures are logged and non-fatal.

**Rationale:** Consistent with the existing seeding contract (never overwrite, idempotent, non-fatal).

**Alternatives considered:** Re-seed logos on every start — rejected; unnecessary writes.

## Risks / Trade-offs

**[Risk] A logo file is missing from the embedded set** → Mitigation: seeding logs a warning per resource and continues; the UI falls back to the default logo.

**[Risk] Rasterized PNG looks soft at large sizes** → Mitigation: rendered at 160 px; the UI shows ~24–32 px, so it stays crisp.

**[Risk] Licensing of third-party marks** → Trade-off: logos are used only to identify the respective tools in a local launcher; each remains the property of its project. Documented in the descriptor set.

**[Trade-off] Larger assembly** → Benefit: no runtime download; each logo is 2–6 KB.

## Migration Plan

1. Add `SeedPlugins/<id>/logo.png` for the six agents
2. Embed `SeedPlugins\**\*.png` in `CLIHub.Core.csproj`
3. Extend `PluginSeeder` to write logos (id-map pairing)
4. Tests: seeding writes a logo for each agent; existing logo is preserved; logo optional
5. Existing users: delete `%APPDATA%\CLIHub\plugins\` to re-seed with logos (documented)

Rollback: revert code; seeded logos are inert.

## Open Questions

- **Add logos for user-added plugins automatically?** No — user plugins provide their own `logo.png`.
- **Per-agent light/dark logo variants?** Deferred; a single mark per agent for now.
