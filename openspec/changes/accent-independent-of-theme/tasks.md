# Tasks

Stages are sized for a fresh session. Each stage ends green: `dotnet build CLIHub.sln` and
`dotnet test CLIHub.sln`. No stage commits.

## Stage 1: Palettes stop declaring the accent; one shared applier

**Goal**: the accent is application-scoped, palettes carry chrome only, and startup and save share
one implementation.

- [ ] 1.1 Remove the `AccentBrush` and `AccentSelectionBrush` declarations, with the comments
  attached to them, from `src/CLIHub/Themes/DarkTheme.xaml`, `src/CLIHub/Themes/LightTheme.xaml`, and
  `src/CLIHub/Themes/BlueTheme.xaml`. Touch no other key.
- [ ] 1.2 Rewrite the `BlueTheme.xaml` header comment (previously lines 5-8) so it no longer claims a
  baked-in Blue accent: the palette defines blue-tinted chrome only, and the application-level accent
  applies independently of the theme. Check the remaining palette comments for the same claim.
- [ ] 1.3 Create `src/CLIHub/Settings/AccentApplier.cs`: `internal static class AccentApplier` with
  `public static void Apply(ResourceDictionary resources, string accentHex)` and the internal
  `Apply(ResourceDictionary resources, string accentHex, Func<string, SolidColorBrush> createBrush)`
  test seam. The public overload normalizes through `AccentPalette.Normalize` and writes
  `resources["AccentBrush"] = createBrush(AccentPalette.Opaque(accent))` and
  `resources["AccentSelectionBrush"] = createBrush(AccentPalette.SelectionTint(accent))`. The default
  factory returns a frozen `SolidColorBrush`. The parameter is a `ResourceDictionary` — pass
  `Application.Resources` itself, never `MergedDictionaries`: local entries are what shadow merged
  palettes, and the merged collection has no keyed indexer. XML docs state the application-scoped
  contract.
- [ ] 1.4 `src/CLIHub/App.xaml.cs`: in `ApplyAccent`, remove the
  `if (accent == AccentPalette.DefaultHex ...) return;` branch and delegate to
  `AccentApplier.Apply(application.Resources, accent)`. Keep the `Application.Current` null guard and
  the normalization. Update the XML doc: the stored accent is always applied, default included.
- [ ] 1.5 `src/CLIHub/Settings/WpfAccentPreferenceTarget.cs`: replace the duplicated brush writing in
  `SetAccent` with `AccentApplier.Apply(application.Resources, accentHex)`; keep the null guard.
- [ ] 1.6 `tests/CLIHub.Tests/Themes/XamlResourceReferenceTests.cs`: add `"AccentBrush"` and
  `"AccentSelectionBrush"` to `RuntimeProvidedKeys` with a comment naming `AccentApplier`, and reword
  the set's doc comment: keys are written into `Application.Resources` by code at startup and on
  settings save, not by `ApplicationHost`. This belongs to this stage — without it this stage's test
  gate fails as soon as the palette keys are gone.

**Gate:** `dotnet build CLIHub.sln` — 0 warnings, 0 errors.

**Gate:** `dotnet test CLIHub.sln` — every suite green.

## Stage 2: Guards and accent-applier tests

**Goal**: the new contract is enforced, and the shared applier is covered without a real
`Application` (the test host asserts `Application.Current` is null).

- [ ] 2.1 `tests/CLIHub.Tests/Themes/ApplicationResourceKeyTests.cs`: keep the existing
  palette-derived `StaticResource` ban and ADD a separate guard asserting (a) no `*Theme.xaml`
  declares `AccentBrush` or `AccentSelectionBrush`, and (b) neither key is referenced with
  `StaticResource` anywhere under `src/CLIHub`. Do not delete the existing test: it still guards
  every remaining palette key.
- [ ] 2.2 `tests/CLIHub.Tests/Themes/ThemePaletteParityTests.cs`: after the identical-key-set check,
  assert that the shared palette key set contains neither `AccentBrush` nor
  `AccentSelectionBrush`, so a future palette cannot reintroduce a theme-bound accent.
- [ ] 2.3 Add `tests/CLIHub.Tests/Settings/AccentApplierTests.cs` (create the folder if needed) with
  cases: (a) a default, missing or invalid stored value writes Sky `#FF4CC2FF` and tint
  `#1F4CC2FF`; (b) a custom accent writes its own colour and tint; (c) applying twice replaces the
  two entries instead of duplicating them; (d) applying to an empty `ResourceDictionary` does not
  throw; (e) independence — after applying an accent, adding, replacing and removing a palette
  dictionary in `MergedDictionaries` leaves both accent entries untouched.
- [ ] 2.4 Add a `SetAccent_NoApplicationInstance_DoesNotThrow` case to
  `tests/CLIHub.Tests/Services/WpfAccentPreferenceTargetTests.cs`, following the existing
  `SetTheme_NoApplicationInstance...` pattern (`Assert.Null(Application.Current)`).

**Gate:** `dotnet build CLIHub.sln` — 0 warnings, 0 errors.

**Gate:** `dotnet test CLIHub.sln` — every suite green, including the new tests.

## Stage 3: Blue theme card description

- [ ] 3.1 `src/CLIHub/Themes/ThemeDescriptors.cs:63`: change the Blue theme description from
  `"Blue-tinted dark chrome with a baked-in blue accent"` to `"Blue-tinted dark chrome"`.
- [ ] 3.2 Search the repository for remaining claims of a baked or palette-supplied accent
  (`baked-in`, `baked accent`, `palette's default accent`) outside `openspec/changes/` and fix any
  that describe current behavior. Files under `openspec/changes/archive/` are historical records and
  stay untouched.

**Gate:** `dotnet build CLIHub.sln` — 0 warnings, 0 errors.

**Gate:** `dotnet test CLIHub.sln` — every suite green.

## Stage 4: Align the spec deltas with the implemented behavior

The deltas already exist under `openspec/changes/accent-independent-of-theme/specs/`; do not
recreate them from scratch.

- [ ] 4.1 Re-read both deltas against the code from Stages 1-3 and correct any statement that does not
  match the implementation. Keep every original scenario name: `Theme palette resolution` ("Dark
  theme", "Light theme", "Blue theme", "System follows Windows", "Shared palette keys"), `Accent
  color preference` ("Default accent", "Preset selected", "Custom accent accepted", "Invalid custom
  value rejected", plus the new "Theme change preserves accent"), and `Application theme selector`
  ("Options offered", "Persist on save", "Restart note").
- [ ] 4.2 Confirm the deltas cover both capabilities and that no requirement was dropped that the
  change does not actually remove.

**Gate:** `openspec validate --all` — every item passes, and the change reports no archive-refusal
warning.

## Stage 5: Docs and final verification

- [ ] 5.1 `docs/backlog.md`: replace the parked entry "A settings save pins the accent even when it
  equals the palette default" with the resolved model — the accent is application-scoped and
  independent of the theme, palettes declare chrome only, and both startup and save always apply the
  stored accent. Remove the superseded remedy (removing local entries when the accent equals the
  default) so it cannot be re-read as pending work. The file already carries uncommitted edits from
  the planning session; keep them and rewrite in place.
- [ ] 5.2 Run `dotnet build CLIHub.sln` — 0 warnings, 0 errors.
- [ ] 5.3 Run `dotnet test CLIHub.sln` — every suite green; report the per-project counts.
- [ ] 5.4 Run `openspec validate --all` — every item passes.
- [ ] 5.5 Manual smoke (user): with the default accent, switch Dark → Blue → Light and confirm the
  accent stays Sky and the chrome changes; select the Blue accent, switch themes and confirm the blue
  accent stays; restart and confirm the stored accent persists on every theme.
- [ ] 5.6 Report the exact gate outputs, any deviation, and the files changed for review. Do not
  commit.

**Final gate:** every step above passes or is explicitly reported as not run.
