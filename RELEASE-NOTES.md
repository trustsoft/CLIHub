# Release Notes

What's new in CLIHub, newest first. These are the short, user-facing notes; the technical record lives in
[CHANGELOG.md](CHANGELOG.md).

## 1.0.1 — 2026-10-24

### New
- The System theme now follows Windows light/dark changes while the app runs: switching Windows personalization repaints every open window immediately, with no restart or Settings save needed

### Improved
- Settings opens instantly: the CLI Agents page renders right away and the running-agent check that gates Update actions completes in the background moments later

## 1.0.0 — 2026-10-23

### New
- A third theme, Blue, joins Dark and Light; System Sync still follows the Windows light/dark setting
- Theme changes take effect as soon as you save Settings, with no restart
- The accent color is now independent of the theme: every accent preset works with every theme, and switching themes leaves your accent untouched

### Improved
- The launch and Settings footers share the same spacing, with the version chip aligned to the CLIHub label

### Fixed
- Opening Settings no longer freezes the window for several seconds while the CLI Agents page loads; agent update blocking behaves exactly as before

## 0.9.9 — 2026-10-20

### New
- The CLI Agents page shows when a newer version of an installed agent is available, as installed → latest, based on where that agent is distributed; all six built-in agents are covered out of the box

### Improved
- Terminal Profiles marks the Git Bash and PowerShell 7 runtimes as not installed when they cannot be found on the machine, so a missing runtime is visible before you launch anything
- Testing or re-detecting one agent no longer discards the cached detection and version data of the other agents

### Fixed
- The Git Bash runtime now always starts Git for Windows' own bash, resolved from the registry, known install folders, or PATH — never the WSL launcher; when Git Bash or PowerShell 7 cannot be resolved, launching fails with a clear, logged error instead of an arbitrary or missing executable

## 0.9.8 — 2026-10-19

### New

- Built-in agents now offer their full command set: Update for Cline CLI and OpenClaude, and Resume for GitHub Copilot and Qwen Code

### Improved

- An agent's Update action is blocked only while that same agent is running; other running agents no longer block it

### Fixed

- Launching an agent now starts it in the selected project folder instead of CLIHub's own working directory
- Agent commands with arguments no longer fail with a script error in PowerShell and PowerShell 7

## 0.9.7 — 2026-10-19

### New

- Settings has a Projects page that lists every project with its path, shows whether it was added manually or discovered, flags missing folders, and lets you add or remove manual projects
- Scanning changes on Project Discovery apply immediately, so a newly added watched folder is scanned without saving
- Each watched folder can keep its own scan depth and discovery mode, and can be scanned on its own
- The Projects summary shows the full project count with the manual and discovered split, and the watcher card shows how many folders the last scan examined
- The CLI Agents page has summary cards for installed agents, the default agent, and the probe policy
- Installed agents can be re-detected from the CLI Agents page, and agents that are not installed can no longer be picked as the default
- Agent cards are compact, with an installation chip, a supported-commands line, and icon actions

### Improved

- Theme and accent choices are preview cards, with new Teal, Indigo, and Tangerine accent presets
- The Updates page shows the version, channel, check status, and release-notes action in one header
- Numeric settings can be stepped with buttons, the Up/Down keys, or the mouse wheel
- Settings page headers stay visible while long pages scroll
- The page formerly called Projects & Paths is now Project Discovery, and launcher position options use clearer monitor and placement names
- The default hotkey is now Ctrl+Shift+Space
- Starting CLIHub a second time exits immediately instead of preparing application data first

### Fixed

- The Settings window no longer shrinks to the height of the launch window
- The pre-release notifications toggle is disabled until a beta channel exists
- The path display selector aligns to the trailing edge of its row

## 0.9.6 — 2026-10-13

### New

- Settings has a new window with sidebar navigation, page icons, and card layouts
- Choose a light, dark, or system theme and pick the accent color; changes apply live
- Pick the terminal used to launch agents: PowerShell, PowerShell 7, Command Prompt, or Git Bash
- Choose where the launch window appears on screen and start CLIHub minimized
- Updates can download in the background, with separate notifications for stable and pre-release versions
- The new Logs page controls log level and retention and opens the log folder
- CLIHub can watch folders you choose and add new projects automatically
- Manage your AI agents from the new CLI Agents page in Settings

### Improved

- Settings pages, drop-downs, and spacing follow one consistent card style
- The Telemetry & Logs page is now called Logs, and the hotkeys page is now Hotkeys & Quick Launcher

### Fixed

- Themed scroll bars no longer fall back to an incorrect color in lists

## 0.9.5 — 2026-10-10

### New

- The launch window now offers only the actions each AI agent actually supports, so commands an agent cannot run no longer appear
- CLIHub blocks an agent update while that agent is still running and explains why in the status area
- Reloading the plugin catalog refreshes the AI Agents list without restarting

### Improved

- Every update entry point shares one update state, so the tray, What's New, the launcher, and Settings always agree
- Settings apply more consistently, and the project path display style now follows your choice
- The launch window's Actions menus render more cleanly

### Fixed

- Background version and update work now finishes or cancels cleanly instead of leaving stale state or hanging

## 0.9.0 — 2026-10-05

### New

- CLIHub now runs on the .NET 10 Desktop Runtime with an explicit C# 14.0 development baseline

### Improved

- The app, tests, and release pipeline use the stable .NET 10 toolchain, with a pinned SDK policy for consistent local and CI builds
- Updated notification, logging, update, and test dependencies keep the application aligned with current compatible releases

### Fixed

- The installer now offers the .NET 10 Desktop Runtime when it is missing from the machine

## 0.8.5 — 2026-10-05

### New

### Improved

- Refreshing the AI Agents list updates rows in place instead of rebuilding them, so your selection and the agent details you are looking at stay put
- Agent version lookups are shared: refreshing several times at once no longer starts duplicate probes or leaves a stale version on screen

### Fixed

- The app no longer fails to start while creating its services — the ambiguous agent-command constructor that could stop startup before the tray appeared is gone
- Launching an agent now works when the project path or the executable contains spaces, and through .cmd and .bat shims
- Update checks and background refreshes no longer hang: they finish on timeout, cancel cleanly, and clear stale state
- Unexpected errors in background UI work are now logged and shown as a status message instead of disappearing silently

## 0.8.0 — 2026-10-04

### New

### Improved

- The launch window update control now uses a reusable state-aware component, keeping update checks, downloads, and restart actions consistent across the window.

### Fixed

## 0.7.0 — 2026-10-04

### New

- One update button in the window footer: it shows the current version, and when a new release is out it walks you through Update and Restart to update without leaving the window
- An update check or download started anywhere counts everywhere: the tray, the What's New window, and the footer button always show the same update state

### Improved

- The launch window opens snappier: project and agent logos are remembered between runs instead of being searched for in folders on every start
- Working with projects and settings no longer stutters: changes are saved in the background while you keep going
- Empty agent probe fields in Settings now show their default values as hints

### Fixed

- The pane scrollbar now renders inside the list's edge and no longer covers row content
- Shortened project paths are measured against the row's text and rows sit evenly against the divider, so nothing is cut off or overlaps

## 0.6.0 — 2026-10-03

### New

- Install updates with one click: download the new version from the tray or the What's New window and CLIHub restarts into it
- The Settings window now shares the launch window's dark look — a drawn header, segmented runtime and path selectors, and a key-chip hotkey field
- The What's New, Settings, and launch windows all share slim dark scrollbars and matching tooltips

### Improved

- The Settings window opens at the same height as the launch window, with its inputs sized and aligned to the new layout
- Hotkey capture happens in a compact chip field: press a combination, see it as keys, press Esc to cancel

### Fixed

- The What's New window's border no longer shows the native frame of a resizable window and now matches the launch window's

## 0.5.0 — 2026-09-30

### New

- Six AI agent CLIs are ready out of the box — OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, and Qwen Code — each with its own logo
- Launch an agent in the folder of your current project, straight from the tray
- Keep your project folders in one place, with favorites, a recent list, and logos detected from the folders themselves
- Press Ctrl+Shift+A in any application to open CLIHub, and choose your own combination in Settings
- A Settings window for the launch runtime, the global hotkey, agent probes, and update checks, with changes applied right away
- Start CLIHub together with Windows, and decide whether the window opens at startup or the app waits quietly in the tray
- See when a new version of CLIHub is available, and check for one whenever you like
- Drag the divider to give the Projects or the AI Agents pane more room
- Choose how project paths are shortened: keep the end of the path or keep both ends

### Improved

- Resume your last session with an agent, or ask it to update or initialize itself, without leaving CLIHub
- See every agent's version at a glance
- Agents you do not use in the current project are dimmed, and you can hide them instead
- The launch window is a compact dark popup with no title bar: always within reach, and out of your way as soon as you click elsewhere unless you pin it
- Remove a project from the list when you no longer need it, and your files are never touched

### Fixed

- Keys such as Space, Enter, Tab, and the arrows now work in a global hotkey combination
- Agent availability is up to date after you refresh the list
