// Pipeline state: path resolution, defensive loading, and schema normalization.
// This module deliberately has no Solid/JSX dependency so it can be exercised
// outside the TUI (see README "Verification").

import { existsSync, readFileSync, statSync } from "node:fs"
import { homedir } from "node:os"
import path from "node:path"
import { fileURLToPath } from "node:url"

export type Stage = "plan" | "impl" | "commit" | "archive"
export type Status = "pending" | "active" | "done" | "blocked"

export type StageState = {
  status: Status
  at?: string
  commit?: string
  note?: string
}

export type Change = {
  id: string
  stage: Stage
  status: Status
  updatedAt?: string
  commits: Partial<Record<Stage, string>>
  stages: Partial<Record<Stage, StageState>>
  note?: string
}

export type Snapshot = {
  ok: boolean
  reason?: "missing" | "unreadable" | "malformed" | "empty" | "invalid"
  detail?: string
  path: string
  project?: string
  updatedAt?: string
  changes: Change[]
}

export const STAGES: readonly Stage[] = ["plan", "impl", "commit", "archive"]
export const STATUSES: readonly Status[] = ["pending", "active", "done", "blocked"]

/** Single-letter stage labels shared by the sidebar header counts and the sidebar change rows. */
export const STAGE_LETTERS: Record<Stage, string> = { plan: "p", impl: "i", commit: "c", archive: "a" }

const cache = new Map<string, { key: string; snapshot: Snapshot }>()

function emptySnapshot(file: string, reason: Snapshot["reason"], detail?: string, project?: string): Snapshot {
  return { ok: false, reason, detail, path: file, project, changes: [] }
}

function text(value: unknown, max = 600): string | undefined {
  if (typeof value !== "string") return undefined
  const flat = value.replace(/\s+/g, " ").trim()
  if (!flat) return undefined
  return flat.length > max ? flat.slice(0, max - 1) + "…" : flat
}

function iso(value: unknown): string | undefined {
  const raw = text(value, 80)
  if (!raw) return undefined
  return Number.isNaN(Date.parse(raw)) ? undefined : raw
}

function sha(value: unknown): string | undefined {
  const raw = text(value, 64)
  if (!raw) return undefined
  return /^[0-9a-f]{4,40}$/i.test(raw) ? raw : undefined
}

function stageOf(value: unknown): Stage | undefined {
  if (typeof value !== "string") return undefined
  const normalized = value.trim().toLowerCase()
  return (STAGES as readonly string[]).includes(normalized) ? (normalized as Stage) : undefined
}

function statusOf(value: unknown): Status | undefined {
  if (typeof value !== "string") return undefined
  const normalized = value.trim().toLowerCase()
  return (STATUSES as readonly string[]).includes(normalized) ? (normalized as Status) : undefined
}

function normalizeChange(raw: unknown): Change | undefined {
  if (!raw || typeof raw !== "object") return undefined
  const source = raw as Record<string, unknown>
  const id = text(source.id, 120)
  const stage = stageOf(source.stage)
  if (!id || !stage) return undefined

  const status = statusOf(source.status) ?? "pending"
  const commits: Partial<Record<Stage, string>> = {}
  if (source.commits && typeof source.commits === "object") {
    for (const stageName of STAGES) {
      const commit = sha((source.commits as Record<string, unknown>)[stageName])
      if (commit) commits[stageName] = commit
    }
  }

  const stages: Partial<Record<Stage, StageState>> = {}
  if (source.stages && typeof source.stages === "object") {
    for (const stageName of STAGES) {
      const rawStage = (source.stages as Record<string, unknown>)[stageName]
      if (!rawStage || typeof rawStage !== "object") continue
      const stageSource = rawStage as Record<string, unknown>
      const stageIndex = STAGES.indexOf(stageName)
      const currentIndex = STAGES.indexOf(stage)
      const fallbackStatus: Status =
        stageIndex < currentIndex ? "done" : stageIndex === currentIndex ? status : "pending"
      stages[stageName] = {
        status: statusOf(stageSource.status) ?? fallbackStatus,
        at: iso(stageSource.at ?? stageSource.timestamp ?? stageSource.updatedAt),
        commit: sha(stageSource.commit) ?? commits[stageName],
        note: text(stageSource.note ?? stageSource.validation),
      }
    }
  }

  return {
    id,
    stage,
    status,
    updatedAt: iso(source.updatedAt),
    commits,
    stages,
    note: text(source.note),
  }
}

export function parseState(input: string, file: string, project?: string): Snapshot {
  let root: unknown
  try {
    root = JSON.parse(input)
  } catch (error) {
    return emptySnapshot(file, "malformed", error instanceof Error ? error.message : String(error), project)
  }

  if (!root || typeof root !== "object") return emptySnapshot(file, "invalid", "root is not an object", project)
  const source = root as Record<string, unknown>
  if (!Array.isArray(source.changes)) return emptySnapshot(file, "invalid", "changes is not an array", project)

  const changes: Change[] = []
  const seen = new Set<string>()
  for (const raw of source.changes) {
    const change = normalizeChange(raw)
    if (!change || seen.has(change.id)) continue
    seen.add(change.id)
    changes.push(change)
  }

  if (changes.length === 0) {
    const reason = source.changes.length === 0 ? "empty" : "invalid"
    return emptySnapshot(file, reason, reason === "empty" ? "changes is empty" : "no valid change entries", project)
  }

  return {
    ok: true,
    path: file,
    project: text(source.project, 120) ?? project,
    updatedAt: iso(source.updatedAt),
    changes,
  }
}

export function configRoot(): string {
  try {
    const self = fileURLToPath(import.meta.url)
    const root = path.resolve(path.dirname(self), "..", "..")
    if (existsSync(path.join(root, "plugins"))) return root
  } catch {
    // Fall through to the home-relative convention.
  }
  return path.join(homedir(), ".config", "opencode")
}

export function slugForProject(project: string | undefined): string {
  if (!project) return "default"
  const base = path.basename(project.replace(/[\\/]+$/, ""))
  const slug = base
    .replace(/[^A-Za-z0-9._-]+/g, "_")
    .replace(/^[._-]+|[._-]+$/g, "")
    .slice(0, 60)
  return slug || "default"
}

function firstString(...values: unknown[]): string | undefined {
  for (const value of values) {
    if (typeof value === "string" && value.trim()) return value.trim()
  }
  return undefined
}

export function projectDirectory(ctx: any, sessionID: string): string | undefined {
  let session: any
  try {
    session = ctx?.data?.session?.get?.(sessionID)
  } catch {
    session = undefined
  }

  let defaultLocation: any
  try {
    defaultLocation = ctx?.data?.location?.default?.()
  } catch {
    defaultLocation = undefined
  }

  return firstString(
    session?.location?.directory,
    session?.directory,
    session?.path?.directory,
    ctx?.location?.directory,
    ctx?.location?.project?.directory,
    defaultLocation?.directory,
    defaultLocation?.project?.directory,
    (() => {
      try {
        return process.cwd()
      } catch {
        return undefined
      }
    })(),
  )
}

export type ResolvedState = {
  file: string
  project?: string
  candidates: string[]
}

export function resolveStateFile(ctx: any, sessionID: string): ResolvedState {
  const project = projectDirectory(ctx, sessionID)
  const override = process.env.OPENCODE_PIPELINE_FILE?.trim()
  const root = configRoot()
  const local = project ? path.join(project, ".opencode", "pipeline.json") : undefined
  const mapped = path.join(root, "pipeline", `${slugForProject(project)}.json`)
  const fallback = path.join(root, "pipeline", "default.json")

  if (override) return { file: override, project, candidates: [override] }

  const candidates = [local, mapped, fallback].filter((value): value is string => Boolean(value))
  const existing = candidates.find((candidate) => {
    try {
      return statSync(candidate).isFile()
    } catch {
      return false
    }
  })

  return { file: existing ?? mapped, project, candidates }
}

export function readSnapshot(ctx: any, sessionID: string): Snapshot {
  const resolved = resolveStateFile(ctx, sessionID)
  const projectName = resolved.project
    ? path.basename(resolved.project.replace(/[\\/]+$/, ""))
    : undefined
  try {
    const stat = statSync(resolved.file)
    if (!stat.isFile()) return emptySnapshot(resolved.file, "unreadable", "not a regular file", projectName)

    const key = `${stat.mtimeMs}:${stat.size}:${stat.ino}`
    const hit = cache.get(resolved.file)
    if (hit && hit.key === key) return hit.snapshot

    const snapshot = parseState(readFileSync(resolved.file, "utf8").replace(/^\uFEFF/, ""), resolved.file, projectName)
    if (snapshot.ok) cache.set(resolved.file, { key, snapshot })
    else cache.delete(resolved.file)
    return snapshot
  } catch (error: any) {
    cache.delete(resolved.file)
    const reason = error?.code === "ENOENT" ? "missing" : "unreadable"
    return emptySnapshot(resolved.file, reason, error?.message ? String(error.message) : String(error), projectName)
  }
}

export function stageState(change: Change, stage: Stage): StageState {
  const explicit = change.stages[stage]
  if (explicit) return explicit

  const currentIndex = STAGES.indexOf(change.stage)
  const stageIndex = STAGES.indexOf(stage)
  const status: Status =
    stageIndex < currentIndex ? "done" : stageIndex === currentIndex ? change.status : "pending"

  return {
    status,
    at: stage === change.stage ? change.updatedAt : undefined,
    commit: change.commits[stage],
  }
}

export function stageRows(change: Change): Array<{ stage: Stage; status: Status; at?: string; commit?: string; note?: string }> {
  return STAGES.map((stage) => {
    const state = stageState(change, stage)
    return {
      stage,
      status: state.status,
      at: state.at,
      commit: state.commit ?? change.commits[stage],
      note: state.note,
    }
  })
}

/** Number of changes whose current stage is each pipeline stage. */
export function stageCounts(changes: Change[]): Record<Stage, number> {
  const counts: Record<Stage, number> = { plan: 0, impl: 0, commit: 0, archive: 0 }
  for (const change of changes) {
    counts[change.stage] += 1
  }
  return counts
}

/**
 * Non-zero stage-count segments for the sidebar header, in stage order
 * ("1 p", "2 a", ...). The letter comes from `STAGE_LETTERS`, shared with the
 * sidebar change rows.
 */
export function headerCounts(changes: Change[]): string[] {
  const counts = stageCounts(changes)
  return STAGES.filter((stage) => counts[stage] > 0).map((stage) => `${counts[stage]} ${STAGE_LETTERS[stage]}`)
}

// --- Sidebar windowing and ordering (display-only, never touches state) -----

/** Maximum number of change rows the sidebar aims to render. */
export const SIDEBAR_LIMIT = 10

/**
 * A change is fully completed — nothing left to do — only when both hold:
 * the change's own `status` is `done`, and all four stages resolve to `done`
 * via `stageState`.
 *
 * The top-level `stage` pointer alone is not trusted. Real bookkeeping leaves
 * legacy entries parked at `stage: "impl"` (even `"commit"`) with every stage
 * row recorded `done` and commit/archive notes of `n/a`; those are finished
 * work and must be trimmable. Conversely, a pointer that says `archive` while a
 * stage row is still unfinished is an inconsistent record: it keeps its row
 * instead of being dropped.
 */
export function isCompleted(change: Change): boolean {
  if (change.status !== "done") return false
  return STAGES.every((stage) => stageState(change, stage).status === "done")
}

/**
 * Completion rank used to order sidebar rows, from 0 to 4.5:
 * the number of the four stages whose resolved status is `done` (0..4), plus
 * `0.5` when the change is started but unfinished (`active` or `blocked`), so
 * work in progress ranks above an equally advanced change that never started.
 *
 * A score of `4` means all four stage rows are `done`. `isCompleted` is
 * stricter: it also requires the change's own `status` to be `done`.
 */
export function completionScore(change: Change): number {
  let done = 0
  for (const stage of STAGES) {
    if (stageState(change, stage).status === "done") done += 1
  }
  const started = change.status === "active" || change.status === "blocked" ? 0.5 : 0
  return done + started
}

/**
 * Window and order `changes` for the sidebar.
 *
 * The window is decided on state-file positions: at most `limit` rows, dropping
 * the oldest changes that `isCompleted` accepts first. "Oldest" is the earliest
 * position in `changes` — that array order is the documented render order and
 * the order the orchestrator appends in. Timestamps are still never used to
 * determine age, because a timestamp sort would not describe append order.
 *
 * The returned rows are then ordered by `completionScore` descending, so the
 * most completed changes sit at the top and untouched work at the bottom. Equal
 * scores keep their trimmed state-file order: `Array#sort` is stable in Bun/V8,
 * and the comparator only compares scores, so that tie-break is explicit by
 * construction. The input array is never mutated and never returned sorted in
 * place; the `/pipe` panel keeps rendering state-file order.
 *
 * Unfinished work is never hidden, so the result exceeds `limit` when too few
 * changes are completed.
 */
export function sidebarChanges(changes: Change[], limit = SIDEBAR_LIMIT): Change[] {
  let windowed = changes
  if (changes.length > limit) {
    const dropped = new Set<number>()
    let excess = changes.length - limit
    for (let index = 0; index < changes.length && excess > 0; index += 1) {
      if (!isCompleted(changes[index])) continue
      dropped.add(index)
      excess -= 1
    }
    if (dropped.size > 0) windowed = changes.filter((_, index) => !dropped.has(index))
  }

  // Copy before sorting: callers keep the snapshot's state-file order.
  return [...windowed].sort((left, right) => completionScore(right) - completionScore(left))
}

export function truncate(value: string, max: number): string {
  if (value.length <= max) return value
  if (max <= 1) return value.slice(0, Math.max(0, max))
  return value.slice(0, max - 1) + "…"
}

export function formatTimestamp(value: string | undefined): string {
  if (!value) return "—"
  const parsed = Date.parse(value)
  if (Number.isNaN(parsed)) return value
  const date = new Date(parsed)
  const pad = (part: number) => String(part).padStart(2, "0")
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(date.getHours())}:${pad(date.getMinutes())}`
}
