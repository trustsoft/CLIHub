# CLIHub

A Windows system tray companion that launches AI agent CLIs (OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, Qwen Code, and any plugin) in the context of your current project.

## Features

- **System tray launcher** — current project, recent projects, add project, launch agent, exit
- **Global hotkey** — `Ctrl+Shift+A` toggles the window from any application; the combination is configurable (letters, digits, `F1`–`F24`, and named keys such as Enter, Tab, Esc, and the arrows)
- **Projects** — track folders with auto-detected logos, recents, and a current-project context
- **Agents as plugins** — JSON descriptors; built-in agents are seeded on first run with logos
- **Agent commands** — launch, resume last session, version, update, initialize (per agent)
- **Availability** — detects whether an agent is installed on the host and used in a project; dim or hide unavailable agents
- **Versions** — each agent's version is captured and shown in the list
- **Resizable layout** — drag the divider between the Projects and AI Agents panes
- **Runtime selection** — open agents in Windows Terminal, Command Prompt, or PowerShell
- **Settings window** — change the runtime, global hotkey, agent probe caching/timeout, and the startup update check from the tray; changes apply without restart
- **Autostart** — start with Windows, and choose whether the window opens on startup (otherwise CLIHub starts in the tray)
- **Updates** — checks for a new version on startup (with a tray notification) and on demand, and shows the current version
- **Single instance** — a second launch activates the running instance
- **Logging** — structured file logs with configurable level and 7-day retention

## Requirements

- Windows 10 or 11
- [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0) SDK (to build) or runtime (to run a published build)
- Windows Terminal (`wt.exe`) recommended for launching agents

## Build & run

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
dotnet run --project src/CLIHub/CLIHub.csproj
```

## Using it

1. Right-click the tray icon (or press **Ctrl+Shift+A**) to open CLIHub.
2. **Add Project…** and pick a project folder.
3. Select the project, then choose an agent and click **Launch** (or **Resume**, **Init**, **Update**, **Version**).
4. Agents not used in the current project are dimmed, or hidden via **Only agents available in project**.

## Data & configuration

Everything lives under `%APPDATA%\CLIHub\`:

```
%APPDATA%\CLIHub\
├── config.json              # projects, current project, preferences
├── logs\clihub-YYYYMMDD.log # daily logs (7-day retention)
├── plugins\<id>\            # plugin.json + logo.png (seeded on first run)
└── cache\
```

Key preferences in `config.json` → `preferences`: `startWithWindows`, `showWindowOnStartup`, `hotkey`, `defaultRuntime`, `logLevel`, `terminalExecutable` (legacy), `showOnlyProjectAgents`, `agentProbeTtlMinutes`, `agentProbeTimeoutSeconds`, `checkForUpdatesOnStartup`. Most are editable from the **Settings** window (tray menu). Full layout and configuration details: [`docs/architecture.md`](docs/architecture.md).

## Adding an agent plugin

Create `%APPDATA%\CLIHub\plugins\<id>\plugin.json` (and optionally `logo.png`):

```jsonc
{
  "id": "my-agent",
  "name": "My Agent",
  "commands": {
    "launch":  { "executable": "my-agent" },
    "resume":  { "executable": "my-agent", "arguments": "--continue" },
    "version": { "executable": "my-agent", "arguments": "--version" }
  },
  "detection": {
    "systemPaths": [ "%USERPROFILE%\\.my-agent" ],
    "projectIndicators": [ ".my-agent" ]
  }
}
```

Only `launch` is required. Built-in descriptors for the six supported agents are embedded in the app and seeded when the plugins folder is empty; delete the folder to re-seed. Full descriptor format: [`docs/architecture.md`](docs/architecture.md#plugin-descriptor-format).

## Project layout

```
src/CLIHub.Core/       # business logic (net8.0, no WPF)
src/CLIHub/            # WPF app (net8.0-windows)
tests/CLIHub.Tests/    # xUnit tests
```

## Documentation

- [`docs/vision.md`](docs/vision.md) — vision and roadmap
- [`docs/architecture.md`](docs/architecture.md) — architecture, plugin format, conventions
- [`docs/repo-structure.md`](docs/repo-structure.md) — repository layout
- [`docs/changelog-and-release-notes.md`](docs/changelog-and-release-notes.md) — changelog & release-notes plan (draft)
- [`AGENTS.md`](AGENTS.md) — guidance for AI coding agents
- `openspec/specs/` — durable capability specs
