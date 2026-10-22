# Design

## Verified Current State

### Palette accent keys (to be removed)

- `src/CLIHub/Themes/DarkTheme.xaml:29` — `AccentSelectionBrush` `#1A4CC2FF`
- `src/CLIHub/Themes/DarkTheme.xaml:47` — `AccentBrush` `#FF4CC2FF`
- `src/CLIHub/Themes/LightTheme.xaml:27` — `AccentSelectionBrush` `#1F4CC2FF`
- `src/CLIHub/Themes/LightTheme.xaml:41` — `AccentBrush` `#FF4CC2FF`
- `src/CLIHub/Themes/BlueTheme.xaml:31` — `AccentSelectionBrush` `#1A3B82F6`
- `src/CLIHub/Themes/BlueTheme.xaml:49` — `AccentBrush` `#FF3B82F6`
- `src/CLIHub/Themes/BlueTheme.xaml:5-8` — documents the baked-in Blue accent; must be updated

### Startup accent application

- `src/CLIHub/App.xaml.cs:57-67` — `ApplyAccent` normalizes the stored accent and returns early when it equals `AccentPalette.DefaultHex`
- `src/CLIHub/App.xaml.cs:65-66` — writes `AccentBrush` (opaque) and `AccentSelectionBrush` (tint) into `application.Resources`

### Settings-save accent application

- `src/CLIHub/Settings/WpfAccentPreferenceTarget.cs:14-23` — `SetAccent` unconditionally writes both keys into `application.Resources`
- `src/CLIHub/Settings/SettingsApplicationService.cs:458` — applies accent before theme
- `src/CLIHub/Settings/SettingsApplicationService.cs:518` — rollback replays accent before theme

### Accent model and helpers

- `src/CLIHub.Core/Models/Appearance/AccentPalette.cs:18` — `DefaultHex` = `"#4CC2FF"`
- `src/CLIHub.Core/Models/Appearance/AccentPalette.cs:45-72` — `TryNormalize` accepts optional `#` and alpha pair, returns `#RRGGBB` uppercase
- `src/CLIHub.Core/Models/Appearance/AccentPalette.cs:79` — `Normalize` wraps `TryNormalize` with a default fallback
- `src/CLIHub.Core/Models/Appearance/AccentPalette.cs:86` — `SelectionTint` returns `#1F` + hex (the alpha unifies to this constant)

### Theme picker description

- `src/CLIHub/Themes/ThemeDescriptors.cs:60-66` — Blue theme descriptor with description "Blue-tinted dark chrome with a baked-in blue accent"
- `src/CLIHub/Themes/ThemeDescriptors.cs:30-33` — documents that the preview dot tracks the live accent (matches the new model)

### Palette switching (unaffected by accent keys)

- `src/CLIHub/Themes/PaletteSwitcher.cs:68-116` — `Apply` merges/removes the single tracked palette overlay; does not touch accent keys

### Test guards

- `tests/CLIHub.Tests/Themes/XamlResourceReferenceTests.cs:52-59` — `RuntimeProvidedKeys` lists keys written by code (currently only `PathToImage`)
- `tests/CLIHub.Tests/Themes/ApplicationResourceKeyTests.cs:37-97` — derives StaticResource ban from palette key set
- `tests/CLIHub.Tests/Themes/ThemePaletteParityTests.cs:8-33` — asserts all palettes define identical key sets
- `tests/CLIHub.Tests/Services/WpfThemePreferenceTargetTests.cs:14-17,24` — test host has no `Application.Current`

## Decisions

### D1: Palettes declare no accent keys

All three palette files (`DarkTheme.xaml`, `LightTheme.xaml`, `BlueTheme.xaml`) will have their `AccentBrush` and `AccentSelectionBrush` declarations removed. The XML comments in `BlueTheme.xaml:5-8` documenting the baked Blue accent will be rewritten to state that the palette defines chrome only and the application-level accent applies independently.

**Rationale:** The accent is application-scoped, not palette-scoped. Palettes define chrome and surface brushes only.

### D2: One shared accent applier used by startup and save

A new internal static class `AccentApplier` in `src/CLIHub/Settings/` will encapsulate the accent-write logic with a testable seam following the `PaletteSwitcher` pattern:
```csharp
internal static class AccentApplier
{
    public static void Apply(ResourceDictionary resources, string accentHex);
    internal static void Apply(ResourceDictionary resources, string accentHex, Func<string, SolidColorBrush> createBrush);
}
```

The parameter is `Application.Resources` itself, a `ResourceDictionary`, and the applier assigns `resources["AccentBrush"]` / `resources["AccentSelectionBrush"]`. It must NOT take `Application.Resources.MergedDictionaries`: that is a collection of dictionaries, has no keyed indexer, and local entries in `Application.Resources` are precisely what shadows merged palettes. The public overload normalizes through `AccentPalette.Normalize` and uses the frozen WPF brush factory; the internal overload is the test seam. Both `App.ApplyAccent` and `WpfAccentPreferenceTarget.SetAccent` will delegate to `AccentApplier.Apply`.

**Rationale:** DRY, testability following the established pattern, and guaranteed parity between startup and save.

### D3: Always apply (no default-skip branch)

Both the startup path and the settings-save path will unconditionally normalize and write the accent keys. The `if (accent == AccentPalette.DefaultHex) return;` branch in `App.ApplyAccent:60-63` is removed. The stored accent (normalized) is always the source of truth, even when it equals the default.

**Rationale:** Consistency with the application-scoped model. The default is a config default, not a runtime skip signal.

### D4: SelectionTint alpha consequence

`AccentPalette.SelectionTint` already returns `#1F` + hex. With the application-level accent replacing palette-baked accents, the Dark and Blue selection tints will change from `#1A` (26 alpha) to `#1F` (31 alpha), a small increase in selection visibility (~2% opacity). This is accepted as part of the unification.

**Rationale:** The function's constant wins. Diverging alphas were palette-local; the app-level model uses one formula.

**Implication:** A barely perceptible change in selection tint visibility on Dark and Blue palettes. Not worth preserving palette-specific alpha logic.

### D5: RuntimeProvidedKeys and guard strategy

- `tests/CLIHub.Tests/Themes/XamlResourceReferenceTests.cs:55-59` — Add `AccentBrush` and `AccentSelectionBrush` to `RuntimeProvidedKeys` with a comment naming `AccentApplier` as the provider, and reword the set's doc comment (it currently says `ApplicationHost` writes these keys at startup; the accent is applied from `App.OnStartup` and again on settings save). This must land in the same stage as the palette removal — otherwise that stage's `dotnet test` gate fails on the reference walk.
- `tests/CLIHub.Tests/Themes/ApplicationResourceKeyTests.cs:37-97` — Keep the existing palette-derived StaticResource ban (it still guards every remaining palette key) and ADD a separate guard for the two accent keys: they are never referenced with `StaticResource`, and no `*Theme.xaml` declares them. The palette-derived ban alone stops covering them once they leave the palettes.
- `tests/CLIHub.Tests/Themes/ThemePaletteParityTests.cs:8-33` — Add an assertion after the identical-key-set check that all palette files lack `AccentBrush` and `AccentSelectionBrush` so no future palette can reintroduce a theme-bound accent.

**Rationale:** The accent keys are runtime-provided and theme-independent, which is a durable contract. The guards encode that explicitly.

### D6: Theme picker description text

`src/CLIHub/Themes/ThemeDescriptors.cs:63` changes from:
```
"Blue-tinted dark chrome with a baked-in blue accent"
```
to:
```
"Blue-tinted dark chrome"
```

**Rationale:** The Blue palette no longer has a baked accent; the description must not claim it does.

## Alternatives Considered

### Keep baked accents as dead fallbacks

**Rejected.** Leaving the accent keys in palettes as a "just in case" fallback complicates the model: the keys are shadowed by application-level entries whenever the applier runs, so they are dead code the moment the app starts. A cleaner contract is that palettes define chrome only, and the runtime-provided keys are the single source of accent truth.

### Per-palette default accents

**Rejected.** The user's decision establishes a single application-level default (Sky). Per-palette defaults would reintroduce the coupling this change removes.

### An "auto/follow palette" accent option

**Rejected.** This adds a third accent mode (explicit preset, explicit custom, auto) and reintroduces palette-accent coupling. The model is simpler with the accent always explicit.

### Preserve the original #1A selection alpha on Dark/Blue

**Rejected.** Preserving palette-specific alpha requires either (a) storing the alpha in palette metadata and threading it through `AccentApplier`, or (b) branching on the active theme inside the applier. Both reintroduce palette-accent coupling. The `#1F` unification is accepted as part of the application-scoped model; the visual difference is negligible.

## Open Questions

None. All decisions are grounded in verified current state and the user's model.

## Stale preferences-ui Scenarios

The `openspec/specs/preferences-ui/spec.md` theme selector requirement (around `:305-325`) contains two scenarios that became stale after the `live-theme-switch` change:
- "Persist on save" (`:318-320`) claims the theme "takes effect on the next start", but themes now apply immediately.
- "Restart note" (`:322-325`) references a restart hint that no longer exists in the UI.

**Choice:** Correct them inside the same MODIFIED block (preserving scenario names) as part of updating the Blue theme description. These are already false statements; leaving them false while restating the Blue description would compound the staleness.

## Delta corrections during review

- "Accent color preference": `Preset selected` and `Custom accent accepted` keep the original user-action WHEN clauses; `Invalid custom value rejected` keeps the original THEN ("an error is shown and the window does not save"). Invalid input is rejected by the store before any save (`src/CLIHub/Settings/SettingsApplicationService.cs:258`), so a silent fallback would misstate the behavior.
- "Accent color preference": `Default accent` reads "no accent has been stored, or the stored value cannot be normalized", which separates a stored value from invalid user input.
- The unchanged "Accent applies live" requirement was dropped from the delta; it carries no behavior change.
- "Application theme selector": the dark card description is `Fluent Dark (default)` (`src/CLIHub/Themes/ThemeDescriptors.cs:49`), not "Windows 11 Fluent Dark (Recommended)".
