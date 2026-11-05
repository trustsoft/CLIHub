// OpenCode CLI plugin: engineering-pipeline status in the sidebar plus a
// drill-down panel opened with /pipe (palette + slash command).
//
// The state file is written by the orchestrating agent, never by this plugin.
// Rendering is defensive: a missing/unreadable/malformed/empty state file
// degrades to one muted sidebar row and never throws.

import { Plugin } from "@opencode/plugin/tui"
import { For, Show, createMemo, createSignal } from "solid-js"
import { createPolling, readCollapsed, sidebarColors as colors, writeCollapsed, SidebarHeader } from "../../shared/sidebar-ui"
import {
  STAGE_LETTERS,
  formatTimestamp,
  headerCounts,
  readSnapshot,
  sidebarChanges,
  stageRows,
  truncate,
  type Change,
  type Snapshot,
  type Status,
} from "./state"

const PANEL_NAME = "sidebar-suite.pipeline"
const POLL_MS = 2000

const ICONS: Record<Status, string> = {
  done: "✓",
  active: "●",
  pending: "○",
  blocked: "!",
}

function statusColor(palette: ReturnType<typeof colors>, status: Status): any {
  if (status === "done") return palette.success
  if (status === "active") return palette.info
  if (status === "blocked") return palette.error
  return palette.muted
}

function reasonLine(snapshot: Snapshot): string {
  if (snapshot.reason === "missing") return "state file is missing"
  if (snapshot.reason === "empty") return "state file has no changes"
  if (snapshot.reason === "malformed") return "state file is not valid JSON"
  if (snapshot.reason === "invalid") return "state file does not match the schema"
  if (snapshot.reason === "unreadable") return "state file is unreadable"
  return "state is unavailable"
}

function Sidebar(props: {
  ctx: any
  sessionID: string
  tick: () => number
  collapsed: () => boolean
  toggleCollapsed: () => void
}) {
  const snapshot = createMemo(() => {
    props.tick()
    return readSnapshot(props.ctx, props.sessionID)
  })
  const palette = () => colors(props.ctx?.theme)
  // The current row: the first active or blocked change in state-file order.
  const currentID = createMemo(
    () => snapshot().changes.find((change) => change.status === "active" || change.status === "blocked")?.id,
  )
  // Display-only window: the panel below still renders every change.
  const visible = createMemo(() => sidebarChanges(snapshot().changes))
  const hidden = () => snapshot().changes.length - visible().length
  // Header describes the whole snapshot, not the windowed `visible()` rows.
  const headerText = createMemo(() => headerCounts(snapshot().changes).join(" · "))

  return (
    <box flexDirection="column" gap={0} paddingTop={1} flexShrink={0}>
      <SidebarHeader
        title="Pipeline"
        collapsed={props.collapsed}
        toggle={props.toggleCollapsed}
        base={palette().base}
        muted={palette().muted}
        suffix={() => (snapshot().ok && headerText().length > 0 ? ` · ${headerText()}` : undefined)}
      />
      <Show when={!props.collapsed()}>
        <Show
          when={snapshot().ok}
          fallback={
            <text wrapMode="none" fg={palette().muted}>
              pipeline: no state
            </text>
          }
        >
          <For each={visible()}>
            {(change: Change) => {
              const current = () => change.id === currentID()
              const fg = () => (current() ? statusColor(palette(), change.status) : palette().muted)
              return (
                <box flexDirection="row">
                  <text wrapMode="none" flexShrink={0} fg={fg()}>
                    <span style={{ fg: statusColor(palette(), change.status) }}>{ICONS[change.status]}</span>
                    <span style={{ fg: fg() }}>{" "}{STAGE_LETTERS[change.stage]}{" ·"}</span>
                  </text>
                  <text wrapMode="none" truncate flexShrink={1} fg={fg()}>
                    <span style={{ fg: fg() }}>{" "}{change.id}</span>
                  </text>
                </box>
              )
            }}
          </For>
          <Show when={hidden() > 0}>
            <text wrapMode="none" fg={palette().muted}>
              +{hidden()} more · /pipe
            </text>
          </Show>
        </Show>
      </Show>
    </box>
  )
}

function PipelinePanelBody(props: { ctx: any; panel: any; tick: () => number }) {
  const snapshot = createMemo(() => {
    props.tick()
    return readSnapshot(props.ctx, props.panel.sessionID)
  })
  const palette = () => colors(props.ctx?.theme)
  const noteMax = () => Math.max(24, Number(props.panel.width ?? 100) - 44)

  props.ctx?.keymap?.layer?.(() => ({
    commands: [
      {
        id: "sidebar-suite.pipeline.fullscreen",
        title: "Toggle pipeline panel fullscreen",
        bind: "f",
        run: () => props.panel.toggleFullscreen?.(),
      },
      {
        id: "sidebar-suite.pipeline.close",
        title: "Close pipeline panel",
        bind: "escape",
        run: () => props.panel.close?.(),
      },
    ],
  }))

  return (
    <box flexDirection="column" paddingLeft={2} paddingRight={2} paddingTop={1} gap={0}>
      <text wrapMode="none" fg={palette().base}>
        <b>Pipeline</b>
        <span style={{ fg: palette().muted }}>
          {" · "}
          {snapshot().project ?? "unknown project"}
        </span>
      </text>

      <Show
        when={snapshot().ok}
        fallback={
          <box flexDirection="column" paddingTop={1} gap={0}>
            <text wrapMode="none" fg={palette().error}>
              {reasonLine(snapshot())}
            </text>
            <text wrapMode="word" fg={palette().muted}>
              expected: {snapshot().path}
            </text>
            <Show when={snapshot().detail}>
              <text wrapMode="word" fg={palette().muted}>
                detail: {snapshot().detail}
              </text>
            </Show>
          </box>
        }
      >
        <text wrapMode="none" fg={palette().muted}>
          {snapshot().changes.length} changes · updated {formatTimestamp(snapshot().updatedAt)}
        </text>
        <text wrapMode="word" fg={palette().muted}>
          {snapshot().path}
        </text>

        <For each={snapshot().changes}>
          {(change: Change) => (
            <box flexDirection="column" paddingTop={1} gap={0}>
              <text wrapMode="none" fg={palette().base}>
                <span style={{ fg: statusColor(palette(), change.status) }}>{ICONS[change.status]}</span>
                {" "}
                <b>{change.id}</b>
                <span style={{ fg: statusColor(palette(), change.status) }}>
                  {"  "}
                  {change.status}
                </span>
                <span style={{ fg: palette().muted }}>
                  {" · "}
                  {change.stage}
                  {change.updatedAt ? ` · ${formatTimestamp(change.updatedAt)}` : ""}
                </span>
              </text>
              <For each={stageRows(change)}>
                {(row) => (
                  <text wrapMode="none" fg={palette().muted}>
                    <span>  </span>
                    <span style={{ fg: statusColor(palette(), row.status) }}>{ICONS[row.status]}</span>
                    <span style={{ fg: row.stage === change.stage ? palette().base : palette().muted }}>
                      {" "}
                      {row.stage.padEnd(7)}
                    </span>
                    <span style={{ fg: palette().muted }}>{formatTimestamp(row.at).padEnd(17)}</span>
                    <span style={{ fg: palette().base }}>{row.commit ? ` ${row.commit}` : ""}</span>
                    <span style={{ fg: palette().muted }}>
                      {row.note ? `  ${truncate(row.note, noteMax())}` : ""}
                    </span>
                  </text>
                )}
              </For>
              <Show when={change.note}>
                <text wrapMode="word" fg={palette().muted}>
                  {"  note: "}
                  {change.note}
                </text>
              </Show>
            </box>
          )}
        </For>
      </Show>

      <box paddingTop={1}>
        <text wrapMode="none" fg={palette().muted}>
          f fullscreen · esc close
        </text>
      </box>
    </box>
  )
}

function PipelinePanel(props: { ctx: any; panel: any; tick: () => number }) {
  return (
    <Show when={props.panel?.name === PANEL_NAME}>
      <PipelinePanelBody ctx={props.ctx} panel={props.panel} tick={props.tick} />
    </Show>
  )
}

export default Plugin.define({
  id: "sidebar-suite.pipeline",
  setup(ctx: any) {
    const polling = createPolling(POLL_MS)
    const [collapsed, setCollapsed] = createSignal(readCollapsed(ctx))
    const toggleCollapsed = () => {
      const next = !collapsed()
      setCollapsed(next)
      writeCollapsed(ctx, next)
    }

    const disposers: Array<() => void> = []

    if (ctx?.keymap?.layer) {
      ctx.keymap.layer(() => ({
        mode: "global",
        commands: [
          {
            id: "sidebar-suite.pipeline.open",
            title: "Open pipeline status",
            group: "Pipeline",
            palette: true,
            slash: { name: "pipe" },
            run: () => {
              if (!ctx?.ui?.panel?.open) {
                ctx?.ui?.toast?.show?.({ message: "Pipeline panel is unavailable", variant: "warning" })
                return
              }
              const opened = ctx.ui.panel.open(PANEL_NAME, { presentation: "fullscreen" })
              if (opened === false) {
                ctx.ui.toast?.show?.({ message: "Open a session first", variant: "warning" })
              }
            },
          },
        ],
      }))
    }

    if (ctx?.ui?.slot) {
      disposers.push(
        ctx.ui.slot({
          append: "sidebar.content",
          render: ({ sessionID }: { sessionID: string }) => (
            <Sidebar
              ctx={ctx}
              sessionID={sessionID}
              tick={polling.tick}
              collapsed={collapsed}
              toggleCollapsed={toggleCollapsed}
            />
          ),
        }),
      )
      disposers.push(
        ctx.ui.slot({
          append: "session.panel",
          render: (panel: any) => <PipelinePanel ctx={ctx} panel={panel} tick={polling.tick} />,
        }),
      )
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
