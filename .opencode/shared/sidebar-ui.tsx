// Shared sidebar helpers for the OpenCode TUI plugins under ../plugins.
// Pure presentation helpers plus collapse/poll/toast plumbing. No plugin state
// lives here, and nothing in this directory is loaded as a plugin: tui.json
// registers plugin entry files explicitly.

import { Show, createSignal } from "solid-js"

const SIDEBAR_FALLBACK = {
  base: "#e6e6e6",
  muted: "#8a8a8a",
  success: "#5fd75f",
  error: "#ff6b6b",
  warning: "#e6c15a",
  info: "#5ac8fa",
}

function pick(theme: any, keys: string[], fallback: any): any {
  let value = theme
  for (const key of keys) {
    if (!value || typeof value !== "object") return fallback
    value = value[key]
  }
  return value ?? fallback
}

function isColor(value: any): boolean {
  if (typeof value === "string") return true
  return Boolean(value) && typeof value === "object" && typeof value.r === "number" && typeof value.b === "number"
}

function color(theme: any, paths: string[][], fallback: string): any {
  for (const path of paths) {
    const value = pick(theme, path, undefined)
    if (isColor(value)) return value
  }
  return fallback
}

// ctx.theme is the renderer's current token set (text.base, text.muted,
// text.feedback.success.base, ...). Older shapes nest it under `.current`.
export function sidebarColors(theme: any) {
  const root = theme?.current && typeof theme.current === "object" ? theme.current : theme
  return {
    base: color(root, [["text", "base"], ["text"]], SIDEBAR_FALLBACK.base),
    muted: color(root, [["text", "muted"], ["textMuted"], ["text", "secondary"]], SIDEBAR_FALLBACK.muted),
    success: color(root, [["text", "feedback", "success", "base"], ["success"]], SIDEBAR_FALLBACK.success),
    error: color(root, [["text", "feedback", "error", "base"], ["error"]], SIDEBAR_FALLBACK.error),
    warning: color(root, [["text", "feedback", "warning", "base"], ["warning"]], SIDEBAR_FALLBACK.warning),
    info: color(
      root,
      [["text", "feedback", "info", "base"], ["text", "action", "primary", "focused"]],
      SIDEBAR_FALLBACK.info,
    ),
  }
}

// The collapsed state is remembered per project when the host offers storage;
// otherwise the toggle still works for the session.
export function readCollapsed(ctx: any): boolean {
  try {
    return ctx?.storage?.memory?.("collapsed", false) === true
  } catch {
    return false
  }
}

export function writeCollapsed(ctx: any, value: boolean): void {
  try {
    ctx?.storage?.store?.("collapsed", value)
  } catch {
    // Best-effort persistence.
  }
}

export function currentSessionID(ctx: any): string | undefined {
  try {
    const route = ctx?.ui?.router?.current?.() ?? ctx?.route?.current
    if (route?.type === "session" && typeof route.sessionID === "string") return route.sessionID
    if (route?.name === "session" && typeof route.params?.sessionID === "string") return route.params.sessionID
  } catch {
    // Fall through to the slot-provided session id.
  }
  return undefined
}

export function showToast(ctx: any, message: string, variant: "info" | "success" | "warning" | "error" = "info"): void {
  try {
    ctx?.ui?.toast?.show?.({ message, variant, duration: 4000 })
  } catch {
    // A missing toast surface must never break a write that already succeeded.
  }
}

/**
 * Runs one dialog call and turns a rejection into a toast plus `undefined`, so a
 * dialog that throws can never fail the caller. `label` is the noun the message
 * starts with ("Prompt", "Confirm", "Details").
 */
export async function withDialog<T>(ctx: any, label: string, run: () => Promise<T>): Promise<T | undefined> {
  try {
    return await run()
  } catch (error: any) {
    showToast(ctx, `${label} failed: ${String(error?.message ?? error)}`, "error")
    return undefined
  }
}

/** A 2 s tick signal for the polling plugins plus its cleanup. */
export function createPolling(intervalMs = 2000): { tick: () => number; bump: () => void; dispose: () => void } {
  const [tick, setTick] = createSignal(0)
  const bump = () => setTick((value) => value + 1)
  const timer = setInterval(bump, intervalMs)
  if (typeof (timer as any).unref === "function") (timer as any).unref()
  return { tick, bump, dispose: () => clearInterval(timer) }
}

/** The clickable header row shared by the sidebar blocks. */
export function SidebarHeader(props: {
  title: string
  collapsed: () => boolean
  toggle: () => void
  base: any
  muted: any
  suffix?: () => string | undefined
}) {
  return (
    <box flexDirection="row" gap={1} onMouseUp={() => props.toggle()}>
      <text fg={props.base}>{props.collapsed() ? "▶" : "▼"}</text>
      <text fg={props.base} wrapMode="none">
        <b>{props.title}</b>
        <Show when={props.suffix?.()}>
          <span style={{ fg: props.muted }}>{props.suffix?.()}</span>
        </Show>
      </text>
    </box>
  )
}
