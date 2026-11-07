// Model-policy state: path resolution, defensive loading of the two JSON layers,
// merge-safe owned-field writes, and observation of the model a subagent ran on.
//
// Layer 1 (versioned, human-curated, read-only here):
//   <project>/docs/agent-model-policy.json
// Layer 2 (runtime, gitignored, partially owned by this plugin):
//   <project>/.opencode/model-policy.json
//
// Ownership rule enforced by this module: the plugin owns `activePreset`,
// `roleOverrides`, `userPresets` and `activeUserPreset`; the orchestrator owns
// `exhaustedUntil` and `sessionLog`. Every write re-reads the file and assigns
// only the owned keys, so an orchestrator write between our read and our rename
// is never clobbered.
//
// This module deliberately has no Solid/JSX dependency so it can be exercised
// outside the TUI (see verify.ts).

export * from "./types"
export * from "./paths"
export * from "./display"
export * from "./observe"
export * from "./snapshot"
export * from "./presets"
export * from "./catalogue"

export { effectivePreset } from "./policy"
export { atomicWriteJson } from "./io"
export { clearRoleOverrides, defaultRuntimeDoc, ensureRuntime, exhaustedModels, recreateRuntime, setActivePreset, setRoleOverride } from "./runtime"
export { checkPresetName, PRESET_NAME_MAX } from "./preset-name"
