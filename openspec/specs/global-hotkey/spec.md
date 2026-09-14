# global-hotkey Specification

## Purpose

Allows invoking the CLIHub popup from anywhere in the system with a single key
combination, without switching to the tray icon or a separate window.

## Requirements

### Requirement: Register global hotkey

The system SHALL register a global hotkey on application startup. The combination
comes from `config.json`, defaulting to `Ctrl+Alt+Space`.

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
