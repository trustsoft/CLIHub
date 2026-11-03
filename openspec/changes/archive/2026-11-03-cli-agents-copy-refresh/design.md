# Design — CLI Agents Copy Refresh

## Verified Current State

### Page Description
- **File**: `src/CLIHub/ViewModels/Settings/SettingsNavigation.cs:73`
- **Current**: `"Installed CLI agents and the default agent."`

### Summary Cards (XAML)
- **File**: `src/CLIHub/Views/SettingsWindow.xaml:220`
- **AGENTS DetailToolTip**: `"Installed agents and supported plugin count"`
- **File**: `src/CLIHub/Views/SettingsWindow.xaml:233`
- **UPDATES DetailToolTip**: `"Agent update availability"`

### Summary Card Details (ViewModel)
- **File**: `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:139`
- **DEFAULT, no agent**: `"No default agent — choose one"`
- **File**: `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:147`
- **DEFAULT, selected, version unresolved**: `"Preselected in the launch window"`
- **File**: `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:319`
- **UPDATES, updates available**: `"{this.UpdatesAvailableCount} of {installed} installed {noun}"`
  - `noun` is "agent" or "agents" depending on `installed` count
- **File**: `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:327`
- **UPDATES, unknown**: `"Run Check updates"`

### Default Agent Hint
- **File**: `src/CLIHub/Views/SettingsAgentsPage.xaml:52`
- **Current**: `"Preselected when the launch window opens."`

### Header Action Tooltips
- **File**: `src/CLIHub/Views/SettingsAgentsPage.xaml:94`
- **Detect tooltip**: `"Detect installed agents and versions"`
- **File**: `src/CLIHub/Views/SettingsAgentsPage.xaml:90`
- **Detect automation name**: `"Detect installed agents and versions"`
- **File**: `src/CLIHub/Views/SettingsAgentsPage.xaml:110`
- **Check updates tooltip**: `"Check for agent updates"`
- **File**: `src/CLIHub/Views/SettingsAgentsPage.xaml:106`
- **Check updates automation name**: `"Check for agent updates"`

### Per-Card Tooltips and Automation Names
- **File**: `src/CLIHub/Views/SettingsAgentsPage.xaml:163`
- **Update chip tooltip**: `"A newer version is available"`
- **File**: `src/CLIHub/Views/SettingsAgentsPage.xaml:188`
- **Detect tooltip**: `"Detect installation and version"`
- **File**: `src/CLIHub/Views/SettingsAgentsPage.xaml:183`
- **Detect automation name**: `"Detect agent"` (keep)
- **File**: `src/CLIHub/Views/SettingsAgentsPage.xaml:200`
- **Check updates tooltip**: `"Check this agent for updates"` (keep)
- **File**: `src/CLIHub/Views/SettingsAgentsPage.xaml:195`
- **Check updates automation name**: `"Check this agent for updates"` (keep)
- **File**: `src/CLIHub/Views/SettingsAgentsPage.xaml:212`
- **Update tooltip**: `"Run the agent's update command"`
- **File**: `src/CLIHub/Views/SettingsAgentsPage.xaml:207`
- **Update automation name**: `"Update agent"` (keep)
- **File**: `src/CLIHub/Views/SettingsAgentsPage.xaml:224`
- **Edit config tooltip**: `"Open the agent's plugin.json"`
- **File**: `src/CLIHub/Views/SettingsAgentsPage.xaml:219`
- **Edit config automation name**: `"Edit agent config"` (keep)

### Status Messages (ViewModel)
- **File**: `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:608`
- **Detect installed with version**: `"Installed · version {version}"`
- **File**: `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:610`
- **Detect missing (DetectCardAsync)**: `"Not installed on the host"`
- **File**: `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:804`
- **Detect missing (CheckCardUpdatesAsync defensive)**: `"Not installed on the host"`
- **File**: `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:583`
- **Update blocked**: `"Update blocked while running"`
- **File**: `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:820`
- **Per-card Check updates, update available**: `"Update available · {installed} → {latest}"`
- **File**: `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:823`
- **Per-card Check updates, unknown**: `"Update availability unknown"`
- **File**: `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:881`
- **Update started**: `"Update started."`
- **File**: `src/CLIHub/ViewModels/Agents/AgentManagementViewModel.cs:882`
- **Update failed fallback**: `result.Message ?? "Update failed."`

### Status Message (Core Service)
- **File**: `src/CLIHub.Core/Agents/AgentManagementService.cs:95`
- **Update blocked**: `"Update blocked while running"`

## Decisions

### 1. Page Description Replacement
Replace the page description with "Agent status, updates, and the default agent." to cover all three sections without redundancy.

**Rationale**: The current description "Installed CLI agents and the default agent" repeats "CLI" from the page title and omits update checking, which is prominent on the page.

### 2. Summary Card Detail Text Simplification
- DEFAULT: Use "Choose an agent below" when none selected, "In the launch window" when selected with unresolved version, keep "Version {version}" otherwise.
- UPDATES: Change "N of M installed agents" to "N of M agents have updates" (singular "has an update" for count=1) and "Run Check updates" to "Check updates".

**Rationale**: The current DEFAULT detail duplicates visible text; the UPDATES detail is ambiguous (reads as "N of M are installed" rather than "N of M have updates").

### 3. Remove Duplicate Tooltips
Remove `DetailToolTip` from AGENTS and UPDATES summary cards entirely; they duplicate the visible detail line.

**Rationale**: WPF tooltips are for additional context, not repetition. The detail line is already visible.

### 4. Default Agent Hint Shortening
Change hint to "Preselected in the launch window." (drop "when the launch window opens").

**Rationale**: Matches the summary card detail pattern and reduces verbosity without losing meaning.

### 5. Header Action Disambiguation
- Detect: "Recheck each agent and its version" (tooltip and automation name)
- Check updates: "Check all installed agents for updates" (tooltip and automation name)

**Rationale**: "each"/"all" disambiguates header actions from per-card actions; the current tooltips lack this distinction.

### 6. Per-Card Tooltip Clarifications
- Update chip: "Newer version available" (shorter, no article)
- Detect: "Recheck this agent and its version" (matches header pattern with "this")
- Update: "Run this agent's update command" (adds "this" for consistency)
- Edit config: "Open this agent's plugin.json" (adds "this", drops "the")

**Rationale**: Consistent use of "this" for per-card actions; brevity without losing clarity.

### 7. Status Message Rewording
- "Installed · v{version}" (prepend "v" to version)
- "Not installed on this PC" (replace "host" with "this PC")
- "Agent is running — stop it to update" (cause + action, drop "blocked")
- "Update available" (no version details; the chip already shows them)
- "Update status unknown" (replace "availability")
- "Update started" / "Update failed" (drop trailing periods)

**Rationale**: Brevity, clarity, user-facing language ("PC" not "host"), action-oriented guidance.

### 8. UPDATES Detail Singular/Plural Handling
When `UpdatesAvailableCount` is 1, use "1 of {installed} agent{s} has an update"; otherwise "{count} of {installed} agent{s} have updates". The `agent{s}` part pluralizes based on `installed` count.

**Rationale**: Grammatically correct for all counts; the approved mockup shows this pattern.

## Alternatives Rejected

### Alternative: Keep "host" in "Not installed on the host"
**Rejected**: "Host" is developer terminology; "this PC" is user-facing and matches Windows UI patterns.

### Alternative: Keep version details in per-card "Update available" message
**Rejected**: The update chip already shows `{installed} → {latest}` visually; repeating it in the message is redundant.

### Alternative: Keep "Run" in "Run Check updates"
**Rejected**: The button is already labeled "Check updates"; the detail line can simply match it without "Run".

### Alternative: Keep trailing periods in "Update started." and "Update failed."
**Rejected**: Other status messages have no periods; consistency favors dropping them.

## Deviations

None. The code and specs match the brief.

## Open Questions

None.
