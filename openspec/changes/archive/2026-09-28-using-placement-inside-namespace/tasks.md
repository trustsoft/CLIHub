## 1. Enforcement

- [x] 1.1 Add `csharp_using_directive_placement = inside_namespace:warning` to `src/.editorconfig`; verify `dotnet format style CLIHub.sln --verify-no-changes` reports IDE0065 for files that still have top-level usings (note: IDE0065 is not surfaced by the command-line build).

## 2. Reformat

- [x] 2.1 Move `using` directives after the file-scoped `namespace X;` in all `src/**/*.cs` (via the IDE0065 code fix); verify `dotnet format CLIHub.sln --verify-no-changes` reports no using-placement issues.
- [x] 2.2 Confirm `global using` directives (none exist) remain at the top level and no assembly attribute files were moved; verify `git diff` shows only moved usings.

## 3. Verification

- [x] 3.1 Run `dotnet build CLIHub.sln` and confirm 0 warnings / 0 errors with `EnforceCodeStyleInBuild=true`.
- [x] 3.2 Run `dotnet test CLIHub.sln` and confirm all tests pass.

## 4. Documentation

- [x] 4.1 Confirm `AGENTS.md` Code Style documents the rule and update `docs/architecture.md` Conventions if it references using placement.
