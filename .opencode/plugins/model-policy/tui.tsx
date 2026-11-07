// OpenCode CLI plugin: a control panel for the CLIHub subagent model policy.
//
// Two layers, both JSON, both read directly:
//   <project>/docs/agent-model-policy.json   versioned, human-curated, read-only
//   <project>/.opencode/model-policy.json    runtime selection
//
// The plugin owns `activePreset` / `roleOverrides` in the runtime file; the
// orchestrator owns `exhaustedUntil` / `sessionLog`. Writes are atomic and
// merge-safe, so an orchestrator write is never clobbered.
//
// The host model cannot be switched from a plugin (there is no model.set), and
// there is no button/menu primitive in the low-level JSX. Controls therefore go
// through ctx.ui.dialog.select / prompt / confirm / alert. Rendering is
// defensive: a missing or broken file degrades to a short panel and never throws.

import { Plugin } from "@opencode/plugin/tui"
import { For, Show, createEffect, createMemo, createSignal } from "solid-js"
import { createPolling, currentSessionID, readCollapsed, showToast, sidebarColors as colors, withDialog, writeCollapsed, SidebarHeader } from "../../shared/sidebar-ui"
import {
  applyUserPreset,
  captureRoles,
  checkPresetName,
  clearRoleOverrides,
  deleteUserPreset,
  describeModel,
  describePreset,
  describeUserPreset,
  describeRole,
  displayPreset,
  ensureRuntime,
  formatClock,
  formatRemaining,
  hostModelsFromClient,
  latestObservationForRole,
  logLine,
  matchOptionValue,
  mergeModelChoices,
  modelLabelFull,
  modelsFromLocation,
  observationLabel,
  observeEvent,
  readSnapshot,
  reasonLine,
  recreateRuntime,
  recordObservation,
  roleForAgent,
  roleModels,
  saveUserPreset,
  setActivePreset,
  setRoleOverride,
  displayRules,
  expandModelId,
  legendFor,
  shortModel,
  shortRole,
  shortVariant,
  truncate,
  type DisplayRules,
  type ExhaustedModel,
  type HostModel,
  type Observation,
  type RoleView,
  type Snapshot,
  type WriteResult,
} from "./state"

const POLL_MS = 2000
const SIDEBAR_EXHAUSTED_LIMIT = 2
const SIDEBAR_MODEL_MAX = 30
// The collapsed header is a single `wrapMode="none"` line, so the active preset
// name is cut well before a full 32-code-point name would overflow it.
const SIDEBAR_PRESET_MAX = 20
const DETAILS_LOG_LIMIT = 12
const CUSTOM_MODEL = "__custom__"

// The sidebar slot is re-rendered from these dialogs, so every prompt is opened
// only after the previous one has been dismissed.
let dialogOpen = false

function roleLabel(role: RoleView, rules: DisplayRules): string {
  const id = shortModel(role.model ?? "—", rules)
  const variant = shortVariant(role.variant, rules)
  return variant ? `${id}#${variant}` : id
}

/** The unabbreviated model id, for the details dialog where exactness matters. */
function roleLabelFull(role: RoleView): string {
  const id = role.model ?? "—"
  return role.variant ? `${id}#${role.variant}` : id
}

function failure(result: WriteResult): string | undefined {
  return result.ok ? undefined : `${result.detail} (${result.file})`
}

function dialogsAvailable(ctx: any): boolean {
  return typeof ctx?.ui?.dialog?.select === "function"
}

async function selectOption(ctx: any, spec: {
  title: string
  placeholder?: string
  current?: string
  options: Array<{ value: string; title: string; description?: string }>
}): Promise<string | undefined> {
  // The host resolves this promise with the chosen option's `value` and closes
  // the dialog itself, so it must not be awaited while another dialog is open.
  if (dialogOpen) {
    showToast(ctx, "Close the open dialog first", "warning")
    return undefined
  }
  dialogOpen = true
  try {
    // `title` is what this build renders; `name` is the documented spelling. The
    // same list is handed to the matcher, so a host that resolves the rendered
    // label instead of `value` still performs the action the user picked.
    const options = spec.options.map((option) => ({ ...option, name: option.title }))
    const choice = await ctx.ui.dialog.select({
      title: spec.title,
      placeholder: spec.placeholder,
      current: spec.current,
      options,
    })
    // A closed dialog, or a result that names no option in this list, is a
    // cancelled selection: the caller must not act on it.
    return matchOptionValue(choice, options)
  } catch (error: any) {
    showToast(ctx, `Dialog failed: ${String(error?.message ?? error)}`, "error")
    return undefined
  } finally {
    dialogOpen = false
  }
}

const HOST_MODELS_TTL_MS = 60_000
let hostModelsCache: { at: number; models: HostModel[] } | undefined

/**
 * The host catalogue is fetched only when the picker opens; the policy list is
 * enough for the panel itself. A short cache keeps repeated picks instant and
 * still notices a provider edit within a minute.
 */
async function loadHostModels(ctx: any): Promise<HostModel[]> {
  if (hostModelsCache && Date.now() - hostModelsCache.at < HOST_MODELS_TTL_MS) {
    return hostModelsCache.models
  }
  // The location cache is local and does not depend on the server transport;
  // the client call is the fallback for hosts that do not expose it.
  const local = modelsFromLocation(ctx)
  if (local.length > 0) {
    hostModelsCache = { at: Date.now(), models: local }
    return local
  }
  const models = await hostModelsFromClient(ctx?.client)
  if (models.length > 0) hostModelsCache = { at: Date.now(), models }
  return models
}

function Sidebar(props: {
  ctx: any
  sessionID: string
  tick: () => number
  observations: () => Observation[]
  bump: () => void
  chooseRole: (roleName: string) => void
  openPresets: () => void
  collapsed: () => boolean
  toggleCollapsed: () => void
}) {
  const snapshot = createMemo(() => {
    props.tick()
    return readSnapshot(props.ctx, props.sessionID)
  })
  const palette = () => colors(props.ctx?.theme)
  const exhausted = () => snapshot().exhausted
  // The stored preset wins the row, and the user preset wins over the policy one:
  // dispatch applies the overrides, so that is the set actually in force. When a
  // role diverges, `displayPreset` answers `custom`; the stored names are untouched.
  const presetName = () => displayPreset(snapshot())
  const presetColor = () => {
    if (snapshot().customSet) return palette().info
    if (snapshot().activeUserPreset) return palette().info
    if (snapshot().activePreset) return palette().base
    return palette().muted
  }
  // The display abbreviations come from the versioned policy, so the panel and
  // the documentation cannot drift apart.
  const rules = createMemo(() => displayRules(snapshot().policy))
  const latest = createMemo(() => {
    const policy = snapshot().policy
    for (const entry of props.observations()) {
      const role = roleForAgent(policy, entry.agent)
      if (role) return { role, observation: entry }
    }
    return undefined
  })

  // The runtime file is created with policy defaults the first time a session
  // sidebar renders for a project that does not have one yet. Guarded so a
  // failed write is not retried on every 2 s poll.
  const attempted = new Set<string>()
  createEffect(() => {
    const current = snapshot()
    if (!current.ok || current.runtime.exists) return
    if (attempted.has(current.runtimePath)) return
    attempted.add(current.runtimePath)
    const result = ensureRuntime(props.ctx, props.sessionID)
    if (result.ok && result.created) props.bump()
  })

  return (
    <box flexDirection="column" gap={0} paddingTop={1} flexShrink={0}>
      <SidebarHeader
        title="Model Policy"
        collapsed={props.collapsed}
        toggle={props.toggleCollapsed}
        base={palette().base}
        muted={palette().muted}
        suffix={() =>
          props.collapsed() ? ` (${truncate(presetName(), SIDEBAR_PRESET_MAX)})` : undefined
        }
      />
      <Show when={!props.collapsed()}>
        <Show
          when={snapshot().ok}
          fallback={
            <box flexDirection="column" gap={0}>
              <text wrapMode="word" fg={snapshot().status === "no-policy" ? palette().warning : palette().error}>
                {reasonLine(snapshot())}
              </text>
              <text wrapMode="none" fg={palette().muted}>
                {truncate(snapshot().policyPath, 38)}
              </text>
              <text wrapMode="none" fg={palette().muted}>
                /policy
              </text>
            </box>
          }
        >

        <For each={snapshot().roles}>
          {(role: RoleView) => {
            const fg = () => {
              if (role.starved) return palette().warning
              if (role.exhausted) return palette().warning
              if (role.source === "override") return palette().info
              if (role.source === "none") return palette().muted
              return palette().base
            }
            return (
              <box
                flexDirection="row"
                onMouseUp={() => props.chooseRole(role.name)}
              >
                <text wrapMode="none" fg={palette().muted}>
                  <span style={{ fg: palette().muted }}>{shortRole(role.name, rules()).padEnd(4)}</span>
                  <span style={{ fg: fg() }}>{truncate(roleLabel(role, rules()), SIDEBAR_MODEL_MAX)}</span>
                  <span style={{ fg: palette().muted }}>{" ›"}</span>
                </text>
              </box>
            )
          }}
        </For>

        <box flexDirection="row" onMouseUp={() => props.openPresets()}>
          <text wrapMode="none" fg={palette().muted}>
            <span style={{ fg: palette().muted }}>{"preset".padEnd(7)}</span>
            <span style={{ fg: presetColor() }}>{truncate(presetName(), SIDEBAR_MODEL_MAX - 3)}</span>
            <span style={{ fg: palette().muted }}>{" ›"}</span>
          </text>
        </box>

        <Show when={latest()}>
          {(seen: () => { role: string; observation: Observation }) => (
            <text wrapMode="none" fg={palette().muted}>
              seen {seen().role}: {truncate(observationLabel(seen().observation), 20)}{" "}
              {formatClock(seen().observation.at)}
            </text>
          )}
        </Show>

        <Show when={exhausted().length > 0}>
          <For each={exhausted().slice(0, SIDEBAR_EXHAUSTED_LIMIT)}>
            {(entry: ExhaustedModel) => (
              <text wrapMode="none" fg={palette().warning}>
                <span style={{ fg: palette().warning }}>! </span>
                <span style={{ fg: palette().muted }}>{truncate(shortModel(entry.model, rules()), SIDEBAR_MODEL_MAX)}</span>
                <span style={{ fg: palette().warning }}> {formatRemaining(entry.remainingMs)}</span>
              </text>
            )}
          </For>
          <Show when={exhausted().length > SIDEBAR_EXHAUSTED_LIMIT}>
            <text wrapMode="none" fg={palette().muted}>
              +{exhausted().length - SIDEBAR_EXHAUSTED_LIMIT} more exhausted
            </text>
          </Show>
        </Show>

        <text wrapMode="none" fg={palette().muted}>
          click a role to change · /policy
        </text>
        </Show>
      </Show>
    </box>
  )
}

// Small helper so the details dialog can say which model a subagent actually
// ran on, when the event stream reported it.
function roleObservationLine(snapshot: Snapshot, role: string, observations: readonly Observation[]): string | undefined {
  const seen = latestObservationForRole(observations, snapshot.policy, role)
  if (!seen) return undefined
  return `seen ${observationLabel(seen)} at ${formatClock(seen.at)}`
}

export default Plugin.define({
  id: "sidebar-suite.policy",
  setup(ctx: any) {
    const polling = createPolling(POLL_MS)
    const tick = polling.tick
    const bump = polling.bump
    const [observations, setObservations] = createSignal<Observation[]>([])
    const [collapsed, setCollapsed] = createSignal(readCollapsed(ctx))
    const toggleCollapsed = () => {
      const next = !collapsed()
      setCollapsed(next)
      writeCollapsed(ctx, next)
      bump()
    }

    const disposers: Array<() => void> = []
    const read = (sessionID?: string) => readSnapshot(ctx, sessionID ?? currentSessionID(ctx))

    function observeFromEvent(payload: any) {
      const details = payload?.details ?? payload?.event ?? payload
      const observation = observeEvent(details)
      if (observation) setObservations((current) => recordObservation(current, observation))
    }

    // ctx.data.listen is the documented event channel; ctx.event.on is the v1
    // surface. Both are optional: observation is a nice-to-have, never required
    // for the panel to work.
    try {
      const data = ctx?.data
      if (typeof data?.listen === "function") {
        const dispose = data.listen((payload: unknown) => observeFromEvent(payload))
        if (typeof dispose === "function") disposers.push(dispose)
      } else if (typeof ctx?.event?.on === "function") {
        for (const type of ["message.updated", "message.part.updated"]) {
          const dispose = ctx.event.on(type, observeFromEvent)
          if (typeof dispose === "function") disposers.push(dispose)
        }
      }
    } catch {
      // Observation is best-effort.
    }

    async function choosePreset(sessionID?: string) {
      const snapshot = read(sessionID)
      if (!snapshot.ok || !snapshot.policy) {
        showToast(ctx, `Cannot read the policy: ${reasonLine(snapshot)}`, "warning")
        return
      }
      if (snapshot.presets.length === 0) {
        showToast(ctx, "The policy declares no presets", "warning")
        return
      }

      // One list for both kinds: policy presets first, then the saved ones, then
      // the actions. Values are prefix-sliced, so a name containing a colon works.
      const options: Array<{ value: string; title: string; description?: string }> = []
      for (const name of snapshot.presets) {
        const active = !snapshot.customSet && !snapshot.activeUserPreset && snapshot.activePreset === name
        options.push({
          value: `policy:${name}`,
          title: `policy · ${name}${active ? " (active)" : ""}`,
          description: describePreset(snapshot, name),
        })
      }
      for (const name of snapshot.userPresets) {
        const active = !snapshot.customSet && snapshot.activeUserPreset === name
        options.push({
          value: `user:${name}`,
          title: `user · ${name}${active ? " (active)" : ""}`,
          description: describeUserPreset(snapshot, name),
        })
      }
      options.push({
        value: "action:save",
        title: "Save current as a new preset…",
        description: "store the model each role resolves to right now",
      })
      if (snapshot.userPresets.length > 0) {
        options.push({
          value: "action:delete",
          title: "Delete a saved preset…",
          description: `${snapshot.userPresets.length} saved preset(s) to choose from`,
        })
      }

      const current = snapshot.customSet
        ? undefined
        : snapshot.activeUserPreset
          ? `user:${snapshot.activeUserPreset}`
          : snapshot.activePreset
            ? `policy:${snapshot.activePreset}`
            : undefined

      const choice = await selectOption(ctx, {
        title: "Presets",
        placeholder: snapshot.customSet
          ? "Current set is custom (does not match a preset)"
          : "Policy presets and your saved presets",
        current,
        options,
      })
      if (choice === undefined) return

      if (choice === "action:save") return savePresetFlow(sessionID)
      if (choice === "action:delete") return deletePresetFlow(sessionID)

      if (choice.startsWith("user:")) {
        const name = choice.slice("user:".length)
        if (name === snapshot.activeUserPreset) return
        const result = applyUserPreset(ctx, sessionID ?? currentSessionID(ctx), name)
        if (!result.ok) {
          showToast(ctx, `Preset not applied: ${result.detail}`, "error")
          return
        }
        bump()
        const notes: string[] = [`${Object.keys(result.applied).length} role(s)`]
        if (result.skipped.length > 0) {
          notes.push(
            `skipped ${result.skipped.map((entry) => `${entry.role} (${entry.reason})`).join(", ")}`,
          )
        }
        if (result.strippedVariants.length > 0) {
          notes.push(`variant dropped for ${result.strippedVariants.join(", ")}`)
        }
        showToast(ctx, `Active preset: ${result.name} — ${notes.join(" · ")}`, "success")
        return
      }

      if (choice.startsWith("policy:")) {
        const name = choice.slice("policy:".length)
        // A policy preset already stored, with no user preset on top of it, is a no-op.
        if (name === snapshot.activePreset && !snapshot.activeUserPreset) return
        const result = setActivePreset(ctx, sessionID ?? currentSessionID(ctx), name)
        const error = failure(result)
        if (error) {
          showToast(ctx, `Preset not written: ${error}`, "error")
          return
        }
        bump()
        showToast(ctx, `Active preset: ${name}`, "success")
      }
    }

    async function savePresetFlow(sessionID?: string) {
      const snapshot = read(sessionID)
      if (!snapshot.ok || !snapshot.policy) {
        showToast(ctx, `Cannot read the policy: ${reasonLine(snapshot)}`, "warning")
        return
      }
      // A role that resolves to nothing has no override to store, so it is left
      // out of the saved map and only reported.
      const captured = captureRoles(snapshot)
      if (Object.keys(captured.roles).length === 0) {
        showToast(ctx, "No role resolves to a model — nothing to save", "warning")
        return
      }

      const typed = await withDialog(ctx, "Prompt", () =>
        ctx.ui.dialog.prompt({
          title: "Save the current stage set",
          description: "printable characters, up to 32 — control characters and non-space whitespace are not allowed",
          placeholder: "cheap planning",
          value: "",
        }),
      )
      if (typeof typed !== "string") return

      const checked = checkPresetName(snapshot, typed)
      if (!checked.ok) {
        showToast(ctx, `Name rejected: ${checked.detail}`, "warning")
        return
      }

      if (checked.replaces !== undefined) {
        const confirmed = await withDialog(ctx, "Confirm", () =>
          ctx.ui.dialog.confirm({
            title: "Replace saved preset",
            message: `"${checked.replaces}" already exists. Replace it with the current set?`,
            label: { confirm: "Replace", cancel: "Cancel" },
          }),
        )
        if (confirmed !== true) return
      }

      const result = saveUserPreset(ctx, sessionID ?? currentSessionID(ctx), checked.name, captured.roles)
      if (!result.ok) {
        showToast(ctx, `Preset not saved: ${result.detail}`, "error")
        return
      }
      bump()
      const omitted = result.dropped.length + captured.unresolved.length
      const suffix = omitted > 0 ? `, ${omitted} role(s) not saved (no model or not declared)` : ""
      // The same write selected the set, so the toast says so: a plain "saved"
      // would leave the user checking the header to learn whether it took effect.
      showToast(
        ctx,
        `Saved preset "${result.name}" and made it active (${result.saved} role(s)${suffix})`,
        "success",
      )
    }

    async function deletePresetFlow(sessionID?: string) {
      const snapshot = read(sessionID)
      if (!snapshot.ok) {
        showToast(ctx, `Cannot read the runtime file: ${reasonLine(snapshot)}`, "warning")
        return
      }
      if (snapshot.userPresets.length === 0) {
        showToast(ctx, "No saved presets to delete", "warning")
        return
      }

      const choice = await selectOption(ctx, {
        title: "Delete a saved preset",
        placeholder: "The role overrides it applied stay in place",
        current: snapshot.activeUserPreset,
        options: snapshot.userPresets.map((name) => ({
          value: name,
          title: `${name}${snapshot.activeUserPreset === name ? " (active)" : ""}`,
          description: describeUserPreset(snapshot, name),
        })),
      })
      if (choice === undefined) return

      const active = choice === snapshot.activeUserPreset
      const confirmed = await withDialog(ctx, "Confirm", () =>
        ctx.ui.dialog.confirm({
          title: "Delete saved preset",
          message: active
            ? `Delete "${choice}"? The models it applied stay in place; the active-preset marker is cleared.`
            : `Delete "${choice}"? The current role overrides are not affected.`,
          label: { confirm: "Delete", cancel: "Cancel" },
        }),
      )
      if (confirmed !== true) return

      const result = deleteUserPreset(ctx, sessionID ?? currentSessionID(ctx), choice)
      if (!result.ok) {
        showToast(ctx, `Preset not deleted: ${result.detail}`, "error")
        return
      }
      bump()
      showToast(ctx, `Deleted preset "${result.removed}" — role overrides kept`, "success")
    }

    async function chooseRole(roleName: string, sessionID?: string) {
      const snapshot = read(sessionID)
      if (!snapshot.ok || !snapshot.policy) {
        showToast(ctx, `Cannot read the policy: ${reasonLine(snapshot)}`, "warning")
        return
      }
      const role = snapshot.roles.find((entry) => entry.name === roleName)
      const current = snapshot.runtime.roleOverrides[roleName] ?? ""
      const chain = snapshot.policy.presets[snapshot.effectivePreset ?? ""]?.[roleName] ?? []
      const host = await loadHostModels(ctx)
      if (host.length === 0) {
        showToast(ctx, "OpenCode model list unavailable; showing the policy catalogue", "warning")
      }
      const choices = mergeModelChoices(roleModels(snapshot, roleName), host)

      const options = [
        {
          value: "",
          title: "(use the preset chain)",
          description: chain.length > 0 ? chain.join(" → ") : "no chain for this role in the effective preset",
        },
        ...choices.map((choice) => {
          const detail = choice.inPolicy
            ? [choice.name, describeModel(snapshot, choice.id)].filter(Boolean).join(" · ")
            : [choice.name, "not in the policy catalogue"].filter(Boolean).join(" · ")
          return {
            value: choice.id,
            // Full id in the title: the picker is a deliberate choice, not the
            // compact sidebar. The description carries the human name and the
            // policy detail (or the outside-policy marker).
            title: choice.inPolicy ? modelLabelFull(snapshot, choice.id, roleName) : choice.id,
            description: detail,
          }
        }),
        { value: CUSTOM_MODEL, title: "Custom model id…", description: "type the full provider/model id" },
      ]

      const choice = await selectOption(ctx, {
        title: `Model for ${roleName}`,
        placeholder: "Override the preset chain for this role",
        current,
        options,
      })
      if (choice === undefined) return

      let model: string | undefined = choice === "" ? undefined : choice
      if (choice === CUSTOM_MODEL) {
        const typed = await withDialog(ctx, "Prompt", () =>
          ctx.ui.dialog.prompt({
            title: `Model for ${roleName}`,
            description: "full provider/model; a short policy id like rf/cl-sonnet-5 is expanded",
            placeholder: "zai-coding-plan/glm-5.3",
            value: current || role?.model || "",
          }),
        )
        if (typeof typed !== "string") return
        // A short display id is expanded back to the full id, so a value copied
        // from the list is never written as an unknown model.
        model = expandModelId(snapshot, typed.trim())
        if (!model) {
          showToast(ctx, "Empty model id — nothing written", "warning")
          return
        }
      }

      const policyModel = model ? (model.includes("#") ? model.slice(0, model.indexOf("#")) : model) : undefined
      if (policyModel && !snapshot.policy.models?.[policyModel]) {
        const proceed = await withDialog(ctx, "Confirm", () =>
          ctx.ui.dialog.confirm({
            title: "Model outside the policy",
            message: `${policyModel} is not in docs/agent-model-policy.json. The dispatch policy check may reject this override. Write it anyway?`,
          }),
        )
        if (proceed !== true) return
      }

      const result = setRoleOverride(ctx, sessionID ?? currentSessionID(ctx), roleName, model)
      const error = failure(result)
      if (error) {
        showToast(ctx, `Override not written: ${error}`, "error")
        return
      }
      bump()
      showToast(ctx, model ? `${roleName}: ${model}` : `${roleName}: preset chain`, "success")
    }

    async function showDetails(sessionID?: string) {
      const snapshot = read(sessionID)
      const lines: string[] = []
      if (snapshot.customSet) {
        const based = snapshot.activeUserPreset ?? snapshot.activePreset
        lines.push(`preset: custom${based ? ` (based on ${based})` : ""}`)
      } else if (snapshot.activePreset) {
        lines.push(`preset: ${snapshot.activePreset} (${snapshot.presetSource})`)
      }
      if (snapshot.activeUserPreset && !snapshot.customSet) {
        lines.push(`user preset: ${snapshot.activeUserPreset} (active)`)
      }
      if (snapshot.userPresets.length > 0) lines.push(`user presets: ${snapshot.userPresets.join(", ")}`)
      if (snapshot.effectivePreset && snapshot.effectivePreset !== snapshot.activePreset) {
        lines.push(`effective preset: ${snapshot.effectivePreset} (policy override window)`)
      }
      lines.push(`policy: ${snapshot.policyPath}`, `runtime: ${snapshot.runtimePath}`)

      if (snapshot.roles.length > 0) {
        lines.push("", "roles:")
        for (const role of snapshot.roles) {
          const detail = describeRole(snapshot, role)
          lines.push(`  ${role.name.padEnd(11)} ${roleLabelFull(role)}${detail ? `  · ${detail}` : ""}`)
          const seen = roleObservationLine(snapshot, role.name, observations())
          if (seen) lines.push(`  ${" ".repeat(11)} ${seen}`)
        }
      }

      if (snapshot.exhausted.length > 0) {
        lines.push("", "exhausted now:")
        for (const entry of snapshot.exhausted) {
          lines.push(`  ${entry.model} — ${formatRemaining(entry.remainingMs)} (until ${entry.until})`)
        }
      }

      lines.push("", "legend:")
      for (const entry of legendFor(displayRules(snapshot.policy))) lines.push(`  ${entry}`)

      lines.push("", "session log (newest first):")
      if (snapshot.log.length === 0) lines.push("  empty")
      else for (const entry of snapshot.log.slice(0, DETAILS_LOG_LIMIT)) lines.push(`  ${logLine(entry, displayRules(snapshot.policy))}`)
      if (snapshot.log.length > DETAILS_LOG_LIMIT) {
        lines.push(`  … ${snapshot.log.length - DETAILS_LOG_LIMIT} older entries`)
      }

      await withDialog(ctx, "Details", () =>
        ctx.ui.dialog.alert({ title: "Model policy", message: lines.join("\n") }),
      )
    }

    async function openPanel(sessionID?: string) {
      if (!dialogsAvailable(ctx)) {
        showToast(ctx, "This OpenCode build has no dialog API", "error")
        return
      }

      const snapshot = read(sessionID)
      if (!snapshot.ok) {
        const actions: Array<{ value: string; title: string; description?: string }> = [
          { value: "details", title: "Show the resolved paths", description: snapshot.runtimePath },
        ]
        if (snapshot.status === "runtime-malformed" || snapshot.status === "runtime-unreadable") {
          actions.push({
            value: "recreate",
            title: "Recreate the runtime file with defaults",
            description: "discards orchestrator fields (exhaustedUntil, sessionLog) and every saved preset",
          })
        }
        const choice = await selectOption(ctx, {
          title: "Model policy",
          placeholder: reasonLine(snapshot),
          options: actions,
        })
        if (choice === "details") return showDetails(sessionID)
        if (choice === "recreate") {
          const confirmed = await withDialog(ctx, "Confirm", () =>
            ctx.ui.dialog.confirm({
              title: "Recreate runtime file",
              message: `${reasonLine(snapshot)}\n\nReplace it with policy defaults? exhaustedUntil, sessionLog and every saved user preset are lost.`,
              label: { confirm: "Recreate", cancel: "Cancel" },
            }),
          )
          if (confirmed !== true) return
          const result = recreateRuntime(ctx, sessionID ?? currentSessionID(ctx))
          const error = failure(result)
          if (error) {
            showToast(ctx, `Not recreated: ${error}`, "error")
            return
          }
          bump()
          showToast(ctx, "Runtime file recreated", "success")
        }
        return
      }

      const options: Array<{ value: string; title: string; description?: string }> = [
        {
          value: "preset",
          title: `Presets: ${displayPreset(snapshot)}`,
          description:
            snapshot.userPresets.length > 0
              ? `switch the preset every role resolves from · ${snapshot.userPresets.length} saved preset(s)`
              : "switch the preset every role resolves from · save the current set",
        },
      ]

      for (const role of snapshot.roles) {
        options.push({
          value: `role:${role.name}`,
          title: `Role ${role.name}: ${modelLabelFull(snapshot, role.model ?? "-", role.name)}`,
          description: `${role.model ?? "-"} · ${describeRole(snapshot, role)}`,
        })
      }
      for (const [role, model] of Object.entries(snapshot.runtime.roleOverrides)) {
        if (snapshot.roles.some((entry) => entry.name === role)) continue
        options.push({
          value: `role:${role}`,
          title: `Role ${role}: ${model} (override)`,
          description: "role is not declared in the policy",
        })
      }

      if (Object.keys(snapshot.runtime.roleOverrides).length > 0) {
        options.push({
          value: "clear",
          title: "Clear all role overrides",
          description: `${Object.keys(snapshot.runtime.roleOverrides).length} override(s) return to the preset chain`,
        })
      }
      options.push({
        value: "details",
        title: "Show details and session log",
        description: "preset, resolved models, exhaustion, log, observed models",
      })
      options.push({
        value: "refresh",
        title: "Re-read both files",
        description: snapshot.runtimePath,
      })

      const choice = await selectOption(ctx, {
        title: "Model policy",
        placeholder: "Select an action",
        options,
      })
      if (choice === undefined) return

      if (choice === "preset") return choosePreset(sessionID)
      if (choice === "details") return showDetails(sessionID)
      if (choice === "refresh") {
        bump()
        showToast(ctx, "Policy and runtime files re-read")
        return
      }
      if (choice === "clear") {
        const confirmed = await withDialog(ctx, "Confirm", () =>
          ctx.ui.dialog.confirm({
            title: "Clear role overrides",
            message: "Every role resolves from the preset chain again.",
            label: { confirm: "Clear", cancel: "Cancel" },
          }),
        )
        if (confirmed !== true) return
        const result = clearRoleOverrides(ctx, sessionID ?? currentSessionID(ctx))
        const error = failure(result)
        if (error) {
          showToast(ctx, `Overrides not cleared: ${error}`, "error")
          return
        }
        bump()
        showToast(ctx, "Role overrides cleared", "success")
        return
      }
      if (choice.startsWith("role:")) return chooseRole(choice.slice("role:".length), sessionID)
    }

    if (ctx?.keymap?.layer) {
      try {
        ctx.keymap.layer(() => ({
          mode: "global",
          commands: [
            {
              id: "sidebar-suite.policy.open",
              title: "Model policy control panel",
              group: "Model policy",
              palette: true,
              slash: { name: "policy" },
              run: () =>
                openPanel().catch((error: any) =>
                  showToast(ctx, `Model policy panel failed: ${String(error?.message ?? error)}`, "error"),
                ),
            },
            {
              id: "sidebar-suite.policy.preset",
              title: "Switch the subagent model-policy preset",
              group: "Model policy",
              palette: true,
              slash: { name: "preset" },
              run: () =>
                choosePreset().catch((error: any) =>
                  showToast(ctx, `Preset switch failed: ${String(error?.message ?? error)}`, "error"),
                ),
            },
          ],
        }))
      } catch {
        // Command registration is optional; the sidebar still renders.
      }
    }

    if (ctx?.ui?.slot) {
      try {
        disposers.push(
          ctx.ui.slot({
            append: "sidebar.content",
            render: ({ sessionID }: { sessionID: string }) => (
              <Sidebar
                ctx={ctx}
                sessionID={sessionID}
                tick={tick}
                observations={observations}
                bump={bump}
                chooseRole={(roleName: string) => chooseRole(roleName, sessionID)}
                openPresets={() => choosePreset(sessionID)}
                collapsed={collapsed}
                toggleCollapsed={toggleCollapsed}
              />
            ),
          }),
        )
      } catch {
        // Never fail plugin setup because a slot changed.
      }
    }

    const cleanup = () => {
      polling.dispose()
      for (const dispose of disposers) {
        try {
          if (typeof dispose === "function") dispose()
        } catch {
          // Unloading must never throw.
        }
      }
    }

    try {
      ctx?.lifecycle?.onDispose?.(cleanup)
    } catch {
      // Older hosts return the cleanup function from setup instead.
    }

    return cleanup
  },
})
