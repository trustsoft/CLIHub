## Why

`docs/backlog.md` records a project-wide idiom — "Enum mappers silently swallow undefined enum values in
`ToToken` (project-wide idiom)" — where all six configuration token mappers coerce an undefined in-memory
enum value to a default token when writing `config.json`. An undefined value such as `(RuntimeKind)42`
becomes `"ps"`, persists, and round-trips as a valid PowerShell token, so the underlying programming error
is invisible.

The read side is deliberately tolerant (unknown string token → default) and stays that way. But the write
side has no such justification: an undefined in-memory enum can only come from a bug in the program, because
the wire format is string tokens parsed tolerantly. Coercing it to a default token silently persists the bug
instead of surfacing it.

## What Changes

Make every `ToToken` mapper fail fast on an undefined value by throwing
`ArgumentOutOfRangeException(nameof(<param>), <param>, "<Enum> token is undefined")`, where `<param>` is
the mapper's own parameter name and `<Enum>` is the enum type name. Keep every `Parse`/`TryParse` unchanged
(tolerant reads preserved).

The six mappers (each currently funnels its default value through a discard arm):

- `RuntimeKinds.ToToken` (`src/CLIHub.Core/Models/Processes/RuntimeKind.cs:100-106`) — `_ => PowerShellToken`;
  add an explicit `RuntimeKind.PowerShell` arm, `_` throws.
- `AppThemes.ToToken` (`src/CLIHub.Core/Models/Appearance/AppTheme.cs:100-106`) — `_ => DarkToken`; add an
  explicit `AppTheme.Dark` arm, `_` throws.
- `PopupScreenPositions.ToToken` (`src/CLIHub.Core/Models/Appearance/PopupScreenPosition.cs:101-107`) —
  `_ => CenterToken`; add an explicit `PopupScreenPosition.Center` arm, `_` throws.
- `PathDisplayStyles.ToToken` (`src/CLIHub.Core/Models/Appearance/PathDisplayStyle.cs:56-60`) —
  `_ => LeftTrimToken`; add an explicit `PathDisplayStyle.LeftTrim` arm, `_` throws.
- `ProjectDiscoveryModes.ToToken` (`src/CLIHub.Core/Models/Projects/ProjectDiscoveryMode.cs:68-73`) —
  `_ => BothToken`; add an explicit `ProjectDiscoveryMode.Both` arm, `_` throws.
- `ProjectSources.ToToken` (`src/CLIHub.Core/Models/Projects/ProjectSource.cs:51-52`) — the binary
  `source == ProjectSource.Discovered ? DiscoveredToken : ManualToken`; replace with a switch expression with
  explicit `Manual` and `Discovered` arms, `_` throws.

One test per mapper (`ToToken_UndefinedValue_Throws`), plus two documentation edits (one sentence in
`docs/architecture-principles.md`; resolve the enum backlog entry).

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a small internal behavior change with no user-visible formatting change on valid data, so
`skip_specs: true` in `.openspec.yaml`.

## Impact

- Code: the six mapper files listed above. Public signatures are unchanged; only the behavior for an
  out-of-range enum value changes (from "coerce to default token" to "throw").
- Tests: four existing test files gain one `ToToken_UndefinedValue_Throws` test each
  (`RuntimeKindsTests.cs`, `AppThemesTests.cs`, `PopupScreenPositionsTests.cs`, `PathDisplayStyleTests.cs`);
  two new test files (`ProjectDiscoveryModesTests.cs`, `ProjectSourcesTests.cs`) under
  `tests/CLIHub.Core.Tests/Models/Projects/`.
- Docs: `docs/architecture-principles.md` (one sentence: parsers are tolerant, token mappers are strict);
  `docs/backlog.md` (mark the enum entry resolved, keeping its evidence).
- Consequence: a programming-error path that produces an undefined enum value now throws at the save boundary
  instead of silently persisting a default token. On valid data the persisted `config.json` bytes are
  unchanged.

## Non-Goals

- Changing the read side (`Parse`/`TryParse`): unknown or missing string tokens continue to fall back to the
  default value.
- Changing the persisted `config.json` format or its atomic write path.
- Adding a shared token-mapper abstraction, interface, or helper.
- Removing the backlog entry's evidence (mark resolved, keep the text).
- Any user-visible formatting or Settings UI change.

## Dependencies

None. The change is self-contained.
