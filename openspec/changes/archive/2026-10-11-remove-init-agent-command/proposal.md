## Why

The agent command set carries an `init` (initialize-in-project) command that no longer fits the model: it is only declared by one seeded descriptor, it is unverified, and it adds a dedicated menu entry, capability property, and enum member for a project-setup action CLIHub does not otherwise use. Removing it simplifies the agent command surface before agent management is built on top of it.

## What Changes

- Remove `init` from the agent command model: drop the `Init` member from `AgentCommandKind`, the `Init` property and `Get` mapping from `AgentCommands`, and the `CanInit` capability from the agent row.
- Remove the **Initialize** action from the launch-window Agents actions menu and from the legacy `MainWindow` command buttons.
- Drop the `init` command from the seeded `github-copilot` descriptor; existing descriptors that still contain an `init` entry ignore it as an unknown field.
- **BREAKING** for the plugin descriptor contract: `init` is no longer a recognized command kind. Descriptors that declare it keep loading (the extra field is ignored); its command is no longer offered.
- The remaining command set is `launch`, `resume`, `version`, `update`.

## Capabilities

### New Capabilities
<!-- None: this change removes a command from an existing capability. -->

### Modified Capabilities
- `agent-commands`: the named command set no longer includes `init`, and `init` is no longer an interactive command.
- `agent-ui`: the init capability detection, init menu enablement, and init dynamic-state example are removed.
- `main-window-layout`: the Agents actions menu no longer offers Initialize.

## Impact

- Core: `AgentCommandKind`, `AgentCommands`, seeded `github-copilot` descriptor.
- App: `AgentItem.CanInit`, `WindowActionCoordinator.InitCommand`, `LaunchWindowViewModel.InitCommand`, `LaunchWindowActionBuilder`, `MenuActionCoordinator`, `MainWindow` Init button/click, `IconGlyphs.Initialize`.
- Tests: `AgentItemTests`, `AgentCommandServiceTests`, `LaunchWindowActionBuilderTests`.
- Docs: `README.md` agent-command list.

## Non-goals

- Changing `launch`, `resume`, `version`, or `update` behavior.
- Migrating stored configuration (no persisted field references `init`).
