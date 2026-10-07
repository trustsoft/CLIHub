## Context

Production references show that `IPluginManager` and `PluginManager` are not consumed outside their own registration and tests. The plugin initialization service already uses `IPluginCatalog` directly. `IProcessLauncher` is only consumed by `ProcessRuntimePreferenceTarget`; all agent command/version services already use the split process ports.

## Decisions

Delete the plugin manager interface and adapter, and remove their DI registrations. Keep plugin catalog tests because they validate canonical catalog behavior, not the removed adapter.

Change `ProcessRuntimePreferenceTarget` to `IInteractiveProcessRunner`. Remove aggregate process registration and make the shared process test fake implement `IInteractiveProcessRunner` and `IProcessOutputRunner` separately. The concrete `ProcessLauncher` may implement both interfaces without exposing a new aggregate contract.

## Risks / Trade-offs

- [Risk] A hidden consumer depends on the old adapter. -> Search all source and test references before deletion and require full build/tests.
- [Risk] DI resolution loses runtime behavior. -> Add/retain registration coverage for the interactive and output ports and resolve `IRuntimePreferenceTarget` through the application composition.

## Removal Decision

The compatibility APIs are removed now because no production consumer remains. Reintroducing an aggregate should require a concrete consumer and a documented ownership decision.
