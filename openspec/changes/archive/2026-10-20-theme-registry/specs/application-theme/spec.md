# application-theme (partial): theme registry with a blue palette

## MODIFIED Requirements

### Requirement: Theme mode preference

The system SHALL offer an application theme preference with the values "Dark", "Light", "Blue", and
"System", defaulting to "Dark".

#### Scenario: Default theme
- **WHEN** no theme preference has been stored
- **THEN** the dark theme is used

#### Scenario: Known theme stored
- **WHEN** the stored theme is "Dark", "Light", "Blue", or "System"
- **THEN** that theme mode is used

#### Scenario: Unknown theme falls back to dark
- **WHEN** the stored theme is missing or unrecognized
- **THEN** the dark theme is used and the application starts normally

### Requirement: Theme palette resolution

The application SHALL resolve the effective theme before any window is created and merge the
palette registered for that theme into the application resources so all windows render
consistently. The dark palette is the base palette; an unknown or missing theme keeps the base.
Each palette SHALL define the same set of brush keys, so every window renders completely under
every theme. The "System" mode SHALL follow the Windows personalization setting and SHALL resolve
only to the dark or light palette, never to the blue palette.

#### Scenario: Dark theme
- **WHEN** the effective theme is dark
- **THEN** the dark palette is applied to the application resources

#### Scenario: Light theme
- **WHEN** the effective theme is light
- **THEN** the light palette is applied to the application resources

#### Scenario: Blue theme
- **WHEN** the theme preference is "Blue"
- **THEN** the blue palette — blue-tinted dark chrome surfaces with a baked-in blue accent — is
  applied to the application resources

#### Scenario: System follows Windows
- **WHEN** the theme preference is "System"
- **THEN** the effective theme matches the Windows apps-use-light-theme setting, resolving to the
  dark or light palette only

#### Scenario: Shared palette keys
- **WHEN** any registered palette is compared with the other registered palettes
- **THEN** all palettes define the same set of brush keys

### Requirement: Accent color preference

The system SHALL offer an application accent color preference with the presets Sky (`#4CC2FF`), Blue
(`#3B82F6`), Teal (`#0D9488`), Emerald (`#10B981`), Purple (`#8B5CF6`), Amber (`#F59E0B`), Indigo
(`#818CF8`), and Tangerine (`#EA580C`), and SHALL accept a custom `#RRGGBB` value, defaulting to Sky.
Each theme palette bakes in its own default accent — Sky on the dark and light palettes, Blue on the
blue palette — and a stored accent other than the palette's default SHALL override the baked accent.

#### Scenario: Default accent
- **WHEN** no accent has been stored
- **THEN** the active palette's baked-in accent is used: Sky on the dark and light palettes, Blue on
  the blue palette

#### Scenario: Preset selected
- **WHEN** the user selects a preset accent
- **THEN** that accent is applied

#### Scenario: Custom accent accepted
- **WHEN** the user enters a valid `#RRGGBB` value
- **THEN** that accent is applied

#### Scenario: Invalid custom value rejected
- **WHEN** the user enters a value that is not a valid color
- **THEN** the value is rejected and an error is shown and the window does not save
