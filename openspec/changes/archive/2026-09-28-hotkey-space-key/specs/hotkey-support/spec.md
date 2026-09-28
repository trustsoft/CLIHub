## ADDED Requirements

### Requirement: Space key support

The system SHALL accept the Space key in a hotkey combination, in addition to letters, digits, and function keys.

#### Scenario: Space combined with a modifier
- **WHEN** a hotkey combines at least one modifier with the Space key (for example `Ctrl+Alt+Space`)
- **THEN** the combination is accepted as a valid hotkey

#### Scenario: Space round-trips through formatting
- **WHEN** a combination that includes the Space key is formatted back to text
- **THEN** the Space key is written as `Space` and the result parses back to the same combination
