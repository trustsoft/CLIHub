## 1. CLIHub.Core

- [x] 1.1 Reformat XML docs in `src/CLIHub.Core/Models` (and add missing docs for public members) per AGENTS.md conventions; verify the batch diff touches only comments and `dotnet build CLIHub.sln` passes
- [x] 1.2 Reformat XML docs in `src/CLIHub.Core/Services` and `src/CLIHub.Core/Interfaces` per the same conventions; verify the batch diff touches only comments and the build passes
- [x] 1.3 Reformat remaining `src/CLIHub.Core` files (hotkeys, config, formatting, root-level files) per the same conventions; verify the batch diff touches only comments and the build passes

## 2. CLIHub app and tests

- [x] 2.1 Reformat XML docs in `src/CLIHub` (ViewModels, windows, hotkeys, interop, converters) and add missing docs for public members; verify the batch diff touches only comments and the build passes
- [x] 2.2 Reformat XML docs in `tests/CLIHub.Tests` per the same conventions; verify the batch diff touches only comments and the build passes

## 3. Verification

- [x] 3.1 Confirm no old-style summary lines remain (`^\s*/// ` finds nothing under `src/` and `tests/`) and `dotnet test tests/CLIHub.Tests` passes with the full suite
- [x] 3.2 Review the complete `git diff` to confirm comment-only changes, then run `openspec validate "xml-doc-formatting"` before archiving
