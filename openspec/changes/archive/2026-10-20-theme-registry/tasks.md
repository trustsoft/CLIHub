# Tasks — theme registry with a full blue palette

Stages are sized for a fresh session; run each stage's gate before moving on. Code style per root
`AGENTS.md`: nullable, file-scoped namespaces, `using` after the namespace, camelCase private
fields, XML docs on public APIs, English text.

## Stage 1 — Core token registry (Blue)

- [x] 1.1 In `src/CLIHub.Core/Models/Appearance/AppTheme.cs`, add a `Blue` member to `AppTheme`
      between `Light` and `System` with an XML doc ("The blue-tinted dark theme."), add
      `public const string BlueToken = "blue";` to `AppThemes`, a `case "blue":` arm in
      `TryParse`, and an `AppTheme.Blue => BlueToken` arm in `ToToken`.
- [x] 1.2 In `tests/CLIHub.Core.Tests/Models/Appearance/AppThemesTests.cs`, add
      `[InlineData("Blue", AppTheme.Blue)]` to the known-token theory and keep the
      unknown→dark theory unchanged; confirm the round-trip test covers `Blue` via
      `Enum.GetValues<AppTheme>()`.
- [x] 1.3 Gate: `dotnet test tests/CLIHub.Core.Tests` passes.

## Stage 2 — Rename LaunchTheme.xaml → DarkTheme.xaml

- [x] 2.1 Rename with history preservation:
      `git mv src/CLIHub/Themes/LaunchTheme.xaml src/CLIHub/Themes/DarkTheme.xaml`.
      (Executed as a plain `Move-Item` file move — no git write commands; git detects the rename by
      similarity at commit time.)
- [x] 2.2 Update the live references: `src/CLIHub/App.xaml:11` Source to
      `/Themes/DarkTheme.xaml`; the header comment in `DarkTheme.xaml` (say "Dark palette" instead
      of "Launch window palette", drop the LaunchTheme.xaml name); the same-name reference in
      `src/CLIHub/Themes/LightTheme.xaml:5`; the comment at
      `src/CLIHub/Views/LaunchWindow.xaml:35`; the path in
      `tests/CLIHub.Tests/Themes/ThemePaletteParityTests.cs:11`. Do not edit
      `openspec/changes/archive/` content.
- [x] 2.3 Gate: `dotnet build CLIHub.sln` succeeds.

## Stage 3 — Generalize the parity test

- [x] 3.1 Rewrite `ThemePaletteParityTests` to enumerate every
      `src/CLIHub/Themes/*Theme.xaml` via the existing `FindRepositoryRoot()` helper, assert the
      set contains at least `DarkTheme.xaml` and `LightTheme.xaml` (loud failure on rename/glob
      accidents), and assert all files expose one identical `x:Key` set (reuse `ReadKeys`); rename
      the test to `ThemePalettes_AllPaletteFilesDefineOneIdenticalKeySet`.
- [x] 3.2 Gate: `dotnet test tests/CLIHub.Tests --filter ThemePaletteParityTests` passes with the
      two existing palettes.

## Stage 4 — Theme registry and generic ApplyTheme

- [x] 4.1 Create `src/CLIHub/Themes/ThemeCatalog.cs` exactly as decided in design.md D1
      (`DarkPalettePath` const + `ResolvePalettePath(AppTheme, bool)` returning the palette path or
      null), with XML docs explaining that null keeps the App.xaml dark base.
- [x] 4.2 Rework `App.ApplyTheme()` (`src/CLIHub/App.xaml.cs:41-54`) to the D1 shape: parse the
      token, resolve via the catalog with `SystemTheme.IsLight()`, merge the resolved dictionary
      when non-null; leave `ApplyAccent()` untouched.
- [x] 4.3 Gate: `dotnet build CLIHub.sln` passes (build gate verified green); user manual smoke
      passed 2026-10-20 — with stored `"appTheme"` values `"dark"`, `"light"`, `"system"` (and a
      bogus value) in `%APPDATA%\CLIHub\config.json`, the app rendered dark/light/system exactly as
      before the rework and an unknown value still rendered dark.

## Stage 5 — Blue palette file

- [x] 5.1 Create `src/CLIHub/Themes/BlueTheme.xaml` with the same 28 brush keys and comment-group
      structure as `DarkTheme.xaml`, using the design.md D2 table (subject to Q1 review; keep the
      stated contrast invariants). Comment the header like `LightTheme.xaml`'s, noting it merges on
      top of the dark base for the blue theme.
- [x] 5.2 Gate: `dotnet test tests/CLIHub.Tests --filter ThemePaletteParityTests` passes with three
      palette files (the generalized test joins Blue automatically).
- [x] 5.3 Manual gate passed 2026-10-20 (user smoke): with `"appTheme": "blue"`, the app showed
      blue-tinted chrome on the launch window and Settings, readable primary/muted/label text,
      visible hairlines and dividers, `#3B82F6` accent on selection/hover/toggles with no stored
      accent override, and a stored non-default accent (e.g. `"accentColor": "#EA580C"`) still
      overrode it live.

## Stage 6 — Settings selector option and preview

- [x] 6.1 In `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs:338-343`, insert
      `new(AppTheme.Blue, "Blue Theme", "Blue-tinted dark chrome with a baked-in blue accent")`
      between the Light and System entries (wording per Q2).
- [x] 6.2 In `src/CLIHub/Themes/SettingsStyles.xaml` next to lines 363-374, add
      `ThemePreviewBlueBrush` (`#FF232833`) and `ThemePreviewBarBlueBrush` (`#FF3E4C66`); in
      `src/CLIHub/Views/SettingsAppearancePage.xaml`, add `DataTrigger`s on
      `{x:Static models:AppTheme.Blue}` to the preview `Border` background (after line 43) and the
      preview bar fill (after line 77).
- [x] 6.3 Gate: `dotnet build CLIHub.sln` passes (build gate verified green); user manual check
      passed 2026-10-20 — the Appearance page shows four theme cards (Dark, Light, Blue, System)
      with the blue preview, the accent ring and check badge land on the stored card, saving
      "Blue Theme" persists `"appTheme": "blue"` in `config.json`, and reopening Settings
      reselects it.

## Stage 7 — Docs and validation

- [x] 7.1 Rewrite `docs/architecture/ui.md`: line 13 layering (DarkTheme.xaml as the base palette,
      LightTheme/BlueTheme merged via `ThemeCatalog`), and lines 66-71 as the four-step "add a
      theme" recipe (XAML file with the same keys → `ThemeCatalog` arm → settings option + preview
      brushes → parity test automatic; restart-based switching note stays).
- [x] 7.2 Update `README.md:46` to "shared Dark/Light/Blue themes".
- [x] 7.3 Gates: `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, and
      `openspec validate --all` all pass.
- [x] 7.4 After the implementation is verified: run `graphify update .` to refresh the project
      knowledge graph (repository rule; the change is archived only with explicit user approval).
