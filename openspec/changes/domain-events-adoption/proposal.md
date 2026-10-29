# Domain Events Adoption Catalogue + AgentLaunched Event

## Why

The POC delivered `DomainEventBus` and `ProjectSelected` as the first domain event. This change adds (a) durable adoption rules so future events are justified and consistent, and (b) exactly one more event with a real consumer — `AgentLaunched` — that enriches audit trails with structured launch context (plugin ID, project path, command kind, launch timestamp) beyond the existing plain log lines.

## What Changes

1. Create `docs/architecture/domain-events.md` as the durable adoption catalogue, linked from `docs/architecture.md`
2. Add `AgentLaunched` event published from `AgentCommandService.ExecuteAsync` after a successful `Launch` or `Resume` (lines 72-75); `Update` and `Version` never publish it
3. Add `AgentLaunchAuditSubscriber` that emits a structured Debug log line with operation scope, plugin ID, project path, and command kind
4. Wire the subscriber in Core DI composition

## Impact

- No user-visible behavior change
- Validates the event bus under a second real event
- Establishes adoption rules for future events

## Non-Goals

- Events for Version/Update commands (not interactive launches; out of scope)
- UI notifications or recency tracking (no consumer need)
- Event subscribers that change behavior (audit/diagnostic only)
- Retroactive publication into existing launches (publication starts after implementation)

## Dependencies

None
