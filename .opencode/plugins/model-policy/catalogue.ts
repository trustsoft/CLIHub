// Model-policy catalogue: the OpenCode host model list for the picker.
//
// The policy's `models` map is the curated catalogue: those are the models the
// dispatch gate understands. The host also reports every model it has
// configured; the picker shows both, with the policy entries first. A model
// outside the policy is selectable, but the caller asks before writing an
// override that the dispatch gate may reject.
//
// Nothing here is written anywhere, and no catalogue is cached to disk.

import { isRecord, text } from "./util"

/** One model reported by the OpenCode host. */
export type HostModel = {
  /** The full `provider/model` id the override would store. */
  id: string
  /** The human display name, when the host reports one. */
  name?: string
  /** The provider id and display name, for the description. */
  provider?: string
  providerName?: string
}

/** One entry of the picker: the policy catalogue first, then host-only models. */
export type ModelChoice = {
  id: string
  name?: string
  providerName?: string
  inPolicy: boolean
}

function payloadOf(value: unknown): unknown {
  let payload = value
  for (let depth = 0; depth < 3; depth += 1) {
    if (!isRecord(payload) || payload.data === undefined) break
    payload = payload.data
  }
  return payload
}

/** The message of an `{data, error}` response, if it is a failure. */
function failureOf(value: unknown): string | undefined {
  if (value === undefined || value === null) return "the client returned no result"
  if (!isRecord(value)) return undefined
  const error = value.error
  if (!error) return undefined
  if (typeof error === "string") return error
  if (!isRecord(error)) return "the request failed"
  const details = isRecord(error.data) ? error.data : undefined
  return (
    text(error.message) ??
    text(details?.message) ??
    text(details?.detail) ??
    text(error._tag) ??
    "the request failed"
  )
}

/**
 * Normalizes the `providers` response of the OpenCode client.
 *
 * The shape is `{providers: [{id, name, models: {modelID: {name}}}]}`, with or
 * without a `{data: ...}` envelope. A provider may omit `models`; a malformed
 * entry is skipped instead of failing the whole list.
 */
export function modelsFromProviders(value: unknown): HostModel[] {
  const payload = payloadOf(value)
  const body = isRecord(payload) && payload.providers !== undefined ? payload.providers : payload
  const providers = Array.isArray(body) ? body : []
  const models: HostModel[] = []
  const seen = new Set<string>()

  for (const raw of providers) {
    if (!isRecord(raw)) continue
    const provider = text(raw.id) ?? text(raw.providerID)
    if (!provider) continue
    const providerName = text(raw.name)
    const catalogue = isRecord(raw.models) ? raw.models : {}
    for (const [key, info] of Object.entries(catalogue)) {
      const name = key.trim()
      if (!name) continue
      const id = name.includes("/") ? name : `${provider}/${name}`
      if (seen.has(id)) continue
      seen.add(id)
      const meta = isRecord(info) ? info : {}
      models.push({ id, name: text(meta.name), provider, providerName })
    }
  }

  models.sort((left, right) => left.id.localeCompare(right.id))
  return models
}

/**
 * Normalizes a flat model list such as `ctx.data.location.model.list()`.
 * A list item carries `providerID` and `id` (or `modelID`); an id that already
 * contains a slash is left alone. Items without a provider are skipped because
 * the override value needs the full `provider/model` form.
 */
export function modelsFromList(value: unknown): HostModel[] {
  const payload = payloadOf(value)
  const body = Array.isArray(payload)
    ? payload
    : isRecord(payload) && Array.isArray(payload.models)
      ? payload.models
      : []
  const models: HostModel[] = []
  const seen = new Set<string>()

  for (const raw of body) {
    if (!isRecord(raw)) continue
    const provider = text(raw.providerID) ?? text(raw.provider && isRecord(raw.provider) ? raw.provider.id : undefined)
    const rawID = text(raw.id) ?? text(raw.modelID)
    if (!rawID) continue
    const id = rawID.includes("/") ? rawID : provider ? `${provider}/${rawID}` : undefined
    if (!id || seen.has(id)) continue
    seen.add(id)
    models.push({
      id,
      name: text(raw.name),
      provider,
      providerName: text(raw.providerName) ?? text(raw.provider && isRecord(raw.provider) ? raw.provider.name : undefined),
    })
  }

  models.sort((left, right) => left.id.localeCompare(right.id))
  return models
}

/** The location-scoped model cache of the TUI, when the host exposes it. */
export function modelsFromLocation(ctx: any): HostModel[] {
  try {
    const list = ctx?.data?.location?.model?.list?.(ctx?.location)
    return modelsFromList(list)
  } catch {
    return []
  }
}

/**
 * Reads the host model list through whichever client shape the build exposes.
 * A rejected call and an error response are both a failed source: the next
 * method is tried, and an empty result means the picker keeps the policy list.
 */
export async function hostModelsFromClient(client: any): Promise<HostModel[]> {
  const calls: Array<() => Promise<unknown>> = []
  if (typeof client?.app?.providers === "function") calls.push(() => client.app.providers())
  if (typeof client?.config?.providers === "function") calls.push(() => client.config.providers())
  if (typeof client?.provider?.list === "function") calls.push(() => client.provider.list())

  for (const call of calls) {
    try {
      const result = await call()
      if (failureOf(result)) continue
      const models = modelsFromProviders(result)
      if (models.length > 0) return models
    } catch {
      // The next client shape may work; the policy list is always available.
    }
  }
  return []
}

/**
 * The picker list: every policy model in its existing order, then every host
 * model the policy does not already name, sorted by id. Duplicates keep the
 * policy entry (and its policy description).
 */
export function mergeModelChoices(
  policyModels: readonly string[],
  hostModels: readonly HostModel[],
): ModelChoice[] {
  const byID = new Map(hostModels.map((model) => [model.id, model]))
  const choices: ModelChoice[] = []
  const seen = new Set<string>()

  for (const id of policyModels) {
    if (seen.has(id)) continue
    seen.add(id)
    const host = byID.get(id)
    choices.push({ id, name: host?.name, providerName: host?.providerName, inPolicy: true })
  }

  for (const model of [...hostModels].sort((left, right) => left.id.localeCompare(right.id))) {
    if (seen.has(model.id)) continue
    seen.add(model.id)
    choices.push({
      id: model.id,
      name: model.name,
      providerName: model.providerName,
      inPolicy: false,
    })
  }

  return choices
}
