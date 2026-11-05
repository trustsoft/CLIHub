// Model-policy layer 5: the three user-preset writers and the description of a
// saved preset. Every write goes through `mutateOwned` in `runtime.ts`.

import { readSnapshot, reasonLine } from "./snapshot"
import { mutateOwned, normalizeUserPresets } from "./runtime"
import { parseEntry } from "./policy"
import { checkPresetName, nameKey } from "./preset-name"
import { displayRules, shortModel, truncate } from "./display"
import { resolvePaths } from "./paths"
import { pick, text } from "./util"
import type {
  ApplyUserPresetResult,
  DeleteUserPresetResult,
  DroppedPresetEntry,
  SaveUserPresetResult,
  Snapshot,
} from "./types"

/**
 * Stores one role → model map as `userPresets[<name>]` and makes it the current
 * set: `roleOverrides` becomes that same map and `activeUserPreset` the saved name,
 * all in one merge-safe write. `activePreset` is never assigned, so it still names
 * a preset the policy declares — the CLIHub policy check requires that.
 */
export function saveUserPreset(
  ctx: any,
  sessionID: string | undefined,
  rawName: unknown,
  roles: Record<string, string>,
): SaveUserPresetResult {
  const paths = resolvePaths(ctx, sessionID)
  const snapshot = readSnapshot(ctx, sessionID)
  if (!snapshot.ok || !snapshot.policy) {
    return { ok: false, file: paths.runtime, detail: reasonLine(snapshot) }
  }

  const checked = checkPresetName(snapshot, rawName)
  if (!checked.ok) return { ok: false, file: paths.runtime, detail: checked.detail }

  const policy = snapshot.policy
  const kept: Record<string, string> = {}
  const dropped: DroppedPresetEntry[] = []
  for (const [rawRole, rawModel] of Object.entries(roles ?? {})) {
    const role = rawRole.trim()
    const entry = text(rawModel, 200)
    if (!role || !entry) continue
    if (!policy.declaredRoles.includes(role)) {
      dropped.push({ role, reason: "role" })
      continue
    }
    if (!policy.models[parseEntry(entry).id]) {
      dropped.push({ role, reason: "model" })
      continue
    }
    kept[role] = entry
  }
  if (Object.keys(kept).length === 0) {
    return { ok: false, file: paths.runtime, detail: "no role resolves to a model, so nothing was saved" }
  }

  const key = nameKey(checked.name)
  const result = mutateOwned(ctx, sessionID, (doc) => {
    const stored = normalizeUserPresets(pick(doc, "userPresets", "user_presets"))
    // Every raw key that compares equal is removed, so a hand-edited NFD or
    // differently-cased duplicate cannot survive as a second row.
    for (const existing of Object.keys(stored)) {
      if (nameKey(existing) === key) delete stored[existing]
    }
    stored[checked.name] = kept
    doc.userPresets = stored
    delete doc.user_presets
    // The same write makes the stored set current: the override map is replaced
    // (not merged) by exactly the entries just saved, so a pin outside the saved
    // set is dropped and the marker describes the map that is in force. The old
    // "an overwrite of the active preset clears the marker" branch is gone: the
    // marker is still true after an overwrite, because both maps were replaced by
    // the new set.
    doc.roleOverrides = kept
    delete doc.role_overrides
    doc.activeUserPreset = checked.name
    delete doc.active_user_preset
    return undefined
  })
  if (!result.ok) return { ok: false, file: result.file, detail: result.detail }

  return {
    ok: true,
    file: result.file,
    name: checked.name,
    saved: Object.keys(kept).length,
    dropped,
    replaces: checked.replaces,
  }
}

/**
 * Materializes a saved preset into `roleOverrides` in one merge-safe write and
 * marks it active. The override map is replaced, not merged; `activePreset` is
 * never touched, so it still names a preset the policy declares.
 */
export function applyUserPreset(
  ctx: any,
  sessionID: string | undefined,
  rawName: string,
): ApplyUserPresetResult {
  const paths = resolvePaths(ctx, sessionID)
  const name = text(rawName, 80)
  if (!name) return { ok: false, file: paths.runtime, detail: "preset is empty" }

  const snapshot = readSnapshot(ctx, sessionID)
  if (!snapshot.ok || !snapshot.policy) {
    return { ok: false, file: paths.runtime, detail: reasonLine(snapshot) }
  }
  const saved = snapshot.runtime.userPresets[name]
  if (!saved) return { ok: false, file: paths.runtime, detail: `"${name}" is not a saved preset` }

  const policy = snapshot.policy
  const applied: Record<string, string> = {}
  const skipped: DroppedPresetEntry[] = []
  const strippedVariants: string[] = []
  for (const [role, entry] of Object.entries(saved)) {
    // "Declared" is the policy's `roles` block: a role only a preset chain names
    // renders but has no declaration the policy check accepts an override for.
    if (!policy.declaredRoles.includes(role)) {
      skipped.push({ role, reason: "role" })
      continue
    }
    const parsed = parseEntry(entry)
    const info = policy.models[parsed.id]
    if (!info) {
      skipped.push({ role, reason: "model" })
      continue
    }
    if (parsed.variant && !(info.variants ?? []).includes(parsed.variant)) {
      strippedVariants.push(role)
      applied[role] = parsed.id
      continue
    }
    // A supported variant keeps the saved spelling; no variant after parsing (a
    // trailing `#`, or a bare model) is written as the bare id, because the
    // policy check reads `model#` as an empty variant and fails on it.
    applied[role] = parsed.variant ? entry : parsed.id
  }
  if (Object.keys(applied).length === 0) {
    return {
      ok: false,
      file: paths.runtime,
      detail: `"${name}" has no entry the current policy can use, so nothing was applied`,
    }
  }

  const result = mutateOwned(ctx, sessionID, (doc) => {
    doc.roleOverrides = applied
    delete doc.role_overrides
    doc.activeUserPreset = name
    delete doc.active_user_preset
    return undefined
  })
  if (!result.ok) return { ok: false, file: result.file, detail: result.detail }

  return { ok: true, file: result.file, name, applied, skipped, strippedVariants }
}

/**
 * Removes one saved preset in a single merge-safe write. The role overrides the
 * preset applied stay in place; only the marker goes when it named this preset.
 */
export function deleteUserPreset(
  ctx: any,
  sessionID: string | undefined,
  rawName: string,
): DeleteUserPresetResult {
  const paths = resolvePaths(ctx, sessionID)
  const name = text(rawName, 80)
  if (!name) return { ok: false, file: paths.runtime, detail: "preset is empty" }

  const key = nameKey(name)
  let removed: string | undefined
  let markerCleared = false
  const result = mutateOwned(ctx, sessionID, (doc) => {
    const stored = normalizeUserPresets(pick(doc, "userPresets", "user_presets"))
    const matches = Object.keys(stored).filter((existing) => nameKey(existing) === key)
    if (matches.length === 0) return `"${name}" is not a saved preset`
    for (const match of matches) {
      delete stored[match]
      removed = match
    }
    doc.userPresets = stored
    delete doc.user_presets
    const marker = text(pick(doc, "activeUserPreset", "active_user_preset"), 80)
    if (marker && nameKey(marker) === key) {
      delete doc.activeUserPreset
      delete doc.active_user_preset
      markerCleared = true
    }
    return undefined
  })
  if (!result.ok) return { ok: false, file: result.file, detail: result.detail }

  return { ok: true, file: result.file, removed: removed ?? name, markerCleared }
}

/** `role=shortModel` for a saved preset, the same shape as `describePreset`. */
export function describeUserPreset(snapshot: Snapshot, name: string): string {
  const saved = snapshot.runtime.userPresets[name]
  if (!saved) return "not a saved preset"
  const rules = displayRules(snapshot.policy)
  const parts = Object.entries(saved).map(([role, entry]) => `${role}=${shortModel(entry, rules)}`)
  return truncate(parts.join(" · "), 110)
}
