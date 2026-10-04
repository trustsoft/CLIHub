# Align Code Style Rules

## Why

Code style rules are split between prose in `AGENTS.md` and a machine-generated `src/.editorconfig` that has never been reconciled with the codebase. The editorconfig only covers `src/` (28 test files and all non-code files are uncovered), carries template rules for technologies the project does not use (Unity, ASP.NET MVC/Razor), and the AGENTS.md prose has drifted from the enforced naming conventions. XML documentation conventions exist only as prose and are not validated at all.

## What Changes

- Move `src/.editorconfig` to the repository root, add `root = true`, and delete the `src/` copy, so style rules cover `src/`, `tests/`, and all non-code files.
- Prune rules for absent technologies: Unity serialized-field naming rules and symbols, ReSharper MVC/Razor/web.config inspection severities, and the redundant file-type indent section (its settings duplicate the `[*]` section).
- Add an interface naming style with `required_prefix = I` so un-prefixed interface names (e.g. `Foo` instead of `IFoo`) are flagged.
- Update `AGENTS.md` naming wording: `_camelCase` applies to private instance and static fields; private `const` and `static readonly` fields use PascalCase. Add a clarification that "usings after `namespace X;`" is the same convention as `csharp_using_directive_placement = inside_namespace`, to prevent a future agent from "fixing" the editorconfig in the opposite direction.
- Enable `GenerateDocumentationFile` in `CLIHub.Core` and `CLIHub` so the compiler validates XML doc comments (malformed XML, `<param>` tags matching actual parameters). `CS1591` (missing doc comment) is intentionally not enabled.
- Not adopted: enforcement of the async-void ban (no built-in analyzer; pulling vs-threading for one rule was judged not worth it).

## Capabilities

### New Capabilities

- `code-style`: Repository-wide code style rules — a single root `.editorconfig` covering all projects, aligned naming conventions, pruned irrelevant rules, and compiler-validated XML documentation comments.

### Modified Capabilities

(none)

## Impact

- Files: root `.editorconfig` (new), `src/.editorconfig` (deleted), `AGENTS.md`, `src/CLIHub.Core/CLIHub.Core.csproj`, `src/CLIHub/CLIHub.csproj`.
- Builds may surface new `CS1570/CS1572/CS1573` warnings where existing doc comments have drifted from signatures; no runtime behavior changes, no new package dependencies.
- The existing severity of these doc warnings needs a quick verification spike during implementation (which of the family fires by default once `GenerateDocumentationFile` is on).
