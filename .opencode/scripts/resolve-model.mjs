// Resolves the effective model (and reasoning effort) for a subagent role from the
// two-layer model policy.
//
//   node .opencode/scripts/resolve-model.mjs <role> [--record]
//
// Role is one of the policy's roles (planner, implementer, verifier, explorer,
// research, mechanical). Prints the model id, with `#variant` when the model
// supports reasoning effort, or exits non-zero when the role is blocked
// (block_and_ask) or has no resolvable model.
//
// Effort: the role declares a normalized level (off|low|medium|high|max); the
// resolver maps it onto the chosen model's supported `variants` (nearest at or
// below the level; `off` means `none` when supported, otherwise no variant). A
// chain entry may override the level inline as `model#variant`.
//
// This is the orchestrator's dispatch gate: run it before every subagent dispatch
// and use its output as the `model` for tools.subagent.
// check-model-policy.mjs validates the result.

import { readFileSync, writeFileSync, renameSync, existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join, resolve } from "node:path";

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const POLICY_PATH = join(repoRoot, "docs", "agent-model-policy.json");
const RUNTIME_PATH = join(repoRoot, ".opencode", "model-policy.json");

const ROLES = ["planner", "implementer", "verifier", "explorer", "research", "mechanical"];
const EFFORTS = new Set(["off", "low", "medium", "high", "max"]);
const EFFORT_RANK = { none: 0, minimal: 1, low: 2, medium: 3, high: 4, xhigh: 5, max: 6 };

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

/** Splits `model#variant` into its parts; a bare id has no variant. */
export function parseModelEntry(entry) {
  const hash = entry.indexOf("#");
  if (hash <= 0) {
    return { id: entry, variant: undefined };
  }
  return { id: entry.slice(0, hash), variant: entry.slice(hash + 1) || undefined };
}

/**
 * Maps a normalized effort level onto a model's supported variants: the lowest
 * variant at or above the level, or the highest available when the model has
 * none that high. `off` selects `none` when supported. Returns undefined when
 * the model has no variants or the level cannot be applied.
 */
export function mapEffort(variants, effort) {
  const list = Array.isArray(variants) ? variants.filter((v) => EFFORT_RANK[v] !== undefined) : [];
  if (list.length === 0) {
    return undefined;
  }
  if (!effort || effort === "off") {
    return list.includes("none") ? "none" : undefined;
  }
  const target = EFFORT_RANK[effort];
  if (target === undefined) {
    return undefined;
  }
  const atOrAbove = list.filter((v) => EFFORT_RANK[v] >= target);
  if (atOrAbove.length > 0) {
    return atOrAbove.reduce((best, v) => (EFFORT_RANK[v] < EFFORT_RANK[best] ? v : best));
  }
  return list.reduce((best, v) => (EFFORT_RANK[v] > EFFORT_RANK[best] ? v : best));
}

export function formatModel(id, variant) {
  return variant ? `${id}#${variant}` : id;
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
  const known = chain
    .map(parseModelEntry)
    .filter((entry) => policy.models?.[entry.id]);

  const override = runtime.roleOverrides?.[role];
  const overrideEntry = override ? parseModelEntry(override) : undefined;
  if (overrideEntry && policy.models?.[overrideEntry.id]) {
    const variants = policy.models?.[overrideEntry.id]?.variants ?? [];
    const variant = overrideEntry.variant ?? mapEffort(variants, roleDef.effort);
    return {
      role,
      model: formatModel(overrideEntry.id, variant),
      modelId: overrideEntry.id,
      variant,
      preset,
      chain,
      reason: "role override",
      presetReason,
    };
  }

  const exhausted = runtime.exhaustedUntil ?? {};
  const skipped = chain.filter((entry) => {
    const { id } = parseModelEntry(entry);
    return policy.models?.[id] && isStillExhausted(exhausted[id], now);
  });
  const usable = known.filter((entry) => !isStillExhausted(exhausted[entry.id], now));

  if (usable.length === 0) {
    const detail = skipped.length > 0 ? ` (${skipped.length} model(s) exhausted)` : "";
    if (roleDef.on_exhaustion === "block_and_ask") {
      return { error: `role '${role}' is blocked: no available model in preset '${preset}'${detail}` };
    }
    return { error: `role '${role}' has no available model in preset '${preset}'${detail}` };
  }

  const chosen = usable[0];
  const variants = policy.models?.[chosen.id]?.variants ?? [];
  const variant = chosen.variant ?? mapEffort(variants, roleDef.effort);

  return {
    role,
    model: formatModel(chosen.id, variant),
    modelId: chosen.id,
    variant,
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
    const presetDefault = (result.chain ?? []).map(parseModelEntry).find((e) => policy.models?.[e.id])?.id ?? result.modelId;
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
