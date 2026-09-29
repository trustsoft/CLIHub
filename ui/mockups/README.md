# UI Mockups

Reference images for upcoming UI work. Descriptions below are a snapshot to guide the `launch-window-ui` change (`preferences-ui` is delivered); the images are the source of truth and may be updated.

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

New UI/logic implied:

- Two-pane window with per-pane **Actions** menus; per-agent **Launch** and **Resume** buttons.
- Footer actions: add project, settings, exit; version pill in both windows.
- Availability display already exists (dim); mockup also dims/filters unavailable agents.
- Downloads/apply updates with install "from tray" (previously deferred).

## Related changes

- `launch-window-ui` — the split layout and agent actions.
- `preferences-ui` — the Settings window and the new preferences above.
- `main-window-layout` (delivered) — resizable Projects/Agents panes and aligned pane layout; the full dark redesign, per-pane Actions menus, and footer are still upcoming.
- `preferences-ui` (delivered) — the Settings window, default-runtime `cmd`/`ps`/`wt` selection, hotkey capture, agent probe TTL/timeout, and the startup update toggle (light theme rather than the mockup's dark styling).
