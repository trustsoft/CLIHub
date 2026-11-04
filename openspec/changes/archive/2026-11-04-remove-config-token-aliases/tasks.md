## 1. Remove aliases from PopupScreenPositions.TryParse

- [x] 1.1 In `src/CLIHub.Core/Models/Appearance/PopupScreenPosition.cs`, remove all alias cases from the switch in `PopupScreenPositions.TryParse` (lines 69-92), keeping only the four canonical tokens: `"center"`, `"topCenter"`, `"cursorRelative"`, `"primary"`
- [x] 1.2 Verify `dotnet build CLIHub.sln` compiles with 0 errors

## 2. Remove aliases from PathDisplayStyles.Parse

- [x] 2.1 In `src/CLIHub.Core/Models/Appearance/PathDisplayStyle.cs`, remove all alias conditions from the pattern match in `PathDisplayStyles.Parse` (lines 43-53), keeping only the two canonical tokens: `leftTrim` (with `OrdinalIgnoreCase`) and `middleEllipsis` (with `OrdinalIgnoreCase`)
- [x] 2.2 Verify `dotnet build CLIHub.sln` compiles with 0 errors

## 3. Update PathDisplayStyleTests

- [x] 3.1 In `tests/CLIHub.Core.Tests/Models/Appearance/PathDisplayStyleTests.cs`, replace the two alias-acceptance theories (`Parse_LeftTrimTokens_ReturnLeftTrim` at lines 16-24 and `Parse_MiddleEllipsisTokens_ReturnMiddleEllipsis` at lines 26-34) with:
  - One theory `Parse_CanonicalTokens_ParseCorrectly` verifying canonical `leftTrim` and `middleEllipsis` (and case variations like `LEFTTRIM`, `MiddleEllipsis`, since `Parse` is `OrdinalIgnoreCase`)
  - One theory `Parse_OldAliases_FallBackToLeftTrim` verifying that old aliases (`left-trim`, `left`, `middle-ellipsis`, `middle`) now fall back to `LeftTrim` (the default)
- [x] 3.2 Verify `dotnet build CLIHub.sln` and `dotnet test tests/CLIHub.Core.Tests` both succeed

## 4. Add PopupScreenPositionsTests coverage

- [x] 4.1 In `tests/CLIHub.Core.Tests/Models/Appearance/PopupScreenPositionsTests.cs`, add two new theories after the existing `Parse_UnknownOrMissingValue_UsesCenter` (line 11):
  - One theory `TryParse_CanonicalTokens_ParseCorrectly` verifying that the four canonical tokens (`center`, `topCenter`, `cursorRelative`, `primary`) parse to their respective positions and return true (case-sensitive exact match)
  - One theory `Parse_OldAliases_FallBackToCenter` verifying that old aliases (`centre`, `center-of-active-monitor`, `top-center`, `top`, `cursor-relative`, `cursor`, `primary-screen`) now fall back to `Center` (return false from `TryParse`, default to `Center` from `Parse`)
- [x] 4.2 Verify `dotnet build CLIHub.sln` and `dotnet test tests/CLIHub.Core.Tests` both succeed

## 5. Verification

- [x] 5.1 Run `dotnet build CLIHub.sln` and verify 0 warnings/errors
- [x] 5.2 Run `dotnet test CLIHub.sln` and verify all tests pass
- [x] 5.3 Run `openspec validate --all` from the repository root and verify 0 errors
- [x] 5.4 Run `graphify update .` to refresh the knowledge graph after source changes
