// Tests for the pipeline state writer. Run with:
//   node .opencode/scripts/pipeline-state.test.mjs
//
// Exercises the 10-entry cap, the completed-only pruning rule, the atomic
// file shape, the duplicate refusal, and the compare-and-swap retry.

import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import {
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

console.log("\npipeline-state: all assertions passed");
