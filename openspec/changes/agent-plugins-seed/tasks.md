## 1. Built-in Descriptors

- [ ] 1.1 Move the opencode descriptor to `src/CLIHub.Core/SeedPlugins/opencode/plugin.json` and add `pi`, `cline-cli`, `github-copilot`, `openclaude`, `qwen-code` descriptors using the verified command table, and verify each file is valid JSON
- [ ] 1.2 Add `<EmbeddedResource Include="SeedPlugins\**\*.json" />` to `src/CLIHub.Core/CLIHub.Core.csproj` and verify the resources are embedded
- [ ] 1.3 Remove `samples/plugins/` (superseded by the embedded seed set)

## 2. Seeder Service

- [ ] 2.1 Add `IPluginSeeder` in `src/CLIHub.Core/Interfaces/IPluginSeeder.cs` with `int SeedIfEmpty()` returning the number of descriptors written, and verify it compiles
- [ ] 2.2 Implement `PluginSeeder` reading embedded descriptors and writing `<plugins>\<id>\plugin.json` only when the folder has no plugin subdirectory and only when the target file does not exist; log per-plugin warnings and never throw, and verify `CLIHub.Core` compiles
- [ ] 2.3 Register `IPluginSeeder` in `AddClIHubCoreServices` and verify the container resolves it

## 3. Startup Integration

- [ ] 3.1 In `App.OnStartup`, call `IPluginSeeder.SeedIfEmpty()` after ensuring the AppData layout and before `IPluginManager.LoadPlugins()`, and verify a fresh install seeds and loads six agents

## 4. Tests

- [ ] 4.1 Add `PluginSeederTests`: seeds into an empty folder; does nothing when a plugin exists; does not overwrite an existing descriptor; is idempotent; writes all six agent ids
- [ ] 4.2 Verify each embedded descriptor deserializes via `PluginManager` into a valid plugin (id, name, launch command)
- [ ] 4.3 Verify `dotnet test` passes

## 5. Verification

- [ ] 5.1 Build the full solution with 0 warnings/errors
- [ ] 5.2 Manually verify: with an empty `%APPDATA%\CLIHub\plugins\`, starting the app seeds six descriptors and lists six agents
- [ ] 5.3 Manually verify: editing a seeded descriptor and restarting does not overwrite the edit
