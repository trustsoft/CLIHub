## MODIFIED Requirements

### Requirement: Accent color preference

The system SHALL offer an application accent color preference with the presets Sky (`#4CC2FF`), Blue
(`#3B82F6`), Teal (`#0D9488`), Emerald (`#10B981`), Purple (`#8B5CF6`), Amber (`#F59E0B`), Indigo
(`#818CF8`), and Tangerine (`#EA580C`), and SHALL accept a custom `#RRGGBB` value, defaulting to Sky.

#### Scenario: Default accent
- **WHEN** no accent has been stored
- **THEN** the Sky accent is used

#### Scenario: Preset selected
- **WHEN** the user selects a preset accent
- **THEN** that accent is applied

#### Scenario: Custom accent accepted
- **WHEN** the user enters a valid `#RRGGBB` value
- **THEN** that accent is applied

#### Scenario: Invalid custom value rejected
- **WHEN** the user enters a value that is not a valid color
- **THEN** the value is rejected and an error is shown and the window does not save
