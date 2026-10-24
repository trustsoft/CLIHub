# Design: consolidate-clone-siblings

## Current state (verified in code)

All citations re-verified against the working tree at HEAD `eb7ca39` (clean except the brief's
pre-existing uncommitted `docs/backlog.md` modification; `git status --porcelain` → ` M
docs/backlog.md` only).

### The models and their properties

| Model | Public properties | File |
|---|---|---|
| `Project` | 7: `Id`, `Name`, `Path`, `Source`, `IsFavorite`, `LastUsed`, `LogoPath` | `src/CLIHub.Core/Models/Projects/Project.cs:14-47` |
| `AppPreferences` | 28: `StartWithWindows`, `Hotkey`, `TerminalExecutable`, `DefaultRuntime`, `LogLevel`, `LogRetentionDays`, `LogFileSizeLimitMb`, `ShowOnlyProjectAgents`, `AgentProbeTtlMinutes`, `AgentProbeTimeoutSeconds`, `CheckForUpdatesOnStartup`, `AutoDownloadUpdates`, `NotifyStableUpdates`, `NotifyPrereleaseUpdates`, `ShowWindowOnStartup`, `StartMinimized`, `PinLaunchWindow`, `PopupScreenPosition`, `AppTheme`, `AccentColor`, `PathDisplayStyle`, `LastSeenReleaseNotesVersion`, `WatchedDirectories`, `WatchedDirectorySettings`, `ScanDepth`, `IgnoredPatterns`, `DiscoveryMode`, `DefaultAgentId` | `src/CLIHub.Core/Models/Configuration/AppConfig.cs:32-181` |

### The five manual copies, field by field

| Copy | Coverage today | Verdict | File |
|---|---|---|---|
| `Project.Copy()` | all 7 properties | canonical | `Project.cs:54-63` |
| `ConfigurationSnapshot.CloneProject` (private; only call sites `:36` and `:48`) | all 7 — same set as `Copy()`, different ordering (`Source` last vs third) | **matches; no divergence** | `src/CLIHub.Core/Configuration/ConfigurationSnapshot.cs:59-68` |
| `AppConfigDocument.ProjectDocument.From` / `ToProject` | all 7 both ways; `Source` tokenized via `ProjectSources.ToToken`/`Parse` (`:100`, `:111`) | **matches; no divergence** | `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs:92-112` |
| `ConfigurationSnapshot.ClonePreferences` | all 28; `WatchedDirectories` (`:94`) and `IgnoredPatterns` (`:101`) deep-copied as new lists; `WatchedDirectorySettings` copied into a new dictionary (`:95-99`) — value sharing is safe because `WatchedDirectoryScanSettings` is an immutable record (`src/CLIHub.Core/Models/Projects/WatchedDirectoryScanSettings.cs:8`) | **matches; no divergence** | `ConfigurationSnapshot.cs:70-104` |
| `AppConfigDocument.PreferencesDocument.From` / `ToPreferences` | all 28 both ways (`From` `:146-187`, `To` `:189-219`), with intentional transforms: `ShowWindowOnStartup = !StartMinimized` on write (`:163-164`, already guarded by a comment), `AccentColor` normalized both ways (`:168`, `:210`; `AccentPalette.TryNormalize` uppercases to `#RRGGBB`, `src/CLIHub.Core/Models/Appearance/AccentPalette.cs:45-72`), enum↔token conversion for `DefaultRuntime`/`PopupScreenPosition`/`AppTheme`/`DiscoveryMode` | **matches; no divergence** | `AppConfigDocument.cs:146-219` |

**Divergence answer to the brief's scope decision 1: none.** Every manual copy enumerates every
public property of its source model today. No public field/property is lost by any copy. The change
is therefore a pure internal refactor → `skip_specs: true` in `.openspec.yaml` (decision D1).

### How the boundary is exercised

- `ConfigurationSnapshot.From(AppConfig)` / `ToAppConfig()` clone projects and preferences in both
  directions (`ConfigurationSnapshot.cs:30-51`); `Clone()` composes them (`:57`).
- `ConfigurationRepository.Read()` returns `cachedSnapshot.Clone()` (`ConfigurationRepository.cs:64`);
  `Update(Action<ConfigurationSnapshot>)` clones before invoking the callback and serializes the
  result through `AppConfigDocument.FromSnapshot` (`:77-82`, `:142-143`). These are the clone
  methods' only production consumers.
- `Project.Copy()`'s other consumers are the `ProjectService` read/add methods
  (`src/CLIHub.Core/Projects/ProjectService.cs:36,49,63,84,91,112,119`);
  `PluginCatalog.GetAllPlugins` uses the unrelated `Plugin.Copy`
  (`src/CLIHub.Core/Plugins/PluginCatalog.cs:119`).
- `ProjectDocument`/`PreferencesDocument` are `internal` nested classes of the `internal`
  `AppConfigDocument`; tests reach them through `InternalsVisibleTo`
  (`src/CLIHub.Core/CLIHub.Core.csproj:28-30`), already used by
  `tests/CLIHub.Core.Tests/Configuration/AppConfigSerializationTests.cs`.

### Existing test coverage and gaps

| Concern | Today | File |
|---|---|---|
| Document round-trip, one test | `AppConfigDocument_RoundTripsEveryConfigurationFieldThroughDtoMapping` asserts 6 of 7 project fields (`Source` excluded) and only 13 of 28 preferences | `tests/CLIHub.Core.Tests/Configuration/AppConfigSerializationTests.cs:93-150` |
| Preferences covered only in scattered tests | scanning prefs (`:170-186`), per-root settings (`:189-209`), `defaultAgentId` (`:227-237`), `source` (`:252-264`), `lastSeenReleaseNotesVersion` (`:29-41`, via raw `AppConfig` serialization, not the document) | same file |
| **Never round-tripped through the document boundary** | 9 preferences: `logRetentionDays`, `logFileSizeLimitMb`, `autoDownloadUpdates`, `notifyStableUpdates`, `notifyPrereleaseUpdates`, `showWindowOnStartup`, `popupScreenPosition`, `appTheme`, `accentColor` (grep: they appear in tests only in UI/service-level contexts, e.g. `UpdateWorkflowTests`, `SettingsApplicationServiceTests`) | — |
| Snapshot detachment | `ConfigurationSnapshot_RoundTripsNestedValuesWithoutSharingReferences` mutates only `Project.Name` + `Preferences.Hotkey` | `AppConfigSerializationTests.cs:301-320` |
| Repository-level detachment | `Read_ReturnsDetachedSnapshot_WhenNestedValuesAreMutated` mutates only `Project.Name` + `Preferences.Hotkey` | `tests/CLIHub.Core.Tests/Configuration/ConfigurationRepositoryTests.cs:192-211` |
| Nested-collection detachment | **no test** mutates `WatchedDirectories`, `IgnoredPatterns`, or `WatchedDirectorySettings` on a returned clone | — |
| Future-field guard | **no test** ties "all public properties" to the round-trip assertions | — |

### Governing requirements (quoted)

- `openspec/specs/configuration-snapshot/spec.md:9-16` — *Configuration reads are mutation-isolated*:
  "The configuration boundary SHALL return a snapshot whose projects, preferences, and nested
  mutable values are detached from service-owned state." Scenario **Mutating a returned snapshot
  does not change stored state**.
- `spec.md:37-44` — *Snapshot persistence preserves the existing document*: "…while preserving its
  JSON property names, defaults, and current schema-version metadata." Scenario **Snapshot
  round-trip preserves configuration data**.

No scenario changes: the tests enforce these two existing requirements more exhaustively; the
delegation is value-identical.

## D1 — Pure refactor → `skip_specs: true` (DECIDED)

The brief's scope decision 1 is resolved by the table above: no copy misses a property today, so
there is no behavior change to spec. Following `2026-10-24-hygiene-sweep` (design D7 there) and
`2026-10-20-fix-settings-open-delay`, the change declares `skip_specs: true`. Escape hatch (also in
proposal.md): if implementation discovers an actual lost property, stop and convert to a spec delta
under `configuration-snapshot`, keeping original scenario names for any MODIFIED requirement.

## D2 — Delegation shape: delete `CloneProject`, call `Copy()` at both sites (DECIDED)

`CloneProject` has exactly two call sites (`ConfigurationSnapshot.cs:36,48`). Replace
`config.Projects.Select(CloneProject).ToList()` with
`config.Projects.Select(project => project.Copy()).ToList()` (and the `this.Projects` equivalent in
`ToAppConfig`), then delete the helper (`:59-68`). The named private method adds no information
once it is a pass-through; deleting it removes the sibling the backlog names.

**Alternative — keep `CloneProject` as `=> project.Copy()`:** rejected. It preserves a second name
for the same operation at the same boundary, which is exactly the drift surface the entry describes.

## D3 — Guard comments on the three remaining manual copies (DECIDED)

`ClonePreferences`, `ProjectDocument.From`/`ToProject`, and `PreferencesDocument.From`/
`ToPreferences` cannot delegate: the document mappings are transformers (model ↔ document with
tokenization, derivation, and normalization), and `ClonePreferences` is the only `AppPreferences`
clone site — an `AppPreferences.Copy()` would relocate, not remove, a manual enumeration
(alternative rejected; reconsider if a second clone site appears). Each copy instead gets a short
comment in the established style (English sentence, cf. `AppConfigDocument.cs:163`):

- above `ClonePreferences`: it must copy every public property of `AppPreferences`, including the
  nested collections; `PersistenceCloneRoundTripTests` fails when a field is missed;
- above `ProjectDocument.From` (covering `ToProject`): both directions must map every public
  property of `Project`; same test pointer;
- above `PreferencesDocument.From` (covering `ToPreferences`): both directions must map every
  public property of `AppPreferences`; same test pointer.

## D4 — Round-trip test scope (DECIDED)

New file `tests/CLIHub.Core.Tests/Configuration/PersistenceCloneRoundTripTests.cs`
(namespace `CLIHub.Core.Tests.Configuration`, consistent with the folder), names following
`MethodOrScenario_Condition_ExpectedResult`:

1. `ProjectDocument_RoundTrip_EveryProjectPropertySurvives` — one `Project` with every property
   set to a non-default value (including `Source = ProjectSource.Discovered` and a non-null
   `LogoPath`), `AppConfigDocument.From` → serialize → deserialize → `ToAppConfig()`, assert all
   seven properties.
2. `PreferencesDocument_RoundTrip_EveryPreferencePropertySurvives` — an `AppPreferences` with all
   28 properties set to non-default values, same document round-trip, assert all 28 — with the
   documented transforms as part of the contract (D5).
3. `ConfigurationSnapshot_FromToAppConfig_RoundTripsEveryProjectAndPreferenceProperty` —
   `From(config).ToAppConfig()` preserves every property of both models (guards `ClonePreferences`
   and the delegated project copy in the `From` direction).
4. `ConfigurationSnapshot_Clone_RoundTripsEveryProjectAndPreferenceProperty` —
   `snapshot.Clone()` preserves every property (guards `ToAppConfig` + `From` composition used by
   `ConfigurationRepository.Read`/`Update`, `ConfigurationRepository.cs:64,77`).
5. `ConfigurationSnapshot_Clone_MutatingNestedCollectionsDoesNotChangeSource` — mutate the clone's
   project fields, `WatchedDirectories`, `IgnoredPatterns`, and `WatchedDirectorySettings`; assert
   the source snapshot's values and collection contents are unchanged.
6. `RoundTripCensus_CoveredPropertyLists_MatchAllPublicPropertiesOfProjectAndAppPreferences` —
   reflection census (see D5a).
7. In `ConfigurationRepositoryTests`: `Read_MutatingReturnedSnapshotCollections_DoesNotChangeStoredState`
   — register state through `Update`, `Read()`, mutate the returned snapshot's nested collections,
   assert a second `Read()` returns the stored values (the brief's "registered state" guard,
   extending `Read_ReturnsDetachedSnapshot_WhenNestedValuesAreMutated`,
   `ConfigurationRepositoryTests.cs:192-211`, which stays unchanged).

Suggested names may be adjusted by the implementer as long as the pattern and coverage hold.

### D5 — Round-trip contract nuances the tests must encode (DECIDED)

- `ShowWindowOnStartup` is **derived** on write: `From` stores `!StartMinimized`
  (`AppConfigDocument.cs:163-164`) while `ToPreferences` reads the stored value verbatim (`:206`).
  A document round-trip therefore restores `ShowWindowOnStartup == !StartMinimized`. The test sets
  the source consistently (`StartMinimized = false`, `ShowWindowOnStartup = true` survives as
  `false`… asserted as the derived relation) — or, simplest, set `StartMinimized = false` and
  assert `restored.ShowWindowOnStartup` is `true`. What must *not* be asserted is that an
  arbitrary `ShowWindowOnStartup` value survives verbatim.
- `AccentColor` is normalized both ways (`:168`, `:210`): use an already-normalized value
  (uppercase `#RRGGBB`, e.g. `#FFB900`) for a verbatim assertion.
- Enum-token fields (`DefaultRuntime`, `PopupScreenPosition`, `AppTheme`, `DiscoveryMode`)
  round-trip through `ToToken`/`Parse`; per-root `ScanDepth` is clamped on load
  (`AppConfigDocument.cs:241-243`) — use in-range values in the exhaustive tests; out-of-range
  behavior is already covered by spec-mapped tests.
- `WatchedDirectorySettings` entries for paths not in `WatchedDirectories` are dropped on load
  (`:227-244`); the exhaustive round-trip uses only watched roots (the drop is covered by
  `PerRootScanSettings_RoundTripAndIgnoreUnwatchedPaths`, `AppConfigSerializationTests.cs:189-209`).

### D5a — Reflection census (DECIDED; beyond the brief's minimum)

The brief requires "at minimum every current public property … round-trips". A fixed enumeration
pins today's fields but degrades to the same silent-loss trap for *future* fields: add a property
to `AppPreferences`, forget `PreferencesDocument.From`, and every new test still passes. Test 6
closes that: a hardcoded `CoveredProperties` set (shared constant with tests 1–4) must equal
`typeof(Project).GetProperties()` and `typeof(AppPreferences).GetProperties()` (public instance).
A new property then fails the census until it is added to the covered set — at which point tests
1–4 fail if any mapping misses it.

**Alternatives rejected:**

- *Manual enumeration only (no census):* rejected as the primary shape — it re-creates the trap the
  backlog entry describes; kept as the fallback if the user vetoes the census (open question 1).
- *Fully reflection-driven value comparison (reflect property values before/after):* rejected —
  opaque failure messages, and it fights the intentional transforms (D5), requiring per-property
  exceptions that duplicate the hand-written assertions.

## D6 — "ProjectState shares live items" stays OUT of scope (DECIDED)

The brief's scope decision 2: default OUT — confirmed. Evidence and reasons:

- It is a **different boundary**: `ProjectState.From` shares `Project` references with its source
  (`ProjectState.cs:31`, `:48` use `AddRange`), `ApplyTo` assigns the live list
  (`:58`, `:70`), and `CopyTo` shares items (`:83`). The clone-sibling consolidation is about
  duplicated *enumerations* inside the snapshot/document boundary; this item is about *aliasing
  semantics* between `ProjectStateStore` and the repository
  (`src/CLIHub.Core/Projects/ProjectStateStore.cs:23-35`).
- Severity Low with its own trigger ("anything that reads projects through
  `ConfigurationRepository`", `docs/backlog.md:182`); the current sharing is currently safe in
  practice because `ProjectStateStore.Update` builds its state from the repository's private clone
  (`ProjectStateStore.cs:30-35` — `From(snapshot)` inside `repository.Update`).
- Fixing it changes observable aliasing for every `ApplyTo`/`CopyTo` caller and needs its own
  caller census — folding it in would inflate this Medium refactor's blast radius and muddy its
  `skip_specs` justification.

The entry stays parked, untouched, like the rest of `docs/backlog.md`.

## Deviations from the brief

None material. Two clarifications recorded:

1. The brief's remedy says "`CloneProject` delegates to `Project.Copy()`"; design D2 implements
   delegation by *deleting* the helper and calling `Copy()` directly at the two call sites —
   delegation in effect, minus a now-empty indirection (the keep-a-pass-through alternative is
   recorded and rejected).
2. The brief's minimum test scope ("every current public property … round-trips, and mutating a
   returned clone does not affect registered state") is implemented plus the D5a census, which goes
   one test beyond the minimum to keep the guard future-proof. Flagged as open question 1 for veto.

## Open questions — resolved by the user (2026-10-24)

1. **Reflection census:** kept. Task 2.6 stays in scope (the user accepted the recommendation).
2. **Test file placement:** new `PersistenceCloneRoundTripTests.cs` (the user accepted the
   recommendation).
3. **Backlog resolved-format rewrite:** moved INTO the implementation scope (user decision,
   overriding the brief's "backlog untouched" constraint): the implementer rewrites the
   "Hand-rolled clone siblings" entry (`docs/backlog.md:168-175`) to the resolved format as task
   3.5, following the established pattern (cf. "AddProject returns the live instance (resolved)").
   The two other uncommitted parked entries in `docs/backlog.md` (pipeline-state foreign-repo shas;
   resolver role override) must remain byte-identical — they belong to no change.
