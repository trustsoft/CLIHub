## Context

See `proposal.md` - Why. Constraints that shape the approach:

- File-scoped namespaces are used everywhere, and `EnforceCodeStyleInBuild=true` turns `.editorconfig` severities into build warnings.
- Moving a `using` into the namespace is legal in C# with file-scoped declarations (the directive becomes namespace-scoped), and IDE0065 (`csharp_using_directive_placement`) governs exactly this.
- `global using` directives must remain at the top level (before the namespace declaration).

## Goals / Non-Goals

**Goals:**

- Place regular `using` directives after the file-scoped `namespace X;` in every `.cs` file.
- Enforce the rule through `.editorconfig` (`inside_namespace:warning`), not manual review.
- Keep build output and runtime behavior identical.

**Non-Goals:**

- Reordering usings within the group (System-first, alphabetical) - separate concern.
- Converting file-scoped namespaces to block-scoped or vice versa.
- Changing `global using` handling beyond keeping them top-level.

## Decisions

- **Enforce via `csharp_using_directive_placement = inside_namespace:warning`** (IDE0065), consistent with the existing `EnforceCodeStyleInBuild` setup. Alternative considered: document-only rule (already in `AGENTS.md`) with no enforcement - rejected because the codebase would drift.
- **Mechanical fix tooling**: use the IDE0065 code fix (for example `dotnet format style`) to move directives, then review the diff. Alternative considered: hand-editing ~55 files - rejected as error-prone.
- **Keep `global using` top-level**: none exist in the codebase today, so no special handling is expected; the rule only targets regular usings.
- **Assembly attribute files** (for example `src/CLIHub/AssemblyInfo.cs`) have no namespace or usings and are unaffected.

## Risks / Trade-offs

- [IDE0065 is not surfaced by the command-line build] -> the rule is enforced in IDEs and by `dotnet format style`; verify with `dotnet format style CLIHub.sln --verify-no-changes`.
- [Name-resolution scope change] -> placing usings inside the namespace can surface rare ambiguity; the compiler and build verify correctness, and tests guard behavior.
- [`dotnet format style` applies unrelated IDE fixes] -> review `git diff` before committing, or scope the run narrowly; revert any out-of-scope reformatting.
- [Large diff across every file] -> single dedicated commit with a mechanical description; review is shape-only (usings moved).

## Migration Plan

Set the `.editorconfig` rule, run the IDE0065 fix, then build and test, and commit as one focused change. Rollback is a revert.
