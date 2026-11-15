## Stage 1: Make the six `ToToken` mappers strict

### 1.1 `RuntimeKinds.ToToken`

- [x] 1.1 Edit `src/CLIHub.Core/Models/Processes/RuntimeKind.cs:100-106`. Add an explicit
      `RuntimeKind.PowerShell => PowerShellToken` arm and change `_ => PowerShellToken` to
      `_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "RuntimeKind token is undefined")`.
      Keep `TryParse`/`Parse` unchanged.
- [x] 1.2 Run `dotnet build CLIHub.sln` — confirm 0 warnings, 0 errors.

### 1.2 `AppThemes.ToToken`

- [x] 1.3 Edit `src/CLIHub.Core/Models/Appearance/AppTheme.cs:100-106`. Add an explicit
      `AppTheme.Dark => DarkToken` arm and change `_ => DarkToken` to
      `_ => throw new ArgumentOutOfRangeException(nameof(theme), theme, "AppTheme token is undefined")`.
      Keep `TryParse`/`Parse` unchanged.
- [x] 1.4 Run `dotnet build CLIHub.sln` — confirm 0 warnings, 0 errors.

### 1.3 `PopupScreenPositions.ToToken`

- [x] 1.5 Edit `src/CLIHub.Core/Models/Appearance/PopupScreenPosition.cs:101-107`. Add an explicit
      `PopupScreenPosition.Center => CenterToken` arm and change `_ => CenterToken` to
      `_ => throw new ArgumentOutOfRangeException(nameof(position), position, "PopupScreenPosition token is
      undefined")`. Keep `TryParse`/`Parse` unchanged.
- [x] 1.6 Run `dotnet build CLIHub.sln` — confirm 0 warnings, 0 errors.

### 1.4 `PathDisplayStyles.ToToken`

- [x] 1.7 Edit `src/CLIHub.Core/Models/Appearance/PathDisplayStyle.cs:56-60`. Add an explicit
      `PathDisplayStyle.LeftTrim => LeftTrimToken` arm and change `_ => LeftTrimToken` to
      `_ => throw new ArgumentOutOfRangeException(nameof(style), style, "PathDisplayStyle token is
      undefined")`. Keep `Parse` unchanged.
- [x] 1.8 Run `dotnet build CLIHub.sln` — confirm 0 warnings, 0 errors.

### 1.5 `ProjectDiscoveryModes.ToToken`

- [x] 1.9 Edit `src/CLIHub.Core/Models/Projects/ProjectDiscoveryMode.cs:68-73`. Add an explicit
      `ProjectDiscoveryMode.Both => BothToken` arm and change `_ => BothToken` to
      `_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "ProjectDiscoveryMode token is
      undefined")`. Keep `Parse` unchanged.
- [x] 1.10 Run `dotnet build CLIHub.sln` — confirm 0 warnings, 0 errors.

### 1.6 `ProjectSources.ToToken`

- [x] 1.11 Edit `src/CLIHub.Core/Models/Projects/ProjectSource.cs:51-52`. Replace the binary expression with a
      switch expression: `source switch { ProjectSource.Manual => ManualToken, ProjectSource.Discovered =>
      DiscoveredToken, _ => throw new ArgumentOutOfRangeException(nameof(source), source, "ProjectSource
      token is undefined") }`. Keep `Parse` unchanged.
- [x] 1.12 Run `dotnet build CLIHub.sln` — confirm 0 warnings, 0 errors.

## Stage 2: Add one test per mapper

### 2.1 Existing test files

- [x] 2.1 Add `ToToken_UndefinedValue_Throws` to `tests/CLIHub.Core.Tests/Models/Processes/RuntimeKindsTests.cs`:
      `Assert.Throws<ArgumentOutOfRangeException>(() => RuntimeKinds.ToToken((RuntimeKind)42));`.
- [x] 2.2 Add `ToToken_UndefinedValue_Throws` to `tests/CLIHub.Core.Tests/Models/Appearance/AppThemesTests.cs`:
      `Assert.Throws<ArgumentOutOfRangeException>(() => AppThemes.ToToken((AppTheme)42));`.
- [x] 2.3 Add `ToToken_UndefinedValue_Throws` to
      `tests/CLIHub.Core.Tests/Models/Appearance/PopupScreenPositionsTests.cs`:
      `Assert.Throws<ArgumentOutOfRangeException>(() => PopupScreenPositions.ToToken((PopupScreenPosition)42));`.
- [x] 2.4 Add `ToToken_UndefinedValue_Throws` to
      `tests/CLIHub.Core.Tests/Models/Appearance/PathDisplayStyleTests.cs`:
      `Assert.Throws<ArgumentOutOfRangeException>(() => PathDisplayStyles.ToToken((PathDisplayStyle)42));`.

### 2.2 New test files for the Projects mappers

- [x] 2.5 Create `tests/CLIHub.Core.Tests/Models/Projects/ProjectDiscoveryModesTests.cs` (namespace
      `CLIHub.Core.Tests.Models`) with a single
      `ToToken_UndefinedValue_Throws` fact:
      `Assert.Throws<ArgumentOutOfRangeException>(() => ProjectDiscoveryModes.ToToken((ProjectDiscoveryMode)42));`.
- [x] 2.6 Create `tests/CLIHub.Core.Tests/Models/Projects/ProjectSourcesTests.cs` (namespace
      `CLIHub.Core.Tests.Models`) with a single
      `ToToken_UndefinedValue_Throws` fact:
      `Assert.Throws<ArgumentOutOfRangeException>(() => ProjectSources.ToToken((ProjectSource)42));`.

### 2.3 Test gate

- [x] 2.7 Run `dotnet test CLIHub.sln` — confirm all tests pass (the six new tests included).

## Stage 3: Documentation

- [x] 3.1 Add one sentence to `docs/architecture-principles.md` (Principle 4, "One document per concern, one
      atomic write path"): enum parsers are tolerant (unknown token → default), while token mappers are strict
      (`ToToken` throws `ArgumentOutOfRangeException` on an undefined enum value).
- [x] 3.2 Edit `docs/backlog.md` — resolve the "Enum mappers silently swallow undefined enum values in
      `ToToken` (project-wide idiom)" entry by adding a `**Status: Resolved by strict-enum-token-mappers**`
      line at the top (keep the existing evidence text intact).

## Stage 4: Verification and graph refresh

- [x] 4.1 Run `dotnet build CLIHub.sln` — confirm 0 warnings, 0 errors.
- [x] 4.2 Run `dotnet test CLIHub.sln` — confirm all pass.
- [x] 4.3 Run `openspec validate --all` — confirm 0 errors (the change declares `skip_specs: true`, so no
      `specs/` directory).
- [x] 4.4 Run `node .gitnexus/run.cjs detect-changes --scope all --repo .` — confirm no unexpected blast
      radius; the six mapper edits are the only source changes.
- [x] 4.5 Run `graphify update .` — refresh the knowledge graph.

## Build / Test Gates

Each stage ends with a gate; a failure stops the stage:

1. After each mapper edit (1.1–1.6): `dotnet build CLIHub.sln` (0 warnings, 0 errors).
2. After the tests (2.7): `dotnet test CLIHub.sln` (all green).
3. After the docs (3.x): `openspec validate --all` (0 errors).
4. Final: `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, `openspec validate --all`,
   `node .gitnexus/run.cjs detect-changes --scope all --repo .`, then `graphify update .`.
