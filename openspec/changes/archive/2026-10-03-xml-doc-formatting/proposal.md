# Normalize XML documentation formatting

## Why

AGENTS.md now defines canonical XML documentation conventions (see the "XML documentation" bullet, with `AgentCommandKind.cs` and `AgentCommandResult.cs` as references), but 86 of 106 `.cs` files still use the old layout: single-space summary content (`/// Text`), unpadded single-line tags, and undocumented public members. The rules were only proven on `AgentCommands.cs`. Applying them everywhere once, mechanically, prevents the two styles from drifting further apart with every new edit.

## What Changes

- Reformat existing XML doc comments across `src/` and `tests/` to the conventions in AGENTS.md:
  - every `<summary>` becomes multi-line, content lines indented one space past the tag alignment (`///` + three spaces);
  - other tags (`<param>`, `<returns>`, `<exception>`, …) become single lines padded with one space inside both ends;
  - doc text normalized to full English sentences (capital first letter, trailing period).
- Add missing XML doc comments to public types and members that currently lack them in application code (`src/CLIHub`), using the same conventions. Test classes and methods are excluded: their `Method_Condition_ExpectedResult` names are the documentation by convention; tests get the formatting pass only.
- No behavior changes: comments only, plus new doc comments; no code statements, signatures, or ordering are modified.

## Capabilities

### New Capabilities

(none — comment-only refactoring, no spec-level behavior)

### Modified Capabilities

(none)

## Impact

- All `.cs` files under `src/CLIHub.Core`, `src/CLIHub`, and `tests/CLIHub.Tests` whose XML docs deviate from the conventions (about 86 files); most affected areas are `CLIHub.Core` services/interfaces/models and the WPF ViewModels/windows.
- Verification: `dotnet build CLIHub.sln` and `dotnet test tests/CLIHub.Tests` must pass with zero changes outside doc comments (checked via git diff).
