## 1. Icons

- [x] 1.1 Add the setting glyphs needed for the two pages (for example zoom, blocked) to `IconGlyphs`; verify the app builds.

## 2. Projects & Paths

- [x] 2.1 Add a page description; `SectionHeader` `REPOSITORIES` with the watched-directories card (stacked: list + add/remove, icon), the scan-depth card (right: numeric field, icon), the discovery card (stacked: segmented, icon), and the ignored-patterns card (stacked: list + input + add/remove, icon); each keeps its existing hint.
- [x] 2.2 `SectionHeader` `BACKGROUND WATCHER` with the watcher-status card (stacked: status line, icon) and its hint; verify every control still binds, loads, and saves.

## 3. CLI Agents

- [x] 3.1 Add a page description and keep the metrics line; `SectionHeader` `MANAGEMENT` with the default-agent card (stacked: combo box selector, icon) and the agent list beneath it; verify the default-agent selector still binds.
- [x] 3.2 `SectionHeader` `PROBE` with the agents-probe card (stacked: TTL/timeout fields, icon) and its hint; verify the probe fields still bind.
- [x] 3.3 Align the custom per-agent card to the card visual language: same surface tokens (already), title typography matching a `SettingCard` title, and the logo drawn in a rounded icon block; verify agent rows render and their actions work.

## 4. Verification

- [x] 4.1 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 4.2 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [x] 4.3 Run `openspec validate settings-data-pages-cards --strict` and verify the change is valid.
- [x] 4.4 Smoke-test: Projects & Paths and CLI Agents show descriptions, section headers, and cards; list editors are stacked cards whose add/remove and inputs work; the default-agent selector and probe fields work; agent cards match the card look and their Test/Update/Edit Config still work.
