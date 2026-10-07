## 1. Reload Consumer

- [x] 1.1 Subscribe `AgentPaneController` to `PluginsChanged` and retain the latest project/filter context.
- [x] 1.2 Invalidate detection, version, and logo caches before refreshing the agent list.
- [x] 1.3 Preserve surviving selection identity and clear selection for removed plugins.

## 2. Tests

- [x] 2.1 Add reload tests for cache invalidation and surviving selection.
- [x] 2.2 Add reload tests for removed selection and stale version population cancellation.

## 3. Verification

- [x] 3.1 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`.
- [x] 3.2 Run `graphify update .`, validate the change, update `improvements.md`, and archive it.
