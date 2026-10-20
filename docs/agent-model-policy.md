# Subagent Model Policy

The policy has two layers:

1. **Policy (this repo, versioned):** [`agent-model-policy.yaml`](agent-model-policy.yaml) — roles,
   model catalog, presets, and time-windowed overrides. Human-curated; changes rarely.
2. **Runtime selection (gitignored):** `.opencode/model-policy.json` — the *effective* choice right
   now. Written by the orchestrator (observations) and the sidebar (user intent); changes often.

## Roles

| Role | Subagents | Tier | On exhaustion |
|---|---|---|---|
| planner | `pipe/planner` | high | **block & ask** |
| implementer | `pipe/implementer` | high | fall through the chain |
| verifier | `pipe/verifier` | high | **block & ask** |
| explorer | `explore` | any | fall through the chain |
| mechanical | `general` | any | fall through the chain |

`block & ask` means the pipeline **stops and asks the user** instead of silently running a planner or
verifier on a weaker model — a nominal verification is worse than no verification.

## Presets

`default`, `plan_exhausted`, `free_only`, `flash_only` — each maps every role to an ordered fallback
chain. Switch with `active_preset` (sidebar or by editing the runtime file), or schedule a switch with
an `overrides` window in the YAML (`valid_from` / `valid_until`).

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
  "roleOverrides": { "verifier": "opencode/claude-sonnet-5-5" },
  "exhaustedUntil": { "zai-coding-plan/glm-5.3": "2026-10-21T14:00:00Z" },
  "sessionLog": [
    { "at": "2026-10-21T10:00:00Z", "role": "verifier", "from": "zai-coding-plan/glm-5.3",
      "to": "deepseek/deepseek-v4-pro", "reason": "429 quota" }
  ]
}
```

Ownership: the orchestrator writes `exhausted_until` and `session_log`; the sidebar writes
`active_preset` and `role_overrides`. Each writer read-modify-writes only its own fields.

## How the orchestrator resolves a dispatch

1. `effective_preset` = first matching `overrides` window, else `active_preset`.
2. Take the role's ordered list; drop models whose `exhausted_until` is still in the future.
3. Try in order. On an **availability** failure (429 / quota / billing / auth / not-found) mark the
   model `exhausted_until = now + backoff` and try the next. On a **quality** failure (the agent
   misunderstood) escalate to the next model in the same list.
4. If a `block_and_ask` role runs out of acceptable models → stop and ask the user.

## Why two layers

The YAML is a *contract* (what should be preferred, and what is safe to fall back to). The runtime
JSON is *state* (what is actually available right now). Keeping state out of git avoids committing
transient quota exhaustion, and lets the sidebar and orchestrator coordinate without a shared
process.

## Related

- Sidebar panel (planned): preset switcher + per-role dropdown + exhausted-until indicator.
- `docs/backlog.md` — escalation notes when a flash model is rejected.
