## Why

The tray context menu is a plain WPF `ContextMenu` built in `src/CLIHub/Tray/TrayMenuBuilder.cs:19-27`
and attached to the tray icon in `src/CLIHub/Tray/TrayIconController.cs:135-146`. The application
defines no `ContextMenu`, `MenuItem`, or `Separator` styles anywhere (verified: a
`TargetType="(MenuItem|ContextMenu|Separator)"` search over `src/CLIHub/**/*.xaml` returns no
matches), so WPF falls back to the default Aero2 templates: a light system menu surface with the
system hover color, regardless of the Windows apps theme and regardless of CLIHub's own palette. The
user reports the menu's visual theme differs strongly from the Windows theme.

The application already themes every other popup surface at application level — the slim scrollbar
(`src/CLIHub/App.xaml:12-17`) and the dark tooltip (`src/CLIHub/App.xaml:49-53`) — precisely because
window-scoped resources do not reach popup-hosted content. Menus are the remaining unstyled popup.

## What Changes

- **Menu palette from the Windows apps theme only.** Two new dictionaries,
  `src/CLIHub/Themes/SystemMenu/SystemMenuDark.xaml` and `SystemMenuLight.xaml`, define one shared
  key set (`SystemMenu.SurfaceBrush`, `BorderBrush`, `TextBrush`, `HoverOverlayBrush`,
  `PressedOverlayBrush`, `DisabledTextBrush`, `SeparatorBrush`, `GestureTextBrush`) with values
  approximating Windows 11 menu neutrals. The active one is selected by
  `CLIHub.Core.Windows.SystemTheme.IsLight()` (`src/CLIHub.Core.Windows/SystemTheme.cs:17-28`) — the
  same `AppsUseLightTheme` value the "System" theme mode reads — and is independent of the stored
  CLIHub `AppTheme` (Dark/Light/Blue/System) and of the accent color.
- **Application-level implicit menu styles.** A new `src/CLIHub/Themes/Common/SystemMenuStyles.xaml`,
  merged from `App.xaml`, provides implicit styles for `ContextMenu` and `MenuItem` with custom
  `ControlTemplate`s: rounded surface (`Sizing.CornerRadius.Card`, 8), 1px border, inner padding 4,
  subtle in-template drop shadow, neutral hover/pressed overlays as 30-height pills (mirroring the
  in-app Actions menu look at `src/CLIHub/Themes/Common/ActionsMenuStyles.xaml:22-63`), dimmed
  disabled text, a submenu arrow, an icon slot that tints the existing tray glyphs, input-gesture
  text (TextBox menus), and preserved keyboard navigation. Separators inside menus are styled through
  the keyed `MenuItem.SeparatorStyleKey` style — the only style WPF applies to menu separators
  (verified in the WPF reference source, see design). Every WPF menu in the application is covered,
  including the tray menu and the framework TextBox context menus.
- **Live follow, always on.** A new `SystemMenuTheme` service owns the menu palette as one tracked
  overlay in `Application.Resources.MergedDictionaries` (mirroring `PaletteSwitcher`'s
  replace-in-place mechanism, `src/CLIHub/Theming/PaletteSwitcher.cs:68-116`), applied at startup
  from `App.OnStartup` after `ApplyAccent()` (`src/CLIHub/App.xaml.cs:28-29`). A new
  `SystemMenuThemeFollower` — with no stored-theme guard, unlike
  `src/CLIHub/Theming/SystemThemeFollower.cs:141-144` — re-applies it from
  `SystemEvents.UserPreferenceChanged` when the Windows light/dark mode flips, regardless of the
  stored CLIHub theme. `ShellCoordinator` owns its lifecycle exactly as it owns the existing
  follower's (`src/CLIHub/Startup/ShellCoordinator.cs:112,155`).
- **Behavior unchanged.** `TrayMenuBuilder`, menu structure, labels, glyph icons, separator rules,
  commands, and the refresh-on-open behavior stay byte-identical; only the visual presentation
  changes.
- **Spec deltas.** `application-theme` gains two ADDED requirements — "Context menus follow the
  Windows apps theme" (palette rule: Windows-driven, independent of the CLIHub palette, no accent,
  visual only) and "Context menu palette applies live" (startup apply, live re-apply regardless of
  the stored theme, follow ends at shutdown); `tray-menu` gains an ADDED requirement (the tray menu
  renders with the themed styles in both Windows modes; everything else unchanged).

## Impact

- New source files: `src/CLIHub/Themes/SystemMenu/SystemMenuDark.xaml`,
  `src/CLIHub/Themes/SystemMenu/SystemMenuLight.xaml`,
  `src/CLIHub/Themes/Common/SystemMenuStyles.xaml`, `src/CLIHub/Theming/SystemMenuTheme.cs`,
  `src/CLIHub/Theming/SystemMenuThemeFollower.cs`.
- Edited source: `src/CLIHub/App.xaml` (one merge entry + comment), `src/CLIHub/App.xaml.cs` (one
  apply call), `src/CLIHub/Startup/ShellCoordinator.cs` (constructor, start, dispose),
  `src/CLIHub/Composition/ServiceRegistration.cs` (one singleton + factory argument).
- New tests: `tests/CLIHub.Tests/Themes/SystemMenuThemeTests.cs`,
  `SystemMenuThemeFollowerTests.cs`, `SystemMenuStyleTests.cs`.
- Edited tests (guards that must learn the new files): `ThemeColorLiteralTests.cs` (allowlist),
  `ApplicationResourceKeyTests.cs` (dynamic-only key set), `ArchitectureBoundaryTests.cs`
  (WPF-file allow-list for the two new `Theming/` classes).
- Docs: `docs/architecture/ui.md` merge tree and theme-data locations.
- Risk surface: the implicit styles are application-global, so every future WPF menu inherits them —
  that is the intent (decision: full custom styling). No `Core`/`Core.Windows` change except zero:
  `SystemTheme.IsLight()` is consumed as-is.

`skip_specs` is **not** set: two observable behavior deltas ship under
`specs/application-theme/spec.md` and `specs/tray-menu/spec.md`.

## Non-Goals

- Not using the Windows accent color anywhere in the menu (hover/pressed are neutral overlays).
- Not changing the CLIHub palettes (`Themes/Dark|Light|Blue.xaml`), their keys, or
  `PaletteSwitcher` behavior; the menu palette is a separate, Windows-driven pair.
- Not changing menu behavior: no structural, label, icon, separator-rule, command, or refresh
  changes (`TrayMenuBuilder` and `TrayIconController` are untouched).
- Not introducing a preference for the menu theme; the menu follows Windows unconditionally.
- Not restyling `Menu` bars or `ComboBox` dropdowns (the app has none; `ComboBox` uses its own
  popup and is unaffected by `ContextMenu`/`MenuItem` styles).
- Not reproducing the default theme's button-based menu scrolling (`MenuScrollViewer`); the
  application's menus are short and a plain `ScrollViewer` wraps the items.

## Dependencies

None. Self-contained against the current tree; no other change is assumed.
