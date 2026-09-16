# tray-menu — Spec Delta

## Purpose

Defines the behavior of the CLIHub system-tray context menu: when it opens,
where it is placed, which items it contains, and how it closes.

## ADDED Requirements

### Requirement: Menu placement

The system SHALL open the tray context menu at the cursor position and fully
inside the working area of the monitor under the cursor, on every open
including the first one after the application starts.

#### Scenario: First open after startup

- **WHEN** the user right-clicks the tray icon as the first menu interaction
  after the application starts, with the cursor near the taskbar
- **THEN** the menu opens adjacent to the cursor and does not overlap the
  taskbar

#### Scenario: Repeated open at the same cursor position

- **WHEN** the user opens the tray menu, closes it, and opens it again without
  moving the cursor
- **THEN** the menu opens at the same position as on the previous open

#### Scenario: Display scale differs from the primary display

- **WHEN** the cursor is on a display whose scale differs from the primary
  display's scale
- **THEN** the menu opens at the cursor position on that display

### Requirement: Menu items

The tray menu SHALL contain an exit item that terminates the application, and
SHALL contain an update item only while an update is ready to apply.

#### Scenario: No update ready

- **WHEN** the user opens the tray menu while no update is ready to apply
- **THEN** the menu contains only the exit item

#### Scenario: Update becomes ready during the session

- **WHEN** an update becomes ready to apply after the application started, and
  the user opens the tray menu
- **THEN** the menu contains the update item alongside the exit item, and
  activating it starts applying the update

#### Scenario: Exit

- **WHEN** the user activates the exit item
- **THEN** the application terminates

### Requirement: Menu dismissal

The tray menu SHALL close when the user clicks outside of it or activates one
of its items.

#### Scenario: Click outside

- **WHEN** the tray menu is open and the user clicks anywhere outside of it
- **THEN** the menu closes and the application keeps running
