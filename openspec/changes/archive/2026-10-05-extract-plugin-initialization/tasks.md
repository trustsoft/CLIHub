## 1. Add Plugin Initialization Boundary

- [x] 1.1 Add `IPluginInitializationService` and its application-layer implementation, delegating exactly once to `IPluginSeeder.SeedIfEmpty()` and then `IPluginManager.LoadPlugins()`; verify call order with focused tests.
- [x] 1.2 Register the new service in WPF composition and replace the two inline plugin calls in `App.OnStartup`; verify the interface resolves from the service provider.

## 2. Verify Preserved Behavior

- [x] 2.1 Verify that plugin seeding and loading remain synchronous and ordered during startup, with no changes to existing Core plugin services or failure boundaries.
- [x] 2.2 Run `dotnet build CLIHub.sln -c Release` and `dotnet test CLIHub.sln -c Release`; verify the full solution remains green.
- [x] 2.3 Run `openspec validate extract-plugin-initialization` and verify the change has no spec deltas.
