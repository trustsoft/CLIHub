# application-theme Specification (Delta)

## MODIFIED Requirements

### Requirement: Theme palette resolution

The application SHALL resolve the effective theme before any window is created and merge the
palette registered for that theme into the application resources so all windows render
consistently. The dark palette is the base palette; an unknown or missing theme keeps the base.
Each palette SHALL define the same set of brush keys, so every window renders completely under
every theme. Palettes SHALL define chrome and surface brushes only; the accent is application-scoped
and supplied at runtime, so no palette defines an accent key. The "System" mode SHALL follow the
Windows personalization setting and SHALL resolve only to the dark or light palette, never to the
blue palette.

#### Scenario: Dark theme
- **WHEN** the effective theme is dark
- **THEN** the dark palette is applied to the application resources

#### Scenario: Light theme
- **WHEN** the effective theme is light
- **THEN** the light palette is applied to the application resources

#### Scenario: Blue theme
- **WHEN** the theme preference is "Blue"
- **THEN** the blue palette — blue-tinted dark chrome surfaces — is applied to the application resources

#### Scenario: System follows Windows
- **WHEN** the theme preference is "System"
- **THEN** the effective theme matches the Windows apps-use-light-theme setting, resolving to the
  dark or light palette only

#### Scenario: Shared palette keys
- **WHEN** any registered palette is compared with the other registered palettes
- **THEN** all palettes define the same set of brush keys and no palette defines an accent key

### Requirement: Accent color preference

The system SHALL offer an application accent color preference with the presets Sky (`#4CC2FF`),
Blue (`#3B82F6`), Teal (`#0D9488`), Emerald (`#10B981`), Purple (`#8B5CF6`), Amber (`#F59E0B`),
Indigo (`#818CF8`), and Tangerine (`#EA580C`), and SHALL accept a custom `#RRGGBB` value, defaulting
to Sky. The accent SHALL be application-scoped: one color for the whole application, regardless of
the active theme. Theme palettes SHALL NOT define accent keys; the accent SHALL be supplied by the
application at runtime and SHALL be independent of the theme preference. The stored value SHALL be
normalized to uppercase `#RRGGBB`; a stored value that cannot be normalized SHALL fall back to Sky,
while invalid user input SHALL be rejected before it is saved.

#### Scenario: Default accent
- **WHEN** no accent has been stored, or the stored value cannot be normalized
- **THEN** the Sky accent `#4CC2FF` is applied, regardless of the active theme

#### Scenario: Preset selected
- **WHEN** the user selects a preset accent
- **THEN** that accent is applied

#### Scenario: Custom accent accepted
- **WHEN** the user enters a valid `#RRGGBB` value
- **THEN** that accent is applied and stored as uppercase `#RRGGBB`

#### Scenario: Invalid custom value rejected
- **WHEN** the user enters a value that is not a valid color
- **THEN** the value is rejected and an error is shown and the window does not save

#### Scenario: Theme change preserves accent
- **WHEN** the user changes the theme preference
- **THEN** the stored accent remains unchanged and continues to apply
- **AND** the new theme renders with the stored accent, not a palette-baked accent
