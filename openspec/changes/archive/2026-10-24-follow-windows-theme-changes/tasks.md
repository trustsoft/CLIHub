# Tasks

Stages are sized for a fresh session. Each stage ends green: `dotnet build CLIHub.sln` and
`dotnet test CLIHub.sln`. No stage commits; the user decides about commit and archive.

## Stage 1: The follower component

**Goal**: a testable `SystemThemeFollower` in `CLIHub.Themes` that decides, from a
`UserPreferenceChanged` event, whether the palette must be re-applied, and hands the apply to a
dispatcher delegate.

- [x] 1.1 Create `src/CLIHub/Themes/SystemThemeFollower.cs`: `public sealed class SystemThemeFollower
  : IDisposable`. Public parameterless constructor delegating to the internal seam with the production
  collaborators: `PaletteSwitcher.Shared`, `static () => PreferenceReader.ReadAppTheme(AppPaths.ConfigFile)`,
  `SystemTheme.IsLight`, `static h => SystemEvents.UserPreferenceChanged += h`, and the matching `-=`
  unsubscribe. Internal seam constructor stores those five and creates one
  `UserPreferenceChangedEventHandler` field used for both subscribe and unsubscribe (static event — one
  instance, detached on dispose, per the API docs). XML docs state: UI-thread apply via the supplied
  dispatch; guards run on the system-events thread; category is deliberately ignored because the
  registry-value comparison is the filter (design D2).
- [x] 1.2 Implement `public void Start(Action<Action> dispatch)`: throw on null, no-op when already
  started; record `dispatch`, record `lastKnownLight = isSystemLight()`, then subscribe (subscription
  last, so no handler can run before the state is initialized). Implement `public void Dispose()`
  (idempotent): unsubscribe, clear the started state. Implement the handler:
  (a) skip when not started; (b) `AppThemes.Parse(readStoredTheme()) != AppTheme.System` → return;
  (c) `isSystemLight()` equal to `lastKnownLight` → return; (d) update `lastKnownLight` and
  `dispatch(() => Apply(AppTheme.System))`. `Apply(AppTheme theme)` mirrors
  `WpfThemePreferenceTarget.SetTheme` (`src/CLIHub/Settings/WpfThemePreferenceTarget.cs:34-39`): guard
  `Application.Current`, then `switcher.Apply(application.Resources.MergedDictionaries, theme,
  SystemTheme.IsLight())`. Amended 2026-10-24 by user decision: a successful re-apply logs one
  informational line through the static Serilog `Log` class (mirroring `PaletteSwitcher`), overriding
  this task's original "No success logging (design D7)" clause.
- [x] 1.3 Create `tests/CLIHub.Tests/Themes/SystemThemeFollowerTests.cs` using the internal seam
  constructor (captured subscribe/unsubscribe delegates, controllable `readStoredTheme` and
  `isSystemLight`, and a `new PaletteSwitcher(_ => new ResourceDictionary())` seam switcher). Assert
  `Application.Current` is null (house precedent,
  `tests/CLIHub.Tests/Services/WpfThemePreferenceTargetTests.cs:24`). Cases, named
  `MethodOrScenario_Condition_ExpectedResult`:
  `Start_SubscribesOnce_AndRaisesNothingWithoutAnEvent`;
  `Start_SecondCall_IsANoOp` (still exactly one subscription);
  `UserPreferenceChanged_StoredSystemThemeAndModeFlip_DispatchesOneApply` (raise via the captured
  handler with `new UserPreferenceChangedEventArgs(UserPreferenceCategory.General)`; the captured
  dispatch receives exactly one action; executing it does not throw — the `Application.Current` guard
  returns before any palette work);
  `UserPreferenceChanged_StoredFixedTheme_DoesNotDispatch` (parameterize Dark, Light, Blue, and a
  null/unknown stored value falling back to Dark);
  `UserPreferenceChanged_ModeUnchanged_DoesNotDispatch` (unrelated event, same light mode);
  `UserPreferenceChanged_TwoFlips_DispatchTwice` (guard state advances);
  `Dispose_Unsubscribes_AndLaterEventsDispatchNothing`.

**Gate:** `dotnet build CLIHub.sln` — 0 warnings, 0 errors.

**Gate:** `dotnet test CLIHub.sln` — every suite green, including the new tests.

## Stage 2: Session wiring

**Goal**: the follower runs for the first-instance session only, started with the startup dispatch and
disposed with the coordinator.

- [x] 2.1 Register the follower in `AddStartupServices`
  (`src/CLIHub/Composition/ServiceRegistration.cs:74-91`): `services.AddSingleton<SystemThemeFollower>();`
  (parameterless production constructor) and mention it in the method's XML doc summary as a
  session-owned subscription. Keep `PaletteSwitcher.Shared` registered where it is (line 163) — the
  follower's default constructor uses the same shared instance.
- [x] 2.2 `src/CLIHub/Lifetime/ApplicationSession.cs`: add `SystemThemeFollower themeFollower` to the
  constructor (null-guard like the rest) and pass it to `ShellCoordinator` in `Start`
  (`src/CLIHub/Lifetime/ApplicationSession.cs:59-65`); update the constructor XML docs.
- [x] 2.3 `src/CLIHub/Startup/ShellCoordinator.cs`: add the follower constructor dependency
  (null-guard), call `this.themeFollower.Start(context.Dispatch)` at the end of `InitializeShell`
  (after the existing event wiring, lines 98-103), and dispose it in `Dispose` with the other event
  unsubscriptions (before the startup-UI disposal, lines 146-149). Update the class XML doc: the
  coordinator also wires the Windows personalization follow for the stored "System" theme.
- [x] 2.4 Extend `tests/CLIHub.Tests/Services/ShellCoordinatorTests.cs`: `CoordinatorFixture`
  (lines 162-212) builds the coordinator with a seam-constructed follower and exposes its captured
  subscription; add `Constructor_StartsThemeFollower` (subscription captured) and
  `Dispose_DisposesThemeFollower` (unsubscribe captured; a raised preference event after dispose
  dispatches nothing, mirroring `Dispose_UnsubscribesAllEvents_AndDisposesStartupUi`, lines 118-140).
- [x] 2.5 Extend `tests/CLIHub.Tests/Services/ApplicationSessionTests.cs`: the `SessionFixture` gains a
  seam follower; `Start_WiresApplicationEventsToSessionWorkflows` asserts the follower subscription was
  taken, and `Dispose_UnsubscribesApplicationEvents` asserts it was released.

**Gate:** `dotnet build CLIHub.sln` — 0 warnings, 0 errors.

**Gate:** `dotnet test CLIHub.sln` — every suite green, including the extended lifecycle tests.

## Stage 3: Spec delta alignment, docs, and final verification

The delta already exists under `openspec/changes/follow-windows-theme-changes/specs/`; correct it in
place, do not recreate it.

- [x] 3.1 Re-read `specs/application-theme/spec.md` against the implemented code from Stages 1-2 and
  correct any statement that does not match. Keep every original scenario name of "Theme applies
  immediately" ("Theme change applies on save", "Theme persists for the next start", "System resolves at
  save time", "Cancel keeps the saved theme", "Unresolvable palette keeps the current palette"); the
  new scenarios stay the three added ones.
- [x] 3.2 `docs/architecture/ui.md`: update the palette paragraph (line 118 — a saved theme applies
  through `PaletteSwitcher`; now also re-applied on a Windows personalization change while running when
  the stored theme is System) and the Theme Data Locations table (line 129 — add
  `SystemThemeFollower` next to the palette-switching row with its file location).
- [x] 3.3 `docs/project-context.md`: extend the Appearance runtime fact (lines 52-54) — the System
  theme follows Windows personalization changes while the application runs; fixed themes ignore them.
- [x] 3.4 `README.md`: optionally extend the Appearance bullet (line 36, "Theme and accent changes
  apply immediately after Save.") with one clause noting the System option follows Windows
  personalization live. Keep it to one sentence; skip if it reads awkwardly.
- [x] 3.5 `docs/backlog.md`: replace the parked entry "Windows personalization changes are not followed
  while the application runs" (lines 375-386) with a resolved entry in the same format as the
  accent entry at lines 403+ ("Resolved by the `follow-windows-theme-changes` change (2026-10-24)"),
  stating the resolved model: the follower subscribes for the session, re-applies through the shared
  switcher when the stored theme is System, and fixed themes ignore personalization changes. The file
  carries unrelated uncommitted edits — rewrite only this entry and leave every other line untouched.
- [x] 3.6 Run `dotnet build CLIHub.sln` — 0 warnings, 0 errors.
- [x] 3.7 Run `dotnet test CLIHub.sln` — every suite green; report the per-project counts.
- [x] 3.8 Run `openspec validate --all` — every item passes.
- [x] 3.9 Run `graphify update .` from the repository root so the graph reflects the new wiring.
- [x] 3.10 Manual smoke (user): store the "System" theme and save; with the app running (launch window
  and Settings both open), switch Windows Settings → Personalization → Colors between light and dark
  and confirm both windows repaint with the re-resolved palette, never blue; store "Dark", toggle the
  Windows mode and confirm nothing changes; exit the app and confirm a clean shutdown with no
  personalization-follow errors in the log.
- [x] 3.11 Report the exact gate outputs, any deviation, and the files changed for review. Do not
  commit; ask the user about archiving and committing.

**Final gate:** every step above passes or is explicitly reported as not run.
