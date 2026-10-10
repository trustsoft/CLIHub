## Purpose

Selects and resolves the application-wide visual theme so every window renders with a consistent light or dark palette that can follow the Windows personalization setting.

## ADDED Requirements

### Requirement: Theme mode preference

The system SHALL offer an application theme preference with the values "Dark", "Light", and "System", defaulting to "Dark".

#### Scenario: Default theme
- **WHEN** no theme preference has been stored
- **THEN** the dark theme is used

#### Scenario: Known theme stored
- **WHEN** the stored theme is "Dark", "Light", or "System"
- **THEN** that theme mode is used

#### Scenario: Unknown theme falls back to dark
- **WHEN** the stored theme is missing or unrecognized
- **THEN** the dark theme is used and the application starts normally

### Requirement: Theme palette resolution

The application SHALL resolve the effective theme before any window is created and merge the corresponding palette (dark or light) so all windows render consistently. The "System" mode SHALL follow the Windows personalization setting.

#### Scenario: Dark theme
- **WHEN** the effective theme is dark
- **THEN** the dark palette is applied to the application resources

#### Scenario: Light theme
- **WHEN** the effective theme is light
- **THEN** the light palette is applied to the application resources

#### Scenario: System follows Windows
- **WHEN** the theme preference is "System"
- **THEN** the effective theme matches the Windows apps-use-light-theme setting

### Requirement: Theme applies on next start

Changing the theme preference SHALL take effect on the next application start.

#### Scenario: Change persists for next start
- **WHEN** the user selects a theme and saves
- **THEN** the value is written to `config.json` and the theme is applied on the next start
