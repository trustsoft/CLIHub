# Proposal

## Why

Tooltips in the launch window and the What's New window still use the default light system style, which clashes with the dark drawn chrome both windows use. The window already has an established dark popover language (the Actions context menus), so tooltips should speak the same visual language instead of flashing a light system popup over a dark window.

## What Changes

- Add a dark tooltip style to the theme, mirroring the existing dark popover style used by the Actions context menus: popover background, hairline field stroke, rounded corners, and a soft drop shadow.
- Apply the style to all tooltips in the launch window (agent Launch/Resume actions, footer buttons, the pin toggle) and the What's New window (install action, close button).
- Share the style between the dark-chrome windows through the window-scoped theme resources, following the existing "palette is global, styles are window-scoped" convention.
- Keep tooltip timing and placement behavior at the platform default; only the visuals change.
- The tray icon's hover tooltip is shell-drawn and out of scope.

## Capabilities

### New Capabilities

<!-- None: tooltips are part of the launch window's dark theme. -->

### Modified Capabilities

- `launch-window-theme`: add a requirement that tooltips shown over the launch window use the dark popover style consistent with the window's chrome, instead of the system light style.

## Impact

- **Modified:** `src/CLIHub/Themes/LaunchWindowStyles.xaml` (new `DarkToolTip` style or a shared dictionary holding it), `src/CLIHub/Windows/LaunchWindow.xaml` (tooltip style references), `src/CLIHub/Windows/WhatsNewWindow.xaml` + `src/CLIHub/Themes/WhatsNewStyles.xaml` (same style merged into the window's resources), possibly a new shared `src/CLIHub/Themes/` dictionary if the style is factored out.
- **Tests:** none new — pure XAML styling, verified by the existing build and manual visual inspection.
- **Dependencies:** none added.
- **Not included:** the tray hover tooltip (OS shell-drawn, not styleable from WPF); Settings window (keeps its light styling per the `launch-window-theme` scope; it defines no tooltips today); custom tooltip timing, placement, or animations.
