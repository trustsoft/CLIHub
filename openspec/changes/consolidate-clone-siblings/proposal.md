# Consolidate hand-rolled clone siblings at the configuration persistence boundary

## Why

`docs/backlog.md` parks "Hand-rolled clone siblings" (Medium — "silent field loss on clone",
`docs/backlog.md:168-175`). The persistence boundary around `config.json` hand-copies the same two
models in four places:

- `ConfigurationSnapshot.CloneProject` (`src/CLIHub.Core/Configuration/ConfigurationSnapshot.cs:59-68`)
  duplicates `Project.Copy()` (`src/CLIHub.Core/Models/Projects/Project.cs:54-63`) property for
  property;
- `AppConfigDocument.ProjectDocument.From`/`ToProject`
  (`src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs:92-112`) map the same seven `Project`
  properties again (tokenizing `Source`);
- `ConfigurationSnapshot.ClonePreferences` (`ConfigurationSnapshot.cs:70-104`) hand-enumerates all
  28 `AppPreferences` properties (`src/CLIHub.Core/Models/Configuration/AppConfig.cs:32-181`);
- `AppConfigDocument.PreferencesDocument.From`/`ToPreferences`
  (`AppConfigDocument.cs:146-219`) enumerate the same 28 properties again (with token/derivation
  transforms).

Every future `Project` or `AppPreferences` field must be remembered in up to five enumerations plus
the test suite; missing one loses the field silently. The backlog trigger ("next
persistence-boundary change or when adding a `Project`/preferences field") is fired by this change
itself, and the user explicitly selected the item.

**Verified current state:** all four copies currently cover **every** public property — no field is
lost today (design.md "Current state", field-by-field table). This is therefore a pure internal
refactor plus test hardening, not a behavior fix.

## What Changes

1. **Delegate `CloneProject` to `Project.Copy()`** — delete the private
   `ConfigurationSnapshot.CloneProject` helper and use `project.Copy()` at its only two call sites
   (`ConfigurationSnapshot.cs:36,48`). One canonical `Project` copy remains.
2. **Guard comments on the remaining manual copies** — `ClonePreferences`
   (`ConfigurationSnapshot.cs:70`), `ProjectDocument.From`/`ToProject`
   (`AppConfigDocument.cs:92-112`), and `PreferencesDocument.From`/`ToPreferences`
   (`AppConfigDocument.cs:146-219`): each gets a short comment stating it must enumerate every
   public property of its source model and pointing to the round-trip tests that fail when a field
   is missed.
3. **Round-trip tests for the persistence boundary** (new
   `tests/CLIHub.Core.Tests/Configuration/PersistenceCloneRoundTripTests.cs` plus one
   repository-level test): every current public property of `Project` (7) and `AppPreferences` (28)
   round-trips through `ProjectDocument.From`/`ToProject`, `PreferencesDocument.From`/
   `ToPreferences`, and `ConfigurationSnapshot.From`/`ToAppConfig`/`Clone`; mutating a returned
   clone — including the nested `WatchedDirectories`/`IgnoredPatterns` lists and the
   `WatchedDirectorySettings` dictionary — does not change the source or registered repository
   state; and a reflection census test ties the asserted property lists to *all* public properties
   of the two models, so a future field fails the suite until it is added to the round-trip tests.
   Test names follow `MethodOrScenario_Condition_ExpectedResult`.

The existing partial round-trip test (`AppConfigSerializationTests.cs:93-150`) covers only 13 of 28
preferences and 6 of 7 project fields in one place; nine preferences (`logRetentionDays`,
`logFileSizeLimitMb`, `autoDownloadUpdates`, `notifyStableUpdates`, `notifyPrereleaseUpdates`,
`showWindowOnStartup`, `popupScreenPosition`, `appTheme`, `accentColor`) have no document-boundary
round-trip anywhere (verified by grep; design.md "Current state"). The new tests close that gap and
consolidate the assertion.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. All edits preserve observable behavior exactly:

- `CloneProject` and `Project.Copy()` produce value-identical copies today (both enumerate all
  seven properties), so delegating changes no output.
- Guard comments change no code.
- The tests strengthen enforcement of existing `configuration-snapshot` scenarios — "Mutating a
  returned snapshot does not change stored state" (`openspec/specs/configuration-snapshot/spec.md:9-16`)
  and "Snapshot round-trip preserves configuration data" (`spec.md:41-44`) — without altering any
  scenario.

Therefore the change declares `skip_specs: true` in `.openspec.yaml`, following
`openspec/changes/archive/2026-10-24-hygiene-sweep/` and
`archive/2026-10-20-fix-settings-open-delay/`. Escape hatch: if the implementation session finds a
property that is actually lost today (contradicting design.md's verified table), stop and convert
the change into a spec delta under `configuration-snapshot` instead (keeping original scenario
names for MODIFIED requirements).

## Impact

- Code:
  - `src/CLIHub.Core/Configuration/ConfigurationSnapshot.cs` — delete `CloneProject` (`:59-68`),
    replace `Select(CloneProject)` with `Select(project => project.Copy())` at `:36` and `:48`,
    add one guard comment above `ClonePreferences` (`:70`).
  - `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs` — guard comments above
    `ProjectDocument.From` (`:92`) and `PreferencesDocument.From` (`:146`), covering their `To*`
    counterparts; the existing `ShowWindowOnStartup` comment (`:163-164`) stays.
- Tests: new `tests/CLIHub.Core.Tests/Configuration/PersistenceCloneRoundTripTests.cs`
  (exhaustive round-trips, nested-collection detachment, reflection census); one new test in
  `tests/CLIHub.Core.Tests/Configuration/ConfigurationRepositoryTests.cs`
  (repository-level nested-collection mutation). No existing test is edited or weakened.
- Docs: the `docs/backlog.md` "Hand-rolled clone siblings" entry is rewritten to the resolved
  format (task 3.5, user decision 2026-10-24); nothing else in docs changes.
- Specs: none (`skip_specs: true`).
- Risk: low. The delegation is value-identical (verified property-by-property); the only files
  touched are the two boundary files and tests. The full-solution build is the proof that no
  residual `CloneProject` reference exists.

## Non-goals

- **"ProjectState shares live items at the persistence boundary" stays parked**
  (`docs/backlog.md:177-182`). It is a different boundary (`ProjectState.From`/`ApplyTo`/`CopyTo`
  share `Project` references, `src/CLIHub.Core/Models/Projects/ProjectState.cs:31,48,58,70,83`),
  severity Low, with its own revisit trigger ("anything that reads projects through
  `ConfigurationRepository`"). Fixing it changes aliasing semantics for every `ApplyTo`/`CopyTo`
  caller and needs its own analysis; folding it into this Medium refactor would inflate the blast
  radius. The implementer must not fix it opportunistically (design D6).
- No `AppPreferences.Copy()` method — `ClonePreferences` is the only `AppPreferences` clone site
  (`Project.Copy()` exists because `ProjectService` hands out copies at many sites,
  `src/CLIHub.Core/Projects/ProjectService.cs:36,49,63,84,91,112,119`); relocating the enumeration
  would still be a manual enumeration. Reconsider only if a second clone site appears (design D3).
- No change to the JSON document shape, property names, defaults, or `schemaVersion` — the
  `configuration-snapshot` persistence contract (`spec.md:37-44`) is untouched.
- `docs/backlog.md`: the "Hand-rolled clone siblings" entry (`:168-175`) is rewritten to the
  resolved format by task 3.5 (user decision 2026-10-24, mirroring the hygiene-sweep precedent);
  the two other uncommitted parked entries in the file stay byte-identical.
- No CHANGELOG entry now; entries land under the next release heading.
- No commit, push, or archive in this change's own tasks; those follow the repo approval policy.

## Dependencies

- None on other active changes — `openspec/changes/` holds no other active change (only `archive/`).
- Implements the remedy recorded in `docs/backlog.md:168-175` ("Hand-rolled clone siblings",
  Medium).
- Consistent with the `configuration-snapshot` capability's mutation-isolation and round-trip
  requirements (`openspec/specs/configuration-snapshot/spec.md:9-16,37-44`); the sibling
  `2026-10-24-hygiene-sweep` change already archived the adjacent "AddProject returns the live
  instance" item.
