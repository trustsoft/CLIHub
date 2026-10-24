# application-theme Specification (Delta)

## MODIFIED Requirements

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
