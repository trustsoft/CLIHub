# application-theme Specification

## Purpose
Selects and resolves the application-wide visual theme so every window renders with a consistent light or dark palette that can follow the Windows personalization setting.

## Requirements

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

### Requirement: Accent applies live

Saving a new accent SHALL update the application accent immediately, without a restart, and SHALL persist the value for later starts.

#### Scenario: Live application on save
- **WHEN** the user selects an accent and chooses "Save"
- **THEN** the accent-bearing controls update immediately and the value is written to `config.json`

#### Scenario: Applied on next start
- **WHEN** the application starts with a stored accent
- **THEN** the accent is applied before the windows render

### Requirement: Theme applies immediately

Changing the theme preference SHALL apply the matching palette to the running application when the user
saves settings, without a restart, and SHALL persist the choice so the next start resolves the same
theme. The dark palette remains the base palette: a saved Dark theme or a theme that resolves to no
palette SHALL leave that base in place, and a palette that cannot be loaded SHALL keep the palette
already in use, so a theme is never partially applied. The "System" mode SHALL resolve to the dark or
light palette at the moment the theme is applied and SHALL never resolve to the blue palette. While the
stored theme is "System", the application SHALL follow the Windows apps-use-light-theme setting for the
rest of the session: when that setting changes while the application runs, the effective theme SHALL be
re-resolved and the matching palette reapplied through the same palette-switching path used at startup
and on save, without a restart, and the blue palette SHALL never be applied by "System". A Windows
personalization change SHALL leave the palette untouched when the stored theme is "Dark", "Light", or
"Blue". The personalization subscription SHALL exist only for the running first-instance session and
SHALL be removed when the application shuts down.

#### Scenario: Theme change applies on save

- **WHEN** the user selects a theme and saves settings
- **THEN** every open window renders with the selected palette without a restart
- **AND** the value is written to `config.json`

#### Scenario: Theme persists for the next start

- **WHEN** a theme has been saved and the application is restarted
- **THEN** the saved theme is applied before any window is created

#### Scenario: System resolves at save time

- **WHEN** the user saves the "System" theme
- **THEN** the applied palette is light when Windows reports the light apps mode and dark otherwise
- **AND** the blue palette is never applied by "System"

#### Scenario: Cancel keeps the saved theme

- **WHEN** the user changes the theme and cancels the Settings window
- **THEN** the running palette and the stored theme remain those of the last save

#### Scenario: Unresolvable palette keeps the current palette

- **WHEN** the palette for the effective theme cannot be loaded
- **THEN** the palette already in use stays in place and the failure is logged
- **AND** the stored theme preference is kept

#### Scenario: System follows a personalization change while running

- **WHEN** the stored theme is "System" and the Windows apps-use-light-theme setting changes while the
  application runs
- **THEN** every open window repaints with the re-resolved dark or light palette without a restart
- **AND** the reapply uses the same palette-switching path as startup and a Settings save
- **AND** the blue palette is never applied by "System"

#### Scenario: Personalization change with a fixed theme

- **WHEN** the stored theme is "Dark", "Light", or "Blue" and the Windows apps-use-light-theme setting
  changes while the application runs
- **THEN** the palette in use stays untouched

#### Scenario: Follow ends at shutdown

- **WHEN** the application shuts down
- **THEN** the personalization subscription is removed so no palette is reapplied afterwards

### Requirement: Context menus follow the Windows apps theme

WPF context menus — the tray menu and TextBox context menus — SHALL render with application-level
menu styles whose palette follows the Windows apps-use-light-theme setting, the same value the
"System" theme mode reads. The menu palette SHALL be independent of the stored application theme
and the applied CLIHub palette, SHALL NOT use the application accent color, and SHALL NOT change
menu structure, labels, icons, or commands.

#### Scenario: Windows dark mode uses the dark menu palette

- **WHEN** the Windows apps-use-light-theme setting is off and a context menu is shown
- **THEN** the menu renders with the dark system-menu palette (dark surface, border, text, and
  overlay tones)

#### Scenario: Windows light mode uses the light menu palette

- **WHEN** the Windows apps-use-light-theme setting is on and a context menu is shown
- **THEN** the menu renders with the light system-menu palette (near-white surface, dark text)

#### Scenario: Menu palette is independent of the CLIHub palette

- **WHEN** the stored application theme is "Dark", "Light", or "Blue" and differs from the Windows
  apps mode
- **THEN** the menu still follows the Windows apps mode, not the stored theme or the applied CLIHub
  palette, and no accent color appears in the menu

### Requirement: Context menu palette applies live

The menu palette matching the current Windows apps-use-light-theme setting SHALL be applied at
startup before any menu can open and SHALL be re-applied without a restart when that setting
changes during the session, regardless of the stored application theme. The follow SHALL exist
only for the running first-instance session and SHALL be removed when the application shuts down.

#### Scenario: Live follow regardless of the stored theme

- **WHEN** the Windows apps-use-light-theme setting changes while the application runs, with any
  stored application theme
- **THEN** context menus opened afterwards render with the re-applied menu palette without a
  restart
- **AND** the application window palette changes only when the stored theme is "System"

#### Scenario: Menu palette applied before the first menu opens

- **WHEN** the application starts
- **THEN** the menu palette matching the current Windows apps mode is applied before any menu can
  be opened

#### Scenario: Menu follow ends at shutdown

- **WHEN** the application shuts down
- **THEN** the menu-palette personalization subscription is removed so no menu palette is
  re-applied afterwards
