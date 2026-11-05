// Model-policy shared helpers: the five defensive readers every sibling module
// uses. Private to the plugin - `state.ts` deliberately does not re-export them.

export function isRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value) && typeof value === "object" && !Array.isArray(value)
}

export function text(value: unknown, max = 200): string | undefined {
  if (typeof value !== "string") return undefined
  const flat = value.replace(/\s+/g, " ").trim()
  if (!flat) return undefined
  return flat.length > max ? flat.slice(0, max - 1) + "…" : flat
}

export function iso(value: unknown): string | undefined {
  const raw = text(value, 80)
  if (!raw) return undefined
  return Number.isNaN(Date.parse(raw)) ? undefined : raw
}

export function pick(source: Record<string, unknown>, ...keys: string[]): unknown {
  for (const key of keys) {
    if (source[key] !== undefined) return source[key]
  }
  return undefined
}

export function stringRecord(value: unknown): Record<string, string> {
  if (!isRecord(value)) return {}
  const result: Record<string, string> = {}
  for (const [key, item] of Object.entries(value)) {
    const name = key.trim()
    const model = text(item, 200)
    if (name && model) result[name] = model
  }
  return result
}
