---
description: "Run the OpenSpec + Scryer architecture loop for a change (gate → plan → sign-off → build → review → close)"
---

Run the architecture-governed change loop for: $ARGUMENTS

Two artifacts govern this repo, and this command keeps them together:

- **OpenSpec** (`openspec/specs`, `openspec/changes`) — *what* the system must do.
- **Scryer model** (`.scryer/`, MCP server `scryer`) — *where* it lives, which
  claims own it, which tests back it. The model is the user's authored spec.

Start by loading the `scryer-architecture` skill (it holds the loop and the
gate) and reading `docs/architecture-map.md` to find the capability and its
model node. Fetch modeling rules with `get_rules` — never infer them.

**Scale to the work (proportionality).** A typo or a refactor that does not
change what the model claims needs no Scryer change and often no OpenSpec change.
A behavioral or contract change runs the whole loop. Say which scale you are on.

---

**1. GATE — is this inside the architecture?**

- `scryer_orient {task, files}` (use `scryer_locate {file, symbol}` for one file).
- Coverage is `files[].ownerChain` + `files[].claims`. `matches` are fuzzy
  task-keyword hits, **not** coverage.
- If the touched files resolve to no governing node or claim, **STOP**. Report
  the gap and ask the user: plan the missing node/claims into the model first,
  or drop the work. Do not write code.
- Honor every binding directive `orient` returns (own and inherited).

**2. PLAN**

- Follow the `openspec-propose` skill to create the change: proposal, spec
  deltas, design, tasks. Planning only — do not edit code.
- Scryer: `scryer_open_change {rationale}`, then author the model delta
  (nodes, responsibilities, links, boundaries) **only** where the change alters
  what the model claims. Then `scryer_sign_off`.
- Present both (OpenSpec artifacts + model delta) and **STOP** for the user's
  go-ahead. Do not implement before sign-off.

**3. BUILD — only after sign-off**

- Follow the `openspec-apply-change` skill to work through the tasks.
- Before each edit, `orient`/`locate` the target and follow its directives.
- Every testable claim gets a test in `tests/CLIHub.Core.Tests`.
- Run the suite with a JUnit report:
  `dotnet test CLIHub.sln --logger "junit;LogFilePath=TestResults/junit.xml"`
- `scryer_ingest_test_report TestResults/junit.xml`
- `scryer_mark_implemented` with `anchors` and `tests` in the **same** call —
  gated on a passing verdict. Never `force: true`.
- `scryer_get_test_radius` → re-run only the test files it names.

**4. REVIEW**

- Delegate to the `architecture-review` subagent for a read-only verdict
  (`in-architecture` / `out-of-architecture` / `drift-risk`).
- `scryer_validate_model`. Fix or report every BLOCKING finding.

**5. CLOSE**

- Fold the change (`mark_implemented {change}`) or `scryer_close_change` if it
  filed nothing.
- `scryer_get_drift` → `scryer_flag_drift` (undescribed/stale) →
  `scryer_reconcile_drift` for anything the code diverged on.
- Archive with the `openspec-archive-change` skill.
- Update `docs/architecture-map.md` if a capability's owning node changed.

---

**Guardrails**

- Do not write application code before the user's sign-off on the plan.
- Write the model only through scryer tools — never edit `.scryer/*.scry` by hand.
- Do not write directives (`set_directives`) unless the user explicitly asks.
- If `orient` finds no governing node, that *is* the finding — surface it, never
  silently proceed.
- Pause and ask on blockers; do not guess. Report the scale and the phase you
  are in at each step.
