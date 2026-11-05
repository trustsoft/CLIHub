// Shared JSON IO helpers for the deployed project scripts.
//
//   pipeline-state.mjs  uses `flagValue` and `writeJsonAtomic` (5 retries / 50 ms
//                       through its own `writeStateAtomic` wrapper).
//   resolve-model.mjs   uses `flagValue`, `readJsonWithRetry` and
//                       `writeJsonAtomic` (module defaults, 8 / 100).
//
// A sibling module in this directory is deployed verbatim by
// install-project.ps1 and keeps the scripts' relative imports unchanged.
//
// Writes are atomic: the value is written to a unique temp file in the target's
// own directory and renamed onto the target, so a reader never observes a
// partial file. The rename is retried on the transient Windows sharing errors;
// any failure removes the temp file and rethrows the original error. A missing
// directory is never created — a write into one fails, exactly as a plain
// `writeFileSync` would.

import { readFileSync, renameSync, rmSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

/** Windows can transiently hold a handle on the target file; these are worth retrying. */
export const TRANSIENT_FILE_ERRORS = new Set(["EPERM", "EBUSY", "EACCES"]);

// Keeps two writers in one process from choosing the same temp file.
let tempCounter = 0;

/** Blocks the current thread for `ms` milliseconds (the scripts are synchronous). */
export function sleep(ms) {
  Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, ms);
}

/** Reads the value of a `--flag value` argument, or undefined when the flag is absent. */
export function flagValue(argv, flag) {
  const index = argv.indexOf(flag);
  return index >= 0 && index + 1 < argv.length ? argv[index + 1] : undefined;
}

/**
 * Writes `value` as UTF-8 JSON, 2-space indented, with a single trailing
 * newline, through a unique temp file in the target's directory and a rename.
 * The target path is resolved first, so the temp file lands beside it. The
 * rename is retried on transient Windows sharing errors; any failure removes
 * the temp file and rethrows the original error.
 */
export function writeJsonAtomic(path, value, { retries = 8, delayMs = 100 } = {}) {
  const target = resolve(path);
  const tmp = `${target}.tmp-${process.pid}-${tempCounter++}`;
  try {
    writeFileSync(tmp, `${JSON.stringify(value, null, 2)}\n`, { encoding: "utf8" });

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

/**
 * Reads and parses a JSON file, retrying while the file is transiently locked.
 * The sidebar plugin polls the runtime file, and Windows can report a sharing
 * violation (EBUSY/EPERM) while another process holds it open. A missing file
 * (`ENOENT`) and malformed JSON fail immediately.
 */
export function readJsonWithRetry(path, { retries = 8, delayMs = 100 } = {}) {
  let lastError;
  for (let attempt = 0; attempt <= retries; attempt += 1) {
    try {
      return JSON.parse(readFileSync(path, "utf8"));
    } catch (error) {
      lastError = error;
      if (!TRANSIENT_FILE_ERRORS.has(error?.code) || attempt === retries) {
        break;
      }
      sleep(delayMs * (attempt + 1));
    }
  }
  throw lastError;
}
