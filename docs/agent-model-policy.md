# Subagent Model Policy

The policy has two layers, both JSON:

1. **Policy (this repo, versioned):** [`agent-model-policy.json`](agent-model-policy.json) — roles,
   model catalog, presets, and time-windowed overrides. Human-curated; changes rarely.
2. **Runtime selection (gitignored):** `.opencode/model-policy.json` — the *effective* choice right
   now. Written by the orchestrator (observations) and the sidebar (user intent); changes often.

## Roles

| Role | Subagents | Tier | Effort | On exhaustion |
|---|---|---|---|---|
| planner | `pipe/planner` | high | high | **block & ask** |
| implementer | `pipe/implementer` | high | medium | fall through the chain |
| verifier | `pipe/verifier` | high | high | **block & ask** |
| explorer | `explore` | any | low | fall through the chain |
| research | `general` | medium | medium | fall through the chain |
| mechanical | *(orchestrator-only)* | any | low | fall through the chain |

## Reasoning effort

Each role declares a normalized `effort` (`off|low|medium|high|max`). The resolver maps it onto
the chosen model's supported `variants` (the lowest variant at or above the level, or the highest
available when the model has none that high), so a `medium` role on a model without `medium` does
not silently drop to `low`. A chain entry may pin a specific variant inline as `model#variant`, and a
`roleOverrides` value may too; both are validated against the model's `variants` in the catalog.

Models differ widely: `zai-coding-plan/glm-5.3` offers `low/high/max` (no `medium`), `relayfast/
claude-haiku-4-5` offers only `high/max`, `relayfast/deepseek-v4-pro` has no `max`, and the free
`longcat-2.5-preview-free`, `mimo-v2.6-flash-free`, and `big-pickle` accept no variant at all. The
resolver omits the `#variant` suffix when a model cannot take one.

**Cost note:** a higher effort consumes more tokens, so on a metered plan it burns the quota faster.
The exhaustion logic does not yet weight usage by effort; treat `max` on a plan model as a
quota-expensive choice.

`research` serves the `general` subagent for investigations and summaries (medium tier). `mechanical`
is an orchestrator-only role — no subagent maps to it by name — used for gate runs, status dumps, and
formatting, where any model is fine. The orchestrator picks `research` vs `mechanical` per task when it
resolves.

`block & ask` means the pipeline **stops and asks the user** instead of silently running a planner or
verifier on a weaker model — a nominal verification is worse than no verification.

## Presets

`default`, `plan_exhausted`, `free_only`, `flash_only` — each maps every role to an ordered fallback
chain. Switch with `active_preset` (sidebar or by editing the runtime file), or schedule a switch with
an `overrides` window in the policy JSON (`valid_from` / `valid_until`).

## Billing modes

| Mode | Providers | Behaviour |
|---|---|---|
| `plan` | `zai-coding-plan` | subscription; shared 5-hour + weekly limits — can be exhausted mid-session |
| `paid` | `relay.fast` (`relayfast`), `deepseek` | pay-per-token; no subscription limit |
| `free` | models whose name contains `free`/`Free`, plus `opencode/big-pickle` | quota-limited (typically daily) |

## Runtime file (`.opencode/model-policy.json`)

```json
{
  "schemaVersion": 1,
  "activePreset": "default",
  "roleOverrides": { "verifier": "relayfast/claude-sonnet-5" },
  "exhaustedUntil": { "zai-coding-plan/glm-5.3": "2026-10-21T14:00:00Z" },
  "sessionLog": [
    { "at": "2026-10-21T10:00:00Z", "role": "verifier", "from": "zai-coding-plan/glm-5.3",
      "to": "relayfast/claude-sonnet-5", "preset": "default", "reason": "429 quota" }
  ]
}
```

Ownership: the orchestrator writes `exhaustedUntil` and `sessionLog`; the sidebar writes
`activePreset` and `roleOverrides`. Each writer read-modify-writes only its own fields, atomically.

## How the orchestrator resolves a dispatch

1. `effective_preset` = first matching `overrides` window in the policy, else the runtime
   `activePreset`, else the policy `active_preset`.
2. Take the role's ordered list; drop models whose runtime `exhaustedUntil` is still in the future.
3. Try in order. On an **availability** failure (429 / quota / billing / auth / not-found) mark the
   model's `exhaustedUntil` = now + backoff and try the next. On a **quality** failure (the agent
   misunderstood) escalate to the next model in the same list.
4. If a `block_and_ask` role runs out of acceptable models → stop and ask the user.
5. Map the role's `effort` level onto the chosen model's `variants` (see
   [Reasoning effort](#reasoning-effort)) and emit `provider/model#variant`.

## Why two layers

The policy JSON is a *contract* (what should be preferred, and what is safe to fall back to). The
runtime JSON is *state* (what is actually available right now). Keeping state out of git avoids
committing transient quota exhaustion, and lets the sidebar and orchestrator coordinate without a
shared process. Both are JSON so the sidebar plugin can read them directly with `node:fs` — no YAML
parser is needed.

## Related

- Sidebar panel: preset switcher + per-role dropdown + exhausted-until indicator (the `model-policy`
  TUI plugin in the OpenCode config).
- Dispatch gate: `.opencode/scripts/resolve-model.mjs <role> --record` — the orchestrator runs this
  before every subagent dispatch and uses its stdout as the model; it appends to `session_log`.
- Policy gate: `.opencode/scripts/check-model-policy.mjs` — validates the policy and runtime layers
  (roles, presets, chains, overrides, session log). Part of the verification commands in `AGENTS.md`.
- Resolver tests: `node .opencode/scripts/resolve-model.test.mjs`.
- `docs/backlog.md` — escalation notes when a flash model is rejected.
