## Context

The application already registers one singleton `IUpdateWorkflow` and the launch window, tray, What's New, and startup paths use it. `SettingsViewModel` still names the Core `IUpdateChecker` contract in its constructor and field, even though composition is intended to provide the shared application workflow. `MainWindow` is an unregistered legacy window and is not part of the active startup graph.

## Goals / Non-Goals

**Goals:**

- Make Settings consume the shared application update boundary while preserving its existing status messages and tracked operation lifetime.
- Add a focused regression test proving the Settings path delegates to the workflow contract.
- Remove the last direct checker dependency from the legacy window if the source remains in the repository.

**Non-Goals:**

- Do not change Core update ports, Velopack behavior, update result values, or cancellation semantics.
- Do not change automatic startup, tray, What's New, or launch-window apply policies.
- Do not remove the legacy window in this change; only align its update dependency if it remains compiled.

## Decisions

1. **Use `IUpdateWorkflow` in Settings.** The application workflow already composes the Core checker and coalesces active checks. Injecting the existing application boundary is preferable to adding an adapter or duplicating coordination in Settings.
2. **Keep the Settings ViewModel presentation adapter.** It will continue to set `UpdateMessage`, use `ApplicationOperationLifetime`, and translate `UpdateCheckResult` to Settings-specific text. This keeps UI wording out of the shared workflow.
3. **Use the existing workflow test seam.** Tests will mock `IUpdateWorkflow`, as the other presentation tests do, and verify delegation/coalescing at the application boundary rather than reaching Core or Velopack.
4. **Align legacy `MainWindow` only where needed for compilation and boundary consistency.** Since it is not registered or assigned as the active shell, no new behavior will be added to it.

## Risks / Trade-offs

- [Risk] Constructor-based tests and DI registrations may still use the old checker type → update all affected fixtures and verify the full solution build.
- [Risk] GitNexus reports the Settings check method as high impact because the method participates in many update-related process traces → preserve the method's public behavior and run focused plus full update tests.
- [Risk] The legacy window may be intentionally retained for compatibility tests → change only its injected type and call site, without deleting the window or altering its UI behavior.

## Migration Plan

No persisted-data or deployment migration is required. The change is source-compatible inside the application composition root; rollback consists of restoring the previous constructor dependency and affected test fixtures.
