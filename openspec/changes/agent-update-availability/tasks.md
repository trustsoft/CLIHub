# Tasks: agent-update-availability (future implementation)

Not implemented yet — this change is the approved design proposal. Tasks are intentionally
unchecked until the implementation is scheduled.

## 1. Descriptor

- [ ] 1.1 Parse the optional `updateCheck` block (`source`: `npm` | `githubReleases`) in the plugin loader
- [ ] 1.2 Expose it on `Plugin` (nullable); ignore unknown source kinds with a logged warning

## 2. Core service

- [ ] 2.1 Port `IAgentUpdateAvailabilityService` + result record (`UpToDate` / `UpdateAvailable(installed, latest)` / `Unknown`)
- [ ] 2.2 HTTP implementation: npm registry client, GitHub releases client (unauthenticated)
- [ ] 2.3 Version extraction (`\d+\.\d+(\.\d+)+`) and numeric triple comparison; TTL cache reusing the probe cadence
- [ ] 2.4 Dependency injection wiring

## 3. UI

- [ ] 3.1 Surface `2.0.32 → 2.0.33` when available (agents pane near the version; final placement is a UI decision)

## 4. Seeds and docs

- [ ] 4.1 Add `updateCheck` blocks to the six built-in descriptors (npm: opencode, cline, pi, openclaude; GitHub releases: qwen-code, github-copilot)
- [ ] 4.2 Document the block in the plugin descriptor docs

## 5. Verification

- [ ] 5.1 Unit tests: parsing, comparison, per-source clients, caching, ignored unknown kinds
- [ ] 5.2 Live: availability shown for agents with published updates; unknown under offline conditions
- [ ] 5.3 `dotnet build CLIHub.sln`, `dotnet test CLIHub.sln`, `openspec validate --all`
