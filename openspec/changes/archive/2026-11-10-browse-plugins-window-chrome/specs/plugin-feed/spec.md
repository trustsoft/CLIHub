## ADDED Requirements

### Requirement: Browse Plugins dialog drawn chrome

The Browse Plugins dialog SHALL provide drawn window chrome with header drag to move the window, user resizing with 6px resize borders and enforced minimum size (520x360), rounded corners, and Escape-to-close. The dialog SHALL remain modal, owned by the launch window, and not shown in the taskbar. The header SHALL contain only the close button (no maximize/restore or minimize buttons).

#### Scenario: Header drag moves the window
- **WHEN** the user drags the dialog's header (the area containing the app icon, "Browse Plugins" title, and close button)
- **THEN** the window moves with the drag

#### Scenario: User can resize the dialog
- **WHEN** the user drags any edge or corner of the dialog
- **THEN** the window is resized
- **AND** the window cannot be resized below MinWidth 520 / MinHeight 360

#### Scenario: Rounded corners on Windows 11
- **WHEN** the dialog is shown on Windows 11
- **THEN** the window has rounded corners

#### Scenario: Escape closes the dialog
- **WHEN** the user presses the Escape key while the dialog is focused
- **THEN** the dialog closes

#### Scenario: Dialog is not shown in the taskbar
- **WHEN** the Browse Plugins dialog is open
- **THEN** no taskbar entry is shown for the dialog
- **AND** the owner window (launch window) remains the only taskbar entry

#### Scenario: Dialog remains modal
- **WHEN** the Browse Plugins dialog is open
- **THEN** the owner window (launch window) is not interactive until the dialog is closed

#### Scenario: Default size is 640x480
- **WHEN** the Browse Plugins dialog is opened
- **THEN** its initial size is 640 pixels wide by 480 pixels tall
