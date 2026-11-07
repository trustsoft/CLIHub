// Model-policy layer 3: reading both documents into a `Snapshot` and deriving
// every display description from it. No writes, no preset mutations.

import path from "node:path"
import { readDocument, readPolicy } from "./io"
import { EMPTY_RUNTIME, exhaustedModels, normalizeRuntime } from "./runtime"
import { declaredPreset, effectivePreset, effortVariant, parseEntry } from "./policy"
import { displayRules, shortModel, shortModelName, shortVariant, truncate } from "./display"
import { resolvePaths } from "./paths"
import { isRecord } from "./util"
import type { Policy, RoleView, Runtime, Snapshot } from "./types"

function firstAvailable(chain: readonly string[], blocked: ReadonlySet<string>): string | undefined {
  return chain.find((entry) => !blocked.has(parseEntry(entry).id))
}

/**
 * The comparison form of a `model` or `model#variant` entry. The stage set is
 * stage → model, so only the id is compared: a variant is a resolution detail (a
 * load may strip an unsupported one), not a different stage model. `parseEntry`
 * also maps a trailing `#` to the bare id.
 */
function entryKey(entry: string): string {
  return parseEntry(entry).id
}

function resolveEntry(
  policy: Policy,
  entry: string | undefined,
  effort: string | undefined,
): { id: string; variant?: string } | undefined {
  if (!entry) return undefined
  const parsed = parseEntry(entry)
  if (!policy.models[parsed.id]) return undefined
  return { id: parsed.id, variant: parsed.variant ?? effortVariant(policy.models[parsed.id].variants, effort) }
}

function resolveRoles(
  policy: Policy,
  preset: string | undefined,
  runtime: Runtime,
  blocked: ReadonlySet<string>,
): RoleView[] {
  const chainByRole = preset ? policy.presets[preset] ?? {} : {}
  const fallbackByRole = policy.presets.default ?? {}
  // D15: a save or a load materializes the set as `roleOverrides`, so the rows
  // would otherwise all carry the manual-pin marker even though the preset row
  // names where they came from. The marker is only trusted per role: the raw
  // override must equal the active preset's saved entry.
  const activeUserEntries = runtime.activeUserPreset ? runtime.userPresets[runtime.activeUserPreset] : undefined

  return policy.roleOrder.map((name) => {
    const info = policy.roles[name] ?? { name, agents: [] }
    const chain = chainByRole[name] ?? []
    const rawOverride = runtime.roleOverrides[name]
    const override = resolveEntry(policy, rawOverride, info.effort)

    const base = {
      name,
      agents: info.agents,
      tier: info.tier,
      onExhaustion: info.onExhaustion,
      chain,
    }

    if (override) {
      const savedEntry = activeUserEntries?.[name]
      const fromUserPreset =
        savedEntry !== undefined &&
        rawOverride !== undefined &&
        entryKey(savedEntry) === entryKey(rawOverride)
      return {
        ...base,
        model: override.id,
        variant: override.variant,
        source: fromUserPreset ? ("user" as const) : ("override" as const),
        exhausted: blocked.has(override.id),
        starved: false,
      }
    }

    const available = firstAvailable(chain, blocked)
    const resolved = resolveEntry(policy, available, info.effort)
    if (resolved) {
      return { ...base, model: resolved.id, variant: resolved.variant, source: "preset" as const, exhausted: false, starved: false }
    }
    if (chain.length > 0) {
      const parsed = parseEntry(chain[0])
      return {
        ...base,
        model: parsed.id,
        variant: parsed.variant ?? effortVariant(policy.models[parsed.id]?.variants, info.effort),
        source: "preset" as const,
        exhausted: true,
        starved: true,
      }
    }

    const fallbackChain = preset === "default" ? [] : fallbackByRole[name] ?? []
    const fallback = resolveEntry(policy, firstAvailable(fallbackChain, blocked), info.effort)
    if (fallback) {
      return {
        ...base,
        chain: fallbackChain,
        model: fallback.id,
        variant: fallback.variant,
        source: "default" as const,
        exhausted: false,
        starved: false,
      }
    }

    return { ...base, model: undefined, source: "none" as const, exhausted: false, starved: false }
  })
}

/**
 * Whether the resolved set still matches the selected preset. A user preset is
 * matched role by role against its saved map (a role it did not capture may only
 * resolve through the chain); a policy preset is matched by the absence of manual
 * pins. `custom` is display-only: the runtime keeps the stored preset names.
 */
function isCustomSet(runtime: Runtime, roles: readonly RoleView[]): boolean {
  const saved = runtime.activeUserPreset ? runtime.userPresets[runtime.activeUserPreset] : undefined
  return roles.some((role) => {
    // A manual pin is the clearest divergence, with or without a user preset.
    if (role.source === "override") return true
    if (!saved) return false
    const savedEntry = saved[role.name]
    // A role the preset never captured, or one the load skipped because its
    // model left the catalogue, is not a manual change: the preset stays selected
    // and the load already reported the gap.
    if (!savedEntry || !role.model) return false
    return entryKey(roleEntry(role)!) !== entryKey(savedEntry)
  })
}

export function readSnapshot(ctx: any, sessionID?: string, now = Date.now()): Snapshot {
  const paths = resolvePaths(ctx, sessionID)
  const base: Snapshot = {
    ok: false,
    status: "no-policy",
    policyPath: paths.policy,
    runtimePath: paths.runtime,
    project: paths.project ? path.basename(paths.project.replace(/[\\/]+$/, "")) : undefined,
    runtime: { ...EMPTY_RUNTIME },
    presetSource: "none",
    presets: [],
    userPresets: [],
    customSet: false,
    roles: [],
    exhausted: [],
    log: [],
  }

  const policyRead = readPolicy(paths.policy)
  if (!policyRead.ok) {
    const missing = "missing" in policyRead && policyRead.missing === true
    return {
      ...base,
      status: missing ? "no-policy" : "policy-malformed",
      detail: policyRead.detail,
    }
  }

  const policy = policyRead.policy
  const runtimeRead = readDocument(paths.runtime)
  let runtime: Runtime
  if (runtimeRead.ok) {
    let parsed: unknown
    try {
      parsed = JSON.parse(runtimeRead.input)
    } catch (error: any) {
      return {
        ...base,
        policy,
        presets: policy.presetOrder,
        status: "runtime-malformed",
        detail: `not valid JSON: ${String(error?.message ?? error)}`,
      }
    }
    if (!isRecord(parsed)) {
      return {
        ...base,
        policy,
        presets: policy.presetOrder,
        status: "runtime-malformed",
        detail: "root is not an object",
      }
    }
    runtime = normalizeRuntime(parsed, now)
  } else if (runtimeRead.reason === "missing") {
    runtime = { ...EMPTY_RUNTIME }
  } else {
    return {
      ...base,
      policy,
      presets: policy.presetOrder,
      status: "runtime-unreadable",
      detail: runtimeRead.detail,
    }
  }

  const runtimePreset = declaredPreset(policy, runtime.activePreset)
  const policyPreset = declaredPreset(policy, policy.activePreset)
  const presetSource: Snapshot["presetSource"] = runtimePreset
    ? "runtime"
    : policyPreset
      ? "policy"
      : "none"
  const activePreset = runtimePreset ?? policyPreset
  const effective = effectivePreset(policy, activePreset, now)
  const exhausted = exhaustedModels(runtime, now)
  const blocked = new Set(exhausted.map((entry) => entry.model))

  const roles = resolveRoles(policy, effective, runtime, blocked)
  const customSet = isCustomSet(runtime, roles)

  return {
    ...base,
    ok: true,
    status: "ok",
    policy,
    runtime,
    activePreset,
    presetSource,
    effectivePreset: effective,
    presets: policy.presetOrder,
    activeUserPreset: runtime.activeUserPreset,
    userPresets: Object.keys(runtime.userPresets),
    customSet,
    roles,
    exhausted,
    log: runtime.log,
  }
}

/** Every model a role could be assigned, active-preset chain first. */
export function roleModels(snapshot: Snapshot, role: string): string[] {
  const models: string[] = []
  const add = (model: string | undefined) => {
    if (model && !models.includes(model)) models.push(model)
  }

  for (const model of snapshot.policy?.presets[snapshot.effectivePreset ?? ""]?.[role] ?? []) add(model)
  add(snapshot.runtime.roleOverrides[role])
  for (const preset of Object.values(snapshot.policy?.presets ?? {})) {
    for (const model of preset[role] ?? []) add(model)
  }
  for (const model of Object.keys(snapshot.policy?.models ?? {}).sort()) add(model)
  return models
}

export function describeModel(snapshot: Snapshot, model: string): string {
  const info = snapshot.policy?.models[model]
  if (!info) return "not in the policy catalogue"
  const parts: string[] = []
  if (info.billing) parts.push(info.billing)
  if (info.tier) parts.push(info.tier)
  if (info.limits) parts.push(info.limits)
  if (info.maxConcurrent !== undefined) parts.push(`max ${info.maxConcurrent}`)
  return parts.join(" · ") || "no catalogue detail"
}

/**
 * The display label of a model for a role: the short model id plus the effort
 * the role would run it with, e.g. `rf/cl-sonnet-5#h`.
 */
export function modelLabel(snapshot: Snapshot, model: string, role?: string): string {
  const rules = displayRules(snapshot.policy)
  const base = shortModel(model, rules)
  const effort = role ? snapshot.policy?.roles[role]?.effort : undefined
  const variant = effortVariant(snapshot.policy?.models[model]?.variants, effort)
  return variant ? `${base}#${shortVariant(variant, rules)}` : base
}

/**
 * The unabbreviated picker label: the full model id plus the full effort the
 * role would run it with, e.g. `relayfast/claude-sonnet-5#high`. A model the
 * policy does not catalogue has no effort mapping and renders as its id.
 */
export function modelLabelFull(snapshot: Snapshot, model: string, role?: string): string {
  const effort = role ? snapshot.policy?.roles[role]?.effort : undefined
  const variant = effortVariant(snapshot.policy?.models[model]?.variants, effort)
  return variant ? `${model}#${variant}` : model
}

/**
 * Maps a short display id back to the full id, e.g. `rf/cl-sonnet-5` ->
 * `relayfast/claude-sonnet-5`. A value that is already a full id, or that
 * matches nothing in the catalogue, is returned unchanged.
 */
export function expandModelId(snapshot: Snapshot, value: string): string {
  const models = snapshot.policy?.models ?? {}
  if (models[value]) return value
  const rules = displayRules(snapshot.policy)
  const hash = value.indexOf("#")
  const base = hash > 0 ? value.slice(0, hash) : value
  const variant = hash > 0 ? value.slice(hash + 1) : undefined
  for (const id of Object.keys(models)) {
    const slash = id.indexOf("/")
    const provider = id.slice(0, slash)
    const name = id.slice(slash + 1)
    const names = [shortModelName(name, rules)]
    if (rules.dropFreeSuffixFor.includes(provider.toLowerCase())) {
      names.push(shortModelName(name.replace(/-free$/, ""), rules))
    }
    if (shortModel(id, rules) === base || names.some((candidate) => `${provider}/${candidate}` === base)) {
      return variant ? `${id}#${variant}` : id
    }
  }
  return value
}

/** The name the sidebar and picker show; `custom` is display-only, never written. */
export function displayPreset(snapshot: Snapshot): string {
  if (snapshot.customSet) return "custom"
  return snapshot.activeUserPreset ?? snapshot.activePreset ?? "no preset"
}

export function describePreset(snapshot: Snapshot, preset: string): string {
  const chain = snapshot.policy?.presets[preset]
  if (!chain) return "not declared in the policy"
  const rules = displayRules(snapshot.policy)
  const parts = Object.entries(chain).map(([role, models]) => `${role}=${shortModel(models[0] ?? "-", rules)}`)
  return truncate(parts.join(" · "), 110)
}

/** The stored form of one resolved role: `model` or `model#variant`. */
export function roleEntry(role: RoleView): string | undefined {
  if (!role.model) return undefined
  return role.variant ? `${role.model}#${role.variant}` : role.model
}

/**
 * The current stage set as a saved-preset map. A role that resolves to nothing
 * is omitted — there is no override to store and no chain to replay — and named
 * in `unresolved` so the confirmation can say so.
 */
export function captureRoles(snapshot: Snapshot): { roles: Record<string, string>; unresolved: string[] } {
  const roles: Record<string, string> = {}
  const unresolved: string[] = []
  for (const role of snapshot.roles) {
    const entry = roleEntry(role)
    if (!entry) {
      unresolved.push(role.name)
      continue
    }
    roles[role.name] = entry
  }
  return { roles, unresolved }
}

export function describeRole(snapshot: Snapshot, role: RoleView): string {
  const parts: string[] = []
  if (role.tier) parts.push(`tier ${role.tier}`)
  if (role.onExhaustion) parts.push(`on exhaustion: ${role.onExhaustion}`)
  if (role.agents.length > 0) parts.push(role.agents.join(", "))
  if (role.source === "override") parts.push(`override (chain ${role.chain.length})`)
  else if (role.source === "user") parts.push("from the active user preset")
  else if (role.source === "default") parts.push("policy default chain")
  else if (role.source === "preset") parts.push(`chain ${role.chain.length}`)
  if (role.starved) parts.push("every model in the chain is exhausted")
  return parts.join(" · ") || "no policy detail"
}

export function reasonLine(snapshot: Snapshot): string {
  if (snapshot.status === "no-policy") return "policy file is missing"
  if (snapshot.status === "policy-unreadable") return "policy file is unreadable"
  if (snapshot.status === "policy-malformed") return "policy file is not usable"
  if (snapshot.status === "runtime-unreadable") return "runtime file is unreadable"
  if (snapshot.status === "runtime-malformed") return "runtime file is not usable"
  return "model policy is unavailable"
}
