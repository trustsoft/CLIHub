# Design: Popup Footer

## Context

The footer is a two-cell Grid: a static `CLIHub` TextBlock and the
`StatusText` TextBlock, both in the same cell. The mockup
(`docs/ui/popup-split.png`) defines the target: brand + version pill on the
left, system actions (folder, settings, power) on the right. `docs/vision.md`
fixes that the folder action opens `%AppData%\CLIHub\` (the stable data
folder), not the install directory. The settings surface does not exist yet.

## Goals / Non-Goals

**Goals:**

- Footer layout per the mockup: brand + version pill (left), status (center),
  three action icons (right).
- Version from Velopack when installed, assembly informational version
  otherwise (`dotnet run`).
- Working open-data-folder and exit actions; visible-but-disabled settings.

**Non-Goals:**

- The settings window itself (separate future change).
- Toast/overlay notifications for status (StatusText behavior stays as is).
- Dark theme, window chrome, or any restyling beyond the footer strip.

## Decisions

### D1. Layout: three-zone Grid

Footer becomes a Grid with columns `Auto | * | Auto`: brand + version pill,
then the centered status text (`TextTrimming=CharacterEllipsis`), then a
horizontal StackPanel of three icon buttons. `StatusText` binding moves to the
center cell unchanged. Rationale: the current one-cell overlap disappears;
center status matches the mockup's empty middle. Alternative (status left,
brand dropped) rejected — the mockup fixes brand on the left.

### D2. Version source: Velopack install manifest first, assembly fallback

Resolve a version string once at startup in `App.OnStartup`: reuse
`VelopackUpdateClient.CreateManager()` and read `UpdateManager.CurrentVersion`
when `UpdateManager.IsInstalled` (the installed Velopack manifest is the only
authoritative version for a packaged app, since vpk stamps the package version
independently of the compiled assemblies); when not installed (`dotnet run`),
fall back to the entry assembly's `AssemblyInformationalVersion`. The resolved
string is passed into `PopupViewModel` as a plain `string appVersion`
constructor argument — no new abstraction, since the value is immutable and
display-only. Rationale: the Velopack package is referenced only by
`CLIHub.App`, so version resolution belongs there. Note: `VelopackRuntimeInfo`
version properties are compile-time constants of the Velopack library itself
(they would report the package version 1.2.0) and are deliberately not used.

### D3. Icons: Segoe MDL2 Assets glyphs

Icon buttons use font glyphs from Segoe MDL2 Assets (folder `E8B7`, settings
`E713`, power `E7E8`) in TextBlocks inside flat buttons. Rationale: no new
image assets, crisp at any DPI, consistent with a borderless dark-on-light
strip. Alternative (PNG assets per plugin-logo pattern) rejected — bitmaps
scale worse and add assets for chrome icons.

### D4. Open data folder: delegate injection, existing pattern

`PopupViewModel` already receives UI-delegates from the composition root
(`pickFolder`, `confirm`, `postToUi`). Follow the same pattern: inject
`Action openDataFolder`, wired in `App.OnStartup` to
`_processRunner.StartDetached("explorer.exe", paths.ConfigDirectory, ...)`.
`IPathProvider.ConfigDirectory` already resolves `%AppData%\CLIHub`.
Rationale: keeps process spawning in the App layer; the VM stays testable and
the pattern is already established.

### D5. Exit: `ExitRequested` event on the ViewModel

The ViewModel raises `ExitRequested` (mirroring the existing
`CloseRequested`); `App` subscribes and calls `Shutdown()`. Rationale: the VM
must not reference `Application.Current`; the event mirrors the established
`CloseRequested` wiring. The tray menu keeps its own Exit item unchanged.

### D6. Settings: present, disabled

The settings icon button is rendered with `IsEnabled=False` (no command wired)
and a tooltip ("Настройки — скоро"). It is enabled by the future settings-window
change. Rationale: the mockup fixes three icons; a disabled control honestly
communicates "not yet" without dead-click behavior.

## Risks / Trade-offs

- [Velopack version API differs between releases] → Verify
  `VelopackRuntimeInfo` members against Velopack 1.2.0 during implementation;
  the assembly-informational fallback bounds the failure to a wrong string,
  never a crash.
- [Segoe MDL2 glyph gaps on exotic Windows builds] → Glyphs exist since
  Windows 10; fallback is a visible empty box, acceptable for an internal
  tool; can switch to PNG later without layout changes.
- [Long status text crowds the center column] → `CharacterEllipsis` trimming;
  the side columns are `Auto` and win the space contest by design.
- [explorer.exe open fails (rare)] → Fire-and-forget like agent launches; no
  error surface in this change.

## Migration Plan

No data or config migration. Rollback is a single revert: the footer is
self-contained in `PopupWindow.xaml` + VM wiring.
