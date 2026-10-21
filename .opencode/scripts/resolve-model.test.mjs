// Tests for the model policy resolver. Run with: node .opencode/scripts/resolve-model.test.mjs
//
// Exercises the resolution rules that the orchestrator relies on at dispatch time:
// override window, runtime activePreset, role override, exhaustedUntil skipping, and
// block_and_ask starvation.

import assert from "node:assert/strict";
import { resolveModel } from "./resolve-model.mjs";

const HOUR = 60 * 60 * 1000;
// `beforeWindow` is outside the override window; `inWindow` is inside it.
const beforeWindow = new Date("2026-10-21T10:00:00Z");
const inWindow = new Date("2026-10-21T12:00:00Z");

const policy = {
  version: 1,
  active_preset: "default",
  roles: {
    planner: { tier: "high", on_exhaustion: "block_and_ask" },
    implementer: { tier: "high", on_exhaustion: "next" },
    verifier: { tier: "high", on_exhaustion: "block_and_ask" },
    explorer: { tier: "any", on_exhaustion: "next" },
    research: { tier: "medium", on_exhaustion: "next" },
    mechanical: { tier: "any", on_exhaustion: "next" },
  },
  models: {
    "zai-coding-plan/glm-5.3": { billing: "plan", tier: "high" },
    "zai-coding-plan/glm-5.3-flash": { billing: "plan", tier: "medium" },
    "relayfast/claude-sonnet-5": { billing: "paid", tier: "high" },
    "opencode/glm-5.3-flash-free": { billing: "free", tier: "medium" },
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

test("uses the preset default when no runtime preset is set", () => {
  assert.equal(resolveModel(policy, runtime(), "planner", beforeWindow).model, "zai-coding-plan/glm-5.3");
});

test("runtime activePreset overrides the policy default", () => {
  const r = runtime({ activePreset: "plan_exhausted" });
  assert.equal(resolveModel(policy, r, "planner", beforeWindow).model, "relayfast/claude-sonnet-5");
});

test("an override window wins over runtime activePreset", () => {
  const r = runtime({ activePreset: "default" });
  const res = resolveModel(policy, r, "planner", inWindow);
  assert.equal(res.model, "relayfast/claude-sonnet-5");
  assert.equal(res.presetReason, "override window");
});

test("an override window outside its range is ignored", () => {
  const r = runtime({ activePreset: "default" });
  assert.equal(resolveModel(policy, r, "planner", beforeWindow).model, "zai-coding-plan/glm-5.3");
});

test("role override beats the preset chain", () => {
  const r = runtime({ roleOverrides: { verifier: "opencode/glm-5.3-flash-free" } });
  assert.equal(resolveModel(policy, r, "verifier", beforeWindow).model, "opencode/glm-5.3-flash-free");
});

test("an ignored role override falls back to the chain", () => {
  const r = runtime({ roleOverrides: { verifier: "does/not-exist" } });
  assert.equal(resolveModel(policy, r, "verifier", beforeWindow).model, "zai-coding-plan/glm-5.3");
});

test("exhausted models are skipped", () => {
  const r = runtime({
    exhaustedUntil: { "zai-coding-plan/glm-5.3": new Date(beforeWindow.getTime() + HOUR).toISOString() },
  });
  const res = resolveModel(policy, r, "planner", beforeWindow);
  assert.equal(res.model, "relayfast/claude-sonnet-5");
  assert.deepEqual(res.skipped, ["zai-coding-plan/glm-5.3"]);
});

test("expired exhaustion no longer skips", () => {
  const r = runtime({
    exhaustedUntil: { "zai-coding-plan/glm-5.3": new Date(beforeWindow.getTime() - HOUR).toISOString() },
  });
  assert.equal(resolveModel(policy, r, "planner", beforeWindow).model, "zai-coding-plan/glm-5.3");
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

test("research resolves from its own chain", () => {
  assert.equal(resolveModel(policy, runtime(), "research", beforeWindow).model, "zai-coding-plan/glm-5.3-flash");
});

test("mechanical resolves even though no subagent maps to it", () => {
  assert.equal(resolveModel(policy, runtime(), "mechanical", beforeWindow).model, "zai-coding-plan/glm-5.3-flash");
});

test("a chain entry missing from the catalog is ignored", () => {
  const broken = structuredClone(policy);
  broken.presets.default.explorer = ["opencode/glm-5.3-flash-free", "ghost/model"];
  assert.equal(resolveModel(broken, runtime(), "explorer", beforeWindow).model, "opencode/glm-5.3-flash-free");
});

console.log(`\n${passed} tests passed`);
