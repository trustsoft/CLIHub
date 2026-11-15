## Context

See `proposal.md` — Why and What Changes. Current state verified against the tree (line numbers from the
working tree at planning time).

### Inventory — the six `ToToken` mappers

Each mapper funnels its default enum value through a discard arm (`_`), so an undefined value is silently
coerced to the default token:

1. `src/CLIHub.Core/Models/Processes/RuntimeKind.cs:100-106` — `RuntimeKinds.ToToken(RuntimeKind kind)`:
   explicit arms for `PowerShell7`/`CommandPrompt`/`GitBash`, then `_ => PowerShellToken` (`:105`). An
   undefined `(RuntimeKind)42` returns `"ps"`.
2. `src/CLIHub.Core/Models/Appearance/AppTheme.cs:100-106` — `AppThemes.ToToken(AppTheme theme)`: explicit
   arms for `Light`/`Blue`/`System`, then `_ => DarkToken` (`:105`).
3. `src/CLIHub.Core/Models/Appearance/PopupScreenPosition.cs:101-107` —
   `PopupScreenPositions.ToToken(PopupScreenPosition position)`: explicit arms for
   `TopCenter`/`CursorRelative`/`PrimaryScreen`, then `_ => CenterToken` (`:106`).
4. `src/CLIHub.Core/Models/Appearance/PathDisplayStyle.cs:56-60` — `PathDisplayStyles.ToToken(PathDisplayStyle
   style)`: explicit arm for `MiddleEllipsis`, then `_ => LeftTrimToken` (`:59`).
5. `src/CLIHub.Core/Models/Projects/ProjectDiscoveryMode.cs:68-73` —
   `ProjectDiscoveryModes.ToToken(ProjectDiscoveryMode mode)`: explicit arms for `Git`/`Agents`, then
   `_ => BothToken` (`:72`).
6. `src/CLIHub.Core/Models/Projects/ProjectSource.cs:51-52` — `ProjectSources.ToToken(ProjectSource source)`:
   the binary `source == ProjectSource.Discovered ? DiscoveredToken : ManualToken`. An undefined cast such as
   `(ProjectSource)42` returns `"manual"` (`:52`).

### Read side — deliberately tolerant, unchanged

Every mapper's read side stays tolerant on unknown/missing tokens:

- `RuntimeKinds.TryParse` (`RuntimeKind.cs:63-89`) returns false for null/empty/unknown and leaves `kind` at
  `Default`; `Parse` (`:94-95`) falls back to `Default` (`RuntimeKind.PowerShell`).
- `AppThemes.TryParse` (`AppTheme.cs:60-86`) and `Parse` (`:93`) fall back to `AppTheme.Dark`.
- `PopupScreenPositions.TryParse` (`PopupScreenPosition.cs:60-86`) and `Parse` (`:93-94`) fall back to
  `PopupScreenPosition.Center`.
- `PathDisplayStyles.Parse` (`PathDisplayStyle.cs:43-51`) falls back to `Default` (`LeftTrim`).
- `ProjectDiscoveryModes.Parse` (`ProjectDiscoveryMode.cs:55-61`) falls back to `Default` (`Both`).
- `ProjectSources.Parse` (`ProjectSource.cs:40-44`) falls back to `ProjectSource.Manual`.

This tolerant-read policy is deliberate and is not changed by this work.

### Callers — write-only

All `ToToken` callers write the token into `config.json` (or the in-memory document that becomes it):

- `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs`:
  - `:86` `Source = ProjectSources.ToToken(project.Source)`
  - `:136` `DefaultRuntime = RuntimeKinds.ToToken(preferences.DefaultRuntime)`
  - `:148` `PopupScreenPosition = PopupScreenPositions.ToToken(preferences.PopupScreenPosition)`
  - `:149` `AppTheme = AppThemes.ToToken(preferences.AppTheme)`
  - `:161` `DiscoveryMode = ProjectDiscoveryModes.ToToken(entry.Value.DiscoveryMode)`
  - `:167` `DiscoveryMode = ProjectDiscoveryModes.ToToken(preferences.Scanning.DiscoveryMode)`
- `src/CLIHub/Settings/SettingsApplicationService.cs:566`
  `preferences.PathDisplayStyle = PathDisplayStyles.ToToken(draft.PathDisplayStyle);`

Note: `AppPreferences.PathDisplayStyle` is a `string` (`AppConfig.cs:124`), so the document path copies it
verbatim (`AppConfigDocument.cs:151`) and the only `ToToken` for `PathDisplayStyle` is the Settings save path
at `SettingsApplicationService.cs:566`. The six mapper types map to the six cited write sites.

## Goals / Non-Goals

**Goals:**

- Make every `ToToken` throw `ArgumentOutOfRangeException` on an undefined enum value.
- Keep every `Parse`/`TryParse` unchanged.
- Add one `ToToken_UndefinedValue_Throws` test per mapper.
- Record the tolerant-read / strict-write split in `docs/architecture-principles.md`; resolve the backlog
  entry.
- No user-visible change for valid data.

**Non-Goals:**

- Touching the read side.
- Changing the persisted format, atomic write, or Settings UI.
- A shared mapper abstraction.
- Deleting the backlog entry.

## Decisions

### Decision 1: Strict on write — throw on undefined, keep reads tolerant

**Chosen:** each `ToToken` throws `ArgumentOutOfRangeException(nameof(<param>), <param>, "<Enum> token is
undefined")` for an undefined value, where `<param>` is the method's own parameter name and `<Enum>` is the
enum type name (`RuntimeKind`, `AppTheme`, `PopupScreenPosition`, `PathDisplayStyle`, `ProjectDiscoveryMode`,
`ProjectSource`). All `Parse`/`TryParse` methods stay unchanged.

**Rationale:**

- An undefined in-memory enum can only originate from a programming error: the wire format is string tokens,
  and every read path parses them tolerantly into a defined value (or the default). There is no legitimate
  runtime path that produces `(RuntimeKind)42` in memory. Failing fast surfaces the bug at the write boundary
  (the save) instead of silently persisting a default token that later round-trips as valid.
- This is the backlog entry's second remedy ("make all mappers throw on undefined values to surface bugs
  earlier") and the user-approved decision.
- The exception type (`ArgumentOutOfRangeException`) is the BCL convention for "an enum value fell outside
  its defined members", and the `(paramName, actualValue, message)` overload records both the parameter and
  the offending value for diagnostics.

**Parameter-name note (deviation from the literal template):** the decision template writes `nameof(value)`,
but none of the six mappers names its parameter `value` — they are `kind`, `theme`, `position`, `style`,
`mode`, and `source`. Each throw site uses its own parameter name so `ArgumentOutOfRangeException.ParamName`
matches the method signature.

### Decision 2: Explicit default arm replaces the discard arm

Each mapper currently maps its default value through the `_` arm. Making `_` throw requires promoting the
default value to an explicit arm first. Concretely:

- `RuntimeKinds.ToToken`: add `RuntimeKind.PowerShell => PowerShellToken`.
- `AppThemes.ToToken`: add `AppTheme.Dark => DarkToken`.
- `PopupScreenPositions.ToToken`: add `PopupScreenPosition.Center => CenterToken`.
- `PathDisplayStyles.ToToken`: add `PathDisplayStyle.LeftTrim => LeftTrimToken`.
- `ProjectDiscoveryModes.ToToken`: add `ProjectDiscoveryMode.Both => BothToken`.
- `ProjectSources.ToToken`: replace the binary expression with a switch with `ProjectSource.Manual =>
  ManualToken`, `ProjectSource.Discovered => DiscoveredToken`, `_ => throw`.

**Rationale:** this keeps every defined member mapped by name (compiler does not warn on an exhaustive
switch-expression over an enum because enum values are unchecked), and makes `_` mean exactly "undefined".

### Decision 3: Tests — one `ToToken_UndefinedValue_Throws` per mapper

Each test casts an out-of-range integer to the enum (e.g. `(RuntimeKind)42`) and asserts
`Assert.Throws<ArgumentOutOfRangeException>(() => Mapper.ToToken((RuntimeKind)42))`, named per the repo
convention `MethodOrCondition_ExpectedResult`. Existing round-trip/token tests are left untouched.

Four tests are added to existing files; two new files are created because `ProjectDiscoveryMode` and
`ProjectSource` have no dedicated mapper test file today (their round-trip behavior is covered indirectly by
`AppConfigSerializationTests.cs:159-265`).

### Alternatives considered

**A — Keep the idiom and document it (rejected).**
Leave every `_ => DefaultToken` arm as-is and add a sentence documenting the tolerant-write behavior.
Pros: zero code churn; symmetric with the tolerant read. Cons: continues to silently persist a default token
when a programming error produces an undefined value, exactly the defect the backlog entry flags; makes a bug
invisible until a bogus token is already on disk. Rejected per the user-approved decision to fail fast.

**B — Make every `ToToken` throw (chosen).**
Promote each default value to an explicit arm and make `_` throw `ArgumentOutOfRangeException`. Pros: surfaces
the bug at the write boundary; no persisted bogus token; minimal, mechanical change; keeps the read side
untouched. Cons: a programming-error path now throws on save — acceptable and intended, since that path is
unreachable from valid data.

**C — Partial/hybrid surfacing (rejected).**
Log a warning (or `Debug.Assert`) on an undefined value without throwing, or add a nullable/try variant
alongside `ToToken`. Pros: avoids a throw on a "should never happen" path. Cons: a log-only signal is still
silently persisted and easy to miss; a second mapper surface (`TryToToken`/nullable return) expands the API
for no real consumer; `Debug.Assert` disappears in release builds. Rejected for not actually preventing the
bogus persistence.

## Consequences

- On valid data, behavior is byte-for-byte unchanged: every defined value still maps to the same token, and
  `config.json` writes are identical.
- On an undefined in-memory enum value, the save (`AppConfigDocument.From` /
  `PreferencesDocument.From`, or the Settings save path `SettingsApplicationService.cs:566`) now throws
  `ArgumentOutOfRangeException` instead of persisting a default token. This is the intended fail-fast at the
  write boundary.
- The read side remains tolerant, so a hand-edited `config.json` containing an unknown token still loads and
  falls back to the default without throwing.

## Why skip_specs: true

The change alters only the behavior for an out-of-range enum value (an internal, programming-error path). No
durable capability, wire format, or user-visible formatting on valid data changes, so no spec delta is
warranted. The tolerant-read contract is already described in the configuration docs and is unchanged.

## Open Questions

1. **Should the tests also assert `ParamName` and `ActualValue`?** The brief specifies only
   `Assert.Throws<ArgumentOutOfRangeException>`. Strengthening to capture the exception and assert
   `ex.ParamName == nameof(...)` and `ex.ActualValue` equals the undefined value would pin the diagnostics
   more precisely. Tentative: keep the minimal `Assert.Throws` per the brief; strengthen only if the
   implementer wants a stronger contract.
2. **`ArgumentOutOfRangeException` message wording.** The template is `"<Enum> token is undefined"`. This is
   used verbatim; confirm no localization/English-only concern (the repo keeps UI text English, and these are
   diagnostic messages, not UI text).
3. **New test-file placement for the Projects mappers.** No `tests/CLIHub.Core.Tests/Models/Projects/` folder
   exists. Tentative: create it for `ProjectDiscoveryModesTests.cs` and `ProjectSourcesTests.cs`, mirroring
   the existing `Appearance`/`Processes` folder layout. Confirm this is acceptable before creating the folder.
