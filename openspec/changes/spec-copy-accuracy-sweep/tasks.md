# Spec Copy Accuracy Sweep: Implementation Tasks

This is a spec-copy-only change. No source code, tests, or configuration are modified.
Build and test are not expected gates — record that explicitly instead of running them.

## Stage 1: Spec deltas

- [x] 1.1 Write `openspec/changes/spec-copy-accuracy-sweep/specs/preferences-ui/spec.md` — MODIFIED "Release notes from Settings" requirement: reword body from "from the status card" to "from the title row"; preserve requirement name and "Open release notes" scenario name.
- [x] 1.2 Write `openspec/changes/spec-copy-accuracy-sweep/specs/plugin-seeding/spec.md` — MODIFIED "Built-in agent descriptors" requirement: replace `copilot --continue` with `omp --continue` in the "Commands match the agent CLI surface" scenario text; preserve all scenario names.
- [x] 1.3 Write `openspec/changes/spec-copy-accuracy-sweep/specs/agent-version/spec.md` — MODIFIED "Concurrent version probes are coalesced" requirement: add one ADDED scenario ("No eviction of newer in-flight probe") stating that a delayed continuation of an awaited probe cannot evict a newer probe's in-flight request; keep the two existing scenario names ("Concurrent cache miss", "Different agents probe independently").

### Gate: Validate and verify

```powershell
openspec validate --all
git status --porcelain
```

- `openspec validate --all` must report 0 errors.
- `git status --porcelain` must show only additions under `openspec/changes/spec-copy-accuracy-sweep/` — no source, test, or config modifications.

## Stage 2: Backlog hygiene

Update the three resolved entries in `docs/backlog.md` following the existing convention
(`**Status: Resolved by <change-id> (2026-11-14)** — <one-line evidence>`).
Do NOT delete the entries — traceability is preserved until the archive lands, after which
their resolution status is the only remaining artifact in the backlog.

- [x] 2.1 "Optional spec scenario pinning the in-flight probe eviction invariant" (around line 97): add `**Status: Resolved by \`spec-copy-accuracy-sweep\` (2026-11-14)** — added scenario "No eviction of newer in-flight probe" to the "Concurrent version probes are coalesced" requirement in \`agent-version\`; the test at \`ProbeCacheTests.cs:194\` is the durable regression pin.`
- [x] 2.2 "plugin-seeding spec example still names a feed agent" (around line 463): add `**Status: Resolved by \`spec-copy-accuracy-sweep\` (2026-11-14)** — replaced \`copilot --continue\` with \`omp --continue\` in the "Commands match the agent CLI surface" scenario.`
- [x] 2.3 "Release notes requirement says 'status card' but the link is on the title row" (around line 530): add `**Status: Resolved by \`spec-copy-accuracy-sweep\` (2026-11-14)** — reworded requirement body from "from the status card" to "from the title row"; the link lives in \`UpdatesHeaderTitleExtra\` on the page title row.`

### Gate: Validate and verify

```powershell
openspec validate --all
git status --porcelain
```

- `openspec validate --all` must report 0 errors.
- `git status --porcelain` must show only `openspec/changes/spec-copy-accuracy-sweep/` and `docs/backlog.md` — no other tracked files changed.

## Notes

- **No build or test runs are needed.** This change modifies only spec text under `openspec/` and one documentation entry in `docs/backlog.md`. No source code, tests, or project files are touched.
- **graphify update .** is skipped. No source code changed, so the knowledge graph is unaffected.
- **Backlog entries are marked resolved, NOT deleted.** The pipeline protocol reviews this file during `/pipe-resume`; keeping resolved entries with their status markers prevents re-deriving completed work from conversation history. Deletion happens in a later prune pass once the archive is committed.