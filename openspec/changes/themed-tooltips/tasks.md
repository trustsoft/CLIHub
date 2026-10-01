# Tasks

## 1. Theme

- [x] 1.1 Create `src/CLIHub/Themes/DarkTooltips.xaml` with the `DarkToolTip` keyed style (dark popover template mirroring `DarkContextMenu`: PopoverBgBrush background, FieldStrokeBrush border, Chunky corner radius, subtle drop shadow, light text) and verify the project builds

## 2. Apply

- [x] 2.1 Merge `DarkTooltips.xaml` into `LaunchWindowStyles.xaml` and apply `Style="{StaticResource DarkToolTip}"` to the launch window's tooltips (Launch, Resume session, Open data folder, Settings, pin toggle, Exit); verify the solution builds
- [x] 2.2 Merge `DarkTooltips.xaml` into `WhatsNewStyles.xaml` and apply the style to the What's New tooltips (install action, Close); verify the solution builds

## 3. Verification

- [x] 3.1 Run `dotnet build CLIHub.sln` and confirm all tests still pass
- [x] 3.2 Manually verify: hover each launch-window control (agent Launch/Resume buttons, footer buttons, pin toggle) and the What's New buttons — each tooltip appears as a dark rounded popover with readable light text, consistent with the Actions menus; check at 100% and 150% display scaling (drop or soften the shadow if it renders poorly); confirm the Settings window and tray hover tooltip are unchanged
