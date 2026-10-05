## Why

Application startup and shutdown currently span `Program`, `App.xaml.cs`, service registration, and several UI and Core services. The behavior is implemented, but its ordering and failure policy are not captured as a concise architecture contract, making future orchestration refactors harder to review safely.

## What Changes

- Document the current startup and shutdown sequence and the responsibilities of each stage.
- Record which initialization failures are tolerated and which startup conditions terminate the process.
- Establish this documented sequence as the baseline for subsequent atomic startup-coordinator changes.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This change documents existing behavior and does not change observable requirements.

## Impact

- Documentation: `docs/architecture.md` and, if useful, a focused startup document linked from it.
- Runtime code, public APIs, dependencies, and user-visible behavior are unchanged.
- OpenSpec specs are intentionally skipped because no behavioral requirement changes.
