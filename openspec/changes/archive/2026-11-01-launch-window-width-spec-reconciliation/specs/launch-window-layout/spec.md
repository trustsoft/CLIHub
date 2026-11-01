# launch-window-layout (partial): width spec reconciliation

## MODIFIED Requirements

### Requirement: Window chrome

The launch window SHALL be presented without operating-system window chrome, delineated instead by its own border and shape.

#### Scenario: No title bar or frame controls
- **WHEN** the launch window is shown
- **THEN** it shows no OS title bar, no window icon, and no minimize, maximize, or close buttons

#### Scenario: Window outline
- **WHEN** the launch window is shown
- **THEN** its outer edge is drawn with a 1px stroke and its top edge carries a 1px inset highlight, so the window boundary stays visible against dark backgrounds

#### Scenario: Rounded corners
- **WHEN** the launch window is shown on Windows 11
- **THEN** its corners are rounded; on an operating system that does not support the rounding preference, the corners stay square without visual artifacts or errors

#### Scenario: Fixed width and content height
- **WHEN** the panes' content changes
- **THEN** the window height follows its content, and the window's size is never settable by the user — there is no OS resize border and `ResizeMode` is `NoResize`

#### Scenario: Window cannot be moved or resized by dragging
- **WHEN** the user drags anywhere on the window surface
- **THEN** the window is neither moved nor resized
