## 1. Catalog Reload

- [x] 1.1 Add `ReloadPlugins()` and `PluginsChanged` to `IPluginCatalog`, and refactor the shared scan path so initial load preserves behavior while reload replaces the current snapshot.
- [x] 1.2 Raise exactly one notification after a completed manual reload and verify added/removed plugins plus initial-load notification behavior with focused tests.

## 2. Verification

- [x] 2.1 Run focused catalog reload tests and the complete Core test project, verifying all tests pass.
- [x] 2.2 Run `dotnet build CLIHub.sln -c Release`, verifying the solution builds without warnings or errors.
- [x] 2.3 Run `dotnet test CLIHub.sln -c Release`, verifying Core and UI test projects pass.
- [x] 2.4 Run `openspec validate add-plugin-catalog-reload`, verifying the delta spec is valid.
