## 1. Built-in Descriptors

- [x] 1.1 Move the opencode descriptor to `src/CLIHub.Core/SeedPlugins/opencode/plugin.json` and add `pi`, `cline-cli`, `github-copilot`, `openclaude`, `qwen-code` descriptors using the verified command table, and verify each file is valid JSON
- [x] 1.2 Add `<EmbeddedResource Include="SeedPlugins\**\*.json" />` to `src/CLIHub.Core/CLIHub.Core.csproj` and verify the resources are embedded
- [x] 1.3 Remove `samples/plugins/` (superseded by the embedded seed set)

## 2. Seeder Service

- [x] 2.1 Add `IPluginSeeder` in `src/CLIHub.Core/Interfaces/IPluginSeeder.cs` with `int SeedIfEmpty()` returning the number of descriptors written, and verify it compiles
- [x] 2.2 Implement `PluginSeeder` reading embedded descriptors and writing `<plugins>\<id>\plugin.json` only when the folder has no plugin subdirectory and only when the target file does not exist; log per-plugin warnings and never throw, and verify `CLIHub.Core` compiles
- [x] 2.3 Register `IPluginSeeder` in `AddClIHubCoreServices` and verify the container resolves it

## 3. Startup Integration

- [x] 3.1 In `App.OnStartup`, call `IPluginSeeder.SeedIfEmpty()` after ensuring the AppData layout and before `IPluginManager.LoadPlugins()`, and verify a fresh install seeds and loads six agents

## 4. Tests

- [x] 4.1 Add `PluginSeederTests`: seeds into an empty folder; does nothing when a plugin exists; does not overwrite an existing descriptor; is idempotent; writes all six agent ids
- [x] 4.2 Verify each embedded descriptor deserializes via `PluginManager` into a valid plugin (id, name, launch command)
- [x] 4.3 Verify `dotnet test` passes

## 5. Verification

- [x] 5.1 Build the full solution with 0 warnings/errors
- [x] 5.2 Manually verify: with an empty `%APPDATA%\CLIHub\plugins\`, starting the app seeds six descriptors and lists six agents
- [x] 5.3 Manually verify: editing a seeded descriptor and restarting does not overwrite the edit

> Verified: fresh run logged `Seeded 6 plugin descriptor(s)` and `Loaded 6 plugin(s)` (cline-cli, github-copilot, openclaude, opencode, pi, qwen-code); a restart with the folder populated logged no seeding and loaded 6. 79/79 tests pass. Note: embedded-resource names sanitize '-' to '_', so the plugin id is read from the descriptor JSON.
