// Local verification for the pure state/path logic. Run with:
//   bun run verify.ts
// This file is not a plugin entrypoint; OpenCode loads tui.tsx only.
//
// The live files are only ever read here: every write goes to a scratch
// directory selected through the two path-override environment variables.

import { strict as assert } from "node:assert"
import { existsSync, mkdirSync, readFileSync, readdirSync, rmSync, writeFileSync } from "node:fs"
import { tmpdir } from "node:os"
import path from "node:path"
import {
  applyUserPreset,
  atomicWriteJson,
  captureRoles,
  checkPresetName,
  clearRoleOverrides,
  defaultRuntimeDoc,
  deleteUserPreset,
  describePreset,
  describeRole,
  describeUserPreset,
  displayPreset,
  displayRules,
  effectivePreset,
  ensureRuntime,
  expandModelId,
  formatRemaining,
  hostModelsFromClient,
  legendFor,
  logLine,
  matchOptionValue,
  mergeModelChoices,
  modelLabel,
  modelLabelFull,
  modelsFromList,
  modelsFromLocation,
  modelsFromProviders,
  observeEvent,
  PRESET_NAME_MAX,
  readSnapshot,
  recreateRuntime,
  resolvePaths,
  roleEntry,
  roleForAgent,
  roleModels,
  saveUserPreset,
  setActivePreset,
  setRoleOverride,
  shortModel,
  shortModelName,
  shortProvider,
  shortRole,
  shortVariant,
  truncate,
  type Policy,
  type Snapshot,
} from "./state"

const LIVE_PROJECT = "D:\\Projects\\CLIHub"
const LIVE_POLICY = path.join(LIVE_PROJECT, "docs", "agent-model-policy.json")
const LIVE_RUNTIME = path.join(LIVE_PROJECT, ".opencode", "model-policy.json")

// The live policy is the richest fixture available, but the harness must also run
// on a machine without the CLIHub checkout: when that document is absent the
// built-in fixture below is seeded instead and every assertion still runs. It has
// the same shape the assertions need - two presets, a two-model planner chain, a
// second role - and a catalogue, because a chain entry outside `models` is not
// resolvable and would silently fall back to the starved branch.
const FALLBACK_POLICY = {
  version: 1,
  active_preset: "default",
  roles: {
    planner: { agents: ["pipe/planner"], tier: "high", on_exhaustion: "next" },
    mechanical: { agents: ["pipe/mechanical"], tier: "low", on_exhaustion: "next" },
  },
  models: {
    "fixture/planner-a": {},
    "fixture/planner-b": {},
    "fixture/free-a": {},
    "fixture/free-b": {},
    "fixture/mechanical-a": {},
  },
  presets: {
    default: { planner: ["fixture/planner-a", "fixture/planner-b"], mechanical: ["fixture/mechanical-a"] },
    free_only: { planner: ["fixture/free-a", "fixture/free-b"], mechanical: ["fixture/free-a"] },
  },
  overrides: [],
}

// The live runtime file may already exist: the running TUI creates it through
// this plugin's own sidebar effect. Verification must leave it byte-identical.
const liveRuntimeBefore = existsSync(LIVE_RUNTIME) ? readFileSync(LIVE_RUNTIME, "utf8") : undefined

const scratch = path.join(tmpdir(), `opencode-model-policy-verify-${process.pid}`)
rmSync(scratch, { recursive: true, force: true })
mkdirSync(scratch, { recursive: true })

const project = path.join(scratch, "project")
mkdirSync(path.join(project, "docs"), { recursive: true })
mkdirSync(path.join(project, ".opencode"), { recursive: true })

const policyFile = path.join(project, "docs", "agent-model-policy.json")
const runtimeFile = path.join(project, ".opencode", "model-policy.json")

const ctx = { data: { session: { get: () => ({ location: { directory: project } }) } } }
const sessionID = "ses_verify"

function setEnv(): void {
  process.env.OPENCODE_MODEL_POLICY_FILE = policyFile
  process.env.OPENCODE_MODEL_POLICY_RUNTIME_FILE = runtimeFile
}

function readRuntime(): Record<string, any> {
  return JSON.parse(readFileSync(runtimeFile, "utf8"))
}

function leftovers(directory: string): string[] {
  return readdirSync(directory).filter((name) => name.endsWith(".tmp"))
}

// --- path resolution without overrides -------------------------------------

const resolved = resolvePaths(ctx, sessionID)
assert.equal(resolved.policy, path.join(project, "docs", "agent-model-policy.json"))
assert.equal(resolved.runtime, path.join(project, ".opencode", "model-policy.json"))
console.log("paths: default project resolution OK")

// --- live policy is readable and matches the contract -----------------------

const hasLivePolicy = existsSync(LIVE_POLICY)
const livePolicy = hasLivePolicy ? JSON.parse(readFileSync(LIVE_POLICY, "utf8")) : FALLBACK_POLICY
writeFileSync(policyFile, JSON.stringify(livePolicy, null, 2), "utf8")
setEnv()

const seeded = readSnapshot(ctx, sessionID)
assert.equal(seeded.ok, true)
assert.equal(seeded.status, "ok")
assert.equal(seeded.activePreset, livePolicy.active_preset)
assert.equal(seeded.presetSource, "policy")
assert.deepEqual(seeded.presets, Object.keys(livePolicy.presets))
assert.equal(seeded.roles.length, Object.keys(livePolicy.roles).length)
assert.equal(seeded.runtime.exists, false)
assert.equal(seeded.roles.find((role) => role.name === "planner")?.model, livePolicy.presets.default.planner[0])
assert.equal(seeded.roles.find((role) => role.name === "mechanical")?.model, livePolicy.presets.default.mechanical[0])
assert.equal(seeded.exhausted.length, 0)
console.log(
  hasLivePolicy
    ? `live policy: ${seeded.presets.length} presets, ${seeded.roles.length} roles`
    : `live policy skipped: ${LIVE_POLICY} not found (the built-in fixture policy is seeded)`,
)

// --- defaults, creation, and ownership merge -------------------------------

const created = ensureRuntime(ctx, sessionID)
if (!created.ok) throw new Error(`ensureRuntime failed: ${created.detail}`)
assert.equal(created.created, true)
const fresh = readRuntime()
assert.equal(fresh.schemaVersion, 1)
assert.equal(fresh.activePreset, livePolicy.active_preset)
assert.deepEqual(fresh.userPresets, {}, "the default document announces the saved-preset map")
assert.deepEqual(fresh.roleOverrides, {})
assert.deepEqual(fresh.exhaustedUntil, {})
assert.deepEqual(fresh.sessionLog, [])
assert.deepEqual(leftovers(path.dirname(runtimeFile)), [], "temp files left behind")
console.log("ensureRuntime: created with policy defaults, no temp leftovers")

const until = new Date(Date.now() + 90 * 60_000).toISOString()
const orchestratorDoc = {
  schemaVersion: 1,
  activePreset: "default",
  roleOverrides: {},
  exhaustedUntil: { [livePolicy.presets.default.planner[0]]: until },
  sessionLog: [{ at: new Date().toISOString(), role: "planner", from: "a", to: "b", reason: "429" }],
  orchestratorOnly: { keep: true },
}
writeFileSync(runtimeFile, JSON.stringify(orchestratorDoc, null, 2), "utf8")

const switched = setActivePreset(ctx, sessionID, "free_only")
assert.equal(switched.ok, true)
const afterSwitch = readRuntime()
assert.equal(afterSwitch.activePreset, "free_only", "owned field written")
assert.deepEqual(afterSwitch.exhaustedUntil, orchestratorDoc.exhaustedUntil, "exhaustedUntil preserved")
assert.deepEqual(afterSwitch.sessionLog, orchestratorDoc.sessionLog, "sessionLog preserved")
assert.deepEqual(afterSwitch.orchestratorOnly, orchestratorDoc.orchestratorOnly, "unknown keys preserved")
assert.deepEqual(leftovers(path.dirname(runtimeFile)), [], "temp files left behind")


const overrideWritten = setRoleOverride(ctx, sessionID, "planner", "relayfast/claude-sonnet-5")
assert.equal(overrideWritten.ok, true)
const afterOverride = readRuntime()
assert.equal(afterOverride.roleOverrides.planner, "relayfast/claude-sonnet-5")
assert.deepEqual(afterOverride.exhaustedUntil, orchestratorDoc.exhaustedUntil, "exhaustedUntil preserved")
assert.deepEqual(afterOverride.sessionLog, orchestratorDoc.sessionLog, "sessionLog preserved")
assert.deepEqual(afterOverride.orchestratorOnly, orchestratorDoc.orchestratorOnly, "unknown keys preserved")

const overrideRemoved = setRoleOverride(ctx, sessionID, "planner", undefined)
assert.equal(overrideRemoved.ok, true)
assert.deepEqual(readRuntime().roleOverrides, {})
assert.deepEqual(readRuntime().exhaustedUntil, orchestratorDoc.exhaustedUntil, "exhaustedUntil preserved")

assert.equal(setRoleOverride(ctx, sessionID, "nope", "x/y").ok, false, "unknown role rejected")
assert.equal(setActivePreset(ctx, sessionID, "nope").ok, false, "unknown preset rejected")
assert.equal(readRuntime().activePreset, "free_only", "rejected write did not change the file")

const cleared = clearRoleOverrides(ctx, sessionID)
assert.equal(cleared.ok, true)
assert.deepEqual(readRuntime().roleOverrides, {})
assert.deepEqual(readRuntime().sessionLog, orchestratorDoc.sessionLog, "sessionLog preserved")
console.log("merge ownership: activePreset/roleOverrides writes never clobber orchestrator fields")

// The runtime preset must drive resolution; the policy active_preset is only a
// default for a runtime file that has no selection yet.
const afterSwitchView = readSnapshot(ctx, sessionID)
assert.equal(afterSwitchView.activePreset, "free_only")
assert.equal(afterSwitchView.effectivePreset, "free_only", "stored preset drives resolution")
assert.equal(
  afterSwitchView.roles.find((role) => role.name === "planner")?.model,
  livePolicy.presets.free_only.planner[0],
)
console.log("resolution: runtime activePreset overrides the policy default")

// --- atomic write, snapshot resolution, exhaustion -------------------------

const atomicTarget = path.join(scratch, "atomic.json")
atomicWriteJson(atomicTarget, { a: 1 })
assert.deepEqual(JSON.parse(readFileSync(atomicTarget, "utf8")), { a: 1 })
assert.deepEqual(leftovers(scratch), [], "atomic write left a temp file")
console.log("atomicWriteJson: round-trip OK")

const chain = livePolicy.presets.default.planner
writeFileSync(
  runtimeFile,
  JSON.stringify({
    schemaVersion: 1,
    activePreset: "default",
    roleOverrides: {},
    exhaustedUntil: {
      [chain[0]]: new Date(Date.now() + 30 * 60_000).toISOString(),
      [chain[1]]: new Date(Date.now() - 60_000).toISOString(),
    },
    sessionLog: [],
  }),
  "utf8",
)
const exhaustedView = readSnapshot(ctx, sessionID)
const planner = exhaustedView.roles.find((role) => role.name === "planner")
assert.equal(planner?.model, chain[1], "expired exhaustion is ignored, so chain[1] is available")
assert.equal(planner?.starved, false)
assert.deepEqual(exhaustedView.exhausted.map((entry) => entry.model), [chain[0]], "expired exhaustion hidden")
assert.match(formatRemaining(exhaustedView.exhausted[0].remainingMs), /^\d+m$/)

writeFileSync(
  runtimeFile,
  JSON.stringify({
    schemaVersion: 1,
    activePreset: "default",
    roleOverrides: {},
    exhaustedUntil: Object.fromEntries(
      chain.map((model: string) => [model, new Date(Date.now() + 60 * 60_000).toISOString()]),
    ),
    sessionLog: [{ at: new Date().toISOString(), role: "planner", from: chain[0], to: chain[1], reason: "quota" }],
  }),
  "utf8",
)
const starvedView = readSnapshot(ctx, sessionID)
const starvedPlanner = starvedView.roles.find((role) => role.name === "planner")
assert.equal(starvedPlanner?.starved, true)
assert.equal(starvedPlanner?.model, chain[0])
assert.equal(starvedView.log.length, 1)
assert.equal(starvedView.log[0].reason, "quota")
assert.equal(exhaustedView.log.length, 0)
console.log("resolution: exhausted models skipped, starved chains flagged, log parsed")

// --- policy override windows ----------------------------------------------

const synthetic = {
  version: 1,
  active_preset: "default",
  roles: { planner: { agents: ["pipe/planner"], tier: "high", on_exhaustion: "next" } },
  models: {},
  presets: {
    default: { planner: ["a/one"] },
    plan_exhausted: { planner: ["b/two"] },
  },
  overrides: [
    { preset: "plan_exhausted", valid_from: new Date(Date.now() - 60_000).toISOString(), valid_until: new Date(Date.now() + 60_000).toISOString() },
  ],
}
writeFileSync(policyFile, JSON.stringify(synthetic, null, 2), "utf8")
const windowed = readSnapshot(ctx, sessionID)
assert.equal(windowed.activePreset, "default", "stored preset unchanged")
assert.equal(windowed.effectivePreset, "plan_exhausted", "override window wins at dispatch")
assert.equal(windowed.roles[0].model, "b/two")
const parsedPolicy: Policy = windowed.policy!
assert.equal(
  effectivePreset(parsedPolicy, windowed.activePreset, Date.now() + 10 * 60_000),
  "default",
  "expired window falls back to the stored preset",
)
console.log("overrides: window selects the effective preset without changing activePreset")

// --- malformed runtime: reported, never overwritten ------------------------

writeFileSync(runtimeFile, "{ not json", "utf8")
const broken = readSnapshot(ctx, sessionID)
assert.equal(broken.ok, false)
assert.equal(broken.status, "runtime-malformed")
const refused = setActivePreset(ctx, sessionID, "plan_exhausted")
assert.equal(refused.ok, false, "malformed runtime is not overwritten")
assert.equal(readFileSync(runtimeFile, "utf8"), "{ not json", "malformed content untouched")
const malformedRepaired = recreateRuntime(ctx, sessionID)
assert.equal(malformedRepaired.ok, true)
const recreated = readRuntime()
assert.deepEqual(recreated.userPresets, {}, "a recreated document announces the saved-preset map")
assert.equal(readSnapshot(ctx, sessionID).status, "ok")
console.log("malformed runtime: refused write, explicit recreate repairs")

// --- missing policy --------------------------------------------------------

process.env.OPENCODE_MODEL_POLICY_FILE = path.join(scratch, "absent-policy.json")
const noPolicy = readSnapshot(ctx, sessionID)
assert.equal(noPolicy.ok, false)
assert.equal(noPolicy.status, "no-policy")
assert.equal(setActivePreset(ctx, sessionID, "default").ok, false)
setEnv()

// --- catalogue and descriptions -------------------------------------------

const finalView = readSnapshot(ctx, sessionID)
const models = roleModels(finalView, "planner")
const effectiveChain = finalView.policy?.presets[finalView.effectivePreset ?? ""]?.planner ?? []
assert.equal(models[0], effectiveChain[0], "effective preset chain comes first")
assert.ok(models.includes("a/one") && models.includes("b/two"))
assert.equal(describePreset(finalView, "default"), "planner=a/one")
assert.equal(truncate("abcdef", 4), "abc…")

// --- host catalogue --------------------------------------------------------
//
// The picker merges the policy catalogue with the providers the host reports:
// all host models are visible, and a model already in the policy keeps its
// policy entry (and description) instead of appearing twice.

const providerPayload = {
  providers: [
    {
      id: "zai-coding-plan",
      name: "Z.ai",
      models: {
        "glm-5.3": { name: "GLM 5.3" },
        "glm-5.3-flash": {},
      },
    },
    {
      id: "relayfast",
      name: "RelayFast",
      models: { "claude-sonnet-5": { name: "Claude Sonnet 5" } },
    },
  ],
}

const host = modelsFromProviders(providerPayload)
assert.deepEqual(
  host.map((model) => model.id),
  ["relayfast/claude-sonnet-5", "zai-coding-plan/glm-5.3", "zai-coding-plan/glm-5.3-flash"],
)
assert.equal(host[0].name, "Claude Sonnet 5")
assert.equal(host[0].providerName, "RelayFast")
assert.deepEqual(
  modelsFromProviders({ data: providerPayload }).map((model) => model.id),
  host.map((model) => model.id),
  "the {data} envelope is unwrapped",
)
assert.deepEqual(modelsFromProviders({ providers: [] }), [])
assert.deepEqual(modelsFromProviders({ providers: [{ models: { "no/provider": {} } }] }), [])
assert.deepEqual(modelsFromProviders(undefined), [])
console.log("catalogue: providers are normalized to full ids and display names")

const flatModels = [
  { providerID: "relayfast", id: "claude-sonnet-5", name: "Claude Sonnet 5" },
  { providerID: "zai-coding-plan", modelID: "glm-5.3", name: "GLM 5.3" },
  { id: "opencode/big-pickle", name: "Big Pickle" },
  { id: "no-provider" },
]
assert.deepEqual(
  modelsFromList(flatModels).map((model) => model.id),
  ["opencode/big-pickle", "relayfast/claude-sonnet-5", "zai-coding-plan/glm-5.3"],
)
assert.equal(modelsFromList({ models: flatModels }).length, 3, "an {models} envelope is accepted")
assert.deepEqual(
  modelsFromLocation({ data: { location: { model: { list: () => flatModels } } }, location: {} }).map(
    (model) => model.id,
  ),
  ["opencode/big-pickle", "relayfast/claude-sonnet-5", "zai-coding-plan/glm-5.3"],
)
assert.deepEqual(modelsFromLocation({}), [], "an absent location cache is not a failure")
console.log("catalogue: the location model cache is normalized too")

const fallbackClient = await hostModelsFromClient({
  app: {
    providers: async () => {
      throw new Error("down")
    },
  },
  config: { providers: async () => providerPayload },
})
assert.equal(fallbackClient.length, 3, "a rejected call falls through to the next client shape")
const errorClient = await hostModelsFromClient({
  app: { providers: async () => ({ error: { message: "down" } }) },
  provider: { list: async () => providerPayload },
})
assert.equal(errorClient.length, 3, "an error payload falls through to the next client shape")
assert.deepEqual(await hostModelsFromClient(undefined), [])
console.log("catalogue: a rejected or failed host call falls back to the next source")

const merged = mergeModelChoices(["zai-coding-plan/glm-5.3", "policy/only"], host)
assert.deepEqual(
  merged.map((choice) => [choice.id, choice.inPolicy]),
  [
    ["zai-coding-plan/glm-5.3", true],
    ["policy/only", true],
    ["relayfast/claude-sonnet-5", false],
    ["zai-coding-plan/glm-5.3-flash", false],
  ],
  "policy entries first, then host-only models sorted, without duplicates",
)
assert.equal(merged[0].name, "GLM 5.3", "a policy entry keeps its host display name")
console.log("catalogue: the picker list is the union of policy and host models")

// --- display rules come from the policy, and nowhere else ------------------
//
// Three fixtures pin the contract:
//   (1) the CLIHub-shaped table renders exactly the abbreviations the panel has
//       always shown;
//   (2) a partial table is used as declared - no merge, no built-in fallback;
//   (3) a policy without the section renders full ids and a `none` legend.
// The policy and runtime documents are captured before and compared after the
// block: reading the rules must never write either.

// The table CLIHub's policy ships - the same strings the panel used to render
// from its former built-in copy.
const FULL_DISPLAY = {
  providers: {
    "zai-coding-plan": "za",
    relayfast: "rf",
    "relay.fast": "rf",
    deepseek: "ds",
    opencode: "oc",
  },
  models: {
    "claude-sonnet-5": "cl-sonnet-5",
    "claude-haiku-4-5": "cl-haiku-4-5",
    "deepseek-v4-pro": "ds-v4-pro",
    "deepseek-v4.1-flash": "ds-v4.1-flash",
    "deepseek-flash": "ds-flash",
    "minimax-m3.1-flash": "mm-m3.1-flash",
    "nemotron-3.5-super-vl": "nemo-3.5-svl",
  },
  variants: { none: "n", minimal: "i", low: "l", medium: "m", high: "h", xhigh: "x", max: "M" },
  roles: {
    planner: "pln",
    implementer: "imp",
    verifier: "ver",
    explorer: "exp",
    research: "res",
    mechanical: "mch",
  },
  dropFreeSuffixFor: ["opencode"],
}

const policyBytesBefore = readFileSync(policyFile, "utf8")
const runtimeBytesBefore = existsSync(runtimeFile) ? readFileSync(runtimeFile, "utf8") : undefined

// (1) A policy with the section: every string is the declared abbreviation.
const fullDisplay = { policy: { display: FULL_DISPLAY } } as unknown as Snapshot
const full = displayRules(fullDisplay.policy)
assert.equal(shortModel("zai-coding-plan/glm-5.3", full), "za/glm-5.3")
assert.equal(shortModel("relayfast/claude-sonnet-5", full), "rf/cl-sonnet-5")
assert.equal(shortModel("relay.fast/claude-sonnet-5", full), "rf/cl-sonnet-5")
assert.equal(shortModel("relayfast/claude-haiku-4-5", full), "rf/cl-haiku-4-5")
assert.equal(shortModel("relayfast/deepseek-v4.1-flash", full), "rf/ds-v4.1-flash")
assert.equal(shortModel("relayfast/deepseek-v4-pro", full), "rf/ds-v4-pro")
assert.equal(shortModel("relayfast/kimi-k2.7-code", full), "rf/kimi-k2.7-code")
assert.equal(shortModel("relayfast/gpt-5.6-sol", full), "rf/gpt-5.6-sol")
assert.equal(shortModel("deepseek/deepseek-flash", full), "ds/ds-flash")
assert.equal(shortModel("opencode/minimax-m3.1-flash-free", full), "oc/mm-m3.1-flash")
assert.equal(shortModel("opencode/nemotron-3.5-super-vl-free", full), "oc/nemo-3.5-svl")
assert.equal(shortModel("opencode/glm-5.3-flash-free", full), "oc/glm-5.3-flash")
assert.equal(shortModel("opencode/big-pickle", full), "oc/big-pickle")
assert.equal(shortModel("no-slash", full), "no-slash")
assert.equal(shortModel("/leading", full), "/leading")
assert.equal(shortModelName("claude-sonnet-5", full), "cl-sonnet-5")
assert.equal(shortModelName("glm-5.3", full), "glm-5.3")
assert.deepEqual(legendFor(full), [
  "providers: za=zai-coding-plan  rf=relayfast  ds=deepseek  oc=opencode",
  "model names: cl=claude  ds=deepseek  mm=minimax  nemo=nemotron",
  "effort: n=none  i=minimal  l=low  m=medium  h=high  x=xhigh  M=max",
  "roles: pln=planner  imp=implementer  ver=verifier  exp=explorer  res=research  mch=mechanical",
])
assert.equal(shortVariant("none", full), "n")
assert.equal(shortVariant("minimal", full), "i")
assert.equal(shortVariant("low", full), "l")
assert.equal(shortVariant("medium", full), "m")
assert.equal(shortVariant("high", full), "h")
assert.equal(shortVariant("xhigh", full), "x")
assert.equal(shortVariant("max", full), "M")
assert.equal(shortVariant(undefined, full), undefined)
assert.equal(shortVariant("unknown", full), "unknown")
assert.equal(shortRole("planner", full), "pln")
assert.equal(shortRole("implementer", full), "imp")
assert.equal(shortRole("mechanical", full), "mch")
assert.equal(shortProvider("zai-coding-plan", full), "za")
assert.equal(shortProvider("unknown-provider", full), "unknown-provider")

// The preset descriptions abbreviate through the same table; the chain itself
// keeps the full ids, which is what a dispatch writes.
const presetFixture = {
  policy: {
    display: FULL_DISPLAY,
    presets: { default: { planner: ["zai-coding-plan/glm-5.3", "relayfast/claude-sonnet-5"] } },
  },
  runtime: { userPresets: { saved: { planner: "opencode/glm-5.3-flash-free" } } },
} as unknown as Snapshot
assert.equal(describePreset(presetFixture, "default"), "planner=za/glm-5.3")
assert.equal(describeUserPreset(presetFixture, "saved"), "planner=oc/glm-5.3-flash")
assert.equal(
  presetFixture.policy.presets.default.planner[0],
  "zai-coding-plan/glm-5.3",
  "the chain keeps the full id",
)

// The compact label of a model for a role, and the reverse mapping back to the
// full id so a short id typed by hand is expanded before it is written. The
// fixture carries the display table: a reverse mapping from a short id only
// means something when a display table declares it.
const labelFixture = {
  policy: {
    models: {
      "relayfast/claude-sonnet-5": { variants: ["low", "medium", "high", "xhigh", "max"] },
      "opencode/glm-5.3-flash-free": { variants: ["low", "high", "max"] },
    },
    roles: { planner: { effort: "high" }, explorer: { effort: "low" } },
    display: FULL_DISPLAY,
  },
} as unknown as Snapshot
assert.equal(modelLabel(labelFixture, "relayfast/claude-sonnet-5", "planner"), "rf/cl-sonnet-5#h")
assert.equal(modelLabel(labelFixture, "opencode/glm-5.3-flash-free", "explorer"), "oc/glm-5.3-flash#l")
assert.equal(
  modelLabelFull(labelFixture, "relayfast/claude-sonnet-5", "planner"),
  "relayfast/claude-sonnet-5#high",
  "the picker shows the full id and the full effort",
)
assert.equal(
  modelLabelFull(labelFixture, "relayfast/gpt-5.6-sol", "planner"),
  "relayfast/gpt-5.6-sol",
  "a model without a policy variant stays a full id",
)
assert.equal(expandModelId(labelFixture, "rf/cl-sonnet-5"), "relayfast/claude-sonnet-5")
assert.equal(expandModelId(labelFixture, "rf/cl-sonnet-5#h"), "relayfast/claude-sonnet-5#h")
assert.equal(expandModelId(labelFixture, "oc/glm-5.3-flash"), "opencode/glm-5.3-flash-free")
assert.equal(expandModelId(labelFixture, "relayfast/claude-sonnet-5"), "relayfast/claude-sonnet-5")
assert.equal(expandModelId(labelFixture, "unknown/model"), "unknown/model")

// (2) A partial section: only the declared entries abbreviate. Nothing is
// merged over them, so an undeclared provider, model name, effort or role
// renders verbatim (the role keeps its first three characters).
const policyDisplay = {
  policy: {
    display: {
      providers: { "zai-coding-plan": "ZZ" },
      models: { "claude-sonnet-5": "cl5" },
      variants: { high: "H" },
      roles: { planner: "PL" },
      dropFreeSuffixFor: ["opencode"],
    },
  },
} as unknown as Snapshot
const custom = displayRules(policyDisplay.policy)
assert.equal(custom.providers["zai-coding-plan"], "ZZ")
assert.equal(shortModel("zai-coding-plan/claude-sonnet-5", custom), "ZZ/cl5")
assert.equal(shortVariant("high", custom), "H")
assert.equal(shortRole("planner", custom), "PL")
assert.equal(
  shortModel("opencode/glm-5.3-flash-free", custom),
  "opencode/glm-5.3-flash",
  "the declared dropFreeSuffixFor still applies, the undeclared provider code does not abbreviate",
)
assert.equal(shortModel("relayfast/claude-sonnet-5", custom), "relayfast/cl5")
assert.equal(shortVariant("low", custom), "low", "an undeclared effort renders verbatim")
assert.equal(shortRole("verifier", custom), "ver", "an undeclared role renders three characters")
const customLegend = legendFor(custom)
assert.deepEqual(customLegend, [
  "providers: ZZ=zai-coding-plan",
  "model names: cl5=claude",
  "effort: H=high",
  "roles: PL=planner",
])
assert.ok(customLegend.some((line) => line.includes("cl5=claude")), customLegend.join(" | "))
assert.ok(!customLegend.some((line) => line.includes("rf=relayfast")), "no built-in table is merged in")
assert.ok(!customLegend.some((line) => line.includes("za=zai-coding-plan")), customLegend.join(" | "))

// (3) A policy without the section: full ids (including `-free`), a
// three-character role, the effort verbatim, and a `none` legend.
const barePolicy = { policy: { models: {}, roles: {} } } as unknown as Snapshot
const none = displayRules(barePolicy.policy)
assert.deepEqual(none, { providers: {}, models: {}, variants: {}, roles: {}, dropFreeSuffixFor: [] })
assert.deepEqual(displayRules(undefined), none, "an absent policy is the same empty set")
assert.equal(shortModel("zai-coding-plan/claude-sonnet-5", none), "zai-coding-plan/claude-sonnet-5")
assert.equal(shortModel("opencode/glm-5.3-flash-free", none), "opencode/glm-5.3-flash-free")
assert.equal(shortProvider("zai-coding-plan", none), "zai-coding-plan")
assert.equal(shortProvider("unknown-provider", none), "unknown-provider")
assert.equal(shortModelName("claude-sonnet-5", none), "claude-sonnet-5")
assert.equal(shortRole("planner", none), "pla")
assert.equal(shortVariant("high", none), "high")
assert.equal(shortVariant(undefined, none), undefined)
assert.deepEqual(legendFor(none), ["providers: none", "model names: none", "effort: none", "roles: none"])
// The default argument is that same empty set.
assert.equal(shortModel("opencode/glm-5.3-flash-free"), "opencode/glm-5.3-flash-free")
assert.equal(shortRole("unknown-role"), "unk")
assert.deepEqual(legendFor(), ["providers: none", "model names: none", "effort: none", "roles: none"])

// The session-log line renders with the caller's rules; the default is the
// empty set, so a one-argument call shows full ids.
const logEntry = {
  at: "2026-01-01T10:00:00Z",
  role: "planner",
  from: "zai-coding-plan/glm-5.3",
  to: "opencode/glm-5.3-flash-free",
  reason: "preset default",
}
const emptyLog = logLine(logEntry, none)
assert.ok(
  emptyLog.endsWith("planner: zai-coding-plan/glm-5.3 → opencode/glm-5.3-flash-free (preset default)"),
  emptyLog,
)
assert.ok(
  logLine(logEntry, full).endsWith("planner: za/glm-5.3 → oc/glm-5.3-flash (preset default)"),
  logLine(logEntry, full),
)
assert.equal(logLine(logEntry), emptyLog, "no rules argument renders the empty set")

assert.equal(readFileSync(policyFile, "utf8"), policyBytesBefore, "reading the display rules never writes the policy")
assert.equal(
  existsSync(runtimeFile) ? readFileSync(runtimeFile, "utf8") : undefined,
  runtimeBytesBefore,
  "reading the display rules never writes the runtime",
)
assert.deepEqual(defaultRuntimeDoc(undefined), {
  schemaVersion: 1,
  userPresets: {},
  roleOverrides: {},
  exhaustedUntil: {},
  sessionLog: [],
})

// --- user presets: schema, name rules, save, apply, delete ------------------
//
// A dedicated policy document: these assertions need a known catalogue with
// variants, two declared roles plus one that resolves to nothing, and two preset
// names a user preset could collide with (D14).

const userPolicy = {
  version: 1,
  active_preset: "steady",
  roles: {
    planner: { agents: ["pipe/planner"], effort: "high" },
    mechanical: { agents: ["pipe/mechanical"] },
    verifier: { agents: ["pipe/verifier"] },
  },
  models: {
    "fixture/alpha": { variants: ["low", "high"] },
    "fixture/beta": {},
    "fixture/gamma": {},
  },
  presets: {
    // `extra` is named by the chain only: no `extra` entry under `roles`. It is
    // the one shape that tells the display order apart from the declared set
    // (D4/L3): it renders as a row, and a save or load must skip it.
    steady: { planner: ["fixture/alpha"], mechanical: ["fixture/beta"], extra: ["fixture/beta"] },
    cheap: { planner: ["fixture/gamma"], mechanical: ["fixture/gamma"] },
  },
  overrides: [],
}
writeFileSync(policyFile, JSON.stringify(userPolicy, null, 2), "utf8")

const exhaustedUntilIso = new Date(Date.now() + 45 * 60_000).toISOString()

// D15: the row renders the info colour (and no text marker) for
// `source === "override"`, so this is the set of coloured rows for a snapshot.
function overrideRows(view: Snapshot): string[] {
  return view.roles.filter((role) => role.source === "override").map((role) => role.name)
}

function sourceOf(view: Snapshot, role: string): string | undefined {
  return view.roles.find((entry) => entry.name === role)?.source
}

// --- resolution only counts declared presets -------------------------------
//
// A runtime document can name a preset the policy does not declare (a hand
// edit, or a policy that dropped the name). Reading must ignore it: the panel
// has to name and resolve the preset dispatch uses for the same documents, and
// a read must never rewrite the file. `userPolicy` is the fixture for the whole
// rule because its active preset is `steady`, not `default`.

const ghostDoc = JSON.stringify({ schemaVersion: 1, activePreset: "ghost", roleOverrides: {} })
writeFileSync(runtimeFile, ghostDoc, "utf8")
const ghostView = readSnapshot(ctx, sessionID)
assert.equal(ghostView.activePreset, "steady", "an undeclared stored preset is not the selection")
assert.equal(ghostView.presetSource, "policy", "the policy preset is reported instead")
assert.equal(ghostView.effectivePreset, "steady", "the policy preset is what dispatch resolves")
const ghostPlanner = ghostView.roles.find((role) => role.name === "planner")
assert.equal(ghostPlanner?.model, "fixture/alpha", "the policy preset's chain carries the role")
assert.equal(ghostPlanner?.source, "preset")
assert.equal(ghostView.customSet, false, "nothing diverges, so the row names the preset")
assert.equal(displayPreset(ghostView), "steady", "the row and the header name the policy preset")
assert.equal(readFileSync(runtimeFile, "utf8"), ghostDoc, "reading never rewrites the runtime document")

// A declared runtime preset still wins, and its own chain is what resolves.
const declaredDoc = JSON.stringify({ schemaVersion: 1, activePreset: "cheap", roleOverrides: {} })
writeFileSync(runtimeFile, declaredDoc, "utf8")
const declaredView = readSnapshot(ctx, sessionID)
assert.equal(declaredView.activePreset, "cheap", "a declared runtime preset is the selection")
assert.equal(declaredView.presetSource, "runtime")
assert.equal(declaredView.effectivePreset, "cheap")
const declaredPlanner = declaredView.roles.find((role) => role.name === "planner")
assert.equal(declaredPlanner?.model, "fixture/gamma", "the runtime preset's chain carries the role")
assert.equal(declaredPlanner?.source, "preset")
assert.equal(displayPreset(declaredView), "cheap", "the row and the header name the runtime preset")

// Neither name is declared: the roles fall back to the first preset in
// `presets`, and no name outside it is reported as the selection.
const anonymousPolicyFile = path.join(scratch, "anonymous-policy.json")
writeFileSync(
  anonymousPolicyFile,
  JSON.stringify({
    version: 1,
    active_preset: "nowhere",
    roles: { planner: { agents: ["pipe/planner"], effort: "high" } },
    models: { "fixture/alpha": {}, "fixture/gamma": {} },
    presets: { plan_a: { planner: ["fixture/alpha"] }, plan_b: { planner: ["fixture/gamma"] } },
    overrides: [],
  }),
  "utf8",
)
process.env.OPENCODE_MODEL_POLICY_FILE = anonymousPolicyFile
const anonymousView = readSnapshot(ctx, sessionID)
assert.equal(anonymousView.activePreset, undefined, "no undeclared name is reported as the selection")
assert.equal(anonymousView.presetSource, "none")
assert.equal(anonymousView.effectivePreset, "plan_a", "the first declared preset carries the roles")
assert.equal(anonymousView.roles[0].model, "fixture/alpha")
setEnv()
console.log("resolution: an undeclared preset never becomes the reported selection")

// Malformed entries are ignored on read and a marker naming one is dropped.
const mixedDoc = JSON.stringify({
  schemaVersion: 1,
  activePreset: "steady",
  activeUserPreset: "broken preset",
  userPresets: {
    "kept preset": { planner: "fixture/alpha#low", mechanical: 7 },
    "broken preset": "not an object",
    "empty preset": {},
  },
  roleOverrides: {},
})
writeFileSync(runtimeFile, mixedDoc, "utf8")
const mixedPresets = readSnapshot(ctx, sessionID)
assert.deepEqual(mixedPresets.userPresets, ["kept preset"], "only the valid entry is listed")
assert.deepEqual(mixedPresets.runtime.userPresets["kept preset"], { planner: "fixture/alpha#low" })
assert.equal(mixedPresets.activeUserPreset, undefined, "a marker naming a dropped preset is ignored")
assert.equal(readFileSync(runtimeFile, "utf8"), mixedDoc, "reading a malformed preset map never rewrites it")

// Insertion order is preserved, and the snake-case aliases are read.
writeFileSync(
  runtimeFile,
  JSON.stringify({
    schemaVersion: 1,
    activePreset: "steady",
    user_presets: {
      zeta: { planner: "fixture/beta" },
      alpha: { planner: "fixture/gamma" },
      mid: { mechanical: "fixture/beta" },
    },
    active_user_preset: "alpha",
    role_overrides: {},
  }),
  "utf8",
)
const orderedPresets = readSnapshot(ctx, sessionID)
assert.deepEqual(orderedPresets.userPresets, ["zeta", "alpha", "mid"], "saved presets keep insertion order")
assert.equal(orderedPresets.activeUserPreset, "alpha", "the snake-case marker is honoured")
assert.deepEqual(orderedPresets.runtime.userPresets.zeta, { planner: "fixture/beta" })

// A marker naming a missing preset is ignored even when the map is otherwise fine.
writeFileSync(
  runtimeFile,
  JSON.stringify({
    schemaVersion: 1,
    activePreset: "steady",
    activeUserPreset: "never saved",
    userPresets: { zeta: { planner: "fixture/beta" } },
    roleOverrides: {},
  }),
  "utf8",
)
const danglingMarker = readSnapshot(ctx, sessionID)
assert.equal(danglingMarker.activeUserPreset, undefined, "a dangling marker is not exposed")
assert.equal(danglingMarker.activePreset, "steady", "the policy preset is shown instead")
assert.deepEqual(danglingMarker.userPresets, ["zeta"])

// Capture records model#variant per role and reports the rest.
writeFileSync(
  runtimeFile,
  JSON.stringify({ schemaVersion: 1, activePreset: "steady", roleOverrides: {} }),
  "utf8",
)
const captureView = readSnapshot(ctx, sessionID)
const plannerView = captureView.roles.find((entry) => entry.name === "planner")
const verifierView = captureView.roles.find((entry) => entry.name === "verifier")
assert.equal(roleEntry(plannerView!), "fixture/alpha#high", "the effort-derived variant is captured")
assert.equal(roleEntry(verifierView!), undefined, "a role with no resolution has no entry")
const captured = captureRoles(captureView)
// A chain-only role does reach the captured map (D5: capture is a pure read of
// the snapshot), which is exactly why the save filter has to drop it again.
assert.deepEqual(captured.roles, {
  planner: "fixture/alpha#high",
  mechanical: "fixture/beta",
  extra: "fixture/beta",
})
assert.deepEqual(captured.unresolved, ["verifier"])
assert.equal("verifier" in captured.roles, false, "an unresolved role is not stored")
assert.equal(captureView.policy?.roleOrder.includes("extra"), true, "a chain-only role is in the display order")
assert.equal(captureView.policy?.declaredRoles.includes("extra"), false, "it is not declared by the roles block")
assert.equal(captureView.roles.find((entry) => entry.name === "extra")?.source, "preset", "it still renders")
assert.equal(describeUserPreset(mixedPresets, "kept preset"), "planner=fixture/alpha#low")
assert.equal(describeUserPreset(mixedPresets, "nope"), "not a saved preset")

// Name rules: composed/decomposed are one spelling, collisions are case-blind.
const composedName = "бесплатный план"
const decomposedName = composedName.replace("й", "и\u0306")
assert.notEqual(decomposedName, composedName, "the decomposed spelling is a different string")
assert.equal([...decomposedName].length, [...composedName].length + 1)

writeFileSync(
  runtimeFile,
  JSON.stringify({
    schemaVersion: 1,
    activePreset: "steady",
    userPresets: {
      [composedName]: { planner: "fixture/beta" },
      "Old Name": { planner: "fixture/gamma" },
    },
    roleOverrides: {},
  }),
  "utf8",
)
const nameView = readSnapshot(ctx, sessionID)

const trimmedName = checkPresetName(nameView, "  cheap planning  ")
if (!trimmedName.ok) throw new Error(`"cheap planning" must be accepted: ${trimmedName.detail}`)
assert.equal(trimmedName.name, "cheap planning", "the name is trimmed")
assert.equal(trimmedName.replaces, undefined)

const cyrillicName = checkPresetName(nameView, composedName)
if (!cyrillicName.ok) throw new Error(`the Cyrillic name must be accepted: ${cyrillicName.detail}`)
assert.equal(cyrillicName.name, composedName, "the typed spelling is stored")

const decomposedCheck = checkPresetName(nameView, decomposedName)
if (!decomposedCheck.ok) throw new Error(`the decomposed spelling must be accepted: ${decomposedCheck.detail}`)
assert.equal(decomposedCheck.name, composedName, "NFC collapses the decomposed spelling")
assert.equal(decomposedCheck.replaces, composedName, "it replaces the stored preset instead of adding one")

const caseFoldedCheck = checkPresetName(nameView, "OLD NAME")
if (!caseFoldedCheck.ok) throw new Error(`"OLD NAME" must be accepted: ${caseFoldedCheck.detail}`)
assert.equal(caseFoldedCheck.replaces, "Old Name", "the stored spelling is reported for the overwrite")

const separatorCheck = checkPresetName(nameView, "plan 1.2_beta-x")
assert.equal(separatorCheck.ok, true, "dot, underscore, space and hyphen are allowed")

// A name is a user-facing label, not an identifier: punctuation, symbols, marks
// and non-Latin scripts are all accepted, and the typed spelling is stored.
for (const accepted of [
  "ds/ds flash",
  "deepseek / all",
  "plan (free)",
  "50% + free!",
  "plan 🚀",
  "a/b",
  "a\\b",
  "а\u0301",
  "it's (mine)",
  "#1",
]) {
  const result = checkPresetName(nameView, accepted)
  if (!result.ok) throw new Error(`"${accepted}" must be accepted: ${result.detail}`)
  assert.equal(result.name, accepted, `"${accepted}" is stored as typed`)
}

// L1: a run of ASCII spaces is one name. The collapsed spelling is both what is
// stored and what the stored key compares equal to, so the overwrite is offered
// instead of a second row. Only U+0020 collapses — a non-breaking space fails the
// printable predicate instead of turning into a space the name never had.
const spacedSnapshot = {
  presets: ["default"],
  runtime: { userPresets: { "cheap planning": { planner: "fixture/beta" } } },
} as unknown as Snapshot
const collapsedCheck = checkPresetName(spacedSnapshot, "cheap  planning")
if (!collapsedCheck.ok) throw new Error(`"cheap  planning" must be accepted: ${collapsedCheck.detail}`)
assert.equal(collapsedCheck.name, "cheap planning", "a run of ASCII spaces collapses to one space")
assert.equal(collapsedCheck.replaces, "cheap planning", "the collapsed name is the key already stored")

const collisionSnapshot = {
  presets: ["default", "план"],
  runtime: { userPresets: {} },
} as unknown as Snapshot
assert.equal(checkPresetName(collisionSnapshot, "Default").ok, false, "a policy preset name is not reusable")
assert.equal(checkPresetName(collisionSnapshot, "План").ok, false, "the collision check ignores case")

function rejectName(snapshot: Snapshot, raw: unknown, label: string): void {
  const result = checkPresetName(snapshot, raw)
  assert.equal(result.ok, false, `${label} must be rejected`)
}

rejectName(nameView, "", "an empty name")
rejectName(nameView, "   ", "a blank name")
rejectName(nameView, 42, "a non-string name")
rejectName(nameView, "x".repeat(PRESET_NAME_MAX + 1), `${PRESET_NAME_MAX + 1} code points`)
rejectName(nameView, "a\nb", "a newline")
rejectName(nameView, "cheap\tplanning", "a tab")
rejectName(nameView, "a\u0007b", "a control character")
rejectName(nameView, "a\u00A0b", "a non-breaking space")
rejectName(nameView, "cheap\u00A0planning", "a non-breaking space instead of a space")
rejectName(nameView, "cheap\u3000planning", "an ideographic space")
rejectName(nameView, "a\u200Bb", "a zero-width space")
rejectName(nameView, "a\u202Eb", "a right-to-left override")
rejectName(nameView, "a\uD800b", "a lone surrogate")
rejectName(nameView, "a\uE000b", "a private-use character")
const controlRejection = checkPresetName(nameView, "a\u0007b")
assert.equal(
  controlRejection.ok === false && controlRejection.detail,
  "printable characters only; control characters and non-space whitespace are not allowed",
  "the rejection names the prohibition",
)
assert.equal(checkPresetName(nameView, "x".repeat(PRESET_NAME_MAX)).ok, true, `${PRESET_NAME_MAX} code points fit`)
assert.equal(
  checkPresetName(nameView, "𝐀".repeat(PRESET_NAME_MAX)).ok,
  true,
  "code points are counted, not UTF-16 units",
)
rejectName(nameView, "𝐀".repeat(PRESET_NAME_MAX + 1), `${PRESET_NAME_MAX + 1} astral code points`)

// Save stores the map and makes it the current set in the same write: the marker
// names the saved preset, `roleOverrides` is exactly the saved map (replaced, not
// merged) and `activePreset` stays a policy preset.
const saveSeed = {
  schemaVersion: 1,
  activePreset: "steady",
  // A pin the save cannot reproduce: `verifier` resolves to nothing, so the saved
  // set omits it and replacing the map drops it.
  roleOverrides: { verifier: "fixture/beta" },
  exhaustedUntil: { "fixture/beta": exhaustedUntilIso },
  sessionLog: [
    { at: new Date().toISOString(), role: "planner", from: "fixture/alpha", to: "fixture/gamma", reason: "quota" },
  ],
  orchestratorOnly: { keep: true },
}
writeFileSync(runtimeFile, JSON.stringify(saveSeed, null, 2), "utf8")

const savedOne = saveUserPreset(ctx, sessionID, "cheap planning", {
  planner: "fixture/alpha#high",
  mechanical: "fixture/beta",
})
if (!savedOne.ok) throw new Error(`saveUserPreset failed: ${savedOne.detail}`)
assert.equal(savedOne.saved, 2)
assert.equal(savedOne.dropped.length, 0)
const afterSaveOne = readRuntime()
assert.deepEqual(afterSaveOne.userPresets["cheap planning"], {
  planner: "fixture/alpha#high",
  mechanical: "fixture/beta",
})
assert.equal(afterSaveOne.activePreset, "steady", "saving leaves activePreset a policy preset")
assert.equal(afterSaveOne.activeUserPreset, "cheap planning", "saving selects the saved set")
assert.deepEqual(
  afterSaveOne.roleOverrides,
  afterSaveOne.userPresets["cheap planning"],
  "roleOverrides equals the saved map",
)
assert.deepEqual(
  afterSaveOne.roleOverrides,
  { planner: "fixture/alpha#high", mechanical: "fixture/beta" },
  "the saved entries are the overrides in force",
)
assert.equal("verifier" in afterSaveOne.roleOverrides, false, "an override outside the saved set is dropped")
assert.equal(readSnapshot(ctx, sessionID).activeUserPreset, "cheap planning", "the marker is read back")
const savedSetView = readSnapshot(ctx, sessionID)
assert.equal(savedSetView.customSet, false, "a freshly saved set matches its preset")
assert.equal(displayPreset(savedSetView), "cheap planning", "the saved preset is the display name")
assert.ok(
  readSnapshot(ctx, sessionID).userPresets.includes("cheap planning"),
  "a re-read snapshot lists the saved preset",
)
assert.ok(readSnapshot(ctx, sessionID).presets.includes(afterSaveOne.activePreset), "activePreset is a policy preset")
assert.deepEqual(afterSaveOne.exhaustedUntil, saveSeed.exhaustedUntil, "exhaustedUntil preserved")
assert.deepEqual(afterSaveOne.sessionLog, saveSeed.sessionLog, "sessionLog preserved")
assert.deepEqual(afterSaveOne.orchestratorOnly, saveSeed.orchestratorOnly, "unknown keys preserved")
assert.deepEqual(leftovers(path.dirname(runtimeFile)), [], "temp files left behind")

// D15: the set the save materialized is the preset's choice, not a manual pin.
// Its overrides equal the saved entries, so every applied role is `user` and the
// sidebar shows no `override` marker; a role the save omitted keeps its own source.
const afterSaveView = readSnapshot(ctx, sessionID)
assert.equal(sourceOf(afterSaveView, "planner"), "user", "a saved set is not a manual pin")
assert.equal(sourceOf(afterSaveView, "mechanical"), "user")
assert.deepEqual(overrideRows(afterSaveView), [], "no role row shows the override marker")
assert.equal(sourceOf(afterSaveView, "extra"), "preset", "a chain-only role still resolves through the chain")
assert.ok(
  describeRole(afterSaveView, afterSaveView.roles.find((entry) => entry.name === "planner")!).includes(
    "from the active user preset",
  ),
  "the details view names the user preset as the source",
)

// L1 round-trip: the key written and the key read back are the same string, so a
// name typed with two spaces against a store that holds the collapsed spelling
// replaces it instead of adding a twin.
writeFileSync(
  runtimeFile,
  JSON.stringify(
    {
      schemaVersion: 1,
      activePreset: "steady",
      userPresets: { "cheap planning": { planner: "fixture/beta" } },
      roleOverrides: {},
    },
    null,
    2,
  ),
  "utf8",
)
const spacedSave = saveUserPreset(ctx, sessionID, "cheap  planning", { planner: "fixture/gamma" })
if (!spacedSave.ok) throw new Error(`saveUserPreset failed: ${spacedSave.detail}`)
assert.equal(spacedSave.name, "cheap planning", "the stored key is the collapsed spelling")
assert.equal(spacedSave.replaces, "cheap planning", "the existing collapsed key is the one replaced")
assert.deepEqual(readRuntime().userPresets, { "cheap planning": { planner: "fixture/gamma" } })
const spacedSaveAgain = saveUserPreset(ctx, sessionID, "cheap  planning", { planner: "fixture/gamma" })
if (!spacedSaveAgain.ok) throw new Error(`saveUserPreset failed: ${spacedSaveAgain.detail}`)
assert.deepEqual(Object.keys(readRuntime().userPresets), ["cheap planning"], "saving twice leaves exactly one key")
assert.deepEqual(
  Object.keys(readSnapshot(ctx, sessionID).runtime.userPresets),
  Object.keys(readRuntime().userPresets),
  "a stored key and its read-back key are the same string",
)
assert.deepEqual(leftovers(path.dirname(runtimeFile)), [], "temp files left behind")

// A role only a preset chain names resolves on screen but is not saved (L3): the
// policy check would reject that override as an unknown role.
const chainOnlySave = saveUserPreset(ctx, sessionID, "chain view", {
  planner: "fixture/beta",
  extra: "fixture/beta",
})
if (!chainOnlySave.ok) throw new Error(`saveUserPreset failed: ${chainOnlySave.detail}`)
assert.deepEqual(chainOnlySave.dropped, [{ role: "extra", reason: "role" }])
assert.equal(chainOnlySave.saved, 1)
assert.deepEqual(readRuntime().userPresets["chain view"], { planner: "fixture/beta" })
assert.equal("extra" in readRuntime().userPresets["chain view"], false, "no saved entry names an undeclared role")
assert.deepEqual(leftovers(path.dirname(runtimeFile)), [], "temp files left behind")

const savedPartial = saveUserPreset(ctx, sessionID, "partial set", {
  planner: "fixture/beta",
  ghost: "fixture/beta",
  mechanical: "gone/model",
})
if (!savedPartial.ok) throw new Error(`saveUserPreset failed: ${savedPartial.detail}`)
assert.deepEqual(savedPartial.dropped, [
  { role: "ghost", reason: "role" },
  { role: "mechanical", reason: "model" },
])
assert.deepEqual(readRuntime().userPresets["partial set"], { planner: "fixture/beta" })

const beforeRefusedSaves = readFileSync(runtimeFile, "utf8")
assert.equal(
  saveUserPreset(ctx, sessionID, "nothing", { ghost: "fixture/beta" }).ok,
  false,
  "a save with no applicable role is refused",
)
assert.equal(saveUserPreset(ctx, sessionID, "a\nb", { planner: "fixture/beta" }).ok, false, "an invalid name is refused")
assert.equal(saveUserPreset(ctx, sessionID, "steady", { planner: "fixture/beta" }).ok, false, "a policy name is refused")
assert.equal(readFileSync(runtimeFile, "utf8"), beforeRefusedSaves, "refused saves leave the file byte-identical")

// Overwriting removes every raw key that compares equal — a hand-edited NFD or
// differently-cased duplicate must not survive as a second row — and stores the
// typed spelling.
const upperName = composedName.toUpperCase()
assert.notEqual(upperName, composedName)
writeFileSync(
  runtimeFile,
  JSON.stringify({
    schemaVersion: 1,
    activePreset: "steady",
    activeUserPreset: decomposedName,
    userPresets: {
      [composedName]: { planner: "fixture/beta" },
      [decomposedName]: { planner: "fixture/gamma" },
      [upperName]: { planner: "fixture/beta" },
    },
    roleOverrides: {},
  }),
  "utf8",
)
assert.equal(readSnapshot(ctx, sessionID).userPresets.length, 3, "the twins are visible before the overwrite")
const overwritten = saveUserPreset(ctx, sessionID, decomposedName, { planner: "fixture/alpha#low" })
if (!overwritten.ok) throw new Error(`saveUserPreset failed: ${overwritten.detail}`)
assert.equal(overwritten.replaces, composedName, "the first matching stored key is reported")
const afterOverwrite = readRuntime()
assert.deepEqual(Object.keys(afterOverwrite.userPresets), [composedName], "one key remains, in the typed spelling")
assert.deepEqual(afterOverwrite.userPresets[composedName], { planner: "fixture/alpha#low" })
assert.equal(afterOverwrite.activeUserPreset, composedName, "the overwrite re-points the marker at the saved key")
assert.deepEqual(
  afterOverwrite.roleOverrides,
  { planner: "fixture/alpha#low" },
  "the overwrite replaces the overrides with the new set",
)
assert.equal(afterOverwrite.activePreset, "steady", "the overwrite keeps a policy preset selected")
assert.deepEqual(leftovers(path.dirname(runtimeFile)), [], "temp files left behind")

// Apply replaces roleOverrides, filters what the policy cannot use, and keeps
// activePreset a policy preset.
const applySeed = {
  schemaVersion: 1,
  activePreset: "steady",
  userPresets: {
    "mixed set": {
      planner: "fixture/alpha#high",
      mechanical: "fixture/gamma#low",
      ghost: "fixture/beta",
      verifier: "gone/model",
    },
    "trailing separator": {
      planner: "fixture/gamma#",
      mechanical: "fixture/beta#",
      extra: "fixture/beta",
    },
  },
  roleOverrides: { planner: "fixture/beta", verifier: "fixture/beta" },
  exhaustedUntil: { "fixture/beta": exhaustedUntilIso },
  sessionLog: [
    { at: new Date().toISOString(), role: "planner", from: "fixture/beta", to: "fixture/alpha", reason: "429" },
  ],
  orchestratorOnly: { keep: true },
}
writeFileSync(runtimeFile, JSON.stringify(applySeed, null, 2), "utf8")

const appliedPreset = applyUserPreset(ctx, sessionID, "mixed set")
if (!appliedPreset.ok) throw new Error(`applyUserPreset failed: ${appliedPreset.detail}`)
assert.deepEqual(appliedPreset.applied, { planner: "fixture/alpha#high", mechanical: "fixture/gamma" })
assert.deepEqual(appliedPreset.skipped, [
  { role: "ghost", reason: "role" },
  { role: "verifier", reason: "model" },
])
assert.deepEqual(appliedPreset.strippedVariants, ["mechanical"], "an unsupported variant keeps the model")
const afterApply = readRuntime()
assert.deepEqual(
  afterApply.roleOverrides,
  { planner: "fixture/alpha#high", mechanical: "fixture/gamma" },
  "the override map is replaced, so stale entries are gone",
)
assert.equal(afterApply.activeUserPreset, "mixed set")
assert.equal(afterApply.activePreset, "steady", "a policy preset stays selected")
assert.deepEqual(afterApply.exhaustedUntil, applySeed.exhaustedUntil, "exhaustedUntil preserved")
assert.deepEqual(afterApply.sessionLog, applySeed.sessionLog, "sessionLog preserved")
assert.deepEqual(afterApply.orchestratorOnly, applySeed.orchestratorOnly, "unknown keys preserved")
assert.deepEqual(leftovers(path.dirname(runtimeFile)), [], "temp files left behind")
const appliedView = readSnapshot(ctx, sessionID)
assert.equal(appliedView.activeUserPreset, "mixed set")
assert.equal(appliedView.customSet, false, "a loaded preset matches its saved map")
assert.equal(displayPreset(appliedView), "mixed set", "the loaded preset is the display name")
assert.ok(appliedView.presets.includes(appliedView.activePreset!), "activePreset still names a policy preset")
assert.equal(appliedView.roles.find((entry) => entry.name === "planner")?.model, "fixture/alpha")
assert.equal(appliedView.roles.find((entry) => entry.name === "planner")?.variant, "high")

// D15: a load obeys the same display rule as a save. Both entries name the saved
// model; `mechanical` was written without the unsupported `#low` variant, but the
// stage model is the same, so it is still the preset's choice.
assert.equal(sourceOf(appliedView, "planner"), "user", "a loaded set is not a manual pin")
assert.equal(sourceOf(appliedView, "mechanical"), "user", "a stripped variant is still the same stage model")
assert.deepEqual(overrideRows(appliedView), [], "a matching set marks no role")

// L2: an entry ending in `#` is applied as its bare model id — the policy check
// re-derives an empty variant from the raw string and fails on it. L3: an entry
// for a role the `roles` block does not declare is skipped, not applied.
const appliedEmpty = applyUserPreset(ctx, sessionID, "trailing separator")
if (!appliedEmpty.ok) throw new Error(`applyUserPreset failed: ${appliedEmpty.detail}`)
assert.deepEqual(appliedEmpty.applied, { planner: "fixture/gamma", mechanical: "fixture/beta" })
assert.deepEqual(appliedEmpty.skipped, [{ role: "extra", reason: "role" }], "a chain-only role is not applied")
assert.deepEqual(appliedEmpty.strippedVariants, [], "an empty variant is not a stripped variant")
const afterEmpty = readRuntime()
assert.deepEqual(
  afterEmpty.roleOverrides,
  { planner: "fixture/gamma", mechanical: "fixture/beta" },
  "the separator is gone and the map is replaced",
)
for (const [role, entry] of Object.entries(afterEmpty.roleOverrides as Record<string, string>)) {
  assert.equal(entry.includes("#"), false, `${role} must carry no separator`)
  assert.notEqual(entry, "", `${role} must not carry an empty variant`)
}
assert.equal(JSON.stringify(afterEmpty.roleOverrides).includes("#"), false, "no separator anywhere in the written map")
assert.equal(afterEmpty.activeUserPreset, "trailing separator")
assert.deepEqual(leftovers(path.dirname(runtimeFile)), [], "temp files left behind")

// Every applied role equals its saved entry here — `model#` compares equal to
// `model` after `parseEntry` — so the load leaves no row marked as a manual pin.
const appliedEmptyView = readSnapshot(ctx, sessionID)
assert.equal(sourceOf(appliedEmptyView, "planner"), "user")
assert.equal(sourceOf(appliedEmptyView, "mechanical"), "user")
assert.deepEqual(overrideRows(appliedEmptyView), [], "no applied role is marked as a manual pin")

// A load with no applicable entry is refused and writes nothing.
const staleSeed = {
  schemaVersion: 1,
  activePreset: "steady",
  userPresets: { stale: { ghost: "fixture/beta", planner: "gone/model" } },
  roleOverrides: { planner: "fixture/beta" },
  exhaustedUntil: { "fixture/beta": exhaustedUntilIso },
}
writeFileSync(runtimeFile, JSON.stringify(staleSeed, null, 2), "utf8")
const beforeStaleLoad = readFileSync(runtimeFile, "utf8")
assert.equal(applyUserPreset(ctx, sessionID, "stale").ok, false, "a load with nothing applicable is refused")
assert.equal(readFileSync(runtimeFile, "utf8"), beforeStaleLoad, "the refused load is byte-identical")
assert.equal(applyUserPreset(ctx, sessionID, "no such preset").ok, false, "an unknown name is refused")
assert.equal(readFileSync(runtimeFile, "utf8"), beforeStaleLoad, "the unknown name writes nothing")
assert.deepEqual(leftovers(path.dirname(runtimeFile)), [], "temp files left behind")

// Delete removes one entry, keeps the overrides, and clears only its own marker.
const deleteSeed = {
  schemaVersion: 1,
  activePreset: "steady",
  activeUserPreset: "keep me",
  userPresets: {
    "keep me": { planner: "fixture/beta" },
    "drop me": { planner: "fixture/gamma" },
  },
  roleOverrides: { planner: "fixture/alpha#high" },
  exhaustedUntil: { "fixture/beta": exhaustedUntilIso },
}
writeFileSync(runtimeFile, JSON.stringify(deleteSeed, null, 2), "utf8")

const deletedOther = deleteUserPreset(ctx, sessionID, "drop me")
if (!deletedOther.ok) throw new Error(`deleteUserPreset failed: ${deletedOther.detail}`)
assert.deepEqual(Object.keys(readRuntime().userPresets), ["keep me"])
assert.equal(readRuntime().activeUserPreset, "keep me", "deleting another preset keeps the marker")
assert.deepEqual(readRuntime().roleOverrides, deleteSeed.roleOverrides, "the overrides survive")
assert.deepEqual(readRuntime().exhaustedUntil, deleteSeed.exhaustedUntil, "exhaustedUntil preserved")

const deletedActive = deleteUserPreset(ctx, sessionID, "keep me")
if (!deletedActive.ok) throw new Error(`deleteUserPreset failed: ${deletedActive.detail}`)
assert.equal(deletedActive.markerCleared, true)
assert.equal(readRuntime().activeUserPreset, undefined, "deleting the active preset clears the marker")
assert.deepEqual(readRuntime().roleOverrides, deleteSeed.roleOverrides, "the applied overrides stay")
assert.deepEqual(readRuntime().userPresets, {})

const beforeUnknownDelete = readFileSync(runtimeFile, "utf8")
assert.equal(deleteUserPreset(ctx, sessionID, "gone").ok, false, "an unknown name is refused")
assert.equal(readFileSync(runtimeFile, "utf8"), beforeUnknownDelete, "the refused delete writes nothing")

// A hand-edited differently-cased twin goes with the name it matches.
writeFileSync(
  runtimeFile,
  JSON.stringify({
    schemaVersion: 1,
    activePreset: "steady",
    activeUserPreset: "plan one",
    userPresets: { "Plan One": { planner: "fixture/beta" }, "plan one": { planner: "fixture/gamma" } },
    roleOverrides: {},
  }),
  "utf8",
)
const twinDelete = deleteUserPreset(ctx, sessionID, "Plan One")
if (!twinDelete.ok) throw new Error(`deleteUserPreset failed: ${twinDelete.detail}`)
assert.deepEqual(readRuntime().userPresets, {}, "both raw keys of one preset are removed")
assert.equal(readRuntime().activeUserPreset, undefined)
assert.deepEqual(leftovers(path.dirname(runtimeFile)), [], "temp files left behind")

// Every other selection change clears the marker and keeps the saved presets.
// The seeded map holds two pins, both vouched for by the marker.
function markerSeed() {
  return {
    schemaVersion: 1,
    activePreset: "steady",
    activeUserPreset: "cheap planning",
    userPresets: { "cheap planning": { planner: "fixture/beta", mechanical: "fixture/gamma" } },
    roleOverrides: { planner: "fixture/beta", mechanical: "fixture/gamma" },
    exhaustedUntil: {},
    sessionLog: [],
  }
}

writeFileSync(runtimeFile, JSON.stringify(markerSeed(), null, 2), "utf8")
assert.equal(setRoleOverride(ctx, sessionID, "planner", "fixture/gamma").ok, true)
assert.equal(
  readRuntime().activeUserPreset,
  "cheap planning",
  "a manual override keeps the marker, so untouched roles stay attributed",
)
const afterManualEdit = readSnapshot(ctx, sessionID)
assert.equal(sourceOf(afterManualEdit, "planner"), "override", "the changed role is a manual pin")
assert.equal(sourceOf(afterManualEdit, "mechanical"), "user", "the untouched role stays the preset's")
assert.deepEqual(overrideRows(afterManualEdit), ["planner"], "only the changed role is marked")
assert.equal(afterManualEdit.customSet, true, "the changed role makes the preset custom")
assert.equal(displayPreset(afterManualEdit), "custom", "the row shows custom instead of the preset")
assert.deepEqual(readRuntime().userPresets, markerSeed().userPresets, "the saved presets survive")

writeFileSync(runtimeFile, JSON.stringify(markerSeed(), null, 2), "utf8")
assert.equal(clearRoleOverrides(ctx, sessionID).ok, true)
assert.equal(readRuntime().activeUserPreset, undefined, "clearing all overrides clears the marker")
assert.deepEqual(readRuntime().roleOverrides, {})
assert.deepEqual(readRuntime().userPresets, markerSeed().userPresets, "the saved presets survive")
const clearedView = readSnapshot(ctx, sessionID)
assert.equal(clearedView.customSet, false, "no pins means the policy preset is back")
assert.equal(displayPreset(clearedView), "steady", "the policy preset is the display name")

writeFileSync(runtimeFile, JSON.stringify(markerSeed(), null, 2), "utf8")
assert.equal(setActivePreset(ctx, sessionID, "cheap").ok, true)
const afterPolicySwitch = readRuntime()
assert.equal(afterPolicySwitch.activeUserPreset, undefined, "a policy-preset switch clears the marker")
assert.equal(afterPolicySwitch.activePreset, "cheap")
// D7: the marker named a saved preset, so the pins it vouched for go with it in
// the same write; leaving them would show a preset whose models dispatch does not
// use, because an override wins over the chain.
assert.deepEqual(afterPolicySwitch.roleOverrides, {}, "the loaded preset's pins are emptied")
assert.equal("role_overrides" in afterPolicySwitch, false, "the snake-case alias is not left behind")
assert.deepEqual(afterPolicySwitch.userPresets, markerSeed().userPresets, "the saved presets survive")
const switchedView = readSnapshot(ctx, sessionID)
assert.equal(
  switchedView.roles.find((entry) => entry.name === "planner")?.source,
  "preset",
  "every role resolves through the chain again",
)
assert.equal(switchedView.roles.find((entry) => entry.name === "planner")?.model, "fixture/gamma")
assert.equal(switchedView.customSet, false, "a policy switch returns to a matching set")
assert.equal(displayPreset(switchedView), "cheap", "the chosen policy preset is the display name")
assert.equal(
  switchedView.roles.find((entry) => entry.name === "mechanical")?.source,
  "preset",
  "no role is `user` once the marker and the pins are gone",
)
assert.deepEqual(overrideRows(switchedView), [], "a policy-preset switch leaves no marker")
assert.deepEqual(leftovers(path.dirname(runtimeFile)), [], "temp files left behind")

// (a2) The marker is matched with the same folding the save and delete paths use,
// so a hand-edited spelling still names the saved preset it means.
const spelledSeed = { ...markerSeed(), activeUserPreset: "Cheap  Planning" }
writeFileSync(runtimeFile, JSON.stringify(spelledSeed, null, 2), "utf8")
assert.equal(setActivePreset(ctx, sessionID, "cheap").ok, true)
assert.deepEqual(readRuntime().roleOverrides, {}, "a differently spelled marker still names the saved preset")
assert.equal(readRuntime().activeUserPreset, undefined)
assert.deepEqual(readRuntime().userPresets, spelledSeed.userPresets, "the saved presets survive")
assert.deepEqual(leftovers(path.dirname(runtimeFile)), [], "temp files left behind")

// D15: the source is decided per role against the active preset's entry, not from
// the marker alone. A role whose override equals its saved entry is the preset's
// choice; a differing entry, and a role the saved map does not mention, stay a
// manual pin. Reading never rewrites the document.
const diffSeed = {
  schemaVersion: 1,
  activePreset: "steady",
  activeUserPreset: "cheap planning",
  userPresets: {
    "cheap planning": { planner: "fixture/beta", mechanical: "fixture/beta" },
    "planner only": { mechanical: "fixture/beta" },
  },
  roleOverrides: { planner: "fixture/beta", mechanical: "fixture/gamma" },
  exhaustedUntil: {},
  sessionLog: [],
}
const diffDoc = JSON.stringify(diffSeed, null, 2)
writeFileSync(runtimeFile, diffDoc, "utf8")
const diffView = readSnapshot(ctx, sessionID)
assert.equal(sourceOf(diffView, "planner"), "user", "a matching entry is the preset's choice")
assert.equal(sourceOf(diffView, "mechanical"), "override", "a differing entry stays manual")
assert.deepEqual(overrideRows(diffView), ["mechanical"], "only the differing role is marked")
assert.ok(
  describeRole(diffView, diffView.roles.find((entry) => entry.name === "mechanical")!).includes("override (chain"),
  "a manual pin keeps the override phrasing",
)
assert.equal(readFileSync(runtimeFile, "utf8"), diffDoc, "deciding the source never rewrites the document")
assert.equal(diffView.customSet, true, "a differing role makes the set custom")
assert.equal(displayPreset(diffView), "custom", "the display name is custom")

const partialSeed = { ...diffSeed, activeUserPreset: "planner only", roleOverrides: { planner: "fixture/beta" } }
writeFileSync(runtimeFile, JSON.stringify(partialSeed, null, 2), "utf8")
const partialView = readSnapshot(ctx, sessionID)
assert.equal(sourceOf(partialView, "planner"), "override", "a saved map with no entry for the role proves nothing")
assert.deepEqual(overrideRows(partialView), ["planner"])
assert.equal(partialView.customSet, true, "an extra manual pin makes the set custom")

// A manual change keeps the marker (D7), so the changed role falls back to
// `override` while the untouched roles keep their `user` attribution. The set no
// longer matches the preset, so the display name becomes `custom`.
const pivotSeed = {
  schemaVersion: 1,
  activePreset: "steady",
  activeUserPreset: "cheap planning",
  userPresets: { "cheap planning": { planner: "fixture/beta" } },
  roleOverrides: { planner: "fixture/beta" },
  exhaustedUntil: {},
  sessionLog: [],
}
writeFileSync(runtimeFile, JSON.stringify(pivotSeed, null, 2), "utf8")
const pivotView = readSnapshot(ctx, sessionID)
assert.equal(sourceOf(pivotView, "planner"), "user")
assert.deepEqual(overrideRows(pivotView), [], "the marker matches, so nothing is marked")
assert.equal(pivotView.customSet, false, "a matching set is not custom")
assert.equal(displayPreset(pivotView), "cheap planning", "the matching set names its preset")
assert.equal(setRoleOverride(ctx, sessionID, "planner", "fixture/gamma").ok, true)
const afterManualPin = readSnapshot(ctx, sessionID)
assert.equal(afterManualPin.activeUserPreset, "cheap planning", "a manual change keeps the marker")
assert.equal(afterManualPin.customSet, true, "a differing role makes the set custom")
assert.equal(displayPreset(afterManualPin), "custom", "the display name is custom, not the preset")
assert.equal(sourceOf(afterManualPin, "planner"), "override", "the changed role is a manual pin again")
assert.equal(sourceOf(afterManualPin, "mechanical"), "preset", "the roles it did not touch resolve through the chain")
assert.equal(sourceOf(afterManualPin, "extra"), "preset")
assert.equal(sourceOf(afterManualPin, "verifier"), "none")
assert.deepEqual(leftovers(path.dirname(runtimeFile)), [], "temp files left behind")

// (b) Choosing a policy preset applies it: the manual set is replaced, not merged.
const manualSeed = {
  schemaVersion: 1,
  activePreset: "steady",
  userPresets: { "cheap planning": { planner: "fixture/beta" } },
  roleOverrides: { planner: "fixture/beta" },
  exhaustedUntil: {},
  sessionLog: [],
}
writeFileSync(runtimeFile, JSON.stringify(manualSeed, null, 2), "utf8")
assert.equal(displayPreset(readSnapshot(ctx, sessionID)), "custom", "a manual pin makes the set custom")
assert.equal(setActivePreset(ctx, sessionID, "cheap").ok, true)
const afterManualSwitch = readRuntime()
assert.deepEqual(afterManualSwitch.roleOverrides, {}, "a policy switch replaces the manual set")
assert.equal(afterManualSwitch.activePreset, "cheap")
assert.equal(afterManualSwitch.activeUserPreset, undefined)
assert.deepEqual(afterManualSwitch.userPresets, manualSeed.userPresets, "the saved presets survive")
assert.equal(displayPreset(readSnapshot(ctx, sessionID)), "cheap", "the chosen preset is the display name again")
assert.deepEqual(leftovers(path.dirname(runtimeFile)), [], "temp files left behind")

// (c) A dangling marker is irrelevant now: a policy switch empties any pins.
const danglingSeed = { ...manualSeed, activeUserPreset: "deleted since" }
writeFileSync(runtimeFile, JSON.stringify(danglingSeed, null, 2), "utf8")
assert.equal(setActivePreset(ctx, sessionID, "cheap").ok, true)
const afterDanglingSwitch = readRuntime()
assert.deepEqual(afterDanglingSwitch.roleOverrides, {}, "a policy switch empties the pins")
assert.equal(afterDanglingSwitch.activeUserPreset, undefined, "the dangling marker is cleared")
assert.equal(afterDanglingSwitch.activePreset, "cheap")
assert.deepEqual(afterDanglingSwitch.userPresets, danglingSeed.userPresets, "the saved presets survive")
assert.deepEqual(leftovers(path.dirname(runtimeFile)), [], "temp files left behind")

// No preset write touches a document it cannot parse.
writeFileSync(runtimeFile, "{ not json", "utf8")
assert.equal(saveUserPreset(ctx, sessionID, "x", { planner: "fixture/beta" }).ok, false)
assert.equal(applyUserPreset(ctx, sessionID, "x").ok, false)
assert.equal(deleteUserPreset(ctx, sessionID, "x").ok, false)
assert.equal(readFileSync(runtimeFile, "utf8"), "{ not json", "a malformed document is never overwritten")
console.log("user presets: schema, name rules, save/apply/delete, marker clearing OK")

// The picker's titles (`policy · x`) differ from its values (`policy:x`), so a
// host that resolved the rendered label would silently do nothing without this
// mapping. `value` wins on the documented host; every fallback is asserted.
const dialogOptions = [
  { value: "policy:cheap", title: "policy · cheap" },
  { value: "policy:steady", title: "policy · steady (active)", name: "policy · steady (active)" },
  { value: "user:cheap planning", title: "user · cheap planning" },
  { value: "action:save", title: "Save current as a new preset…" },
]
assert.equal(matchOptionValue("policy:cheap", dialogOptions), "policy:cheap", "an exact value wins")
assert.equal(
  matchOptionValue("policy · steady (active)", dialogOptions),
  "policy:steady",
  "a rendered title carrying the (active) suffix resolves to its value",
)
assert.equal(matchOptionValue("policy · cheap", dialogOptions), "policy:cheap", "a title resolves without a `name`")
assert.equal(matchOptionValue("user · cheap planning", dialogOptions), "user:cheap planning", "a `name` resolves")
assert.equal(
  matchOptionValue({ value: "user:cheap planning" }, dialogOptions),
  "user:cheap planning",
  "an object's `.value` resolves",
)
assert.equal(matchOptionValue({ title: "policy · cheap" }, dialogOptions), "policy:cheap", "an object with only a title")
assert.equal(matchOptionValue({ name: "user · cheap planning" }, dialogOptions), "user:cheap planning", "an object's `.name`")
assert.equal(matchOptionValue({ id: "action:save" }, dialogOptions), "action:save", "an object's `.id`")
assert.equal(
  matchOptionValue({ value: "no such value", title: "policy · cheap" }, dialogOptions),
  "policy:cheap",
  "a value that matches nothing falls through to the title",
)
assert.equal(matchOptionValue(undefined, dialogOptions), undefined, "a closed dialog writes nothing")
assert.equal(matchOptionValue(null, dialogOptions), undefined, "a null result writes nothing")
assert.equal(matchOptionValue("policy · unknown", dialogOptions), undefined, "an unrecognized label is a cancellation")
assert.equal(matchOptionValue("", dialogOptions), undefined, "an empty string names no option here")
assert.equal(matchOptionValue(42, dialogOptions), undefined, "a non-string, non-object result writes nothing")

// A value match beats another option's title, even when the title is the only
// exact match for the candidate in the other list.
const shadowedOptions = [
  { value: "alpha", title: "shared" },
  { value: "shared", title: "beta" },
]
assert.equal(matchOptionValue("shared", shadowedOptions), "shared", "an exact value outranks another option's title")

// The role picker's first option has an empty value, so "" is a real choice and
// must not be treated as a cancellation.
const chainOption = [{ value: "", title: "(use the preset chain)" }]
assert.equal(matchOptionValue("", chainOption), "", "an empty value is a real choice")
assert.equal(matchOptionValue("(use the preset chain)", chainOption), "", "its label maps back to the empty value")

// Restore the policy the later sections were seeded with.
writeFileSync(policyFile, JSON.stringify(synthetic, null, 2), "utf8")

// --- observation helpers ---------------------------------------------------

const event = {
  type: "message.updated",
  properties: { sessionID: "ses_1", info: { modelID: "glm-5.3", providerID: "zai-coding-plan", variant: "high" }, agent: "pipe/planner" },
}
const observation = observeEvent(event)
assert.ok(observation)
assert.equal(observation?.model, "glm-5.3")
assert.equal(observation?.provider, "zai-coding-plan")
assert.equal(observation?.agent, "pipe/planner")
assert.equal(observeEvent({ type: "session.updated", properties: { title: "x" } }), undefined)
assert.equal(roleForAgent(parsedPolicy, "pipe/planner"), "planner")
assert.equal(roleForAgent(parsedPolicy, "PIPE\\planner"), "planner")
assert.equal(roleForAgent(parsedPolicy, "general"), undefined)
console.log("observation: event extraction and role attribution OK")

const liveRuntimeAfter = existsSync(LIVE_RUNTIME) ? readFileSync(LIVE_RUNTIME, "utf8") : undefined
assert.equal(liveRuntimeAfter, liveRuntimeBefore, `verification must not write ${LIVE_RUNTIME}`)
console.log("verify: all assertions passed")
