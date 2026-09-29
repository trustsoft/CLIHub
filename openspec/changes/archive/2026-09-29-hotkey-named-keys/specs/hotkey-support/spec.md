## ADDED Requirements

### Requirement: Named key support

The system SHALL accept a defined set of named, non-character keys in a hotkey combination, in addition to letters, digits, and function keys. The recognized canonical names SHALL be `Enter`, `Tab`, `Escape`, `Backspace`, `Delete`, `Insert`, `Home`, `End`, `PageUp`, `PageDown`, `Up`, `Down`, `Left`, `Right`, and `Space`, each mapped to its Windows virtual key. The system SHALL also accept the aliases `Return` (Enter), `Esc` (Escape), `Back` (Backspace), `Del` (Delete), `Ins` (Insert), `PgUp` (PageUp), and `PgDn` (PageDown). Name matching SHALL be case-insensitive.

#### Scenario: Named key combined with a modifier
- **WHEN** a hotkey combines at least one modifier with a recognized named key (for example `Ctrl+Enter` or `Ctrl+Alt+Space`)
- **THEN** the combination is accepted as a valid hotkey

#### Scenario: Named key aliases
- **WHEN** a hotkey uses an alias such as `Return`, `Esc`, `Back`, `Del`, `Ins`, `PgUp`, or `PgDn`
- **THEN** it is accepted and resolves to the same key as the canonical name

#### Scenario: Named key round-trips through formatting
- **WHEN** a combination that includes a named key is formatted back to text
- **THEN** the key is written using its canonical name and the result parses back to the same combination

#### Scenario: Unknown key is rejected
- **WHEN** a hotkey contains a token that is neither a modifier, a letter, a digit, a function key, nor a recognized named key
- **THEN** the combination is rejected

## REMOVED Requirements

### Requirement: Space key support

**Reason**: Superseded by the generalized "Named key support" requirement, which covers Space along with the other named keys and aliases.

**Migration**: No user action required; existing `Space` hotkeys parse and format exactly as before under the new requirement.
