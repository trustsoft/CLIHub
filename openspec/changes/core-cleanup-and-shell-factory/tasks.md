# Tasks: core-cleanup-and-shell-factory

Master line numbers below were verified at HEAD `8acfbf3`; if any other change touches these files
first, re-read them and re-verify the cited lines before editing (design.md Q3). Porting sources
are read with `git show backup-before-reset:<path>` and `git show <sha> -- <path>`.

**Smoke-gate protocol:** implementation pauses after every stage's `Manual smoke (user)` task until
the user confirms the smoke result; the orchestrator ticks the smoke checkbox only after that
confirmation, and the next stage begins only then.

## 1. UpdateService cleanups (proposal items 1-2)

- [ ] 1.1 Baseline capture: run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`; record the
      warning count (expected 0; unrelated pre-existing warnings are out of scope) and the exact
      passed/failed test totals as the gate baseline for every later stage
- [ ] 1.2 In `src/CLIHub.Core/Updates/UpdateService.cs` change `internal const string RepositoryUrl`
      (`:20`) to `private const string RepositoryUrl` and replace the two-line field comment
      (`:18-19`) with the backup's single line: `// The GitHub repository whose Releases feed is
      the Velopack update source (GitHub Releases only).` (design.md D1; zero external references,
      verified)
- [ ] 1.3 In the same file's `CreateDefaultManager()` (`:87-108`) delete the stale first sentence
      of the method comment (`:91-92`, "A GitHub repository URL uses … uses Velopack's default
      source."), delete the `if (RepositoryUrl.Contains("github.com", ...))` wrapper (`:94-97,
      98`) and the `return new UpdateManager(RepositoryUrl);` fallback (`:99`), keeping one
      unconditional `return new UpdateManager(new GithubSource(RepositoryUrl, null, prerelease:
      false));` — master's argument list exactly, NOT the backup's `prerelease` parameter — and
      keeping the "Only stable releases are published…" comment line and the whole try/catch
      (`:101-107`) untouched (design.md D1, Context §1)
- [ ] 1.4 Create `src/CLIHub.Core/Updates/UpdateVersionText.cs` (no `// <file …>` header comment —
      design.md D8): `namespace CLIHub.Core.Updates;`, `internal static class UpdateVersionText`
      with full XML docs and one `internal static string StripBuildMetadata(string version)` whose
      body is the exact `Normalize` body (`var plus = version.IndexOf('+'); return plus >= 0 ?
      version[..plus] : version;`) — copy the file from
      `git show backup-before-reset:src/CLIHub.Core/Updates/UpdateVersionText.cs` and delete its
      header block (design.md D2)
- [ ] 1.5 Rewire the five call sites and delete the three private `Normalize` methods with their
      doc blocks: `UpdateDownloader.cs:77` and `:115-119`; `UpdateInstaller.cs:41`, `:77`, and
      `:101-105`; `UpdateService.cs:115`, `:122`, `:253`, and `:311-315` — every `Normalize(x)`
      becomes `UpdateVersionText.StripBuildMetadata(x)`; no using directives change (same
      namespace)
- [ ] 1.6 Create `tests/CLIHub.Core.Tests/Updates/UpdateVersionTextTests.cs` from
      `git show backup-before-reset:tests/CLIHub.Core.Tests/Updates/UpdateVersionTextTests.cs`
      minus the header block: `namespace CLIHub.Core.Tests.Updates;`, `using CLIHub.Core.Updates;`,
      the three facts `StripBuildMetadata_VersionWithCommitSuffix_StripsSuffix`,
      `StripBuildMetadata_PlainVersion_ReturnsUnchanged`,
      `StripBuildMetadata_TrailingPlus_ReturnsCoreVersion`
- [ ] 1.7 Gate: run `dotnet build CLIHub.sln` (0 warnings / 0 errors) and `dotnet test CLIHub.sln`
      (0 failed; passed total = 1.1 baseline + 3; `UpdateServiceTests.cs:97`
      `Assert.DoesNotContain("+", version)` must still pass) — both must pass before stage 2
- [ ] 1.8 Manual smoke (user): launch the app (it must start cleanly), open Settings → Updates and
      run the update check — it completes normally with the usual outcome; every version string the
      stage touched renders exactly as before with no `+build` suffix (Settings Updates "Current
      version" and the launch window update control both show the plain version; on a dev build
      whose informational version carries a `+…` suffix it still displays stripped)

## 2. AtomicFileWriter (proposal item 4)

- [ ] 2.1 Create `src/CLIHub.Core/Infrastructure/FileSystem/AtomicFileWriter.cs` (no header
      comment) from `git show backup-before-reset:src/CLIHub.Core/Infrastructure/FileSystem/AtomicFileWriter.cs`:
      `namespace CLIHub.Core.Infrastructure.FileSystem;`, `internal static class AtomicFileWriter`
      with full XML docs and one `internal static void Write(string targetPath, string contents)`
      that computes `var tempPath = targetPath + ".tmp";`, ensures
      `Path.GetDirectoryName(targetPath)` via `Directory.CreateDirectory` when non-empty, then
      `File.WriteAllText(tempPath, contents);` and `File.Move(tempPath, targetPath, overwrite:
      true);` — no locks, no logging, no policy parameters (design.md D4)
- [ ] 2.2 In `src/CLIHub.Core/Configuration/ConfigurationRepository.cs` `WriteConfig` (`:235-267`):
      delete the tempPath/directory block (`:240-245`) and replace `File.WriteAllText(tempPath,
      pendingWrite.Json); File.Move(tempPath, this.configFilePath, overwrite: true);` (`:257-258`)
      with `AtomicFileWriter.Write(this.configFilePath, pendingWrite.Json);` — the `writeLock`
      (`:247`), the generation discard (`:249-255`), the try/catch, and the debug log stay exactly
      as they are; the `using CLIHub.Core.Infrastructure.FileSystem;` at `:5` already exists
      (behavior-equivalence argument in design.md D4)
- [ ] 2.3 In `src/CLIHub.Core/Infrastructure/Persistence/LogoCacheService.cs` `Save` (`:155-178`):
      replace the directory block, tempPath, write, and move (`:167-175`) with
      `AtomicFileWriter.Write(this.stateFilePath, json);` keeping the preceding comment lines, the
      `this.isDirty = false;` (`:176`) and debug log (`:177`); `using` at `:7` already exists
- [ ] 2.4 In `src/CLIHub.Core/Plugins/PluginStateRepository.cs` `Save` (`:76-98`): replace the
      directory block, tempPath, write, and move (`:82-92`) with
      `AtomicFileWriter.Write(this.stateFilePath, JsonSerializer.Serialize(document,
      CoreJson.Options));` keeping the "Atomic write …" comment and the try/catch; `using` at `:6`
      already exists
- [ ] 2.5 Gate: run `dotnet build CLIHub.sln` (0 warnings / 0 errors) and `dotnet test CLIHub.sln`
      (0 failed; totals = 1.1 baseline + 3; the atomicity pins must pass unchanged:
      `ConfigurationRepositoryTests.cs:291`
      `Update_WhenAtomicReplacementCannotCreateTempFile_PreservesPreviousValidJson`,
      `PluginStateRepositoryTests.cs:50` `Save_WritesAtomicallyViaTempFileThenRename`,
      `PluginStateRepositoryTests.cs:68` `SaveCreatesMissingParentDirectory`, and the logo-cache
      persistence tests) — both must pass before stage 3
- [ ] 2.6 Manual smoke (user): change a setting in Settings and save, then restart the app and
      confirm the change persisted and reloads correctly (config.json — a consumer of the new
      helper per design.md D4); agent logos still resolve after the restart (logos.json cache), and
      a plugin toggle — or a quick plugin install, if convenient — still sticks across the restart
      (plugin-state.json); no `.tmp` leftovers in `%APPDATA%\CLIHub\`

## 3. PreferenceReader generic reader (proposal item 5)

- [ ] 3.1 In `src/CLIHub.Core/Logging/PreferenceReader.cs` port the backup shape from
      `git show backup-before-reset:src/CLIHub.Core/Logging/PreferenceReader.cs`: rewrite
      `ReadLogLevel` (`:15-37`) as `=> ReadString(configFilePath, "logLevel");` keeping its XML
      doc; rewrite `ReadString` (`:67-89`) as `=> ReadPreference<string>(configFilePath,
      propertyName, value => value.GetString());` and `ReadInt` (`:91-114`) as
      `=> ReadPreference<int?>(configFilePath, propertyName, value => value.TryGetInt32(out var
      result) ? result : null);`; add the private generic
      `static T? ReadPreference<T>(string configFilePath, string propertyName,
      Func<JsonElement, T?> read)` owning the file-exists check, the `preferences`/`propertyName`
      walk, the tolerant catch with its existing comment, and the `return default;`/`return
      default;` tail — behavior-identical per design.md Context §5; public members and their doc
      comments unchanged
- [ ] 3.2 Gate: run `dotnet build CLIHub.sln` (0 warnings / 0 errors) and `dotnet test CLIHub.sln`
      (0 failed; totals = 1.1 baseline + 3; logging/startup configuration tests keep passing) —
      both must pass before stage 4
- [ ] 3.3 Manual smoke (user): change the log level on Settings → Logging and the theme or accent
      on Settings → Appearance, save, restart the app — the stored log level still drives the log
      file and the theme/accent still apply on startup (the `PreferenceReader` read paths this
      stage consolidated: `ReadLogLevel`/`ReadString` and `ReadInt` via `ReadPreference<T>`)

## 4. UpdateOutcomeText (proposal item 3)

- [ ] 4.1 Create `src/CLIHub/ViewModels/Shared/UpdateOutcomeText.cs` (no header comment):
      `namespace CLIHub.ViewModels;`, `using CLIHub.Core.Models;`, `internal static class
      UpdateOutcomeText` with full XML docs and one `internal static string
      ForCheck(UpdateCheckResult result)` containing the four-arm switch exactly as written at
      `SettingsViewModel.cs:778-784` — body copied from
      `git show backup-before-reset:src/CLIHub/Helpers/UpdateOutcomeText.cs`, only the namespace
      changes (design.md D3)
- [ ] 4.2 In `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs` replace the switch expression
      assigned to `this.UpdateMessage` in `ApplyUpdateResult` (`:778-784`) with
      `this.UpdateMessage = UpdateOutcomeText.ForCheck(result);` — the card switch below
      (`:786-816`), the `lastChecked` line, and every using stay untouched; no using is added
      (same namespace)
- [ ] 4.3 In `src/CLIHub/ViewModels/Updates/UpdateControlViewModel.cs` replace the switch
      expression inside `this.ReportOutcome(...)` in `CheckForUpdatesAsync` (`:144-150`) with
      `this.ReportOutcome(UpdateOutcomeText.ForCheck(result));` — the catch literal at `:140`, the
      download-outcome switch (`:183-190`), and `SetState` logic stay untouched (design.md D3)
- [ ] 4.4 Create `tests/CLIHub.Tests/ViewModels/Shared/UpdateOutcomeTextTests.cs` (no header
      comment): `namespace CLIHub.Tests.ViewModels.Shared;`, `using CLIHub.Core.Models;`, `using
      CLIHub.ViewModels;`, and the `[Theory]` `ForCheck_KnownStatus_ReturnsSharedOutcomeText` with
      the four `[InlineData]` cases copied from
      `git show backup-before-reset:tests/CLIHub.Tests/Helpers/UpdateOutcomeTextTests.cs`
- [ ] 4.5 Gate: run `dotnet build CLIHub.sln` (0 warnings / 0 errors) and `dotnet test CLIHub.sln`
      (0 failed; totals = 1.1 baseline + 7; `SharedUpdatePresentationTests.cs:67-72` must pass
      unchanged) — both must pass before stage 5
- [ ] 4.6 Manual smoke (user): run an update check from both surfaces — the launch window's update
      control and Settings → Updates — and confirm both report the same, unchanged outcome wording
      ("Up to date (v…)" on the current build, or "Update available: X (current vY)"; a check with
      the network blocked still says "Update check failed or timed out.")

## 5. Shell coordinator factory (proposal item 6)

- [ ] 5.1 Rewrite `src/CLIHub/Lifetime/ApplicationSession.cs` to the backup shape from
      `git show backup-before-reset:src/CLIHub/Lifetime/ApplicationSession.cs` (minus the header
      block): keep `namespace CLIHub.Lifetime;` with `using CLIHub.Startup;` only; replace the six
      dependency fields and constructor (`:14-47`) with one
      `private readonly Func<ApplicationStartupContext, IShellCoordinator> shellFactory;` and a
      single-parameter constructor that null-guards it (updated XML doc: the session takes the
      lazy shell coordinator factory invoked on first start); in `Start` (`:50-75`) replace the
      inline `new ShellCoordinator(...)` (`:65-72`) with
      `this.shellCoordinator = this.shellFactory(context);` — guard order (`ThrowIfNull(context)`,
      `ThrowIfNull(context.Dispatch)`, disposed check, already-started short-circuit) and
      `Dispose` (`:78-88`) stay byte-identical (design.md D6)
- [ ] 5.2 In `src/CLIHub/Composition/ServiceRegistration.cs` `AddStartupServices` (`:71-88`)
      insert before `services.AddSingleton<IApplicationSession, ApplicationSession>();` (`:77`):
      `services.AddSingleton<Func<ApplicationStartupContext, IShellCoordinator>>(sp => context =>
      new ShellCoordinator(sp.GetRequiredService<IApplicationOperationLifetime>(),
      sp.GetRequiredService<Func<IApplicationStartupUi>>(),
      sp.GetRequiredService<Func<IUpdateRequestSource>>(),
      sp.GetRequiredService<ISingleInstanceGuard>(), sp.GetRequiredService<IUpdateWorkflow>(),
      sp.GetRequiredService<SystemThemeFollower>(), context));` — copy the shape from
      `git show backup-before-reset:src/CLIHub/Composition/ServiceRegistration.cs`
      (`AddStartupServices`); all needed namespaces are already imported (verified, design.md
      Context §6); extend the method's summary doc to mention the session's lazy shell coordinator
      factory
- [ ] 5.3 Rework `tests/CLIHub.Tests/Services/ApplicationSessionTests.cs` to the backup's post-port
      shape from `git show backup-before-reset:tests/CLIHub.Tests/Services/ApplicationSessionTests.cs`:
      keep the three existing facts with their exact names and assertions
      (`Start_WiresApplicationEventsToSessionWorkflows`,
      `Dispose_UnsubscribesApplicationEvents`,
      `DownloadOutcomes_ActiveSession_DispatchesTrayNotifications`); hoist the `SystemThemeFollower`
      to a `ThemeFollower` fixture property (master namespace `CLIHub.Theming` at `:8` — the
      backup used `CLIHub.Themes`; keep master's); build the session with a stub factory that
      records each `ApplicationStartupContext` into `ShellFactoryContexts` and returns a real
      `ShellCoordinator(this.OperationLifetime.Object, () => this.StartupUi.Object, () =>
      this.UpdateRequests.Object, this.Guard.Object, this.UpdateDownload.Object,
      this.ThemeFollower, context)`; port the two new facts
      `Start_CalledTwice_InvokesShellFactoryOnce` and
      `Start_PassesSameContextInstanceToShellFactory` (Assert.Same on the context — the record's
      value equality makes `Assert.Same` the correct identity pin)
- [ ] 5.4 Gate: run `dotnet build CLIHub.sln` (0 warnings / 0 errors) and `dotnet test CLIHub.sln`
      (0 failed; totals = 1.1 baseline + 9; `ArchitectureBoundaryTests.cs:83`
      (`IApplicationSession, ApplicationSession` still present in the registration text) and
      `ServiceRegistrationTests.cs:37,147-148` (session resolves, singleton identity, no WPF
      construction at resolution) must pass unchanged) — both must pass before stage 6
- [ ] 5.5 Manual smoke (user): start the app — the launch window shows (or the app starts tray-only
      when start-minimized is enabled); the tray icon works (menu opens, Show reveals the window),
      Settings opens from the window, and Exit from the tray closes the app cleanly — the shell
      construction and session dispose path this stage rewired to the factory

## 6. ADR bookkeeping and final gates

- [ ] 6.1 In `docs/adr/0013-no-generic-persistence-cache.md` append one dated line under
      "## Reopen triggers" (after trigger 3, before nothing else changes):
      `**2026-11-12:** Trigger 1 exercised by the change \`core-cleanup-and-shell-factory\` —
      \`PluginStateRepository\` became the third JSON-persisted atomic-write consumer; extracted
      the minimal two-statement \`AtomicFileWriter\` deduplication. The decision stands: still no
      generic persistence or cache abstraction.` — no other ADR text is edited (design.md D4, Q4)
- [ ] 6.2 Run `dotnet build CLIHub.sln` and confirm 0 warnings / 0 errors; any warning in the
      touched files is a defect of this change
- [ ] 6.3 Run `dotnet test CLIHub.sln` and confirm 0 failed with totals = 1.1 baseline + 9
      (3 `UpdateVersionText` facts + 4 `UpdateOutcomeText` theory cases + 2 `ApplicationSession`
      facts) and no test deleted, skipped, or renamed except the `ApplicationSessionTests` fixture
      rework that preserves all three existing fact names
- [ ] 6.4 Run `openspec validate --all` and confirm 0 failed (expect the pre-change items plus
      this change; pre-existing WARNING items on unrelated specs are not this change's to fix)
- [ ] 6.5 Run `graphify update .` so the knowledge graph reflects the edits and the three new
      source files plus the two new test files
- [ ] 6.6 Scope check: `git status --short` lists only this change's files —
      `openspec/changes/core-cleanup-and-shell-factory/`, the new
      `src/CLIHub.Core/Updates/UpdateVersionText.cs`,
      `src/CLIHub.Core/Infrastructure/FileSystem/AtomicFileWriter.cs`,
      `src/CLIHub/ViewModels/Shared/UpdateOutcomeText.cs`,
      `tests/CLIHub.Core.Tests/Updates/UpdateVersionTextTests.cs`,
      `tests/CLIHub.Tests/ViewModels/Shared/UpdateOutcomeTextTests.cs`, the modified
      `src/CLIHub.Core/Updates/UpdateService.cs`, `UpdateDownloader.cs`, `UpdateInstaller.cs`,
      `src/CLIHub.Core/Configuration/ConfigurationRepository.cs`,
      `src/CLIHub.Core/Infrastructure/Persistence/LogoCacheService.cs`,
      `src/CLIHub.Core/Plugins/PluginStateRepository.cs`,
      `src/CLIHub.Core/Logging/PreferenceReader.cs`,
      `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`,
      `src/CLIHub/ViewModels/Updates/UpdateControlViewModel.cs`,
      `src/CLIHub/Lifetime/ApplicationSession.cs`,
      `src/CLIHub/Composition/ServiceRegistration.cs`,
      `tests/CLIHub.Tests/Services/ApplicationSessionTests.cs`, and
      `docs/adr/0013-no-generic-persistence-cache.md` — no `.csproj`, no XAML, no other test file,
      and `src/CLIHub/ViewModels/Shared/PreferenceSyncHelper.cs` must NOT appear; then report the
      result and ask the user for `git commit` and `openspec archive` approval (approval never
      carries over, and nothing is staged before it)
