# Spec Delta

## Purpose

Surfaces the user-facing release notes inside the application: a **What's New** window reachable from the system tray, and a one-time display after the application is upgraded to a new version.

## ADDED Requirements

### Requirement: What's New window

The application SHALL provide a **What's New** window that presents the user-facing release notes, newest version first, with the groups New, Improved, and Fixed.

#### Scenario: Opened from the tray
- **WHEN** the user chooses "What's New" from the tray menu
- **THEN** the window opens showing the release notes

#### Scenario: Newest version first
- **WHEN** the window shows more than one version
- **THEN** the newest version appears first and older versions follow

#### Scenario: Version and date shown
- **WHEN** a version is shown
- **THEN** its version number and date are displayed with it

#### Scenario: Only non-empty groups are shown
- **WHEN** a version has no entries in one of the New, Improved, or Fixed groups
- **THEN** that group is not displayed for that version

#### Scenario: Long history scrolls
- **WHEN** the notes do not fit in the window
- **THEN** the notes area scrolls while the window header stays in place

#### Scenario: No notes available
- **WHEN** the notes cannot be read
- **THEN** the window opens showing a message that no release notes are available instead of failing

#### Scenario: Single window
- **WHEN** the window is already open and the user chooses "What's New" again
- **THEN** the existing window is brought to the front instead of opening a second one

### Requirement: One-time display after an upgrade

The application SHALL record the version whose notes the user has seen, and SHALL show the **What's New** window once when it starts at a version that differs from the recorded one.

#### Scenario: Shown once after an upgrade
- **WHEN** the application starts at a version that differs from the recorded version and the user has not been shown that version's notes
- **THEN** the **What's New** window opens once and the current version is recorded

#### Scenario: Not shown again on the next start
- **WHEN** the application starts again at the same version whose notes were already shown
- **THEN** the **What's New** window is not opened automatically

#### Scenario: First run does not open the window
- **WHEN** the application starts and no version has been recorded yet
- **THEN** the **What's New** window is not opened automatically and the current version is recorded

#### Scenario: Notes missing for the running version
- **WHEN** the application starts at a version that differs from the recorded one but no notes exist for that version
- **THEN** nothing is opened and the current version is recorded so the check does not repeat

#### Scenario: Opening manually does not disturb the record
- **WHEN** the user opens the window from the tray
- **THEN** the recorded version is updated to the running version

#### Scenario: Recorded version persists
- **WHEN** the application is closed and started again at the same version
- **THEN** the recorded version is still the one stored in configuration

#### Scenario: Recorded version cannot be written
- **WHEN** the recorded version cannot be persisted
- **THEN** the failure is logged and the application continues normally
