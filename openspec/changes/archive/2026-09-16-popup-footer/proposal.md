# Proposal: Popup Footer

## Why

The popup footer is a stub: a static "CLIHub" label on the left and the status
text on the right. The approved UI mockup (`docs/ui/popup-split.png`) and
`docs/ui.md` define the footer as brand + version plus system actions
(open data folder, settings, exit). Today there is no in-popup way to see the
app version, reach the data folder, or exit the app.

## What Changes

- Rebuild the footer layout to match the mockup: brand + version pill on the
  left, system action icons on the right; the status text moves to the footer
  center (behavior unchanged). The static brand-only label is removed.
- Version display: show the application version (Velopack app version, with a
  fallback to the assembly informational version when running unpackaged via
  `dotnet run`).
- Add an "open data folder" action: opens `%AppData%\CLIHub\` (the stable data
  folder per `docs/vision.md`, not the install directory) in Explorer.
- Add a "settings" action placeholder: the icon is present but disabled until
  the settings surface exists (that is a separate future change).
- Add an "exit" action: shuts the application down. The tray menu keeps its own
  "Exit" item.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `popup`: adds a Footer requirement — brand + version display, footer actions
  (open data folder, exit; settings disabled until the settings surface
  exists), and the status text position in the footer center.

## Impact

- `src/CLIHub.App/Views/PopupWindow.xaml` — footer layout and icon buttons.
- `src/CLIHub.App/ViewModels/PopupViewModel.cs` — version property and footer
  commands.
- `src/CLIHub.App/App.xaml.cs` — composition root: version source and command
  wiring.
- Possibly a small App-layer version provider (see design.md).
- No `src/CLIHub.Core` behavior changes expected.
- `docs/ui.md` implementation snapshot is updated after implementation.
