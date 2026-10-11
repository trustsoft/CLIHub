## 1. Configuration and model

- [x] 1.1 Add `DefaultAgentId` (`string?`, default null) to `AppPreferences`, mirror it in `PreferencesDocument` and `ConfigurationSnapshot.ClonePreferences`, and round-trip it; verify serialization tests (absent ⇒ null, value round-trips).
- [x] 1.2 Thread `defaultAgentId` through `SettingsInput`/`SettingsDraft` and the `SettingsApplicationService` load/save path; verify the Settings application tests.

## 2. Core agent management

- [x] 2.1 Add `CLIHub.Core.Agents.IAgentManagementService` + `AgentManagementService` with `ProbeAsync(plugin)` (invalidate detection+version, re-check host install, re-probe version) and `UpdateAsync(plugin)` (capability check, running-agent block via `AgentProcessMonitor`, interactive update in the user profile directory), plus the result records; verify unit tests for each branch (installed/not, version/unknown, unsupported, blocked, started).
- [x] 2.2 Expose the agent's `plugin.json` path (from `Plugin.PluginDirectory`) for the Edit Config action; verify a helper/test for the path.
- [x] 2.3 Register the service in `CLIHub.Core` composition; verify the solution builds and `ServiceCollectionExtensionsTests` pass.

## 3. Default agent selection

- [x] 3.1 Preselect the stored default agent when the launch window opens (ignore missing/unknown ids); verify launch-window view-model tests for preselect and no-preselect.

## 4. Settings UI

- [x] 4.1 Add `AgentCardItem` (name, logo, installed, version, plugin path, command labels, detection markers, action availability, probe message) with change notifications; verify item tests.
- [x] 4.2 Populate the card list and the supported/installed metrics in `SettingsViewModel` on load; verify view-model tests.
- [x] 4.3 Add the per-card actions (Test/Update/Edit Config) as commands with the D4 guards and outcome reporting, run through `IApplicationOperationLifetime` and marshalled to the UI thread; verify action tests.
- [x] 4.4 Add the "Default agent" selector (options from loaded agents + "None") wired to the persisted preference; verify save/cancel tests.
- [x] 4.5 Rebuild `SettingsAgentsPage` to show the metrics line, the default-agent selector, the card list (identity, status, version, path, commands, markers, action buttons), and keep the existing probe fields; verify the navigation/view tests still pass and the architecture boundary test is green.

## 5. Verification

- [x] 5.1 Run `dotnet build CLIHub.sln` and verify it succeeds.
- [x] 5.2 Run `dotnet test CLIHub.sln` and verify all tests pass.
- [x] 5.3 Run `openspec validate settings-agents-management --strict` and verify the change is valid.
- [x] 5.4 Smoke-test: cards render with correct fields; Test re-probes and updates the card; Update opens a terminal in the profile dir and is blocked while an agent runs; Edit Config opens plugin.json; the default agent is preselected on the next launch-window open; metrics match the loaded/installed agents.
