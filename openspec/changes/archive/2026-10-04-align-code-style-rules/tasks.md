# Tasks: Align Code Style Rules

## 1. Root style configuration

- [x] 1.1 Create a root `.editorconfig` with the current `src/.editorconfig` content plus `root = true` at the top, and delete `src/.editorconfig`. Verify: a repository search finds exactly one `.editorconfig` (at the root) and `dotnet build CLIHub.sln` succeeds.
- [x] 1.2 Prune inapplicable rules: Unity serialized-field naming rule, symbols, and the `lower_camel_case_style_1` style; ReSharper MVC/Razor/web.config inspection severities; the redundant `[*.{...}]` file-type indent section (duplicates the `[*]` section). Verify: searching the file for `unity_serialized_field`, `resharper_mvc`, `resharper_razor`, and `resharper_web_config` finds nothing, and `dotnet build CLIHub.sln` succeeds.
- [x] 1.3 Add interface naming enforcement: dedicated naming symbols and style with `required_prefix = I` (a new style, not the shared `upper_camel_case_style` used by constant and static-readonly rules). Verify: existing interfaces (`IPreferenceApplier`, `IReleaseNotesLauncher`, `ISettingsLauncher`) produce no warnings and `dotnet build CLIHub.sln` succeeds.
  - Implemented with a user-approved extension: `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` in all three projects plus `dotnet_diagnostic` severity pins (IDE0011, IDE0065, IDE0161, IDE1006) in `.editorconfig`, because the .NET 8 SDK ignores the `option:severity` format at build time. Probe verified: wrong interface name and field prefix produce build warnings.

## 2. AGENTS.md alignment

- [x] 2.1 Update the naming bullet: `_camelCase` for private instance and static fields; PascalCase for private `const` and `static readonly` fields. Verify: the wording matches the `.editorconfig` naming rules with no remaining contradiction.
- [x] 2.2 Extend the using placement bullet with a clarification that usings after `namespace X;` are the same convention as `csharp_using_directive_placement = inside_namespace` under file-scoped namespaces. Verify: the bullet states the equivalence explicitly.

## 3. Compiler-validated XML docs

- [x] 3.1 Add `<GenerateDocumentationFile>true</GenerateDocumentationFile>` to `src/CLIHub.Core/CLIHub.Core.csproj` and `src/CLIHub/CLIHub.csproj`. Verify: `dotnet build CLIHub.sln` succeeds and emits `.xml` documentation artifacts next to the assemblies.
  - Note: build output is centralized under `artifacts\` by `Directory.Build.props`; the `.xml` doc files appear there next to the assemblies. The test project pins `GenerateDocumentationFile=false` explicitly (doc validation is for shipped public API only).
- [x] 3.2 Spike: determine which of `CS1570`, `CS1572`, `CS1573` fire by default once documentation generation is on, and pin their severities in `.editorconfig` if any do not surface. Verify: a deliberately drifted `<param>` tag produces a build warning.
  - Spike result: all three fire by default as compiler warnings; a probe with a mismatched param tag, a missing param tag, and malformed XML produced CS1572, CS1573, and CS1570. No editorconfig pins needed.
- [x] 3.3 Resolve any doc-comment drift the new warnings expose in existing code. Verify: `dotnet build CLIHub.sln` reports 0 warnings.
  - No drift found: the full solution builds with 0 warnings with doc validation enabled.

## 4. Final verification

- [x] 4.1 Run `dotnet build CLIHub.sln` (0 warnings, 0 errors), the test suite (`dotnet test`), and `openspec validate align-code-style-rules`. Verify: all three pass.
