# Port the Core/UI cleanup bundle and the shell-coordinator factory from the backup branch

## Why

Six small duplications or dead branches remain on master after wave 1 of the backup port. Each was
already solved on `backup-before-reset` and each keeps costing re-reading or inviting a fresh copy:

- **Dead update-source branch.** `UpdateService.CreateDefaultManager` still branches on
  `RepositoryUrl.Contains("github.com", ...)` (`UpdateService.cs:94`) even though `RepositoryUrl` is
  a compile-time constant GitHub URL (`:20`), so the `new UpdateManager(RepositoryUrl)` fallback
  (`:99`) is unreachable by construction, and the field comment still advertises "A non-GitHub URL or
  local folder is also supported" (`:18-19`) — support the code no longer intends.
- **Build-metadata strip duplicated three times.** The private `Normalize` (`IndexOf('+')` +
  substring) is byte-identical in `UpdateDownloader.cs:115-119`, `UpdateInstaller.cs:101-105`, and
  `UpdateService.cs:311-315`, with five call sites (`UpdateDownloader.cs:77`,
  `UpdateInstaller.cs:41,77`, `UpdateService.cs:115,122,253`).
- **Update-check outcome text duplicated.** The four-literal outcome switch ("Up to date (v…)",
  "Updates apply to installed builds only.", …) exists verbatim in
  `SettingsViewModel.ApplyUpdateResult` (`SettingsViewModel.cs:778-784`) and
  `UpdateControlViewModel.CheckForUpdatesAsync` (`UpdateControlViewModel.cs:144-150`); the strings
  are cross-surface-pinned by `SharedUpdatePresentationTests.cs:67-72`.
- **Atomic temp-file write duplicated three times.** "ensure directory → write `<target>.tmp` →
  `File.Move(..., overwrite: true)`" now appears in `ConfigurationRepository.WriteConfig`
  (`ConfigurationRepository.cs:240-258`), `LogoCacheService.Save` (`LogoCacheService.cs:167-175`),
  and — new on master since the backup branched — `PluginStateRepository.Save`
  (`PluginStateRepository.cs:82-92`). ADR-0013 rejected a *generic persistence abstraction* but
  recorded an explicit reopen trigger for "a third JSON-persisted consumer … making the extraction a
  **deduplication** rather than a generalization" (`docs/adr/0013-no-generic-persistence-cache.md:60-64`).
  That trigger is now met.
- **`PreferenceReader` try/parse/catch skeleton duplicated.** `ReadLogLevel` (`:15-37`),
  `ReadString` (`:67-89`), and `ReadInt` (`:91-114`) each carry the full file-exists → parse →
  tolerant-catch body; only the value extraction differs.
- **`ShellCoordinator` constructed inline in the session.** `ApplicationSession.Start` news up the
  coordinator with six session-held dependencies (`ApplicationSession.cs:65-72`), so the session is
  untestable without the full dependency set and the archived factory shape
  (`2026-10-28-application-session-shell-factory`, backup `50d28af`) never landed on master.

The backup branch already contains verified solutions for all six (commits `08efe63`, `491fd27`,
`af70805`, `50d28af`, read via `git show`). This change re-lands them as one bundle, adapted to
master's current code. Every item is behavior-preserving: the update-source branch is unreachable,
the extractions are copy-then-delete, and the factory moves construction from `Start` to a DI closure
without changing the session's observable lifecycle.

## What Changes

All master facts below were re-verified by this preparation session at HEAD `8acfbf3` against the
clean working tree (evidence with `file:line` in `design.md`).

**Stage 1 — UpdateService cleanups (backup `08efe63` + `491fd27`):**

1. **Dead branch removal.** In `src/CLIHub.Core/Updates/UpdateService.cs`: delete the
   `RepositoryUrl.Contains("github.com", ...)` conditional and the `new UpdateManager(RepositoryUrl)`
   fallback so `CreateDefaultManager` unconditionally returns
   `new UpdateManager(new GithubSource(RepositoryUrl, null, prerelease: false))` — **master's
   argument list, not the backup's** (backup still had a `prerelease` parameter; master hardcodes
   `prerelease: false` since remove-prerelease-updates-option). Replace the stale field comment
   (`:18-19`) and reduce `internal const RepositoryUrl` (`:20`) to `private const` (backup shape;
   zero references outside the file, verified by solution-wide grep).
2. **`UpdateVersionText` extraction.** Port `src/CLIHub.Core/Updates/UpdateVersionText.cs` from the
   backup (~19 lines, `internal static class`, `StripBuildMetadata`), adapted to master style (no
   `// <file>` header comment). Delete the three private `Normalize` methods and rewire all five
   call sites to `UpdateVersionText.StripBuildMetadata(...)`. Port
   `tests/CLIHub.Core.Tests/Updates/UpdateVersionTextTests.cs` (3 facts).

**Stage 2 — `AtomicFileWriter` (backup `af70805`, adapted to three consumers):**

3. Port `src/CLIHub.Core/Infrastructure/FileSystem/AtomicFileWriter.cs` from the backup: an
   `internal static class` with one `Write(string targetPath, string contents)` that ensures the
   target directory, writes `<targetPath>.tmp`, and `File.Move`s it over the target with
   `overwrite: true`. Rewire all **three** consumers — `ConfigurationRepository.WriteConfig`,
   `LogoCacheService.Save`, and `PluginStateRepository.Save` (the third post-dates the backup).
   All three files already have `using CLIHub.Core.Infrastructure.FileSystem;` (for `AppPaths`), so
   no using changes. This is a minimal deduplication of exactly the two statements ADR-0013 named
   ("write the temp file, move it over the target"), NOT a persistence abstraction: every policy —
   debounce, `writeLock`, generation discard, dirty flag, try/catch logging — stays at its call
   site. Design.md records the option analysis and the behavior-equivalence argument for the
   configuration boundary, and the change appends a dated note under ADR-0013's reopen triggers
   recording that trigger 1 was exercised.

**Stage 3 — `PreferenceReader` generic reader (backup `491fd27`):**

4. In `src/CLIHub.Core/Logging/PreferenceReader.cs` port the backup's
   `ReadPreference<T>(string, string, Func<JsonElement, T?>)`; reduce `ReadString` and `ReadInt` to
   one-line delegations and `ReadLogLevel` to `ReadString(configFilePath, "logLevel")`
   (behavior-identical: same tolerant-catch, same null fallbacks, same `TryGetInt32` semantics —
   argument in design.md).

**Stage 4 — `UpdateOutcomeText` (backup `491fd27`, placement adapted):**

5. Port the backup's `UpdateOutcomeText.ForCheck(UpdateCheckResult)` — owning the four check-outcome
   literals once — as `src/CLIHub/ViewModels/Shared/UpdateOutcomeText.cs` with namespace
   `CLIHub.ViewModels` (master's post-tidy-application-folders convention; the backup's
   `Helpers/CLIHub.Helpers` placement no longer exists on master). Both consumers declare
   `namespace CLIHub.ViewModels`, so no using directives change. Rewire the two switches
   (`SettingsViewModel.cs:778-784`, `UpdateControlViewModel.cs:144-150`); the download-outcome
   switch (`UpdateControlViewModel.cs:183-190`) and the `SettingsUpdateCard` subtitles stay inline
   (backup scope). Port the theory test as
   `tests/CLIHub.Tests/ViewModels/Shared/UpdateOutcomeTextTests.cs`.

**Stage 5 — shell coordinator factory (backup `50d28af`):**

6. Port the backup's `ApplicationSession` shape: the constructor takes only
   `Func<ApplicationStartupContext, IShellCoordinator> shellFactory` (the six dependencies move to
   the registration site), `Start` becomes `this.shellCoordinator = this.shellFactory(context);`.
   In `src/CLIHub/Composition/ServiceRegistration.cs` (`AddStartupServices`, `:71-88`) register
   `Func<ApplicationStartupContext, IShellCoordinator>` as the multi-line `sp => context =>
   new ShellCoordinator(...)` closure (backup's exact registration shape; all required namespaces
   are already imported) and keep `AddSingleton<IApplicationSession, ApplicationSession>()` so the
   architecture-boundary pin (`ArchitectureBoundaryTests.cs:83`) still passes. Rework
   `tests/CLIHub.Tests/Services/ApplicationSessionTests.cs` to the backup's post-port shape: fixture
   builds a real `ShellCoordinator` through a recording stub factory; the three existing facts keep
   their names and assertions; port the two new facts
   (`Start_CalledTwice_InvokesShellFactoryOnce`, `Start_PassesSameContextInstanceToShellFactory`).

**Stage 6 — ADR bookkeeping and final gates** (ADR-0013 addendum, build, test, validate, graphify).

## Impact

- New files (5): `src/CLIHub.Core/Updates/UpdateVersionText.cs`,
  `src/CLIHub.Core/Infrastructure/FileSystem/AtomicFileWriter.cs`,
  `src/CLIHub/ViewModels/Shared/UpdateOutcomeText.cs`,
  `tests/CLIHub.Core.Tests/Updates/UpdateVersionTextTests.cs`,
  `tests/CLIHub.Tests/ViewModels/Shared/UpdateOutcomeTextTests.cs`.
- Modified files (13): the six Core files above plus
  `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`,
  `src/CLIHub/ViewModels/Updates/UpdateControlViewModel.cs`,
  `src/CLIHub/Lifetime/ApplicationSession.cs`, `src/CLIHub/Composition/ServiceRegistration.cs`,
  `tests/CLIHub.Tests/Services/ApplicationSessionTests.cs`, and a dated addendum in
  `docs/adr/0013-no-generic-persistence-cache.md`.
- Test impact: +3 facts (`UpdateVersionTextTests`), +1 theory with 4 inline cases
  (`UpdateOutcomeTextTests`), +2 facts and a fixture rework in `ApplicationSessionTests`; no other
  test file is edited or deleted. Existing pins that must keep passing unchanged:
  `SharedUpdatePresentationTests.cs:67-72` (outcome strings),
  `UpdateServiceTests.cs:97` (`Assert.DoesNotContain("+", version)`),
  `ConfigurationRepositoryTests.cs:291` (temp-file-blocked atomic write),
  `PluginStateRepositoryTests.cs:50,68` (temp-then-rename, parent-directory creation),
  `ArchitectureBoundaryTests.cs:83`, `ServiceRegistrationTests.cs:37,147-148`.
- User-visible impact: none. The removed update-source branch is unreachable by construction; every
  extraction is copy-then-delete with call-site rewiring.
- Spec impact: none — `skip_specs: true`. No spec scenario constrains the source-branch shape, the
  helper file layout, the reader's internal structure, or how the session obtains its coordinator;
  the specs that touch the affected behavior (`update-checking`, `logo-cache` "Atomic write",
  `configuration-snapshot` "Repository retains atomic single-file persistence", `shell-coordinator`,
  `app-lifecycle`) describe observable outcomes this change preserves verbatim.

## Non-goals

- No behavior change of any kind. In particular the `prerelease: false` argument, the tolerant
  `catch` blocks, the debounce/lock/generation machinery in `ConfigurationRepository`, and the
  session's `Start`/`Dispose` contract stay exactly as on master.
- **Not** porting the UI half of backup `08efe63` — the `PreferenceSyncHelper` inlining into
  `LaunchWindowViewModel`. Master already owns a different `PreferenceSyncHelper` at
  `src/CLIHub/ViewModels/Shared/PreferenceSyncHelper.cs`; that file and its tests are untouched.
- **Not** porting the rest of backup `491fd27` or `af70805`: the Settings view-model decomposition
  (`SettingsOptions`, `SettingsUpdateStatusPresenter`), tray/file-header work
  (`TrayIconController`, `WindowsRuntimeLocator`, the removed `// <file>` headers), project/scanning
  changes (`ProjectService`, `ProjectScanningService`), `AgentProbeDefaults`, `FolderPathInput`,
  the `VelopackManagerProvider` removal, `AgentCardItem`/`AgentManagementViewModel` rework, and the
  recording-logger fakes.
- **Not** introducing `IPersistenceService<T>`, `ICacheService`, or any persistence/cache interface,
  repository wrapper, or policy parameter — ADR-0013's decision stands; `AtomicFileWriter` is a
  two-statement static helper with no knobs.
- **Not** adding direct `PreferenceReader` or `AtomicFileWriter` unit tests (none exist on master or
  the backup; behavior is pinned through the three consumer repositories' existing tests).
- No commit, push, or archive in this change's tasks; those follow the repo approval policy.

## Dependencies

- Porting sources, read via `git show`: backup `08efe63` (dead branch), `491fd27`
  (`UpdateVersionText`, `UpdateOutcomeText`, `PreferenceReader`), `af70805` (`AtomicFileWriter`),
  `50d28af` + archive `2026-10-28-application-session-shell-factory` (factory), and the final backup
  files under `backup-before-reset:`.
- **ADR-0013** (`docs/adr/0013-no-generic-persistence-cache.md`): the change exercises reopen
  trigger 1 (third JSON-persisted consumer). The design records the option analysis — extract the
  minimal helper for all three consumers (chosen) versus scoping it to the two non-config consumers
  and leaving `ConfigurationRepository` inline (rejected) — and why neither option violates the
  ADR's decision.
- Wave-1 changes (`2026-11-12-probe-cache-extraction`, `2026-11-12-simplify-mechanical-sweep`,
  `2026-11-12-dialog-and-chrome-helpers`) are merged and archived; none share a file with this
  change except `SettingsViewModel.cs`, whose wave-1 edit (`ResolvePositiveIntOr`, lines ~309-317)
  does not overlap the outcome switch at `:778-784`. No other in-flight change is known; if any
  change touches these files before implementation, re-verify the cited line numbers first
  (design.md Q3).
- No spec dependencies: `skip_specs: true`, pure internal refactor.
