## 1. Core model

- [x] 1.1 Remove `AgentCommandKind.Init` and the `AgentCommands.Init` property and its `Get` mapping; verify `CLIHub.Core` builds and `AgentCommandServiceTests` are updated (drop the Init case).
- [x] 1.2 Confirm an unknown `init` entry in a descriptor is ignored; verify a plugin with an `init` field still loads (descriptor reader test or existing coverage).

## 2. Seeded descriptors

- [x] 2.1 Remove the `init` command from `SeedPlugins/github-copilot/plugin.json`; verify the plugin still loads and seeds.

## 3. Application wiring

- [x] 3.1 Remove `CanInit` from `AgentItem` and update `AgentItemTests`; verify tests pass.
- [x] 3.2 Remove `InitCommand` from `WindowActionCoordinator` and `LaunchWindowViewModel`, and the `initialize` parameter/entry from `LaunchWindowActionBuilder` and `MenuActionCoordinator`; update `LaunchWindowActionBuilderTests`; verify the app builds and menu tests pass.
- [x] 3.3 Remove the Init button/click from the legacy `MainWindow` and the now-unused `IconGlyphs.Initialize`; verify the app builds.

## 4. Documentation

- [x] 4.1 Update `README.md` so the agent command list no longer mentions initialize; verify the wording matches the remaining commands.

## 5. Verification

- [x] 5.1 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 5.2 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [x] 5.3 Run `openspec validate remove-init-agent-command --strict` and verify the change is valid.
- [x] 5.4 Smoke-test: the Agents actions menu offers launch, resume, update, version, and refresh only (no Initialize); an agent with a legacy `init` descriptor field still loads and launches.
