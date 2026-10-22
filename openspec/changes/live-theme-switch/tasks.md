# Tasks: Live Theme Switch

Stages are sized for a fresh session. Each stage ends with build + tests green and a user acceptance
checkpoint. No stage commits.

---

## Stage 1: Runtime palette slot and the preference chain

**Goal**: one tracked palette overlay, applied by startup and by Save through the existing preference
applier, with rollback.

- [ ] 1.1 Create `src/CLIHub/Themes/PaletteSwitcher.cs`: `public sealed class PaletteSwitcher` with
  `public static PaletteSwitcher Shared { get; } = new();`, `internal PaletteSwitcher()`, one private
  `ResourceDictionary? overlay` field, and
  `public bool Apply(IList<ResourceDictionary> merged, AppTheme theme, bool systemPrefersLight)`.
  Resolve the path through `ThemeCatalog.ResolvePalettePath`; build the candidate dictionary from
  `pack://application:,,,/CLIHub;component{path}` before touching the collection; null path removes
  the tracked overlay when it is still present; a non-null path replaces the tracked overlay in place
  or appends it; any exception logs `Log.Warning` and leaves the collection untouched. Document on the
  type: UI-thread only, one overlay, dark base stays in `App.xaml`.

- [ ] 1.2 Add `tests/CLIHub.Tests/Themes/PaletteSwitcherTests.cs`. Use a fresh `new PaletteSwitcher()`
  (never `Shared`) against a local `ResourceDictionaryCollection`. Cases: applying Light to an empty
  collection adds exactly one dictionary and its `Source` points at LightTheme.xaml; applying Blue
  afterwards keeps the count at one and replaces the same slot; applying Dark on top removes it and
  the count returns to zero; applying Dark twice is a no-op; an unresolvable palette (construct the
  path via a small seam, or assert that `ThemeCatalog` is not consulted for a theme whose file is
  absent) returns false and leaves the collection unchanged. If `pack://application:,,,` URIs cannot
  be created without an `Application` instance in the test host, use component-relative
  `"/CLIHub;component/Themes/LightTheme.xaml"` URIs and say so in a comment — do not weaken the
  assertions.

- [ ] 1.3 Create `src/CLIHub/Settings/IThemePreferenceTarget.cs`
  (`void SetTheme(AppTheme theme);`, XML docs, same shape as `IAccentPreferenceTarget`) and
  `src/CLIHub/Settings/WpfThemePreferenceTarget.cs`: null-guard `Application.Current`, call
  `PaletteSwitcher.Shared.Apply(application.Resources.MergedDictionaries, theme, SystemTheme.IsLight())`,
  log INFO on success and WARNING when the switcher returns false via
  `ILogger<WpfThemePreferenceTarget>`.

- [ ] 1.4 Add `void ApplyTheme(AppTheme theme);` to `src/CLIHub/Settings/IPreferenceApplier.cs` with
  XML docs stating it applies the palette immediately. Implement it in `PreferenceApplier`: take
  `IThemePreferenceTarget theme` as a constructor dependency beside `IAccentPreferenceTarget`, forward
  to it, and log like `ApplyAccent`.

- [ ] 1.5 Register in `src/CLIHub/Composition/ServiceRegistration.cs` inside `AddSettingsServices`:
  `PaletteSwitcher.Shared` as a singleton, `IThemePreferenceTarget` → `WpfThemePreferenceTarget`
  beside the accent target; update the method's XML summary list of the preference chain.

- [ ] 1.6 Replace the merge logic in `App.ApplyTheme` (`src/CLIHub/App.xaml.cs:44-57`) with a call to
  `PaletteSwitcher.Shared.Apply(application.Resources.MergedDictionaries, theme, SystemTheme.IsLight())`;
  keep the preference read and the `Current is { } application` guard. Update the XML docs of the
  method (no longer "resolved before any window is created, applies on this start" only).

- [ ] 1.7 In `src/CLIHub/Settings/SettingsApplicationService.cs`, call
  `this.applier.ApplyTheme(draft.AppTheme);` immediately after `ApplyAccent(draft.AccentColor)`
  (`:458`) and before `preferencesStore.Update(...)`, and add
  `TryRollback("theme", () => this.applier.ApplyTheme(previous.AppTheme));` to `Rollback`
  (`:512-520`).

- [ ] 1.8 Update every strict `Mock<IPreferenceApplier>` that reaches `Save` so the new call is
  expected: `tests/CLIHub.Tests/Services/SettingsApplicationServiceTests.cs` (the `CreateApplier`
  helper and each explicit `MockBehavior.Strict` instance that runs a successful save),
  `tests/CLIHub.Tests/ViewModels/Settings/SettingsNavigationTests.cs`,
  `tests/CLIHub.Tests/ViewModels/Settings/SettingsViewModelApplicationTests.cs`,
  `tests/CLIHub.Tests/ViewModels/Settings/SettingsOperationLifetimeTests.cs`,
  `tests/CLIHub.Tests/ViewModels/Updates/SharedUpdatePresentationTests.cs`. Use
  `Setup(x => x.ApplyTheme(It.IsAny<AppTheme>()))`; do not switch a mock to loose behavior to avoid
  this. Add one assertion in `SettingsApplicationServiceTests` that the draft's theme is forwarded
  (`ApplyTheme(draft.AppTheme)`) and one that a failed hotkey registration rolls the theme back to the
  previous value.

- [ ] 1.9 Extend `tests/CLIHub.Tests/Services/PreferenceApplierTests.cs`: construct with an
  `IThemePreferenceTarget` mock and cover forwarding plus the success and failure log paths.
  Re-check `tests/CLIHub.Tests/Composition/ServiceRegistrationTests.cs` — the new registrations must
  not break the existing descriptor assertions, and `IThemePreferenceTarget` must resolve.

- [ ] 1.10 Gates: `dotnet build CLIHub.sln` (0 warnings/errors), `dotnet test CLIHub.sln` (all green),
  `openspec validate --all` (all passed). Manual smoke, for the user: start with a Dark preference,
  open Settings → Appearance and the launch window, save Light → both windows turn light without a
  restart; save Blue → both turn blue and a default accent follows the blue baked accent; save Dark →
  both return to dark; set a custom accent, switch Dark → Blue → the accent stays the custom color;
  change the theme and press Cancel → nothing changes; restart → the saved theme is applied.

**Acceptance**: build/tests/validate green, the smoke checklist confirmed by the user.

---

## Stage 2: UI text, documentation, and the spec contract

**Goal**: the product text and the durable contract stop promising a restart.

- [ ] 2.1 `src/CLIHub/Views/SettingsAppearancePage.xaml:14` — the theme card hint currently reads
  "Select the visual palette and luminosity mode. Applies after the next start; System Sync follows
  Windows." Change it to state that a saved theme applies immediately and that System Sync follows
  Windows.

- [ ] 2.2 `docs/architecture/ui.md` — replace the closing line "Switching the palette without a
  restart is not supported; a theme change takes effect on the next start." with the new behavior and
  a one-paragraph description of the palette slot (dark base in `App.xaml`, one tracked overlay
  swapped by `PaletteSwitcher`, startup and Save share it). Update the "To add another theme" recipe
  if any step still implies a restart.

- [ ] 2.3 Replace `openspec/changes/live-theme-switch/specs/application-theme/spec.md` with the
  REMOVED + ADDED delta described in `design.md` (D5): remove "Theme applies on next start" with
  `**Reason**` and `**Migration**`, add "Theme applies immediately" with scenarios for save-time
  application with open windows, persistence for the next start, System resolving at save time, Cancel
  leaving the persisted theme in force, and an unresolvable palette retaining the current one.
  Requirement-level wording only, no method names, no `Acceptance` blocks.

- [ ] 2.4 `docs/backlog.md` — park two findings under a theme/UI heading using the file's standard
  fields: (a) following Windows personalization changes live while the application runs, (b) a save
  always pins the accent as local application resources even when it equals the palette default, so
  the baked accent of the next palette is not seen until a restart.

- [ ] 2.5 Gates: `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, `openspec validate --all`
  (quote the totals line). `openspec validate live-theme-switch` must not report the
  "Archive would refuse this delta" warning for `application-theme`.

**Acceptance**: green gates, delta validates without archive warnings, docs and hint text match the
delivered behavior, user confirms.
