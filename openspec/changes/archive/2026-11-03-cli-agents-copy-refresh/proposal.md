# CLI Agents Page Copy Refresh

## Why

The CLI Agents page (Settings → CLI Agents) currently has text that is redundant, ambiguous, or longer than needed:
- The page description repeats "CLI" from the title and omits update checking, which occupies a third of the page.
- The DEFAULT summary card detail line says "No default agent — choose one" when the value already shows "None", and then says "Preselected in the launch window" when an agent is selected with an unknown version, duplicating the hint text.
- The UPDATES summary card detail line reads "N of M installed agents" when updates are available, which can be misread as "N of M agents are installed"; the unknown-availability detail "Run Check updates" includes "Run" before the button name.
- The "Default agent" hint duplicates "Preselected in the launch window."
- Per-card status messages include verbose phrases like "Update blocked while running", "Not installed on the host", "Installed · version {version}", "Update available · {installed} → {latest}", "Update availability unknown", "Update started.", and "Update failed." that can be shortened or rephrased.
- Tooltips on the AGENTS and UPDATES detail lines duplicate visible text; header and per-card action tooltips lack "all/each" vs. "this" distinctions.

An approved mockup (`cli-agents-copy-mockup.html`) shows the refreshed copy, validated for clarity, brevity, and consistency.

## What Changes

Replace the CLI Agents page text with the approved set:

1. **Page description** (SettingsNavigation.cs:73): "Agent status, updates, and the default agent."
2. **Summary cards** (SettingsWindow.xaml:216-239, AgentManagementViewModel.cs):
   - AGENTS: Remove the `DetailToolTip` attribute entirely (the detail "N supported · M not installed" stays).
   - DEFAULT detail:
     - No default: "Choose an agent below"
     - Selected, version unresolved: "In the launch window"
     - Selected, version known: "Version {version}" (unchanged)
   - DEFAULT: Keep `ValueToolTip="{Binding AgentsState.DefaultAgentName}"`.
   - UPDATES detail:
     - 1 update: "1 of {installed} agent{s} has an update" (singular/plural for installed count)
     - N updates: "{count} of {installed} agent{s} have updates"
     - No installed: "No installed agents" (unchanged)
     - All up to date: "All installed agents up to date" (unchanged)
     - Unknown: "Check updates" (no "Run")
   - UPDATES: Remove the `DetailToolTip` attribute entirely.
3. **Default agent hint** (SettingsAgentsPage.xaml:52): "Preselected in the launch window."
4. **Header actions** (SettingsAgentsPage.xaml:90-110):
   - Detect: tooltip and automation name "Recheck each agent and its version"
   - Check updates: tooltip and automation name "Check all installed agents for updates"
5. **Agent card** (SettingsAgentsPage.xaml:160-230, AgentManagementViewModel.cs):
   - Keep visible "Supported commands: " label and automation name unchanged.
   - Update chip tooltip (XAML:163): "Newer version available"
   - Per-card Detect tooltip (XAML:188): "Recheck this agent and its version"; automation name stays "Detect agent".
   - Per-card Check updates tooltip and automation name: keep "Check this agent for updates".
   - Per-card Update tooltip (XAML:212): "Run this agent's update command"; automation name stays "Update agent".
   - Per-card Edit config tooltip (XAML:224): "Open this agent's plugin.json"; automation name stays "Edit agent config".
   - Status messages:
     - Detect installed with version: "Installed · v{version}" (prepend "v")
     - Detect installed without version: "Installed" (unchanged)
     - Detect missing: "Not installed on this PC" (replace "host" in both DetectCardAsync and CheckCardUpdatesAsync defensive branch)
     - Update blocked: "Agent is running — stop it to update" (both ViewModel PopulateBlockedStateAsync and Core Service.UpdateAsync)
     - Per-card Check updates: "Update available" (no version details), "Up to date" (unchanged), "Update status unknown" (no "availability")
     - Update started: "Update started" (drop trailing period)
     - Update failed fallback: "Update failed" (drop trailing period)
     - Unchanged: "Updating {name}…", "Opened {path}", "Could not open {path}", "Update check failed", "No installed agents to check", "Checked N agents · M update(s) available"

## Impact

- Tested surface: Settings → CLI Agents page, summary cards, default agent selector, agent cards and their actions, tooltips, automation names.
- 16% reduction in visible UI text, 22% in tooltips.
- Tests updated: string assertions in AgentManagementViewModelTests (Load, Detection, UpdateAvailability, CardActions) and AgentManagementServiceTests.
- No breaking change: string updates only, no behavior or data-format change.

## Non-Goals

- Do not change: page title "CLI Agents", sidebar label, section headers MANAGEMENT/AGENTS, summary labels (AGENTS/DEFAULT/UPDATES), state chip texts (v{version}/Installed/Not installed/Detecting…), combo options (None/{Name} (version)/{Name} (Not installed)), Detect/Check updates button labels, invalid card "Invalid" and reason, layout, colors.
- Do not modify the mockup file (`cli-agents-copy-mockup.html`); it must remain untracked and excluded from any commit.
- Do not run git write commands or commit.

## Dependencies

None.
