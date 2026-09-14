# popup Specification

## Purpose

Provides the popup surface: a window that on hotkey shows projects and the agents
of the selected project, positions itself at the cursor, and lives as a reusable
instance.

## Requirements

### Requirement: Application host in the tray

The system SHALL keep the application running through a system tray icon with an
"Exit" item. The passive popup mode triggered by clicking the icon is out of scope
for this change.

#### Scenario: Application start

- **WHEN** the application starts
- **THEN** a CLIHub icon appears in the system tray

#### Scenario: Exit

- **WHEN** the user selects "Exit" in the tray icon menu
- **THEN** the application terminates

### Requirement: Reusable window instance (warm singleton)

The system SHALL create the popup window once and reuse it. Hiding the window
SHALL call Hide, not Close.

#### Scenario: Reopening

- **WHEN** the popup was hidden and is invoked again
- **THEN** the same window instance is shown without being recreated

#### Scenario: Hiding preserves the window

- **WHEN** the popup is closed
- **THEN** the window is hidden and stays ready to be shown

### Requirement: Show at cursor on hotkey

The system SHALL show the popup in hotkey mode on the screen where the cursor is
located and give it focus.

#### Scenario: Show at cursor

- **WHEN** the hotkey fires
- **THEN** the popup is shown on the screen with the cursor and receives focus

### Requirement: Keep popup within the working area

The system SHALL position the popup so that it stays entirely within the working
area (WorkingArea) of the screen with the cursor, shifting it when space is short.

#### Scenario: Not enough room at the cursor

- **WHEN** the popup does not fit next to the cursor
- **THEN** the popup is repositioned and stays entirely within the WorkingArea

### Requirement: Correct coordinates across DPI

The system SHALL convert on-screen pixel coordinates to DIP when positioning the
window.

#### Scenario: Scaled screen

- **WHEN** the screen has a scale other than 100%
- **THEN** the popup is positioned at the cursor without offset

### Requirement: Close on Esc and focus loss

The system SHALL hide the popup when Esc is pressed and when it loses focus.

#### Scenario: Esc

- **WHEN** the popup is visible and the user presses Esc
- **THEN** the popup is hidden

#### Scenario: Focus loss

- **WHEN** the popup is visible and stops being the active window
- **THEN** the popup is hidden
