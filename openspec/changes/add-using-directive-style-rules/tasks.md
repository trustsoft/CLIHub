## 1. Style configuration

- [x] 1.1 Add the using-directive organization keys to the "Microsoft .NET properties" section of the root `.editorconfig`: `dotnet_sort_system_directives_first = true`, `dotnet_separate_import_directive_groups = true`, `resharper_blank_lines_between_using_groups = 1`, `resharper_sort_usings_with_system_first = true` (per design D1, no severity pins per D2). Verify the keys sit under the existing section header and the file loads in the IDE without parsing complaints.

## 2. Normalize existing sources

- [x] 2.1 Reorganize using blocks in `src/CLIHub.Core`: `System.*` (and `Windows.*`) directives first in alphabetical order, one blank line, remaining namespaces in alphabetical order; commit separately from logic edits. Verify `dotnet build src/CLIHub.Core/CLIHub.Core.csproj` succeeds.
- [x] 2.2 Apply the same reorganization to `src/CLIHub`. Verify `dotnet build src/CLIHub/CLIHub.csproj` succeeds.
- [x] 2.3 Apply the same reorganization to `tests/CLIHub.Tests`. Verify `dotnet build tests/CLIHub.Tests/CLIHub.Tests.csproj` succeeds.
- [x] 2.4 Revise the grouping per user feedback: split using blocks into four buckets — `System.*`/`Windows.*`, `Microsoft.*`, `CLIHub.*`, remaining third-party — each alphabetically sorted by namespace name and separated by a blank line (UpdateService.cs as the reference layout); re-run the normalization pass over `src/` and `tests/`. Verify `dotnet build CLIHub.sln` and `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj` pass and a second normalization run reports no changes.

## 3. Documentation

- [x] 3.1 Add the three rules to the `AGENTS.md` Code Style section (BCL namespaces first, blank line between using groups, alphabetical order within each group), matching the wording of the neighboring rules. Verify the section reads coherently end to end.

## 4. Verification

- [x] 4.1 Spot-check compliance across the tree: pick a file with `System.*` usings (BCL group first, blank line before the rest), a file with no `System.*` usings (single alphabetically sorted group), and a file with `using` directives inside the namespace (unchanged placement rule). Verify `dotnet build CLIHub.sln` and `dotnet test tests/CLIHub.Tests/CLIHub.Tests.csproj` both pass.
