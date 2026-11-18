## ADDED Requirements

### Requirement: Tray menu visual theme

The tray context menu SHALL render with the application's themed context-menu styles in both
Windows apps light and dark modes. The visual change SHALL NOT alter the menu's structure, labels,
glyphs, separator rules, commands, or refresh-on-open behavior specified by the other requirements
of this capability.

#### Scenario: Themed rendering in Windows dark mode

- **WHEN** the tray menu is shown while Windows apps use the dark theme
- **THEN** it renders with the dark themed menu styles, and the project and Updates submenus render
  with the same styling

#### Scenario: Themed rendering in Windows light mode

- **WHEN** the tray menu is shown while Windows apps use the light theme
- **THEN** it renders with the light themed menu styles, and the project and Updates submenus render
  with the same styling

#### Scenario: Visual change only

- **WHEN** the tray menu is rendered with the themed styles
- **THEN** the item order, headers, icons, separator placement, enabled/disabled states, and command
  behavior remain exactly as specified by the other requirements of this capability

#### Scenario: Themed menu details

- **WHEN** the tray menu is rendered with the themed styles in either Windows mode
- **THEN** it shows a rounded card surface with a one-pixel border and a subtle shadow, hover and
  pressed highlights on entries, dimmed disabled entries, a visible submenu arrow, an icon slot
  whose glyphs follow the entry's text color, and themed separators
