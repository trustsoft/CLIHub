## Why

The CLI Agents page is the only data page in the Engines & Repos group without the pinned summary cards that Project Discovery and Projects already use. Its header shows only a single hint line ("N supported · M installed"), so the installed/supported split, the selected default agent, and the effective probe policy cannot be read at a glance.

## What Changes

- Add three summary cards to the CLI Agents page header, matching the Project Discovery and Projects card style: **AGENTS**, **DEFAULT**, and **PROBE**.
- Remove the standalone supported/installed metrics line from the page body; the same counts now live in the AGENTS summary card.
- The agents card shows the installed-agent count with the supported count and the number of agents that are not installed.
- The default card shows the selected default agent name, with its detected version beneath it; when no default is selected it shows "None" and a hint to choose one.
- The probe card shows the effective TTL and timeout in minutes and seconds, including the built-in defaults while the fields are empty, and states whether the values are built-in defaults or custom.
- The cards refresh when the agent cards are rebuilt or re-probed, when the default agent changes, and when the probe fields are edited.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `settings-theme`: extend the page-header summary-card contract to the CLI Agents page and describe the summary row for every page that has one.
- `preferences-ui`: replace the CLI Agents "supported/installed metrics line" with the page-header summary cards.

## Impact

- `src/CLIHub/Views/SettingsWindow.xaml`: a new summary-card template and the header trigger for the `agents` page.
- `src/CLIHub/Views/SettingsAgentsPage.xaml`: remove the duplicated metrics line from the page body.
- `src/CLIHub/ViewModels/Agents/IAgentManagementSection.cs` and `AgentManagementViewModel.cs`: installed/supported counts, availability summary, and default-agent display values; remove the obsolete `Metrics` line property.
- `src/CLIHub/ViewModels/Settings/SettingsViewModel.cs`: effective probe-policy summary and detail derived from the TTL/timeout fields and their built-in defaults.
- `docs/glossary.md` and `docs/architecture/ui.md`: define and use the "summary card" and "summary row" terminology.
- `tests/CLIHub.Tests/ViewModels/Agents/AgentManagementViewModelTests.cs` and the settings view-model tests: assertions for the new summary values.
- No configuration or persistence changes.
