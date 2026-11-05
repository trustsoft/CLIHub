// Tests for the pipeline state writer. Run with:
//   node .opencode/scripts/pipeline-state.test.mjs
//
// Exercises the 10-entry cap, the completed-only pruning rule, the atomic
// file shape, the duplicate refusal, the compare-and-swap retry, the
// `update` transitions (stage/status, stage rows, notes, commits, stage
// backfill), and the `check` evidence reconciliation plus its write-time
// warning and record-consistency findings.

import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import {
  mkdirSync,
  mkdtempSync,
  readFileSync,
  readdirSync,
  rmSync,
  writeFileSync,
} from "node:fs";
import { tmpdir } from "node:os";
import { join, resolve as resolvePath } from "node:path";
import { fileURLToPath } from "node:url";
import {
  LIMIT,
  stageState,
  isCompleted,
  pruneCompleted,
  buildChange,
  readState,
  addChange,
  pruneChanges,
  applyUpdate,
  updateChange,
  collectFindings,
  evidenceProbes,
} from "./pipeline-state.mjs";

const SCRIPT = fileURLToPath(new URL("./pipeline-state.mjs", import.meta.url));
const BASE_UPDATED_AT = "2026-01-01T00:00:00.000Z";

let passed = 0;
function test(name, fn) {
  try {
    fn();
  } catch (error) {
    console.error(`not ok - ${name}`);
    throw error;
  }
  passed += 1;
  console.log(`ok - ${name}`);
}

function withDir(fn) {
  const dir = mkdtempSync(join(tmpdir(), "pipeline-state-"));
  try {
    return fn(dir);
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
}

/** A fully completed entry (stage pointer archive, no explicit rows needed). */
const completed = (id) => ({ id, stage: "archive", status: "done", updatedAt: BASE_UPDATED_AT });

/** An unfinished entry. */
const unfinished = (id, stage = "impl") => ({ id, stage, status: "active", updatedAt: BASE_UPDATED_AT });

function stateOf(changes, extra = {}) {
  return {
    schemaVersion: 1,
    project: "Fixture",
    updatedAt: BASE_UPDATED_AT,
    changes,
    ...extra,
  };
}

function writeStateRaw(file, state) {
  writeFileSync(file, `${JSON.stringify(state, null, 2)}\n`, "utf8");
}

function completedEntries(count) {
  return Array.from({ length: count }, (_, index) => completed(`done-${index}`));
}

function unfinishedEntries(count) {
  return Array.from({ length: count }, (_, index) => unfinished(`open-${index}`));
}

test("LIMIT is 10", () => {
  assert.equal(LIMIT, 10);
});

test("adding to 10 entries with 3 completed removes the oldest completed one", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    const changes = [];
    for (let index = 0; index < 10; index += 1) {
      changes.push(unfinished(`work-${index}`));
    }
    changes[0] = completed("oldest-done");
    changes[3] = completed("middle-done");
    changes[7] = completed("newest-done");
    writeStateRaw(file, stateOf(changes));

    const result = addChange(file, { id: "fresh", stage: "plan", status: "active" });
    const state = readState(file);

    assert.equal(state.changes.length, 10);
    assert.deepEqual(result.removed.map((entry) => entry.id), ["oldest-done"]);
    assert.equal(result.before, 10);
    assert.equal(result.after, 10);
    assert.equal(state.changes.at(-1).id, "fresh");
    assert.ok(!state.changes.some((entry) => entry.id === "oldest-done"));
    assert.ok(state.changes.some((entry) => entry.id === "middle-done"));
    assert.ok(state.changes.some((entry) => entry.id === "newest-done"));
    assert.notEqual(state.updatedAt, BASE_UPDATED_AT);
  });
});

test("adding to 10 unfinished entries appends anyway and warns", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    writeStateRaw(file, stateOf(unfinishedEntries(10)));

    const result = addChange(file, { id: "fresh" });
    const state = readState(file);

    assert.equal(state.changes.length, 11);
    assert.equal(state.changes.at(-1).id, "fresh");
    assert.equal(result.removed.length, 0);
    assert.equal(result.before, 10);
    assert.equal(result.after, 11);
    assert.equal(result.warnings.length, 1);
    assert.match(result.warnings[0], /exceeds limit 10/);
  });
});

test("adding to 9 entries appends without removing anything", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    const changes = [...completedEntries(3), ...unfinishedEntries(6)];
    writeStateRaw(file, stateOf(changes));

    const result = addChange(file, { id: "fresh" });
    const state = readState(file);

    assert.equal(state.changes.length, 10);
    assert.equal(result.removed.length, 0);
    assert.equal(result.before, 9);
    assert.equal(result.after, 10);
    assert.equal(state.changes.at(-1).id, "fresh");
  });
});

test("a duplicate id is refused with exit 2 and the file is byte-identical", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    writeStateRaw(file, stateOf([unfinished("dup"), ...unfinishedEntries(2)]));
    const before = readFileSync(file);

    const run = spawnSync(process.execPath, [SCRIPT, "add", "dup", "--file", file], { encoding: "utf8" });

    assert.equal(run.status, 2, run.stderr);
    assert.match(run.stderr, /error: .*already exists/);
    assert.equal(run.stdout, "");
    assert.deepEqual(readFileSync(file), before);
  });
});

test("prune trims 12 entries to 10 from the oldest completed, and --dry-run writes nothing", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    const changes = [...completedEntries(9), ...unfinishedEntries(3)];
    writeStateRaw(file, stateOf(changes));
    const before = readFileSync(file);

    const dry = spawnSync(process.execPath, [SCRIPT, "prune", "--dry-run", "--file", file], {
      encoding: "utf8",
    });
    assert.equal(dry.status, 0, dry.stderr);
    assert.deepEqual(dry.stdout.trimEnd().split(/\r?\n/), [
      "pruned done-0 (archive/done)",
      "pruned done-1 (archive/done)",
      `state: ${resolvePath(file)} (10 changes)`,
    ]);
    assert.deepEqual(readFileSync(file), before);

    const real = spawnSync(process.execPath, [SCRIPT, "prune", "--file", file], { encoding: "utf8" });
    assert.equal(real.status, 0, real.stderr);
    assert.deepEqual(real.stdout.trimEnd().split(/\r?\n/), [
      "pruned done-0 (archive/done)",
      "pruned done-1 (archive/done)",
      `state: ${resolvePath(file)} (10 changes)`,
    ]);

    const state = readState(file);
    assert.equal(state.changes.length, 10);
    assert.deepEqual(state.changes.map((entry) => entry.id), [
      ...Array.from({ length: 7 }, (_, index) => `done-${index + 2}`),
      "open-0",
      "open-1",
      "open-2",
    ]);
    assert.notEqual(state.updatedAt, BASE_UPDATED_AT);
  });
});

test("pruneCompleted removes only completed entries, oldest first", () => {
  const input = [completed("a"), unfinished("b"), completed("c"), unfinished("d")];
  const { changes, removed } = pruneCompleted(input, 2);
  assert.deepEqual(removed.map((entry) => entry.id), ["a", "c"]);
  assert.deepEqual(changes.map((entry) => entry.id), ["b", "d"]);
  assert.deepEqual(input.map((entry) => entry.id), ["a", "b", "c", "d"]);

  const stuck = pruneCompleted([unfinished("x"), unfinished("y"), unfinished("z")], 1);
  assert.equal(stuck.changes.length, 3);
  assert.equal(stuck.removed.length, 0);
});

test("addChange_LimitBelowCompletedCount_PrunesThenAppendsAndWarns", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    writeStateRaw(file, stateOf([completed("done-old"), unfinished("open-1"), unfinished("open-2")]));

    const result = addChange(file, { id: "fresh", limit: 1 });
    const state = readState(file);

    assert.deepEqual(result.removed.map((entry) => entry.id), ["done-old"]);
    assert.equal(result.before, 3);
    assert.equal(result.after, 3);
    assert.deepEqual(state.changes.map((entry) => entry.id), ["open-1", "open-2", "fresh"]);
    assert.equal(result.warnings.length, 1);
    assert.match(result.warnings[0], /exceeds limit 1/);
  });
});

test("runAdd_LimitZero_RefusedWithExitOneAndFileUnchanged", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    writeStateRaw(file, stateOf(unfinishedEntries(2)));
    const before = readFileSync(file);

    const run = spawnSync(
      process.execPath,
      [SCRIPT, "add", "fresh", "--limit", "0", "--file", file],
      { encoding: "utf8" },
    );

    assert.equal(run.status, 1, run.stderr);
    assert.match(run.stderr, /--limit must be a positive integer/);
    assert.equal(run.stdout, "");
    assert.deepEqual(readFileSync(file), before);
  });
});

test("the completion rule matches the sidebar plugin", () => {
  const allDone = {
    id: "impl-done",
    stage: "impl",
    status: "done",
    commits: {},
    stages: {
      plan: { status: "done" },
      impl: { status: "done" },
      commit: { status: "done" },
      archive: { status: "done" },
    },
  };
  assert.equal(isCompleted(allDone), true);

  const archivePending = structuredClone(allDone);
  archivePending.stages.archive = { status: "pending" };
  assert.equal(isCompleted(archivePending), false);

  assert.equal(isCompleted({ id: "a", stage: "archive", status: "active" }), false);
  assert.equal(isCompleted({ id: "b", stage: "impl", status: "blocked" }), false);
  assert.equal(isCompleted({ id: "c", stage: "plan", status: "pending" }), false);

  // Derived rows: archive/done means every earlier stage is done too.
  assert.equal(isCompleted({ id: "d", stage: "archive", status: "done" }), true);
  assert.equal(stageState({ id: "d", stage: "impl", status: "active" }, "archive").status, "pending");
});

test("buildChange derives the stage rows and rejects invalid input", () => {
  const change = buildChange({
    id: "new-one",
    stage: "impl",
    status: "active",
    note: "hello",
    now: new Date("2026-10-22T12:00:00.000Z"),
  });
  assert.equal(change.updatedAt, "2026-10-22T12:00:00.000Z");
  assert.deepEqual(change.commits, { plan: null, impl: null, archive: null });
  assert.deepEqual(Object.keys(change.stages), ["plan", "impl", "commit", "archive"]);
  assert.equal(change.stages.plan.status, "done");
  assert.equal(change.stages.impl.status, "active");
  assert.equal(change.stages.commit.status, "pending");
  assert.equal(change.stages.archive.status, "pending");
  assert.equal(change.stages.impl.at, "2026-10-22T12:00:00.000Z");
  assert.equal(change.note, "hello");

  assert.throws(() => buildChange({ id: "x", stage: "bogus" }), /invalid stage/);
  assert.throws(() => buildChange({ id: "x", status: "bogus" }), /invalid status/);
  assert.throws(() => buildChange({ id: "  " }), /id is required/);
});

test("the written file has no BOM, one trailing newline, and keeps field order", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    const legacy = {
      id: "legacy",
      stage: "archive",
      status: "done",
      updatedAt: BASE_UPDATED_AT,
      commits: {},
      stages: {},
    };
    writeStateRaw(file, stateOf([legacy]));

    addChange(file, { id: "fresh", note: "hello" });

    const bytes = readFileSync(file);
    assert.notEqual(bytes[0], 0xef, "file must not start with a BOM");
    const text = bytes.toString("utf8");
    assert.ok(text.endsWith("\n"), "file must end with a newline");
    assert.ok(!text.endsWith("\n\n"), "file must end with exactly one newline");

    const parsed = JSON.parse(text);
    assert.deepEqual(Object.keys(parsed.changes[0]), [
      "id",
      "stage",
      "status",
      "updatedAt",
      "commits",
      "stages",
    ]);
    assert.equal(parsed.changes.at(-1).id, "fresh");
    assert.equal(parsed.changes.at(-1).note, "hello");

    const bomFile = join(dir, "bom.json");
    writeFileSync(bomFile, `\uFEFF${JSON.stringify(stateOf([unfinished("bom-entry")]))}`, "utf8");
    assert.equal(readState(bomFile).changes[0].id, "bom-entry");
  });
});

test("the CLI prints pruned, added, and state lines for one add", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    const changes = [completed("old"), ...unfinishedEntries(9)];
    writeStateRaw(file, stateOf(changes));

    const run = spawnSync(
      process.execPath,
      [SCRIPT, "add", "new-one", "--stage", "impl", "--status", "active", "--file", file],
      { encoding: "utf8" },
    );

    assert.equal(run.status, 0, run.stderr);
    assert.equal(run.stderr, "");
    assert.deepEqual(run.stdout.trimEnd().split(/\r?\n/), [
      "pruned old (archive/done)",
      "added new-one (10 -> 10)",
      `state: ${resolvePath(file)} (10 changes)`,
    ]);

    const added = readState(file).changes.at(-1);
    assert.equal(added.id, "new-one");
    assert.equal(added.stage, "impl");
    assert.equal(added.status, "active");
    assert.deepEqual(added.commits, { plan: null, impl: null, archive: null });
    assert.equal(added.stages.plan.status, "done");
    assert.equal(added.stages.impl.status, "active");
    assert.equal(added.stages.commit.status, "pending");
    assert.equal(added.stages.archive.status, "pending");
    assert.equal(added.note, undefined);
  });
});

test("a competing write between read and rename is re-applied on fresh content", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    writeStateRaw(file, stateOf([unfinished("start", "impl")]));

    const result = addChange(file, {
      id: "mine",
      beforeRename: ({ attempt }) => {
        if (attempt !== 1) {
          return;
        }
        const rival = readState(file);
        rival.changes.push(unfinished("rival", "plan"));
        rival.updatedAt = "2026-02-02T00:00:00.000Z";
        writeStateRaw(file, rival);
      },
    });

    assert.equal(result.attempts, 2);
    assert.equal(result.before, 2);
    assert.equal(result.after, 3);

    const state = readState(file);
    assert.deepEqual(state.changes.map((entry) => entry.id), ["start", "rival", "mine"]);
    assert.equal(state.changes.at(-1).id, "mine");
    assert.deepEqual(readdirSync(dir), ["pipeline.json"], "no temp file may be left behind");
  });
});

test("a persistent writer race fails after 3 attempts without writing", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    writeStateRaw(file, stateOf([unfinished("start", "impl")]));

    assert.throws(
      () =>
        addChange(file, {
          id: "mine",
          beforeRename: ({ attempt }) => {
            const rival = readState(file);
            rival.changes.push(unfinished(`rival-${attempt}`, "plan"));
            writeStateRaw(file, rival);
          },
        }),
      /kept changing during 3 attempts/,
    );

    const state = readState(file);
    assert.ok(!state.changes.some((entry) => entry.id === "mine"));
    assert.deepEqual(readdirSync(dir), ["pipeline.json"]);
  });
});

test("pruneChanges reads a missing file as an error", () => {
  withDir((dir) => {
    const file = join(dir, "missing.json");
    assert.throws(() => pruneChanges(file, { dryRun: true }), /state file not found/);
  });
});

test("update sets stage and status, refreshes both updatedAt values, and leaves other entries intact", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    const target = {
      id: "target",
      stage: "plan",
      status: "active",
      updatedAt: BASE_UPDATED_AT,
      commits: {},
      stages: {},
    };
    writeStateRaw(file, stateOf([unfinished("keep-1"), completed("keep-2"), target]));
    const untouchedBefore = JSON.stringify(readState(file).changes.slice(0, 2));

    const result = updateChange(file, { id: "target", stage: "impl", status: "active" });
    assert.equal(result.attempts, 1);
    assert.equal(result.change.id, "target");

    const state = readState(file);
    const updated = state.changes[2];
    assert.equal(updated.stage, "impl");
    assert.equal(updated.status, "active");
    assert.equal(
      updated.stages.plan.status,
      "done",
      "advancing the pointer backfills the passed stage",
    );
    assert.match(updated.stages.plan.at, /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/);
    assert.notEqual(updated.updatedAt, BASE_UPDATED_AT);
    assert.match(updated.updatedAt, /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/);
    assert.notEqual(state.updatedAt, BASE_UPDATED_AT);
    assert.match(state.updatedAt, /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/);
    assert.equal(
      JSON.stringify(state.changes.slice(0, 2)),
      untouchedBefore,
      "untouched entries must be byte-for-byte equivalent",
    );
    assert.deepEqual(Object.keys(updated), [
      "id",
      "stage",
      "status",
      "updatedAt",
      "commits",
      "stages",
    ]);
  });
});

test("update drives a partially finished entry to isCompleted", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    const partial = {
      id: "partial",
      stage: "impl",
      status: "active",
      updatedAt: BASE_UPDATED_AT,
      commits: {},
      stages: {
        plan: { status: "done", at: BASE_UPDATED_AT },
        impl: { status: "active", at: BASE_UPDATED_AT },
      },
    };
    writeStateRaw(file, stateOf([partial]));
    assert.equal(isCompleted(partial), false);

    updateChange(file, {
      id: "partial",
      status: "done",
      stageStatuses: { plan: "done", impl: "done", commit: "done", archive: "done" },
      commits: { archive: "abcd123" },
    });

    const updated = readState(file).changes[0];
    assert.equal(isCompleted(updated), true);
    assert.equal(updated.commits.archive, "abcd123");
    assert.equal(updated.stages.archive.status, "done");
    assert.ok(updated.stages.archive.at, "a created row carries a fresh at");
    assert.equal(updated.stages.archive.commit, "abcd123");
  });
});

test("update creates a missing stage row with a fresh at and never mutates the input entry", () => {
  const now = new Date("2026-10-23T12:34:56.000Z");
  const input = {
    id: "x",
    stage: "plan",
    status: "active",
    updatedAt: BASE_UPDATED_AT,
    commits: {},
    stages: { plan: { status: "done", at: BASE_UPDATED_AT } },
  };

  const updated = applyUpdate(input, { stageStatuses: { impl: "done" }, now });
  assert.deepEqual(updated.stages.impl, { status: "done", at: "2026-10-23T12:34:56.000Z" });
  assert.equal(updated.updatedAt, "2026-10-23T12:34:56.000Z");
  assert.deepEqual(
    input.stages,
    { plan: { status: "done", at: BASE_UPDATED_AT } },
    "the input entry is not mutated",
  );

  // Re-applying the same status keeps the recorded at.
  const same = applyUpdate(updated, {
    stageStatuses: { impl: "done" },
    now: new Date("2026-10-24T00:00:00.000Z"),
  });
  assert.equal(same.stages.impl.at, "2026-10-23T12:34:56.000Z");

  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    writeStateRaw(file, stateOf([structuredClone(input)]));
    updateChange(file, { id: "x", stageStatuses: { impl: "done" }, now });
    assert.deepEqual(readState(file).changes[0].stages.impl, {
      status: "done",
      at: "2026-10-23T12:34:56.000Z",
    });
  });
});

test("applyUpdate_AdvancePlanToArchive_BackfillsEarlierStagesToDone", () => {
  const now = new Date("2026-10-25T08:00:00.000Z");
  const fixture = buildChange({
    id: "jump",
    stage: "plan",
    status: "active",
    now: new Date(BASE_UPDATED_AT),
  });
  assert.equal(fixture.stages.plan.status, "active");
  assert.equal(fixture.stages.impl.status, "pending");
  assert.equal(fixture.stages.commit.status, "pending");

  const updated = applyUpdate(fixture, {
    stage: "archive",
    status: "done",
    stageStatuses: { archive: "done" },
    now,
  });

  for (const name of ["plan", "impl", "commit"]) {
    assert.equal(updated.stages[name].status, "done", `${name} must be backfilled to done`);
    assert.equal(updated.stages[name].at, "2026-10-25T08:00:00.000Z", `${name} gets a fresh at`);
  }
  assert.equal(updated.stages.archive.status, "done");
  assert.equal(updated.stage, "archive");
  assert.equal(isCompleted(updated), true);
});

test("applyUpdate_BackfilledEarlierRow_KeepsNoteCommitAndRecordedAt", () => {
  const input = {
    id: "keep",
    stage: "commit",
    status: "active",
    updatedAt: BASE_UPDATED_AT,
    commits: { impl: "abc1234" },
    stages: {
      plan: { status: "done", at: BASE_UPDATED_AT, note: "plan note" },
      impl: {
        status: "done",
        at: "2026-01-02T00:00:00.000Z",
        commit: "abc1234",
        note: "impl note",
      },
      commit: { status: "pending" },
    },
  };

  const updated = applyUpdate(input, {
    stage: "archive",
    stageStatuses: { archive: "done" },
    now: new Date("2026-10-25T08:00:00.000Z"),
  });

  assert.deepEqual(updated.stages.plan, {
    status: "done",
    at: BASE_UPDATED_AT,
    note: "plan note",
  });
  assert.deepEqual(updated.stages.impl, {
    status: "done",
    at: "2026-01-02T00:00:00.000Z",
    commit: "abc1234",
    note: "impl note",
  });
  assert.deepEqual(updated.stages.commit, {
    status: "done",
    at: "2026-10-25T08:00:00.000Z",
  });
});

test("applyUpdate_ExplicitEarlierStageStatus_WinsOverBackfill", () => {
  const fixture = buildChange({
    id: "blocked-impl",
    stage: "impl",
    status: "active",
    now: new Date(BASE_UPDATED_AT),
  });

  const updated = applyUpdate(fixture, {
    stage: "archive",
    stageStatuses: { impl: "blocked", archive: "done" },
    now: new Date("2026-10-25T08:00:00.000Z"),
  });

  assert.equal(updated.stages.impl.status, "blocked", "an explicit status is never backfilled");
  assert.equal(updated.stages.plan.status, "done");
  assert.equal(updated.stages.commit.status, "done");
  assert.equal(isCompleted(updated), false);
});

test("updateChange_AdvanceWithPartialStageStatuses_CompletedAndIdempotent", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    const now = new Date("2026-10-26T09:30:00.000Z");
    writeStateRaw(
      file,
      stateOf([
        buildChange({
          id: "jump",
          stage: "plan",
          status: "active",
          now: new Date(BASE_UPDATED_AT),
        }),
      ]),
    );

    const options = {
      id: "jump",
      stage: "archive",
      status: "done",
      stageStatuses: { archive: "done" },
      stageNotes: { archive: "archive=n/a: docs-only" },
      now,
    };

    const first = updateChange(file, options);
    assert.equal(isCompleted(first.change), true);
    const firstBytes = readFileSync(file);

    const second = updateChange(file, options);
    assert.equal(isCompleted(second.change), true);
    assert.deepEqual(
      readFileSync(file),
      firstBytes,
      "re-applying the same advance must not churn the file",
    );
  });
});

test("update with an unknown id exits 1 and leaves the file byte-identical", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    writeStateRaw(file, stateOf([unfinished("known")]));
    const before = readFileSync(file);

    const run = spawnSync(
      process.execPath,
      [SCRIPT, "update", "missing", "--status", "done", "--file", file],
      { encoding: "utf8" },
    );

    assert.equal(run.status, 1, run.stderr);
    assert.match(run.stderr, /error: change 'missing' not found in /);
    assert.equal(run.stdout, "");
    assert.deepEqual(readFileSync(file), before);
  });
});

test("update rejects an invalid stage or stage-status and writes nothing", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    writeStateRaw(file, stateOf([unfinished("known")]));
    const before = readFileSync(file);

    const badStage = spawnSync(
      process.execPath,
      [SCRIPT, "update", "known", "--stage", "bogus", "--file", file],
      { encoding: "utf8" },
    );
    assert.equal(badStage.status, 1, badStage.stderr);
    assert.match(badStage.stderr, /error: invalid stage 'bogus'/);
    assert.equal(badStage.stdout, "");
    assert.deepEqual(readFileSync(file), before);

    const badStageStatus = spawnSync(
      process.execPath,
      [SCRIPT, "update", "known", "--stage-status", "nope=done", "--file", file],
      { encoding: "utf8" },
    );
    assert.equal(badStageStatus.status, 1, badStageStatus.stderr);
    assert.match(badStageStatus.stderr, /error: invalid stage 'nope' in --stage-status/);
    assert.equal(badStageStatus.stdout, "");
    assert.deepEqual(readFileSync(file), before);

    assert.deepEqual(readdirSync(dir), ["pipeline.json"], "no temp file may be left behind");
  });
});

test("the CLI prints updated and state lines for one update", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    writeStateRaw(file, stateOf([unfinished("target", "impl"), unfinished("other")]));

    const run = spawnSync(
      process.execPath,
      [
        SCRIPT,
        "update",
        "target",
        "--stage",
        "commit",
        "--status",
        "active",
        "--note",
        "ready to commit",
        "--file",
        file,
      ],
      { encoding: "utf8" },
    );

    assert.equal(run.status, 0, run.stderr);
    assert.equal(run.stderr, "");
    assert.deepEqual(run.stdout.trimEnd().split(/\r?\n/), [
      `warning: no ground truth evidence for 'target'; if this is out-of-repo tooling work, add --note "tooling: <location>"`,
      "updated target (commit/active)",
      `state: ${resolvePath(file)} (2 changes)`,
    ]);

    const updated = readState(file).changes[0];
    assert.equal(updated.stage, "commit");
    assert.equal(updated.status, "active");
    assert.equal(updated.note, "ready to commit");
  });
});

test("the CLI records repeatable stage-status, stage-note, and commit pairs", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    writeStateRaw(file, stateOf([unfinished("target", "impl")]));

    const run = spawnSync(
      process.execPath,
      [
        SCRIPT,
        "update",
        "target",
        "--stage-status",
        "impl=active",
        "--stage-status",
        "commit=pending",
        "--stage-note",
        "impl=handed off",
        "--commit",
        "impl=abc1234",
        "--file",
        file,
      ],
      { encoding: "utf8" },
    );

    assert.equal(run.status, 0, run.stderr);
    assert.deepEqual(run.stdout.trimEnd().split(/\r?\n/), [
      "updated target (impl/active)",
      `state: ${resolvePath(file)} (1 changes)`,
    ]);

    const updated = readState(file).changes[0];
    assert.equal(updated.stages.impl.status, "active");
    assert.equal(updated.stages.impl.note, "handed off");
    assert.equal(updated.stages.impl.commit, "abc1234");
    assert.equal(updated.stages.commit.status, "pending");
    assert.equal(updated.commits.impl, "abc1234");
  });
});

/** Probes that report no evidence of any kind, without touching the filesystem or git. */
const NO_EVIDENCE_PROBES = {
  repoRoot: "C:/nowhere",
  archiveIds: () => new Set(),
  specDirExists: () => false,
  commitResolves: () => false,
};

/**
 * Probes whose `commitResolves` records every sha it is asked about, so a test
 * can assert how many answers `collectFindings` actually requested.
 */
function countingCommitProbes(resolves) {
  const calls = [];
  return {
    calls,
    probes: {
      repoRoot: "C:/repo",
      archiveIds: () => new Set(),
      specDirExists: () => false,
      commitResolves: (sha) => {
        calls.push(sha);
        return resolves(sha);
      },
    },
  };
}

/** Three changes that record the same three commit shas, plan/impl/archive. */
function changesSharingShas(shas) {
  return ["shared-0", "shared-1", "shared-2"].map((id) => ({
    ...completed(id),
    commits: { plan: shas[0], impl: shas[1], archive: shas[2] },
  }));
}

test("collectFindings_DoneEntryWithResolvableSha_OkWithCommitEvidence", () => {
  const entry = {
    ...completed("sha-entry"),
    commits: { plan: null, impl: "abc1234", archive: null },
  };
  const probes = {
    repoRoot: "C:/repo",
    archiveIds: () => new Set(),
    specDirExists: () => false,
    commitResolves: (sha) => sha === "abc1234",
  };

  const { entries, findings } = collectFindings([entry], probes);

  assert.equal(findings.length, 0);
  assert.equal(entries[0].level, "ok");
  assert.deepEqual(entries[0].evidence, { kind: "commit", detail: "abc1234" });
});

test("collectFindings_DoneEntryWithArchiveDir_OkWithArchiveEvidence", () => {
  const probes = {
    repoRoot: "C:/repo",
    archiveIds: () => new Set(["2026-01-01-archived-entry"]),
    specDirExists: () => false,
    commitResolves: () => false,
  };

  const { entries, findings } = collectFindings([completed("archived-entry")], probes);

  assert.equal(findings.length, 0);
  assert.equal(entries[0].level, "ok");
  assert.deepEqual(entries[0].evidence, { kind: "archive-dir" });
});

test("collectFindings_ActivePlanEntryNoEvidence_NoFinding", () => {
  const { entries, findings } = collectFindings(
    [{ id: "wip", stage: "plan", status: "active" }],
    NO_EVIDENCE_PROBES,
  );

  assert.equal(findings.length, 0);
  assert.equal(entries[0].level, "ok");
  assert.deepEqual(entries[0].evidence, { kind: "mid-flight" });
});

test("collectFindings_DoneEntryNoEvidence_WarningNoEvidence", () => {
  const { entries, findings } = collectFindings([completed("orphan")], NO_EVIDENCE_PROBES);

  assert.equal(findings.length, 1);
  assert.equal(findings[0].id, "orphan");
  assert.equal(findings[0].level, "warning");
  assert.equal(findings[0].kind, "no-evidence");
  assert.equal(entries[0].level, "warning");
});

test("collectFindings_MissingSha_WarningShaMissing", () => {
  const entry = {
    ...completed("stale"),
    commits: { plan: null, impl: "deadbeef", archive: null },
  };
  const probes = { ...NO_EVIDENCE_PROBES, repoRoot: "C:/repo" };

  const { findings } = collectFindings([entry], probes);

  assert.equal(findings.length, 1, "the recorded sha suppresses no-evidence");
  assert.equal(findings[0].kind, "sha-missing");
  assert.equal(findings[0].detail, "deadbeef");
  assert.equal(findings[0].repoRoot, "C:/repo");
});

test("collectFindings_ToolingNote_OkAttestation", () => {
  const entry = {
    ...completed("tooling-work"),
    note: "  Tooling: C:/Users/Maxim/.config/opencode/plugins/x",
  };

  const { entries, findings } = collectFindings([entry], NO_EVIDENCE_PROBES);

  assert.equal(findings.length, 0);
  assert.deepEqual(entries[0].evidence, { kind: "attestation" });
});

test("collectFindings_ArchivePointerWithPendingCommitRow_WarningInconsistentStages", () => {
  const entry = {
    id: "stuck",
    stage: "archive",
    status: "done",
    updatedAt: BASE_UPDATED_AT,
    commits: {},
    stages: {
      plan: { status: "done" },
      impl: { status: "done" },
      commit: { status: "pending" },
      archive: { status: "done" },
    },
  };

  const { entries, findings } = collectFindings([entry], NO_EVIDENCE_PROBES);

  assert.equal(findings.length, 1, "the new finding suppresses no-evidence");
  assert.equal(findings[0].kind, "inconsistent-stages");
  assert.equal(findings[0].id, "stuck");
  assert.equal(findings[0].stage, "archive");
  assert.equal(findings[0].detail, "commit");
  assert.equal(entries[0].level, "warning");
});

test("collectFindings_DoneWithoutOpenSpecLifecycle_WarningNoArchivePath", () => {
  const entry = { id: "docs-only", stage: "commit", status: "done" };

  const { findings } = collectFindings([entry], NO_EVIDENCE_PROBES);

  assert.equal(findings.length, 1);
  assert.equal(findings[0].kind, "no-archive-path");
  assert.equal(findings[0].detail, "archive");

  const withSpec = collectFindings([entry], { ...NO_EVIDENCE_PROBES, specDirExists: () => true });
  assert.equal(withSpec.findings.length, 0, "an OpenSpec directory is an archive path");

  const withoutProbe = collectFindings([entry], undefined);
  assert.deepEqual(
    withoutProbe.findings.map((finding) => finding.kind),
    ["no-evidence"],
    "the rule is skipped when the probes cannot answer",
  );
});

test("collectFindings_ThreeChangesSharingThreeShas_ResolvesEachShaOnce", () => {
  const shas = ["aaaa111", "bbbb222", "cccc333"];
  const { calls, probes } = countingCommitProbes(() => true);

  const { entries, findings } = collectFindings(changesSharingShas(shas), probes);

  assert.deepEqual(calls, shas, "one probe call per unique sha, not one per change and loop");
  assert.deepEqual(findings, []);
  for (const entry of entries) {
    assert.equal(entry.level, "ok");
    assert.deepEqual(entry.evidence, { kind: "commit", detail: shas[0] });
    assert.deepEqual(entry.findings, []);
  }
});

test("collectFindings_ThreeUnresolvableShas_ResolvesEachShaOnceAndKeepsAllFindings", () => {
  const shas = ["dead111", "dead222", "dead333"];
  const { calls, probes } = countingCommitProbes(() => false);

  const { entries, findings } = collectFindings(changesSharingShas(shas), probes);

  assert.deepEqual(calls, shas, "a cached false answer still costs one probe call per unique sha");
  assert.equal(findings.length, 9, "every change keeps its own three sha-missing findings");
  for (const entry of entries) {
    assert.equal(entry.evidence, null);
    assert.deepEqual(
      entry.findings.map((finding) => finding.kind),
      ["sha-missing", "sha-missing", "sha-missing"],
    );
    assert.deepEqual(entry.findings.map((finding) => finding.detail), shas);
  }
});

test("the CLI check renders the inconsistent-stages and no-archive-path lines", () => {
  withDir((dir) => {
    const repo = join(dir, "repo");
    const file = join(repo, ".opencode", "pipeline.json");
    mkdirSync(join(repo, ".opencode"), { recursive: true });
    writeStateRaw(
      file,
      stateOf([
        {
          id: "stuck",
          stage: "archive",
          status: "done",
          updatedAt: BASE_UPDATED_AT,
          commits: {},
          stages: { commit: { status: "pending" } },
        },
        { id: "docs-only", stage: "commit", status: "done", updatedAt: BASE_UPDATED_AT, commits: {} },
      ]),
    );

    const run = spawnSync(process.execPath, [SCRIPT, "check", "--file", file], { encoding: "utf8" });

    assert.equal(run.status, 0, run.stderr);
    assert.deepEqual(run.stdout.trimEnd().split(/\r?\n/), [
      "warning: stuck stage archive with unfinished earlier stage(s): commit",
      'warning: docs-only done without an archive path; close it with --stage archive --status done --stage-status archive=done --stage-note "archive=n/a: <reason>"',
      "checked 2 changes: 0 ok, 2 warning(s)",
      `state: ${resolvePath(file)} (2 changes)`,
    ]);
  });
});

test("the CLI check reports evidence, mid-flight, and warnings without writing", () => {
  withDir((dir) => {
    const repo = join(dir, "repo");
    const file = join(repo, ".opencode", "pipeline.json");
    mkdirSync(join(repo, ".opencode"), { recursive: true });
    mkdirSync(join(repo, "openspec", "changes", "archive", "2026-01-01-archived-one"), {
      recursive: true,
    });
    mkdirSync(join(repo, "openspec", "changes", "spec-one"), { recursive: true });
    writeStateRaw(
      file,
      stateOf([
        completed("archived-one"),
        completed("spec-one"),
        completed("orphan"),
        unfinished("wip", "plan"),
      ]),
    );
    const before = readFileSync(file);

    const run = spawnSync(process.execPath, [SCRIPT, "check", "--file", file], { encoding: "utf8" });

    assert.equal(run.status, 0, run.stderr);
    assert.equal(run.stderr, "");
    assert.deepEqual(run.stdout.trimEnd().split(/\r?\n/), [
      "ok archived-one (archive-dir)",
      "ok spec-one (spec-dir)",
      "warning: orphan no evidence (commit sha, openspec dir, archive dir, or note 'tooling: ...')",
      "ok wip (mid-flight)",
      "checked 4 changes: 3 ok, 1 warning(s)",
      `state: ${resolvePath(file)} (4 changes)`,
    ]);
    assert.deepEqual(readFileSync(file), before, "check must not write the state file");

    const strict = spawnSync(process.execPath, [SCRIPT, "check", "--file", file, "--strict"], {
      encoding: "utf8",
    });
    assert.equal(strict.status, 1, strict.stderr);
    assert.equal(strict.stderr, "", "strict exits 1 without an error message");
    assert.deepEqual(readFileSync(file), before);
  });
});

test("the CLI check exits 1 on a malformed state file", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    writeFileSync(file, "{ not json", "utf8");

    const run = spawnSync(process.execPath, [SCRIPT, "check", "--file", file], { encoding: "utf8" });

    assert.equal(run.status, 1);
    assert.match(run.stderr, /error: malformed state file /);
    assert.equal(run.stdout, "");
  });
});

test("updateChange_DoneWithNoEvidence_AppendsGroundTruthWarning", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    writeStateRaw(file, stateOf([unfinished("tooling-entry", "impl")]));

    const probed = updateChange(file, {
      id: "tooling-entry",
      status: "done",
      evidenceProbe: NO_EVIDENCE_PROBES,
    });

    assert.equal(probed.warnings.length, 1);
    assert.match(probed.warnings[0], /no ground truth evidence for 'tooling-entry'/);
    assert.match(probed.warnings[0], /--note "tooling: <location>"/);
    assert.equal(readState(file).changes[0].status, "done");

    const backCompat = updateChange(file, { id: "tooling-entry", note: "no probe" });
    assert.deepEqual(backCompat.warnings, [], "no probe keeps the previous behavior");
  });
});

test("updateChange_AttestedToolingNote_SkipsGroundTruthWarning", () => {
  withDir((dir) => {
    const file = join(dir, "pipeline.json");
    writeStateRaw(file, stateOf([unfinished("tooling-entry", "impl")]));

    const result = updateChange(file, {
      id: "tooling-entry",
      status: "done",
      note: "tooling: C:/Users/Maxim/.config/opencode/plugins/x",
      evidenceProbe: NO_EVIDENCE_PROBES,
    });

    assert.deepEqual(result.warnings, []);
  });
});

test("evidenceProbes_RealRepoLayout_ReportsArchiveAndSpecDirs", () => {
  withDir((repo) => {
    mkdirSync(join(repo, "openspec", "changes", "archive", "2026-01-01-one"), { recursive: true });
    mkdirSync(join(repo, "openspec", "changes", "two"), { recursive: true });
    const probes = evidenceProbes(repo);

    assert.deepEqual([...probes.archiveIds()], ["2026-01-01-one"]);
    assert.equal(probes.specDirExists("two"), true);
    assert.equal(probes.specDirExists("missing"), false);
    assert.equal(probes.commitResolves("deadbeef"), false, "an unknown sha is not evidence");
  });
});

console.log("\npipeline-state: all assertions passed");
