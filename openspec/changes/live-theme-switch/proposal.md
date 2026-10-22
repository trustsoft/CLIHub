# Live Theme Switch

## Why

Changing the theme preference today only takes effect on the next application start. The Settings
Save path applies the runtime, path-display, accent, hotkey, and start-with-Windows preferences
immediately (`SettingsApplicationService.Save`), but it never applies the theme: it writes
`preferences.AppTheme` and the running windows keep the palette they were created with. There is no
restart prompt and no error — the new theme simply appears the next time CLIHub starts, which makes
the Appearance page feel broken next to the accent picker, which does apply live.

The deferral is not a WPF limitation. Every palette brush is consumed through `DynamicResource`, so
replacing the palette dictionary in the application resources reaches every open window with no
per-window refresh code. Only the "apply once at startup" implementation stands in the way.

## What Changes

**Stage 1 — runtime palette slot.** Add a `PaletteSwitcher` in the WPF layer that owns the single
replaceable palette overlay in `Application.Current.Resources.MergedDictionaries`: the dark palette
stays the base merged by `App.xaml`; a resolved light or blue palette is swapped in as one tracked
overlay, and a null resolution (dark, system-on-dark, unknown) removes that overlay. Startup
(`App.ApplyTheme`) and the Settings Save path then share the same apply code, so the two cannot
drift. A palette that cannot be loaded leaves the current palette untouched and is logged; the saved
preference still persists.

Wire the apply into the existing preference chain: a new `IThemePreferenceTarget` (sibling of
`IAccentPreferenceTarget`) implemented by a WPF target, forwarded by `PreferenceApplier`, called from
`SettingsApplicationService.Save` next to the accent, and rolled back with the other preferences when
a later step of the save fails.

**Stage 2 — UI text, documentation, and the spec contract.** Update the Appearance page's theme hint
(currently "Applies after the next start"), the theming recipe in `docs/architecture/ui.md` (which
states that switching the palette needs a restart), and the `application-theme` spec, whose
requirement "Theme applies on next start" is replaced by "Theme applies immediately".

## Impact

- **Users**: selecting a theme and saving updates every open window at once, like the accent picker.
  The no-feedback restart dependency disappears.
- **Startup**: `App.OnStartup` no longer contains palette-merging logic of its own; it calls the same
  switcher the Save path uses.
- **Theming**: a future theme picker (or an OS-theme subscription) reuses the switcher instead of
  inventing a second apply path.
- **Tests**: the switcher is a plain class over a `MergedDictionaries` collection, so it is unit
  testable without a running window.

## Non-goals

- Subscribing to Windows personalization changes while CLIHub runs (the System mode resolves at
  startup and at save; live OS switching is parked in `docs/backlog.md`).
- Animated theme transitions.
- Per-window theme overrides.
- Changing the accent precedence rule: a stored accent still overrides the palette's baked accent.
- Editing the legacy `MainWindow.xaml`.

## Dependencies

- Independent of `theme-xaml-structure`. If that change lands first, the palette path comes from the
  theme descriptor; this change only reads `ThemeCatalog.ResolvePalettePath`, so the switch is
  transparent.
