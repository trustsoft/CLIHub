## Why

Several singleton UI controllers subscribe to application events but do not explicitly unsubscribe when the service provider is disposed. Making those lifecycle owners deterministic and enforcing the legacy-window boundary will reduce shutdown leaks and prevent retired UI paths from re-entering the active composition.

## What Changes

- Add idempotent disposal and event unsubscription to pane controllers, the update control, and launch-window composition.
- Cancel and dispose agent version-population work when its pane controller is disposed.
- Keep the old `MainWindow` source as an explicitly deprecated, unregistered legacy reference.
- Extend architecture tests to enforce lifecycle ownership and legacy registration boundaries.
- Update architecture and repository documentation.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. These changes formalize internal resource ownership without changing observable application behavior.

## Impact

Affected WPF ViewModels/controllers, application composition, architecture tests, and architecture documentation. No Core contracts, persisted settings, or user workflows change.
