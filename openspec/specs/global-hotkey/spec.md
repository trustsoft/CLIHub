# global-hotkey Specification

## Purpose

Allows invoking the CLIHub popup from anywhere in the system with a single key
combination, without switching to the tray icon or a separate window.

## Requirements

### Requirement: Register global hotkey

The system SHALL register a global hotkey on application startup. The combination
comes from `settings.json`, defaulting to `Ctrl+Alt+Space`.

#### Scenario: Successful registration

- **WHEN** the application starts and the combination is not taken by another application
- **THEN** the hotkey is registered and fires while the application runs

#### Scenario: Combination already taken

- **WHEN** the combination is already registered by another application
- **THEN** registration does not happen, the application reports the problem and keeps running without the hotkey

### Requirement: Open popup via hotkey

The system SHALL open the popup when the registered combination is pressed.

#### Scenario: Hotkey pressed

- **WHEN** the user presses the registered combination
- **THEN** the popup is shown in hotkey mode

### Requirement: Change the hotkey at runtime

The system SHALL re-register the global hotkey when a new combination is
saved in the settings window, so the popup opens via the saved combination
without restarting the application. When the new combination cannot be
registered because another application already uses it, the system SHALL keep
the previous combination registered and stored, save the other edited values,
and report the conflict.

#### Scenario: Hotkey changed successfully

- **WHEN** the user saves a new combination that can be registered
- **THEN** the popup opens via the new combination and no longer via the
  previous one

#### Scenario: New combination is taken

- **WHEN** the user saves a combination that another application has already
  registered
- **THEN** the previous combination remains registered and stored, the other
  saved values are written, and the conflict is reported
