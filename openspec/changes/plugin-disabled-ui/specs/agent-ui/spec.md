# agent-ui Specification Delta

## ADDED Requirements

### Requirement: Invalid plugin entry display

The CLI Agents management page SHALL display invalid plugin entries as visually distinct, disabled cards alongside valid agent cards. An invalid entry is one that failed descriptor read or validation, exposed through `IPluginCatalog.GetInvalidPlugins()`. Invalid cards SHALL present diagnostic information and SHALL NOT allow any actions. Valid agent cards, counts, and behaviors SHALL remain unchanged by the presence of invalid entries.

#### Scenario: Invalid entry shown as disabled card

- **WHEN** the CLI Agents page loads and the catalog contains one invalid plugin entry
- **THEN** the page shows a disabled card for the invalid entry below the valid agent cards
- **AND** the card has reduced opacity or muted styling to distinguish it from valid cards

#### Scenario: Validation errors shown as reason

- **WHEN** an invalid entry has validation errors "Missing required field: id" and "Invalid command format"
- **THEN** the card shows reason text "Missing required field: id; Invalid command format" (errors joined with semicolons)

#### Scenario: Read errors shown as reason

- **WHEN** an invalid entry has a read error with exception message "Unexpected character at line 5"
- **THEN** the card shows reason text "Unexpected character at line 5"

#### Scenario: Fallback reason when no error detail

- **WHEN** an invalid entry has no readable plugin and no error message
- **THEN** the card shows reason text including the plugin directory path (e.g., "Plugin directory: C:\\AppData\\CLIHub\\plugins\\broken-agent")

#### Scenario: Invalid cards are visually distinct

- **WHEN** the CLI Agents page shows valid and invalid cards
- **THEN** invalid cards have reduced opacity (e.g., 0.6) or muted text styling
- **AND** invalid cards show an "Invalid" state chip styled with danger coloring

#### Scenario: No actions on invalid cards

- **WHEN** an invalid card is displayed
- **THEN** no action buttons (Detect, Check updates, Update, Edit Config) are shown or enabled
- **AND** the card is non-interactive except for reading the displayed information

#### Scenario: Valid cards unchanged by invalid entries

- **WHEN** the catalog contains both valid and invalid plugins
- **THEN** valid agent cards render with all existing behaviors, action buttons, and styling
- **AND** no valid card is affected by the presence of invalid entries

#### Scenario: Valid counts exclude invalid entries

- **WHEN** the CLI Agents page shows 3 valid agents and 2 invalid entries
- **THEN** the "Supported agents" summary reads "3 supported" (or "3 supported / X not installed" per existing logic)
- **AND** the installed-agent count derives from valid cards only
- **AND** invalid entries do not contribute to either count

#### Scenario: Reload refreshes both valid and invalid sets

- **WHEN** the catalog is reloaded (manual Reload action or `PluginsChanged` event)
- **THEN** valid cards are rebuilt from `GetAllPlugins()` as before
- **AND** invalid cards are rebuilt from `GetInvalidPlugins()`
- **AND** entries that moved from valid to invalid (or vice versa) appear in the correct set after reload

#### Scenario: Invalid card shows name when available

- **WHEN** an invalid entry has a partial plugin with name "BrokenAgent"
- **THEN** the card shows "BrokenAgent" as the display name

#### Scenario: Invalid card shows directory when name unavailable

- **WHEN** an invalid entry has no partial plugin (deserialization failed)
- **THEN** the card shows the directory basename or full path in the reason or as a fallback label

#### Scenario: Default-agent selector excludes invalid entries

- **WHEN** the default-agent dropdown is populated
- **THEN** only valid agents appear in the selector
- **AND** invalid entries are excluded from the dropdown options

#### Scenario: Launch window agent pane excludes invalid entries

- **WHEN** the launch window agent pane is populated
- **THEN** only valid agents appear in the list
- **AND** invalid entries do not appear in the launch selector
