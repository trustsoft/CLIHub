// Resolves the effective model (and reasoning effort) for a subagent role from the
// two-layer model policy.
//
//   node .opencode/scripts/resolve-model.mjs <role> [--record] [--exhaust <model>]
//                                       [--until <iso> | --backoff <minutes>]
//
// Role is one of the policy's roles (planner, implementer, verifier, explorer,
// research, mechanical). Prints the model id, with `#variant` when the model
// supports reasoning effort, or exits non-zero when the role is blocked
// (block_and_ask) or has no resolvable model.
//
// --record appends the dispatch to the runtime session log.
// --exhaust marks a model as unavailable (429 / quota / billing / auth /
// not-found) and resolves the next usable model for the role, so the fallback
// happens in one command. The mark expires at --until, or after --backoff
// minutes (default 30). A `model#variant` argument is reduced to its model id.
//
// Effort: the role declares a normalized level (off|low|medium|high|max); the
// resolver maps it onto the chosen model's supported `variants` (the lowest
// variant at or above the level, else the highest available; `off` means
// `none` when supported, otherwise no variant). A chain entry may override the
// level inline as `model#variant`.
//
// This is the orchestrator's dispatch gate: run it before every subagent dispatch
// and use its output as the `model` for tools.subagent.
// check-model-policy.mjs validates the result.

import { readFileSync, writeFileSync, renameSync, existsSync, rmSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join, resolve } from "node:path";

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const POLICY_PATH = join(repoRoot, "docs", "agent-model-policy.json");
const RUNTIME_PATH = join(repoRoot, ".opencode", "model-policy.json");

const ROLES = ["planner", "implementer", "verifier", "explorer", "research", "mechanical"];
const EFFORTS = new Set(["off", "low", "medium", "high", "max"]);
const EFFORT_RANK = { none: 0, minimal: 1, low: 2, medium: 3, high: 4, xhigh: 5, max: 6 };

// Windows can transiently hold a handle on the target file, which surfaces as
// EPERM/EBUSY on the rename. These errors are worth retrying; everything else
// is a real failure.
const TRANSIENT_FILE_ERRORS = new Set(["EPERM", "EBUSY", "EACCES"]);

/** Blocks the current thread for `ms` milliseconds (the script is synchronous). */
function sleep(ms) {
  Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, ms);
}

/**
 * Writes `value` as JSON to `path` through a temp file and a rename, so a
 * reader never observes a partially written file. The rename is retried on
 * transient Windows sharing errors; the temp file is removed when the write
 * ultimately fails.
 */
export function writeJsonAtomic(path, value, { retries = 8, delayMs = 100 } = {}) {
  const tmp = `${path}.tmp-${process.pid}`;
  writeFileSync(tmp, `${JSON.stringify(value, null, 2)}\n`, "utf8");
  let lastError;
  for (let attempt = 0; attempt <= retries; attempt += 1) {
    try {
      renameSync(tmp, path);
      return;
    } catch (error) {
      lastError = error;
      const transient = TRANSIENT_FILE_ERRORS.has(error?.code);
      if (!transient || attempt === retries) {
        break;
      }
      sleep(delayMs * (attempt + 1));
    }
  }
  try {
    rmSync(tmp, { force: true });
  } catch {
    // The temp file is best-effort cleanup; the original error is the one that matters.
  }
  throw lastError;
}

function readJson(path) {
  return JSON.parse(readFileSync(path, "utf8"));
}

/**
 * Reads a JSON file, retrying while the file is transiently locked. The
 * sidebar plugin polls the runtime file, and Windows can report a sharing
 * violation (EBUSY/EPERM) while another process holds it open.
 */
export function readJsonWithRetry(path, { retries = 8, delayMs = 100 } = {}) {
  let lastError;
  for (let attempt = 0; attempt <= retries; attempt += 1) {
    try {
      return readJson(path);
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

/**
 * Marks a model as unavailable until `until` (ISO-8601) and returns the mutated
 * runtime object. The model may be given as `model` or `model#variant`; the
 * variant is dropped because exhaustion applies to the model, not the effort.
 */
export function applyExhaustion(runtime, model, until) {
  const { id } = parseModelEntry(model);
  runtime.exhaustedUntil ??= {};
  runtime.exhaustedUntil[id] = until;
  return runtime;
}

/** Reads the value of a `--flag value` argument, or undefined when the flag is absent. */
function flagValue(argv, flag) {
  const index = argv.indexOf(flag);
  return index >= 0 && index + 1 < argv.length ? argv[index + 1] : undefined;
}

function main() {
  const argv = process.argv.slice(2);
  const role = argv[0];
  const record = argv.includes("--record");
  const exhaust = flagValue(argv, "--exhaust");
  const until = flagValue(argv, "--until");
  const backoff = flagValue(argv, "--backoff");

  if (!ROLES.includes(role)) {
    console.error(
      `usage: resolve-model.mjs <${ROLES.join("|")}> [--record] [--exhaust <model>] [--until <iso> | --backoff <minutes>]`,
    );
    process.exit(2);
  }

  const policy = readJsonWithRetry(POLICY_PATH);
  const runtime = existsSync(RUNTIME_PATH) ? readJsonWithRetry(RUNTIME_PATH) : {};
  const now = new Date();

  runtime.schemaVersion ??= 1;
  runtime.activePreset ??= policy.active_preset;
  runtime.roleOverrides ??= {};
  runtime.exhaustedUntil ??= {};
  runtime.sessionLog ??= [];

  /** Persists the runtime file; a failed write is reported but never fatal. */
  function writeRuntime() {
    try {
      writeJsonAtomic(RUNTIME_PATH, runtime);
    } catch (error) {
      // The model is already printed; a failed runtime write must not fail the dispatch.
      console.error(`warning: could not update ${RUNTIME_PATH}: ${error?.message ?? error}`);
    }
  }

  let exhaustedModel;
  if (exhaust) {
    const { id } = parseModelEntry(exhaust);
    if (!policy.models?.[id]) {
      console.error(`unknown model '${id}' (not in the policy catalog)`);
      process.exit(2);
    }
    let expiry;
    if (until) {
      expiry = until;
      if (Number.isNaN(Date.parse(expiry))) {
        console.error(`--until is not a valid date: '${until}'`);
        process.exit(2);
      }
    } else {
      const minutes = backoff === undefined ? 30 : Number(backoff);
      if (!Number.isFinite(minutes) || minutes <= 0) {
        console.error(`--backoff must be a positive number of minutes, got '${backoff}'`);
        process.exit(2);
      }
      expiry = new Date(now.getTime() + minutes * 60_000).toISOString();
    }
    applyExhaustion(runtime, id, expiry);
    exhaustedModel = id;
  }

  const result = resolveModel(policy, runtime, role, now);
  if (result.error) {
    console.error(result.error);
    if (exhaust) {
      // Keep the exhaustion mark even when the role is now blocked: dropping it
      // would send the next dispatch straight back to the failing model.
      writeRuntime();
    }
    process.exit(1);
  }

  // The resolved model is printed before the session-log write: the dispatch
  // decision must survive a failed write.
  console.log(result.model);

  if (exhaust) {
    const expiry = runtime.exhaustedUntil[exhaustedModel];
    console.error(`marked ${exhaustedModel} exhausted until ${expiry}`);
  }

  if (record) {
    const presetDefault = exhaustedModel
      ?? (result.chain ?? []).map(parseModelEntry).find((e) => policy.models?.[e.id])?.id
      ?? result.modelId;
    runtime.sessionLog.push({
      at: now.toISOString(),
      role,
      from: presetDefault,
      to: result.model,
      preset: result.preset,
      reason: result.reason,
    });
    if (runtime.sessionLog.length > 100) {
      runtime.sessionLog = runtime.sessionLog.slice(-100);
    }
  }

  if (record || exhaust) {
    writeRuntime();
  }
}

const isMain = process.argv[1] && resolve(process.argv[1]) === resolve(fileURLToPath(import.meta.url));
if (isMain) {
  main();
}
