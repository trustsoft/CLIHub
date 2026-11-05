// Local verification for the pure state/path logic. Run with:
//   bun run verify.ts
// This file is not a plugin entrypoint; OpenCode loads tui.tsx only.

import { strict as assert } from "node:assert"
import { existsSync, mkdirSync, writeFileSync } from "node:fs"
import path from "node:path"
import { tmpdir } from "node:os"
import {
  SIDEBAR_LIMIT,
  STAGE_LETTERS,
  completionScore,
  configRoot,
  formatTimestamp,
  headerCounts,
  isCompleted,
  parseState,
  projectDirectory,
  readSnapshot,
  resolveStateFile,
  sidebarChanges,
  slugForProject,
  stageCounts,
  stageRows,
  type Change,
  type Stage,
  type Status,
} from "./state"

const project = "D:\\YandexDisk\\CLIHub"
const ctx = { data: { session: { get: () => ({ location: { directory: project } }) } } }

// configRoot() is the OpenCode config directory: the plugin's grandparent when
// it holds `plugins/`, otherwise ~/.config/opencode. Assert the contract
// (a directory that contains `plugins/`), not a machine-specific suffix: in
// the opencode-sidebar-suite repository the same layout holds at the repo root.
assert.ok(existsSync(path.join(configRoot(), "plugins")), "configRoot() resolves to the directory holding plugins/")
assert.equal(slugForProject(project), "CLIHub")
assert.equal(slugForProject(undefined), "default")
assert.equal(slugForProject("D:\\A B\\repo-name"), "repo-name")

const live = path.join(configRoot(), "pipeline", "CLIHub.json")
if (existsSync(live)) {
  process.env.OPENCODE_PIPELINE_FILE = live
  const snapshot = readSnapshot(ctx, "ses_verify")
  assert.equal(snapshot.ok, true)
  assert.equal(snapshot.changes.length, 5)
  for (const id of [
    "unify-logo-pipeline",
    "introduce-project-model",
    "modularize-di-registration",
    "harden-agent-plugin-boundaries",
  ]) {
    const change = snapshot.changes.find((entry) => entry.id === id)
    assert.ok(change, `missing ${id}`)
    assert.equal(change?.stage, "archive")
    assert.equal(change?.status, "done")
  }
  const last = snapshot.changes.find((entry) => entry.id === "harden-probe-caching-and-process-matching")
  assert.ok(last, "missing harden-probe-caching-and-process-matching")
  assert.equal(stageRows(last!).length, 4)
  delete process.env.OPENCODE_PIPELINE_FILE
  console.log(`live state OK: ${live} (${snapshot.changes.length} changes)`)
} else {
  console.log(`live state skipped: ${live} not found`)
}

assert.equal(parseState("{", "x").reason, "malformed")
assert.equal(parseState('{"changes":[]}', "x").reason, "empty")
assert.equal(parseState('{"changes":[{"id":"a","stage":"bogus"}]}', "x").reason, "invalid")

const derived = parseState(JSON.stringify({ changes: [{ id: "x", stage: "commit", status: "active" }] }), "x")
assert.equal(derived.ok, true)
assert.deepEqual(
  stageRows(derived.changes[0]).map((row) => row.status),
  ["done", "done", "active", "pending"],
)

assert.equal(formatTimestamp(undefined), "—")
assert.equal(formatTimestamp("2026-10-20T02:10:25+03:00").startsWith("2026-10-20 "), true)

const scratch = path.join(tmpdir(), "opencode-pipeline-verify")
mkdirSync(scratch, { recursive: true })

const badFile = path.join(scratch, "bad.json")
writeFileSync(badFile, "{ nope", "utf8")
process.env.OPENCODE_PIPELINE_FILE = badFile
assert.equal(readSnapshot(ctx, "ses_verify").reason, "malformed")

const missingFile = path.join(scratch, "missing.json")
process.env.OPENCODE_PIPELINE_FILE = missingFile
assert.equal(readSnapshot(ctx, "ses_verify").reason, "missing")
delete process.env.OPENCODE_PIPELINE_FILE

const localProject = path.join(scratch, "local-project")
mkdirSync(path.join(localProject, ".opencode"), { recursive: true })
const localFile = path.join(localProject, ".opencode", "pipeline.json")
writeFileSync(localFile, JSON.stringify({ changes: [{ id: "local", stage: "plan", status: "active" }] }), "utf8")
const localCtx = { data: { session: { get: () => ({ location: { directory: localProject } }) } } }
assert.equal(resolveStateFile(localCtx, "ses_verify").file, localFile)

// The project a host exposes on the ctx is a candidate: without it the block
// falls through to `process.cwd()` and reads another project's state file. The
// list matches `plugins/model-policy/state.ts`, so both sidebar blocks read the
// project the session is in.
assert.equal(projectDirectory({ location: { project: { directory: localProject } } }, "ses_verify"), localProject)
assert.equal(projectDirectory(localCtx, "ses_verify"), localProject)
// A session location, when the host exposes one, still outranks the ctx project.
const otherProject = path.join(scratch, "other-project")
assert.equal(
  projectDirectory(
    {
      location: { project: { directory: otherProject } },
      data: { session: { get: () => ({ location: { directory: localProject } }) } },
    },
    "ses_verify",
  ),
  localProject,
)

// --- Sidebar window and order: pure, no file and no Solid involvement -----

const makeChange = (id: string, stage: Stage, status: Status): Change => ({
  id,
  stage,
  status,
  commits: {},
  stages: {},
})
const unfinished = (id: string) => makeChange(id, "impl", "pending")
const archived = (id: string) => makeChange(id, "archive", "done")
const idsOf = (changes: Change[]) => changes.map((entry) => entry.id)

// Per-stage header counts: every stage key is present, zero included, and the
// counts follow the current `stage` pointer of each change.
const sixChanges = [
  makeChange("plan-0", "plan", "pending"),
  makeChange("impl-0", "impl", "active"),
  makeChange("impl-1", "impl", "pending"),
  makeChange("commit-0", "commit", "done"),
  makeChange("archive-0", "archive", "done"),
  makeChange("archive-1", "archive", "done"),
]
assert.deepEqual(stageCounts(sixChanges), { plan: 1, impl: 2, commit: 1, archive: 2 })
assert.deepEqual(stageCounts([]), { plan: 0, impl: 0, commit: 0, archive: 0 })
const sixCounts = stageCounts(sixChanges)
assert.equal(sixCounts.plan + sixCounts.impl + sixCounts.commit + sixCounts.archive, sixChanges.length)

// The sidebar header drops zero-count stages entirely and keeps stage order.
assert.deepEqual(headerCounts(sixChanges), ["1 p", "2 i", "1 c", "2 a"])
assert.deepEqual(headerCounts([]), [])
// Real-world shape: everything archived, so only the archive block survives.
assert.deepEqual(headerCounts(Array.from({ length: 10 }, () => archived("a"))), ["10 a"])
// Mixed: plan work in flight next to finished archives.
assert.deepEqual(
  headerCounts([
    makeChange("header-plan", "plan", "pending"),
    ...Array.from({ length: 2 }, () => archived("a")),
  ]),
  ["1 p", "2 a"],
)

// The shared stage letters feed both the header counts and the sidebar change
// rows: all four stages map to one letter, and each header segment uses the
// letter of the stage at that position.
assert.deepEqual(STAGE_LETTERS, { plan: "p", impl: "i", commit: "c", archive: "a" })
const sixStages = ["plan", "impl", "commit", "archive"] as const
const sixSegments = headerCounts(sixChanges)
assert.equal(sixSegments.length, sixStages.length)
sixStages.forEach((stage, index) => {
  assert.equal(sixSegments[index], `${sixCounts[stage]} ${STAGE_LETTERS[stage]}`)
  assert.equal(STAGE_LETTERS[stage].length, 1)
})
assert.deepEqual(idsOf(sixChanges), [
  "plan-0",
  "impl-0",
  "impl-1",
  "commit-0",
  "archive-0",
  "archive-1",
])

assert.equal(SIDEBAR_LIMIT, 10)
assert.equal(isCompleted(archived("x")), true)
assert.equal(isCompleted(makeChange("x", "impl", "done")), false)
assert.equal(isCompleted(makeChange("x", "commit", "done")), false)
assert.equal(isCompleted(makeChange("x", "archive", "active")), false)
assert.equal(isCompleted(makeChange("x", "archive", "blocked")), false)

// Fully completed means "status done AND all four stage rows done". The
// top-level `stage` pointer alone is not trusted: legacy records sit at
// `impl`/`commit` with every stage row finished.
const allStagesDone = (
  id: string,
  stage: Stage,
  status: Status,
  overrides: Partial<Record<Stage, { status: Status }>> = {},
): Change => ({
  id,
  stage,
  status,
  commits: {},
  stages: {
    plan: { status: "done" },
    impl: { status: "done" },
    commit: { status: "done" },
    archive: { status: "done" },
    ...overrides,
  },
})

assert.equal(isCompleted(allStagesDone("legacy-impl", "impl", "done")), true)
assert.equal(isCompleted(allStagesDone("legacy-commit", "commit", "done")), true)
assert.equal(isCompleted(allStagesDone("all-done-archive", "archive", "done")), true)
assert.equal(completionScore(allStagesDone("legacy-impl", "impl", "done")), 4)

// A missing or pending stage row keeps the row: the record still shows work.
assert.equal(isCompleted(allStagesDone("missing-archive", "impl", "done", { archive: undefined })), false)
assert.equal(
  isCompleted(allStagesDone("impl-archive-pending", "impl", "done", { archive: { status: "pending" } })),
  false,
)
// An `archive` pointer over an unfinished stage row is inconsistent, not completed.
assert.equal(
  isCompleted(allStagesDone("archive-pointer-pending", "archive", "done", { archive: { status: "pending" } })),
  false,
)
// Unfinished changes are never "completed", however many stage rows are done.
assert.equal(isCompleted(allStagesDone("all-done-active", "archive", "active")), false)
assert.equal(isCompleted(allStagesDone("all-done-blocked", "impl", "blocked")), false)
assert.equal(isCompleted(makeChange("plan-pending", "plan", "pending")), false)

// Empty lists stay empty; equal-score rows keep their state-file order.
assert.deepEqual(sidebarChanges([]), [])
const tenUnfinished = Array.from({ length: 10 }, (_, index) => unfinished(`u${index}`))
assert.deepEqual(idsOf(sidebarChanges(tenUnfinished)), idsOf(tenUnfinished))
assert.deepEqual(idsOf(sidebarChanges(tenUnfinished, 10)), idsOf(tenUnfinished))

// 11 changes whose oldest entry is fully completed: only that entry is dropped.
const elevenWithOldestCompleted = [
  archived("oldest"),
  ...Array.from({ length: 10 }, (_, index) => unfinished(`u${index}`)),
]
const elevenResult = sidebarChanges(elevenWithOldestCompleted)
assert.deepEqual(
  idsOf(elevenResult),
  Array.from({ length: 10 }, (_, index) => `u${index}`),
)
assert.equal(idsOf(elevenResult).includes("oldest"), false)

// 12 changes with 5 completed interleaved: the 2 oldest completed go first;
// the newest completed and every unfinished entry survive, completed first.
const interleaved: Change[] = []
for (let index = 0; index < 12; index += 1) {
  interleaved.push(index % 2 === 0 && index < 9 ? archived(`c${index}`) : unfinished(`c${index}`))
}
const interleavedResult = sidebarChanges(interleaved)
assert.deepEqual(idsOf(interleavedResult), ["c4", "c6", "c8", "c1", "c3", "c5", "c7", "c9", "c10", "c11"])
assert.equal(idsOf(interleavedResult).includes("c0"), false)
assert.equal(idsOf(interleavedResult).includes("c2"), false)
assert.equal(idsOf(interleavedResult).includes("c8"), true)
for (const index of [1, 3, 5, 7, 9, 10, 11]) {
  assert.equal(idsOf(interleavedResult).includes(`c${index}`), true, `unfinished c${index} was hidden`)
}

// `status: "done"` on an intermediate stage is not fully completed: never dropped.
// Keeping both proves nothing is hidden; the stage counts then decide the order.
const notFullyCompleted = [
  makeChange("impl-done", "impl", "done"),
  makeChange("commit-done", "commit", "done"),
  ...Array.from({ length: 9 }, (_, index) => unfinished(`v${index}`)),
]
const notFullyCompletedResult = sidebarChanges(notFullyCompleted)
assert.deepEqual(idsOf(notFullyCompletedResult), [
  "commit-done",
  "impl-done",
  ...Array.from({ length: 9 }, (_, index) => `v${index}`),
])
assert.equal(notFullyCompletedResult.length, 11)

// Active and blocked entries are never dropped, even as the oldest rows.
const protectedStates = [
  makeChange("oldest-active", "impl", "active"),
  makeChange("oldest-blocked", "commit", "blocked"),
  archived("only-completed"),
  ...Array.from({ length: 8 }, (_, index) => unfinished(`w${index}`)),
]
const protectedResult = sidebarChanges(protectedStates)
assert.deepEqual(idsOf(protectedResult).slice(0, 2), ["oldest-blocked", "oldest-active"])
assert.equal(idsOf(protectedResult).includes("oldest-active"), true)
assert.equal(idsOf(protectedResult).includes("oldest-blocked"), true)
assert.equal(idsOf(protectedResult).includes("only-completed"), false)
assert.equal(protectedResult.length, 10)

// 12 unfinished changes: nothing is dropped, so the sidebar exceeds the limit.
const twelveUnfinished = Array.from({ length: 12 }, (_, index) => unfinished(`n${index}`))
const twelveResult = sidebarChanges(twelveUnfinished)
assert.equal(twelveResult.length, 12)
assert.deepEqual(idsOf(twelveResult), idsOf(twelveUnfinished))

// Not enough completed changes to reach the limit: drop what exists, hide no work.
const thirteenWithOneCompleted = [
  archived("lone-completed"),
  ...Array.from({ length: 12 }, (_, index) => unfinished(`m${index}`)),
]
const thirteenResult = sidebarChanges(thirteenWithOneCompleted)
assert.equal(thirteenResult.length, 12)
assert.equal(idsOf(thirteenResult).includes("lone-completed"), false)

// --- Completion score and sidebar sort ------------------------------------

// Reference scale: done stages (0..4) plus 0.5 for started-but-unfinished work.
assert.equal(completionScore(makeChange("a", "archive", "done")), 4)
assert.equal(completionScore(makeChange("b", "commit", "done")), 3)
assert.equal(completionScore(makeChange("c", "impl", "active")), 1.5)
assert.equal(completionScore(makeChange("d", "impl", "pending")), 1)
assert.equal(completionScore(makeChange("e", "plan", "pending")), 0)
assert.equal(completionScore(makeChange("f", "plan", "blocked")), 0.5)
assert.equal(
  completionScore(makeChange("f", "plan", "blocked")) - completionScore(makeChange("e", "plan", "pending")),
  0.5,
)

// Rank order: archive/done > commit/done > impl/active > impl/pending > plan/pending.
const ranked = [
  makeChange("rank-plan-pending", "plan", "pending"),
  makeChange("rank-impl-pending", "impl", "pending"),
  makeChange("rank-impl-active", "impl", "active"),
  makeChange("rank-commit-done", "commit", "done"),
  makeChange("rank-archive-done", "archive", "done"),
]
assert.deepEqual(idsOf(sidebarChanges(ranked)), [
  "rank-archive-done",
  "rank-commit-done",
  "rank-impl-active",
  "rank-impl-pending",
  "rank-plan-pending",
])
assert.equal(idsOf(sidebarChanges(ranked)).length, ranked.length)

// Equal scores keep state-file order (stable sort, score-only comparator).
const ties = [archived("tie-a"), archived("tie-b"), archived("tie-c")]
assert.deepEqual(idsOf(sidebarChanges(ties)), ["tie-a", "tie-b", "tie-c"])
assert.equal(completionScore(ties[0]), completionScore(ties[2]))

// Window plus sort: 14 entries, 6 fully completed. The 4 oldest completed are
// dropped, then the 10 survivors are ordered completed-first.
const sortWindow = [
  archived("old-completed-0"), // 0 -> dropped
  makeChange("mid-active", "impl", "active"), // 1
  archived("old-completed-1"), // 2 -> dropped
  makeChange("pending-0", "plan", "pending"), // 3
  archived("old-completed-2"), // 4 -> dropped
  makeChange("mid-done", "commit", "done"), // 5
  archived("old-completed-3"), // 6 -> dropped
  makeChange("pending-1", "plan", "pending"), // 7
  archived("old-completed-4"), // 8
  unfinished("mid-pending"), // 9
  makeChange("pending-2", "plan", "pending"), // 10
  archived("old-completed-5"), // 11
  makeChange("pending-3", "plan", "pending"), // 12
  makeChange("pending-4", "plan", "pending"), // 13
]
const sortWindowBefore = idsOf(sortWindow)
const sortWindowResult = sidebarChanges(sortWindow)
assert.equal(sortWindowResult.length, SIDEBAR_LIMIT)
assert.deepEqual(idsOf(sortWindowResult), [
  "old-completed-4",
  "old-completed-5",
  "mid-done",
  "mid-active",
  "mid-pending",
  "pending-0",
  "pending-1",
  "pending-2",
  "pending-3",
  "pending-4",
])
assert.equal(isCompleted(sortWindowResult[0]), true)
for (const id of ["old-completed-0", "old-completed-1", "old-completed-2", "old-completed-3"]) {
  assert.equal(idsOf(sortWindowResult).includes(id), false, `${id} should be the oldest dropped`)
}
for (const id of ["old-completed-4", "old-completed-5", "mid-done", "mid-active", "mid-pending"]) {
  assert.equal(idsOf(sortWindowResult).includes(id), true, `${id} must survive the window`)
}
// Unfinished work sits at the bottom: the last five rows are all score 0.
for (const change of sortWindowResult.slice(5)) {
  assert.equal(completionScore(change), 0)
}
// Scores never increase down the list.
for (let index = 1; index < sortWindowResult.length; index += 1) {
  assert.ok(
    completionScore(sortWindowResult[index - 1]) >= completionScore(sortWindowResult[index]),
    `scores out of order at row ${index}`,
  )
}

// The input array is neither reordered nor returned in place.
assert.deepEqual(idsOf(sortWindow), sortWindowBefore)
assert.notEqual(sortWindowResult, sortWindow)

// Legacy trimmable rows: only `impl`/`done` entries whose four stage rows are
// done count as completed, so 11 of them plus 1 active still fit into 10 rows.
const legacyWindow = [
  ...Array.from({ length: 11 }, (_, index) => allStagesDone(`legacy-${index}`, "impl", "done")),
  makeChange("legacy-active", "impl", "active"),
]
const legacyWindowResult = sidebarChanges(legacyWindow)
assert.equal(legacyWindowResult.length, SIDEBAR_LIMIT)
assert.equal(isCompleted(legacyWindowResult[0]), true)
assert.equal(idsOf(legacyWindowResult).includes("legacy-0"), false)
assert.equal(idsOf(legacyWindowResult).includes("legacy-1"), false)
assert.equal(idsOf(legacyWindowResult).includes("legacy-2"), true)
assert.deepEqual(idsOf(legacyWindowResult).slice(-1), ["legacy-active"])

// 12 such entries plus 1 active: 13 rows trim to 10 by dropping the 3 earliest
// completed rows, so legacy finished work cannot push the sidebar past the limit.
const legacyBulk = [
  ...Array.from({ length: 12 }, (_, index) => allStagesDone(`bulk-${index}`, "impl", "done")),
  makeChange("bulk-active", "impl", "active"),
]
const legacyBulkResult = sidebarChanges(legacyBulk)
assert.equal(legacyBulkResult.length, SIDEBAR_LIMIT)
for (const id of ["bulk-0", "bulk-1", "bulk-2"]) {
  assert.equal(idsOf(legacyBulkResult).includes(id), false, `${id} should be the oldest dropped`)
}
assert.equal(idsOf(legacyBulkResult).includes("bulk-3"), true)
assert.equal(idsOf(legacyBulkResult).includes("bulk-active"), true)

console.log("verify: all assertions passed")
