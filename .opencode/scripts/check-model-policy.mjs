// Validates the two-layer model policy and the runtime selection.
//
//   node .opencode/scripts/check-model-policy.mjs
//
// Exit 0 when everything is consistent, non-zero with a report otherwise. This is the
// pipeline gate: run it before dispatching subagents and during verification.

import { readFileSync, existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join, resolve } from "node:path";
import { parseModelEntry } from "./resolve-model.mjs";

const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const POLICY_PATH = join(repoRoot, "docs", "agent-model-policy.json");
const RUNTIME_PATH = join(repoRoot, ".opencode", "model-policy.json");

const problems = [];
const notes = [];

const EFFORTS = new Set(["off", "low", "medium", "high", "max"]);

function isRecord(value) {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function fail(msg) {
  problems.push(msg);
}

const policy = JSON.parse(readFileSync(POLICY_PATH, "utf8"));

// ── policy ────────────────────────────────────────────────────────────────────
if (policy.version !== 1) {
  fail(`policy.version expected 1, got ${policy.version}`);
}

const roles = Object.keys(policy.roles ?? {});
if (roles.length === 0) {
  fail("policy has no roles");
}

const models = new Set(Object.keys(policy.models ?? {}));
if (models.size === 0) {
  fail("policy has no models in the catalog");
}

for (const [role, def] of Object.entries(policy.roles ?? {})) {
  if (!def.tier) {
    fail(`role '${role}' has no tier`);
  }
  if (!def.on_exhaustion) {
    fail(`role '${role}' has no on_exhaustion`);
  } else if (!["next", "block_and_ask"].includes(def.on_exhaustion)) {
    fail(`role '${role}' has invalid on_exhaustion '${def.on_exhaustion}'`);
  }
  if (def.effort !== undefined && !EFFORTS.has(def.effort)) {
    fail(`role '${role}' has invalid effort '${def.effort}'`);
  }
}

const presets = Object.keys(policy.presets ?? {});
if (presets.length === 0) {
  fail("policy has no presets");
}
if (!presets.includes("default")) {
  fail("policy has no 'default' preset (required fallback)");
}

for (const preset of presets) {
  const chains = policy.presets[preset] ?? {};
  for (const role of roles) {
    const chain = chains[role];
    if (!Array.isArray(chain) || chain.length === 0) {
      fail(`preset '${preset}' has no chain for role '${role}'`);
      continue;
    }
    for (const entry of chain) {
      const hash = entry.indexOf("#");
      const model = hash > 0 ? entry.slice(0, hash) : entry;
      const variant = hash > 0 ? entry.slice(hash + 1) : undefined;
      if (!models.has(model)) {
        fail(`preset '${preset}' role '${role}' references unknown model '${model}'`);
        continue;
      }
      if (variant !== undefined) {
        const supported = policy.models[model]?.variants ?? [];
        if (!supported.includes(variant)) {
          fail(`preset '${preset}' role '${role}' variant '#${variant}' is not supported by '${model}'`);
        }
      }
    }
  }
}

if (policy.active_preset && !presets.includes(policy.active_preset)) {
  fail(`policy.active_preset '${policy.active_preset}' is not a defined preset`);
}

for (const [i, o] of (policy.overrides ?? []).entries()) {
  if (!presets.includes(o.preset)) {
    fail(`override #${i} references unknown preset '${o.preset}'`);
  }
  if (!o.valid_from || !o.valid_until) {
    fail(`override #${i} needs valid_from and valid_until`);
  }
}

// ── display ───────────────────────────────────────────────────────────────────
// The display section holds the abbreviations the panel renders. It never
// changes the resolution, so it is optional, but when it is present every key
// must point at something the catalogue knows.
const display = policy.display;
if (display !== undefined) {
  const catalogueProviders = new Set();
  const catalogueNames = new Set();
  const catalogueVariants = new Set();
  for (const id of models) {
    const slash = id.indexOf("/");
    const provider = slash > 0 ? id.slice(0, slash) : id;
    const name = slash > 0 ? id.slice(slash + 1) : id;
    catalogueProviders.add(provider);
    catalogueNames.add(name);
    // A model name may be listed without its `-free` suffix when the provider
    // is in `drop_free_suffix_for`: that suffix is dropped for display.
    if (name.endsWith("-free")) {
      catalogueNames.add(name.slice(0, -"-free".length));
    }
    for (const variant of policy.models[id]?.variants ?? []) {
      catalogueVariants.add(variant);
    }
  }

  const checkTable = (label, table, known, { unique = true } = {}) => {
    if (!isRecord(table)) {
      fail(`display.${label} must be an object`);
      return;
    }
    const seen = new Set();
    for (const [key, value] of Object.entries(table)) {
      if (typeof value !== "string" || value.length === 0) {
        fail(`display.${label}['${key}'] must be a non-empty string`);
        continue;
      }
      if (unique && seen.has(value)) {
        fail(`display.${label} reuses the short form '${value}'`);
      }
      seen.add(value);
      if (known && !known.has(key)) {
        fail(`display.${label} references unknown '${key}'`);
      }
    }
  };

  // Provider aliases may share a short form (`relayfast` and `relay.fast` are
  // the same vendor), so uniqueness is only checked among catalogue providers.
  checkTable("providers", display.providers, null, { unique: false });
  const providerCodes = new Set();
  for (const provider of catalogueProviders) {
    const code = (display.providers ?? {})[provider];
    if (!code) {
      fail(`display.providers has no short form for the catalogue provider '${provider}'`);
      continue;
    }
    if (providerCodes.has(code)) {
      fail(`display.providers reuses the short form '${code}' for two catalogue providers`);
    }
    providerCodes.add(code);
  }
  checkTable("models", display.models, catalogueNames);
  checkTable("variants", display.variants, catalogueVariants);
  checkTable("roles", display.roles, new Set(roles));
  for (const role of roles) {
    if (!(display.roles ?? {})[role]) {
      fail(`display.roles has no short form for the role '${role}'`);
    }
  }
  const dropFree = display.drop_free_suffix_for ?? [];
  if (!Array.isArray(dropFree)) {
    fail("display.drop_free_suffix_for must be an array");
  } else {
    for (const provider of dropFree) {
      if (!catalogueProviders.has(provider)) {
        fail(`display.drop_free_suffix_for references unknown provider '${provider}'`);
      }
    }
  }
}

// ── runtime ───────────────────────────────────────────────────────────────────
if (!existsSync(RUNTIME_PATH)) {
  notes.push("runtime file absent (the sidebar creates it on first render)");
} else {
  const runtime = JSON.parse(readFileSync(RUNTIME_PATH, "utf8"));
  if (runtime.activePreset && !presets.includes(runtime.activePreset)) {
    fail(`runtime.activePreset '${runtime.activePreset}' is not a defined preset`);
  }
  for (const [role, model] of Object.entries(runtime.roleOverrides ?? {})) {
    if (!roles.includes(role)) {
      fail(`runtime.roleOverrides has unknown role '${role}'`);
    }
    const hash = model.indexOf("#");
    const id = hash > 0 ? model.slice(0, hash) : model;
    if (!models.has(id)) {
      fail(`runtime.roleOverrides['${role}'] references unknown model '${id}'`);
    } else if (hash > 0) {
      const variant = model.slice(hash + 1);
      const supported = policy.models[id]?.variants ?? [];
      if (!supported.includes(variant)) {
        fail(`runtime.roleOverrides['${role}'] variant '#${variant}' is not supported by '${id}'`);
      }
    }
  }
  for (const [model, until] of Object.entries(runtime.exhaustedUntil ?? {})) {
    if (!models.has(model)) {
      fail(`runtime.exhaustedUntil has unknown model '${model}'`);
    }
    if (Number.isNaN(Date.parse(until))) {
      fail(`runtime.exhaustedUntil['${model}'] is not a valid date: '${until}'`);
    }
  }
  for (const [i, entry] of (runtime.sessionLog ?? []).entries()) {
    if (!roles.includes(entry.role)) {
      fail(`sessionLog #${i} has unknown role '${entry.role}'`);
    }
    for (const field of ["from", "to"]) {
      const value = entry[field];
      if (!value) {
        continue;
      }
      const { id, variant } = parseModelEntry(value);
      if (!models.has(id)) {
        fail(`sessionLog #${i} references unknown model '${id}'`);
        continue;
      }
      if (variant !== undefined) {
        const supported = policy.models[id]?.variants ?? [];
        if (!supported.includes(variant)) {
          fail(`sessionLog #${i} variant '#${variant}' is not supported by '${id}'`);
        }
      }
    }
    if (entry.preset && !presets.includes(entry.preset)) {
      fail(`sessionLog #${i} references unknown preset '${entry.preset}'`);
    }
  }
}

if (problems.length > 0) {
  console.error("model policy check FAILED:");
  for (const p of problems) {
    console.error(`  - ${p}`);
  }
  process.exit(1);
}

const displaySummary = policy.display
  ? `, ${Object.keys(policy.display.providers ?? {}).length} display providers`
  : "";
console.log(`model policy check OK (${roles.length} roles, ${presets.length} presets, ${models.size} models${displaySummary})`);
for (const n of notes) {
  console.log(`  note: ${n}`);
}
