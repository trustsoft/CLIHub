## Why

The "Default agent" selector on the CLI Agents page offers every loaded agent, including agents that are not installed on the host. Choosing an uninstalled agent produces a default that cannot launch, and the option availability only reflects the detection pass from page open — there is no way to re-run detection while the page stays open.

## What Changes

- Mark agents that are not installed on the host as disabled entries in the "Default agent" dropdown; they stay visible (dimmed, with their version) but cannot be selected. A stored default agent that became uninstalled stays visible as a disabled entry instead of silently resetting.
- Add a "Re-detect installed agents" action above the agent cards: a ghost chip button with a refresh icon and a "Refresh" label in the agents-list header, using the launcher footer chip behavior (hover/pressed overlays, dimmed when disabled); it re-probes every supported agent against the host and refreshes the agent cards, the availability summary, and the default-agent option availability.
- Refresh the default-agent option availability after a single-agent "Test" probe as well, so the dropdown always reflects the latest known installed states.

## Capabilities

### Modified Capabilities

- `preferences-ui`: The "CLI Agents page management" requirement changes so the "Default agent" selector marks uninstalled agents as disabled entries, the option availability refreshes when detection runs, and the page offers a detect action above the agent cards.

## Impact

- `src/CLIHub/ViewModels/Agents/AgentCardItem.cs` — `AgentOption` gains an `IsInstalled` flag.
- `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs` — option building passes the installed flag; new `DetectCommand`/`IsDetecting`/`CanDetect`; `TestAsync` rebuilds the options.
- `src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs` — surface the detect command and state.
- `src/CLIHub/Views/SettingsAgentsPage.xaml` — disabled/dimmed style for uninstalled dropdown entries; detect icon button in the agents-list header.
- Tests: `AgentManagementViewModelTests` for option gating and the detect pass.
- No persistence changes; "None" and stored-default behavior are unchanged.
