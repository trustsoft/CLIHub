# UI Mockups

Reference images for UI work. The descriptions below are a snapshot of each image; the images are the source of truth and may be updated.

Status: `popup-split.png` is delivered by the `launch-window-ui` change (dark theme applied to the launch window); `settings.png` is delivered by `preferences-ui` (light theme).

## `popup-split.png` — launch window (Popup)

A dark, two-pane launcher.

- **PROJECTS** (left) and **AGENTS** (right) panes, each with a header and an **Actions ▾** menu.
- **Project rows:** rounded logo thumbnail; bold name; dimmed, middle-ellipsized path; a ★ for favorites. The selected row is highlighted with a left accent bar.
- **Agent rows:** logo; name; version beneath the name; two buttons on the right — **▶ launch** and **↻ resume session**. Agents not available are dimmed.
- **Footer:** left `CLIHub` + a version pill; right icon buttons — **folder** (add project), **gear** (settings), **power** (exit).
- Long lists scroll.

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
- Footer actions: add project, settings, exit; version pill in both windows. *(launch window delivered; the Settings window keeps its own footer)*
- Availability display already exists (dim); the mockup also dims/filters unavailable agents, and the filter lives in the **AGENTS** Actions menu. *(delivered)*
- The update check stays reachable from the launch window footer next to the version pill. *(delivered)*
- Downloading and applying updates with install "from tray" — still deferred to the packaging work.
- The dark palette is currently applied to the launch window only; the Settings window keeps its light styling.

## Related changes

- `launch-window-ui` (delivered) — the dark two-pane launch window: pane headers with Actions menus, project and agent rows, inline launch/resume, footer with version pill, update check and add-project/settings/exit actions, scrolling, and the `LaunchWindowViewModel` behind it.
- `main-window-layout` (delivered, redesigned) — resizable Projects/Agents panes and aligned pane layout, now expressed in the launch window as aligned pane headers and pane bodies ending at the footer.
- `preferences-ui` (delivered) — the Settings window, default-runtime `cmd`/`ps`/`wt` selection, hotkey capture, agent probe TTL/timeout, and the startup update toggle (light theme rather than the mockup's dark styling).
