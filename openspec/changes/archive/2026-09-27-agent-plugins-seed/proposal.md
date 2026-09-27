## Why

A fresh CLIHub install has an empty `plugins\` folder, so the agent list is blank until the user hand-writes `plugin.json` files. The project targets six specific agents (OpenCode, Pi, Cline CLI, GitHub Copilot, OpenClaude, Qwen Code). Shipping built-in descriptors and seeding them on first run makes the app useful immediately and demonstrates the new schema.

## What Changes

- Add built-in descriptors for all six agents, verified against their real installed CLIs (executable names, version/resume/update/init subcommands, and host install markers)
- Seed the `%APPDATA%\CLIHub\plugins\` folder from the built-in descriptors on first run (when it contains no plugin subdirectories)
- Never overwrite or delete existing user plugins
- Expose seeding as a service invoked during startup before plugin loading

## Capabilities

### New Capabilities

- `plugin-seeding`: populating the plugins folder with built-in descriptors on first run without disturbing user content

### Modified Capabilities

<!-- None: plugin loading behavior is unchanged. -->

## Impact

**New code:**
- `src/CLIHub.Core/SeedPlugins/<agent>/plugin.json` — six embedded descriptors
- `src/CLIHub.Core/Interfaces/IPluginSeeder.cs`, `src/CLIHub.Core/Services/PluginSeeder.cs`

**Modified code:**
- `src/CLIHub.Core/CLIHub.Core.csproj` — embed the seed descriptors
- `src/CLIHub.Core/ServiceCollectionExtensions.cs` — register `IPluginSeeder`
- `src/CLIHub/App.xaml.cs` — call `SeedIfEmpty()` before `LoadPlugins()`

**Removed:**
- `samples/plugins/` (its content becomes the embedded seed set; a single source of truth)

**Data:**
- `%APPDATA%\CLIHub\plugins\<id>\plugin.json` — created on first run for the six agents

**No breaking changes.**
