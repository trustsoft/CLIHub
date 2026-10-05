## Context

The current sequence is implemented by `Program.Main`, `App.OnStartup`, service registration, and `App.OnExit`. The existing `app-lifecycle` specification already defines observable behavior such as single-instance activation, dependency injection, data directory initialization, startup window visibility, and graceful shutdown. This change records implementation ordering and failure handling without redefining those requirements.

## Goals / Non-Goals

**Goals:**

- Provide one discoverable, accurate startup and shutdown sequence for maintainers.
- Distinguish process bootstrap, single-instance gating, infrastructure initialization, UI composition, and optional startup work.
- Record failure handling where the current code establishes it.
- Make later startup refactors comparable against the same baseline.

**Non-Goals:**

- Change startup ordering, error handling, dependencies, service lifetimes, or user-visible behavior.
- Introduce startup coordinators or move code between projects.
- Duplicate the behavioral requirements already maintained by `openspec/specs/app-lifecycle/spec.md`.

## Decisions

- Keep the durable behavioral contract in the existing `app-lifecycle` specification.
- Add implementation-oriented sequence and failure-policy documentation to `docs/architecture.md`, with a separate focused document only if that improves navigation without duplicating the lifecycle spec.
- Describe the current code as observed and label any proposed future boundaries as future direction, not current behavior.
- Treat a second instance as an early exit path; describe first-instance initialization in actual execution order.
- Record shutdown disposal order and the configuration flush behavior provided by DI disposal.

## Risks / Trade-offs

- **Documentation can drift from code** -> Include source file references and make later startup changes update the contract in the same OpenSpec change.
- **Duplicated lifecycle requirements** -> Keep normative user-observable behavior in the existing app-lifecycle spec and document only implementation sequence/policy here.

## Migration Plan

No runtime migration is required. Review the documented sequence against the current code and update it whenever a later coordinator extraction changes ownership or ordering.

## Open Questions

None.
