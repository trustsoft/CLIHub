# Add missing built-in agent commands to the seeded descriptors

## Why

The built-in descriptors expose fewer commands than the agents actually support. A gap analysis
against the installed CLIs (local `--help`), the agent knowledge base, and the official
documentation and repositories found four missing commands:

| Agent | Missing command | Evidence |
|---|---|---|
| cline-cli | `update` = `cline update` | Cline CLI reference (docs.cline.bot/cli/cli-reference): `update` — "Check for updates and install if available" |
| github-copilot | `resume` = `copilot --continue` | GitHub Copilot CLI docs: "To quickly resume the most recently closed local session, enter `copilot --continue`" |
| openclaude | `update` = `openclaude update` | CLI help: `update\|upgrade` — "Check for updates and install if available"; repo Twigpine/openclaude |
| qwen-code | `resume` = `qwen --continue` | CLI help: `-c, --continue` — "Resume the most recent session for the current project" |

`cline` resume is intentionally NOT added: the CLI only resumes by explicit session id
(`cline --id <session-id>`) and has no resume-most-recent command, which the resume action
requires.

## What Changes

- Seed descriptors for `cline-cli`, `github-copilot`, `openclaude`, and `qwen-code` gain the
  missing command entries listed above.
- A test locks the expected command set per built-in agent so future gaps are caught.
- Existing installations are untouched by seeding (never overwrite user content); users update
  their installed descriptors manually or by re-seeding a fresh folder.

## Impact

- Affected specs: `plugin-seeding` (modified requirement: built-in descriptor coverage)
- Affected code:
  - `src/CLIHub.Core/SeedPlugins/{cline-cli,github-copilot,openclaude,qwen-code}/plugin.json`
  - `tests/CLIHub.Core.Tests/Plugins/PluginSeederTests.cs`
