# CLIHub Agent Instructions

Read [`docs/project-context.md`](docs/project-context.md) first. It is the compact project context and
routes task-specific questions to the canonical documentation. Read only the relevant topic or OpenSpec
spec; do not load the whole documentation tree.

## Project Rules

- Windows 10/11 only; the solution is a .NET 10 WPF application using C# 14.0.
- `CLIHub.Core` has no WPF dependency. Keep UI-independent logic in Core and UI composition in `CLIHub`.
- Plugins are JSON descriptors only. Do not load DLLs, execute plugin code in-process, or add Windows Forms.
- Use the existing MVVM, coordinator, controller, service, and interface patterns before adding abstractions.
- Preserve the single camelCase configuration document and its atomic persistence boundary.
- Keep code, comments, and UI text in English. Documentation may use English or Russian when appropriate.
- Do not create projects in the repository root; use the existing solution layout under `src/` and `tests/`.

## Code Style

- Nullable reference types, implicit usings, file-scoped namespaces, and braces are required.
- Under a file-scoped namespace, place `using` directives after the namespace declaration and keep the configured
  ordering from `.editorconfig`.
- Use PascalCase for public members, `_camelCase` for private fields, and the test naming pattern
  `MethodOrScenario_Condition_ExpectedResult`.
- Public APIs need XML documentation. Use `async Task`/`ValueTask`; avoid `async void`.
- Keep WPF code-behind thin; put state, commands, and workflows in ViewModels or their existing collaborators.

## Commands

```powershell
dotnet build CLIHub.sln
dotnet test CLIHub.sln
dotnet run --project src/CLIHub/CLIHub.csproj
openspec validate --all
```

## Documentation and OpenSpec

- `README.md` is user-facing documentation.
- `docs/project-context.md` is the compact current context for agents.
- `docs/architecture.md` is the architecture index; topic files under `docs/architecture/` hold focused detail.
- `openspec/specs/` is the durable behavior contract. `openspec/changes/archive/` is historical context.
- `docs/adr/` records why architectural decisions were made; it does not replace current implementation docs.
- Propose and validate an OpenSpec change before implementation when the work changes a durable capability.
- Archive completed changes after implementation and verification.

## Progressive Disclosure

1. Read this file and `docs/project-context.md`.
2. Read one relevant architecture topic or OpenSpec capability spec.
3. Inspect the source and tests for exact behavior and signatures.
4. Read ADRs or archived changes only when the reason or history matters.

Do not scan `openspec/changes/archive/`, `.opencode/`, `.pi/`, `graphify-out/`, `artifacts/`, or `obj/` for a
normal implementation task.

## Codebase Navigation

When `graphify-out/graph.json` exists, use graphify for repository-level questions. Use CodeGraph for symbol
and call-path exploration before broad grep/read loops. After modifying source code, refresh graphify as required
by the repository tooling.

<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **CLIHub** (7990 symbols, 14777 relationships, 482 execution flows).

> Index stale? Run `node .gitnexus/run.cjs analyze --index-only` from the project root — it auto-selects an available runner. No `.gitnexus/run.cjs` yet? Bootstrap with `npx`, `bunx`, or `pnpm dlx` — e.g. `bunx gitnexus@latest analyze` (npm 11 npx crash; #1939).

## Always Do

- **MUST run impact before editing.** Use `impact({target: "symbolName", direction: "upstream"})` or `node .gitnexus/run.cjs impact "symbolName" --direction upstream --repo .`; report callers, processes, and risk. Never substitute grep for graph analysis.
- **MUST analyze graph changes before committing.** Use `detect_changes({scope: "all"})` (MCP) or `node .gitnexus/run.cjs detect-changes --scope all --repo .` (CLI fallback). `partial: true` or `truncated: true` is not a clean check — a zero means unseen, not unaffected; re-run it. For regression review: `detect_changes({scope: "compare", base_ref: "master"})` or `node .gitnexus/run.cjs detect-changes --scope compare --base-ref "master" --repo .`.
- MUST warn on HIGH/CRITICAL `risk` pre-edit; never use `riskSharedAxes` to waive a HIGH/CRITICAL `risk` warning. Compare File/symbol: MCP File omits axes; Graph-RAG expands File.
- **MUST treat `risk: UNKNOWN` as unresolved, not as low.** An empty caller set is not evidence the symbol is unused — it can also mean the callers are not resolvable by the index (plain-object property access, dynamic dispatch, cross-language calls). `impact` pairs `UNKNOWN` with a `riskNote` saying so. Confirm with a text search before treating the symbol as safe to change or delete; do not proceed on the strength of a zero.
- **MUST use `query({search_query: "concept"})` for concepts/flows, `context({name: "symbolName"})` for a named symbol, or `impact` for blast radius, on read-only callers, dependencies, imports, or execution flow.** Graph first; text search only for empty/`UNKNOWN`/literals.
- For security review, `explain({target: "fileOrSymbol"})` lists taint findings (source→sink flows; needs `analyze --pdg`).

## Never Do

- NEVER edit a function, class, or method before MCP/CLI impact analysis.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis, and never read `UNKNOWN` as an all-clear — it means the walk could not answer, which is the one verdict that requires confirming by other means.
- NEVER rename symbols with find-and-replace — use `rename` which understands the call graph.
- NEVER commit before MCP/CLI graph change analysis.

## Resources

| Resource | Use for |
| --- | --- |
| `gitnexus://repo/CLIHub/context` | Codebase overview, check index freshness |
| `gitnexus://repo/CLIHub/clusters` | All functional areas |
| `gitnexus://repo/CLIHub/processes` | All execution flows |
| `gitnexus://repo/CLIHub/process/{name}` | Step-by-step execution trace |

## CLI

| Task | Read this skill file |
| --- | --- |
| Understand architecture / "How does X work?" | `.claude/skills/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.claude/skills/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.claude/skills/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.claude/skills/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.claude/skills/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.claude/skills/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->
