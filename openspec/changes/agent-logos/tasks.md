## 1. Assets

- [x] 1.1 Add `logo.png` for each built-in agent under `src/CLIHub.Core/SeedPlugins/<id>/` (opencode, pi, cline-cli, github-copilot, openclaude, qwen-code) from the local assets or the official source, and verify each file is a valid PNG
- [x] 1.2 Embed `SeedPlugins\**\*.png` in `src/CLIHub.Core/CLIHub.Core.csproj` and verify the logo resources are embedded

## 2. Seeder

- [x] 2.1 Update `PluginSeeder` to build a sanitized-key → plugin-id map from the `*.plugin.json` resources, then write each `*.logo.png` resource to `<plugins>\<id>\logo.png`, and verify `CLIHub.Core` compiles
- [x] 2.2 Ensure logos obey the same guards as descriptors (only during first-run seeding; never overwrite an existing file; failures logged, non-fatal)

## 3. Tests

- [x] 3.1 Extend `PluginSeederTests`: seeding writes `logo.png` for all six agents; an existing `logo.png` is preserved; a plugin without a bundled logo still loads
- [x] 3.2 Verify `dotnet test` passes

## 4. Verification

- [x] 4.1 Build the full solution with 0 warnings/errors
- [x] 4.2 Manually verify: with an empty `%APPDATA%\CLIHub\plugins\`, starting the app seeds descriptors and logos, and the agent list shows the six real logos

> Verified: fresh run logged `Seeded plugin descriptor <id>` and `Seeded logo for <id>` for all six agents; each folder contains `logo.png` + `plugin.json`; `Loaded 6 plugin(s)`. 81/81 tests pass; build 0 warnings/errors. User-confirmed 4.2 (all six logos display).
