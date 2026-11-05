// Model-policy observation: extracting the model an event reports and attributing
// it to a role. Reads event payloads only - no documents and no writes.

import { isRecord, text } from "./util"
import type { Observation, Policy } from "./types"

function findModelNode(
  root: unknown,
): { node: Record<string, unknown>; ancestry: Record<string, unknown>[] } | undefined {
  const queue: Array<{ value: unknown; depth: number; ancestry: Record<string, unknown>[] }> = [
    { value: root, depth: 0, ancestry: [] },
  ]
  let visited = 0

  while (queue.length > 0 && visited < 300) {
    const current = queue.shift()!
    visited += 1
    if (!current.value || typeof current.value !== "object" || current.depth > 5) continue
    const record = current.value as Record<string, unknown>

    if (text(record.modelID, 200)) return { node: record, ancestry: current.ancestry }
    if (isRecord(record.model) && text(record.model.modelID, 200)) return { node: record, ancestry: current.ancestry }

    for (const child of Object.values(record)) {
      if (child && typeof child === "object") {
        queue.push({ value: child, depth: current.depth + 1, ancestry: [...current.ancestry, record] })
      }
    }
  }

  return undefined
}

/**
 * Extracts the provider/model an event reports, plus the agent, variant, and
 * session when the event carries them. Returns undefined for unrelated events.
 */
export function observeEvent(details: unknown): Observation | undefined {
  if (!isRecord(details)) return undefined
  const found = findModelNode(details)
  if (!found) return undefined

  const node = found.node
  const nested = isRecord(node.model) ? node.model : undefined
  const model = text(node.modelID, 200) ?? text(nested?.modelID, 200)
  if (!model) return undefined

  // Agent and session metadata usually live on an ancestor (message or event
  // properties), so the nearest node wins and ancestors are the fallback.
  const candidates = [node, ...[...found.ancestry].reverse()]
  let agent: string | undefined
  let sessionID: string | undefined
  for (const candidate of candidates) {
    if (!agent) agent = text(candidate.agent, 120) ?? text(candidate.agentID, 120) ?? text(candidate.agentName, 120)
    if (!sessionID) sessionID = text(candidate.sessionID, 120)
  }

  return {
    at: Date.now(),
    agent,
    provider: text(node.providerID, 120) ?? text(nested?.providerID, 120),
    model,
    variant: text(node.variant, 40),
    sessionID,
  }
}

export function observationLabel(observation: Observation): string {
  return observation.provider && !observation.model.includes("/")
    ? `${observation.provider}/${observation.model}`
    : observation.model
}

export function recordObservation(list: Observation[], next: Observation, max = 16): Observation[] {
  const newest = list[0]
  if (
    newest &&
    newest.model === next.model &&
    newest.agent === next.agent &&
    newest.sessionID === next.sessionID &&
    Math.abs(next.at - newest.at) < 5000
  ) {
    return [{ ...newest, at: next.at }, ...list.slice(1)]
  }
  return [next, ...list].slice(0, max)
}

function lastSegment(value: string): string {
  const parts = value.split(/[\\/]/)
  return (parts[parts.length - 1] ?? value).toLowerCase()
}

export function roleForAgent(policy: Policy | undefined, agent: string | undefined): string | undefined {
  if (!policy || !agent) return undefined
  const candidate = agent.trim().toLowerCase()
  const tail = lastSegment(agent)
  for (const role of policy.roleOrder) {
    const info = policy.roles[role]
    if (!info) continue
    if (role.toLowerCase() === candidate) return role
    for (const name of info.agents) {
      if (name.toLowerCase() === candidate || lastSegment(name) === tail) return role
    }
  }
  return undefined
}

export function latestObservationForRole(
  observations: readonly Observation[],
  policy: Policy | undefined,
  role: string,
): Observation | undefined {
  return observations.find((entry) => roleForAgent(policy, entry.agent) === role)
}
