## MODIFIED Requirements

### Requirement: Agent Detection page summary cards

The Settings window SHALL show three summary cards under the "Agent Detection" page header: cache TTL, timeout, and defaults. The cache TTL card SHALL show the effective detection cache time-to-live in minutes with the "min" unit, resolving an empty field value to the built-in default, and SHALL state whether the value is a built-in default or custom with the default numeric value for comparison. The timeout card SHALL show the effective version-check timeout in seconds with the "sec" unit, resolving an empty field value to the built-in default, and SHALL state whether the value is a built-in default or custom with the default numeric value for comparison. The defaults card SHALL show the built-in default values for both detection parameters in the format "N min · M sec" (e.g., "15 min · 10 sec") and SHALL name the two detection parameters those default values apply to. All three cards SHALL use the Diagnostic icon. The cache TTL and timeout cards SHALL refresh when either detection field is edited.

#### Scenario: Cards shown

- **WHEN** the Agent Detection page is shown
- **THEN** the page header shows the "CACHE TTL", "TIMEOUT", and "DEFAULTS" cards using the Diagnostic icon

#### Scenario: Cache TTL card shows the effective value

- **WHEN** the Agent Detection page is shown
- **THEN** the cache TTL card shows the effective cache time-to-live with the "min" unit
- **AND** the card resolves an empty field value to the built-in default

#### Scenario: Timeout card shows the effective value

- **WHEN** the Agent Detection page is shown
- **THEN** the timeout card shows the effective version-check timeout with the "sec" unit
- **AND** the card resolves an empty field value to the built-in default

#### Scenario: Built-in default detail

- **WHEN** either detection field is empty (using the built-in default)
- **THEN** the corresponding card's detail line states "Built-in default"

#### Scenario: Custom value detail

- **WHEN** either detection field contains a positive custom value
- **THEN** the corresponding card's detail line states "Custom (default: N min|sec)" with the built-in default numeric value for comparison

#### Scenario: Cards follow live changes

- **WHEN** a detection field is edited
- **THEN** the corresponding summary card refreshes without reopening the window

#### Scenario: Defaults card shown

- **WHEN** the Agent Detection page is shown
- **THEN** the defaults card uses the Diagnostic icon and shows the built-in default values for both detection parameters

#### Scenario: Defaults card value

- **WHEN** the defaults card is visible
- **THEN** the value line shows the built-in defaults formatted as "N min · M sec" (e.g., "15 min · 10 sec") where N is the TTL default and M is the timeout default

#### Scenario: Defaults card detail

- **WHEN** the defaults card is visible
- **THEN** the detail line shows "Cache TTL · Checking timeout"
