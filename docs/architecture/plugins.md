# Plugins

Each plugin lives in `%APPDATA%\CLIHub\plugins\<id>\` with `plugin.json` and an optional `logo.png`:

```jsonc
{
  "schemaVersion": 1,
  "id": "opencode",
  "name": "OpenCode",
  "description": "AI coding agent CLI",
  "commands": {
    "launch":  { "executable": "opencode" },
    "resume":  { "executable": "opencode", "arguments": "--continue" },
    "version": { "executable": "opencode", "arguments": "--version" },
    "update":  { "executable": "opencode", "arguments": "upgrade" }
  },
  "detection": {
    "systemPaths": [ "%USERPROFILE%\\.opencode" ],
    "projectIndicators": [ ".opencode", "openspec" ]
  },
  "updateCheck": {
    "source": "npm",
    "package": "@opencode/cli"
  }
}
```

- `schemaVersion` is optional and defaults to 1. Values greater than 1 or non-integer values are invalid;
  the supported maximum is currently 1.

- `commands` is a named set; only `launch` is required.
- `detection.systemPaths` are host install markers (env vars expanded); `detection.projectIndicators` are project-relative markers.
- `updateCheck` is optional and names the distribution channel the agent publishes to, so CLIHub can tell when a newer version exists. Two source kinds are supported:
  - `"source": "npm"` with `"package": "<name>"` — the latest version is read from `https://registry.npmjs.org/<package>/latest`.
  - `"source": "githubReleases"` with `"repo": "<owner>/<repository>"` — the latest version is read from that repository's latest GitHub release (`tag_name`, leading `v` stripped).
  - An unrecognized `source` is ignored with a logged warning; the agent is treated as having no update source. The result never gates or alters the Update action.
- Built-in descriptors for OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, and Qwen Code are embedded and seeded on first run.

## Plugin Errors

- **Invalid plugin.json:** skip during discovery, log a warning (with the missing field)
- **Duplicate plugin IDs:** load the first, skip duplicates, log a warning
- **Missing logo:** fall back to the default project logo
