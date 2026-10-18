## 1. Agent summary values

- [x] 1.1 Add `InstalledAgentCount`, `SupportedAgentCount`, `AvailabilitySummary`, `DefaultAgentName`, `DefaultAgentDetail`, and `HasDefaultAgent` to `IAgentManagementSection` and compute them in `AgentManagementViewModel`; raise the availability properties from `RefreshAvailabilitySummary` and the default properties from the selected-agent setter; verify with unit tests for the counts, the split text, the no-default state, and the refresh after a successful probe. Remove the obsolete `Metrics` line property and keep the tests on the counts.
- [x] 1.2 Add `ProbePolicySummary` and `ProbePolicyDetail` to `SettingsViewModel`, resolving empty or invalid fields to the built-in defaults; raise both from the TTL/timeout setters and from `Load`.

## 2. Summary cards

- [x] 2.1 Add an `AgentsSummaryCards` data template to `SettingsWindow.xaml` with the AGENTS, DEFAULT, and PROBE cards, using the existing card padding, icon block, number, and hint styles; verify the page builds.
- [x] 2.2 Show the template for `SelectedPage.Key = "agents"` in the fixed summary row; verify the cards appear under the CLI Agents header and scroll independently of the content.
- [x] 2.3 Mute the default-agent name when it is "None" and trim a long name with a tooltip; verify both states.

## 3. Terminology and specs

- [x] 3.1 Define "summary card" and "summary row" in `docs/glossary.md`, including the Russian terms «сводная карточка» and «ряд сводных карточек», and point `docs/architecture/ui.md` at that definition.
- [x] 3.2 Replace the CLI Agents "supported/installed metrics line" requirement in `preferences-ui` with the page-header summary cards, and update the `settings-theme` page-header scenario so pages without summary cards are the ones that show no summary row.
- [x] 3.3 Remove the `Metrics` line property and align code comments, XML documentation, and test names with the "summary cards" and counts terminology.

## 4. Verification

- [x] 4.1 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`; verify no errors or failures.
- [x] 4.2 Run `openspec validate --all`; verify the change and all specs are valid.
- [x] 4.3 Manually check the CLI Agents page: three cards, installed/supported split, default agent and version, effective probe values, and refresh after Test and after editing the probe fields.
