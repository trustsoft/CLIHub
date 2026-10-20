# Subagent Model Policy

How the pipeline orchestrator chooses the model for a child task (`subagent` dispatch).
Applies to every change run in this repository.

## Available models (Z.AI Coding Plan)

- `zai-coding-plan/glm-5.3` — the main model; quality-critical work.
- `zai-coding-plan/glm-5.3-flash` — the light variant; faster and cheaper on plan quota.
- Variants (reasoning effort): pass as `providerID/modelID#variant`, e.g. `zai-coding-plan/glm-5.3-flash#low`.

## Mapping

| Task kind | Agent | Model | Why |
|---|---|---|---|
| OpenSpec plan (proposal/design/tasks) | `pipe/planner` | `glm-5.3` | design decisions, spec deltas, scope judgement |
| Implementation | `pipe/implementer` | `glm-5.3` | code changes, deviation protocol, gate discipline |
| Independent verification | `pipe/verifier` | `glm-5.3` | trust anchor of the pipeline — never weaken this |
| Codebase exploration / lookup | `explore` | `glm-5.3-flash` | search + read + report |
| Research / summarisation | `general` | `glm-5.3-flash` | reading and synthesis |
| Mechanical checks (gate runs, status dumps) | `explore`/`general` | `glm-5.3-flash#low` | minimal reasoning |

## Rules

- The plan/implement/verify stages always run on `glm-5.3`; flash is only for the auxiliary stages.
- Escalation: if a flash-agent report shows misunderstanding or hallucination, rerun the same task
  on `glm-5.3` and record the escalation in the pipeline state note (`docs/backlog.md` if it is a
  recurring pattern).
- Cost is not the only axis: flash models usually have their own plan quota and higher rate limits,
  so using them for simple tasks preserves `glm-5.3` headroom for the stages that need it.
- If a future change needs a model outside the Z.AI plan, add it here with the reason and the
  provider it comes from.
