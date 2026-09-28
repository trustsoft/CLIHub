# main-window-layout Specification

## Purpose

Defines the main window's two-pane layout — a Projects pane and an AI Agents pane — including how the user can resize their proportions and how the panes stay visually aligned.

## Requirements

### Requirement: Resizable pane split

The system SHALL provide a draggable vertical splitter between the Projects pane and the AI Agents pane that lets the user change the width ratio of the two panes.

#### Scenario: Splitter is available
- **WHEN** the main window is shown
- **THEN** a vertical splitter is present in the gap between the Projects pane and the AI Agents pane

#### Scenario: Dragging changes pane widths
- **WHEN** the user drags the splitter horizontally
- **THEN** the Projects pane and the AI Agents pane resize so that the boundary follows the pointer

#### Scenario: Dragging does not overlap content
- **WHEN** the splitter is dragged
- **THEN** the panes are resized rather than overlapped, and the total available width is preserved

### Requirement: Pane width limits

The system SHALL enforce minimum widths for both panes so that neither pane can be collapsed to zero or made unusable.

#### Scenario: Minimum width respected
- **WHEN** the user drags the splitter toward either pane beyond its minimum width
- **THEN** the splitter stops at the minimum width and the pane remains usable

#### Scenario: Initial proportions
- **WHEN** the main window first opens
- **THEN** the Projects pane starts at its default width and the AI Agents pane takes the remaining width

### Requirement: Consistent pane alignment

The system SHALL align the two panes so their layout is visually consistent.

#### Scenario: Bottom action rows aligned
- **WHEN** the main window is shown
- **THEN** the bottom edge of the Projects pane's "Add Project..." action and the bottom edge of the AI Agents pane's action button row share the same vertical position

#### Scenario: Uniform outer margins
- **WHEN** the main window is shown
- **THEN** both panes use the same outer margin and padding on all sides
