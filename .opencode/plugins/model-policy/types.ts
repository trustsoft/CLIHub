// Model-policy types: the two JSON document shapes, the resolved snapshot view
// and the write results. Declarations only - no imports, no runtime statements.

export type SnapshotStatus =
  | "ok"
  | "no-policy"
  | "policy-unreadable"
  | "policy-malformed"
  | "runtime-unreadable"
  | "runtime-malformed"

export type PolicyModel = {
  billing?: string
  tier?: string
  limits?: string
  maxConcurrent?: number
  variants?: string[]
}

export type RoleDef = {
  name: string
  agents: string[]
  tier?: string
  effort?: string
  onExhaustion?: string
}

export type PolicyOverride = {
  preset: string
  validFrom?: string
  validUntil?: string
}

export type Policy = {
  version?: number
  activePreset?: string
  roles: Record<string, RoleDef>
  /** Display order: the declared roles, then every role a preset chain names. */
  roleOrder: string[]
  /**
   * The roles the policy document's `roles` block declares, captured before the
   * chain-only synthesis. This is the set `check-model-policy.mjs` validates an
   * override against, so it — not `roleOrder` — decides applicability.
   */
  declaredRoles: string[]
  models: Record<string, PolicyModel>
  presets: Record<string, Record<string, string[]>>
  presetOrder: string[]
  overrides: PolicyOverride[]
  display?: DisplayRules
}

export type LogEntry = {
  at?: string
  role?: string
  from?: string
  to?: string
  reason?: string
}

/** name → role → `model` or `model#variant`, the plugin-owned saved presets. */
export type UserPresets = Record<string, Record<string, string>>

export type Runtime = {
  schemaVersion?: number
  activePreset?: string
  activeUserPreset?: string
  userPresets: UserPresets
  roleOverrides: Record<string, string>
  exhaustedUntil: Record<string, string>
  log: LogEntry[]
  exists: boolean
}

export type ExhaustedModel = {
  model: string
  until: string
  remainingMs: number
}

export type RoleView = {
  name: string
  agents: string[]
  tier?: string
  onExhaustion?: string
  chain: string[]
  model?: string
  variant?: string
  /**
   * `user` is an override that came from the active user preset rather than a
   * manual pin: dispatch still reads `roleOverrides`, this is a display rule so
   * a preset-pinned role is not marked `override` (D15).
   */
  source: "override" | "user" | "preset" | "default" | "none"
  exhausted: boolean
  starved: boolean
}

export type Snapshot = {
  ok: boolean
  status: SnapshotStatus
  detail?: string
  project?: string
  policyPath: string
  runtimePath: string
  policy?: Policy
  runtime: Runtime
  activePreset?: string
  presetSource: "runtime" | "policy" | "none"
  effectivePreset?: string
  presets: string[]
  activeUserPreset?: string
  userPresets: string[]
  /** True when the resolved set matches no selectable preset (display only). */
  customSet: boolean
  roles: RoleView[]
  exhausted: ExhaustedModel[]
  log: LogEntry[]
}

export type WriteResult =
  | { ok: true; file: string; created: boolean }
  | { ok: false; file: string; detail: string }

export type Observation = {
  at: number
  agent?: string
  provider?: string
  model: string
  variant?: string
  sessionID?: string
}

export type ResolvedPaths = {
  project?: string
  policy: string
  runtime: string
}

export type PresetNameCheck =
  | { ok: true; name: string; replaces?: string }
  | { ok: false; detail: string }

export type DroppedPresetEntry = { role: string; reason: "role" | "model" }

export type SaveUserPresetResult =
  | {
      ok: true
      file: string
      name: string
      saved: number
      dropped: DroppedPresetEntry[]
      replaces?: string
    }
  | { ok: false; file: string; detail: string }

export type ApplyUserPresetResult =
  | {
      ok: true
      file: string
      name: string
      applied: Record<string, string>
      skipped: DroppedPresetEntry[]
      strippedVariants: string[]
    }
  | { ok: false; file: string; detail: string }

export type DeleteUserPresetResult =
  | { ok: true; file: string; removed: string; markerCleared: boolean }
  | { ok: false; file: string; detail: string }

// ── display rules ──────────────────────────────────────────────────────────
// The versioned policy's `display` section is the only source of the display
// abbreviations, so the panel and the documentation cannot drift apart. A policy
// without that section renders full ids: there is no built-in table to fall back
// to. Nothing here is ever written: the full model id stays in the data.

export type DisplayRules = {
  providers: Record<string, string>
  models: Record<string, string>
  variants: Record<string, string>
  roles: Record<string, string>
  dropFreeSuffixFor: string[]
}
