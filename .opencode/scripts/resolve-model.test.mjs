// Tests for the model policy resolver. Run with: node .opencode/scripts/resolve-model.test.mjs
//
// Exercises the resolution rules the orchestrator relies on at dispatch time:
// override window, runtime activePreset, role override (including exhaustion
// skipping of a pinned model), exhaustedUntil skipping, block_and_ask
// starvation, and reasoning-effort mapping onto model variants.

import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import {
  copyFileSync,
  existsSync,
  mkdtempSync,
  mkdirSync,
  readFileSync,
  rmSync,
  writeFileSync,
} from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import {
  resolveModel,
  mapEffort,
  applyExhaustion,
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

/**
 * Stages a throwaway project under `%TEMP%`: the resolver, the shared JSON-IO
 * module it imports, and the gate script that imports the resolver, plus the
 * policy document the test writes. The caller removes `root` in `finally`.
 */
function stageProject(policyDoc, { runtime: runtimeDoc } = {}) {
  const root = mkdtempSync(join(tmpdir(), "model-policy-project-"));
  const scriptsDir = join(root, ".opencode", "scripts");
  mkdirSync(scriptsDir, { recursive: true });
  mkdirSync(join(root, "docs"), { recursive: true });
  for (const name of ["resolve-model.mjs", "check-model-policy.mjs", "json-io.mjs"]) {
    copyFileSync(fileURLToPath(new URL(`./${name}`, import.meta.url)), join(scriptsDir, name));
  }
  writeFileSync(
    join(root, "docs", "agent-model-policy.json"),
    `${JSON.stringify(policyDoc, null, 2)}\n`,
    "utf8",
  );
  if (runtimeDoc !== undefined) {
    writeFileSync(
      join(root, ".opencode", "model-policy.json"),
      `${JSON.stringify(runtimeDoc, null, 2)}\n`,
      "utf8",
    );
  }
  return {
    root,
    check: join(scriptsDir, "check-model-policy.mjs"),
    resolve: join(scriptsDir, "resolve-model.mjs"),
    runtimePath: join(root, ".opencode", "model-policy.json"),
  };
}

/** A minimal policy the gate accepts: one role, one preset, a configurable chain and catalog. */
function stagedPolicy({
  role = "auditor",
  chain = ["vendor/model#high"],
  variants = ["low", "high"],
  models,
} = {}) {
  return {
    version: 1,
    active_preset: "default",
    roles: { [role]: { tier: "high", on_exhaustion: "next", effort: "low" } },
    models: models ?? { "vendor/model": { billing: "plan", tier: "high", variants } },
    presets: { default: { [role]: chain } },
  };
}

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

test("an exhausted role override is skipped in favour of the chain", () => {
  const r = runtime({
    roleOverrides: { planner: "zai-coding-plan/glm-5.3#max" },
    exhaustedUntil: { "zai-coding-plan/glm-5.3": new Date(beforeWindow.getTime() + HOUR).toISOString() },
  });
  const res = resolveModel(policy, r, "planner", beforeWindow);
  assert.equal(res.model, "relayfast/claude-sonnet-5#high");
  assert.equal(res.reason, "exhausted skip");
  assert.equal(res.overrideSkipped, "zai-coding-plan/glm-5.3#max");
  assert.deepEqual(res.skipped, ["zai-coding-plan/glm-5.3"]);
});

test("an exhausted override outside the chain also falls back with the skip reason", () => {
  const r = runtime({
    roleOverrides: { verifier: "opencode/glm-5.3-flash-free#low" },
    exhaustedUntil: { "opencode/glm-5.3-flash-free": new Date(beforeWindow.getTime() + HOUR).toISOString() },
  });
  const res = resolveModel(policy, r, "verifier", beforeWindow);
  assert.equal(res.model, "zai-coding-plan/glm-5.3#high");
  assert.equal(res.reason, "exhausted skip");
  assert.equal(res.overrideSkipped, "opencode/glm-5.3-flash-free#low");
  assert.deepEqual(res.skipped, []);
});

test("an exhausted override cannot unblock a starved block_and_ask role", () => {
  const r = runtime({
    roleOverrides: { planner: "zai-coding-plan/glm-5.3#high" },
    exhaustedUntil: {
      "zai-coding-plan/glm-5.3": new Date(beforeWindow.getTime() + HOUR).toISOString(),
      "relayfast/claude-sonnet-5": new Date(beforeWindow.getTime() + HOUR).toISOString(),
    },
  });
  const res = resolveModel(policy, r, "planner", beforeWindow);
  assert.ok(res.error, "expected an error");
  assert.match(res.error, /blocked/);
  assert.equal(res.model, undefined);
});

test("an exhausted override cannot unblock a starved next role", () => {
  const r = runtime({
    roleOverrides: { implementer: "zai-coding-plan/glm-5.3#high" },
    exhaustedUntil: {
      "zai-coding-plan/glm-5.3": new Date(beforeWindow.getTime() + HOUR).toISOString(),
      "relayfast/claude-sonnet-5": new Date(beforeWindow.getTime() + HOUR).toISOString(),
    },
  });
  const res = resolveModel(policy, r, "implementer", beforeWindow);
  assert.ok(res.error, "expected an error");
  assert.match(res.error, /no available model/);
  assert.equal(res.model, undefined);
});

test("a not-exhausted override still wins over an exhausted chain", () => {
  const r = runtime({
    roleOverrides: { planner: "relayfast/claude-sonnet-5#xhigh" },
    exhaustedUntil: { "zai-coding-plan/glm-5.3": new Date(beforeWindow.getTime() + HOUR).toISOString() },
  });
  const res = resolveModel(policy, r, "planner", beforeWindow);
  assert.equal(res.model, "relayfast/claude-sonnet-5#xhigh");
  assert.equal(res.reason, "role override");
  assert.equal(res.overrideSkipped, undefined);
});

test("an expired exhaustion mark lets the override win again", () => {
  const r = runtime({
    roleOverrides: { planner: "zai-coding-plan/glm-5.3#low" },
    exhaustedUntil: { "zai-coding-plan/glm-5.3": new Date(beforeWindow.getTime() - HOUR).toISOString() },
  });
  const res = resolveModel(policy, r, "planner", beforeWindow);
  assert.equal(res.model, "zai-coding-plan/glm-5.3#low");
  assert.equal(res.reason, "role override");
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

test("the policy gate passes on the current policy and runtime files", () => {
  const checker = fileURLToPath(new URL("./check-model-policy.mjs", import.meta.url));
  const result = spawnSync(process.execPath, [checker], { encoding: "utf8" });
  assert.equal(result.status, 0, `check-model-policy.mjs failed:\n${result.stderr}`);
  assert.match(result.stdout, /model policy check OK/);
});

test("checkModelPolicy_ChainVariantSupported_ExitZero", () => {
  const staged = stageProject(stagedPolicy({ chain: ["vendor/model#high"] }));
  try {
    const result = spawnSync(process.execPath, [staged.check], { encoding: "utf8" });
    assert.equal(result.status, 0, result.stderr);
    assert.match(result.stdout, /model policy check OK/);
  } finally {
    rmSync(staged.root, { recursive: true, force: true });
  }
});

test("checkModelPolicy_ChainVariantUnsupported_ReportsTheVariant", () => {
  const staged = stageProject(stagedPolicy({ chain: ["vendor/model#bogus"] }));
  try {
    const result = spawnSync(process.execPath, [staged.check], { encoding: "utf8" });
    assert.equal(result.status, 1, result.stdout);
    assert.match(result.stderr, /variant '#bogus' is not supported by 'vendor\/model'/);
  } finally {
    rmSync(staged.root, { recursive: true, force: true });
  }
});

test("checkModelPolicy_ChainTrailingHash_RejectsTheEmptyVariant", () => {
  const staged = stageProject(stagedPolicy({ chain: ["vendor/model#"] }));
  try {
    const result = spawnSync(process.execPath, [staged.check], { encoding: "utf8" });
    assert.equal(result.status, 1, result.stdout);
    assert.match(result.stderr, /variant '#' is not supported by 'vendor\/model'/);
  } finally {
    rmSync(staged.root, { recursive: true, force: true });
  }
});

test("checkModelPolicy_RuntimeOverrideTrailingHash_RejectsTheEmptyVariant", () => {
  const staged = stageProject(stagedPolicy(), {
    runtime: {
      schemaVersion: 1,
      activePreset: "default",
      roleOverrides: { auditor: "vendor/model#" },
      exhaustedUntil: {},
      sessionLog: [],
    },
  });
  try {
    const result = spawnSync(process.execPath, [staged.check], { encoding: "utf8" });
    assert.equal(result.status, 1, result.stdout);
    assert.match(result.stderr, /runtime\.roleOverrides\['auditor'\] variant '#' is not supported by 'vendor\/model'/);
  } finally {
    rmSync(staged.root, { recursive: true, force: true });
  }
});

test("resolveModelCli_PolicyDeclaredRole_ResolvesWithoutWriting", () => {
  const staged = stageProject(stagedPolicy());
  try {
    const result = spawnSync(process.execPath, [staged.resolve, "auditor"], { encoding: "utf8" });
    assert.equal(result.status, 0, result.stderr);
    assert.equal(result.stdout.trim(), "vendor/model#high");
    assert.equal(existsSync(staged.runtimePath), false, "a plain resolve writes no runtime file");
  } finally {
    rmSync(staged.root, { recursive: true, force: true });
  }
});

test("resolveModelCli_RoleAbsentFromPolicy_ExitTwoAndUsageNamesTheRoles", () => {
  const staged = stageProject(stagedPolicy({ role: "planner" }));
  try {
    const result = spawnSync(process.execPath, [staged.resolve, "auditor"], { encoding: "utf8" });
    assert.equal(result.status, 2, result.stdout);
    assert.equal(result.stdout, "");
    assert.match(result.stderr, /usage: resolve-model\.mjs <planner> \[--record\]/);
    assert.equal(existsSync(staged.runtimePath), false);
  } finally {
    rmSync(staged.root, { recursive: true, force: true });
  }
});

test("resolveModelCli_PolicyWithoutRoles_ExitTwoWithRolePlaceholder", () => {
  const staged = stageProject({ ...stagedPolicy(), roles: {} });
  try {
    const result = spawnSync(process.execPath, [staged.resolve, "auditor"], { encoding: "utf8" });
    assert.equal(result.status, 2, result.stdout);
    assert.match(result.stderr, /usage: resolve-model\.mjs <role> \[--record\]/);
  } finally {
    rmSync(staged.root, { recursive: true, force: true });
  }
});

test("resolveModelCli_ExhaustedRoleOverride_ResolvesTheChainFallback", () => {
  const staged = stageProject(
    stagedPolicy({
      chain: ["vendor/model#high", "other/model#high"],
      models: {
        "vendor/model": { billing: "plan", tier: "high", variants: ["low", "high"] },
        "other/model": { billing: "plan", tier: "high", variants: ["low", "high"] },
      },
    }),
    {
      runtime: {
        schemaVersion: 1,
        activePreset: "default",
        roleOverrides: { auditor: "vendor/model#high" },
        exhaustedUntil: {},
        sessionLog: [],
      },
    },
  );
  try {
    const result = spawnSync(process.execPath, [staged.resolve, "auditor", "--exhaust", "vendor/model"], {
      encoding: "utf8",
    });
    assert.equal(result.status, 0, result.stderr);
    // The pinned override is exhausted by this very command: dispatch must move
    // to the next chain entry (and its own pinned variant), not keep the override.
    assert.equal(result.stdout.trim(), "other/model#high");
    assert.notEqual(result.stdout.trim(), "vendor/model#high");
    assert.match(result.stderr, /marked vendor\/model exhausted until/);
    const doc = JSON.parse(readFileSync(staged.runtimePath, "utf8"));
    assert.ok(doc.exhaustedUntil["vendor/model"], "the exhaustion mark is persisted");
  } finally {
    rmSync(staged.root, { recursive: true, force: true });
  }
});

console.log(`\n${passed} tests passed`);
