# CLIHub

A Windows system tray companion that launches AI agent CLIs in the context of your current project. It tracks your projects, detects which agents are installed and in use, and starts them in the right folder with a click or a global hotkey.

Supported out of the box: **OpenCode**, **Pi**, **Cline CLI**, **GitHub Copilot**, **OpenClaude**, **Qwen Code** — plus any agent you describe with a small JSON plugin.

## Features

- **System tray launcher** — current project, recent projects, add project, launch agent, exit
- **Global hotkey** — `Ctrl+Shift+A` toggles the window from any application (configurable)
- **Projects** — track folders with auto-detected logos, a recent list, and a current-project context
- **Agents as plugins** — JSON descriptors; the six built-in agents are seeded on first run, with logos
- **Agent commands** — launch, resume last session, version, update, initialize (per agent)
- **Availability** — detects whether an agent is installed on the host and used in a project; show all (dimming unavailable) or hide unavailable
- **Versions** — each agent's version is captured and shown in the list
- **Windows Terminal integration** — agents run in Windows Terminal, in the project folder
- **Single instance** — a second launch activates the running instance instead of opening a duplicate
- **Logging** — structured file logs with a configurable level and 7-day retention

## Requirements

- Windows 10 or 11
- [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) SDK to build, or the runtime to run a published build
- [Windows Terminal](https://aka.ms/terminal) (`wt.exe`) recommended for launching agents

## Build & run

```powershell
# From the repository root
dotnet build CLIHub.sln
dotnet test CLIHub.sln
dotnet run --project src/CLIHub/CLIHub.csproj
```

Build output (self-contained publish is not configured yet) lands under `artifacts\`:

```
artifacts\CLIHub\Debug\net8.0-windows\CLIHub.exe
```

## Quick start

1. Start CLIHub. A tray icon appears; the main window opens.
2. Click **Add Project…** and choose a project folder.
3. Select the project, choose an agent, and click **Launch** (or **Resume**, **Init**, **Update**, **Version**).
4. Close the window to keep CLIHub in the tray; press **Ctrl+Shift+A** to bring it back.

## Using the agent list

Each agent row shows its **logo**, **name**, **version**, and availability:

```
System: yes | Project: yes
```

- **System** — an agent CLI is installed on the host (its marker paths exist).
- **Project** — the agent is used in the selected project (its indicator files/folders exist).

Agents not used in the current project are **dimmed**, or **hidden** when *Only agents available in project* is checked. The choice is remembered.

Agent commands (each maps to the agent's own CLI):

| Command | Meaning |
|---------|---------|
| **Launch** | Start a new agent session in the project folder |
| **Resume** | Resume the previous session (where the agent supports it) |
| **Init** | Initialize the agent in the project (where supported) |
| **Update** | Run the agent's own updater |
| **Version** | Capture and show the agent's version |

Commands an agent does not define are reported as unsupported instead of running.

## Configuration and data

All application data lives under `%APPDATA%\CLIHub\`:

```
%APPDATA%\CLIHub\
├── config.json              # projects, current project, preferences
├── logs\
│   └── clihub-YYYYMMDD.log  # daily logs (7-day retention)
├── plugins\
│   └── <id>\
│       ├── plugin.json      # agent descriptor
│       └── logo.png         # agent logo (optional)
└── cache\
```

`config.json` structure (camelCase):

```jsonc
{
  "projects": [
    {
      "id": "…",
      "name": "MyApp",
      "path": "C:\\projects\\myapp",
      "isFavorite": false,
      "lastUsed": "2026-09-27T00:00:00Z",
      "logoPath": "C:\\projects\\myapp\\logo.png"
    }
  ],
  "preferences": {
    "startWithWindows": false,
    "hotkey": "Ctrl+Shift+A",
    "terminalExecutable": "wt.exe",
    "logLevel": "Information",
    "showOnlyProjectAgents": false
  },
  "currentProjectId": "…"
}
```

Editable preferences: `hotkey` (e.g. `Ctrl+Shift+A`), `logLevel` (`Debug`/`Information`/`Warning`/`Error`), `terminalExecutable` (default `wt.exe`), `showOnlyProjectAgents` (the filter toggle). A hotkey or log-level change takes effect after restart.

## Agent plugins

Each agent is described by `%APPDATA%\CLIHub\plugins\<id>\plugin.json` with an optional `logo.png`. Example:

```jsonc
{
  "id": "my-agent",
  "name": "My Agent",
  "description": "Optional description",
  "commands": {
    "launch":  { "executable": "my-agent" },
    "resume":  { "executable": "my-agent", "arguments": "--continue" },
    "version": { "executable": "my-agent", "arguments": "--version" },
    "update":  { "executable": "my-agent", "arguments": "upgrade" },
    "init":    { "executable": "my-agent", "arguments": "init" }
  },
  "detection": {
    "systemPaths": [ "%USERPROFILE%\\.my-agent" ],
    "projectIndicators": [ ".my-agent" ]
  }
}
```

- `id`, `name`, and `commands.launch` are required; the other commands are optional.
- `commands.*.arguments` is an optional single string.
- `detection.systemPaths` — paths (environment variables allowed) whose existence means the agent is installed on the host.
- `detection.projectIndicators` — names, relative to a project root, whose existence means the agent is used in that project.

Built-in descriptors for the six supported agents are embedded in the app. They are written to the plugins folder **only when it is empty**, and existing files are never overwritten. To restore them, delete `%APPDATA%\CLIHub\plugins\` and restart.

## Global hotkey

The default hotkey is `Ctrl+Shift+A`. It toggles the CLIHub window from anywhere. To change it, edit `preferences.hotkey` in `config.json` and restart. A hotkey must include at least one modifier (`Ctrl`, `Shift`, `Alt`, `Win`) and one key (`A`–`Z`, `0`–`9`, `F1`–`F24`); an invalid value falls back to the default.

## Logging

Logs are written to `%APPDATA%\CLIHub\logs\clihub-YYYYMMDD.log` (daily files, 7-day retention). The level is controlled by `preferences.logLevel` (default `Information`). Log lines look like:

```
[2026-09-27 20:20:21.621 INF] Loaded plugin opencode (OpenCode)
```

## Project layout

```
src/CLIHub.Core/   # business logic — models, services, plugin/agent handling (net8.0, no WPF)
src/CLIHub/        # WPF application — tray, hotkey, windows (net8.0-windows)
src/CLIHub.Tests/  # xUnit tests
```

## Troubleshooting

- **No tray icon** — check the tray overflow (`^`); ensure `CLIHub.exe` is running.
- **An agent shows `unknown` version or fails to launch** — verify its executable is on `PATH` (run its `--version` in a terminal) and that the descriptor's `commands` are correct.
- **Nothing happens on hotkey** — another app may own the combination; change `preferences.hotkey` and restart.
- **Need a reset** — exit CLIHub, then delete `%APPDATA%\CLIHub\config.json` (and/or the `plugins\` folder) to regenerate defaults on next start.

## Documentation

- [`docs/vision.md`](docs/vision.md) — vision and roadmap
- [`docs/architecture.md`](docs/architecture.md) — architecture, plugin format, conventions
- [`docs/repo-structure.md`](docs/repo-structure.md) — repository layout
- [`AGENTS.md`](AGENTS.md) — guidance for AI coding agents
