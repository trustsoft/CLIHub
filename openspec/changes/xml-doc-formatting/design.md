## Context

The conventions are fixed by AGENTS.md ("XML documentation") and already proven on `src/CLIHub.Core/Models/AgentCommands.cs`. The remaining drift is mechanical and wide: 86 of 106 `.cs` files carry the old single-space summary indent; many also miss docs on public members. Because the change is comment-only, the risk sits in accidental code edits during mass reformatting, not in logic.

## Goals / Non-Goals

**Goals:**
- Bring every `.cs` file under `src/` and `tests/` to the documented conventions in one pass.
- Prove the pass touched nothing but comments with a diff check.

**Non-Goals:**
- No rewording campaigns beyond sentence normalization (capital letter, trailing period) — existing meaning is preserved.
- No `.editorconfig` keys for doc-comment layout (no reliable standard keys exist).
- No docs for private members beyond what already exists.

## Decisions

- **Add missing docs only outside the test project.** Compiling with `GenerateDocumentationFile` shows ~90 undocumented public members under `src/CLIHub` and ~300 in `tests/CLIHub.Tests`. Test classes and methods keep the repo's `MethodOrScenario_Condition_ExpectedResult` naming convention as their documentation; XML docs there would duplicate every test name as noise. Decided during apply with the user; the proposal's "add missing docs" goal is scoped to application code. The tests still get the formatting pass for their existing doc comments (fakes and helpers carry some).
- **Manual per-area passes instead of a regex script.** The transformations interact (multi-line vs single-line summaries, tag padding, adding missing docs), and the volume (~86 files) is small enough for a few focused batches: Core models → Core services/interfaces → CLIHub app code → tests. A script would need review of every output anyway. (In practice a reviewed per-batch script handles the mechanical indent/padding steps; sentence normalization, continuation lines starting with a tag, and new docs stay manual.)
- **Verification by diff, build, and tests.** After each batch: `git diff` inspected for comment-only changes, then `dotnet build CLIHub.sln` and `dotnet test tests/CLIHub.Tests`. Doc-comment XML well-formedness is verified by building with `GenerateDocumentationFile` (surfaces CS1570). This catches accidental code edits deterministically.

## Risks / Trade-offs

- [Accidental code changes while editing comments at scale] → per-batch diff review plus build/test gates; batches are scoped by directory so a failure localizes.
- [Newly added docs may paraphrase behavior incorrectly] → doc text is derived from member names, existing summaries, and usage sites already in context; same review gate as the reformatting.
