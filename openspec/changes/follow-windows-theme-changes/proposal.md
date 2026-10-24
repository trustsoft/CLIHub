# Follow Windows personalization changes while the application runs

## Why

The "System" theme mode resolves the Windows apps-use-light setting only at two moments: startup
(`App.ApplyTheme`, `src/CLIHub/App.xaml.cs:49`) and a Settings save (`WpfThemePreferenceTarget.SetTheme`,
`src/CLIHub/Settings/WpfThemePreferenceTarget.cs:39`). Those are exactly the two production callers of
`PaletteSwitcher.Apply` (`src/CLIHub/Themes/PaletteSwitcher.cs:68`). Nothing re-resolves the palette when
the user switches Windows between light and dark afterwards, so a "System"-synced CLIHub stays on the old
palette until the next restart or the next Settings save. This is parked as
"Windows personalization changes are not followed while the application runs"
(`docs/backlog.md:375-386`, severity Medium), and the `application-theme` purpose already promises
palettes that "can follow the Windows personalization setting" — following currently stops at the apply
moment.

## What Changes

**Behavior:**
- While the stored theme is "System" and the application runs, a change of the Windows
  apps-use-light-theme setting re-resolves the effective theme and reapplies the matching dark or light
  palette to every open window, without a restart. "System" still never resolves to blue.
- When the stored theme is "Dark", "Light", or "Blue", a Windows personalization change is a no-op for
  the palette.
- The personalization subscription lives only for the running first-instance session and is removed at
  shutdown.

**Code:**
- A new `SystemThemeFollower` in `src/CLIHub/Themes/` subscribes to
  `Microsoft.Win32.SystemEvents.UserPreferenceChanged`, filters with two cheap value guards
  (stored theme is "System"; the freshly read `SystemTheme.IsLight()` differs from the last observed
  mode), and re-applies the palette through the existing shared `PaletteSwitcher` — the same mechanism
  startup and save already use; no second palette application path.
- The subscription is owned by `ShellCoordinator`, the existing event-wiring owner of the first-instance
  session (`openspec/specs/app-lifecycle/spec.md:206-225`): started in its shell-initialization wiring
  with `ApplicationStartupContext.Dispatch` (the UI-thread marshal), disposed with the coordinator during
  session disposal. Registered as a DI singleton alongside the other startup services.
- Spec delta: the `application-theme` requirement "Theme applies immediately" gains the
  follow-while-running contract (original scenario names preserved, new scenarios added).

## Impact

**User-visible:**
- With theme "System", switching Windows personalization repaints the launch window, Settings window,
  and any open popups/tooltips within the session. Nothing else changes; no new settings.

**Internal:**
- One new class (`SystemThemeFollower`), one new DI registration, and one added constructor dependency
  each on `ApplicationSession` and `ShellCoordinator`. `PaletteSwitcher`, `ThemeCatalog`,
  `SystemTheme`, `PreferenceReader`, and the Settings save path are untouched.
- Second instances never subscribe (no session is created for them,
  `src/CLIHub/Startup/ApplicationHost.cs:51-56`).

**Tests:**
- New unit tests for the follower's decision logic (guards, subscribe/unsubscribe, dispatch) using an
  internal test seam that follows the `PaletteSwitcher` constructor-seam pattern; the coordinator and
  session lifecycle tests are extended to cover start/dispose wiring. The real palette swap under a live
  `Application` stays manual smoke, per the documented precedent in
  `tests/CLIHub.Tests/Services/WpfThemePreferenceTargetTests.cs:14-17` (the test host has no
  `Application`).

## Non-goals

- Live reaction to Windows accent-color changes: the accent is application-scoped and stays applied at
  startup and on save only.
- Following the light/dark theme of other applications, or per-window themes.
- Any Settings page UX change: no new options, no copy changes on the "System Sync" card.
- A hand-rolled registry watcher (`RegNotifyChangeKeyValue`) — the managed `SystemEvents` broadcast is
  used instead.
- Polling or timers: the palette re-resolves only at startup, on save, and on an actual personalization
  change.
- Watching the Windows *system* (taskbar) light/dark mode: only the apps-use-light setting
  (`AppsUseLightTheme`) is followed, as today.

## Dependencies

None. Self-contained within the theming and startup-lifetime subsystems, and builds on the
`accent-independent-of-theme` model (a palette swap never touches the accent, so following needs no
accent work).
