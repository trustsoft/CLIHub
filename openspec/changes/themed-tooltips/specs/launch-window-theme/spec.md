# Spec Delta

## ADDED Requirements

### Requirement: Themed tooltips

Tooltips shown over controls in the launch window SHALL use the dark popover style consistent with the window's chrome, rather than the system light tooltip style.

#### Scenario: Tooltip matches the dark chrome
- **WHEN** the pointer rests on a control in the launch window that shows a tooltip, such as an agent action button or a footer button
- **THEN** the tooltip appears as a dark popover consistent with the window's other popovers: dark background, light text, rounded corners

#### Scenario: Tooltip text stays readable
- **WHEN** a themed tooltip is shown
- **THEN** its text is light on the dark surface and fully readable, matching the window's primary text tone

#### Scenario: Tooltip timing unchanged
- **WHEN** the pointer rests on a control with a tooltip
- **THEN** the tooltip appears and disappears with the platform's default timing and placement behavior
