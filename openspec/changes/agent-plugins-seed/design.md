## Context

See proposal.md - Why. Plugins load from `%APPDATA%\CLIHub\plugins\<id>\plugin.json` (`PluginManager`, with an injectable path added in `agent-commands`). `DirectoryInitializer` creates the `plugins\` folder. Startup now: single-instance guard → ensure AppData → logging → DI → `IPluginManager.LoadPlugins()` → tray/window. The descriptors use the schema introduced by `agent-commands`.

Command data below was captured from the actually installed CLIs (`<exe> --version` and `--help`), not invented.

## Goals / Non-Goals

**Goals:**
- A fresh install shows six agents with no manual setup
- Seeding is safe (never clobbers user files) and idempotent
- One source of truth for the built-in descriptors
- Testable without touching the real `%APPDATA%`

**Non-Goals:**
- Downloading or installing the agents themselves
- Auto-updating descriptors when the app updates (only first-run seeding)
- A UI to reset/restore descriptors
- Guaranteeing each agent's optional commands are perfect (descriptors are editable defaults)

## Decisions

### Decision 1: Built-in descriptors live in Core and are embedded

**Chosen:** `src/CLIHub.Core/SeedPlugins/<id>/plugin.json`, included as `<EmbeddedResource>`, read via `Assembly.GetManifestResourceStream`.

**Rationale:**
- Ships inside the assembly; no loose files to lose next to the exe
- One canonical copy (replaces `samples/plugins/`)
- Resource names are deterministic: `<RootNamespace>.SeedPlugins.<id>.plugin.json`

**Alternatives considered:**
- Loose content files copied to output: Rejected — another runtime path to manage
- C# string constants: Rejected — JSON in strings is harder to read/review than `.json` files

### Decision 2: `IPluginSeeder.SeedIfEmpty()` seeds only when there are no plugins

**Chosen:** Seed when the plugins folder has no subdirectory containing `plugin.json`.

**Rationale:**
- Matches "first run" semantics and the spec
- A user who deleted a single plugin and kept others is not re-seeded (at least one remains)
- Simple, deterministic check

**Alternatives considered:**
- Always rewrite descriptors: Rejected — clobbers user edits
- Track a "seeded" flag in config: Rejected — more state to keep consistent with the folder

### Decision 3: Seeding never overwrites; failures are logged, not fatal

**Chosen:** Write a descriptor only when its target file does not already exist; wrap the whole operation in try/catch and log per-plugin warnings.

**Rationale:**
- Safety first for user data
- Seeding is a convenience; it must not block startup (spec: non-fatal)

**Alternatives considered:**
- Fail startup on seeding error: Rejected — the app is still usable without seeded plugins

### Decision 4: Descriptor contents (verified against installed CLIs)

| Agent | id | launch | resume | version | update | init | system markers | project indicators |
|-------|----|--------|--------|---------|--------|------|----------------|--------------------|
| OpenCode | `opencode` | `opencode` | `opencode --continue` | `opencode --version` | `opencode upgrade` | — | `%USERPROFILE%\.opencode`, `%USERPROFILE%\.config\opencode` | `.opencode`, `openspec` |
| Pi | `pi` | `pi` | `pi --continue` | `pi --version` | `pi update` | — | `%USERPROFILE%\.pi` | `.pi` |
| Cline CLI | `cline-cli` | `cline` | — | `cline --version` | — | — | `%USERPROFILE%\.cline`, `%LOCALAPPDATA%\cline` | `.clinerules` |
| GitHub Copilot | `github-copilot` | `copilot` | — | `copilot --version` | `copilot update` | `copilot init` | `%USERPROFILE%\.copilot`, `%LOCALAPPDATA%\GitHubCopilotCLI` | `.github/copilot-instructions.md` |
| OpenClaude | `openclaude` | `openclaude` | `openclaude --continue` | `openclaude --version` | — | — | `%USERPROFILE%\.openclaude` | `.claude`, `CLAUDE.md` |
| Qwen Code | `qwen-code` | `qwen` | — | `qwen --version` | `qwen update` | — | `%USERPROFILE%\.qwen`, `%LOCALAPPDATA%\qwen-code` | `.qwen`, `QWEN.md` |

Verified facts: `pi 0.85.1`, `cline 3.0.65`, `openclaude 0.30.0 (OpenClaude)`, `qwen 0.24.1`, `copilot 1.0.88`; `--continue` present for pi/openclaude; `update`/`init` present for copilot; `update` for qwen/pi.

**Rationale:** Descriptors reflect the tools' real interfaces. Reserved commands are omitted rather than fabricated (`cline` has no simple "resume"; opencode has no `init`).

**Note:** system path checks resolved against the host confirmed `.pi`, `.cline`, `.openclaude`, `.qwen`, `.copilot` under `%USERPROFILE%` and `qwen-code`, `GitHubCopilotCLI` under `%LOCALAPPDATA%`. Project indicators are sensible defaults (for example `.clinerules`, `.github/copilot-instructions.md`) that users may adjust.

**Alternatives considered:** Guessing subcommands per tool — rejected; the table is grounded in `--help` output.

## Risks / Trade-offs

**[Risk] An agent's CLI changes its flags** → Mitigation: descriptors are editable; seeding only runs on an empty folder, so later corrections are not overwritten.

**[Risk] `%LOCALAPPDATA%` differs per user** → Mitigation: `Environment.ExpandEnvironmentVariables` handles it; markers are additive (any one match suffices).

**[Risk] Seeding surprises a user who wants a clean list** → Mitigation: only on empty folder; documented; user can delete entries.

**[Trade-off] Removing `samples/plugins`** → Benefit: single source of truth. Cost: fewer standalone examples in the repo; the embedded files serve as examples.

**[Trade-off] Project indicators are defaults, not guarantees** → Benefit: useful out of the box. Cost: may need editing for some projects; detection degrades to "no" harmlessly.

## Migration Plan

1. Move the opencode descriptor into `src/CLIHub.Core/SeedPlugins/opencode/plugin.json`; add the other five
2. Embed `SeedPlugins\**\*.json` in `CLIHub.Core.csproj`
3. Add `IPluginSeeder`/`PluginSeeder` (injectable plugins root); register in `AddClIHubCoreServices`
4. In `App.OnStartup`, call `SeedIfEmpty()` before `LoadPlugins()`
5. Delete `samples/plugins/`; tests with a temp plugins root

Rollback: revert code; seeded `%APPDATA%` plugins remain harmless and correct.

## Open Questions

- **Restore/reset command for seeded descriptors?** Deferred; delete the folder to re-seed.
- **Per-agent project indicators verified per real project?** Deferred; defaults are documented.
