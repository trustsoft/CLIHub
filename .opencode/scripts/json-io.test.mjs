// Tests for the shared JSON IO helpers the deployed project scripts import.
// Run with: node .opencode/scripts/json-io.test.mjs
//
// Exercises the atomic write (round-trip, exact bytes, temp-file cleanup, no
// directory creation), the retrying read (round-trip, missing file, malformed
// JSON), the `--flag value` reader, the retryable-code set, and the
// pipeline-state wrapper's own retry budget.

import assert from "node:assert/strict";
import {
  mkdirSync,
  mkdtempSync,
  readdirSync,
  readFileSync,
  rmSync,
  writeFileSync,
} from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import {
  TRANSIENT_FILE_ERRORS,
  flagValue,
  readJsonWithRetry,
  writeJsonAtomic,
} from "./json-io.mjs";
import { writeStateAtomic } from "./pipeline-state.mjs";

let passed = 0;
function test(name, fn) {
  fn();
  passed += 1;
  console.log(`ok - ${name}`);
}

/** Runs `fn` against a throwaway directory and removes it afterwards. */
function withDir(fn) {
  const dir = mkdtempSync(join(tmpdir(), "json-io-"));
  try {
    return fn(dir);
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
}

test("writeJsonAtomic replaces the target and leaves no temp file", () => {
  withDir((dir) => {
    const target = join(dir, "runtime.json");
    writeJsonAtomic(target, { activePreset: "default" });
    writeJsonAtomic(target, { activePreset: "flash_only" });
    assert.deepEqual(JSON.parse(readFileSync(target, "utf8")), { activePreset: "flash_only" });
    assert.deepEqual(readdirSync(dir), ["runtime.json"]);
  });
});

test("writeJsonAtomic removes the temp file when the write fails", () => {
  withDir((dir) => {
    const target = join(dir, "target.json");
    // A directory cannot be replaced by a rename, so the write fails.
    mkdirSync(target);
    assert.throws(() => writeJsonAtomic(target, { a: 1 }, { retries: 0 }));
    assert.deepEqual(readdirSync(dir), ["target.json"]);
  });
});

test("readJsonWithRetry reads a file and reports a missing one", () => {
  withDir((dir) => {
    const target = join(dir, "runtime.json");
    writeFileSync(target, JSON.stringify({ activePreset: "default" }), "utf8");
    assert.deepEqual(readJsonWithRetry(target), { activePreset: "default" });
    assert.throws(() => readJsonWithRetry(join(dir, "missing.json"), { retries: 0 }));
  });
});

test("writeJsonAtomic_Object_WritesIndentedUtf8WithOneTrailingNewline", () => {
  withDir((dir) => {
    const target = join(dir, "value.json");
    const value = { activePreset: "default", nested: { effort: [1, true, null] }, note: "héllo" };

    writeJsonAtomic(target, value);

    const text = readFileSync(target, "utf8");
    assert.equal(text, `${JSON.stringify(value, null, 2)}\n`, "exact bytes, one trailing newline");
    assert.notEqual(text.charCodeAt(0), 0xfeff, "no BOM");
    assert.deepEqual(JSON.parse(text), value, "non-ASCII text round-trips as UTF-8");
    assert.deepEqual(readdirSync(dir), ["value.json"]);
  });
});

test("writeJsonAtomic_MissingSubdirectory_ThrowsAndCreatesNothing", () => {
  withDir((dir) => {
    const target = join(dir, "missing", "nested.json");

    assert.throws(() => writeJsonAtomic(target, { a: 1 }), /ENOENT/);
    assert.deepEqual(readdirSync(dir), [], "no directory and no temp file may be created");
  });
});

test("flagValue_PresentAbsentAndTrailing_ReturnsTheNextArgumentOrUndefined", () => {
  assert.equal(flagValue(["--file", "state.json"], "--file"), "state.json");
  assert.equal(flagValue(["add", "id", "--limit", "5"], "--limit"), "5");
  assert.equal(flagValue(["--file"], "--file"), undefined, "a trailing flag has no value");
  assert.equal(flagValue(["add", "id"], "--file"), undefined, "an absent flag yields undefined");
});

test("TRANSIENT_FILE_ERRORS_Set_HoldsExactlyTheThreeRetryableCodes", () => {
  assert.equal(TRANSIENT_FILE_ERRORS.size, 3);
  assert.deepEqual([...TRANSIENT_FILE_ERRORS].sort(), ["EACCES", "EBUSY", "EPERM"]);
});

test("readJsonWithRetry_MalformedJson_FailsWithoutRetrying", () => {
  withDir((dir) => {
    const target = join(dir, "broken.json");
    writeFileSync(target, "{ not json", "utf8");

    assert.throws(() => readJsonWithRetry(target), SyntaxError, "malformed JSON is not retried");
  });
});

test("writeStateAtomic_RetriesZero_SurfacesTheRenameErrorAndLeavesNoTempFile", () => {
  withDir((dir) => {
    const target = join(dir, "pipeline.json");
    // A directory cannot be replaced by a rename, so the write fails on the first attempt.
    mkdirSync(target);

    assert.throws(
      () => writeStateAtomic(target, { schemaVersion: 1, changes: [] }, { retries: 0 }),
      (error) => error instanceof Error && typeof error.code === "string",
      "the original rename error surfaces, not a wrapper",
    );
    assert.deepEqual(readdirSync(dir), ["pipeline.json"], "the temp file is cleaned up");
  });
});

console.log(`\n${passed} tests passed`);
