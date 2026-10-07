## Why

`IUpdateService` currently combines version reporting, update checks, state observation, downloading, and installation/restart. This forces UI and startup code to depend on operations they do not use and makes check-only tests construct a download/apply-capable service.

## What Changes

- Split update contracts into version, check, state, download, and installation ports.
- Keep the existing Velopack-backed implementation as the infrastructure adapter implementing the composed service.
- Update application coordinators and views to depend on only the ports they need.
- Add explicit state-source registration and preserve current duplicate-download, failure, cancellation, and apply semantics.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- None. This is an internal contract refactor; `skip_specs: true` is set because update behavior remains unchanged.

## Impact

- Affects update interfaces, the Velopack-backed adapter, DI aliases, startup/UI consumers, and tests.
- No changes to update labels, status transitions, timeout behavior, or release feed configuration.
