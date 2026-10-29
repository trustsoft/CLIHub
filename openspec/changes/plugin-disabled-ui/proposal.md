# Proposal: Disabled-plugin UI surface for invalid catalog entries

## Why

Change A (descriptor schema validation) added `IPluginCatalog.GetInvalidPlugins()` exposing invalid entries with their validation or read errors. Decision 3 states invalid descriptors stay visible but disabled with the failure reason shown. This change renders that visibility contract: the CLI Agents management page shows invalid entries as disabled cards with diagnostic reasons, so users see broken descriptors immediately and can act on the error (fix the descriptor or contact the source) rather than discovering the plugin silently vanished.

Without this surface, broken descriptors disappear; no feedback explains why reload did not expose an agent. This change closes the diagnostic gap by surfacing validation/read failures at the point of interaction.

## What Changes

1. `AgentManagementViewModel` consumes `GetInvalidPlugins()` alongside `GetAllPlugins()` during `Load()` and populates two card sets - valid and invalid.
2. Invalid entries render as visually distinct, disabled cards: grayed-out appearance, no action buttons enabled, and a persistent message line composing the failure reason from `ValidationResult.Errors` (joined with semicolons) or `ReadResult.Error.Message` (or the directory path when `PartialPlugin` is null and no error message is available).
3. The AGENTS section header counts stay valid-only; invalid entries do not contribute to the "supported agents" or "installed agents" tallies.
4. Reload (`PluginsChanged` event) refreshes both valid and invalid sets; the existing `Load()` path already responds to the event.
5. Default-agent selector stays valid-only (already uses `GetAllPlugins()`); launch window agent pane stays valid-only (confirmed: `AgentPaneController` line 99 uses `GetAllPlugins()`).
6. Manual smoke task verifies broken descriptor and schemaVersion > 1 descriptor both render disabled with distinct reasons, and valid agents remain unchanged.

## Impact

- Settings CLI Agents page gains disabled-card rendering; all existing valid-agent card behaviors, counts, and actions are preserved.
- Reload workflow unchanged (already raises `PluginsChanged`).
- Launch window unaffected.
- Test coverage: view-model pins (invalid cards appear disabled with reason, valid set unchanged, reload swaps both, no actions possible on invalid cards) + card/model shape tests.

## Non-Goals

- Marketplace, watchers, lifecycle enums, seeding changes, or `updateCheck` validation (out of scope).
- Launch window/agent selector changes (confirmed valid-only by design).
- Changing validation rules or catalog logic (change A complete).

## Dependencies

- Requires change A complete (verified: `GetInvalidPlugins()` implemented; `InvalidPluginEntry`, `PluginDescriptorReadResult`, `PluginValidationResult` available).
