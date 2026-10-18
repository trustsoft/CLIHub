## 1. Card view model

- [x] 1.1 Replace `AgentCardItem.StatusText` with `StatusVersionText` combining status and version; omit the version while it is "…", "unknown", or empty; raise change notifications from the `IsInstalled` and `Version` setters

## 2. Card layout

- [x] 2.1 Restructure the agent card template in `SettingsAgentsPage.xaml`: combined status line, "Not installed" danger chip, ellipsized plugin path with tooltip, and page-local chip/button-icon resources
- [x] 2.2 Move Commands and Detection markers into a collapsed "Details" expander and keep the action outcome message visible below it
- [x] 2.3 Move Test / Update / Edit Config into a right-hand action column with icon+text buttons and keep the update note under them
- [x] 2.4 Add the `SettingsDetailsExpander` style to `SettingsStyles.xaml` (muted header, chevron flip on expand, hover state)
- [x] 2.5 Compact the card after visual review: actions in one right-hand row of compact buttons, hide the status line while the "Not installed" chip is shown, rotate the chevron around its center, align card margin and corner radius with the sibling project lists
- [x] 2.6 Final pass after visual review: replace the action buttons with shared icon-only chip buttons, show installation state and version through a single state chip next to the name (`VersionChipText`), and raise the Details header contrast with an accent hover state
- [x] 2.7 Pin the action row to the top of the card so expanding Details does not shift the buttons
- [x] 2.8 Restructure the card into a two-row grid: identity, path, notes, and actions in the top row; the Details expander as a full-width second row
- [x] 2.9 Drop the static update note (the disabled Update button conveys missing update support; a blocked update is explained by the action outcome message) and remove `AgentCardItem.UpdateNote`; show the declared command set as the visible line under the name and move the plugin folder path into the Details section
- [x] 2.10 Remove the Details block entirely: drop the path and detection-marker display plus the marker properties from `AgentCardItem`, delete the interim expander style, and replace the spec requirement through REMOVED "Agent card content" + ADDED "Agent card displayed content"
- [x] 2.11 Label the commands line with a muted "Supported commands:" prefix so the list does not read as a bare word set
- [x] 2.12 Replace the "Supported commands:" text prefix with a muted command icon whose tooltip carries the label; wrap the list beside the icon
- [x] 2.13 Align the command icon with the commands text by matching the icon font size to the text size and dropping the offset margin
- [x] 2.14 Revert the icon experiment after visual review: the commands line is back to the muted "Supported commands:" text prefix

## 3. Spec and tests

- [x] 3.1 Add the `agent-ui` spec delta for the modified "Agent card content" requirement
- [x] 3.2 Add `AgentCardItemTests` covering `StatusVersionText` formatting and change notifications

## 4. Validation

- [x] 4.1 Run `dotnet build CLIHub.sln` and `dotnet test CLIHub.sln`
- [x] 4.2 Run `openspec validate --all` and confirm the change validates
