## Why

Three spec-accuracy items from `docs/backlog.md` represent factual discrepancies between spec text and the implemented behavior. Each is low-severity (no behavioral defect), but the inaccuracies mislead readers and erode the specs' value as a durable behavior contract. This sweep corrects them in one pass with zero code changes.

## What Changes

### 1. Release notes location in `preferences-ui` spec
- The "Release notes from Settings" requirement body says the user opens release notes "from the status card."
- The "Release notes →" link actually lives on the Updates page title row (`UpdatesHeaderTitleExtra`), not on the status card (`UpdatesHeaderAction` status chip).
- Reword the requirement body to "from the title row." Preserve the requirement name and the "Open release notes" scenario name.

### 2. Built-in agent example in `plugin-seeding` spec
- The "Commands match the agent CLI surface" scenario uses `copilot --continue` as its example.
- GitHub Copilot is a feed plugin, not a built-in. The five built-in seed descriptors are OpenCode, Pi, OMP, Qwen Code, and Devin — all use `--continue` as their resume argument.
- Switch the example to `omp --continue`. Preserve the scenario name.

### 3. In-flight probe eviction invariant in `agent-version` spec
- The "Concurrent version probes are coalesced" requirement already pins probe coalescing, but does not pin the invariant that a delayed continuation of an awaited probe cannot evict a newer probe's in-flight request.
- This invariant is implemented at `src/CLIHub.Core/Agents/ProbeCache.cs:66` (compare-and-remove `TryRemove`) and regression-tested by `ProbeCacheTests.GetOrProbe_HeldCallerContinuationAfterNewerProbePublished_DoesNotEvictNewerInFlightProbe`.
- Add one ADDED scenario to the `agent-version` spec under that requirement. The `agent-update-detection` spec's concurrent-check coalescing scenario calls out the same shared mechanism without defining its invariants; the `agent-version` requirement is the natural home (see design.md D3).

### 4. Backlog hygiene
- Mark the three resolved entries in `docs/backlog.md` with `**Status: Resolved by \`spec-copy-accuracy-sweep\` (2026-11-14)**` following the file's existing convention.
- Entries are marked, NOT deleted — traceability is preserved until the archive lands (design.md D4).

## Capabilities

### Modified Capabilities

- `preferences-ui`: Requirements text updated (no behavioral change)
- `plugin-seeding`: Example updated (no behavioral change)
- `agent-version`: One scenario added (no behavioral change)

### New Capabilities

None.

## Impact

**Behavior changes:** None. This is a spec-copy-only sweep. No source code, tests, or configuration are modified.

**Code changes:** None.

**Spec deltas:**
- `preferences-ui`: MODIFIED "Release notes from Settings" — reworded requirement body
- `plugin-seeding`: MODIFIED "Built-in agent descriptors" — updated example
- `agent-version`: MODIFIED "Concurrent version probes are coalesced" — one ADDED scenario

**Documentation:**
- `docs/backlog.md`: Three entries marked resolved

## Explicit Non-Goals

- No source-code or behavior changes.
- Do not change the `PluginFeedClient` redirect hop off-by-one (spec says "maximum of 5 hops", the loop allows 4 followed redirects); that stays a separate backlog item for a code change.
- Do not touch UI, tests, or unrelated spec text; no opportunistic rewrites beyond these three items.
- Do not add a scenario to `agent-update-detection` — the invariant is probe-cache internal and the `agent-version` "Concurrent version probes are coalesced" requirement is the natural spec home (see design.md D3).

## Dependencies

None. This change is self-contained and does not block or depend on other changes.