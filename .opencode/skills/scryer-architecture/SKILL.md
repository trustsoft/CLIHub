---
name: scryer-architecture
description: Validate that development stays inside the architecture model the user defined in Scryer. Use before any code change in this repo, when planning a feature or OpenSpec change, when a task or file may fall outside the modeled architecture, or when reconciling code with the model. Triggers on architecture, scryer, model, drift, responsibility, source mapping, validate_model, orient.
license: MIT
compatibility: Requires the scryer-mcp MCP server (the `scryer` server in opencode.jsonc).
---

# Architecture model (Scryer)

This repo carries an authored architecture model in `.scryer/` (committed
`model.scry` = what the code is believed to satisfy; `planned.scry` = the draft
you and the canvas edit; their difference is the plan). Scryer is connected as
the `scryer` MCP server. Its tools are named `orient`, `locate`, `read_model`,
`get_health`, `get_pending`, `get_drift`, `get_rules`, `validate_model`,
`open_change`, `mark_implemented`, `update_source_map`, `flag_drift`,
`ingest_test_report`, and so on; opencode may expose them with a `scryer_`
prefix.

**The model is the user's authored spec, not background. Do not treat the
codebase as the source of truth, and never infer modeling conventions from
existing nodes — fetch the rules: `get_rules {id: "slug-a,slug-b"}` (or
`get_rules {}` for the index).**

## The gate: is this work inside the architecture?

Before writing code for any task beyond a one-line fix:

1. `open_change {rationale}` — one sentence, as the user put it. Plan writes are
   refused while no change is open.
2. `orient {task, files}` — the front door. It returns the governing node chain
   per file, anchored claims (with `untested` flags), **binding directives**, the
   matching rule slugs, and a `phase` verdict. Use `locate {file, symbol}` for a
   single-file reverse lookup.
3. **Coverage is `files[].ownerChain` + `files[].claims`, not `matches`.**
   `matches` are fuzzy task-keyword hits and may fire on any node; only an
   `ownerChain`/claim means the file is actually modeled. **If the touched files
   resolve to no governing node/claim, the work is outside the modeled
   architecture.** Do not start coding: surface the gap to the user and either
   (a) plan the missing node/claims into the model first, or (b) get a decision.
   Honor every directive `orient` returns — directives are the user's binding
   HOW-constraints; never write them unasked (`set_directives` only when the user
   explicitly asks).
4. `validate_model` after structural model edits; `get_health` to see where work
   is needed before reading subtrees.

## The build loop (bidirectional)

Plan first, then build to match the plan, then fold the work back:

- **PLAN** — author the change into the model before code, only where the change
  alters what the model claims.
- **SIGN-OFF** — tell the user what you planned and get their go-ahead; record it
  with `sign_off`.
- **BUILD** — implement claim by claim. Every testable claim (EARS
  When/While/If) gets its test in the project's own suite.
- **CLOSE** — run the affected tests with a JUnit reporter
  (`dotnet test --logger "junit;LogFilePath=TestResults/junit.xml"`; add the
  logger package if the solution lacks one), then `ingest_test_report` the XML,
  then `mark_implemented` with `anchors` and `tests` **in the same call**. Finish
  with `get_test_radius`, `flag_drift` for anything the code diverged on, and
  `reconcile_drift`. A change that filed nothing closes with `close_change`.

`mark_implemented` folds are gated on a passing verdict — run tests and ingest
the report first. Never use `force: true` to bypass the gate.

## Relation to OpenSpec

OpenSpec answers **what** the change is (proposal → specs → design → tasks).
Scryer answers **where it lands** in the architecture: which nodes own it, which
claims and tests back it, and whether it drifted. Run both: as a change takes
shape, plan it into the model; as it is implemented, fold it back.

`docs/architecture-map.md` is the index between the two: set an OpenSpec
capability name to the model node (id + breadcrumb) and code paths it owns.
Start there to find the node a capability belongs to, then confirm with
`orient`.

## Project commands

- Build: `dotnet build CLIHub.sln`
- All tests: `dotnet test CLIHub.sln`
- One test: `dotnet test tests/CLIHub.Core.Tests --filter FullyQualifiedName~<Name>`

For a read-only compliance verdict without touching the model, delegate to the
`architecture-review` subagent.
