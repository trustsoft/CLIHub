# README Images

Assets used by the repository [README](../../README.md). The source HTML lives next to the rendered
PNGs so the banner can be edited and regenerated without design tools.

| Image | Source | Purpose |
|---|---|---|
| `banner.png` | `src/banner.html` + `launch-window.png` | Repository banner: identity, tagline, the default global hotkey, and the launch window as a translucent background. |
| `social-preview.png` | `src/social-preview.html` + `launch-window.png` | Repository social preview (1280 × 640) for the GitHub link card. Not referenced by the README. |
| `workflow.png` | `src/workflow.html` | Three-step product summary: project, agent, terminal. |
| `launch-window.png` | App capture (`tools\Capture-ReadmeImages.ps1`) | The launch window with the Projects and Agents panes. |
| `settings-appearance.png` | App capture (`tools\Capture-ReadmeImages.ps1`) | Settings → Appearance (theme, accent color, and path display). |
| `settings-discovery.png` | App capture (`tools\Capture-ReadmeImages.ps1`) | Settings → Project Scanning (watched roots, scanning rules, watcher status). |
| `settings-agents.png` | App capture (`tools\Capture-ReadmeImages.ps1`) | Settings → CLI Agents (installed agents, default agent, detection settings). |

## Regenerate all README images

The app captures are rendered in-process by the debug build, so the harness works on a locked or
headless desktop where screen-copy and `PrintWindow` return black frames. It builds the Debug app,
seeds a throwaway configuration and mock workspace, captures each window at 144 DPI (1.5×), applies
the 12 px rounded-corner mask, regenerates the banner, workflow, and social preview with headless
Chrome, then restores the original configuration and logo cache.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File docs\images\tools\Capture-ReadmeImages.ps1
```

The script refuses to run while CLIHub is running. Useful parameters:

- `-MockWorkspaceRoot` — illustrative workspace used by the projects and the watched root
  (default `D:\Projects\Acme`). Point it at a disposable folder; the script only deletes a root it
  created itself.
- `-OutputDirectory` — image directory (default `docs\images`).
- `-SkipBuild` — reuse the existing Debug build.
- `-SkipWebImages` — keep `banner.png`, `workflow.png`, and `social-preview.png` unchanged.
- `-KeepMockWorkspace` — keep the mock workspace when the script created it.

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

The app captures are produced by `tools\Capture-ReadmeImages.ps1` through the debug-only
`CLIHUB_CAPTURE_DIR` mode (`src/CLIHub/Views/ReadmeImageCapture.cs`). The debug build renders
the launch window and the Appearance, Project Scanning, and CLI Agents Settings pages with
`RenderTargetBitmap` at 144 DPI (1.5×), applies the 12 px rounded-corner mask, and shuts down. The
render path has no DWM frame ring to crop, which is why the harness does not perform the old
two-pixel crop.

The harness seeds a throwaway configuration with illustrative project names and a watched
directory, backs up and restores `%APPDATA%\CLIHub\config.json` and `cache\logos.json`, and creates
only mock project folders for the workspace. Run it from an unlocked desktop or a headless session;
no 150% display scaling is required because the render target is DPI-independent.
