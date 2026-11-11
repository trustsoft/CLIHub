# Design: core-cleanup-and-shell-factory

## Context (verified current state)

Every `file:line` below was verified by direct reads and solution-wide greps against the clean
working tree at HEAD `8acfbf3` (`git status --porcelain` empty; `openspec/changes/` holds only
`archive/`). Porting sources were read via `git show backup-before-reset:<path>` and
`git show <sha> -- <path>` for `08efe63`, `491fd27`, `af70805`, `50d28af`.

### 1 — `UpdateService` dead branch (item 1)

`src/CLIHub.Core/Updates/UpdateService.cs` (316 lines, file-scoped namespace `CLIHub.Core.Updates`
at `:1`):

- Field comment `:18-19`: "The release repository used as the Velopack update feed (GitHub
  Releases). / A non-GitHub URL or local folder is also supported (see CreateDefaultManager)."
- `internal const string RepositoryUrl = "https://github.com/trustsoft/clihub";` at `:20`.
- `CreateDefaultManager()` `:87-108`: comment `:91-93` ("A GitHub repository URL uses the GitHub
  Releases source; any other URL or a local folder (useful for testing and self-hosted mirrors)
  uses Velopack's default source. / Only stable releases are published, so the source never includes
  pre-release builds."), branch `if (RepositoryUrl.Contains("github.com",
  StringComparison.OrdinalIgnoreCase))` (`:94`) returning
  `new UpdateManager(new GithubSource(RepositoryUrl, null, prerelease: false))` (`:96`), else
  `return new UpdateManager(RepositoryUrl);` (`:99`), wrapped in a try/catch that logs
  "Velopack is not initialized" at debug and returns null (`:101-107`).
- Dead-by-construction proof: `RepositoryUrl` is a `const` assigned a literal containing
  `"github.com"`, so `:94` always evaluates true and `:99` never runs. The try/catch must stay —
  `UpdateServiceTests` constructs the default service in a test host
  (`CreateDefaultService() => new(NullLogger<UpdateService>.Instance)`, `UpdateServiceTests.cs:67-68`)
  and relies on the null-manager path (`CheckForUpdates_WhenNotInstalled_ReturnsNotInstalledWithoutNetwork`
  `:101-107`).
- `RepositoryUrl` references: solution-wide grep finds exactly four hits, all inside
  `UpdateService.cs` (`:20`, `:94`, `:96`, `:99`). Nothing in `src/`, `tests/`, or XAML uses
  `UpdateService.RepositoryUrl`, so reducing `internal const` to `private const` is compile-safe.
- Backup `08efe63` (diff verified) did exactly this: `private const`, comment "The GitHub repository
  whose Releases feed is the Velopack update source (GitHub Releases only).", branch deleted. Its
  resulting return was `new UpdateManager(new GithubSource(RepositoryUrl, null, prerelease))`
  because the backup still had a `prerelease` parameter; master removed that parameter
  (remove-prerelease-updates-option) and hardcodes `prerelease: false` at `:96` — the port keeps
  master's argument list.

### 2 — duplicated build-metadata strip (item 2)

Three byte-identical private helpers (doc comment "Strips build metadata (for example, the "+commit"
suffix) so the version displays as a plain semantic version." + `IndexOf('+')` body):

| File | Helper | Call sites |
|---|---|---|
| `src/CLIHub.Core/Updates/UpdateDownloader.cs` | `Normalize` `:115-119` | `:77` (`var version = Normalize(asset.Version.ToString());`) |
| `src/CLIHub.Core/Updates/UpdateInstaller.cs` | `Normalize` `:101-105` | `:41` (`ApplyAndRestart`) and `:77` (`ApplyAndExit`) |
| `src/CLIHub.Core/Updates/UpdateService.cs` | `Normalize` `:311-315` | `:115`, `:122` (`GetCurrentVersion`), `:253` (`CheckForUpdateCoreAsync`) |

Backup `491fd27` (diff verified): added `src/CLIHub.Core/Updates/UpdateVersionText.cs`
(`internal static class`, one `internal static string StripBuildMetadata(string version)` with the
same body and doc text), deleted all three private helpers, and rewired every call site to
`UpdateVersionText.StripBuildMetadata(...)`; added
`tests/CLIHub.Core.Tests/Updates/UpdateVersionTextTests.cs` with three facts
(`StripBuildMetadata_VersionWithCommitSuffix_StripsSuffix`,
`StripBuildMetadata_PlainVersion_ReturnsUnchanged`,
`StripBuildMetadata_TrailingPlus_ReturnsCoreVersion`).

Coverage of the strip behavior on master: `UpdateServiceTests.cs:97`
(`Assert.DoesNotContain("+", version)`) pins the observable version text; the downloader/installer
paths feed log messages and result records built from the same helper.

Internal visibility: `src/CLIHub.Core/CLIHub.Core.csproj:30` grants
`InternalsVisibleTo Include="CLIHub.Core.Tests"`, so the `internal` helper is testable.

### 3 — duplicated update-check outcome text (item 3)

- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` (`namespace CLIHub.ViewModels` at `:1`),
  `ApplyUpdateResult` `:774-817`: the message switch at `:778-784` maps `UpdateStatus.UpdateAvailable`
  → `$"Update available: {result.AvailableVersion} (current v{result.CurrentVersion})"`, `UpToDate` →
  `$"Up to date (v{result.CurrentVersion})"`, `NotInstalled` → `"Updates apply to installed builds
  only."`, `_` → `"Update check failed or timed out."`. The card switch below (`:786-816`) reuses
  two of the literals as card subtitle text (`:805`) and titles (`:811`).
- `src/CLIHub/ViewModels/Updates/UpdateControlViewModel.cs` (`namespace CLIHub.ViewModels` at `:1`),
  `CheckForUpdatesAsync` `:122-155`: the identical switch at `:144-150`, plus a duplicated catch-path
  literal `"Update check failed or timed out."` at `:140`, and a separate download-outcome switch at
  `:183-190` that shares only `"Updates apply to installed builds only."` (`:188`).
- Cross-surface pin: `tests/CLIHub.Tests/ViewModels/Updates/SharedUpdatePresentationTests.cs:67-72`
  asserts these exact strings for both surfaces.
- Wave-1 overlap check: `simplify-mechanical-sweep` added `ResolvePositiveIntOr` around
  `SettingsViewModel.cs:309-317` — a different region of the same file; the outcome switch numbers
  above are post-wave-1 and were read fresh for this design.
- Backup `491fd27` (diff verified): added `src/CLIHub/Helpers/UpdateOutcomeText.cs`
  (`internal static class ForCheck(UpdateCheckResult result)` owning the four literals) +
  `tests/CLIHub.Tests/Helpers/UpdateOutcomeTextTests.cs` (one `[Theory]`, four `[InlineData]`
  cases); rewired exactly the two check-outcome switches (`this.UpdateMessage =
  UpdateOutcomeText.ForCheck(result);` and `this.ReportOutcome(UpdateOutcomeText.ForCheck(result));`);
  left the catch literal, the download switch, and the card texts inline.
- Master placement facts: `src/CLIHub/Helpers/` does not exist on master (directory list verified:
  Assets, Behaviors, Composition, Controls, Converters, Diagnostics, Hotkeys, Imaging, Interop,
  Launching, Lifetime, Settings, Startup, Themes, Theming, Tray, Updates, ViewModels, Views). Since
  `2026-11-09-tidy-application-folders`, VM-shared helpers live in `src/CLIHub/ViewModels/Shared/`
  and all six files there declare `namespace CLIHub.ViewModels;` (`MenuAction.cs:1`,
  `PreferenceSyncHelper.cs`, `PromptState.cs`, `RestartablePass.cs`, `StatusMessageCoordinator.cs`,
  `WindowActionCoordinator.cs` — verified). Tests for them live in
  `tests/CLIHub.Tests/ViewModels/Shared/` with `namespace CLIHub.Tests.ViewModels.Shared;`
  (3 of 4 existing files; `WindowActionCoordinatorTests.cs` matches, `StatusMessageCoordinatorTests.cs`
  is a one-off outlier with `CLIHub.ViewModels`).

### 4 — atomic temp-file write ×3 and ADR-0013 (item 4)

The duplicated core, exactly as ADR-0013 described it ("The duplicated code is two statements -
write the temp file, move it over the target", `docs/adr/0013-no-generic-persistence-cache.md:22-23`):

| Consumer | Site | Mechanics | Surrounding policy (stays inline) |
|---|---|---|---|
| `ConfigurationRepository.WriteConfig` | `src/CLIHub.Core/Configuration/ConfigurationRepository.cs:235-267` | tempPath `:240`, directory create `:241-245`, `File.WriteAllText` + `File.Move(overwrite: true)` `:257-258` | `writeLock` `:247`, generation discard `:249-255`, try/catch warn `:237,263-266`, debounce/worker elsewhere |
| `LogoCacheService.Save` | `src/CLIHub.Core/Infrastructure/Persistence/LogoCacheService.cs:155-178` | directory create `:167-171`, tempPath `:173`, write+move `:174-175` | dirty flag `:176`, dispose-time save, caller try/catch `:115-123` |
| `PluginStateRepository.Save` | `src/CLIHub.Core/Plugins/PluginStateRepository.cs:76-98` | directory create `:82-86`, tempPath `:90`, write+move `:91-92` | try/catch warn `:80,94-97`; direct synchronous save (this consumer post-dates the backup) |

Backup `af70805` (diff verified): added
`src/CLIHub.Core/Infrastructure/FileSystem/AtomicFileWriter.cs` — `internal static class` with
`internal static void Write(string targetPath, string contents)`: compute `tempPath =
targetPath + ".tmp"`, ensure `Path.GetDirectoryName(targetPath)` via `Directory.CreateDirectory`,
`File.WriteAllText(tempPath, contents)`, `File.Move(tempPath, targetPath, overwrite: true)` — and
rewired the then-two consumers, leaving each call site's locks, generation check, flags, and
try/catch untouched. The folder `src/CLIHub.Core/Infrastructure/FileSystem/` exists on master with
`AppPaths.cs` and `DirectoryInitializer.cs`, and **all three consumers already import it** (for
`AppPaths`): `ConfigurationRepository.cs:5`, `LogoCacheService.cs:7`, `PluginStateRepository.cs:6`.

Behavior pins that must keep passing:

- `tests/CLIHub.Core.Tests/Configuration/ConfigurationRepositoryTests.cs:291`
  `Update_WhenAtomicReplacementCannotCreateTempFile_PreservesPreviousValidJson` — blocks the temp
  path by creating a **directory** at `this.ConfigPath + ".tmp"` (`:297`); proves the `<target>.tmp`
  naming convention and the "previous valid JSON survives a failed write" outcome.
- `tests/CLIHub.Core.Tests/Plugins/PluginStateRepositoryTests.cs:50`
  `Save_WritesAtomicallyViaTempFileThenRename` (asserts at `:63` the temp file is renamed away) and
  `:68` `SaveCreatesMissingParentDirectory`; fixture deletes `stateFilePath + ".tmp"` (`:21`).
- Logo-cache atomic write is a spec scenario: `openspec/specs/logo-cache/spec.md` "Safe cache file
  handling → Atomic write" ("written through a temporary file and moved into place").
- Configuration atomic replacement is a spec scenario: `openspec/specs/configuration-snapshot/spec.md`
  "Snapshot persistence preserves the document without schema migration → Repository retains atomic
  single-file persistence".

ADR-0013 (`docs/adr/0013-no-generic-persistence-cache.md`):

- Decision `:39-41`: "Do not introduce `IPersistenceService<T>` or `ICacheService`. Keep the atomic
  write inline where each policy lives (configuration and logo cache)…"
- Context `:13-20` lists exactly two persistence consumers ("Nothing else carries the atomic
  temp+rename pattern").
- Reopen triggers `:60-64`: "**1.** A third JSON-persisted consumer appears whose concurrency model
  matches an existing one (debounced writer or dispose-only writer), making the extraction a
  **deduplication** rather than a generalization."
- `PluginStateRepository` is that third JSON-persisted consumer (`plugin-state.json`,
  `PluginStateRepository.cs:11-14` even documents "mirroring the persistence pattern of
  `ConfigurationRepository`"): a synchronous, lock-free, tolerant-read writer whose two-statement
  core matches the other two. The trigger's intent — deduplication, not generalization — is
  satisfied by a parameterless static helper; its literal "(debounced writer or dispose-only
  writer)" clause matches the *policy* taxonomy, and `PluginStateRepository`'s policy is a third
  (direct save) shape, which the option analysis below weighs (D4).

### 5 — `PreferenceReader` duplication (item 5)

`src/CLIHub.Core/Logging/PreferenceReader.cs` (115 lines, `public static class` `:9`):

- `ReadLogLevel` `:15-37` — full inline try/parse/catch over `preferences.logLevel` returning
  `logLevel.GetString()`.
- `ReadString` `:67-89` — the identical skeleton parameterized by `propertyName`.
- `ReadInt` `:91-114` — same skeleton plus `value.TryGetInt32(out var result)` inside the property
  guard; falls through to `return null` at `:113` when the property exists but is not an int.
- Callers: `ReadLogRetentionDays`/`ReadLogFileSizeLimitMb` → `ReadInt` (`:43-51`),
  `ReadAppTheme`/`ReadAccentColor` → `ReadString` (`:57-65`), `ReadLogLevel` inline.
- Backup `491fd27` final file (verified): `ReadLogLevel(...) => ReadString(configFilePath,
  "logLevel");`, `ReadString(...) => ReadPreference<string>(configFilePath, propertyName, value =>
  value.GetString());`, `ReadInt(...) => ReadPreference<int?>(configFilePath, propertyName, value =>
  value.TryGetInt32(out var result) ? result : null);`, and one
  `private static T? ReadPreference<T>(string configFilePath, string propertyName,
  Func<JsonElement, T?> read)` owning the file-exists check, the `preferences`/`propertyName` walk,
  and the tolerant catch.
- Behavior-identity argument (per branch): file missing → `default` (was `null`); property absent →
  falls out of try → `default` (was `null`); property present → `read(value)` (string:
  `GetString()`, identical; int: `TryGetInt32` success → value, failure → `null` from inside try —
  the current code reaches the same `null` by falling through to `:113`); any exception → catch →
  `default` (was `null`), same comment retained. `ReadLogLevel` delegating to `ReadString` is
  identical because its inline body is the `ReadString` body with a fixed property name.
- Test coverage: no direct `PreferenceReader` tests exist on master or the backup (solution-wide
  grep over `tests/` for `PreferenceReader`: zero matches); behavior is exercised indirectly by
  logging/startup configuration paths. The backup added no reader tests; the port adds none (D6).

### 6 — `ApplicationSession` inline shell construction (item 6)

- `src/CLIHub/Lifetime/ApplicationSession.cs` (89 lines): six dependency fields `:14-19`
  (`IApplicationOperationLifetime`, `Func<IApplicationStartupUi>`,
  `Func<IUpdateRequestSource>`, `ISingleInstanceGuard`, `IUpdateWorkflow`, `SystemThemeFollower`),
  `shellCoordinator`/`disposed` `:21-22`, six-parameter constructor `:34-47`, `Start` `:50-75` with
  `this.shellCoordinator = new ShellCoordinator(this.operationLifetime, this.startupUiFactory,
  this.updateRequestSourceFactory, this.singleInstanceGuard, this.updateWorkflow,
  this.themeFollower, context);` at `:65-72`, `Dispose` `:78-88`. Usings `:3-6`:
  `CLIHub.Core.Windows`, `CLIHub.Startup`, `CLIHub.Theming`, `CLIHub.Updates`.
- `src/CLIHub/Startup/ShellCoordinator.cs:43-49` — constructor takes exactly those six dependencies
  plus `ApplicationStartupContext context`, guards each, and runs `InitializeShell()` at `:59`.
- `src/CLIHub/Startup/ApplicationStartupContext.cs:8-9` —
  `public sealed record ApplicationStartupContext(Action<Action> Dispatch,
  Action<LaunchWindow> SetMainWindow);` (master uses `Action<LaunchWindow>`; the backup-era record
  used `Action<IStartupWindow>` — only the test fixture's callback types are affected).
- Registration: `src/CLIHub/Composition/ServiceRegistration.cs` — `AddStartupServices` `:71-88`,
  `services.AddSingleton<IApplicationSession, ApplicationSession>();` at `:77`,
  `services.AddSingleton<SystemThemeFollower>();` at `:78`. Usings `:3-23` already include every
  namespace the factory closure needs: `CLIHub.Core.Windows` (`:7`, `ISingleInstanceGuard`),
  `CLIHub.Lifetime` (`:11`, `IApplicationOperationLifetime`), `CLIHub.Startup` (`:13`,
  `Func<IApplicationStartupUi>`), `CLIHub.Theming` (`:14`, `SystemThemeFollower`),
  `CLIHub.Updates` (`:16`, `Func<IUpdateRequestSource>`/`IUpdateWorkflow`).
- Construction/consumption of the session: `ApplicationBootstrapper` injects `IApplicationSession`
  (`ApplicationBootstrapper.cs:30`), `ApplicationHost` disposes the resolved session
  (`ApplicationHost.cs:101`); `ShellCoordinator` is constructed nowhere else in `src/`
  (solution-wide grep). Pins: `ArchitectureBoundaryTests.cs:83` asserts
  `Assert.Contains("IApplicationSession, ApplicationSession", registration)` against
  `ServiceRegistration.cs` text; `ServiceRegistrationTests.cs:37` resolves `IApplicationSession`
  from the built provider and `:147-148` resolves it twice for singleton identity; resolving the
  session must not construct WPF shell types (the factory is invoked only inside `Start`).
- Backup `50d28af` + archive `2026-10-28-application-session-shell-factory` (both verified): session
  constructor reduced to `Func<ApplicationStartupContext, IShellCoordinator> shellFactory` alone;
  `Start` keeps its guard order (null context, null `context.Dispatch`, disposed, already-started
  short-circuit) and becomes `this.shellCoordinator = this.shellFactory(context);`; registration
  adds `services.AddSingleton<Func<ApplicationStartupContext, IShellCoordinator>>(sp => context =>
  new ShellCoordinator(sp.GetRequiredService<IApplicationOperationLifetime>(),
  sp.GetRequiredService<Func<IApplicationStartupUi>>(), sp.GetRequiredService<Func<IUpdateRequestSource>>(),
  sp.GetRequiredService<ISingleInstanceGuard>(), sp.GetRequiredService<IUpdateWorkflow>(),
  sp.GetRequiredService<SystemThemeFollower>(), context));` before the session registration; tests
  reworked to a fixture whose stub factory records contexts and builds a real `ShellCoordinator`,
  adding `Start_CalledTwice_InvokesShellFactoryOnce` and
  `Start_PassesSameContextInstanceToShellFactory`.

### Build/style constraints

- `src/CLIHub.Core/CLIHub.Core.csproj:28-31` and `src/CLIHub/CLIHub.csproj:25` grant
  `InternalsVisibleTo` to the test projects, so `internal` helpers are testable without `public`.
- `GenerateDocumentationFile` is on for source projects; XML docs on public members are enforced,
  and repo style requires docs on new types regardless of accessibility (all cited backup helpers
  carry them).
- No master source file carries a `// <file …>` header comment (wave-1 D1); the backup's headers are
  dropped when porting.
- `Directory.Build.props` enforces `EnforceCodeStyleInBuild` (`this.` qualification, using
  placement after the file-scoped namespace, IDE0161); the new files follow
  `MenuAction.cs:1-3` / `AtomicFileWriter` backup shape: namespace first, usings after.

## Decisions

**D1 — `RepositoryUrl` becomes `private const`; comment replaced with backup wording.**
Backup `08efe63` made the const private and rewrote the comment; master's grep shows zero external
references, so `internal` is now unjustified visibility. Port both. Keep master's
`prerelease: false` argument (see Deviations). Keep the still-true sentence "Only stable releases
are published, so the source never includes pre-release builds." over the return; delete only the
stale non-GitHub sentence of the method comment (`:91-92`).

**D2 — `UpdateVersionText` ported verbatim minus the file header; all three `Normalize` methods
deleted.** Rather than keeping one-line delegators, the five call sites call
`UpdateVersionText.StripBuildMetadata(...)` directly (backup shape) — three private aliases on top
of a shared helper would re-create the indirection the extraction removes. The helper stays
`internal` (not public): it is an implementation detail of the Updates subsystem and
`CLIHub.Core.Tests` sees internals.

**D3 — `UpdateOutcomeText` lives in `ViewModels/Shared/` under `namespace CLIHub.ViewModels`.**
The backup's `Helpers/CLIHub.Helpers` placement no longer exists on master; the established
convention (six files, verified) is folder `ViewModels/Shared/` with namespace `CLIHub.ViewModels`.
Both consumers already declare that namespace, so no using directives change in either file — the
smallest possible diff. The test goes to `tests/CLIHub.Tests/ViewModels/Shared/` with namespace
`CLIHub.Tests.ViewModels.Shared` (majority convention of the four existing files there). Scope is
the backup's: `ForCheck` owns exactly the four check-outcome literals; the catch literal
(`UpdateControlViewModel.cs:140`), the download-outcome switch (`:183-190`), and the
`SettingsUpdateCard` subtitle/title texts stay inline because they are not the check-outcome
message (only one literal coincidentally overlaps).

**D4 — `AtomicFileWriter` covers all three consumers (option a); ADR-0013 trigger 1 recorded as
exercised.** Option analysis:

- *Chosen (a): minimal behavior-identical helper for all three consumers.* It deduplicates exactly
  the two statements ADR-0013 itself identified as the shared code (`:22-23`) and nothing else: no
  interface, no generic parameter, no policy options; debounce, locks, generation counter, dirty
  flag, and every try/catch remain at their call sites. The ADR's decision — no generic
  persistence/cache abstraction — is untouched; the reopen trigger anticipated exactly this
  extraction once a third consumer appeared ("the extraction will be revisited with three real
  data points", `:55-56`). We now have three real data points and they confirm the ADR's analysis:
  the policies are mutually exclusive and stay inline; only the two statements are shared.
  Regarding the trigger's parenthetical "(debounced writer or dispose-only writer)":
  `PluginStateRepository`'s policy is a direct synchronous save, a third policy shape — but the
  trigger's operative clause is "making the extraction a deduplication rather than a
  generalization", and a parameterless static helper cannot generalize anything. Reading the
  clause as a strict policy-type match would mean the trigger can never fire for any future
  consumer that isn't a clone of an existing policy, which reduces the recorded trigger to dead
  text — contrary to the ADR's stated commitment ("recording reopen triggers so this is a decision,
  not an omission", `:58`).
- *Rejected (b): scope the helper to `LogoCacheService` + `PluginStateRepository`, leave
  `ConfigurationRepository` inline.* This is the conservative reading of "the single camelCase
  configuration document's atomic persistence boundary". Rejected because (i) it preserves a third
  copy of the exact two statements the ADR called "the duplicated code"; (ii) it leaves the
  codebase with two idioms for the same write (helper vs inline) in sibling repositories, which is
  how the next copy gets written; (iii) the equivalence argument for the config site holds (next
  paragraph); and (iv) the config write's real protection — debounce, `writeLock`, generation
  discard, its test pins — is unaffected by who spells the two statements.
- *Behavior equivalence at the configuration site (the only non-trivial part).* On master the
  directory-ensure runs **before** `writeLock` and the generation check sits between it and the
  write (`:240-258`); the helper does directory-ensure → write → move as one unit, so the order
  becomes: generation check (inside `writeLock`) → helper call. Two deltas, both unobservable:
  (1) `Directory.CreateDirectory` moves inside `writeLock` — it is idempotent and non-throwing for
  an existing directory, the lock serializes writers only, and for the default path the directory
  already exists by construction (the repository constructor creates `AppPaths.Root`, `:43-47`;
  startup's directory initialization creates the structure before any write per the app-lifecycle
  spec). (2) When the generation check discards a superseded write, the current code has already
  created the directory as a side effect while the port does nothing — but no file is written in
  either version and the surviving write creates the directory, so the resulting `config.json` and
  directory tree are identical. The pinned failure mode is preserved: with a directory parked at
  `<config>.tmp` (`ConfigurationRepositoryTests.cs:291-297`), the helper's `File.WriteAllText`
  throws the same `UnauthorizedAccessException`/`IOException` inside the same try/catch, the
  previous JSON survives, and the warning logs as before.
- *Bookkeeping.* The change appends a dated note under ADR-0013's "Reopen triggers" recording that
  trigger 1 was exercised on 2026-11-12 by this change with the minimal-deduplication outcome. The
  ADR's decision text is not edited (the immutability rule in `docs/adr/README.md` forbids changing
  meaning; recording a trigger's exercise does not).

**D5 — `PreferenceReader` ports the backup's `ReadPreference<T>` shape verbatim, including the
`ReadLogLevel` delegation.** The public surface (`ReadLogLevel`, `ReadLogRetentionDays`,
`ReadLogFileSizeLimitMb`, `ReadAppTheme`, `ReadAccentColor` and their doc comments) is unchanged;
three copies of the skeleton collapse into one. No new tests: none exist for this class on master
or the backup, the refactor is copy-then-delete, and adding a test harness for bootstrap JSON
parsing is out of a port's scope (Q1 records the option).

**D6 — `ApplicationSession` takes only the factory (backup's final shape), not six deps + a
factory.** The alternative — keep the six constructor dependencies and add a factory parameter —
would leave the session holding six fields whose only use is building the coordinator once; the
session's responsibility is subscription lifecycle, not shell construction (its class doc says
"Delegates shell creation and event wiring to the shell coordinator"). The factory-only shape
moves the dependency list to the composition root, matches the archived change exactly, and
matches existing registration idioms (`Func<...>` accessors at `ServiceRegistration.cs:81-82` and
the multi-line `sp => new GlobalHotkeyService(...)` shape). The registration must precede
`AddSingleton<IApplicationSession, ApplicationSession>()` conceptually (DI resolves the `Func` as a
constructor argument); both live in `AddStartupServices`. `Start`'s guard order and `Dispose` are
byte-identical to master. The two new facts pin the factory contract (invoked once, receives the
caller's context instance).

**D7 — test-count gate accounting differs from wave 1.** Wave 1 added no tests, so its gate was
"totals equal to baseline". This change adds tests (3 facts + 1×4-case theory + 2 facts) and
reworks one fixture; the gate is "0 failed, and the passed total equals baseline + 9 new passing
cases (3 + 4 + 2), with no existing test deleted or skipped". The reworked
`ApplicationSessionTests` keeps all three existing fact names and assertions.

**D8 — no `// <file …>` header comments on ported files** (established wave-1 D1; verified against
master's current sources).

## Alternatives rejected

- *Option (b) for `AtomicFileWriter`* — see D4 for the full analysis.
- *Keep `internal` on `RepositoryUrl`* — visibility with zero consumers is noise; the backup
  already chose `private`.
- *One-line `Normalize` delegators instead of call-site rewiring* — keeps three dead private
  methods; rejected (D2).
- *Extending `UpdateOutcomeText` to the download-outcome switch and card texts* — broader than the
  backup, touches literals only one surface uses, and breaks the `ForCheck` naming; rejected
  (D3, Q2).
- *Direct unit tests for `AtomicFileWriter` / `PreferenceReader`* — every observable behavior is
  already pinned through the three repositories' tests and the logging/startup paths; standalone
  helper tests would duplicate those pins. Rejected for this port (Q1).

## Deviations from the backup sources

1. `UpdateService`: backup's final file passes a `prerelease` parameter into `GithubSource`; master
   removed that parameter, so the port keeps master's `prerelease: false` argument. The backup's
   `IPreferencesStore` field and `CreateForTesting` shape also predate master's current
   test seam — only the branch/comment/const hunk is ported.
2. `UpdateOutcomeText`: moved from `src/CLIHub/Helpers/` (`namespace CLIHub.Helpers`) to
   `src/CLIHub/ViewModels/Shared/` (`namespace CLIHub.ViewModels`); test moved from
   `tests/CLIHub.Tests/Helpers/` to `tests/CLIHub.Tests/ViewModels/Shared/` (D3).
3. `AtomicFileWriter`: master has a third consumer (`PluginStateRepository`) the backup never saw;
   the port rewires it too (D4).
4. `ApplicationSessionTests`: master's fixture constructs `SystemThemeFollower` inline in the
   session constructor call with `using CLIHub.Theming;` (`:8`); the port hoists it to a fixture
   property (backup shape) and the `ApplicationStartupContext` callbacks are typed for master's
   `Action<LaunchWindow> SetMainWindow` (backup-era used `Action<IStartupWindow>`).
5. All ported files drop the backup's `// <file …>` header comments (D8).
6. Backup `50d28af`'s `ServiceRegistration` needed no using changes; on master the same is true but
   for a different namespace set (`CLIHub.Theming` vs the backup's `CLIHub.Themes`) — verified in
   Context §6.

## Open questions

1. **`PreferenceReader` test coverage.** No direct tests exist on master or the backup. The port
   adds none (D5/D6). If the implementation session prefers a safety net, a small
   `ReadPreference` theory over a temp `config.json` (missing file, missing property, wrong-typed
   int, malformed JSON) would fit `tests/CLIHub.Core.Tests/Logging/` — optional, not required by
   the gates.
2. **`UpdateOutcomeText` scope.** The download-outcome switch and card subtitles stay inline
   (backup scope). If the maintainer wants the shared `"Updates apply to installed builds only."`
   deduplicated everywhere, that is a follow-up with a different helper name (`ForDownload`), not
   this change.
3. **Line-number drift.** All cited line numbers are valid at `8acfbf3`. If another change lands
   first and touches `SettingsViewModel.cs`, `UpdateControlViewModel.cs`,
   `ConfigurationRepository.cs`, `LogoCacheService.cs`, `PluginStateRepository.cs`,
   `PreferenceReader.cs`, `UpdateService.cs`/`UpdateDownloader.cs`/`UpdateInstaller.cs`,
   `ApplicationSession.cs`, or `ServiceRegistration.cs`, re-read the file and re-verify the cited
   lines before editing.
4. **ADR addendum wording.** The task proposes one sentence under ADR-0013's reopen triggers. If
   the maintainer reads the ADR immutability rule strictly (no edits at all to accepted records),
   the addendum task can be dropped without affecting any code; the design/proposal already record
   the trigger analysis.
