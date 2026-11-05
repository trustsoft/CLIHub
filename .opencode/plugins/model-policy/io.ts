// Model-policy I/O: reading the two JSON documents through a small policy cache,
// and the atomic write protocol every owned-field mutation goes through.

import { mkdirSync, readFileSync, renameSync, statSync, unlinkSync, writeFileSync } from "node:fs"
import path from "node:path"

import { normalizePolicy } from "./policy"

type DocumentRead =
  | { ok: true; input: string }
  | { ok: false; reason: "missing" | "unreadable"; detail: string }

export function readDocument(file: string): DocumentRead {
  try {
    const stat = statSync(file)
    if (!stat.isFile()) return { ok: false, reason: "unreadable", detail: "not a regular file" }
  } catch (error: any) {
    const missing = error?.code === "ENOENT"
    return {
      ok: false,
      reason: missing ? "missing" : "unreadable",
      detail: missing ? "file does not exist" : String(error?.message ?? error),
    }
  }

  try {
    return { ok: true, input: readFileSync(file, "utf8").replace(/^\uFEFF/, "") }
  } catch (error: any) {
    return { ok: false, reason: "unreadable", detail: String(error?.message ?? error) }
  }
}

const policyCache = new Map<string, { key: string; value: ReturnType<typeof normalizePolicy> }>()

export function readPolicy(file: string): ReturnType<typeof normalizePolicy> | { ok: false; detail: string; missing: boolean } {
  const read = readDocument(file)
  if (!read.ok) return { ok: false, detail: read.detail, missing: read.reason === "missing" }

  const key = String(read.input.length) + ":" + read.input.slice(0, 64) + ":" + read.input.slice(-64)
  const hit = policyCache.get(file)
  if (hit && hit.key === key) return hit.value

  let parsed: unknown
  try {
    parsed = JSON.parse(read.input)
  } catch (error: any) {
    const value = { ok: false as const, detail: `not valid JSON: ${String(error?.message ?? error)}` }
    policyCache.set(file, { key, value })
    return value
  }

  const value = normalizePolicy(parsed)
  policyCache.set(file, { key, value })
  return value
}

function pauseSync(ms: number): void {
  try {
    Atomics.wait(new Int32Array(new SharedArrayBuffer(4)), 0, 0, ms)
  } catch {
    const end = Date.now() + ms
    while (Date.now() < end) {
      // Bounded spin; never long enough to freeze the TUI.
    }
  }
}

function renameWithRetry(from: string, to: string): void {
  for (let attempt = 0; ; attempt += 1) {
    try {
      renameSync(from, to)
      return
    } catch (error: any) {
      const code = error?.code
      const transient = code === "EPERM" || code === "EACCES" || code === "EBUSY"
      if (attempt >= 4 || !transient) throw error
      pauseSync(15 * (attempt + 1))
    }
  }
}

let writeSequence = 0

/**
 * Writes JSON atomically: a unique temporary file in the destination directory,
 * then a rename over the destination. Readers never observe a partial document.
 */
export function atomicWriteJson(file: string, value: unknown): void {
  const directory = path.dirname(file)
  mkdirSync(directory, { recursive: true })
  writeSequence += 1
  const temp = path.join(directory, `.${path.basename(file)}.${process.pid}.${writeSequence}.tmp`)
  const body = `${JSON.stringify(value, null, 2)}\n`
  try {
    writeFileSync(temp, body, { encoding: "utf8", flag: "wx" })
    renameWithRetry(temp, file)
  } catch (error) {
    try {
      unlinkSync(temp)
    } catch {
      // The rename may already have consumed the temporary file.
    }
    throw error
  }
}
