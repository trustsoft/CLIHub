## 1. Assets

- [ ] 1.1 Add `logo.png` for each built-in agent under `src/CLIHub.Core/SeedPlugins/<id>/` (opencode, pi, cline-cli, github-copilot, openclaude, qwen-code) from the local assets or the official source, and verify each file is a valid PNG
- [ ] 1.2 Embed `SeedPlugins\**\*.png` in `src/CLIHub.Core/CLIHub.Core.csproj` and verify the logo resources are embedded

## 2. Seeder

- [ ] 2.1 Update `PluginSeeder` to build a sanitized-key → plugin-id map from the `*.plugin.json` resources, then write each `*.logo.png` resource to `<plugins>\<id>\logo.png`, and verify `CLIHub.Core` compiles
- [ ] 2.2 Ensure logos obey the same guards as descriptors (only during first-run seeding; never overwrite an existing file; failures logged, non-fatal)

## 3. Tests

- [ ] 3.1 Extend `PluginSeederTests`: seeding writes `logo.png` for all six agents; an existing `logo.png` is preserved; a plugin without a bundled logo still loads
- [ ] 3.2 Verify `dotnet test` passes

## 4. Verification

- [ ] 4.1 Build the full solution with 0 warnings/errors
- [ ] 4.2 Manually verify: with an empty `%APPDATA%\CLIHub\plugins\`, starting the app seeds descriptors and logos, and the agent list shows the six real logos
