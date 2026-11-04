## Context

`RuntimeKinds` is a static helper class for the `RuntimeKind` enum (PowerShell, PowerShell7, CommandPrompt, GitBash) that maps runtime values to stored tokens and parses tokens/legacy executable names back to `RuntimeKind`. The class has token constants for each enum value (`PowerShellToken`, `PowerShell7Token`, `CommandPromptToken`, `GitBashToken`), a `TryParse` method (lines 59-99) that returns false for missing/unknown values and initializes `out RuntimeKind kind = RuntimeKind.PowerShell` (line 61), a `Parse` method (lines 104-105) that falls back to `RuntimeKind.PowerShell`, and a `ToToken` method (lines 110-116) that returns the canonical token.

File: `src/CLIHub.Core/Models/Processes/RuntimeKind.cs` (117 lines).

Verified state:
- Line 38-53: Token constants (4 public `const string` fields)
- Line 61: `kind = RuntimeKind.PowerShell;` — the `TryParse` `out`-init fallback
- Line 73: `case "ps":` arm sets `RuntimeKind.PowerShell` (a parsed mapping, not a fallback)
- Line 104-105: `Parse` method `=> TryParse(value, out var kind) ? kind : RuntimeKind.PowerShell;` — the parse-method fallback
- Line 102: `Parse` XML doc says "falling back to <see cref="RuntimeKind.PowerShell" />"
- Line 110-116: `ToToken` switch uses `RuntimeKind.PowerShell7`, `RuntimeKind.CommandPrompt`, `RuntimeKind.GitBash` explicitly and defaults to `PowerShellToken`

Callers (GitNexus-verified):
- `src/CLIHub.Core/Models/Configuration/AppConfigDocument.cs:198` — `RuntimeKinds.Parse(this.DefaultRuntime)` (preference deserialization)
- `src/CLIHub.Core/Configuration/Migrations/LegacyTerminalPreferenceMigration.cs:25` — `RuntimeKinds.TryParse(...)` and line 28 assigns `RuntimeKind.PowerShell` when the old `TerminalExecutable` does not map to `CommandPrompt`

Tests: `tests/CLIHub.Core.Tests/Models/Processes/RuntimeKindsTests.cs` has two tests:
- `Parse_UnknownOrMissingValue_UsesPowerShell` (lines 12-15): asserts `Parse(null/""/"future-runtime"/"wt") == RuntimeKind.PowerShell`
- `ToToken_UsesCanonicalStoredToken` (lines 22-25): asserts each `RuntimeKind` maps to its token constant

Sibling pattern (to mirror): `src/CLIHub.Core/Models/Projects/ProjectDiscoveryMode.cs` has:
- Token constants (lines 32, 37, 42)
- **`public const ProjectDiscoveryMode Default = ProjectDiscoveryMode.Both;`** (line 47) with XML doc "The mode used when none is stored or the stored value is unrecognized."
- `Parse` method (lines 55-61) that returns `Default` for unrecognized values (line 60)
- `ToToken` (lines 68-73) using explicit enum values and a default arm

Also `src/CLIHub.Core/Models/Appearance/PathDisplayStyle.cs`:
- Token constants (lines 27, 32)
- **`public const PathDisplayStyle Default = PathDisplayStyle.LeftTrim;`** (line 37) with XML doc "The style used when none is stored or the stored value is unrecognized."
- `Parse` method (lines 43-53) that returns `Default` for null/empty (line 45) and unrecognized (line 52)
- `ToToken` (lines 58-62) defaulting to `LeftTrimToken`

Both siblings have the `Default` constant declared after token constants and before the parse method; both reference `Default` in their parse methods and XML docs.

## Goals / Non-Goals

**Goals:**
- Add a public `Default` constant to `RuntimeKinds` mirroring the sibling pattern
- Make the fallback value single-source-of-truth by referencing `Default` from `TryParse` and `Parse`
- Update XML documentation to reference the constant
- Zero behavior change: all existing call sites and tests remain unchanged

**Non-Goals:**
- Changing `ToToken` behavior or its default arm (already returns `PowerShellToken`, no need to reference `Default`)
- Modifying tests (the existing fallback behavior tests continue to pass unchanged)
- Changing callers (`AppConfigDocument`, `LegacyTerminalPreferenceMigration`) — they are unaffected
- Creating a spec (this is a pure refactor; `skip_specs: true` is set)

## Decisions

### Decision 1: Add `Default` constant in the established location

**Chosen:** Add `public const RuntimeKind Default = RuntimeKind.PowerShell;` after the token constants (after line 53) and before `TryParse` (line 59), matching the sibling layout.

**Rationale:**
- Consistent with `ProjectDiscoveryModes.Default` (line 47) and `PathDisplayStyles.Default` (line 37)
- Placed with the other constants for discoverability
- Public visibility lets callers reference the fallback if needed (though none currently do)

**Alternatives considered:**
- Private constant: Rejected — siblings are public and the fallback is part of the public contract
- Place after `TryParse`: Rejected — breaks the constants-then-methods ordering

**XML doc:** "The runtime used when none is stored or the stored value is unrecognized." (mirrors the sibling wording).

### Decision 2: Reference `Default` from `TryParse` and `Parse`

**Chosen:**
- Line 61: Change `kind = RuntimeKind.PowerShell;` to `kind = Default;`
- Line 105: Change `RuntimeKind.PowerShell` to `Default` in the `Parse` ternary

**Rationale:**
- Single source of truth for the fallback value
- Self-documenting: the code and XML doc both point to the constant
- Matches the sibling pattern (`ProjectDiscoveryModes.Parse` returns `Default`)

**Alternatives considered:**
- Leave `TryParse` as is: Rejected — duplicates the fallback constant
- Leave `Parse` as is: Rejected — `Parse` should use `Default` for consistency with siblings

### Decision 3: Update `Parse` XML doc to reference `Default`

**Chosen:** Change line 102 from `falling back to <see cref="RuntimeKind.PowerShell" />` to `falling back to <see cref="Default" />`.

**Rationale:** Points readers to the fallback constant; matches the sibling XML doc pattern.

**Alternatives considered:**
- Leave the XML doc unchanged: Rejected — now that `Default` exists, referencing it is clearer

### Decision 4: Do NOT reference `Default` from the `case "ps":` arm (line 73)

**Chosen:** Keep line 73 as `kind = RuntimeKind.PowerShell;` (do not change to `kind = Default;`).

**Rationale:**
- Line 73 is a **parsed mapping** (the "ps" token explicitly means PowerShell), not a fallback
- Changing it to `Default` would conflate "parsed the ps token" with "fell back to the default"
- If `Default` ever changes to a different runtime (extremely unlikely but hypothetically possible for a fork/variant), line 73 should stay `RuntimeKind.PowerShell` because "ps" means PowerShell, not "the default"
- The `ToToken` default arm (line 115) also uses `PowerShellToken` explicitly (not via `Default`), for the same reason: the token constant is the stable mapping, the `Default` constant is a parse-time fallback policy

**Alternatives considered:**
- Change line 73 to `kind = Default;`: Rejected for the reasons above; the orchestrator brief leaned toward "no" and this design confirms it.

**Documented rationale:** The `case "ps":` arm is a token-to-enum mapping, distinct from the fallback policy, so it continues to reference `RuntimeKind.PowerShell` explicitly.

### Decision 5: No `ToToken` changes

**Chosen:** Leave `ToToken` (lines 110-116) unchanged. The default arm (line 115) already returns `PowerShellToken`.

**Rationale:**
- `ToToken` maps enum values to stored tokens; it is not a parse-time fallback
- The default arm handles any unknown `RuntimeKind` value (defensive); it returns `PowerShellToken` (the canonical token), not `Default` (which is a `RuntimeKind`, not a string)
- Referencing `Default` from `ToToken` would be a type mismatch and conceptual confusion

**Alternatives considered:**
- Change line 115 to `_ => ToToken(Default)`: Rejected — equivalent to the current code but needlessly indirect; the token constant is the right primitive here

### Decision 6: No test changes

**Chosen:** Leave `RuntimeKindsTests.cs` unchanged. The existing tests already verify the fallback behavior.

**Rationale:**
- `Parse_UnknownOrMissingValue_UsesPowerShell` asserts that `Parse(null/""/unknown) == RuntimeKind.PowerShell`, which remains true
- `ToToken_UsesCanonicalStoredToken` covers all enum values; no new behavior to test
- The constant itself needs no test (it is a public field with a fixed value; the tests exercise its usage)

**Alternatives considered:**
- Add a test for the `Default` constant: Rejected — `Parse_UnknownOrMissingValue_UsesPowerShell` already exercises the fallback; adding `Assert.Equal(RuntimeKind.PowerShell, RuntimeKinds.Default)` would test a trivial assignment

### Decision 7: No caller changes

**Chosen:** Leave `AppConfigDocument.ToPreferences()` (line 198) and `LegacyTerminalPreferenceMigration.Migrate` (line 28) unchanged.

**Rationale:**
- Both call `RuntimeKinds.Parse` or `TryParse`; the methods' signatures and behavior are unchanged
- Neither caller currently references the fallback constant directly; no need to introduce it
- If a future caller needs the fallback value without parsing, `RuntimeKinds.Default` is now available

**Alternatives considered:**
- Rewrite line 28 of `LegacyTerminalPreferenceMigration` to use `Default`: Rejected — the current code (`RuntimeKind.PowerShell`) is clear and the migration is legacy code that should not be touched without reason

## Risks / Trade-offs

**[Trade-off] `Default` is now public but has no current external consumers** → Benefit: API consistency with siblings; future-proof. Cost: one more public API member. Accepted: the public visibility matches the sibling pattern and costs nothing.

**[Risk] Confusion between the `case "ps":` arm and the `Default` constant** → Mitigation: the design decision records the distinction (parsed mapping vs. fallback policy); the code remains unchanged at line 73. The constant's XML doc clarifies "when none is stored or the stored value is unrecognized."

**[Trade-off] `ToToken` default arm does not reference `Default`** → Benefit: `ToToken` maps enum→token, not a fallback policy; `PowerShellToken` is the right primitive. Cost: two different default primitives (`Default` for parsing, `PowerShellToken` for token mapping). Accepted: they serve different roles.

## Migration Plan

This is a single-file, three-line change with zero behavioral impact and no test changes.

1. Add the `Default` constant (one line after line 53)
2. Update line 61 (`kind = Default;`)
3. Update line 105 (`Default` in the `Parse` ternary)
4. Update line 102 (XML doc `<see cref="Default" />`)
5. Build and verify `dotnet build CLIHub.sln` passes with 0 warnings
6. Verify `dotnet test CLIHub.sln` passes (all tests green, no changes to tests)

Rollback: revert the file; no configuration or test changes to unwind.

## Open Questions

None. The orchestrator provided verified line numbers, the sibling pattern is clear, and the design decision for line 73 is recorded.
