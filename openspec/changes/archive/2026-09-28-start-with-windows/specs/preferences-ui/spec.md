## MODIFIED Requirements

### Requirement: Settings sections

The window SHALL present the sections "Startup", "Default runtime", "Global hotkey", "Agents probe", and "Updates", and SHALL show the current application version.

#### Scenario: Sections shown
- **WHEN** the Settings window opens
- **THEN** it shows "Start with Windows" and "Show window on startup" toggles, a default-runtime selector (`cmd` / `ps` / `wt`), a global-hotkey capture field, agents-probe `TTL, minutes` and `Timeout, seconds` fields, and a "Check for updates on startup" toggle

#### Scenario: Current values loaded
- **WHEN** the Settings window opens
- **THEN** each control shows the value currently stored in configuration (and, for "Start with Windows", the current Windows registration state)

#### Scenario: Version shown
- **WHEN** the Settings window opens
- **THEN** the footer shows the current application version

### Requirement: Save applies preferences

Saving SHALL persist the preferences and apply them without restarting the application.

#### Scenario: Persist on save
- **WHEN** the user changes one or more settings and chooses "Save"
- **THEN** the values are written to `config.json` and the window closes

#### Scenario: Runtime applied
- **WHEN** the user saves a different default runtime
- **THEN** subsequent agent launches use the newly selected runtime

#### Scenario: Hotkey applied immediately
- **WHEN** the user saves a different global hotkey
- **THEN** the previous hotkey is unregistered and the new one is registered without restarting the application

#### Scenario: Startup update check applied
- **WHEN** the user disables "Check for updates on startup" and saves
- **THEN** subsequent application starts do not perform the automatic update check

#### Scenario: Autostart applied
- **WHEN** the user toggles "Start with Windows" and saves
- **THEN** the per-user Run registration is created or removed to match the toggle

#### Scenario: Startup window visibility applied
- **WHEN** the user changes "Show window on startup" and saves
- **THEN** the next application start shows or hides the main window accordingly
