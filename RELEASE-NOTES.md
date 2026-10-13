# Release Notes

What's new in CLIHub, newest first. These are the short, user-facing notes; the technical record lives in
[CHANGELOG.md](CHANGELOG.md).

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
