# application-theme Specification Delta

## REMOVED Requirements

### Requirement: Theme applies on next start

**Reason**: The requirement deferred a theme change to the next application start. The theme now
applies to the running application when the user saves, so the deferral no longer holds and the
requirement name would contradict its own behavior.

**Migration**: The immediate-application behavior is specified by the added requirement "Theme applies
immediately", which keeps the persistence guarantee of the removed requirement. The palette resolution
rules in "Theme palette resolution" are unchanged.

## ADDED Requirements

### Requirement: Theme applies immediately

Changing the theme preference SHALL apply the matching palette to the running application when the user
saves settings, without a restart, and SHALL persist the choice so the next start resolves the same
theme. The dark palette remains the base palette: a saved Dark theme or a theme that resolves to no
palette SHALL leave that base in place, and a palette that cannot be loaded SHALL keep the palette
already in use, so a theme is never partially applied. The "System" mode SHALL resolve to the dark or
light palette at the moment the theme is applied and SHALL never resolve to the blue palette.

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
