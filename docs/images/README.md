# README Images

Assets used by the repository [README](../../README.md). The source HTML lives next to the rendered
PNGs so the banner can be edited and regenerated without design tools.

| Image | Source | Purpose |
|---|---|---|
| `banner.png` | `src/banner.html` + `launch-window.png` | Repository banner: identity, tagline, the default global hotkey, and the launch window as a translucent background. |
| `social-preview.png` | `src/social-preview.html` + `launch-window.png` | Repository social preview (1280 × 640) for the GitHub link card. Not referenced by the README. |
| `workflow.png` | `src/workflow.html` | Three-step product summary: project, agent, terminal. |
| `launch-window.png` | App capture | The launch window with the Projects and Agents panes. |
| `settings-appearance.png` | App capture | Settings → Appearance (theme, accent color, and path display). |
| `settings-discovery.png` | App capture | Settings → Project Scanning (watched roots, scanning rules, watcher status). |
| `settings-agents.png` | App capture | Settings → CLI Agents (installed agents, default agent, detection settings). |

## Regenerate the banner and workflow

The HTML pages are 1280×360 and 1200×272 CSS pixels; render them at 1.5× for a crisp result on
high-density displays:

```powershell
& "C:\Program Files\Google\Chrome\Application\chrome.exe" --headless=new --disable-gpu `
  --hide-scrollbars --force-device-scale-factor=1.5 --window-size=1280,360 `
  --virtual-time-budget=3000 --screenshot="docs\images\banner.png" `
  "file:///<repo>/docs/images/src/banner.html"

& "C:\Program Files\Google\Chrome\Application\chrome.exe" --headless=new --disable-gpu `
  --hide-scrollbars --force-device-scale-factor=1.5 --window-size=1200,272 `
  --virtual-time-budget=3000 --screenshot="docs\images\workflow.png" `
  "file:///<repo>/docs/images/src/workflow.html"
```

## Social preview

`social-preview.png` follows GitHub's social-preview recommendation: 1280 × 640 px, PNG under 1 MB.
Render `src/social-preview.html` at 2× and downscale the result to exactly 1280 × 640 so the text
stays sharp:

```powershell
# Render at 2x, then resample to 1280x640 with a high-quality bicubic filter.
& "C:\Program Files\Google\Chrome\Application\chrome.exe" --headless=new --disable-gpu `
  --hide-scrollbars --force-device-scale-factor=2 --window-size=1280,640 `
  --virtual-time-budget=3000 --screenshot="docs\images\social-preview-2x.png" `
  "file:///<repo>/docs/images/src/social-preview.html"
```

Upload the final image in the repository's **Settings → Social preview**. GitHub accepts PNG, JPG,
or GIF files under 1 MB.

## Replace the app screenshots

Capture the windows from a running build at 150% display scaling, then crop the two-pixel DWM frame
ring and apply a 12 px rounded-corner mask. The README uses the launch window plus the Appearance,
Project Scanning, and CLI Agents settings pages. Capture them against a throwaway configuration with
illustrative project names and watched directories so the screenshots do not expose private workspaces.
