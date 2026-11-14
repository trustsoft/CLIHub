# Spec Copy Accuracy Sweep: Design Document

## Verified Current State

### Item 1: Release notes location

**Spec claim** (`openspec/specs/preferences-ui/spec.md:770`):
> The Updates page SHALL let the user open the application release notes from the status card.

**Actual location** — the "Release notes →" link lives on the page title row, not on a status card:

- `src/CLIHub/Views/SettingsWindow.xaml:79-94`: `DataTemplate x:Key="UpdatesHeaderTitleExtra"` contains the `Button` with `Content="Release notes →"` (line 90-93) and the version chip in a `HeaderChip` border (lines 81-86). This template is the **page title extra** — it renders inline with the page title.
- `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:108`: The Updates page binds `HeaderTitleExtraKey: "UpdatesHeaderTitleExtra"`, attaching this template to the title row.
- `src/CLIHub/Views/SettingsWindow.xaml:368-381`: The header layout renders `HeaderTitleExtraKey` in a `WrapPanel` next to the `PageHeader` `TextBlock` ("Updates") on the left side of the header — this is the **title row**.
- `src/CLIHub/Views/SettingsWindow.xaml:98-121`: `DataTemplate x:Key="UpdatesHeaderAction"` contains the `StatusChip` (line 102) with the update badge and the "Check for updates" button. This is the **page action area** on the right side of the header — it is the "status card" the spec refers to, but the "Release notes →" link is **not** in it.
- `src/CLIHub/Views/SettingsWindow.xaml:397-409`: `HeaderActionKey` renders on the right side (`Grid.Column="1"`), separate from the title row on the left side.

**Conclusion:** The "Release notes →" link is on the title row, not on the status card. The requirement body should say "from the title row."

### Item 2: Built-in agent example

**Spec claim** (`openspec/specs/plugin-seeding/spec.md:322`):
> for example `copilot --continue` resumes the most recent session

**Actual built-in set** — five seed descriptors confirmed under `src/CLIHub.Core/SeedPlugins/`:

| Agent | File | Resume command |
|---|---|---|
| OpenCode | `SeedPlugins/opencode/plugin.json:10` | `opencode --continue` |
| Pi | `SeedPlugins/pi/plugin.json:10` | `pi --continue` |
| OMP | `SeedPlugins/omp/plugin.json:10` | `omp --continue` |
| Qwen Code | `SeedPlugins/qwen-code/plugin.json:10` | `qwen --continue` |
| Devin | `SeedPlugins/devin/plugin.json:10` | `devin --continue` |

GitHub Copilot is **not** a built-in agent. It is a feed plugin (installed from the remote plugin feed). The spec's "Built-in agents seeded" scenario (`openspec/specs/plugin-seeding/spec.md:306`) correctly lists "OpenCode, Pi, OMP, Qwen Code, and Devin."

**Conclusion:** Replace `copilot --continue` with `omp --continue` (or any built-in; OMP is chosen as the shortest and clearest example with a straightforward CLI).

### Item 3: In-flight probe eviction invariant

**Implemented behavior:**

- `src/CLIHub.Core/Agents/ProbeCache.cs:66`:
  ```csharp
  this.inFlight.TryRemove(new KeyValuePair<string, Lazy<Task<TValue>>>(key, lazy));
  ```
  This is a **compare-and-remove** (`TryRemove` with a key-value pair), not a key-based `TryRemove`. It only removes the entry if the dictionary still holds the exact `Lazy<Task<TValue>>` instance that this caller awaited. If a newer probe's `Lazy` instance replaced it in the meantime, the remove is a no-op.

- `tests/CLIHub.Core.Tests/Agents/ProbeCacheTests.cs:194-264`:
  `GetOrProbe_HeldCallerContinuationAfterNewerProbePublished_DoesNotEvictNewerInFlightProbe`:
  1. Caller A starts a probe and its `finally` continuation is held on a queueing `SynchronizationContext`.
  2. Caller B joins the same probe and completes it (removing the completed in-flight entry normally).
  3. The cache entry is invalidated (`Remove(key)`), and caller C starts a **newer** probe with a different `Lazy` instance.
  4. Caller A's held `finally` continuation runs. The `TryRemove(key, lazy)` with the **old** `Lazy` instance does not remove the **new** `Lazy` instance.
  5. Caller D joins caller C's probe (not starting a third one), proving the newer in-flight entry survived.

**Existing spec coverage:**

- `agent-version` spec: "Concurrent version probes are coalesced" requirement — scenarios for concurrent cache miss and independent-agent probing. Does **not** pin the delayed-continuation eviction safety.
- `agent-update-detection` spec: "Availability results are cached per agent" requirement — "Concurrent checks are coalesced" scenario. Also does **not** pin the invariant.

**Conclusion:** Add one ADDED scenario to `agent-version` "Concurrent version probes are coalesced" stating that a delayed continuation cannot evict a newer probe's in-flight request. The two existing scenarios ("Concurrent cache miss" and "Different agents probe independently") keep their names.

---

## Decisions

### D1: Reword requirement body, not the scenario

**Decision:** Change the "Release notes from Settings" requirement body from "from the status card" to "from the title row." Leave the "Open release notes" scenario untouched.

**Rationale:** The scenario text is already correct — it says "WHEN the user activates 'Release notes'" — and covers the behavior. Only the requirement body misstates the location. Keeping the delta minimal reduces risk of inadvertently changing scenario meaning.

### D2: Use `omp --continue` as the example

**Decision:** Replace `copilot --continue` with `omp --continue` in the "Commands match the agent CLI surface" scenario.

**Rationale:** OMP is one of the five built-in agents listed in the "Built-in agents seeded" scenario. It has the simplest and most common CLI pattern among the five (single-word executable, standard `--continue` flag). The other built-ins also use `--continue`, so any of them would work; OMP is chosen for brevity and clarity.

**Alternative rejected — OpenCode or Pi:** Both also use `--continue`. OpenCode's `executable` is `opencode` (two-part package name), and Pi's is `pi`. OMP has the shortest executable name (`omp`) and a straightforward agent identity, which makes the example self-contained without needing to explain the CLI naming.

**Alternative rejected — phrasing without an agent name:** The scenario's purpose is to give a concrete, recognizable example of how the `resume` command maps to the agent CLI. An abstract description ("for example `<agent> --continue`") would lose that concreteness and weaken the scenario.

### D3: Add the invariant scenario to `agent-version`, not `agent-update-detection`

**Decision:** Add the in-flight probe eviction scenario to the `agent-version` spec under "Concurrent version probes are coalesced." Do not add it to `agent-update-detection`.

**Rationale:**
- The test name (`ProbeCacheTests.GetOrProbe_HeldCallerContinuationAfterNewerProbePublished_DoesNotEvictNewerInFlightProbe`) names "Probe," and `ProbeCache` is named for probes — its primary consumer is the version probe service.
- The `agent-version` spec has the dedicated "Concurrent version probes are coalesced" requirement, whose body describes the probe-cache contract most directly.
- The `agent-update-detection` spec's "Concurrent checks are coalesced" scenario says "one source request runs and all callers receive the same result" — it calls out the shared mechanism but does not define the implementation invariants of the coalescing cache itself.
- The invariant is a property of `ProbeCache<T>`, which both services use, but version probing is the original and primary use case. Placing the invariant in `agent-version` puts it where the coalescing contract is defined.

---

### D4: Mark backlog entries resolved, do not delete

**Decision:** Add a `**Status: Resolved by \`spec-copy-accuracy-sweep\` (2026-11-14)**` line to each of the three backlog entries. Do NOT delete the entries.

**Rationale:** The pipeline protocol (`pipeline-protocol` skill) reviews `docs/backlog.md` during `/pipe-resume` to prevent re-deriving completed work from conversation history. Keeping resolved entries with their status markers ensures that any session that reads the file sees they were already addressed. Full deletion waits for a later prune pass once the archive commit lands (at which point git history is the permanent record and the resolution marker has served its purpose).

**Alternative rejected — delete on resolution:** Removes the traceability surface too early. A concurrent session or a resume could re-discover the same evidence and propose the same change again. The status line is a cheap, well-understood signal.

---

## Deviations from Brief

### Scope expanded: backlog hygiene added

The original brief listed only the three spec deltas. During review it was noted that the three corresponding backlog entries were still open and needed a resolution marker. Stage 2 (`tasks.md`) and D4 (`design.md`) now cover updating `docs/backlog.md` in the same change.

---

## Open Questions

### OQ-1: Should the redirect-hop off-by-one be documented as an explicit non-goal in the spec?

The `PluginFeedClient` redirect loop allows 4 followed redirects (`hop < MaxRedirectHops` with `MaxRedirectHops = 5`, so the 5th hop is rejected). This is recorded as a non-goal in this change's proposal — but note that it is a **genuine spec-vs-code mismatch**, not an undocumented detail. The specs already promise a 5-hop cap:

- `openspec/specs/plugin-feed/spec.md:583` — "redirects followed manually with per-hop HTTPS and a maximum of 5 hops"
- `openspec/specs/plugin-feed/spec.md:588` — logo bytes "downloaded under the redirect, hop-cap, and size rules"
- `openspec/specs/plugin-feed-sync/spec.md:179` — "the same redirect, hop-count, and size-bound guarantees" (with `:196` "no more than 5 hops are followed")
- `openspec/specs/plugin-index-signing/spec.md:53` — "a 5-hop cap" (with `:86` "no more than 5 hops are followed")

So this is not a missing-spec gap that this sweep could close with a copy edit. The specs promise 5 hops while the implementation follows at most 4 — that is a behavioral defect, and fixing it requires a code change plus its own spec delta (or a deliberate spec downgrade to "4 hops"). Either way it is out of scope for this copy-only sweep, which is why it stays a non-goal here.

**Recommendation for implementation session:** No action. The off-by-one is tracked separately in `docs/backlog.md`; its resolution will be a behavior-bearing change with its own spec delta.