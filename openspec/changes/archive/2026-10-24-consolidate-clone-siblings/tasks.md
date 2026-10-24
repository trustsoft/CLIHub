# Tasks: consolidate-clone-siblings

Stage 1 is the source edit (delegation + guard comments), stage 2 the round-trip tests, stage 3 the
final gates. Each stage ends with a gate; do not start the next stage on a red gate. Per
`AGENTS.md`, run GitNexus impact (`node .gitnexus/run.cjs impact "<symbol>" --direction upstream
--repo .`) before the edits in stage 1 and `detect-changes` before any commit — the caller census in
`design.md` ("Current state") is the baseline any impact result must agree with; if impact reports
a caller not listed there, stop and reconcile before editing. No commits, pushes, or archives are
part of these tasks; they follow the repo approval policy after the user reviews the result.

## 1. Stage 1 — source: delegate `CloneProject`, guard the remaining manual copies

- [x] 1.1 Run impact for `ConfigurationSnapshot` and `Project.Copy` and compare with the census in
      `design.md` (CloneProject: private, call sites `ConfigurationSnapshot.cs:36,48` only;
      `Copy`: `ProjectService.cs:36,49,63,84,91,112,119`; repository consumers `Read`/`Update` via
      `Clone()`). Stop and reconcile on any surprise.
- [x] 1.2 In `src/CLIHub.Core/Configuration/ConfigurationSnapshot.cs`: replace
      `Projects = config.Projects.Select(CloneProject).ToList()` (`:36`) with
      `Projects = config.Projects.Select(project => project.Copy()).ToList()`, and
      `Projects = this.Projects.Select(CloneProject).ToList()` (`:48`) with
      `Projects = this.Projects.Select(project => project.Copy()).ToList()`; delete the private
      `CloneProject` helper (`:59-68`). Change nothing else in `From`/`ToAppConfig`/`Clone`.
- [x] 1.3 Add the guard comment above `ClonePreferences` (`ConfigurationSnapshot.cs:70`), in the
      established comment style (cf. `AppConfigDocument.cs:163`): it must copy every public
      property of `AppPreferences`, including the nested collections, and
      `PersistenceCloneRoundTripTests` fails when a field is missed.
- [x] 1.4 In `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs`: add the guard comments
      above `ProjectDocument.From` (`:92`) — both `From` and `ToProject` must map every public
      property of `Project` — and above `PreferencesDocument.From` (`:146`) — both `From` and
      `ToPreferences` must map every public property of `AppPreferences` — each pointing at
      `PersistenceCloneRoundTripTests`. Keep the existing `ShowWindowOnStartup` comment
      (`:163-164`) and all mapping code byte-identical.
- [x] 1.5 Gate: `dotnet build CLIHub.sln` then `dotnet test CLIHub.sln` — zero failures. The build
      is the proof that no residual `CloneProject` reference exists; no existing test may be edited
      or skipped (design "Current state" verifies none asserts the old shape).

## 2. Stage 2 — round-trip tests for the persistence boundary

New file `tests/CLIHub.Core.Tests/Configuration/PersistenceCloneRoundTripTests.cs`
(namespace `CLIHub.Core.Tests.Configuration`; plain `[Fact]`s, no fixture needed — the document and
snapshot mappings are pure). Names follow `MethodOrScenario_Condition_ExpectedResult`; the
suggested names below may be adjusted as long as the pattern and coverage hold. A shared private
constant lists the covered property names per model for the census test (2.6). Full shape, field
lists, and transform nuances: design D4/D5.

- [x] 2.1 `ProjectDocument_RoundTrip_EveryProjectPropertySurvives`: one `Project` with all seven
      properties at non-default values (incl. `Source = ProjectSource.Discovered`, non-null
      `LogoPath`, `IsFavorite = true`, non-default `LastUsed`), through
      `AppConfigDocument.From(config)` → `JsonSerializer.Serialize/Deserialize` with
      `CoreJson.Options` → `ToAppConfig()`, asserting every property (mirror the plumbing of
      `AppConfigSerializationTests.cs:126-128`).
- [x] 2.2 `PreferencesDocument_RoundTrip_EveryPreferencePropertySurvives`: an `AppPreferences`
      with all 28 properties at non-default values, same document round-trip, asserting all 28 per
      the D5 contract: `ShowWindowOnStartup` asserted as `!StartMinimized` (derived on write,
      `AppConfigDocument.cs:163-164`); `AccentColor` already-normalized (uppercase `#RRGGBB`);
      enum tokens (`DefaultRuntime`, `PopupScreenPosition`, `AppTheme`, `DiscoveryMode`) through
      their `ToToken`/`Parse` values; in-range `ScanDepth` and per-root depths; per-root settings
      only for roots present in `WatchedDirectories`.
- [x] 2.3 `ConfigurationSnapshot_FromToAppConfig_RoundTripsEveryProjectAndPreferenceProperty`:
      `ConfigurationSnapshot.From(config).ToAppConfig()` preserves every property of both models
      (guards `ClonePreferences` and the delegated `Copy()` in the `From` direction).
- [x] 2.4 `ConfigurationSnapshot_Clone_RoundTripsEveryProjectAndPreferenceProperty`:
      `snapshot.Clone()` preserves every property of both models (the composition
      `ConfigurationRepository.Read`/`Update` use, `ConfigurationRepository.cs:64,77`).
- [x] 2.5 `ConfigurationSnapshot_Clone_MutatingNestedCollectionsDoesNotChangeSource`: after
      `From`/`Clone`, mutate the clone's project fields, `WatchedDirectories` (`Add`),
      `IgnoredPatterns` (`Add`), and `WatchedDirectorySettings` (add/overwrite an entry); assert
      the source's values and collection contents are unchanged.
- [x] 2.6 `RoundTripCensus_CoveredPropertyLists_MatchAllPublicPropertiesOfProjectAndAppPreferences`:
      reflection census — assert the covered-property constant equals
      `typeof(Project).GetProperties()` names and `typeof(AppPreferences).GetProperties()` names
      (public instance), so a future property fails the suite until it is added to tests 2.1–2.4.
      If the user vetoes the census (design open question 1), skip only this task and record the
      veto in the change notes.
- [x] 2.7 In `tests/CLIHub.Core.Tests/Configuration/ConfigurationRepositoryTests.cs`, append
      `Read_MutatingReturnedSnapshotCollections_DoesNotChangeStoredState`: register a project plus
      scanning preferences via `Update` + `Flush`, `Read()`, mutate the returned snapshot's nested
      collections and a project field, assert a second `Read()` returns the stored values (extend,
      do not modify, `Read_ReturnsDetachedSnapshot_WhenNestedValuesAreMutated` at `:192-211`).
- [x] 2.8 Gate: `dotnet build CLIHub.sln` then `dotnet test CLIHub.sln` — zero failures, with no
      existing test edited or skipped. If any new round-trip assertion fails, treat it as the D1
      escape hatch firing (a real divergence): stop, record the field, and convert the change into
      a spec delta under `configuration-snapshot` instead of adjusting the test to pass.

## 3. Stage 3 — final verification

- [x] 3.1 Gate: `dotnet build CLIHub.sln` then `dotnet test CLIHub.sln` — zero failures (full
      re-run, not cached results).
- [x] 3.2 Gate: `openspec validate --all` passes.
- [x] 3.3 `graphify update .` to refresh the knowledge graph after the source edits.
- [x] 3.4 Review `git status --porcelain` and `git diff --stat`: only files belonging to this
      change (the two source files, the new test file, `ConfigurationRepositoryTests.cs`,
      `docs/backlog.md`, and this change's artifacts under
      `openspec/changes/consolidate-clone-siblings/`) are modified. Run
      `node .gitnexus/run.cjs detect-changes --scope all --repo .` and attach the result to the
      report. Report, then wait for the user's commit/archive approval per the repo policy.
- [x] 3.5 Rewrite the `docs/backlog.md` "Hand-rolled clone siblings" entry (`:168-175`) to the
      resolved format, following the established pattern (cf. "AddProject returns the live
      instance (resolved)" in the same file): status resolved by `consolidate-clone-siblings`
      (2026-10-24), the resolved model (the `CloneProject` helper deleted, `project.Copy()` at
      both former call sites, guard comments on the remaining manual copies, the round-trip and
      census tests in `PersistenceCloneRoundTripTests.cs` plus the repository-level test), the
      guarded-by test names, and the revisit trigger. Leave the two other uncommitted parked
      entries in the file (pipeline-state foreign-repo shas; resolver role override) byte-identical.
