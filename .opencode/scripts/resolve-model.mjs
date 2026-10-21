// Resolves the effective model for a subagent role from the two-layer model policy.
//
//   node .opencode/scripts/resolve-model.mjs <role> [--record]
//
// Role is one of the policy's roles (planner, implementer, verifier, explorer, mechanical).
// Prints the model id to stdout, or exits non-zero with a message when the role is
// blocked (block_and_ask) or has no resolvable model.
//
// With --record, appends a sessionLog entry to .opencode/model-policy.json describing the
// resolution (preset default -> resolved model, with the reason).
//
// This is the orchestrator's dispatch gate: run it before every subagent dispatch and use
// its output as the `model` for tools.subagent. check-model-policy.mjs validates the result.

import { readFileSync, writeFileSync, renameSync, existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join, resolve } from "node:path";

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const POLICY_PATH = join(repoRoot, "docs", "agent-model-policy.json");
const RUNTIME_PATH = join(repoRoot, ".opencode", "model-policy.json");

const ROLES = ["planner", "implementer", "verifier", "explorer", "research", "mechanical"];

function readJson(path) {
  return JSON.parse(readFileSync(path, "utf8"));
}

function writeJsonAtomic(path, value) {
  const tmp = `${path}.tmp-${process.pid}`;
  writeFileSync(tmp, `${JSON.stringify(value, null, 2)}\n`, "utf8");
  renameSync(tmp, path);
}

// True when the model is currently exhausted (its exhaustedUntil is still in the future).
function isStillExhausted(iso, now) {
  if (!iso) {
    return false;
  }
  const t = Date.parse(iso);
  return Number.isNaN(t) ? false : t > now.getTime();
}

// effective_preset = first override whose window contains now, else runtime activePreset,
// else the policy's active_preset.
function effectivePreset(policy, runtime, now) {
  for (const o of policy.overrides ?? []) {
    const from = o.valid_from ? Date.parse(o.valid_from) : -Infinity;
    const until = o.valid_until ? Date.parse(o.valid_until) : Infinity;
    if (from <= now.getTime() && now.getTime() < until) {
      return { preset: o.preset, reason: "override window" };
    }
  }
  if (runtime.activePreset && policy.presets?.[runtime.activePreset]) {
    return { preset: runtime.activePreset, reason: "runtime activePreset" };
  }
  return { preset: policy.active_preset, reason: "policy default" };
}

export function resolveModel(policy, runtime, role, now = new Date()) {
  const roleDef = policy.roles?.[role];
  if (!roleDef) {
    return { error: `unknown role '${role}'` };
  }

  const { preset, reason: presetReason } = effectivePreset(policy, runtime, now);
  const chain = policy.presets?.[preset]?.[role] ?? policy.presets?.default?.[role] ?? [];

  const override = runtime.roleOverrides?.[role];
  if (override && policy.models?.[override]) {
    return {
      role,
      model: override,
      preset,
      chain,
      reason: "role override",
      presetReason,
    };
  }

  const exhausted = runtime.exhaustedUntil ?? {};
  const skipped = chain.filter((m) => policy.models?.[m] && isStillExhausted(exhausted[m], now));
  const usable = chain.filter((m) => policy.models?.[m] && !isStillExhausted(exhausted[m], now));

  if (usable.length === 0) {
    const detail = skipped.length > 0 ? ` (${skipped.length} model(s) exhausted)` : "";
    if (roleDef.on_exhaustion === "block_and_ask") {
      return { error: `role '${role}' is blocked: no available model in preset '${preset}'${detail}` };
    }
    return { error: `role '${role}' has no available model in preset '${preset}'${detail}` };
  }

  return {
    role,
    model: usable[0],
    preset,
    chain,
    skipped,
    reason: skipped.length > 0 ? "exhausted skip" : "preset default",
    presetReason,
  };
}

function main() {
  const role = process.argv[2];
  const record = process.argv.includes("--record");

  if (!ROLES.includes(role)) {
    console.error(`usage: resolve-model.mjs <${ROLES.join("|")}> [--record]`);
    process.exit(2);
  }

  const policy = readJson(POLICY_PATH);
  const runtime = existsSync(RUNTIME_PATH) ? readJson(RUNTIME_PATH) : {};

  const result = resolveModel(policy, runtime, role, new Date());
  if (result.error) {
    console.error(result.error);
    process.exit(1);
  }

  if (record) {
    const presetDefault = result.chain.find((m) => policy.models?.[m]) ?? result.model;
    runtime.schemaVersion ??= 1;
    runtime.activePreset ??= policy.active_preset;
    runtime.roleOverrides ??= {};
    runtime.exhaustedUntil ??= {};
    runtime.sessionLog ??= [];
    runtime.sessionLog.push({
      at: new Date().toISOString(),
      role,
      from: presetDefault,
      to: result.model,
      preset: result.preset,
      reason: result.reason,
    });
    if (runtime.sessionLog.length > 100) {
      runtime.sessionLog = runtime.sessionLog.slice(-100);
    }
    writeJsonAtomic(RUNTIME_PATH, runtime);
  }

  console.log(result.model);
}

const isMain = process.argv[1] && resolve(process.argv[1]) === resolve(fileURLToPath(import.meta.url));
if (isMain) {
  main();
}
