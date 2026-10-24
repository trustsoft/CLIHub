# Design

## Verified Current State

All citations verified against the working tree at `2e9ae6b`. The backlog entry
(`docs/backlog.md:375-386`) is **not drifted** — its file:line references match the current code.

### The two palette-apply callers (the whole problem)

- `src/CLIHub/App.xaml.cs:44-51` — `App.ApplyTheme` reads the stored theme
  (`AppThemes.Parse(PreferenceReader.ReadAppTheme(AppPaths.ConfigFile))`, line 46) and calls
  `PaletteSwitcher.Shared.Apply(application.Resources.MergedDictionaries, theme, SystemTheme.IsLight())`
  (line 49), once, in `OnStartup` (line 28) before the host starts.
- `src/CLIHub/Settings/WpfThemePreferenceTarget.cs:32-49` — `SetTheme` guards on
  `Application.Current` (lines 34-37) and calls
  `this.switcher.Apply(application.Resources.MergedDictionaries, theme, SystemTheme.IsLight())`
  (line 39); reached from a Settings save through `PreferenceApplier.ApplyTheme`.
- `src/CLIHub/Themes/PaletteSwitcher.cs:56` — `PaletteSwitcher.Shared` is the single switcher shared by
  startup and the DI-resolved target; registered as a singleton at
  `src/CLIHub/Composition/ServiceRegistration.cs:163`. `Apply` (lines 68-116) swaps or removes the one
  tracked overlay; a palette that cannot load keeps the previous palette and logs a Serilog warning
  (lines 81-90). Class remarks: UI thread only, no synchronization (lines 16-19).

### System theme resolution

- `src/CLIHub.Core.Windows/SystemTheme.cs:17-28` — `IsLight()` reads
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme`
  (constants at lines 10-11); any failure yields false (dark).
- `src/CLIHub/Themes/ThemeCatalog.cs:21-28` — `ResolvePalettePath(System, systemPrefersLight)` resolves
  to `Light` or `Dark` only; never blue. `ThemeDescriptors` gives System a null palette path (dark base
  mirror), `src/CLIHub/Themes/ThemeDescriptors.cs:67-74`.
- `src/CLIHub.Core/Models/Appearance/AppTheme.cs:93` — `AppThemes.Parse` falls back to `Dark` for null,
  missing, or unknown stored values (spec'd as "Unknown theme falls back to dark").

### Where a session-scoped event subscription belongs

- `openspec/specs/app-lifecycle/spec.md:206-225` — "First-instance session owns application event
  subscriptions": subscriptions get an explicit owner, are removed during shutdown before tracked
  operations stop and the provider is disposed, and the session delegates event wiring to a shell
  coordinator.
- `src/CLIHub/Lifetime/ApplicationSession.cs:44-68` — `Start` constructs `ShellCoordinator` with the
  startup context; `Dispose` (lines 71-81) disposes it.
- `src/CLIHub/Startup/ShellCoordinator.cs:69-104` — `InitializeShell` wires every application-level
  event through `this.context.Dispatch` (lines 74, 83-87); `Dispose` (lines 107-150) unsubscribes each
  stored handler, then disposes the startup UI.
- `src/CLIHub/Startup/ApplicationStartupContext.cs:8` — `Dispatch` is the injected UI-thread marshal;
  production wires it to `this.Dispatcher.Invoke` at `src/CLIHub/App.xaml.cs:33`.
- `src/CLIHub/Startup/ApplicationHost.cs:51-56` — a second instance signals activation and shuts down
  without a bootstrapper/session, so it never reaches coordinator wiring; `Shutdown` (lines 87-124)
  disposes the session (line 101) before the service provider (line 121).
- DI composition: `AddStartupServices` holds the session/bootstrapper registrations
  (`src/CLIHub/Composition/ServiceRegistration.cs:74-91`); the session is resolved and started through
  the bootstrapper (`src/CLIHub/Startup/ApplicationBootstrapper.cs:65`).

### SystemEvents facts (verified via Microsoft Learn, .NET 10 API pages)

- `Microsoft.Win32.SystemEvents` raises events on **the dedicated thread that monitors system events**,
  so handlers must be thread-safe and must marshal to touch UI; handlers must not do time-consuming
  work on that thread (SystemEvents class remarks).
- `UserPreferenceChanged` "is only raised if the message pump is running" (satisfied: WPF app) and it is
  a **static event** — handlers must be detached when the application is disposed or they leak
  (UserPreferenceChanged remarks).
- `Microsoft.Win32.SystemEvents` ships with the Windows Desktop SDK; `src/CLIHub/CLIHub.csproj:9` sets
  `UseWPF=true` on `net10.0-windows`, so no new package reference is needed.
- No existing `SystemEvents` usage in the codebase (searched `*.cs`).

### Existing test precedent for the no-Application limitation

- `tests/CLIHub.Tests/Services/WpfThemePreferenceTargetTests.cs:14-17,24` — the test host intentionally
  creates no `Application` (`Assert.Null(Application.Current)`); only the null-guard path is unit-tested
  and the applied path is covered by manual smoke. The same limitation applies to the follower's
  dispatched apply step.
- `tests/CLIHub.Tests/Themes/PaletteSwitcherTests.cs` — the switcher itself is fully unit-tested through
  its internal factory seam (`PaletteSwitcher` internal constructors at
  `src/CLIHub/Themes/PaletteSwitcher.cs:40-50`).
- `tests/CLIHub.Tests/Services/ShellCoordinatorTests.cs:162-212` — `CoordinatorFixture` builds the
  coordinator with strict mocks and an injectable dispatch; lifecycle tests live at
  `tests/CLIHub.Tests/Services/ApplicationSessionTests.cs` (`Start_WiresApplicationEventsToSessionWorkflows`,
  `Dispose_UnsubscribesApplicationEvents`).

### Specs

- `openspec/specs/application-theme/spec.md:101-136` — "Theme applies immediately" pins System
  resolution to "the moment the theme is applied" (requirement text, line 108; scenario
  "System resolves at save time", lines 121-125). This is the requirement the change modifies.
  "Theme palette resolution" (lines 25-55) defines the System→dark|light mapping and stays true
  unchanged; "Unresolvable palette keeps the current palette" (lines 132-136) already covers a failed
  follow-up apply generically.
- `openspec/specs/settings-theme/spec.md` — Settings window styling follows "the active theme"; live
  repaint through the existing `DynamicResource` palette mechanism satisfies it with no delta.
- `openspec/specs/app-lifecycle/spec.md:206-225` — already generic over which application events the
  coordinator wires; adding one more subscription needs no lifecycle delta.

## Decisions

### D1: Event source — `SystemEvents.UserPreferenceChanged`

Subscribe to the managed broadcast wrapper (`Microsoft.Win32.SystemEvents.UserPreferenceChanged`).
It requires a message pump (WPF provides one), needs no new package, and its static-event detach
requirement maps directly onto the existing session-disposal path.

**Rationale:** The parked alternative — watching `AppsUseLightTheme` with
`RegNotifyChangeKeyValue` — means a dedicated thread, interop, and lifetime code for the same signal.
`SystemEvents` already owns that machinery.

### D2: The filter is the observed value, not the event category (challenge to the brief)

The handler ignores `UserPreferenceChangedEventArgs.Category` and applies two cheap value guards on the
system-events thread:

1. `AppThemes.Parse(readStoredTheme()) == AppTheme.System` — a fixed theme (Dark/Light/Blue, including
   unknown/missing fallback) makes the event a strict no-op, satisfying "when the stored theme is not
   System, a personalization change must be a no-op for the palette".
2. The freshly read `SystemTheme.IsLight()` differs from the last observed mode (recorded at `Start`) —
   unrelated preference events (wallpaper, keyboard, power, accent) read the same registry value, see no
   flip, and dispatch nothing.

**Rationale (why not a category filter):** `UserPreferenceCategory.General` is overloaded — wallpaper
and other unrelated changes raise it too — and the category raised for the apps-light/dark toggle has
varied across Windows 10/11 builds (General, sometimes Color alongside). A category filter is therefore
both too broad (still fires on wallpaper) and too fragile (may miss the toggle). The registry-value
comparison is the precise definition of "the personalization changed": the *observable reaction* is
exactly one palette re-apply per actual light/dark flip, and every other event costs one registry read
and one small JSON read with zero palette work — satisfying the intent of "react only to the
personalization change".

### D3: Reuse `PaletteSwitcher` — third caller, no fork

The follower holds the same `PaletteSwitcher` singleton and its dispatched apply step mirrors
`WpfThemePreferenceTarget.SetTheme` exactly: guard `Application.Current`, then
`switcher.Apply(application.Resources.MergedDictionaries, theme, SystemTheme.IsLight())`. The mode is
re-read inside the dispatched apply (on the UI thread, milliseconds after the guard read), so the value
actually applied is the freshest one, matching the save path.

**Rationale:** One palette mechanism, one overlay-tracking owner, one failure contract (a palette that
cannot load keeps the previous palette and logs the switcher's existing warning,
`PaletteSwitcher.cs:81-90`). "System" can never resolve to blue because `ThemeCatalog` governs both
callers.

### D4: Stored theme is re-read at event time

The handler resolves the stored theme per event via `PreferenceReader.ReadAppTheme(AppPaths.ConfigFile)`
+ `AppThemes.Parse` — the same bootstrap read `App.ApplyTheme` uses (`App.xaml.cs:46`). No
theme-changed notification wiring between `PreferenceApplier` and the follower is needed; a Settings
save is visible to the very next event. Configuration writes are atomic (single camelCase document),
so a torn read is impossible; an unreadable config parses to Dark → no-op.

**Rationale:** Zero new coupling; config.json remains the single source of truth for the stored theme.

### D5: Subscription ownership — `ShellCoordinator`, started and disposed with the session

`ShellCoordinator` gains one constructor dependency, `SystemThemeFollower`. `InitializeShell` calls
`follower.Start(context.Dispatch)` alongside its other event wiring; `Dispose` disposes the follower
with the other unsubscriptions. `ApplicationSession` passes the follower through (constructor +1) and
keeps its ownership-by-disposal role. The follower is registered as a singleton in `AddStartupServices`
(`ServiceRegistration.cs:74-91`).

**Rationale:** This is the pattern the app-lifecycle spec already mandates for exactly this shape of
thing ("first-instance event subscriptions ... delegate ... event wiring to a shell coordinator"), it
already has the dispatch marshal, and it guarantees the spec'd shutdown order: session disposal removes
the subscription (ApplicationHost.cs:101) before tracked operations stop and the provider is disposed
(line 121). A second instance never creates a session (`ApplicationHost.cs:51-56`) and never subscribes.
The static-event leak warning from the API docs is satisfied by the same dispose path.

### D6: Threading — guards on the system-events thread, apply dispatched

The two guard reads run directly in the handler (a registry read plus a small JSON parse — well within
the API's "no time-consuming work" guidance), and only the palette swap is marshaled with
`context.Dispatch` (production: `Dispatcher.Invoke`, `App.xaml.cs:33`). Handler invocations are
serialized on the single system-events thread, so the follower's `lastKnownLight`/`started`/`dispatch`
fields need no locking; `Start` writes them on the UI thread before subscribing, and `Dispose` runs on
the UI thread during `App.OnExit` while the dispatcher and `Application.Current` are still alive.

**Rationale:** Identical to every other coordinator handler (RefreshMenu, ShowLaunchWindow,
notifications all go through `context.Dispatch`); synchronous `Invoke` is the established codebase
pattern and the swap is fast. An event racing shutdown at worst dispatches one guarded no-op.

### D7: Logging — silent on success

A successful follow re-applies through the switcher, which logs nothing on success (same as
`App.ApplyTheme`) and logs its own warning when a palette fails to load. The follower adds no logger
dependency.

**Rationale:** Consistent with the startup path; the failure case is already observable.

### D8: Component shape — `SystemThemeFollower` with an internal test seam

Public parameterless constructor wiring the production collaborators
(`PaletteSwitcher.Shared`, `PreferenceReader.ReadAppTheme(AppPaths.ConfigFile)`, `SystemTheme.IsLight`,
`SystemEvents.UserPreferenceChanged +=/-=`); internal seam constructor taking those five as delegates,
following the `PaletteSwitcher` two-constructor pattern (`PaletteSwitcher.cs:40-50`). `Start(Action<Action>
dispatch)` records the current mode and subscribes; `Dispose` unsubscribes (idempotent). Unit tests
capture the subscribe/unsubscribe delegates and "raise" the event by invoking the captured handler with
`new UserPreferenceChangedEventArgs(UserPreferenceCategory.General)`; the real-`Application` apply step
stays manual smoke per the documented precedent.

**Rationale:** The decision logic is unit-testable without `Application`, without the OS, and without
threads; the production wiring is a few `static` lambdas.

## Alternatives Considered

### Watch the registry value (`RegNotifyChangeKeyValue`)

**Rejected.** Requires a dedicated waiting thread or `RegisterWaitForSingleObject`, interop
declarations, and its own shutdown protocol — a parallel event infrastructure for the same signal that
`SystemEvents` already delivers.

### Subscribe in `App.xaml.cs` (OnStartup/OnExit)

**Rejected.** The app-lifecycle spec gives first-instance application event subscriptions an explicit
owner (the session/coordinator), and the WPF entry point is spec'd not to coordinate individual
services. It would also subscribe in second-instance processes that are about to exit, and it has no
dispatcher-independent logging or DI access.

### A new standalone lifetime service started by the bootstrapper

**Rejected.** The bootstrapper is a startup orchestrator; starting an individual follower from it adds
a new start/stop convention beside the spec'd coordinator wiring, plus an interface and explicit
dispose ordering in `ApplicationHost`. The coordinator already owns exactly this lifecycle.

### Filter on `UserPreferenceCategory.General` (or `Color`)

**Rejected** as the primary filter — see D2. (A category pre-filter would save one registry read per
unrelated event but can miss real toggles on some Windows builds; the value guard is authoritative.)

### Track the last-applied theme via `PreferenceApplier` notifications

**Rejected.** Extra event wiring between the save path and the follower for information that
config.json already holds; D4 reads it when needed.

### Apply through `IThemePreferenceTarget.SetTheme`

**Rejected.** It would reuse the target verbatim, but its log line ("Theme set to X") misattributes a
Windows-personalization follow to a preference change, and depending on it from `CLIHub.Themes` would
create a new Themes→Settings namespace dependency cycle (Settings already imports Themes,
`WpfThemePreferenceTarget.cs:9`).

### Poll with a timer

**Rejected.** Latency versus cost with no advantage over a push notification that Windows already
broadcasts.

## Open Questions

1. **Follow log line (informational):** D7 keeps a successful follow silent. If support needs it, add
   one information log when a flip re-applies the palette — cost: a logger dependency on the follower
   (or a static Serilog call as in `PaletteSwitcher`). Default chosen: silent.
2. **Manual smoke environment:** the smoke task toggles Windows Settings → Personalization → Colors
   while the app runs. Purely informational for the implementer: note which `UserPreferenceCategory`
   values arrive (expected `General`); the design ignores the category either way.
3. **README wording:** one optional clause on the Appearance bullet (line 36) is proposed in the tasks;
   if the user prefers README untouched, drop task 3.4.

No question blocks implementation.
