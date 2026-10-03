# Changelog

The technical record of what changed in CLIHub: what, why, and which capability spec it belongs to.
User-facing notes for the same releases live in [RELEASE-NOTES.md](RELEASE-NOTES.md); both files use the
same version heading (`## <version> — <date>`) so a release lines up across them.

Groupings: `Added`, `Changed`, `Fixed`, `Removed`. A change that breaks existing behavior or
configuration is marked `**BREAKING**`. Capability names in parentheses refer to `openspec/specs/<name>`.

## 0.6.0 — 2026-10-02

### Added

- CI and release pipeline: build-and-test workflow on every pull request and push to `master`, and a
  tag-driven release workflow that tests, packages with `vpk` (framework-dependent win-x64 with a
  .NET 8 Desktop Runtime bootstrap), and publishes to GitHub Releases (`ci-build`, `release-pipeline`)
- Release runbook and pipeline documentation (`docs/releasing.md`, `docs/architecture.md → Packaging & CI/CD`)
- Downloading and applying updates from the tray: a one-click download-and-restart action in the tray menu
  and the What's New window, with a downloading state in the menu, completion and failure notifications, and
  a guard against concurrent downloads (`update-checking`)
- Dark theme for the Settings window: drawn chrome like the launch window (no OS title bar, header drag,
  Escape to close, DWM-rounded corners), an uppercase section rhythm with hint lines, segmented runtime and
  path-display selectors, a chip-styled hotkey capture field, themed inputs, checkboxes, and footer — with
  the window's height following the launch window's (`settings-theme`)
- Themed tooltips in the launch window, Settings, and What's New, sharing one slim dark style
  (`launch-window-theme`)

### Changed

- The update feed points at the real repository (`trustsoft/clihub`) instead of the `your-org/clihub`
  placeholder, so installed builds resolve the live GitHub Releases source (`update-checking`)
- The slim dark scrollbar is now an application-level style (it must live there to reach the scrollbars
  inside templates) and shrank to 4px, so the launch window lists, Settings, and What's New scroll
  consistently
- The Settings window opens at its 420px minimum width with a 30px probe input pair sized to the
  "Timeout, seconds" caption, auto-width checkboxes, an auto-width hotkey field with equal 8px insets, and
  segment groups with 2px gaps
- The Settings footer's brand and version chip reuse the launch window's styles

### Fixed

- The What's New window no longer draws the native frame of a resizable window around its own border and
  drops the top highlight, so its chrome matches the launch window's

## 0.5.0 — 2026-09-30

### Added

- .NET 8 WPF system-tray application scaffold: a dependency-injection container, Serilog file logging
  with 7-day retention and a configurable level, and the core service interfaces (`app-lifecycle`, `logging`)
- Single-instance enforcement via a named mutex, with a named pipe so a second launch activates the running
  instance and exits (`app-lifecycle`)
- Project management: folders tracked with an auto-detected logo, a favorite flag and last-used time, a
  recent-projects list, and a current project that provides launch context (`project-management`)
- Plugin descriptors (`%APPDATA%\CLIHub\plugins\<id>\plugin.json`) describing an agent's commands and
  detection markers, seeded on first run for six built-in agents — OpenCode, Pi, Cline CLI, GitHub Copilot,
  OpenClaude, and Qwen Code — together with their logos (`plugin-seeding`)
- Agent command set — launch, resume, version, update, and init — executed in the current project's folder,
  interactively through Windows Terminal, Command Prompt, or PowerShell, with output captured for the
  non-interactive commands (`agent-commands`)
- Agent availability detection: installed on the host and usable in a given project, with TTL-cached
  results (`agent-detection`)
- Per-agent version display, filled in asynchronously after the list renders, with an explicit cache
  invalidation on refresh (`agent-version`)
- Availability display: agents unavailable in the current project are dimmed, optionally hidden
  (`agent-availability-display`)
- Global hotkey (default `Ctrl+Shift+A`) registered with Windows, configurable, and re-registered at runtime
  without a restart; the parser accepts letters, digits, `F1`–`F24`, Space, and named keys such as Enter,
  Tab, and the arrows (`hotkey-support`)
- Update checking through Velopack: an asynchronous startup check, a manual check, a tray notification
  carrying the new version, and the current version shown in the window (`update-checking`)
- Settings window: default runtime, global-hotkey capture, agents-probe TTL and timeout, the startup update
  check, and the project path display style — all applied without restarting (`preferences-ui`)
- Start with Windows through a per-user Run registration, plus a "show window on startup" preference so the
  application can start in the tray (`app-lifecycle`)
- Resizable Projects and AI Agents panes with a draggable divider and an aligned pane layout
  (`main-window-layout`)
- Dark two-pane launch window, rebuilt as a chromeless popup shell: DWM-rounded corners, always on top,
  hidden when it loses focus unless pinned, Escape to hide, opening on the pointer's monitor, pane Actions
  menus, footer actions, and a version chip (`main-window-layout`, `launch-window-theme`)
- Remove-project action in the launch window, behind a confirmation that never deletes files

### Changed

- **BREAKING**: the plugin descriptor schema changed — the flat `commands` array became a `commands` object
  (`launch`, `resume`, `version`, `update`, `init`) plus a `detection` block. Existing descriptors must be
  updated (`agent-commands`)
- The `terminalExecutable` path preference is superseded by a three-way `defaultRuntime` (`wt` / `cmd` / `ps`);
  the old value is migrated once and kept for backward compatibility (`preferences-ui`)
- `user32.dll` declarations moved to `src/CLIHub/Interop/User32.cs` and converted to source-generated
  P/Invoke; behavior is unchanged
- `using` directives moved inside file-scoped namespaces, with an editorconfig rule that flags regressions
- The pre-redesign window is retained for reference but is no longer wired; the launch window is the
  application window (`main-window-layout`)

### Fixed

- Refreshing the agent list now invalidates the detection cache, so availability is re-evaluated instead of
  being served from a stale result (`agent-detection`)
- The hotkey parser accepts Space and additional named keys instead of falling back to the default
  combination (`hotkey-support`)
