# Plugin Catalog Diagnostics

## Why

Plugin validation errors today are logged and dropped - the catalog exposes only valid plugins, so users and UI code cannot discover invalid descriptors or their errors. A descriptor with missing fields or malformed JSON disappears silently; the user must hunt logs to diagnose. Remote catalogs (marketplace) and forward-compatible schema versioning both require keeping invalid entries visible so the UI can render them as "disabled with reason".

## What Changes

- Add optional `schemaVersion` (integer) to plugin.json; absent defaults to 1; > 1 or non-integer is invalid.
- The catalog retains invalid entries with validation errors; GetAllPlugins() stays valid-only, a new accessor exposes invalid ones.
- Introduce IPluginDescriptorSource port (local-folder implementation only) to reserve remote-catalog extension.
- Catalog load operations get a logging scope through ILoggingContextProvider.

## Non-Goals

UI rendering (change B), file watchers, new lifecycle states, marketplace implementation, seeding changes, updateCheck validation, remote source implementation.

## Dependencies

None. Change B (UI surface for invalid entries) depends on this change.
