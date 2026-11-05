// Model-policy layer 2: reading the runtime document and writing the four
// plugin-owned fields back merge-safely. The orchestrator owns the other fields.

import { atomicWriteJson, readDocument, readPolicy } from "./io"
import { resolvePaths } from "./paths"
import { isRecord, iso, pick, stringRecord, text } from "./util"
import type { ExhaustedModel, LogEntry, Policy, Runtime, UserPresets, WriteResult } from "./types"

const LOG_LIMIT = 20

export const EMPTY_RUNTIME: Runtime = {
  userPresets: {},
  roleOverrides: {},
  exhaustedUntil: {},
  log: [],
  exists: false,
}

/**
 * Reads `userPresets` leniently: a name whose value is not an object, and a role
 * whose entry is not text, are skipped instead of failing the whole document.
 * Keys are not NFC-normalized here — the write path normalizes, and the name
 * comparisons normalize both sides, so a hand-edited NFD key is still matched.
 */
export function normalizeUserPresets(value: unknown): UserPresets {
  if (!isRecord(value)) return {}
  const result: UserPresets = {}
  for (const [key, item] of Object.entries(value)) {
    const name = text(key, 80)
    if (!name || !isRecord(item)) continue
    const roles: Record<string, string> = {}
    for (const [role, model] of Object.entries(item)) {
      const roleName = role.trim()
      const entry = text(model, 200)
      if (roleName && entry) roles[roleName] = entry
    }
    if (Object.keys(roles).length === 0) continue
    result[name] = roles
  }
  return result
}

export function normalizeRuntime(raw: unknown, now: number): Runtime {
  if (!isRecord(raw)) return { ...EMPTY_RUNTIME }
  const exhaustedUntil = stringRecord(pick(raw, "exhaustedUntil", "exhausted_until"))
  const userPresets = normalizeUserPresets(pick(raw, "userPresets", "user_presets"))
  // A marker that names no saved preset would dangle in the sidebar and in the
  // picker, so it is dropped here, where both values are in scope.
  const storedMarker = text(pick(raw, "activeUserPreset", "active_user_preset"), 80)
  const activeUserPreset = storedMarker && userPresets[storedMarker] ? storedMarker : undefined
  const logRaw = pick(raw, "sessionLog", "session_log")
  const log: LogEntry[] = []
  if (Array.isArray(logRaw)) {
    for (const item of logRaw) {
      if (!isRecord(item)) continue
      log.push({
        at: iso(item.at),
        role: text(item.role, 40),
        from: text(item.from, 200),
        to: text(item.to, 200),
        reason: text(item.reason, 120),
      })
    }
  }

  return {
    schemaVersion: typeof raw.schemaVersion === "number" ? raw.schemaVersion : undefined,
    activePreset: text(pick(raw, "activePreset", "active_preset"), 80),
    activeUserPreset,
    userPresets,
    roleOverrides: stringRecord(pick(raw, "roleOverrides", "role_overrides")),
    exhaustedUntil,
    log: log.reverse().slice(0, LOG_LIMIT),
    exists: true,
  }
}

export function exhaustedModels(runtime: Runtime, now = Date.now()): ExhaustedModel[] {
  const result: ExhaustedModel[] = []
  for (const [model, until] of Object.entries(runtime.exhaustedUntil)) {
    const at = Date.parse(until)
    const remainingMs = at - now
    if (!Number.isFinite(remainingMs) || remainingMs <= 0) continue
    result.push({ model, until, remainingMs })
  }
  result.sort((left, right) => left.remainingMs - right.remainingMs)
  return result
}

export function defaultRuntimeDoc(policy: Policy | undefined): Record<string, unknown> {
  const doc: Record<string, unknown> = { schemaVersion: 1 }
  if (policy?.activePreset) doc.activePreset = policy.activePreset
  doc.userPresets = {}
  doc.roleOverrides = {}
  doc.exhaustedUntil = {}
  doc.sessionLog = []
  return doc
}

type OwnedMutation = (doc: Record<string, unknown>) => string | undefined

/**
 * Reads the runtime document, applies an owned-field mutation, and writes it
 * back atomically. Unparsable documents are never overwritten: the orchestrator
 * owns fields we cannot reconstruct, so recovery is an explicit user action.
 */
export function mutateOwned(ctx: any, sessionID: string | undefined, mutate: OwnedMutation): WriteResult {
  const paths = resolvePaths(ctx, sessionID)
  const read = readDocument(paths.runtime)

  let doc: Record<string, unknown>
  if (read.ok) {
    let parsed: unknown
    try {
      parsed = JSON.parse(read.input)
    } catch (error: any) {
      return {
        ok: false,
        file: paths.runtime,
        detail: `runtime file is not valid JSON (${String(error?.message ?? error)}); fix or recreate it`,
      }
    }
    if (!isRecord(parsed)) {
      return { ok: false, file: paths.runtime, detail: "runtime file root is not an object; fix or recreate it" }
    }
    doc = parsed
  } else if (read.reason === "missing") {
    const policyRead = readPolicy(paths.policy)
    if (!policyRead.ok) {
      return { ok: false, file: paths.runtime, detail: `policy is unavailable: ${policyRead.detail}` }
    }
    doc = defaultRuntimeDoc(policyRead.policy)
  } else {
    return { ok: false, file: paths.runtime, detail: read.detail }
  }

  const problem = mutate(doc)
  if (problem) return { ok: false, file: paths.runtime, detail: problem }

  if (typeof doc.schemaVersion !== "number") doc.schemaVersion = 1
  try {
    atomicWriteJson(paths.runtime, doc)
  } catch (error: any) {
    return { ok: false, file: paths.runtime, detail: String(error?.message ?? error) }
  }
  return { ok: true, file: paths.runtime, created: !read.ok }
}

/** Creates the runtime file with policy-derived defaults when it is missing. */
export function ensureRuntime(ctx: any, sessionID?: string): WriteResult {
  const paths = resolvePaths(ctx, sessionID)
  const read = readDocument(paths.runtime)
  if (read.ok) return { ok: true, file: paths.runtime, created: false }
  if (read.reason === "unreadable") return { ok: false, file: paths.runtime, detail: read.detail }

  const policyRead = readPolicy(paths.policy)
  if (!policyRead.ok) {
    return { ok: false, file: paths.runtime, detail: `policy is unavailable: ${policyRead.detail}` }
  }

  try {
    atomicWriteJson(paths.runtime, defaultRuntimeDoc(policyRead.policy))
  } catch (error: any) {
    return { ok: false, file: paths.runtime, detail: String(error?.message ?? error) }
  }
  return { ok: true, file: paths.runtime, created: true }
}

/** Writes `activePreset` only; orchestrator fields are preserved. */
export function setActivePreset(ctx: any, sessionID: string | undefined, preset: string): WriteResult {
  const name = text(preset, 80)
  if (!name) return { ok: false, file: resolvePaths(ctx, sessionID).runtime, detail: "preset is empty" }

  const paths = resolvePaths(ctx, sessionID)
  const policyRead = readPolicy(paths.policy)
  if (!policyRead.ok) {
    return { ok: false, file: paths.runtime, detail: `policy is unavailable: ${policyRead.detail}` }
  }
  if (!policyRead.policy.presets[name]) {
    return { ok: false, file: paths.runtime, detail: `preset "${name}" is not declared in the policy` }
  }

  return mutateOwned(ctx, sessionID, (doc) => {
    // Choosing a preset means applying it: the pins in force (from a user preset
    // or written by hand) would outrank the chain, so they go with the marker in
    // the same write. This is what makes the picker's choice visible instead of
    // leaving a `custom` set in place.
    doc.activePreset = name
    delete doc.active_preset
    doc.roleOverrides = {}
    delete doc.role_overrides
    delete doc.activeUserPreset
    delete doc.active_user_preset
    return undefined
  })
}

/** Writes or removes one `roleOverrides` entry; other keys are preserved. */
export function setRoleOverride(
  ctx: any,
  sessionID: string | undefined,
  role: string,
  model: string | undefined,
): WriteResult {
  const roleName = text(role, 64)
  const paths = resolvePaths(ctx, sessionID)
  if (!roleName) return { ok: false, file: paths.runtime, detail: "role is empty" }

  const policyRead = readPolicy(paths.policy)
  if (!policyRead.ok) {
    return { ok: false, file: paths.runtime, detail: `policy is unavailable: ${policyRead.detail}` }
  }
  if (!policyRead.policy.roleOrder.includes(roleName)) {
    return { ok: false, file: paths.runtime, detail: `role "${roleName}" is not declared in the policy` }
  }

  const modelName = model === undefined ? undefined : text(model, 200)
  if (model !== undefined && !modelName) {
    return { ok: false, file: paths.runtime, detail: "model is empty" }
  }

  return mutateOwned(ctx, sessionID, (doc) => {
    const overrides = stringRecord(doc.roleOverrides ?? doc.role_overrides)
    if (modelName) overrides[roleName] = modelName
    else delete overrides[roleName]
    doc.roleOverrides = overrides
    delete doc.role_overrides
    // The marker stays: the changed role no longer equals its saved entry, so D15
    // shows `override` for this row while every untouched role remains the preset's.
    // Clearing it here made all the other pins look manual (the smoke report).
    return undefined
  })
}

export function clearRoleOverrides(ctx: any, sessionID: string | undefined): WriteResult {
  return mutateOwned(ctx, sessionID, (doc) => {
    doc.roleOverrides = {}
    delete doc.role_overrides
    delete doc.activeUserPreset
    delete doc.active_user_preset
    return undefined
  })
}

/**
 * Replaces a malformed runtime file with policy-derived defaults. This is the
 * only path that discards orchestrator-owned fields, so it is user-confirmed.
 */
export function recreateRuntime(ctx: any, sessionID: string | undefined): WriteResult {
  const paths = resolvePaths(ctx, sessionID)
  const policyRead = readPolicy(paths.policy)
  if (!policyRead.ok) {
    return { ok: false, file: paths.runtime, detail: `policy is unavailable: ${policyRead.detail}` }
  }
  try {
    atomicWriteJson(paths.runtime, defaultRuntimeDoc(policyRead.policy))
  } catch (error: any) {
    return { ok: false, file: paths.runtime, detail: String(error?.message ?? error) }
  }
  return { ok: true, file: paths.runtime, created: true }
}
