# Plugins

Each plugin lives in `%APPDATA%\CLIHub\plugins\<id>\` with `plugin.json` and an optional `logo.png`:

```jsonc
{
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
  }
}
```

- `commands` is a named set; only `launch` is required.
- `detection.systemPaths` are host install markers (env vars expanded); `detection.projectIndicators` are project-relative markers.
- Built-in descriptors for OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, and Qwen Code are embedded and seeded on first run.

## Plugin Errors

- **Invalid plugin.json:** skip during discovery, log a warning (with the missing field)
- **Duplicate plugin IDs:** load the first, skip duplicates, log a warning
- **Missing logo:** fall back to the default project logo
