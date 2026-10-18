## 1. Default-agent option gating

- [x] 1.1 Add `IsInstalled` to `AgentOption` and pass the card's installed state in `RebuildDefaultAgentOptions`; verify uninstalled agents map to disabled options
- [x] 1.2 Style the "Default agent" dropdown entries in `SettingsAgentsPage.xaml`: uninstalled agents render dimmed and disabled, "None" and installed agents stay selectable

## 2. Detect action

- [x] 2.1 Add `DetectCommand`, `IsDetecting`, and `CanDetect` to `AgentManagementViewModel` and `IAgentManagementSection`: re-probe every supported agent, update the cards, the availability summary, and the default-agent options; keep the command disabled while the pass runs
- [x] 2.2 Rebuild the default-agent options after a single-agent "Test" probe
- [x] 2.3 Add the detect icon button above the agent cards in `SettingsAgentsPage.xaml` (agents-list header, command-bound, tooltip, automation name)
- [x] 2.4 Move the detect action out of the AGENTS summary card after review: the summary card keeps its badge only, and the action lives in the agents-list header
- [x] 2.15 Restyle the detect action as a ghost chip button (refresh icon + "Refresh" label) with the launcher footer hover/pressed/disabled behavior

## 3. Tests and validation

- [x] 3.1 Add `AgentManagementViewModelTests` coverage: option gating on load, and the detect pass updating cards, summary, and option availability
- [x] 3.2 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`
- [x] 3.3 Run `openspec validate --all` and confirm the change validates
