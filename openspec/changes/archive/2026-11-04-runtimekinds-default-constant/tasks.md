## 1. Add the `Default` Constant

- [x] 1.1 Add `public const RuntimeKind Default = RuntimeKind.PowerShell;` with XML doc to `RuntimeKinds` after the token constants (after line 53, before `TryParse`) and verify `dotnet build CLIHub.sln` compiles with 0 warnings

## 2. Update Fallback References

- [x] 2.1 Change line 61 in `TryParse` from `kind = RuntimeKind.PowerShell;` to `kind = Default;` and verify `dotnet build CLIHub.sln` compiles with 0 warnings
- [x] 2.2 Change line 105 in `Parse` from `RuntimeKind.PowerShell` to `Default` in the ternary fallback and verify `dotnet build CLIHub.sln` compiles with 0 warnings
- [x] 2.3 Update the `Parse` XML doc (line 102) to reference `<see cref="Default" />` instead of `<see cref="RuntimeKind.PowerShell" />` and verify `dotnet build CLIHub.sln` compiles with 0 warnings

## 3. GitNexus Impact Analysis

- [x] 3.1 Run GitNexus upstream impact analysis on `RuntimeKinds` before committing: `node .gitnexus/run.cjs impact "RuntimeKinds" --direction upstream --repo .`, confirm callers are `AppConfigDocument.ToPreferences` and `LegacyTerminalPreferenceMigration.Migrate`, and verify RISK is not HIGH/CRITICAL (expected: LOW or MEDIUM, as this is a zero-behavior-change refactor)

## 4. Verification

- [x] 4.1 Build the solution: `dotnet build CLIHub.sln` and verify 0 errors, 0 warnings
- [x] 4.2 Run all tests: `dotnet test CLIHub.sln` and verify all tests pass (specifically `Parse_UnknownOrMissingValue_UsesPowerShell` and `ToToken_UsesCanonicalStoredToken` remain green with no changes to test code)
- [x] 4.3 Validate OpenSpec: `openspec validate --all` and verify 0 errors (the change has `skip_specs: true` so no delta specs are expected)
- [x] 4.4 Update graphify: `graphify update .` (AST-only, no API cost) and verify the command completes successfully
- [x] 4.5 Run GitNexus detect-changes before commit: `node .gitnexus/run.cjs detect-changes --scope all --repo .` and verify the report shows the change to `RuntimeKind.cs` only, with zero regressions (`partial: false`, `truncated: false`)

## 5. Final Checks

- [x] 5.1 Review the diff: verify exactly 4 lines changed in `RuntimeKind.cs` (add `Default` constant + XML doc, update line 61, update line 105, update XML doc line 102), and the `case "ps":` arm at line 73 remains `kind = RuntimeKind.PowerShell;` unchanged
- [x] 5.2 Verify no test files were modified (zero changes to `RuntimeKindsTests.cs` or any other test file)
- [x] 5.3 Verify no changes outside `src/CLIHub.Core/Models/Processes/RuntimeKind.cs`
