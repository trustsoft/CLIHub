## Why

The agent card on the CLI Agents settings page shows every piece of plugin metadata at once: the full `plugin.json` path wrapped over several lines, a "Commands" section, and a "Detection markers" section with host paths and project indicators. This makes each card tall and noisy, and it is the only settings list that ignores the compact-row language used by the Projects and Project Discovery pages. Most users need only identity, installation status, version, and actions; the descriptor metadata is reference information for troubleshooting.

## What Changes

- Show the installation state as a chip next to the agent name: the installed version when known ("v1.2.3"), an "Installed" state while the version is unknown, and a danger-styled "Not installed" chip when the agent is missing from the host.
- Show the declared command set as a compact labeled line ("Supported commands: …") under the agent name.
- Remove the plugin folder path and the declared detection markers from the card entirely; this reference information stays in each agent's `plugin.json` descriptor, which remains reachable through the card's Edit Config action.
- Move the card actions (Test / Update / Edit Config) into a right-hand icon-only action row using the shared icon-chip button style from the project lists, with tooltips and automation names.
- Keep the action outcome message visible on the card at all times; drop the static update note because the disabled Update button already conveys missing update support, and a blocked update is explained by the action outcome message.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `agent-ui`: The "Agent card content" requirement is replaced by "Agent card displayed content": the card presents identity, the installation state chip, and the declared command set, while the plugin folder path and detection markers are no longer presented on the card.

## Impact

- `src/CLIHub/ViewModels/Agents/AgentCardItem.cs` — `StatusVersionText` replaces `StatusText`; change notifications from the `IsInstalled` and `Version` setters.
- `src/CLIHub/Views/SettingsAgentsPage.xaml` — new card template: status chip, ellipsized path with tooltip, "Details" expander, right-hand action column with icon+text buttons.
- `src/CLIHub/Themes/SettingsStyles.xaml` — new `SettingsDetailsExpander` style.
- Tests: new `AgentCardItemTests` covering `StatusVersionText` formatting and change notifications.
- No changes to agent commands, probing, updates, or persistence.
