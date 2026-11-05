// Model-policy layer 1: parsing the curated policy document and resolving which
// preset dispatch uses. Pure functions over plain objects - no I/O.

import { isRecord, iso, pick, stringRecord, text } from "./util"
import type { DisplayRules, Policy, PolicyModel, PolicyOverride, RoleDef } from "./types"

function normalizePolicyOverrides(raw: unknown): PolicyOverride[] {
  if (!Array.isArray(raw)) return []
  const result: PolicyOverride[] = []
  for (const item of raw) {
    if (!isRecord(item)) continue
    const preset = text(pick(item, "preset", "name"), 80)
    if (!preset) continue
    result.push({
      preset,
      validFrom: iso(pick(item, "valid_from", "validFrom")),
      validUntil: iso(pick(item, "valid_until", "validUntil")),
    })
  }
  return result
}

export function normalizePolicy(raw: unknown): { ok: true; policy: Policy } | { ok: false; detail: string } {
  if (!isRecord(raw)) return { ok: false, detail: "root is not an object" }

  const presetsRaw = raw.presets
  if (!isRecord(presetsRaw)) return { ok: false, detail: "presets is not an object" }

  const presets: Record<string, Record<string, string[]>> = {}
  for (const [presetName, chainRaw] of Object.entries(presetsRaw)) {
    if (!isRecord(chainRaw)) continue
    const chain: Record<string, string[]> = {}
    for (const [roleName, modelsRaw] of Object.entries(chainRaw)) {
      const list = Array.isArray(modelsRaw) ? modelsRaw : [modelsRaw]
      const models = list.map((model) => text(model, 200)).filter((model): model is string => Boolean(model))
      if (models.length > 0) chain[roleName.trim()] = models
    }
    presets[presetName.trim()] = chain
  }

  const presetOrder = Object.keys(presets)
  if (presetOrder.length === 0) return { ok: false, detail: "presets is empty" }

  const rolesRaw = isRecord(raw.roles) ? raw.roles : {}
  const roles: Record<string, RoleDef> = {}
  const roleOrder: string[] = []
  for (const [roleName, infoRaw] of Object.entries(rolesRaw)) {
    const name = roleName.trim()
    if (!name) continue
    const info = isRecord(infoRaw) ? infoRaw : {}
    const agentsRaw = info.agents
    const agents = (Array.isArray(agentsRaw) ? agentsRaw : [agentsRaw])
      .map((agent) => text(agent, 120))
      .filter((agent): agent is string => Boolean(agent))
    roles[name] = {
      name,
      agents,
      tier: text(info.tier, 40),
      effort: text(info.effort, 20),
      onExhaustion: text(pick(info, "on_exhaustion", "onExhaustion"), 60),
    }
    roleOrder.push(name)
  }
  // Captured before the synthesis below, which appends chain-only roles to
  // `roles` and `roleOrder` as well: only the roles recorded here are the ones
  // the policy check accepts an override for.
  const declaredRoles = [...roleOrder]
  for (const chain of Object.values(presets)) {
    for (const roleName of Object.keys(chain)) {
      if (roleOrder.includes(roleName)) continue
      roleOrder.push(roleName)
      roles[roleName] = { name: roleName, agents: [] }
    }
  }

  const modelsRaw = isRecord(raw.models) ? raw.models : {}
  const models: Record<string, PolicyModel> = {}
  for (const [modelName, infoRaw] of Object.entries(modelsRaw)) {
    const name = modelName.trim()
    if (!name) continue
    const info = isRecord(infoRaw) ? infoRaw : {}
    models[name] = {
      billing: text(info.billing, 40),
      tier: text(info.tier, 40),
      limits: text(info.limits, 80),
      maxConcurrent: typeof info.max_concurrent === "number" ? info.max_concurrent : undefined,
      variants: Array.isArray(info.variants)
        ? info.variants.map((variant) => text(variant, 20)).filter((variant): variant is string => Boolean(variant))
        : [],
    }
  }

  const version = typeof raw.version === "number" ? raw.version : undefined
  const displayRaw = isRecord(raw.display) ? raw.display : undefined
  const display: DisplayRules | undefined = displayRaw
    ? {
        providers: stringRecord(displayRaw.providers),
        models: stringRecord(displayRaw.models),
        variants: stringRecord(displayRaw.variants),
        roles: stringRecord(displayRaw.roles),
        dropFreeSuffixFor: Array.isArray(pick(displayRaw, "drop_free_suffix_for", "dropFreeSuffixFor"))
          ? (pick(displayRaw, "drop_free_suffix_for", "dropFreeSuffixFor") as unknown[])
              .map((value) => text(value, 40))
              .filter((value): value is string => Boolean(value))
          : [],
      }
    : undefined
  return {
    ok: true,
    policy: {
      version,
      activePreset: text(pick(raw, "active_preset", "activePreset"), 80),
      roles,
      roleOrder,
      declaredRoles,
      models,
      presets,
      presetOrder,
      overrides: normalizePolicyOverrides(raw.overrides),
      display,
    },
  }
}

/**
 * A preset name counts only when the policy declares it. The policy is the only
 * document that defines the chains, so a name it does not declare (a runtime
 * hand edit, or a preset the policy dropped) cannot select one.
 */
export function declaredPreset(policy: Policy | undefined, name: string | undefined): string | undefined {
  return name && policy?.presets[name] ? name : undefined
}

/**
 * The preset dispatch actually uses: the first override window that contains
 * `now`, otherwise the stored runtime selection when the policy declares it,
 * otherwise the policy's declared active preset, otherwise the first preset in
 * `presets`.
 */
export function effectivePreset(
  policy: Policy | undefined,
  stored: string | undefined,
  now = Date.now(),
): string | undefined {
  if (!policy) return undefined
  for (const entry of policy.overrides) {
    const from = entry.validFrom ? Date.parse(entry.validFrom) : Number.NEGATIVE_INFINITY
    const until = entry.validUntil ? Date.parse(entry.validUntil) : Number.POSITIVE_INFINITY
    if (Number.isNaN(from) || Number.isNaN(until)) continue
    if (now >= from && now < until && policy.presets[entry.preset]) return entry.preset
  }
  return (
    declaredPreset(policy, stored) ??
    declaredPreset(policy, policy.activePreset) ??
    policy.presetOrder[0]
  )
}

const EFFORT_RANK: Record<string, number> = { none: 0, minimal: 1, low: 2, medium: 3, high: 4, xhigh: 5, max: 6 }

export function parseEntry(entry: string): { id: string; variant?: string } {
  const hash = entry.indexOf("#")
  return hash <= 0 ? { id: entry } : { id: entry.slice(0, hash), variant: entry.slice(hash + 1) || undefined }
}

// Mirrors .opencode/scripts/resolve-model.mjs — keep in sync.
export function effortVariant(variants: string[] | undefined, effort: string | undefined): string | undefined {
  const list = (variants ?? []).filter((variant) => EFFORT_RANK[variant] !== undefined)
  if (list.length === 0) return undefined
  if (!effort || effort === "off") return list.includes("none") ? "none" : undefined
  const target = EFFORT_RANK[effort]
  if (target === undefined) return undefined
  const atOrAbove = list.filter((variant) => EFFORT_RANK[variant] >= target)
  if (atOrAbove.length > 0) return atOrAbove.reduce((best, v) => (EFFORT_RANK[v] < EFFORT_RANK[best] ? v : best))
  return list.reduce((best, v) => (EFFORT_RANK[v] > EFFORT_RANK[best] ? v : best))
}
