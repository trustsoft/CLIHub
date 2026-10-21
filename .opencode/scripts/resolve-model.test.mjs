// Tests for the model policy resolver. Run with: node .opencode/scripts/resolve-model.test.mjs
//
// Exercises the resolution rules the orchestrator relies on at dispatch time:
// override window, runtime activePreset, role override, exhaustedUntil skipping,
// block_and_ask starvation, and reasoning-effort mapping onto model variants.

import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { mkdtempSync, mkdirSync, readFileSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import {
  resolveModel,
  mapEffort,
  applyExhaustion,
  writeJsonAtomic,
  readJsonWithRetry,
} from "./resolve-model.mjs";

const HOUR = 60 * 60 * 1000;
// `beforeWindow` is outside the override window; `inWindow` is inside it.
const beforeWindow = new Date("2026-10-21T10:00:00Z");
const inWindow = new Date("2026-10-21T12:00:00Z");

const policy = {
  version: 1,
  active_preset: "default",
  roles: {
    planner: { tier: "high", on_exhaustion: "block_and_ask", effort: "high" },
    implementer: { tier: "high", on_exhaustion: "next", effort: "medium" },
    verifier: { tier: "high", on_exhaustion: "block_and_ask", effort: "high" },
    explorer: { tier: "any", on_exhaustion: "next", effort: "low" },
    research: { tier: "medium", on_exhaustion: "next", effort: "medium" },
    mechanical: { tier: "any", on_exhaustion: "next", effort: "low" },
  },
  models: {
    "zai-coding-plan/glm-5.3": { billing: "plan", tier: "high", variants: ["low", "high", "max"] },
    "zai-coding-plan/glm-5.3-flash": { billing: "plan", tier: "medium", variants: ["low", "high", "max"] },
    "relayfast/claude-sonnet-5": { billing: "paid", tier: "high", variants: ["low", "medium", "high", "xhigh", "max"] },
    "opencode/glm-5.3-flash-free": { billing: "free", tier: "medium", variants: ["low", "high", "max"] },
  },
  presets: {
    default: {
      planner: ["zai-coding-plan/glm-5.3", "relayfast/claude-sonnet-5"],
      implementer: ["zai-coding-plan/glm-5.3", "relayfast/claude-sonnet-5"],
      verifier: ["zai-coding-plan/glm-5.3", "relayfast/claude-sonnet-5"],
      explorer: ["zai-coding-plan/glm-5.3-flash", "opencode/glm-5.3-flash-free"],
      research: ["zai-coding-plan/glm-5.3-flash", "relayfast/claude-sonnet-5"],
      mechanical: ["zai-coding-plan/glm-5.3-flash", "opencode/glm-5.3-flash-free"],
    },
    plan_exhausted: {
      planner: ["relayfast/claude-sonnet-5"],
      implementer: ["relayfast/claude-sonnet-5"],
      verifier: ["relayfast/claude-sonnet-5"],
      explorer: ["opencode/glm-5.3-flash-free"],
      research: ["relayfast/claude-sonnet-5"],
      mechanical: ["opencode/glm-5.3-flash-free"],
    },
  },
  overrides: [
    { preset: "plan_exhausted", valid_from: "2026-10-21T11:00:00Z", valid_until: "2026-10-21T13:00:00Z" },
  ],
};

const runtime = (over = {}) => ({
  schemaVersion: 1,
  activePreset: "default",
  roleOverrides: {},
  exhaustedUntil: {},
  sessionLog: [],
  ...over,
});

let passed = 0;
function test(name, fn) {
  fn();
  passed += 1;
  console.log(`ok - ${name}`);
}

test("uses the preset default with the role effort applied", () => {
  assert.equal(resolveModel(policy, runtime(), "planner", beforeWindow).model, "zai-coding-plan/glm-5.3#high");
});

test("runtime activePreset overrides the policy default", () => {
  const r = runtime({ activePreset: "plan_exhausted" });
  assert.equal(resolveModel(policy, r, "planner", beforeWindow).model, "relayfast/claude-sonnet-5#high");
});

test("an override window wins over runtime activePreset", () => {
  const r = runtime({ activePreset: "default" });
  const res = resolveModel(policy, r, "planner", inWindow);
  assert.equal(res.model, "relayfast/claude-sonnet-5#high");
  assert.equal(res.presetReason, "override window");
});

test("an override window outside its range is ignored", () => {
  const r = runtime({ activePreset: "default" });
  assert.equal(resolveModel(policy, r, "planner", beforeWindow).model, "zai-coding-plan/glm-5.3#high");
});

test("role override beats the preset chain and keeps the role effort", () => {
  const r = runtime({ roleOverrides: { verifier: "opencode/glm-5.3-flash-free" } });
  assert.equal(resolveModel(policy, r, "verifier", beforeWindow).model, "opencode/glm-5.3-flash-free#high");
});

test("a role override may pin its own variant", () => {
  const r = runtime({ roleOverrides: { verifier: "opencode/glm-5.3-flash-free#low" } });
  assert.equal(resolveModel(policy, r, "verifier", beforeWindow).model, "opencode/glm-5.3-flash-free#low");
});

test("an ignored role override falls back to the chain", () => {
  const r = runtime({ roleOverrides: { verifier: "does/not-exist" } });
  assert.equal(resolveModel(policy, r, "verifier", beforeWindow).model, "zai-coding-plan/glm-5.3#high");
});

test("exhausted models are skipped", () => {
  const r = runtime({
    exhaustedUntil: { "zai-coding-plan/glm-5.3": new Date(beforeWindow.getTime() + HOUR).toISOString() },
  });
  const res = resolveModel(policy, r, "planner", beforeWindow);
  assert.equal(res.model, "relayfast/claude-sonnet-5#high");
  assert.deepEqual(res.skipped, ["zai-coding-plan/glm-5.3"]);
});

test("expired exhaustion no longer skips", () => {
  const r = runtime({
    exhaustedUntil: { "zai-coding-plan/glm-5.3": new Date(beforeWindow.getTime() - HOUR).toISOString() },
  });
  assert.equal(resolveModel(policy, r, "planner", beforeWindow).model, "zai-coding-plan/glm-5.3#high");
});

test("block_and_ask roles fail closed when the chain is exhausted", () => {
  const r = runtime({
    activePreset: "plan_exhausted",
    exhaustedUntil: { "relayfast/claude-sonnet-5": new Date(beforeWindow.getTime() + HOUR).toISOString() },
  });
  const res = resolveModel(policy, r, "verifier", beforeWindow);
  assert.ok(res.error, "expected an error");
  assert.match(res.error, /blocked/);
});

test("next roles still report no model when the chain is empty", () => {
  const r = runtime({
    activePreset: "plan_exhausted",
    exhaustedUntil: { "opencode/glm-5.3-flash-free": new Date(beforeWindow.getTime() + HOUR).toISOString() },
  });
  const res = resolveModel(policy, r, "explorer", beforeWindow);
  assert.ok(res.error, "expected an error");
});

test("unknown role is rejected", () => {
  assert.ok(resolveModel(policy, runtime(), "wizard", beforeWindow).error);
});

test("a chain entry may pin the variant", () => {
  const pinned = structuredClone(policy);
  pinned.presets.default.planner = ["relayfast/claude-sonnet-5#xhigh"];
  assert.equal(resolveModel(pinned, runtime(), "planner", beforeWindow).model, "relayfast/claude-sonnet-5#xhigh");
});

test("a chain entry missing from the catalog is ignored", () => {
  const broken = structuredClone(policy);
  broken.presets.default.explorer = ["opencode/glm-5.3-flash-free", "ghost/model"];
  assert.equal(resolveModel(broken, runtime(), "explorer", beforeWindow).model, "opencode/glm-5.3-flash-free#low");
});

test("research and mechanical resolve from their own chains", () => {
  assert.equal(resolveModel(policy, runtime(), "research", beforeWindow).model, "zai-coding-plan/glm-5.3-flash#high");
  assert.equal(resolveModel(policy, runtime(), "mechanical", beforeWindow).model, "zai-coding-plan/glm-5.3-flash#low");
});

test("mapEffort picks the lowest variant at or above the level", () => {
  assert.equal(mapEffort(["low", "high", "max"], "high"), "high");
  assert.equal(mapEffort(["low", "high", "max"], "max"), "max");
  assert.equal(mapEffort(["low", "high", "max"], "medium"), "high");
  assert.equal(mapEffort(["low", "medium", "high", "xhigh"], "max"), "xhigh");
  assert.equal(mapEffort(["high", "max"], "low"), "high");
  assert.equal(mapEffort(["none", "low", "high", "max"], "off"), "none");
  assert.equal(mapEffort(["low", "high", "max"], "off"), undefined);
  assert.equal(mapEffort([], "high"), undefined);
  assert.equal(mapEffort(["low", "high"], undefined), undefined);
  assert.equal(mapEffort(["low", "high"], "bogus"), undefined);
});

test("applyExhaustion marks the model and drops the variant", () => {
  const state = runtime();
  applyExhaustion(state, "zai-coding-plan/glm-5.3#high", "2026-10-22T00:00:00Z");
  assert.equal(state.exhaustedUntil["zai-coding-plan/glm-5.3"], "2026-10-22T00:00:00Z");
  assert.equal(state.exhaustedUntil["zai-coding-plan/glm-5.3#high"], undefined);
});

test("an exhausted model is skipped and the next chain entry is used", () => {
  const state = runtime();
  applyExhaustion(state, "zai-coding-plan/glm-5.3", "2026-10-22T00:00:00Z");
  const result = resolveModel(policy, state, "planner", beforeWindow);
  assert.equal(result.model, "relayfast/claude-sonnet-5#high");
  assert.equal(result.reason, "exhausted skip");
});

test("writeJsonAtomic replaces the target and leaves no temp file", () => {
  const dir = mkdtempSync(join(tmpdir(), "model-policy-"));
  const target = join(dir, "runtime.json");
  try {
    writeJsonAtomic(target, { activePreset: "default" });
    writeJsonAtomic(target, { activePreset: "flash_only" });
    assert.deepEqual(JSON.parse(readFileSync(target, "utf8")), { activePreset: "flash_only" });
    assert.deepEqual(readdirSync(dir), ["runtime.json"]);
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
});

test("writeJsonAtomic removes the temp file when the write fails", () => {
  const dir = mkdtempSync(join(tmpdir(), "model-policy-"));
  const target = join(dir, "target.json");
  try {
    // A directory cannot be replaced by a rename, so the write fails.
    mkdirSync(target);
    assert.throws(() => writeJsonAtomic(target, { a: 1 }, { retries: 0 }));
    assert.deepEqual(readdirSync(dir), ["target.json"]);
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
});

test("the policy gate passes on the current policy and runtime files", () => {
  const checker = fileURLToPath(new URL("./check-model-policy.mjs", import.meta.url));
  const result = spawnSync(process.execPath, [checker], { encoding: "utf8" });
  assert.equal(result.status, 0, `check-model-policy.mjs failed:\n${result.stderr}`);
  assert.match(result.stdout, /model policy check OK/);
});

test("readJsonWithRetry reads a file and reports a missing one", () => {
  const dir = mkdtempSync(join(tmpdir(), "model-policy-"));
  try {
    const target = join(dir, "runtime.json");
    writeFileSync(target, JSON.stringify({ activePreset: "default" }), "utf8");
    assert.deepEqual(readJsonWithRetry(target), { activePreset: "default" });
    assert.throws(() => readJsonWithRetry(join(dir, "missing.json"), { retries: 0 }));
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
});

console.log(`\n${passed} tests passed`);
