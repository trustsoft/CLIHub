## MODIFIED Requirements

### Requirement: Close on Esc and focus loss

The system SHALL hide the popup when Esc is pressed, and SHALL hide it when it
loses focus unless the pin toggle is enabled.

#### Scenario: Esc

- **WHEN** the popup is visible and the user presses Esc
- **THEN** the popup is hidden

#### Scenario: Focus loss

- **WHEN** the popup is visible, the pin toggle is disabled, and the popup stops
  being the active window
- **THEN** the popup is hidden

#### Scenario: Focus loss while pinned

- **WHEN** the popup is visible, the pin toggle is enabled, and the popup stops
  being the active window
- **THEN** the popup stays visible

#### Scenario: Esc while pinned

- **WHEN** the popup is visible, the pin toggle is enabled, and the user presses
  Esc
- **THEN** the popup is hidden

#### Scenario: Pin default state

- **WHEN** the application starts
- **THEN** the pin toggle is disabled

#### Scenario: Pin persists across shows

- **WHEN** the pin toggle is enabled, the popup is hidden, and the popup is
  shown again by the hotkey
- **THEN** the pin toggle stays enabled