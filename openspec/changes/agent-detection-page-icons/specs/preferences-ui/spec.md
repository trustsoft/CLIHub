# Spec Delta

## MODIFIED Requirements

### Requirement: Agent Detection page content

The Agent Detection page SHALL present the agent detection cache TTL and version check timeout fields under a "DETECTION" section header with a "Reset" action that clears both fields to empty (built-in defaults) in one action, and SHALL show three page-header summary cards reporting the effective cache TTL and timeout values (distinguishing built-in defaults from custom values) and the built-in default values.

#### Scenario: Page header summary cards shown

- **WHEN** the Settings window opens on "Agent Detection"
- **THEN** the page header shows three summary cards: CACHE TTL (effective value with unit "min" and a detail line), TIMEOUT (effective value with unit "sec" and a detail line), and DEFAULTS (built-in default values with a detail line)

#### Scenario: Summary card detail distinguishes defaults from custom values

- **WHEN** a detection field is empty (using the built-in default)
- **THEN** the corresponding summary card's detail line indicates "Built-in default" or shows the default numeric value
- **WHEN** a detection field holds a custom value
- **THEN** the corresponding summary card's detail line indicates "Custom" or distinguishes the custom value from the default

#### Scenario: Detection fields shown

- **WHEN** the Agent Detection page is shown
- **THEN** the page body shows a "DETECTION" section header followed by the "Cache TTL" field (with unit "min" and a placeholder showing the built-in default) and the "Version check timeout" field (with unit "sec" and a placeholder showing the built-in default)

#### Scenario: Fields accept valid numeric input

- **WHEN** the user enters a positive whole number in either detection field
- **THEN** the value is accepted and shown in the field

#### Scenario: Empty fields use defaults

- **WHEN** either detection field is left empty and saved
- **THEN** the default for that field is used

#### Scenario: Invalid number rejected

- **WHEN** a detection field contains a non-numeric or non-positive value
- **THEN** the value is rejected and an error is shown and the window does not save

#### Scenario: Current values loaded

- **WHEN** the Agent Detection page opens
- **THEN** the fields show the stored custom values or are empty (placeholder visible) when using defaults
- **AND** the summary cards show the effective values (custom or default) with appropriate detail

#### Scenario: Icon and description

- **WHEN** the Agent Detection page is selected in the sidebar
- **THEN** the sidebar item shows the Radar icon (IconGlyphs.Radar, EB44) and the page description "Caching and timeout behavior for agent detection."

#### Scenario: DEFAULTS card shown

- **WHEN** the Agent Detection page header is visible
- **THEN** the third summary card is labelled "DEFAULTS" with the UpdateRestore icon (IconGlyphs.UpdateRestore, E777) and shows the built-in default values for both detection parameters

#### Scenario: DEFAULTS card value

- **WHEN** the DEFAULTS card is visible
- **THEN** the value line shows the built-in defaults formatted as "N min · M sec" (e.g., "15 min · 10 sec") where N is the TTL default and M is the timeout default

#### Scenario: DEFAULTS card detail

- **WHEN** the DEFAULTS card is visible
- **THEN** the detail line shows "Cache TTL · Version timeout"

#### Scenario: Reset action shown

- **WHEN** the Agent Detection page body is visible
- **THEN** the "DETECTION" section header has a right-aligned "Reset" chip button with the Refresh icon

#### Scenario: Reset action disabled at defaults

- **WHEN** both detection fields are empty (using built-in defaults)
- **THEN** the Reset button is disabled (dimmed)

#### Scenario: Reset action enabled with custom values

- **WHEN** either detection field contains a custom value (non-empty)
- **THEN** the Reset button is enabled

#### Scenario: Reset action clears both fields

- **WHEN** the user clicks the Reset button with custom values in either or both fields
- **THEN** both fields are cleared to empty (placeholders/watermarks visible)
- **AND** the summary cards update to show "Built-in default" or equivalent
- **AND** the Reset button becomes disabled

#### Scenario: Reset edits the draft

- **WHEN** the user clicks Reset and then clicks "Cancel"
- **THEN** the Settings window closes and the stored custom values remain unchanged (Reset edits the draft only; Cancel discards the draft)

#### Scenario: Reset button tooltip

- **WHEN** the user hovers over the Reset button
- **THEN** a tooltip shows "Restore the built-in defaults" or equivalent
