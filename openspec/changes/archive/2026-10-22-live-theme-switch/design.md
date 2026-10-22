# Design: Live Theme Switch

## Verified current state (commit 13ae4e6)

### Startup

`src/CLIHub/App.xaml.cs:29-32` — `OnStartup` calls the static `ApplyTheme()` and `ApplyAccent()`
**before** `new ApplicationHost()` and before the DI container exists.

`App.ApplyTheme` (`App.xaml.cs:44-57`) reads the stored theme with
`PreferenceReader.ReadAppTheme(AppPaths.ConfigFile)`, resolves it with
`ThemeCatalog.ResolvePalettePath(theme, SystemTheme.IsLight())`, and merges the palette only when the
result is non-null:

```csharp
application.Resources.MergedDictionaries.Add(new ResourceDictionary
{
    Source = new Uri($"pack://application:,,,/CLIHub;component{palettePath}", UriKind.Absolute)
});
```

Dark, System-on-dark, and unknown themes return `null` and keep the dark base merged by
`App.xaml:11`. No reference to the added dictionary is kept, so it cannot be removed or replaced.

### Save path

`src/CLIHub/Settings/SettingsApplicationService.cs:439-511` — `Save(SettingsDraft)` loads
`previousPreferences`, applies start-with-Windows, runtime, path-display style, accent, and hotkey
through `IPreferenceApplier`, persists the draft (`preferences.AppTheme = draft.AppTheme` at `:481`),
reloads the project scanner, and rolls back runtime / path-display / accent / hotkey / start-with-
Windows on failure (`Rollback`, `:512-520`). **It never applies the theme**, and no restart prompt
exists anywhere in the code base (grep-verified). A changed theme appears on the next start only.

`SettingsDraft` (`:92-120`) already carries `AppTheme`, so no draft or view-model change is needed.

### Preference chain

`src/CLIHub/Settings/IPreferenceApplier.cs` — members: `ApplyRuntime`, `ApplyPathDisplayStyle`,
`ApplyHotkey`, `ApplyStartupUpdateCheck`, `ApplyStartWithWindows`, `ApplyAccent`. The only
implementation is `PreferenceApplier` (`src/CLIHub/Settings/PreferenceApplier.cs`), which forwards the
accent to `IAccentPreferenceTarget`; `WpfAccentPreferenceTarget` writes
`Application.Current.Resources["AccentBrush"]` and `["AccentSelectionBrush"]` as local entries.

DI: `src/CLIHub/Composition/ServiceRegistration.cs:157-159` registers `IPreferenceApplier`,
`IRuntimePreferenceTarget`, and `IAccentPreferenceTarget` inside `AddSettingsServices`.

### Resource resolution

- Palette brushes are consumed **exclusively** through `DynamicResource` (verified: zero
  `StaticResource` references to any palette key across the XAML tree), so a swapped palette
  dictionary re-evaluates in every open window without a per-window refresh.
- Local `Application.Resources` entries shadow merged dictionaries; merged dictionaries are searched
  newest-last-first, and only the palette dictionaries define palette keys.
- The application-level implicit styles (tooltip, slim scrollbar) resolve their brushes through
  `DynamicResource` as well.

### Logging

Serilog's static `Log.Logger` is configured by `ApplicationHost.ConfigureLogging`, which runs **after**
`App.OnStartup`'s theme apply. A palette failure at startup therefore has no file logger yet
(`Log.Logger` is Serilog's silent default until configured). The design must not require `ILogger`
before the container exists.

### Spec contract

`openspec/specs/application-theme/spec.md`: "Theme palette resolution" (resolve before any window is
created; System resolves to dark or light only), "Theme applies on next start" (scenario "Change
persists for next start"), "Accent applies live".

## D1 — Palette slot: `PaletteSwitcher`

New `src/CLIHub/Themes/PaletteSwitcher.cs`:

```csharp
public sealed class PaletteSwitcher
{
    public static PaletteSwitcher Shared { get; } = new();
    private ResourceDictionary? overlay;                 // null = dark base only
    internal PaletteSwitcher() { }
    public bool Apply(IList<ResourceDictionary> merged, AppTheme theme, bool systemPrefersLight);
}
```

`Apply`:

1. `path = ThemeCatalog.ResolvePalettePath(theme, systemPrefersLight)`.
2. Build the candidate dictionary, if any, **before** touching the collection:
   `new ResourceDictionary { Source = new Uri($"pack://application:,,,/CLIHub;component{path}", UriKind.Absolute) }`.
3. If `path is null`: remove `this.overlay` when it is still in `merged`; clear the field; return true.
4. Otherwise: when `this.overlay` is still in `merged`, replace it at its index; else append. Store the
   new dictionary in `this.overlay`; return true.
5. Any exception while building the dictionary: `Log.Warning`, leave `merged` untouched, return false.

Decisions:

- **Track the instance, not an index.** `merged.IndexOf(overlay)` keeps the swap correct if code ever
  inserts or removes another dictionary; a fixed index silently corrupts the slot. Appending is safe
  because only palette dictionaries define palette keys, so the newest palette still wins.
- **One shared instance.** Startup applies the theme before the container exists, so `App` must reach
  the same tracker the DI-resolved target uses. Two instances would each hold their own overlay and
  leak the first one on the initial switch. `Shared` is the single tracker; the constructor is
  `internal` so tests can create isolated instances.
- **No locking.** All calls happen on the UI thread (startup and Save both run on the dispatcher).
  Documented on the type.
- **Failure means "keep what is on screen".** A palette file that is missing or malformed must never
  produce a half-applied theme; the previous overlay stays.

## D2 — Preference chain wiring

- New `IThemePreferenceTarget { void SetTheme(AppTheme theme); }` beside `IAccentPreferenceTarget`.
- New `WpfThemePreferenceTarget` (same folder, same shape as `WpfAccentPreferenceTarget`) calls
  `PaletteSwitcher.Shared.Apply(Application.Current.Resources.MergedDictionaries, theme, SystemTheme.IsLight())`
  and logs through `ILogger<WpfThemePreferenceTarget>` (available after the container is built).
- `IPreferenceApplier` gains `void ApplyTheme(AppTheme theme);`; `PreferenceApplier` takes
  `IThemePreferenceTarget` in its constructor and forwards, logging like `ApplyAccent`.
- DI: register `IThemePreferenceTarget` → `WpfThemePreferenceTarget` beside the accent target, and
  register `PaletteSwitcher.Shared` as a singleton for the target's constructor.
- `SettingsApplicationService.Save`: call `this.applier.ApplyTheme(draft.AppTheme);` immediately after
  `ApplyAccent(draft.AccentColor)` and before `preferencesStore.Update(...)`; add
  `TryRollback("theme", () => this.applier.ApplyTheme(previous.AppTheme));` to `Rollback`.

Rejected: injecting `IThemePreferenceTarget` into `SettingsApplicationService` directly. It would give
the service two apply channels and break the single-applier pattern that the accent already follows.

## D3 — Startup uses the same path

`App.ApplyTheme` keeps its preference read and becomes:

```csharp
var theme = AppThemes.Parse(PreferenceReader.ReadAppTheme(AppPaths.ConfigFile));
if (Current is { } application)
{
    PaletteSwitcher.Shared.Apply(application.Resources.MergedDictionaries, theme, SystemTheme.IsLight());
}
```

No DI dependency, no duplicated merge logic, identical behavior for the dark base.

Rejected: resolving the target from the container in `OnStartup`. The container is built inside
`ApplicationHost.Start`, i.e. after the palette must already be applied (windows are created during
`Start`).

## D4 — Accent behavior across a live switch

No accent re-application is added, and the reasoning is verified:

- After any settings save, `WpfAccentPreferenceTarget.SetAccent` writes **both** accent keys as local
  `Application.Resources` entries. Local entries shadow merged dictionaries, so a palette swap cannot
  change the accent that the user last saved — exactly the required precedence.
- At startup, when the stored accent equals `AccentPalette.DefaultHex`, `App.ApplyAccent` writes
  nothing, so the accent comes from the palette dictionary and therefore follows a palette swap
  (dark/light bake Sky, blue bakes Blue). That matches the spec's baked-accent rule.
- The existing asymmetry (a save always pins the accent locally, even when it equals the default) is
  pre-existing behavior outside this change's scope; it is recorded in `docs/backlog.md`.

## D5 — Spec delta structure: REMOVED + ADDED

The requirement being changed is named after the old behavior. `openspec` resolves a MODIFIED section
by its header against the base spec, so a MODIFIED entry either keeps the misleading header or fails
archive validation. The delta therefore:

- **REMOVES** "Theme applies on next start" (`**Reason**` + `**Migration**`), and
- **ADDS** "Theme applies immediately" with the persistence, System-resolution, cancel, and
  failure-retention scenarios.

Requirement text stays behavioral (what the user observes), not structural (which method runs).

## Risks

| Risk | Mitigation |
| --- | --- |
| A palette fails to load at save, so the user sees no change although the preference persisted | Switcher returns false, WPF target logs a warning; the next start retries and falls back to the dark base. Documented in the spec scenario. |
| Another component clears or replaces `Application.Resources.MergedDictionaries`, orphaning the tracked overlay | `IndexOf` guard: a missing overlay is treated as "no overlay" and a new one is appended instead of throwing. Unsupported mutation is documented on the type. |
| Static `Shared` state leaks between tests | Constructor is `internal`; tests use fresh instances against a local `ResourceDictionaryCollection`. |
| Settings window (or another open window) keeps stale colors | No `StaticResource` palette references exist; `DynamicResource` re-resolves. Manual smoke covers three open windows. |
| A save rolls back after a successful theme swap (e.g. hotkey registration fails) | `Rollback` restores the previous theme through the same applier. |

## Open questions

- Should Windows personalization changes be followed live while the application runs? Parked; a
  registry or `SystemEvents.UserPreferenceChanged` subscription would call the same switcher.
- Should the Appearance page preview cards reflect the *selected* (unsaved) theme instead of the
  active one? Unchanged by this design; the cards are per-theme previews, not the live palette.
