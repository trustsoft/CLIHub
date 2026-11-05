# launch-window-layout Specification Delta

## MODIFIED Requirements

### Requirement: Scrolling lists

The system SHALL keep long project and agent lists usable within the window, and SHALL scroll both lists
continuously rather than in whole-row steps, so that scrolling either list to its end never leaves a blank
band below the last row.

#### Scenario: Long project list scrolls
- **WHEN** the registered projects do not fit in the Projects pane
- **THEN** the project list scrolls while the pane header and the footer stay in place
- **AND** when the project list is scrolled to its end, the last project row ends flush with the bottom of
  the Projects pane's list area, with no blank space below it

#### Scenario: Long agent list scrolls
- **WHEN** the available agents do not fit in the Agents pane
- **THEN** the agent list scrolls while the pane header and the footer stay in place
- **AND** when the agent list is scrolled to its end, the last agent row ends flush with the bottom of the
  Agents pane's list area, with no blank space below it

#### Scenario: Both lists share one scroll rhythm
- **WHEN** either the project list or the agent list is scrolled to its end
- **THEN** the last row of that list ends flush with its pane's list area, with the same result for both
  lists regardless of their row heights

### Requirement: Agents pane and row sizing

The Agents pane SHALL be sized to the content of its list, each agent row SHALL be sized to its own
content, and the agent row's layout height SHALL match the project row's layout height so a full viewport
of either kind of row fits without a scrollbar.

#### Scenario: Pane fits its list
- **WHEN** the launch window is shown
- **THEN** the Agents pane is as wide as its widest agent row requires, so no empty space is left beside
  the list

#### Scenario: Rows fill the list width
- **WHEN** the agents list is shown
- **THEN** every agent row spans the full width of the list, so all rows share one width

#### Scenario: Actions are right aligned
- **WHEN** an agent row is shown
- **THEN** its action buttons sit at the row's right edge, aligned with the buttons of the other rows

#### Scenario: Gap before the actions
- **WHEN** an agent row is shown
- **THEN** the space between the row's text (its name and version) and its action buttons is at least 24px

#### Scenario: Window fits its content
- **WHEN** the panes' content changes
- **THEN** the window's width follows the panes, so the window never keeps empty space beside its content

#### Scenario: A full viewport of agent rows needs no scrollbar
- **WHEN** the number of available agents exactly fills the Agents pane's maximum list height at the
  shared row pitch with the Projects pane
- **THEN** the Agents list shows no vertical scrollbar, matching the Projects list's behavior when it is
  exactly filled by projects at the same row count
