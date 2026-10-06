## 1. Catalog Boundary

- [x] 1.1 Add `IPluginCatalog` and move the current plugin loading/state implementation into `PluginCatalog`, preserving existing loading behavior and focused plugin tests.
- [x] 1.2 Convert `IPluginManager`/`PluginManager` into a delegating compatibility adapter and verify it shares the catalog instance through DI.

## 2. Consumer Migration

- [x] 2.1 Migrate startup, tray, launch-window, and main-window consumers from `IPluginManager` to `IPluginCatalog`, verifying project compilation and existing workflows.
- [x] 2.2 Update Core/UI tests and composition coverage for the catalog boundary and adapter delegation.

## 3. Verification

- [x] 3.1 Run focused plugin/catalog tests and the complete Core test project, verifying all tests pass.
- [x] 3.2 Run `dotnet build CLIHub.sln -c Release`, verifying the solution builds without warnings or errors.
- [x] 3.3 Run `dotnet test CLIHub.sln -c Release`, verifying Core and UI test projects pass.
- [x] 3.4 Run `openspec validate introduce-plugin-catalog-boundary`, verifying the structural, spec-free change is valid.
