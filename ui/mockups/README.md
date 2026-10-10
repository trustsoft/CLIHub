# UI Mockups

Reference images for UI work. The descriptions below are a snapshot of each image; the images are the source of truth and may be updated.

Status: `popup-split.png` is delivered — dark theme and layout by the `launch-window-ui` change, chromeless popup shell and reference metrics by the `launch-window-chrome` change; `settings.png` is delivered by `preferences-ui` with the current dark theme. `settings-target.html` is the **target** design for the Settings window — it is not implemented yet and supersedes `settings.png` as the design direction for future Settings work.

## `popup-split.png` — launch window (Popup)

A dark, two-pane launcher, presented as a chromeless popup: no title bar, a 1px outline with a top inset highlight, rounded window corners, always on top, hidden when it loses focus unless pinned, and centred on the pointer's monitor whenever it is shown.

- **PROJECTS** (left) and **AGENTS** (right) panes, each with a header and an **Actions ▾** menu; the panes are separated by a hairline divider that is also the splitter.
- **Project rows:** rounded logo thumbnail; bold name; dimmed, middle-ellipsized path; a ★ for favorites. The selected row is highlighted with a left accent bar.
- **Agent rows:** logo; name; version beneath the name; two buttons on the right — **▶ launch** and **↻ resume session**. Agents not available are dimmed.
- **Footer:** left `CLIHub` + a version pill; right icon buttons — **folder** (open the data folder), **gear** (settings), **pin** (keep the window open) and **power** (exit); the update check sits next to the version pill and status text is centred.
- Long lists scroll inside the panes; the window height follows the content up to a cap.

## `settings-target.html` / `settings-target.png` — Settings window (TARGET, not implemented)

An interactive HTML mockup (Tailwind CDN, dark Fluent style) of the Settings window CLIHub should move to; `settings-target.png` is its rendered snapshot at 1024×740. It replaces the single-column `settings.png` layout with a Windows 11 Fluent–style shell:

- **Window:** native-looking title bar with app icon, `CLIHub Settings` title, version pill, and minimize/maximize/close buttons.
- **Left sidebar:** `Find a setting...` search field and grouped category navigation — *General* (**General & Startup**, **Hotkeys & Launchers** with an `Alt+Space` badge), *Engines & Repos* (**Projects & Paths**, **CLI Agents (Claude/Cline)**, **Terminal Profiles**), *System* (**Appearance & Mica**, **Telemetry & Logs**, **Updates** with a `Latest` badge). A quick profile card sits at the bottom.
- **Main content:** one page per category (the mockup shows "Hotkeys & Quick Launcher") with a page header, description, and an **Apply Changes** action; settings are rendered as cards with a title, hint, and an inline control:
  - key-chip hotkey display with a **Change** button (Quick Launcher `Alt+Space`; Direct Agent Run `Ctrl+Shift+Enter`);
  - toggle switches (Frameless Quick Launcher Mode; Dismiss on Focus Loss / Esc);
  - a dropdown for popup screen position (Center of Active Monitor / Top Center / Remember Last Position / Near Mouse Cursor).
- **Footer status bar:** `CLIHub` + version pill, a live status ("CLIHub ready" with a pulsing indicator), a sync note ("All hooks registered"), and right-aligned icon actions: toggle layout, minimize to tray, reload configurations, shutdown.

The categories, hotkey cards, and status bar imply capabilities beyond the current preferences; a future OpenSpec change should scope which of them become real settings.

## `settings.png` — Settings window (CURRENT, delivered)

Rendered from the running application (v0.9.0, 630×806):

- Title **Settings** with a close (×).
- **STARTUP:** checkboxes "Start with Windows" and "Show window on startup".
- **DEFAULT RUNTIME:** segmented control `cmd | ps | wt`.
- **APPEARANCE:** segmented control `Left trim | Middle ellipsis`; hint: "How long project paths are shortened in the launch window."
- **GLOBAL HOTKEY:** a capture field showing key chips (e.g. `Ctrl Alt Space`); hint: "Press a combination in the field. Esc cancels capture."
- **AGENTS PROBE:** numeric fields `TTL, minutes` and `Timeout, seconds`; hint: "Empty - use defaults."
- **UPDATES:** checkbox "Check for updates on startup"; button "Check for updates".
- **Footer:** `CLIHub` + version pill; **Save** and **Cancel**.

## Elements/settings implied for implementation

New or extended preferences:

| Setting | Notes |
|---------|-------|
| Default runtime | `cmd` / `ps` / `wt` → the legacy `terminalExecutable` preference as a 3-way choice |
| Global hotkey | capture control (currently edited only in `config.json`) |
| Agents probe TTL (minutes) | cache availability detection results; empty → default |
| Agents probe timeout (seconds) | per-probe timeout; empty → default |
| Check for updates on startup | boolean toggle for the Velopack check |

Launch window and Settings elements:

- Two-pane window with per-pane **Actions** menus; per-agent **Launch** and **Resume** buttons. *(delivered)*
- Chromeless popup shell, hairline divider, reference metrics and vector row actions. *(delivered)*
- Footer actions: add project, open the data folder, settings, pin, exit; version pill in both windows. *(launch window delivered; the Settings window keeps its own footer)*
- Availability display already exists (dim); the mockup also dims/filters unavailable agents, and the filter lives in the **AGENTS** Actions menu. *(delivered)*
- The update check stays reachable from the launch window footer next to the version pill. *(delivered)*
- Path shortening is selectable in Settings: **Left trim** (the mockup's `...tail` look) or **Middle ellipsis**. *(delivered)*
- Downloading and applying updates with install "from tray". *(delivered)*
- The dark palette is applied to the launch window, Settings window, and What's New window; each window draws its own chrome where required.

## Related changes

- `launch-window-chrome` (delivered) — the chromeless popup shell: no OS chrome with DWM-rounded corners, always on top, hide on focus loss with a persisted pin, Escape to hide, pointer-monitor placement, the reference palette and sizing tokens, hairline divider, reference row metrics and vector action buttons, Actions menus with icons, and the path display preference.
- `launch-window-ui` (delivered) — the dark two-pane launch window: pane headers with Actions menus, project and agent rows, inline launch/resume, footer with version pill, update check and add-project/settings/exit actions, scrolling, and the `LaunchWindowViewModel` behind it.
- `main-window-layout` (delivered, redesigned) — resizable Projects/Agents panes and aligned pane layout, now expressed in the launch window as aligned pane headers and pane bodies ending at the footer.
- `preferences-ui` (delivered) — the Settings window, default-runtime `cmd`/`ps`/`wt` selection, hotkey capture, agent probe TTL/timeout, and the startup update toggle.
