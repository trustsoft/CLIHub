// Model-policy display helpers: the abbreviations, the session-log line and the
// host-dialog answer mapping. Presentation only - nothing here is ever written.

import { isRecord } from "./util"
import type { DisplayRules, LogEntry, Policy } from "./types"

/** Every table empty: what a policy without a `display` section renders. */
const EMPTY_DISPLAY: DisplayRules = {
  providers: {},
  models: {},
  variants: {},
  roles: {},
  dropFreeSuffixFor: [],
}

/** The policy's `display` section, or an empty set when it declares none. */
export function displayRules(policy: Policy | undefined): DisplayRules {
  const section = policy?.display
  if (!section) return EMPTY_DISPLAY
  return {
    providers: { ...section.providers },
    models: { ...section.models },
    variants: { ...section.variants },
    roles: { ...section.roles },
    dropFreeSuffixFor: [...section.dropFreeSuffixFor],
  }
}

export function shortProvider(provider: string, rules: DisplayRules = EMPTY_DISPLAY): string {
  return rules.providers[provider.toLowerCase()] ?? provider
}

export function shortModelName(name: string, rules: DisplayRules = EMPTY_DISPLAY): string {
  return rules.models[name] ?? name
}

/**
 * `zai-coding-plan/glm-5.3` -> `za/glm-5.3`. Leaves values without a slash alone.
 * The `-free` suffix is dropped for the providers listed in
 * `dropFreeSuffixFor`; every `opencode` model in the catalogue is a free one, so
 * the suffix carries no information there.
 */
export function shortModel(model: string, rules: DisplayRules = EMPTY_DISPLAY): string {
  const slash = model.indexOf("/")
  if (slash <= 0) return model
  const provider = model.slice(0, slash)
  let name = model.slice(slash + 1)
  if (rules.dropFreeSuffixFor.includes(provider.toLowerCase())) {
    name = name.replace(/-free$/, "")
  }
  return shortProvider(provider, rules) + "/" + shortModelName(name, rules)
}

/** The reasoning effort as a single letter: `M` is max and `m` is medium. */
export function shortVariant(variant: string | undefined, rules: DisplayRules = EMPTY_DISPLAY): string | undefined {
  if (!variant) return undefined
  return rules.variants[variant] ?? variant
}

/** The role name as a short code; the full name stays in the details dialog. */
export function shortRole(role: string, rules: DisplayRules = EMPTY_DISPLAY): string {
  return rules.roles[role.toLowerCase()] ?? role.slice(0, 3)
}

/**
 * The legend of the abbreviations in use, built from the rules themselves so it
 * can never disagree with what the panel renders. Each empty table renders
 * `none`.
 */
export function legendFor(rules: DisplayRules = EMPTY_DISPLAY): string[] {
  const seen = new Set<string>()
  const pairs = (table: Record<string, string>): string[] => {
    const out: string[] = []
    for (const [key, value] of Object.entries(table)) {
      const code = value.split("-")[0]
      if (seen.has(code)) continue
      seen.add(code)
      out.push(`${value}=${key}`)
    }
    return out
  }
  const providers: string[] = []
  const providerSeen = new Set<string>()
  for (const [key, value] of Object.entries(rules.providers)) {
    if (providerSeen.has(value)) continue
    providerSeen.add(value)
    providers.push(`${value}=${key}`)
  }
  const models: string[] = []
  const modelSeen = new Set<string>()
  for (const [key, value] of Object.entries(rules.models)) {
    const code = value.split("-")[0]
    const vendor = key.split("-")[0]
    if (code === vendor || modelSeen.has(code)) continue
    modelSeen.add(code)
    models.push(`${code}=${vendor}`)
  }
  const variants: string[] = []
  for (const [key, value] of Object.entries(rules.variants)) variants.push(`${value}=${key}`)
  const roles: string[] = []
  for (const [key, value] of Object.entries(rules.roles)) roles.push(`${value}=${key}`)
  return [
    `providers: ${providers.join("  ") || "none"}`,
    `model names: ${models.join("  ") || "none"}`,
    `effort: ${variants.join("  ") || "none"}`,
    `roles: ${roles.join("  ") || "none"}`,
  ]
}

export function truncate(value: string, max: number): string {
  if (value.length <= max) return value
  if (max <= 1) return value.slice(0, Math.max(0, max))
  return value.slice(0, max - 1) + "…"
}

export function formatClock(at: number): string {
  const date = new Date(at)
  const pad = (part: number) => String(part).padStart(2, "0")
  return `${pad(date.getHours())}:${pad(date.getMinutes())}`
}

export function formatRemaining(ms: number): string {
  if (ms <= 0) return "now"
  const minutes = Math.ceil(ms / 60000)
  if (minutes < 60) return `${minutes}m`
  const hours = Math.floor(minutes / 60)
  if (hours < 24) return minutes % 60 === 0 ? `${hours}h` : `${hours}h${minutes % 60}m`
  const days = Math.floor(hours / 24)
  return hours % 24 === 0 ? `${days}d` : `${days}d${hours % 24}h`
}

/**
 * One session-log line. The caller passes the rules it renders with; the default
 * is the empty set, so a one-argument call renders full ids instead of silently
 * abbreviating through a table that is not in scope.
 */
export function logLine(entry: LogEntry, rules: DisplayRules = EMPTY_DISPLAY): string {
  const clock = entry.at ? formatClock(Date.parse(entry.at)) : "--:--"
  const role = entry.role ?? "?"
  const route = `${shortModel(entry.from ?? "?", rules)} → ${shortModel(entry.to ?? "?", rules)}`
  return entry.reason ? `${clock} ${role}: ${route} (${entry.reason})` : `${clock} ${role}: ${route}`
}

/**
 * Maps a `ctx.ui.dialog.select` result back to the `value` of the option it
 * names. The documented host resolves the chosen option's `value`, but the
 * picker's titles (`policy · x`) no longer equal its values (`policy:x`), so a
 * build that resolved the rendered label would close the dialog and do nothing.
 *
 * Matching order: an exact `value` first, then an exact `title`, then `name`. An
 * object result is tried as `.value`, `.title`, `.name`, `.id` — every candidate
 * value is compared against the option values before any of them is compared
 * against a title or a name, so a value match always wins. Anything that matches
 * nothing — an unrecognized string, `undefined`, `null`, a non-string — returns
 * `undefined`, which the caller treats as a cancelled selection, never a write.
 * An empty string is a real option value in the role picker, so it is matched
 * literally and never trimmed.
 */
export function matchOptionValue(
  choice: unknown,
  options: ReadonlyArray<{ value: string; title: string; name?: string }>,
): string | undefined {
  const candidates: string[] = []
  if (typeof choice === "string") {
    candidates.push(choice)
  } else if (isRecord(choice)) {
    for (const key of ["value", "title", "name", "id"]) {
      const candidate = choice[key]
      if (typeof candidate === "string") candidates.push(candidate)
    }
  }
  if (candidates.length === 0) return undefined

  for (const candidate of candidates) {
    const hit = options.find((option) => option.value === candidate)
    if (hit) return hit.value
  }
  for (const candidate of candidates) {
    const hit = options.find((option) => option.title === candidate || option.name === candidate)
    if (hit) return hit.value
  }
  return undefined
}
