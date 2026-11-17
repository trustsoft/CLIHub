## ADDED Requirements

### Requirement: Context menus follow the Windows apps theme

WPF context menus — the tray menu and TextBox context menus — SHALL render with application-level
menu styles whose palette follows the Windows apps-use-light-theme setting, the same value the
"System" theme mode reads. The menu palette SHALL be independent of the stored application theme
and the applied CLIHub palette, SHALL NOT use the application accent color, and SHALL NOT change
menu structure, labels, icons, or commands.

#### Scenario: Windows dark mode uses the dark menu palette

- **WHEN** the Windows apps-use-light-theme setting is off and a context menu is shown
- **THEN** the menu renders with the dark system-menu palette (dark surface, border, text, and
  overlay tones)

#### Scenario: Windows light mode uses the light menu palette

- **WHEN** the Windows apps-use-light-theme setting is on and a context menu is shown
- **THEN** the menu renders with the light system-menu palette (near-white surface, dark text)

#### Scenario: Menu palette is independent of the CLIHub palette

- **WHEN** the stored application theme is "Dark", "Light", or "Blue" and differs from the Windows
  apps mode
- **THEN** the menu still follows the Windows apps mode, not the stored theme or the applied CLIHub
  palette, and no accent color appears in the menu

### Requirement: Context menu palette applies live

The menu palette matching the current Windows apps-use-light-theme setting SHALL be applied at
startup before any menu can open and SHALL be re-applied without a restart when that setting
changes during the session, regardless of the stored application theme. The follow SHALL exist
only for the running first-instance session and SHALL be removed when the application shuts down.

#### Scenario: Live follow regardless of the stored theme

- **WHEN** the Windows apps-use-light-theme setting changes while the application runs, with any
  stored application theme
- **THEN** context menus opened afterwards render with the re-applied menu palette without a
  restart
- **AND** the application window palette changes only when the stored theme is "System"

#### Scenario: Menu palette applied before the first menu opens

- **WHEN** the application starts
- **THEN** the menu palette matching the current Windows apps mode is applied before any menu can
  be opened

#### Scenario: Menu follow ends at shutdown

- **WHEN** the application shuts down
- **THEN** the menu-palette personalization subscription is removed so no menu palette is
  re-applied afterwards
