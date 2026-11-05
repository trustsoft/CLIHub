// Model-policy path resolution: where the two JSON documents live under a
// project, and the environment overrides that replace each of them. No I/O here.

import path from "node:path"
import type { ResolvedPaths } from "./types"

const POLICY_FILE_ENV = "OPENCODE_MODEL_POLICY_FILE"
const RUNTIME_FILE_ENV = "OPENCODE_MODEL_POLICY_RUNTIME_FILE"

export const POLICY_PATH = path.join("docs", "agent-model-policy.json")
export const RUNTIME_PATH = path.join(".opencode", "model-policy.json")

export function projectDirectory(ctx: any, sessionID?: string): string | undefined {
  let session: any
  try {
    session = sessionID ? ctx?.data?.session?.get?.(sessionID) : undefined
  } catch {
    session = undefined
  }

  let defaultLocation: any
  try {
    defaultLocation = ctx?.data?.location?.default?.()
  } catch {
    defaultLocation = undefined
  }

  const candidates = [
    session?.location?.directory,
    session?.directory,
    session?.path?.directory,
    ctx?.location?.directory,
    ctx?.location?.project?.directory,
    defaultLocation?.directory,
    defaultLocation?.project?.directory,
  ]
  for (const candidate of candidates) {
    if (typeof candidate === "string" && candidate.trim()) return candidate.trim()
  }

  try {
    return process.cwd()
  } catch {
    return undefined
  }
}

export function resolvePaths(ctx: any, sessionID?: string): ResolvedPaths {
  const project = projectDirectory(ctx, sessionID)
  const root = project ?? process.cwd()
  const policyOverride = process.env[POLICY_FILE_ENV]?.trim()
  const runtimeOverride = process.env[RUNTIME_FILE_ENV]?.trim()
  return {
    project,
    policy: policyOverride ? path.resolve(policyOverride) : path.join(root, POLICY_PATH),
    runtime: runtimeOverride ? path.resolve(runtimeOverride) : path.join(root, RUNTIME_PATH),
  }
}
