## Why

`AGENTS.md` now documents that "the namespace declaration must be followed by using directives" (usings inside the file-scoped namespace). Every `.cs` file currently places `using` directives before `namespace X;`, so the codebase contradicts its own documented convention, and no tooling enforces the rule.

## What Changes

- Move `using` directives after the file-scoped `namespace X;` in every `.cs` file under `src/`.
- Add `csharp_using_directive_placement = inside_namespace:warning` to `src/.editorconfig` so IDEs and `dotnet format` flag regressions (IDE0065 is not surfaced by command-line build analyzers).
- Keep `global using` directives at the top level (C# requirement) and assembly-level attributes outside namespaces.
- No behavior change; formatting/organization only.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is a pure style change with no spec-level behavior change, so the change opts out of specs (`skip_specs: true` in `.openspec.yaml`).

## Impact

- Code: all `.cs` files in `src/CLIHub.Core`, `src/CLIHub`, `src/CLIHub.Tests`.
- Config: `src/.editorconfig`.
- Docs: `AGENTS.md` already documents the rule.
- Build: no new dependencies. Risk: `using` directives inside a namespace change name-resolution scope; verify with build and tests.
