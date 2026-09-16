# global-hotkey Delta

## ADDED Requirements

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
