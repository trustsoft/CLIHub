// Single writer for the pipeline state file (.opencode/pipeline.json).
//
//   node .opencode/scripts/pipeline-state.mjs add <id> [--stage plan] [--status active]
//                                              [--file <path>] [--limit <n>] [--note "..."]
//   node .opencode/scripts/pipeline-state.mjs update <id> [--stage plan|impl|commit|archive]
//                                              [--status pending|active|done|blocked]
//                                              [--note "..."] [--stage-status <stage>=<status>]
//                                              [--stage-note <stage>=<text>]
//                                              [--commit <stage>=<sha>] [--file <path>]
//   node .opencode/scripts/pipeline-state.mjs prune [--file <path>] [--limit <n>] [--dry-run]
//
// The sidebar plugin (plugins/pipeline-status) only reads this file; every
// mutation goes through this script so that the 10-entry cap and the atomic
// write are enforced in one place.
//
// `add` prunes the oldest fully completed entries until the file is below the
// limit, then appends the new entry last. "Fully completed" is the plugin's
// rule: the change's own `status` is `done` and all four stage rows resolve to
// `done`. Unfinished work is never removed; when nothing is prunable the entry
// is appended anyway with a warning and the read-only sidebar window keeps the
// display at the limit.
//
// `update` applies a state transition to one existing entry: the stage pointer,
// the change status, the entry note, per-stage status/note rows, and per-stage
// commit shas. It never prunes. Unknown ids and invalid stage/status values are
// rejected before anything is written.
//
// `prune` removes the oldest completed entries until the file is at or below
// the limit; `--dry-run` reports the removals without writing.
//
// Writes are atomic (temp file in the same directory, then rename) and
// UTF-8 without BOM. Before the rename the target is re-statted: if another
// writer changed its mtime/size since the read, the state is re-read, the
// operation is re-applied, and the write is retried (compare-and-swap). After
// three failed attempts the script exits 1 without writing.
//
// Output (one line each, greppable):
//   pruned <id> (<stage>/<status>)
//   warning: ...
//   added <id> (<before> -> <after>)
//   updated <id> (<stage>/<status>)
//   state: <absolute path> (<n> changes)
//
// Exit codes: 0 success, 1 usage/IO/malformed-state error, 2 refused (duplicate id).

import { readFileSync, renameSync, rmSync, statSync, writeFileSync } from "node:fs";
import { basename, dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const SCRIPT_DIR = dirname(fileURLToPath(import.meta.url));
const DEFAULT_FILE = join(SCRIPT_DIR, "..", "pipeline.json");

/** Maximum number of entries the state file aims to hold. */
export const LIMIT = 10;

/** Pipeline stages in order; the stage pointer's position decides the fallback rows. */
export const STAGES = Object.freeze(["plan", "impl", "commit", "archive"]);

/** Allowed change and stage statuses. */
export const STATUSES = Object.freeze(["pending", "active", "done", "blocked"]);

// Windows can transiently hold a handle on the target file, which surfaces as
// EPERM/EBUSY on the rename. These errors are worth retrying; everything else
// is a real failure.
const TRANSIENT_FILE_ERRORS = new Set(["EPERM", "EBUSY", "EACCES"]);

let tempCounter = 0;

/** A duplicate change id; the CLI maps this to exit code 2. */
export class RefusalError extends Error {
  constructor(message) {
    super(message);
    this.name = "RefusalError";
  }
}

/** A usage, IO, or malformed-state error; the CLI maps this to exit code 1. */
export class StateError extends Error {
  constructor(message) {
    super(message);
    this.name = "StateError";
  }
}

/** Blocks the current thread for `ms` milliseconds (the script is synchronous). */
function sleep(ms) {
  Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, ms);
}

/**
 * Resolves the stage row for `stage`, using the same derivation as the sidebar
 * plugin: an explicit row wins; otherwise earlier stages are `done`, the
 * current stage uses the change `status`, and later stages are `pending`.
 */
export function stageState(change, stage) {
  const explicit = change?.stages?.[stage];
  if (explicit) {
    return explicit;
  }

  const currentIndex = STAGES.indexOf(change?.stage);
  const stageIndex = STAGES.indexOf(stage);
  const status = stageIndex < currentIndex ? "done" : stageIndex === currentIndex ? change?.status : "pending";

  return {
    status,
    at: stage === change?.stage ? change?.updatedAt : undefined,
    commit: change?.commits?.[stage],
  };
}

/**
 * True when a change is fully completed — nothing left to do. Both conditions
 * must hold: the change's own `status` is `done`, and all four stage rows
 * resolve to `done`. The stage pointer alone is not trusted, so legacy entries
 * parked at `impl` with all rows `done` are still removable.
 */
export function isCompleted(change) {
  if (!change || change.status !== "done") {
    return false;
  }
  return STAGES.every((stage) => stageState(change, stage).status === "done");
}

/**
 * Removes the oldest extracted completed entries until at most `limit` remain.
 * "Oldest" is the earliest array index, the order the orchestrator appends in.
 * Returns a new array plus the removed entries in removal order; the input is
 * never mutated.
 */
export function pruneCompleted(changes, limit = LIMIT) {
  if (!Number.isInteger(limit) || limit < 0) {
    throw new StateError(`limit must be a non-negative integer, got '${limit}'`);
  }

  const kept = changes.slice();
  const removed = [];
  while (kept.length > limit) {
    const index = kept.findIndex(isCompleted);
    if (index < 0) {
      break;
    }
    removed.push(kept.splice(index, 1)[0]);
  }

  return { changes: kept, removed };
}

/**
 * Builds a new change entry. The current stage carries `status` (default
 * `active`); earlier stages are `done`, later stages `pending`, mirroring
 * `stageState`. `commits` starts empty for every stage the file records.
 */
export function buildChange({ id, stage = "plan", status = "active", note, now = new Date() } = {}) {
  if (typeof id !== "string" || id.trim() === "") {
    throw new StateError("change id is required");
  }
  if (!STAGES.includes(stage)) {
    throw new StateError(`invalid stage '${stage}' (expected ${STAGES.join("|")})`);
  }
  if (!STATUSES.includes(status)) {
    throw new StateError(`invalid status '${status}' (expected ${STATUSES.join("|")})`);
  }

  const at = now.toISOString();
  const currentIndex = STAGES.indexOf(stage);
  const stages = {};
  for (const name of STAGES) {
    const index = STAGES.indexOf(name);
    const derived = index < currentIndex ? "done" : index === currentIndex ? status : "pending";
    const row = { status: derived };
    if (name === stage) {
      row.at = at;
    }
    row.commit = null;
    stages[name] = row;
  }

  const change = {
    id,
    stage,
    status,
    updatedAt: at,
    commits: { plan: null, impl: null, archive: null },
    stages,
  };
  if (note !== undefined) {
    change.note = note;
  }
  return change;
}

function emptyState(file) {
  const dir = dirname(resolve(file));
  const project = basename(dir) === ".opencode" ? basename(dirname(dir)) : "default";
  return {
    schemaVersion: 1,
    project,
    updatedAt: new Date().toISOString(),
    changes: [],
  };
}

/** Reads and validates the state file. A leading BOM is tolerated. */
export function readState(file) {
  const target = resolve(file);
  const text = readFileSync(target, "utf8").replace(/^\uFEFF/, "");

  let parsed;
  try {
    parsed = JSON.parse(text);
  } catch (error) {
    throw new StateError(`malformed state file ${target}: ${error?.message ?? error}`);
  }
  if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
    throw new StateError(`invalid state file ${target}: root is not an object`);
  }
  if (!Array.isArray(parsed.changes)) {
    throw new StateError(`invalid state file ${target}: changes is not an array`);
  }
  return parsed;
}

/**
 * Writes `state` as UTF-8 without BOM, 2-space indented, with a single
 * trailing newline, through a temp file in the same directory and a rename.
 * The rename is retried on transient Windows sharing errors; the temp file is
 * removed when the write ultimately fails.
 */
export function writeStateAtomic(file, state, { retries = 5, delayMs = 50 } = {}) {
  const target = resolve(file);
  const tmp = `${target}.tmp-${process.pid}-${tempCounter++}`;
  try {
    writeFileSync(tmp, `${JSON.stringify(state, null, 2)}\n`, { encoding: "utf8" });

    let lastError;
    for (let attempt = 0; attempt <= retries; attempt += 1) {
      try {
        renameSync(tmp, target);
        return;
      } catch (error) {
        lastError = error;
        if (!TRANSIENT_FILE_ERRORS.has(error?.code) || attempt === retries) {
          break;
        }
        sleep(delayMs * (attempt + 1));
      }
    }
    throw lastError;
  } catch (error) {
    try {
      rmSync(tmp, { force: true });
    } catch {
      // The temp file is best-effort cleanup; the original error is the one that matters.
    }
    throw error;
  }
}

function statOrNull(file) {
  try {
    const stat = statSync(file);
    return { mtimeMs: stat.mtimeMs, size: stat.size };
  } catch (error) {
    if (error?.code === "ENOENT") {
      return null;
    }
    throw error;
  }
}

function sameStat(left, right) {
  if (left === null || right === null) {
    return left === right;
  }
  return left.mtimeMs === right.mtimeMs && left.size === right.size;
}

/**
 * Compare-and-swap wrapper around a read-modify-write of the state file.
 *
 * `operation(state, context)` receives a fresh state object and returns the
 * outcome to report plus the `state` to write. After the operation the target
 * is re-statted; when another writer changed it since the read, the operation
 * is re-applied to the fresh content. `beforeRename` is invoked after the
 * operation and before the stat check, so tests can simulate a competing
 * writer. Throws after `attempts` lost races without writing anything.
 */
export function updateState(file, operation, { attempts = 3, beforeRename } = {}) {
  const target = resolve(file);

  for (let attempt = 1; attempt <= attempts; attempt += 1) {
    const before = statOrNull(target);
    const state = before === null ? emptyState(target) : readState(target);
    const outcome = operation(state, { attempt, file: target });

    if (beforeRename) {
      beforeRename({ attempt, file: target, state: outcome.state ?? state });
    }

    const after = statOrNull(target);
    if (!sameStat(before, after)) {
      continue;
    }

    writeStateAtomic(target, outcome.state ?? state);
    return { ...outcome, attempts: attempt };
  }

  throw new StateError(
    `state file kept changing during ${attempts} attempts (${target}); nothing was written`,
  );
}

/**
 * Appends `id` and prunes the oldest completed entries first. Throws
 * `RefusalError` (before any write) when the id already exists. Returns
 * `{ removed, before, after, change, state, warnings, attempts }`.
 */
export function addChange(
  file,
  { id, stage = "plan", status = "active", note, limit = LIMIT, now = new Date(), beforeRename } = {},
) {
  const change = buildChange({ id, stage, status, note, now });

  return updateState(
    file,
    (state) => {
      if (state.changes.some((entry) => entry?.id === id)) {
        throw new RefusalError(`change '${id}' already exists in ${resolve(file)}`);
      }

      const before = state.changes.length;
      const kept = state.changes.slice();
      const removed = [];
      while (kept.length >= limit) {
        const index = kept.findIndex(isCompleted);
        if (index < 0) {
          break;
        }
        removed.push(kept.splice(index, 1)[0]);
      }

      kept.push(change);
      state.changes = kept;
      state.updatedAt = now.toISOString();

      const warnings = [];
      if (kept.length > limit) {
        warnings.push(
          `change count ${kept.length} exceeds limit ${limit}: no completed entry left to prune`,
        );
      }

      return { state, change, removed, before, after: kept.length, warnings };
    },
    { beforeRename },
  );
}

/**
 * Removes the oldest completed entries until at most `limit` remain. With
 * `dryRun` the file is read and reported but never written. Returns
 * `{ removed, before, after, state, attempts }`.
 */
export function pruneChanges(
  file,
  { limit = LIMIT, dryRun = false, now = new Date(), beforeRename } = {},
) {
  const target = resolve(file);

  const apply = (state) => {
    const before = state.changes.length;
    const { changes, removed } = pruneCompleted(state.changes, limit);
    if (!dryRun) {
      state.changes = changes;
      state.updatedAt = now.toISOString();
    }
    return { state, removed, before, after: changes.length };
  };

  if (dryRun) {
    if (statOrNull(target) === null) {
      throw new StateError(`state file not found: ${target}`);
    }
    return { ...apply(readState(target)), attempts: 0 };
  }

  return updateState(target, apply, { beforeRename });
}

/**
 * Validates the option payload of an update. Throws `StateError` for an
 * unknown stage or status, including the stage names used as keys by the
 * per-stage options, and for an empty commit sha.
 */
function validateUpdateInput({ stage, status, stageStatuses = {}, stageNotes = {}, commits = {} }) {
  if (stage !== undefined && !STAGES.includes(stage)) {
    throw new StateError(`invalid stage '${stage}' (expected ${STAGES.join("|")})`);
  }
  if (status !== undefined && !STATUSES.includes(status)) {
    throw new StateError(`invalid status '${status}' (expected ${STATUSES.join("|")})`);
  }
  for (const [name, value] of Object.entries(stageStatuses)) {
    if (!STAGES.includes(name)) {
      throw new StateError(`invalid stage '${name}' in --stage-status (expected ${STAGES.join("|")})`);
    }
    if (!STATUSES.includes(value)) {
      throw new StateError(
        `invalid status '${value}' for stage '${name}' (expected ${STATUSES.join("|")})`,
      );
    }
  }
  for (const name of Object.keys(stageNotes)) {
    if (!STAGES.includes(name)) {
      throw new StateError(`invalid stage '${name}' in --stage-note (expected ${STAGES.join("|")})`);
    }
  }
  for (const [name, value] of Object.entries(commits)) {
    if (!STAGES.includes(name)) {
      throw new StateError(`invalid stage '${name}' in --commit (expected ${STAGES.join("|")})`);
    }
    if (typeof value !== "string" || value.trim() === "") {
      throw new StateError(`--commit ${name} requires a non-empty sha`);
    }
  }
}

/**
 * Applies one update to a single change entry and returns the updated entry;
 * the input is never mutated. Existing keys keep their position, missing
 * `commits`/`stages` containers and touched stage rows are created on demand.
 *
 * `stageStatuses` sets a stage row's `status` and refreshes its `at` when the
 * row had none or the status actually changed; `stageNotes` sets a row's
 * `note`; `commits` records a sha under `commits` and mirrors it into the
 * stage row's `commit`. The entry's `updatedAt` always becomes `now`.
 */
export function applyUpdate(
  change,
  {
    stage,
    status,
    note,
    stageStatuses = {},
    stageNotes = {},
    commits = {},
    now = new Date(),
  } = {},
) {
  if (!change || typeof change !== "object" || Array.isArray(change)) {
    throw new StateError("change entry is not an object");
  }
  validateUpdateInput({ stage, status, stageStatuses, stageNotes, commits });

  const at = now.toISOString();
  const next = { ...change };

  if (stage !== undefined) {
    next.stage = stage;
  }
  if (status !== undefined) {
    next.status = status;
  }
  next.updatedAt = at;
  if (note !== undefined) {
    next.note = note;
  }

  const touched = STAGES.filter(
    (name) =>
      stageStatuses[name] !== undefined ||
      stageNotes[name] !== undefined ||
      commits[name] !== undefined,
  );
  if (touched.length === 0) {
    return next;
  }

  const stages = change.stages && typeof change.stages === "object" ? { ...change.stages } : {};
  let commitMap =
    change.commits && typeof change.commits === "object" ? { ...change.commits } : undefined;

  for (const name of touched) {
    const row = { ...(stages[name] ?? {}) };

    if (stageStatuses[name] !== undefined) {
      const previousStatus = row.status;
      row.status = stageStatuses[name];
      if (row.at === undefined || previousStatus !== row.status) {
        row.at = at;
      }
    }
    if (commits[name] !== undefined) {
      const sha = commits[name].trim();
      row.commit = sha;
      commitMap = commitMap ?? {};
      commitMap[name] = sha;
    }
    if (stageNotes[name] !== undefined) {
      row.note = stageNotes[name];
    }

    stages[name] = row;
  }

  if (commitMap !== undefined) {
    next.commits = commitMap;
  }
  next.stages = stages;
  return next;
}

/**
 * Applies an update to the entry with `id` through the compare-and-swap
 * driver. Throws `StateError` (before any write) when the id is unknown or the
 * options are invalid. Returns `{ change, attempts, state }`.
 */
export function updateChange(
  file,
  {
    id,
    stage,
    status,
    note,
    stageStatuses = {},
    stageNotes = {},
    commits = {},
    now = new Date(),
    beforeRename,
  } = {},
) {
  if (typeof id !== "string" || id.trim() === "") {
    throw new StateError("change id is required");
  }
  validateUpdateInput({ stage, status, stageStatuses, stageNotes, commits });

  const at = now.toISOString();
  const target = resolve(file);

  return updateState(
    target,
    (state) => {
      const index = state.changes.findIndex((entry) => entry?.id === id);
      if (index < 0) {
        throw new StateError(`change '${id}' not found in ${target}`);
      }

      const change = applyUpdate(state.changes[index], {
        stage,
        status,
        note,
        stageStatuses,
        stageNotes,
        commits,
        now,
      });

      const changes = state.changes.slice();
      changes[index] = change;
      state.changes = changes;
      state.updatedAt = at;

      return { state, change };
    },
    { beforeRename },
  );
}

/** Reads the value of a `--flag value` argument, or undefined when the flag is absent. */
function flagValue(argv, flag) {
  const index = argv.indexOf(flag);
  return index >= 0 && index + 1 < argv.length ? argv[index + 1] : undefined;
}

function parseLimit(raw) {
  if (raw === undefined) {
    return LIMIT;
  }
  const limit = Number(raw);
  if (!Number.isInteger(limit) || limit < 1) {
    throw new StateError(`--limit must be a positive integer, got '${raw}'`);
  }
  return limit;
}

/**
 * Collects every `<key>=<value>` pair following a repeatable flag (`--stage-status`,
 * `--stage-note`, `--commit`). A repeated key keeps the last value.
 */
function parsePairs(argv, flag) {
  const pairs = {};
  for (let index = 0; index < argv.length; index += 1) {
    if (argv[index] !== flag) {
      continue;
    }
    const raw = argv[index + 1];
    if (raw === undefined || raw.startsWith("--")) {
      throw new StateError(`${flag} requires <stage>=<value>`);
    }
    const separator = raw.indexOf("=");
    if (separator <= 0) {
      throw new StateError(`${flag} expects <stage>=<value>, got '${raw}'`);
    }
    pairs[raw.slice(0, separator)] = raw.slice(separator + 1);
    index += 1;
  }
  return pairs;
}

function statePath(argv) {
  const flag = flagValue(argv, "--file");
  if (flag) {
    return resolve(flag);
  }
  const env = process.env.OPENCODE_PIPELINE_FILE?.trim();
  return env ? resolve(env) : resolve(DEFAULT_FILE);
}

function formatRemoved(entry) {
  const stage = typeof entry?.stage === "string" ? entry.stage : "?";
  const status = typeof entry?.status === "string" ? entry.status : "?";
  return `pruned ${entry?.id ?? "?"} (${stage}/${status})`;
}

function formatUpdated(entry) {
  const stage = typeof entry?.stage === "string" ? entry.stage : "?";
  const status = typeof entry?.status === "string" ? entry.status : "?";
  return `updated ${entry?.id ?? "?"} (${stage}/${status})`;
}

function stateLine(file, count) {
  return `state: ${file} (${count} changes)`;
}

const USAGE =
  "usage: pipeline-state.mjs add <id> [--stage plan|impl|commit|archive] [--status pending|active|done|blocked] " +
  "[--file <path>] [--limit <n>] [--note \"...\"] | " +
  "update <id> [--stage plan|impl|commit|archive] [--status pending|active|done|blocked] [--note \"...\"] " +
  "[--stage-status <stage>=<status>] [--stage-note <stage>=<text>] [--commit <stage>=<sha>] [--file <path>] | " +
  "prune [--file <path>] [--limit <n>] [--dry-run]";

function runAdd(argv) {
  const id = argv[1];
  if (!id || id.startsWith("--")) {
    throw new StateError(USAGE);
  }

  const file = statePath(argv);
  const stage = flagValue(argv, "--stage") ?? "plan";
  const status = flagValue(argv, "--status") ?? "active";
  const note = flagValue(argv, "--note");
  const limit = parseLimit(flagValue(argv, "--limit"));

  const result = addChange(file, { id, stage, status, note, limit });
  for (const entry of result.removed) {
    console.log(formatRemoved(entry));
  }
  for (const warning of result.warnings) {
    console.log(`warning: ${warning}`);
  }
  console.log(`added ${id} (${result.before} -> ${result.after})`);
  console.log(stateLine(file, result.after));
}

function runUpdate(argv) {
  const id = argv[1];
  if (!id || id.startsWith("--")) {
    throw new StateError(USAGE);
  }

  const file = statePath(argv);
  const stage = flagValue(argv, "--stage");
  const status = flagValue(argv, "--status");
  const note = flagValue(argv, "--note");
  const stageStatuses = parsePairs(argv, "--stage-status");
  const stageNotes = parsePairs(argv, "--stage-note");
  const commits = parsePairs(argv, "--commit");

  const result = updateChange(file, { id, stage, status, note, stageStatuses, stageNotes, commits });
  console.log(formatUpdated(result.change));
  console.log(stateLine(file, result.state.changes.length));
}

function runPrune(argv) {
  const file = statePath(argv);
  const limit = parseLimit(flagValue(argv, "--limit"));
  const dryRun = argv.includes("--dry-run");

  const result = pruneChanges(file, { limit, dryRun });
  for (const entry of result.removed) {
    console.log(formatRemoved(entry));
  }
  console.log(stateLine(file, result.after));
}

function main() {
  const argv = process.argv.slice(2);
  const command = argv[0];

  try {
    if (command === "add") {
      runAdd(argv);
    } else if (command === "update") {
      runUpdate(argv);
    } else if (command === "prune") {
      runPrune(argv);
    } else {
      throw new StateError(USAGE);
    }
  } catch (error) {
    console.error(`error: ${error?.message ?? error}`);
    // Use exitCode rather than process.exit so pending stdout is flushed.
    process.exitCode = error instanceof RefusalError ? 2 : 1;
  }
}

const isMain = process.argv[1] && resolve(process.argv[1]) === resolve(fileURLToPath(import.meta.url));
if (isMain) {
  main();
}
