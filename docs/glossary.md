# CLIHub Glossary

Canonical definitions of the terms used across this documentation, the capability specs in
[`openspec/specs/`](../openspec/specs/), and the source code. When writing docs, specs, or code,
use these terms with these meanings.

## Projects

- **Project folder** — an ordinary directory on disk where the user works. CLIHub treats it as
  read-only context: it never creates or modifies files inside it. Registered via **Add Project…**.
- **Project** — CLIHub's registration record for a project folder: a unique ID, the folder path,
  a display name derived from the folder name, a favorite flag, and a last-used timestamp. Stored
  in `%APPDATA%\CLIHub\config.json`. Removing a project removes the record only — the folder and
  its contents stay untouched. See [project-management](../openspec/specs/project-management/spec.md).
- **Current project** — the selected project that provides [launch context](#projects) for agent
  commands. Persisted as `currentProjectId` and restored on startup. With no current project,
  every agent command is blocked with a "select a project" message.
- **Launch context** — the current project's folder, used as the working directory of a launched
  agent or any other [agent command](#agents-and-plugins).
- **Recent projects** — projects ordered by last-used time (selection or a launch updates the
  timestamp); shown first in the project list and tray menu.
- **Favorite project** — a project flagged by the user; favorites can be pinned above recents.
- **Project logo** — an image resolved from well-known filenames in the project folder
  (for example `logo.png`, `icon.png`); falls back to a built-in default logo.

## Agents and plugins

- **Agent** — an AI coding agent CLI (OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude,
  Qwen Code, or any user-added plugin). CLIHub never embeds or runs an agent in-process; it only
  launches the agent's commands in an external terminal and inspects it with file-system checks.
- **Plugin** — the descriptor-only way to define an agent: a folder under
  `%APPDATA%\CLIHub\plugins\<id>\` containing a `plugin.json` [descriptor](#agents-and-plugins)
  and an optional `logo.png`. No DLLs, no code execution — a plugin is data. See
  [Plugin Descriptor Format](architecture.md#plugin-descriptor-format).
- **Plugin descriptor** — the `plugin.json` file: `id`, `name`, `commands`, and `detection`
  markers. Only the `launch` command is required; a descriptor without it is rejected.
- **Built-in agent** — one of the agents whose descriptors (and logos) are embedded in the
  application binary: OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, Qwen Code.
- **Seeding** — writing the built-in descriptors into the plugins folder on first run, when it is
  empty. Seeding never overwrites or deletes existing plugin files; delete the plugins folder to
  re-seed. See [plugin-seeding](../openspec/specs/plugin-seeding/spec.md).
- **Agent command** — a named operation an agent exposes, identified by its kind: `launch`,
  `resume`, `version`, `update`, or `init`. A plugin defines commands as a named set, not an
  ordered list; requesting a kind the plugin does not define is rejected. See
  [agent-commands](../openspec/specs/agent-commands/spec.md).
  - *Interactive commands* (`launch`, `resume`, `init`, `update`) open in the configured
    [runtime](#agents-and-plugins) with the project folder as the working directory.
  - *Captured commands* (`version`) run headlessly and their standard output is parsed —
    see [agent-version](../openspec/specs/agent-version/spec.md).
- **Runtime** — the terminal application used for interactive agent commands: Windows Terminal
  (default), Command Prompt, or PowerShell. Configured via the `defaultRuntime` preference.

## Detection and availability

- **Detection** — deciding, with file-system checks only, whether an agent is installed on the
  host and whether it is used in a project. Detection never starts an agent process. See
  [agent-detection](../openspec/specs/agent-detection/spec.md).
- **System path** (install marker) — a path declared in the plugin's `detection.systemPaths`
  whose existence (environment variables expanded) means the agent is **installed on the host**.
- **Project indicator** — a file or folder name declared in the plugin's
  `detection.projectIndicators` whose existence inside a project folder means the agent is
  **available in that project** (typically the agent's own config folder, e.g. `.opencode`).
- **Installed on the host** — at least one declared system path exists. Agents that are not
  installed are excluded from the agent list entirely, regardless of any filter.
- **Available in a project** — at least one declared project indicator exists in the current
  project folder. Agents that are installed but not available in the current project are dimmed,
  or hidden when the **Only agents available in project** filter is enabled. See
  [agent-availability-display](../openspec/specs/agent-availability-display/spec.md).
- **Dimming** — presentation-only de-emphasis of agents not available in the current project.
  Dimmed agents stay selectable and their commands stay invocable.
- **Detection cache** — per-agent detection results reused until a configurable time-to-live
  elapses (`agentProbeTtlMinutes`, default 15 minutes) or the cache is invalidated by a manual
  refresh.
- **Probe** — an umbrella term for the checks CLIHub runs against agents: detection checks and
  the [version](#agents-and-plugins) lookup. `agentProbeTtlMinutes` bounds how long detection
  and version results are cached; `agentProbeTimeoutSeconds` bounds how long a version probe may
  run before the process is terminated and the version is reported as `unknown`.

## Application

- **Launch window** — the main CLIHub window, opened from the tray or the global hotkey, with the
  Projects pane and the AI Agents pane separated by a resizable divider.
- **Tray** — the system tray (notification area) icon with the context menu: current project,
  recent projects, Add Project…, launch agents, Settings, What's New, exit.
- **Global hotkey** — a configurable key combination (default `Ctrl+Shift+A`) that toggles the
  launch window from any application. See [hotkey-support](../openspec/specs/hotkey-support/spec.md).
- **Single instance** — a second CLIHub launch does not start a new process; it activates the
  running instance via a named mutex and a named-pipe activation signal.
- **Preferences** — user settings persisted under the `preferences` object of `config.json`
  (hotkey, runtime, log level, filter, probe TTL/timeout, autostart, update check). Editable from
  the Settings window; changes apply without restart.
- **Settings window** — the window (opened from the tray) where preferences are edited.

## Releases and updates

- **Changelog** — `CHANGELOG.md` at the repository root: the technical, developer-facing record
  of changes (Keep-a-Changelog style). Not rendered in the application.
- **Release notes** — `RELEASE-NOTES.md` at the repository root: short, user-facing notes in
  plain text, embedded into `CLIHub.Core` and parsed by the application. The only file the app
  renders. See [release-notes](../openspec/specs/release-notes/spec.md) and
  [changelog-and-release-notes.md](changelog-and-release-notes.md).
- **What's New** — the window that shows release notes per version, openable from the tray; after
  an update it opens once for the new version.
- **Update** — the Velopack-based self-update: a check on startup (with a tray notification) and
  on demand; **Download and restart** fetches the update and relaunches CLIHub into it. See
  [update-checking](../openspec/specs/update-checking/spec.md).
