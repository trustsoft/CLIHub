## MODIFIED Requirements

### Requirement: Dark settings window chrome

The Settings window SHALL be rendered with the application's shared theme palette (the dark palette by default, the light palette when the effective application theme is light) and drawn chrome instead of the system window: no OS title bar, a title bar showing the application icon and the "CLIHub - Settings" title, drawn maximize/restore and close buttons, header drag, rounded corners, and Escape closing the window. The title bar SHALL NOT show a version pill or a minimize button.

#### Scenario: Themed surfaces
- **WHEN** the Settings window is shown
- **THEN** the window background, header, content area, and footer use the palette shared with the launch window for the active theme

#### Scenario: Title bar shows identity
- **WHEN** the Settings window is shown
- **THEN** its title bar shows the application icon and the "CLIHub - Settings" title, and no version pill or minimize button

#### Scenario: Drawn header works
- **WHEN** the user drags the drawn header
- **THEN** the window moves with the drag

#### Scenario: Maximize toggles window state
- **WHEN** the user activates the maximize button
- **THEN** the window is maximized and the button becomes a restore button; activating it again restores the previous size

#### Scenario: Maximized window fits the work area
- **WHEN** the window is maximized
- **THEN** it fills the monitor's work area only, so the footer stays visible above the taskbar

#### Scenario: Close button closes
- **WHEN** the user activates the close button
- **THEN** the window closes with no changes saved

#### Scenario: Escape closes
- **WHEN** the user presses Escape in the Settings window
- **THEN** the window closes with no changes saved
