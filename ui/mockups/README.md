# UI Mockups

Reference images for UI work. The descriptions below are a snapshot of each image; the images are the source of truth and may be updated.

Status: `popup-split.png` is delivered — dark theme and layout by the `launch-window-ui` change, chromeless popup shell and reference metrics by the `launch-window-chrome` change; `settings.png` is delivered by `preferences-ui` (light theme).

## `popup-split.png` — launch window (Popup)

A dark, two-pane launcher, presented as a chromeless popup: no title bar, a 1px outline with a top inset highlight, rounded window corners, always on top, hidden when it loses focus unless pinned, and centred on the pointer's monitor whenever it is shown.

- **PROJECTS** (left) and **AGENTS** (right) panes, each with a header and an **Actions ▾** menu; the panes are separated by a hairline divider that is also the splitter.
- **Project rows:** rounded logo thumbnail; bold name; dimmed, middle-ellipsized path; a ★ for favorites. The selected row is highlighted with a left accent bar.
- **Agent rows:** logo; name; version beneath the name; two buttons on the right — **▶ launch** and **↻ resume session**. Agents not available are dimmed.
- **Footer:** left `CLIHub` + a version pill; right icon buttons — **folder** (open the data folder), **gear** (settings), **pin** (keep the window open) and **power** (exit); the update check sits next to the version pill and status text is centred.
- Long lists scroll inside the panes; the window height follows the content up to a cap.

## `settings.png` — Settings window

- Title **Settings** with a close (×).
- **DEFAULT RUNTIME:** segmented control `cmd | ps | wt` (wt selected).
- **GLOBAL HOTKEY:** a capture field showing key chips (e.g. `Ctrl Alt Space`); hint: "Press a combination in the field. Esc cancels capture."
- **AGENTS PROBE:** numeric fields `TTL, minutes` (15) and `Timeout, seconds` (3); hint: "Empty — use defaults."
- **UPDATES:** checkbox "Check for updates on startup" (on); button "Check for updates"; status line, e.g. "v0.6.0 — ready to install (install from tray)".
- **Footer:** `CLIHub` + version pill; **Save** and **Cancel**.

## Elements/settings implied for implementation

New or extended preferences:

| Setting | Notes |
|---------|-------|
| Default runtime | `cmd` / `ps` / `wt` → the current `terminalExecutable` as a 3-way choice |
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
- Downloading and applying updates with install "from tray" — still deferred to the packaging work.
- The dark palette is applied to the launch window and to the What's New window (which draws its own chrome), while the Settings window keeps its light styling (owned by the launch window so it stays visible above it).

## Related changes

- `launch-window-chrome` (delivered) — the chromeless popup shell: no OS chrome with DWM-rounded corners, always on top, hide on focus loss with a persisted pin, Escape to hide, pointer-monitor placement, the reference palette and sizing tokens, hairline divider, reference row metrics and vector action buttons, Actions menus with icons, and the path display preference.
- `launch-window-ui` (delivered) — the dark two-pane launch window: pane headers with Actions menus, project and agent rows, inline launch/resume, footer with version pill, update check and add-project/settings/exit actions, scrolling, and the `LaunchWindowViewModel` behind it.
- `main-window-layout` (delivered, redesigned) — resizable Projects/Agents panes and aligned pane layout, now expressed in the launch window as aligned pane headers and pane bodies ending at the footer.
- `preferences-ui` (delivered) — the Settings window, default-runtime `cmd`/`ps`/`wt` selection, hotkey capture, agent probe TTL/timeout, and the startup update toggle (light theme rather than the mockup's dark styling).
