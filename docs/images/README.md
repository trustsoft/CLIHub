# README Images

Assets used by the repository [README](../../README.md). The source HTML lives next to the rendered
PNGs so the banner can be edited and regenerated without design tools.

| Image | Source | Purpose |
|---|---|---|
| `banner.png` | `src/banner.html` + `launch-window.png` | Repository banner: identity, tagline, the default global hotkey, and the launch window as a translucent background. |
| `workflow.png` | `src/workflow.html` | Three-step product summary: project, agent, terminal. |
| `launch-window.png` | App capture | The launch window with the Projects and Agents panes. |
| `settings.png` | App capture | Settings → Appearance (theme, accent color, and path display). |

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

## Replace the app screenshots

Capture the windows from a running build at 150% display scaling, then crop the two-pixel DWM frame
ring and apply a 12 px rounded-corner mask. Use illustrative project names in the configuration so the
screenshots do not expose private workspaces.
